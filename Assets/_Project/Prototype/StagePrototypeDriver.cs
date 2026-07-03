using System.Collections.Generic;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KaijuBreaker.Prototype
{
    /// <summary>
    /// THROWAWAY vertical-slice prototype of the first-stage combat loop. Builds the real
    /// <see cref="PartStateSystem"/> + laser/missile weapon classes entirely in code (config SOs
    /// created at runtime with sensible defaults — no .asset wiring needed), spawns crude sprite
    /// visuals for the player and a two-part boss, and drives the loop:
    ///   move (keyboard / mouse-drag / touch) → laser heats a part → SOFTENED → missile breaks it →
    ///   break the BossCore → STAGE CLEAR.
    /// Proves the dual-track soften→break chain end-to-end in a live scene. Delete the Prototype
    /// folder once real scenes/prefabs (stage + hud-ui epics) exist.
    /// </summary>
    public sealed class StagePrototypeDriver : MonoBehaviour
    {
        private const int KaijuId = 1;
        private const float MoveSpeed = 9f;

        private IEventBus _bus;
        private PartStateSystem _parts;
        private L1SpreadLaser _laser;
        private M1HomingMissile _missile;
        private ResidualHeatTracker _residual;

        private Transform _player;
        private readonly Dictionary<int, PartVisual> _visuals = new Dictionary<int, PartVisual>();
        private Sprite _sprite;
        private Camera _cam;
        private bool _cleared;
        private string _hint = "";

        private sealed class PartVisual
        {
            public SpriteRenderer Body;
            public Transform HeatBar;   // scale.x = heat ratio
            public Transform BreakBar;  // scale.x = break ratio
            public PartType Type;
        }

        private void Start()
        {
            SetupCamera();
            _sprite = MakeSprite();

            // ── Build config + content in code (prototype: defaults are fine) ──────
            var balance = ScriptableObject.CreateInstance<WeaponBalanceConfig>();
            var partConfig = ScriptableObject.CreateInstance<PartSystemConfig>();
            var laserDef = MakeWeapon(WeaponId.L1, WeaponType.Laser);
            var missileDef = MakeWeapon(WeaponId.M1, WeaponType.Missile);
            var kaiju = BuildBoss();

            // ── Systems ───────────────────────────────────────────────────────────
            _bus = new TypedEventBus();
            _parts = new PartStateSystem(_bus, balance, partConfig);
            _parts.InitializeParts(kaiju, KaijuId);
            _residual = new ResidualHeatTracker(_bus);

            var tier = new FixedTierQuery();
            _laser = new L1SpreadLaser(_bus, tier, _parts, balance, laserDef, _residual);
            _missile = new M1HomingMissile(_bus, tier, _parts, balance, missileDef);
            _laser.Enable();
            _missile.Enable();

            _bus.Subscribe<PartSoftened>(OnSoftened);
            _bus.Subscribe<PartBroke>(OnBroke);
            _bus.Subscribe<BossCoreBroke>(_ => { _cleared = true; });

            SpawnPlayer();
            SpawnParts(kaiju);
        }

        // ── Per-frame loop ────────────────────────────────────────────────────────

        private void Update()
        {
            if (_cleared) return;
            float dt = Time.deltaTime;

            MovePlayer(dt);
            FireLaser(dt);
            HandleMissileInput();

            _missile.Tick(dt);
            _parts.Tick(dt);

            RefreshBars();
        }

        private void MovePlayer(float dt)
        {
            Vector2 move = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
            }

            Vector3 pos = _player.position + (Vector3)(move.normalized * MoveSpeed * dt);

            // Pointer / touch drag: follow the held pointer (Sky-Force style).
            Vector2 ptr = default; bool held = false;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            { ptr = Touchscreen.current.primaryTouch.position.ReadValue(); held = true; }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            { ptr = Mouse.current.position.ReadValue(); held = true; }
            if (held)
            {
                Vector3 world = _cam.ScreenToWorldPoint(new Vector3(ptr.x, ptr.y, -_cam.transform.position.z));
                pos = Vector3.Lerp(pos, new Vector3(world.x, world.y, 0f), 12f * dt);
            }

            // Clamp to the camera view.
            float halfH = _cam.orthographicSize, halfW = halfH * _cam.aspect;
            pos.x = Mathf.Clamp(pos.x, -halfW + 0.5f, halfW - 0.5f);
            pos.y = Mathf.Clamp(pos.y, -halfH + 0.5f, halfH - 0.5f);
            pos.z = 0f;
            _player.position = pos;
        }

        private void FireLaser(float dt)
        {
            // Raycast straight up from the player; heat whatever part it hits.
            var hit = Physics2D.Raycast(_player.position, Vector2.up, 100f);
            if (hit.collider == null) { _hint = "move under a part to heat it (laser auto-fires up)"; return; }

            var pv = hit.collider.GetComponent<PartTag>();
            if (pv == null) return;
            int beams = _laser.ExpectedBeamCount;
            var targets = new int[beams];
            for (int i = 0; i < beams; i++) targets[i] = pv.PartId;
            _laser.FireFrame(dt, KaijuId, targets);
            _hint = "SPACE / tap-hold-lower = fire missiles (break SOFTENED parts)";
        }

        private void HandleMissileInput()
        {
            bool fire = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            // second touch = missile
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 1 &&
                Touchscreen.current.touches[1].press.wasPressedThisFrame) fire = true;
            if (!fire) return;

            int target = _parts.GetHottestSoftenedPartId();
            if (target < 0) target = _parts.GetHottestAlivePartId();
            if (target >= 0) _missile.TryFire(target, KaijuId);
        }

        // ── Event reactions (visual feedback) ───────────────────────────────────

        private void OnSoftened(PartSoftened e)
        {
            if (_visuals.TryGetValue(e.PartId, out var v))
                v.Body.color = new Color(1f, 0.4f, 0.1f); // SOFTENED = orange (art-bible reserved)
        }

        private void OnBroke(PartBroke e)
        {
            if (_visuals.TryGetValue(e.PartId, out var v))
            {
                v.Body.color = new Color(0.25f, 0.25f, 0.25f, 0.35f);
                if (v.HeatBar) v.HeatBar.localScale = new Vector3(0, v.HeatBar.localScale.y, 1);
                if (v.BreakBar) v.BreakBar.localScale = new Vector3(0, v.BreakBar.localScale.y, 1);
            }
        }

        private void RefreshBars()
        {
            foreach (var kv in _parts.Parts)
            {
                if (!_visuals.TryGetValue(kv.Key, out var v)) continue;
                var p = kv.Value;
                if (p.BreakState == BreakState.Broken) continue;
                if (v.HeatBar) v.HeatBar.localScale = new Vector3(Mathf.Clamp01(p.HCurrent / p.HMax) * 1.6f, 0.12f, 1f);
                if (v.BreakBar) v.BreakBar.localScale = new Vector3(Mathf.Clamp01(p.BCurrent / p.BMax) * 1.6f, 0.12f, 1f);
                if (p.HeatState == HeatState.Intact && v.Body.color.r < 0.9f)
                    v.Body.color = new Color(0.6f, 0.62f, 0.7f); // INTACT slate
            }
        }

        // ── Spawning ────────────────────────────────────────────────────────────

        private void SpawnPlayer()
        {
            var go = new GameObject("Player");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite; sr.color = new Color(0.3f, 0.8f, 1f);
            go.transform.localScale = new Vector3(0.7f, 0.9f, 1f);
            go.transform.position = new Vector3(0, -_cam.orthographicSize + 1.2f, 0);
            _player = go.transform;
        }

        private void SpawnParts(KaijuDef kaiju)
        {
            float top = _cam.orthographicSize - 1.5f;
            var parts = kaiju.Parts;
            for (int i = 0; i < parts.Length; i++)
            {
                int id = _parts.GetPartId(parts[i].PartId);
                if (id < 0) continue;
                float x = (i - (parts.Length - 1) * 0.5f) * 2.4f;
                var pos = new Vector2(x, top);
                _parts.SetWorldPosition(id, pos);

                var go = new GameObject("Part_" + parts[i].PartId);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(1.8f, 1.4f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = parts[i].PartType == PartType.BossCore
                    ? new Color(0.8f, 0.5f, 0.5f) : new Color(0.6f, 0.62f, 0.7f);
                var col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
                go.AddComponent<PartTag>().PartId = id;

                var heat = MakeBar(go.transform, new Vector3(0, 0.62f, 0), new Color(1f, 0.55f, 0.1f));
                var brk = MakeBar(go.transform, new Vector3(0, 0.78f, 0), new Color(0.2f, 0.8f, 1f));

                _visuals[id] = new PartVisual { Body = sr, HeatBar = heat, BreakBar = brk, Type = parts[i].PartType };
            }
        }

        private Transform MakeBar(Transform parent, Vector3 localPos, Color c)
        {
            var go = new GameObject("Bar");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(0f, 0.12f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite; sr.color = c; sr.sortingOrder = 5;
            return go.transform;
        }

        // ── Setup helpers ─────────────────────────────────────────────────────────

        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                _cam = go.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = 6f;
            _cam.transform.position = new Vector3(0, 0, -10);
            _cam.backgroundColor = new Color(0.05f, 0.06f, 0.1f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private static Sprite MakeSprite()
        {
            // 1×1 white texture at 1 pixel-per-unit → a 1-world-unit sprite; size is set via localScale.
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static WeaponDef MakeWeapon(WeaponId id, WeaponType type)
        {
            var w = ScriptableObject.CreateInstance<WeaponDef>();
            SetPrivate(w, "_id", id);
            SetPrivate(w, "_type", type);
            return w;
        }

        private KaijuDef BuildBoss()
        {
            var carapace = NewPart("carapace", PartType.Normal, "proto_drop");
            var core = NewPart("core", PartType.BossCore, "proto_drop");
            var k = ScriptableObject.CreateInstance<KaijuDef>();
            SetPrivate(k, "_kaijuId", "proto_boss");
            SetPrivate(k, "_parts", new[] { carapace, core });
            return k;
        }

        private static PartDef NewPart(string id, PartType type, string drop)
        {
            var p = new PartDef();
            SetPrivate(p, "_partId", id);
            SetPrivate(p, "_partType", type);
            SetPrivate(p, "_dropTableId", drop);
            SetPrivate(p, "_adjacency", new string[0]);
            return p;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var t = target.GetType();
            FieldInfo fi = null;
            while (t != null && fi == null)
            {
                fi = t.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                t = t.BaseType;
            }
            fi?.SetValue(target, value);
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            GUI.Label(new Rect(12, 8, 900, 26), "殲獸戰機 — Stage 01 (prototype)   移動:WASD/方向鍵/拖曳", style);
            GUI.Label(new Rect(12, 34, 900, 26), _hint, style);
            GUI.Label(new Rect(12, 60, 900, 26), "飛彈彈藥: " + _missile?.Ammo + (_missile != null && _missile.IsReloading ? " (裝填中)" : ""), style);

            if (_cleared)
            {
                var win = new GUIStyle(GUI.skin.label)
                { fontSize = 54, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 80), "STAGE CLEAR!", win);
            }
        }

        private sealed class FixedTierQuery : IWeaponTierQuery
        {
            public int GetTier(WeaponId weapon) => 0;
        }
    }

    /// <summary>Tags a part collider with its runtime part id so the laser raycast can resolve a target.</summary>
    public sealed class PartTag : MonoBehaviour
    {
        public int PartId;
    }
}
