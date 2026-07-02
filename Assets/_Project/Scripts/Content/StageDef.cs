using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Data definition for a complete stage: segment pool, recombination rules,
    /// weapon pod pools, and boss reference.
    /// <para>
    /// Describes the static configuration consumed by <c>KaijuBreaker.Stage</c> at run start
    /// to build the per-run segment sequence via the randomised draw algorithm
    /// (stage-system.md §D.1).
    /// </para>
    /// <para>
    /// <b>TR-content-004 enforcement</b>: this SO deliberately contains NO difficulty
    /// multiplier fields (<c>EnemyCountMult</c>, <c>BulletDensityMult</c> etc.).
    /// Those are the sole property of <c>DifficultyConfig</c> (Story 003).
    /// </para>
    /// <para>
    /// <b>Pure static data container</b> — all sequencing logic lives in
    /// <c>KaijuBreaker.Stage</c>.
    /// </para>
    /// <para>
    /// <c>WeaponDef</c> is authored by Story 001 in the same assembly
    /// (<c>KaijuBreaker.Content</c>) and will resolve at import.
    /// </para>
    /// See stage-system.md §C, §D, §F.4, §K.2, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/StageDef", fileName = "NewStageDef")]
    public sealed class StageDef : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable string ID for this stage (e.g. 'stage_01'). Must not be empty.")]
        [SerializeField] private string _stageId = string.Empty;

        [Tooltip("String ID of the boss kaiju for this stage (e.g. 'carapex'). " +
                 "Resolved at run time via ContentRegistry. " +
                 "Stage 1 value: 'carapex' per stage-system.md §G.1.")]
        [SerializeField] private string _bossKaijuId = string.Empty;

        [Header("Segment Recombination (K.2)")]
        [Tooltip("Number of SegmentDef assets drawn from the pool each run. " +
                 "Range: [1, 10]. Must be <= SegmentPool.Length. " +
                 "Stage 1 default: 3 (draws 3 from pool of 5).")]
        [SerializeField] private int _segmentDrawCount = 3;

        [Tooltip("No-repeat window: segments played in the last N runs are excluded from this run's draw pool. " +
                 "Prevents back-to-back repetition across runs. Stage default: 1.")]
        [SerializeField] private int _noRepeatWindow = 1;

        [Tooltip("Duration in seconds of the fixed pre-boss lull phase (no enemies; Cycling Pod spawned). " +
                 "stage-system.md §K.2 pre_boss_lull_duration default: 20 s. Must be > 0.")]
        [SerializeField] private float _preBossLullDurationSeconds = 20f;

        [Header("Segment Pool")]
        [Tooltip("All hand-crafted SegmentDef assets available for randomised draw this stage. " +
                 "Length must be >= SegmentDrawCount. " +
                 "Stage 1 MVP pool: 5 segments (S1-01 through S1-05).")]
        [SerializeField] private SegmentDef[] _segmentPool = System.Array.Empty<SegmentDef>();

        [Header("Weapon Pod Pools (F.4)")]
        [Tooltip("Primary weapon (laser family) pool for Cycling Pod contents this stage. " +
                 "Stage 1: { L1, L2 } per stage-system.md §F.4. " +
                 "WeaponDef is authored by Story 001 in the KaijuBreaker.Content assembly.")]
        [SerializeField] private WeaponDef[] _primaryWeaponPool = System.Array.Empty<WeaponDef>();

        [Tooltip("Secondary weapon (missile family) pool for Cycling Pod contents this stage. " +
                 "Stage 1: { M1, M3 } per stage-system.md §F.4.")]
        [SerializeField] private WeaponDef[] _secondaryWeaponPool = System.Array.Empty<WeaponDef>();

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>Stable stage identifier (e.g. "stage_01").</summary>
        public string StageId => _stageId;

        /// <summary>
        /// String ID of the boss kaiju for this stage.
        /// Resolved at run time via ContentRegistry.
        /// Stage 1 value: "carapex".
        /// </summary>
        public string BossKaijuId => _bossKaijuId;

        /// <summary>Segments drawn per run from <see cref="SegmentPool"/>. Range: [1, 10].</summary>
        public int SegmentDrawCount => _segmentDrawCount;

        /// <summary>
        /// Cross-run no-repeat window: last N run's final segment(s) are excluded from
        /// the current draw before shuffling.
        /// </summary>
        public int NoRepeatWindow => _noRepeatWindow;

        /// <summary>Duration in seconds of the fixed pre-boss lull phase.</summary>
        public float PreBossLullDurationSeconds => _preBossLullDurationSeconds;

        /// <summary>
        /// Hand-crafted segment pool for randomised per-run draw (stage-system.md §D.1).
        /// Must contain at least <see cref="SegmentDrawCount"/> entries.
        /// </summary>
        public SegmentDef[] SegmentPool => _segmentPool;

        /// <summary>Primary (laser) weapon pool for Cycling Pod display. See stage-system.md §F.4.</summary>
        public WeaponDef[] PrimaryWeaponPool => _primaryWeaponPool;

        /// <summary>Secondary (missile) weapon pool for Cycling Pod display.</summary>
        public WeaponDef[] SecondaryWeaponPool => _secondaryWeaponPool;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_stageId))
                Debug.LogError(
                    $"[StageDef] '{name}': StageId must not be empty.", this);

            if (_segmentDrawCount < 1 || _segmentDrawCount > 10)
                Debug.LogError(
                    $"[StageDef] '{name}': SegmentDrawCount must be in [1, 10]. " +
                    $"Current: {_segmentDrawCount}.", this);

            if (_preBossLullDurationSeconds <= 0f)
                Debug.LogError(
                    $"[StageDef] '{name}': PreBossLullDurationSeconds must be > 0. " +
                    $"Current: {_preBossLullDurationSeconds}.", this);

            if (_segmentPool != null && _segmentPool.Length < _segmentDrawCount)
                Debug.LogError(
                    $"[StageDef] '{name}': SegmentPool length ({_segmentPool.Length}) " +
                    $"must be >= SegmentDrawCount ({_segmentDrawCount}). " +
                    $"Cannot draw {_segmentDrawCount} segments from a pool of {_segmentPool.Length}.", this);
        }
    }
}
