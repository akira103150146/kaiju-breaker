using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Owns the boss encounter on the scene side: on <see cref="BeginBossFight"/> it initialises the real
    /// <see cref="KaijuBreaker.KaijuParts.PartStateSystem"/> from the boss <see cref="KaijuDef"/>, binds each
    /// scene <see cref="BossPart"/> to its runtime id + world position, transitions the run into BOSS, then
    /// ticks the part system each frame so heat fills and softening/stagger resolve. It reacts to the system's
    /// events — hiding a part on <see cref="PartBroke"/>, ending the fight on <see cref="BossCoreBroke"/> (the
    /// win, which <see cref="Stage.RunController"/> already turns into RESULTS + <see cref="HuntEnded"/>).
    /// Pure orchestration: every combat rule lives in the injected systems (ADR-0005).
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class BossController : MonoBehaviour
    {
        [Tooltip("Boss definition (parts + adjacency + theme). Required.")]
        [SerializeField] private KaijuDef _kaiju;

        [Tooltip("Runtime kaiju id used for the part system + theme lookup.")]
        [SerializeField] private int _kaijuId = 1;

        [Tooltip("Root of the boss part hierarchy (holds the BossPart children). Hidden until the fight starts.")]
        [SerializeField] private GameObject _bossRoot;

        private GameComposition _comp;
        private readonly Dictionary<int, BossPart> _partsById = new Dictionary<int, BossPart>();
        private Action<PartBroke> _onPartBroke;
        private Action<BossCoreBroke> _onBossCoreBroke;
        private bool _fighting;

        /// <summary>True while the boss fight is live (parts ticking).</summary>
        public bool Fighting => _fighting;

        /// <summary>The boss's total breakable part count (incl. core), for the run's full-clear check.</summary>
        public int BreakablePartCount => _kaiju != null && _kaiju.Parts != null ? _kaiju.Parts.Length : 0;

        private void Awake()
        {
            if (_bossRoot != null) _bossRoot.SetActive(false); // hidden through the 道中
        }

        /// <summary>
        /// Start the boss fight: initialise the part system, bind scene parts, enter BOSS, begin ticking.
        /// Called by <see cref="GameplaySceneDirector"/> when the wave sequence clears.
        /// </summary>
        public void BeginBossFight(GameComposition comp)
        {
            if (_fighting) return;
            _comp = comp;
            if (_comp == null || _kaiju == null) { Debug.LogError("[BossController] Missing composition or KaijuDef."); return; }

            _comp.Parts.InitializeParts(_kaiju, _kaijuId);
            _comp.Themes.Register(_kaijuId, _kaiju.Theme); // so Economy can source the theme core on break

            if (_bossRoot != null) _bossRoot.SetActive(true);

            _partsById.Clear();
            var parts = _bossRoot != null ? _bossRoot.GetComponentsInChildren<BossPart>(true) : Array.Empty<BossPart>();
            foreach (var bp in parts)
            {
                int id = _comp.Parts.GetPartId(bp.PartName);
                if (id < 0) { Debug.LogWarning($"[BossController] BossPart '{bp.PartName}' has no matching PartDef."); continue; }
                bp.Configure(id, _kaijuId, _comp.Bus);
                _comp.Parts.SetWorldPosition(id, bp.transform.position);
                _partsById[id] = bp;
            }

            _onPartBroke = OnPartBroke;
            _onBossCoreBroke = OnBossCoreBroke;
            _comp.Bus.Subscribe(_onPartBroke);
            _comp.Bus.Subscribe(_onBossCoreBroke);

            _comp.Run.EnterBoss(BreakablePartCount); // STAGE → BOSS (publishes RunStateChanged)
            _fighting = true;
            Debug.Log($"[BossController] Boss fight started — {BreakablePartCount} breakable parts ({_kaiju.KaijuId}).");
        }

        private void Update()
        {
            if (_fighting && _comp != null) _comp.Parts.Tick(Time.deltaTime); // drives heat fill + soften/stagger
        }

        private void OnPartBroke(PartBroke evt)
        {
            if (evt.Type == PartType.BossCore) return; // the core hides via the win path
            if (_partsById.TryGetValue(evt.PartId, out var bp) && bp != null) bp.Hide();
        }

        private void OnBossCoreBroke(BossCoreBroke evt)
        {
            if (!_fighting) return;
            _fighting = false;
            Debug.Log("[BossController] BOSS CORE BROKEN — VICTORY. (RunController → RESULTS + HuntEnded.)");
            // Hide remaining boss visuals; RunController already published HuntEnded for settlement.
            if (_bossRoot != null) _bossRoot.SetActive(false);
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
