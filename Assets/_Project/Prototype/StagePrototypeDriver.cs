using System.Collections.Generic;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace KaijuBreaker.Prototype
{
    /// <summary>
    /// Playable first-stage prototype (parity target: prototypes/vision-slice/prototype.html).
    /// Real <see cref="PartStateSystem"/> + L1 laser + M1 missiles drive the dual-track loop; the boss
    /// fires bullet patterns at the player (danger), the player has HP, and there is a win (break the
    /// BossCore) and a lose (HP → 0, press R to retry). Throwaway; delete once real scenes/hud-ui land.
    /// Controls: WASD/arrows or mouse/touch drag = move · laser auto-fires up · SPACE = missiles.
    /// </summary>
    public sealed class StagePrototypeDriver : MonoBehaviour
    {
        private const int KaijuId = 1;
        private const float MoveSpeed = 10f;
        private const int MaxHp = 5;

        // Systems
        private IEventBus _bus;
        private PartStateSystem _parts;
        private L1SpreadLaser _laser;
        private M1HomingMissile _missile;
        private ResidualHeatTracker _residual;

        // Scene
        private Camera _cam;
        private Transform _player;
        private LineRenderer _beam;
        private Sprite _sprite;
        private readonly Dictionary<int, PartVis> _vis = new Dictionary<int, PartVis>();
        private readonly List<Bullet> _bullets = new List<Bullet>(256);
        private readonly List<Spark> _sparks = new List<Spark>(128);
        private readonly List<Transform> _bg = new List<Transform>(24);

        // State
        private int _hp = MaxHp;
        private bool _cleared, _dead;
        private float _invuln, _shake, _flash, _missileFx;
        private readonly Dictionary<int, float> _partFireTimer = new Dictionary<int, float>();
        private string _hint = "";
        private float _bounds;

        private sealed class PartVis { public SpriteRenderer Body; public Transform Heat, Break; }
        private sealed class Bullet { public GameObject Go; public Vector2 Vel; }
        private sealed class Spark { public GameObject Go; public Vector2 Vel; public float Life; }

        private void Start()
        {
            SetupCamera();
            _sprite = MakeSprite();

            var balance = ScriptableObject.CreateInstance<WeaponBalanceConfig>();
            var partConfig = ScriptableObject.CreateInstance<PartSystemConfig>();
            var laserDef = MakeWeapon(WeaponId.L1, WeaponType.Laser);
            var missileDef = MakeWeapon(WeaponId.M1, WeaponType.Missile);
            var kaiju = BuildBoss();

            _bus = new TypedEventBus();
            _parts = new PartStateSystem(_bus, balance, partConfig);
            _parts.InitializeParts(kaiju, KaijuId);
            _residual = new ResidualHeatTracker(_bus);
            var tier = new FixedTierQuery();
            _laser = new L1SpreadLaser(_bus, tier, _parts, balance, laserDef, _residual);
            _missile = new M1HomingMissile(_bus, tier, _parts, balance, missileDef);
            _laser.Enable(); _missile.Enable();

            _bus.Subscribe<PartSoftened>(e => Tint(e.PartId, new Color(1f, 0.42f, 0.12f)));
            _bus.Subscribe<PartBroke>(OnBroke);
            _bus.Subscribe<BossCoreBroke>(_ => { _cleared = true; _shake = 0.6f; _flash = 0.6f; });

            BuildBackground();
            SpawnPlayer();
            SpawnParts(kaiju);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            if (_dead || _cleared) { HandleEndInput(); TickFx(dt); return; }

            MovePlayer(dt);
            FireLaser(dt);
            HandleMissileInput();
            BossFire(dt);
            MoveBullets(dt);

            _missile.Tick(dt);
            _residual.Tick(dt);
            _parts.Tick(dt);
            RefreshBars();
            TickFx(dt);
            ScrollBackground(dt);
        }

        // ── Player ──────────────────────────────────────────────────────────────

        private void MovePlayer(float dt)
        {
            Vector2 m = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) m.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) m.x += 1;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) m.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) m.y -= 1;
            }
            Vector3 p = _player.position + (Vector3)(m.normalized * MoveSpeed * dt);

            Vector2 ptr = default; bool held = false;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            { ptr = Touchscreen.current.primaryTouch.position.ReadValue(); held = true; }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            { ptr = Mouse.current.position.ReadValue(); held = true; }
            if (held)
            {
                var w = _cam.ScreenToWorldPoint(new Vector3(ptr.x, ptr.y, 10f));
                p = Vector3.Lerp(p, new Vector3(w.x, w.y, 0f), 14f * dt);
            }

            float hH = _cam.orthographicSize, hW = hH * _cam.aspect;
            p.x = Mathf.Clamp(p.x, -hW + 0.4f, hW - 0.4f);
            p.y = Mathf.Clamp(p.y, -hH + 0.4f, hH - 0.4f);
            p.z = 0;
            _player.position = p;
        }

        private void FireLaser(float dt)
        {
            var hit = Physics2D.Raycast(_player.position, Vector2.up, 100f);
            if (hit.collider != null && hit.collider.TryGetComponent<PartTag>(out var tag))
            {
                int n = _laser.ExpectedBeamCount;
                var beams = new int[n];
                for (int i = 0; i < n; i++) beams[i] = tag.PartId;
                _laser.FireFrame(dt, KaijuId, beams);
                DrawBeam(_player.position, hit.point, true);
                _hint = "SPACE = fire missiles at SOFTENED parts";
            }
            else { DrawBeam(_player.position, _player.position + Vector3.up * 12f, false); _hint = "move under a part — laser heats it → SOFTENED (orange)"; }
        }

        private void HandleMissileInput()
        {
            bool fire = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 1 &&
                Touchscreen.current.touches[1].press.wasPressedThisFrame) fire = true;
            if (!fire) return;
            int t = _parts.GetHottestSoftenedPartId();
            if (t < 0) t = _parts.GetHottestAlivePartId();
            if (t >= 0 && _missile.TryFire(t, KaijuId))
            {
                _missileFx = 0.12f;
                if (_vis.TryGetValue(t, out var v)) SpawnSparks(v.Body.transform.position, new Color(1f, 0.8f, 0.3f), 5);
            }
        }

        private void Damage()
        {
            if (_invuln > 0f) return;
            _hp--; _invuln = 1.0f; _shake = 0.35f; _flash = 0.4f;
            if (_hp <= 0) { _dead = true; _shake = 0.7f; }
        }

        // ── Boss firing (danger) ────────────────────────────────────────────────

        private void BossFire(float dt)
        {
            foreach (var kv in _parts.Parts)
            {
                if (kv.Value.BreakState == BreakState.Broken) continue;
                if (!_partFireTimer.TryGetValue(kv.Key, out float t)) t = Random.Range(0.5f, 1.6f);
                t -= dt;
                if (t <= 0f)
                {
                    FirePattern(kv.Value.WorldPosition);
                    t = Random.Range(1.1f, 1.9f);
                }
                _partFireTimer[kv.Key] = t;
            }
        }

        private void FirePattern(Vector2 from)
        {
            // Aimed 5-shot spread toward the player.
            Vector2 toP = ((Vector2)_player.position - from).normalized;
            float baseAng = Mathf.Atan2(toP.y, toP.x) * Mathf.Rad2Deg;
            for (int i = -2; i <= 2; i++)
            {
                float a = (baseAng + i * 12f) * Mathf.Deg2Rad;
                var vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 5.5f;
                SpawnBullet(from, vel);
            }
        }

        private void SpawnBullet(Vector2 pos, Vector2 vel)
        {
            var go = new GameObject("b");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite; sr.color = new Color(1f, 0.3f, 0.35f); sr.sortingOrder = 3;
            _bullets.Add(new Bullet { Go = go, Vel = vel });
        }

        private void MoveBullets(float dt)
        {
            float hH = _cam.orthographicSize + 1f, hW = hH * _cam.aspect + 1f;
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var b = _bullets[i];
                b.Go.transform.position += (Vector3)(b.Vel * dt);
                Vector2 bp = b.Go.transform.position;
                if (Vector2.Distance(bp, _player.position) < 0.42f) { Damage(); Destroy(b.Go); _bullets.RemoveAt(i); continue; }
                if (Mathf.Abs(bp.x) > hW || Mathf.Abs(bp.y) > hH) { Destroy(b.Go); _bullets.RemoveAt(i); }
            }
        }

        // ── Events / visuals ────────────────────────────────────────────────────

        private void OnBroke(PartBroke e)
        {
            _shake = 0.4f; _flash = 0.3f;
            if (_vis.TryGetValue(e.PartId, out var v))
            {
                SpawnSparks(v.Body.transform.position, new Color(1f, 0.7f, 0.2f), 14);
                v.Body.color = new Color(0.22f, 0.22f, 0.25f, 0.3f);
                if (v.Heat) v.Heat.localScale = new Vector3(0, v.Heat.localScale.y, 1);
                if (v.Break) v.Break.localScale = new Vector3(0, v.Break.localScale.y, 1);
            }
        }

        private void Tint(int id, Color c) { if (_vis.TryGetValue(id, out var v)) v.Body.color = c; }

        private void RefreshBars()
        {
            foreach (var kv in _parts.Parts)
            {
                if (!_vis.TryGetValue(kv.Key, out var v) || kv.Value.BreakState == BreakState.Broken) continue;
                var p = kv.Value;
                if (v.Heat) v.Heat.localScale = new Vector3(Mathf.Clamp01(p.HCurrent / p.HMax) * 1.7f, 0.12f, 1);
                if (v.Break) v.Break.localScale = new Vector3(Mathf.Clamp01(p.BCurrent / p.BMax) * 1.7f, 0.12f, 1);
                if (p.HeatState == HeatState.Intact && v.Body.color.r < 0.9f) v.Body.color = new Color(0.62f, 0.64f, 0.72f);
            }
        }

        private void SpawnSparks(Vector2 at, Color c, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("s");
                go.transform.position = at;
                go.transform.localScale = Vector3.one * 0.16f;
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.color = c; sr.sortingOrder = 6;
                float a = Random.value * 6.28f, sp = Random.Range(2f, 7f);
                _sparks.Add(new Spark { Go = go, Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp, Life = 0.5f });
            }
        }

        private void TickFx(float dt)
        {
            if (_invuln > 0f) _invuln -= dt;
            if (_shake > 0f) _shake = Mathf.Max(0f, _shake - dt * 2.5f);
            if (_flash > 0f) _flash = Mathf.Max(0f, _flash - dt * 2.5f);
            if (_missileFx > 0f) _missileFx -= dt;

            Vector3 basePos = new Vector3(0, 0, -10);
            _cam.transform.position = basePos + (Vector3)(Random.insideUnitCircle * _shake * 0.5f);

            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var s = _sparks[i]; s.Life -= dt;
                s.Go.transform.position += (Vector3)(s.Vel * dt);
                var sr = s.Go.GetComponent<SpriteRenderer>();
                var c = sr.color; c.a = Mathf.Clamp01(s.Life * 2f); sr.color = c;
                if (s.Life <= 0f) { Destroy(s.Go); _sparks.RemoveAt(i); }
            }
            // blink player while invulnerable
            if (_player != null)
            {
                var psr = _player.GetComponent<SpriteRenderer>();
                psr.color = (_invuln > 0f && Mathf.Repeat(_invuln, 0.2f) < 0.1f)
                    ? new Color(1f, 1f, 1f, 0.4f) : new Color(0.3f, 0.85f, 1f);
            }
        }

        // ── Background scroll (前進感) ───────────────────────────────────────────

        private void BuildBackground()
        {
            float hH = _cam.orthographicSize, hW = hH * _cam.aspect;
            for (int i = 0; i < 22; i++)
            {
                var go = new GameObject("bg");
                go.transform.position = new Vector3(Random.Range(-hW, hW), Random.Range(-hH, hH), 5);
                float s = Random.Range(0.05f, 0.16f);
                go.transform.localScale = new Vector3(s, s * 3f, 1);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite; sr.color = new Color(0.3f, 0.35f, 0.5f, Random.Range(0.15f, 0.4f)); sr.sortingOrder = -5;
                _bg.Add(go.transform);
            }
        }

        private void ScrollBackground(float dt)
        {
            float hH = _cam.orthographicSize, hW = hH * _cam.aspect;
            foreach (var t in _bg)
            {
                var p = t.position; p.y -= (2f + t.localScale.x * 30f) * dt;
                if (p.y < -hH) { p.y = hH; p.x = Random.Range(-hW, hW); }
                t.position = p;
            }
        }

        // ── Spawning / setup ────────────────────────────────────────────────────

        private void SpawnPlayer()
        {
            var go = new GameObject("Player");
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.color = new Color(0.3f, 0.85f, 1f); sr.sortingOrder = 2;
            go.transform.localScale = new Vector3(0.6f, 0.85f, 1f);
            go.transform.position = new Vector3(0, -_cam.orthographicSize + 1.1f, 0);
            _player = go.transform;

            var beamGo = new GameObject("Beam");
            _beam = beamGo.AddComponent<LineRenderer>();
            _beam.material = new Material(Shader.Find("Sprites/Default"));
            _beam.widthMultiplier = 0.12f; _beam.positionCount = 2; _beam.sortingOrder = 1; _beam.enabled = false;
        }

        private void DrawBeam(Vector3 a, Vector3 b, bool hot)
        {
            _beam.enabled = true;
            _beam.SetPosition(0, a); _beam.SetPosition(1, b);
            var c = hot ? new Color(1f, 0.5f, 0.15f, 0.9f) : new Color(0.4f, 0.9f, 1f, 0.5f);
            _beam.startColor = c; _beam.endColor = new Color(c.r, c.g, c.b, 0.1f);
        }

        private void SpawnParts(KaijuDef kaiju)
        {
            float top = _cam.orthographicSize - 1.4f;
            var parts = kaiju.Parts;
            for (int i = 0; i < parts.Length; i++)
            {
                int id = _parts.GetPartId(parts[i].PartId);
                if (id < 0) continue;
                float x = (i - (parts.Length - 1) * 0.5f) * 2.6f;
                var pos = new Vector2(x, top - (parts[i].PartType == PartType.BossCore ? 0f : 0.2f));
                _parts.SetWorldPosition(id, pos);

                var go = new GameObject("Part_" + parts[i].PartId);
                go.transform.position = pos;
                go.transform.localScale = parts[i].PartType == PartType.BossCore ? new Vector3(2.0f, 1.6f, 1) : new Vector3(1.7f, 1.3f, 1);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.sortingOrder = 2;
                sr.color = parts[i].PartType == PartType.BossCore ? new Color(0.85f, 0.5f, 0.5f) : new Color(0.62f, 0.64f, 0.72f);
                go.AddComponent<BoxCollider2D>().size = Vector2.one;
                go.AddComponent<PartTag>().PartId = id;

                _vis[id] = new PartVis
                {
                    Body = sr,
                    Heat = MakeBar(go.transform, new Vector3(0, 0.62f, 0), new Color(1f, 0.55f, 0.1f)),
                    Break = MakeBar(go.transform, new Vector3(0, 0.78f, 0), new Color(0.2f, 0.8f, 1f))
                };
            }
        }

        private Transform MakeBar(Transform parent, Vector3 lp, Color c)
        {
            var go = new GameObject("Bar"); go.transform.SetParent(parent, false);
            go.transform.localPosition = lp; go.transform.localScale = new Vector3(0, 0.12f, 1);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.color = c; sr.sortingOrder = 5;
            return go.transform;
        }

        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null) { var go = new GameObject("Main Camera") { tag = "MainCamera" }; _cam = go.AddComponent<Camera>(); }
            _cam.orthographic = true; _cam.orthographicSize = 6f;
            _cam.transform.position = new Vector3(0, 0, -10);
            _cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f); _cam.clearFlags = CameraClearFlags.SolidColor;
            _bounds = _cam.orthographicSize;
        }

        private void HandleEndInput()
        {
            bool r = (Keyboard.current != null && (Keyboard.current.rKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                     || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                     || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            if (!r) return;
            if (_cleared) SceneManager.LoadScene("MainMenu");
            else SceneManager.LoadScene("Stage01Prototype");
        }

        private void OnGUI()
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            GUI.Label(new Rect(12, 8, 900, 26), "殲獸戰機 — Stage 01   HP: " + new string('♥', Mathf.Max(0, _hp)) + new string('·', MaxHp - Mathf.Max(0, _hp)), s);
            GUI.Label(new Rect(12, 34, 900, 26), _hint, s);
            GUI.Label(new Rect(12, 60, 900, 26), "飛彈: " + _missile?.Ammo + (_missile != null && _missile.IsReloading ? " (裝填中)" : ""), s);

            if (_cleared || _dead)
            {
                var big = new GUIStyle(GUI.skin.label) { fontSize = 52, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 80), _cleared ? "STAGE CLEAR!" : "GAME OVER", big);
                var sub = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, Screen.height * 0.52f, Screen.width, 40), _cleared ? "click / R = 回主選單" : "click / R = 重試", sub);
            }
        }

        // ── Content built in code (prototype) ───────────────────────────────────

        private static Sprite MakeSprite()
        {
            var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static WeaponDef MakeWeapon(WeaponId id, WeaponType type)
        {
            var w = ScriptableObject.CreateInstance<WeaponDef>();
            SetPrivate(w, "_id", id); SetPrivate(w, "_type", type); return w;
        }

        private KaijuDef BuildBoss()
        {
            var k = ScriptableObject.CreateInstance<KaijuDef>();
            SetPrivate(k, "_kaijuId", "proto_boss");
            SetPrivate(k, "_parts", new[]
            {
                NewPart("left_claw", PartType.Normal), NewPart("right_claw", PartType.Normal), NewPart("core", PartType.BossCore)
            });
            return k;
        }

        private static PartDef NewPart(string id, PartType type)
        {
            var p = new PartDef();
            SetPrivate(p, "_partId", id); SetPrivate(p, "_partType", type);
            SetPrivate(p, "_dropTableId", "proto_drop"); SetPrivate(p, "_adjacency", new string[0]);
            return p;
        }

        private static void SetPrivate(object o, string f, object v)
        {
            var t = o.GetType(); FieldInfo fi = null;
            while (t != null && fi == null) { fi = t.GetField(f, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); t = t.BaseType; }
            fi?.SetValue(o, v);
        }

        private sealed class FixedTierQuery : IWeaponTierQuery { public int GetTier(WeaponId weapon) => 0; }
    }

    /// <summary>Tags a part collider with its runtime part id for the laser raycast.</summary>
    public sealed class PartTag : MonoBehaviour { public int PartId; }
}
