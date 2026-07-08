using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Owns the boss encounter on the scene side, for a ROSTER of bosses (target-select hub): on
    /// <see cref="BeginBossFight"/> it activates the chosen boss, initialises the real
    /// <see cref="KaijuBreaker.KaijuParts.PartStateSystem"/> from its <see cref="KaijuDef"/>, binds each scene
    /// <see cref="BossPart"/> to its runtime id + world position, transitions the run into BOSS, then ticks the
    /// part system each frame so heat fills and softening/stagger resolve. It reacts to the system's events —
    /// hiding a part on <see cref="PartBroke"/>, ending on <see cref="BossCoreBroke"/> (the win, which
    /// <see cref="Stage.RunController"/> turns into RESULTS + <see cref="HuntEnded"/>). Every combat rule lives
    /// in the injected systems (ADR-0005).
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class BossController : MonoBehaviour
    {
        /// <summary>One selectable boss: its data, runtime id, and the scene part hierarchy (hidden until chosen).</summary>
        [Serializable]
        public struct BossEntry
        {
            public KaijuDef Kaiju;
            public int KaijuId;
            public GameObject BossRoot;
        }

        [Tooltip("Boss roster, indexed by the target-select order (0 = CARAPEX …). Each has a KaijuDef + hidden root.")]
        [SerializeField] private BossEntry[] _bosses = Array.Empty<BossEntry>();

        [Tooltip("Damage each boss-part bullet deals to the player on hit (warm enemy bullets, Mono pool).")]
        [SerializeField] private float _bulletDamage = 8f;

        // One live emission source belonging to a part (per-part-firing-schema.md §3). Bullet emitters fire via
        // the shared pool; spawner slots (SpawnEnemyId) are noted but minion spawning is a follow-up.
        private struct ActiveEmitter
        {
            public int OwnerPartId;
            public EmitterPatternSO Pattern;
            public PartFireGate Gate;
            public int GatePartId;      // resolved from GatePartId string, −1 when none
            public bool IsSpawner;
            public float Cooldown;
            public float SpinPhaseDeg;
        }

        private GameComposition _comp;
        private BossEntry _active;
        private readonly Dictionary<int, BossPart> _partsById = new Dictionary<int, BossPart>();
        private readonly Dictionary<int, PartDef> _partDefById = new Dictionary<int, PartDef>();
        private readonly Dictionary<int, Vector3> _partBaseLocal = new Dictionary<int, Vector3>();
        private readonly List<ActiveEmitter> _emitters = new List<ActiveEmitter>();
        private readonly HashSet<int> _brokenPartIds = new HashSet<int>();
        private EnemyBulletPool _bulletPool;
        private Transform _playerTarget;
        private Action<PartBroke> _onPartBroke;
        private Action<BossCoreBroke> _onBossCoreBroke;
        private bool _fighting;
        private Vector3 _rootBase;
        private float _idleT;

        /// <summary>True while a boss fight is live (parts ticking).</summary>
        public bool Fighting => _fighting;

        /// <summary>Number of bosses in the roster.</summary>
        public int BossCount => _bosses != null ? _bosses.Length : 0;

        /// <summary>The active boss's total breakable part count (incl. core), for the run's full-clear check.</summary>
        public int BreakablePartCount => _active.Kaiju != null && _active.Kaiju.Parts != null ? _active.Kaiju.Parts.Length : 0;

        private void Awake()
        {
            if (_bosses == null) return;
            for (int i = 0; i < _bosses.Length; i++) // hide every boss through the 道中
                if (_bosses[i].BossRoot != null) _bosses[i].BossRoot.SetActive(false);
        }

        /// <summary>
        /// Start the fight against boss <paramref name="bossIndex"/>: activate it, initialise the part system,
        /// bind scene parts, enter BOSS, begin ticking. Called when the wave sequence clears.
        /// </summary>
        public void BeginBossFight(GameComposition comp, int bossIndex,
                                   EnemyBulletPool bulletPool = null, Transform playerTarget = null)
        {
            if (_fighting) return;
            _comp = comp;
            _bulletPool = bulletPool;
            _playerTarget = playerTarget;
            if (_comp == null || _bosses == null || bossIndex < 0 || bossIndex >= _bosses.Length)
            { Debug.LogError("[BossController] Invalid composition or boss index " + bossIndex + "."); return; }

            _active = _bosses[bossIndex];
            if (_active.Kaiju == null) { Debug.LogError("[BossController] Boss " + bossIndex + " has no KaijuDef."); return; }

            _comp.Parts.InitializeParts(_active.Kaiju, _active.KaijuId);
            _comp.Themes.Register(_active.KaijuId, _active.Kaiju.Theme); // Economy sources the theme core on break

            if (_active.BossRoot != null) _active.BossRoot.SetActive(true);
            _rootBase = _active.BossRoot != null ? _active.BossRoot.transform.position : Vector3.zero;
            _idleT = 0f;

            _partsById.Clear();
            _partDefById.Clear();
            _partBaseLocal.Clear();
            _emitters.Clear();
            _brokenPartIds.Clear();

            // Map authored part-id string -> PartDef so bound scene parts can pull their emitters/movement.
            var nameToDef = new Dictionary<string, PartDef>();
            if (_active.Kaiju.Parts != null)
                foreach (var pd in _active.Kaiju.Parts)
                    if (pd != null && !string.IsNullOrEmpty(pd.PartId)) nameToDef[pd.PartId] = pd;

            var parts = _active.BossRoot != null ? _active.BossRoot.GetComponentsInChildren<BossPart>(true) : Array.Empty<BossPart>();
            foreach (var bp in parts)
            {
                int id = _comp.Parts.GetPartId(bp.PartName);
                if (id < 0) { Debug.LogWarning("[BossController] BossPart '" + bp.PartName + "' has no matching PartDef."); continue; }
                bp.Configure(id, _active.KaijuId, _comp.Bus);
                _comp.Parts.SetWorldPosition(id, bp.transform.position);
                _partsById[id] = bp;
                _partBaseLocal[id] = bp.transform.localPosition;
                if (nameToDef.TryGetValue(bp.PartName, out var def)) _partDefById[id] = def;
            }

            BuildEmitters(nameToDef);

            _onPartBroke = OnPartBroke;
            _onBossCoreBroke = OnBossCoreBroke;
            _comp.Bus.Subscribe(_onPartBroke);
            _comp.Bus.Subscribe(_onBossCoreBroke);

            _comp.Run.EnterBoss(BreakablePartCount); // STAGE → BOSS
            _fighting = true;
            Debug.Log("[BossController] Boss fight started — " + BreakablePartCount + " parts (" + _active.Kaiju.KaijuId + ").");
        }

        private void Update()
        {
            if (!_fighting || _comp == null) return;
            float dt = Time.deltaTime;
            _comp.Parts.Tick(dt); // drives heat fill + soften/stagger
            _idleT += dt;
            SyncPartVisuals();
            TickPartMotion();
            TickEmitters(dt);
            IdleMotion();
        }

        // Build one ActiveEmitter per authored PartEmitter across all parts (bullet emitters + noted spawners).
        private void BuildEmitters(Dictionary<string, PartDef> nameToDef)
        {
            foreach (var kv in _partDefById)
            {
                PartDef def = kv.Value;
                if (def == null || def.Emitters == null) continue;
                foreach (var e in def.Emitters)
                {
                    int gateId = -1;
                    if (e.Gate == PartFireGate.RequireGatePartBroken && !string.IsNullOrEmpty(e.GatePartId))
                        gateId = _comp.Parts.GetPartId(e.GatePartId);
                    _emitters.Add(new ActiveEmitter
                    {
                        OwnerPartId = kv.Key,
                        Pattern = e.Pattern,
                        Gate = e.Gate,
                        GatePartId = gateId,
                        IsSpawner = e.IsSpawner,
                        Cooldown = e.Pattern != null ? e.Pattern.FireIntervalSeconds : 0f,
                        SpinPhaseDeg = 0f
                    });
                }
            }
        }

        // Drive each part's authored PartMovement (orbit / sweep / spin) about its captured base local position.
        private void TickPartMotion()
        {
            foreach (var kv in _partDefById)
            {
                if (!_partsById.TryGetValue(kv.Key, out var bp) || bp == null) continue;
                var mv = kv.Value.Movement;
                if (mv.Type == PartMovementType.None) continue;
                Vector3 baseLocal = _partBaseLocal.TryGetValue(kv.Key, out var b) ? b : bp.transform.localPosition;
                bp.transform.localPosition = PartMotion.LocalPosition(mv, baseLocal, _idleT);
                float z = PartMotion.ZRotationDeg(mv, _idleT);
                if (z != 0f) bp.transform.localRotation = Quaternion.Euler(0f, 0f, z);
                _comp.Parts.SetWorldPosition(kv.Key, bp.transform.position);
            }
        }

        // Fire each live, gate-open bullet emitter from its owner part's current world position, aimed at the player.
        private void TickEmitters(float dt)
        {
            if (_bulletPool == null || _emitters.Count == 0) return;
            for (int i = 0; i < _emitters.Count; i++)
            {
                var e = _emitters[i];
                if (e.IsSpawner || e.Pattern == null) continue;              // minion spawners: follow-up
                if (_brokenPartIds.Contains(e.OwnerPartId)) continue;        // broken part = silenced
                if (!GateOpen(e)) continue;
                if (!_partsById.TryGetValue(e.OwnerPartId, out var owner) || owner == null || !owner.gameObject.activeSelf) continue;

                if (e.Pattern.PatternType == EmitterPatternType.Spiral)
                    e.SpinPhaseDeg += e.Pattern.SpinRateDegPerSec * dt;

                e.Cooldown -= dt;
                if (e.Cooldown <= 0f)
                {
                    FireVolley(e, owner.transform.position);
                    e.Cooldown = e.Pattern.FireIntervalSeconds;
                }
                _emitters[i] = e; // struct — write back the mutated cooldown/phase
            }
        }

        private bool GateOpen(ActiveEmitter e)
        {
            switch (e.Gate)
            {
                case PartFireGate.SilenceWhenSoftened:
                    return _comp.Parts.GetHeatState(e.OwnerPartId) != HeatState.Softened;
                case PartFireGate.RequireArmorStripped:
                    return _comp.Parts.GetArmorState(e.OwnerPartId) == ArmorState.Stripped;
                case PartFireGate.RequireGatePartBroken:
                    return e.GatePartId >= 0 && _brokenPartIds.Contains(e.GatePartId);
                default:
                    return true; // AliveOnly (owner-alive already checked by the caller)
            }
        }

        private void FireVolley(ActiveEmitter e, Vector3 muzzle)
        {
            int count = Mathf.Max(1, e.Pattern.BulletCountBase);
            float speed = e.Pattern.BulletSpeedPxPerSec * EnemyMovement.PxToWorld;
            Vector2 aim = _playerTarget != null ? (Vector2)(_playerTarget.position - muzzle) : Vector2.down;
            var vels = EnemyEmission.Velocities(e.Pattern.PatternType, count, e.Pattern.SpreadAngleDeg, speed, aim, e.SpinPhaseDeg);
            for (int i = 0; i < vels.Length; i++)
                _bulletPool.Spawn(muzzle, vels[i], _bulletDamage, e.Pattern.BulletLifetimeSeconds);
        }

        // Push each part's live armor/heat state into its BossPart so the art swaps (intact↔stripped) and the
        // softened tint follow the real system.
        private void SyncPartVisuals()
        {
            foreach (var kv in _partsById)
            {
                var part = kv.Value;
                if (part == null) continue;
                part.SetArmorStripped(_comp.Parts.GetArmorState(kv.Key) == ArmorState.Stripped);
                part.SetSoftened(_comp.Parts.GetHeatState(kv.Key) == HeatState.Softened);
            }
        }

        // Gentle breathing/drift so the kaiju reads as alive (the whole part hierarchy moves together).
        private void IdleMotion()
        {
            if (_active.BossRoot == null) return;
            // Data-driven body drift (KaijuDef.Body) when authored; otherwise the default gentle sway. (_idleT
            // is advanced once per frame in Update.)
            var body = _active.Kaiju != null ? _active.Kaiju.Body : default;
            float ax = body.DriftAmpX > 0f ? body.DriftAmpX : 0.18f;
            float ay = body.DriftAmpY > 0f ? body.DriftAmpY : 0.12f;
            float fx = body.DriftFreqX > 0f ? body.DriftFreqX : (0.6f / (2f * Mathf.PI));
            float fy = body.DriftFreqY > 0f ? body.DriftFreqY : (1.2f / (2f * Mathf.PI));
            var o = new Vector3(Mathf.Sin(_idleT * 2f * Mathf.PI * fx) * ax,
                                Mathf.Sin(_idleT * 2f * Mathf.PI * fy) * ay, 0f);
            _active.BossRoot.transform.position = _rootBase + o;
        }

        private void OnPartBroke(PartBroke evt)
        {
            _brokenPartIds.Add(evt.PartId); // silences this part's emitters + opens RequireGatePartBroken gates
            if (evt.Type == PartType.BossCore) return;
            if (_partsById.TryGetValue(evt.PartId, out var bp) && bp != null) bp.Hide();
        }

        private void OnBossCoreBroke(BossCoreBroke evt)
        {
            if (!_fighting) return;
            _fighting = false;
            Debug.Log("[BossController] BOSS CORE BROKEN — VICTORY. (RunController → RESULTS + HuntEnded.)");
            if (_active.BossRoot != null) _active.BossRoot.SetActive(false);
            Unsubscribe();
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_comp == null) return;
            if (_onPartBroke != null) _comp.Bus.Unsubscribe(_onPartBroke);
            if (_onBossCoreBroke != null) _comp.Bus.Unsubscribe(_onBossCoreBroke);
            _onPartBroke = null;
            _onBossCoreBroke = null;
        }
    }
}
