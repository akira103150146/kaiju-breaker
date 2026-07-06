using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Data definition for a single hand-crafted wave segment in a stage.
    /// Holds an enemy pool (set of <see cref="EnemyDef"/> assets eligible for this segment)
    /// and structural parameters consumed by the Stage system at run time.
    /// <para>
    /// Wave arrangement — specific spawn timings, positions, and per-wave counts —
    /// is resolved at run time by <c>KaijuBreaker.Stage.WaveBuilder</c> using the pool
    /// as a source of eligible enemy types. This SO describes <em>what can appear</em>,
    /// not the exact order or layout.
    /// </para>
    /// <para>
    /// <b>Pure static data container</b> — no runtime scheduling or spawning logic.
    /// </para>
    /// See stage-system.md §D.2, §D.3, §E.0, §K.2, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/SegmentDef", fileName = "NewSegmentDef")]
    public sealed class SegmentDef : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable string ID for this segment (e.g. 's1_02'). " +
                 "Matches stage-system.md §G wave-pool tables. Must not be empty.")]
        [SerializeField] private string _segmentId = string.Empty;

        [Tooltip("Human-readable development name (e.g. 'Turret Line / 砲台陣線'). " +
                 "Editor-only — not shown to players.")]
        [SerializeField] private string _segmentDisplayName = string.Empty;

        [Header("Enemy Pool")]
        [Tooltip("Set of EnemyDef assets eligible to appear in this segment. " +
                 "WaveBuilder samples this pool per wave according to WaveCount and EliteWaveIndex. " +
                 "Must not be empty.")]
        [SerializeField] private EnemyDef[] _enemyPool = System.Array.Empty<EnemyDef>();

        [Header("Wave Configuration")]
        [Tooltip("Total number of waves within this segment. Range: [1, 8].")]
        [SerializeField] private int _waveCount = 1;

        [Tooltip("Zero-based index of the wave that contains an elite mob and guarantees a Cycling Pod drop. " +
                 "-1 = no elite wave in this segment (e.g. difficulty-gated or tutorial segments).")]
        [SerializeField] private int _eliteWaveIndex = -1;

        [Header("Recombination Ordering")]
        [Tooltip("Escalation weight used to order drawn segments lightest-first within a run " +
                 "(stage-system.md §D.1 step 5). Range: [1, 5]; 1 = gentlest opener, 5 = heaviest pre-boss. " +
                 "Equal weights may sit adjacent (order among equals is otherwise unspecified).")]
        [SerializeField, Range(1, 5)] private int _difficultyWeight = 1;

        [Header("Difficulty Gate")]
        [Tooltip("Minimum difficulty tier required for this segment to enter the draw pool. " +
                 "Segments with min_difficulty_tier > current tier are excluded before shuffling " +
                 "per stage-system.md §D.1 step 2.")]
        [SerializeField] private DifficultyTier _minDifficultyTier = DifficultyTier.D1;

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>Stable segment identifier used by ContentRegistry and run history.</summary>
        public string SegmentId => _segmentId;

        /// <summary>Development-facing display name. Editor use only.</summary>
        public string SegmentDisplayName => _segmentDisplayName;

        /// <summary>
        /// Enemy types eligible to spawn in this segment.
        /// WaveBuilder selects from this pool — the pool is not a fixed spawn order.
        /// </summary>
        public EnemyDef[] EnemyPool => _enemyPool;

        /// <summary>Number of sequential waves in this segment. Range: [1, 8].</summary>
        public int WaveCount => _waveCount;

        /// <summary>
        /// Zero-based wave index containing an elite mob.
        /// The Stage system uses this to guarantee a Cycling Pod drop on that wave.
        /// -1 indicates no elite wave.
        /// </summary>
        public int EliteWaveIndex => _eliteWaveIndex;

        /// <summary>
        /// Escalation weight (1–5) used to order drawn segments lightest-first within a run
        /// (stage-system.md §D.1 step 5). Lower = earlier. Consumed by <c>SegmentRecombinator</c>.
        /// </summary>
        public int DifficultyWeight => _difficultyWeight;

        /// <summary>
        /// Minimum difficulty tier for this segment to be included in the run draw.
        /// Enforces difficulty-gated content per stage-system.md §D.3.
        /// </summary>
        public DifficultyTier MinDifficultyTier => _minDifficultyTier;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_segmentId))
                Debug.LogError(
                    $"[SegmentDef] '{name}': SegmentId must not be empty.", this);

            if (_waveCount < 1 || _waveCount > 8)
                Debug.LogError(
                    $"[SegmentDef] '{name}': WaveCount must be in [1, 8]. " +
                    $"Current: {_waveCount}.", this);

            if (_enemyPool == null || _enemyPool.Length == 0)
                Debug.LogError(
                    $"[SegmentDef] '{name}': EnemyPool must not be empty.", this);
        }
    }
}
