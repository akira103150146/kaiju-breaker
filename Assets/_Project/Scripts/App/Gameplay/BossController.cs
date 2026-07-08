using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
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

        private GameComposition _comp;
        private BossEntry _active;
        private readonly Dictionary<int, BossPart> _partsById = new Dictionary<int, BossPart>();
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
        public void BeginBossFight(GameComposition comp, int bossIndex)
        {
            if (_fighting) return;
            _comp = comp;
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
            var parts = _active.BossRoot != null ? _active.BossRoot.GetComponentsInChildren<BossPart>(true) : Array.Empty<BossPart>();
            foreach (var bp in parts)
            {
                int id = _comp.Parts.GetPartId(bp.PartName);
                if (id < 0) { Debug.LogWarning("[BossController] BossPart '" + bp.PartName + "' has no matching PartDef."); continue; }
                bp.Configure(id, _active.KaijuId, _comp.Bus);
                _comp.Parts.SetWorldPosition(id, bp.transform.position);
                _partsById[id] = bp;
            }

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
            _comp.Parts.Tick(Time.deltaTime); // drives heat fill + soften/stagger
            SyncPartVisuals();
            IdleMotion();
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
            _idleT += Time.deltaTime;
            var o = new Vector3(Mathf.Sin(_idleT * 0.6f) * 0.18f, Mathf.Sin(_idleT * 1.2f) * 0.12f, 0f);
            _active.BossRoot.transform.position = _rootBase + o;
        }

        private void OnPartBroke(PartBroke evt)
        {
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
