using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Wave pacing + layout tuning for the pool-model wave planner (stage-system.md §D.2; ADR-0003). The
    /// committed <see cref="SegmentDef"/> describes WHAT can appear (enemy pool + wave count + elite index);
    /// this SO supplies the pacing/geometry knobs the planner needs to turn that into concrete spawns —
    /// keeping all tuning out of code. Balance values here are placeholder-tunable (not final).
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/WaveTimingConfig", fileName = "WaveTimingConfig")]
    public sealed class WaveTimingConfig : ScriptableObject
    {
        [Header("Pacing")]
        [Tooltip("Base enemies per wave BEFORE the difficulty enemy-count multiplier. Range [1, 20].")]
        [SerializeField, Range(1, 20)] private int _enemiesPerWaveBase = 4;

        [Tooltip("Legacy fixed wave cadence (seconds). Retained for reference; the runtime now uses clear-gated " +
                 "pacing below so a new wave never stacks on an unkilled one. Must be > 0.")]
        [SerializeField] private float _waveIntervalSeconds = 6f;

        [Header("Clear-gated pacing (no wave stacks on an unkilled one)")]
        [Tooltip("Release the next wave once the alive-enemy count drops to/below this (field mostly clear). " +
                 "Higher = waves overlap more; 0 = wait until fully clear. Range [0, 12].")]
        [SerializeField, Range(0, 12)] private int _nextWaveAliveThreshold = 2;

        [Tooltip("Anti-stall cap: release the next wave after this many seconds even if the field isn't clear " +
                 "(e.g. a straggler stuck at an edge). Must be > 0.")]
        [SerializeField] private float _maxWaveWaitSeconds = 8f;

        [Tooltip("Minimum seconds between consecutive wave starts — a floor so waves never machine-gun out even " +
                 "when the field clears instantly. Must be > 0.")]
        [SerializeField] private float _minWaveGapSeconds = 2.2f;

        [Tooltip("Seconds of stagger between enemies WITHIN a single wave so they don't all pop in on one frame.")]
        [SerializeField] private float _intraWaveStaggerSeconds = 0.14f;

        [Header("Layout")]
        [Tooltip("Default formation for a wave's enemies.")]
        [SerializeField] private SpawnLayout _defaultLayout = SpawnLayout.HorizontalSpread;

        [Tooltip("Playfield width the layout spreads enemies across (world units).")]
        [SerializeField] private float _fieldWidth = 8f;

        [Tooltip("World-space Y the wave spawns at (top of the screen; enemies descend from here).")]
        [SerializeField] private float _spawnY = 6f;

        [Tooltip("Vertical spacing between enemies in a Column layout (world units). Must be > 0.")]
        [SerializeField] private float _columnSpacing = 1.2f;

        /// <summary>Base enemies per wave before the difficulty multiplier.</summary>
        public int EnemiesPerWaveBase => _enemiesPerWaveBase;

        /// <summary>Legacy fixed cadence (seconds). The runtime uses the clear-gated knobs below instead.</summary>
        public float WaveIntervalSeconds => _waveIntervalSeconds;

        /// <summary>Alive-enemy count at/below which the next wave releases early (field mostly clear).</summary>
        public int NextWaveAliveThreshold => _nextWaveAliveThreshold;

        /// <summary>Anti-stall cap: release the next wave after this long even if not clear.</summary>
        public float MaxWaveWaitSeconds => _maxWaveWaitSeconds;

        /// <summary>Minimum seconds between consecutive wave starts (a floor).</summary>
        public float MinWaveGapSeconds => _minWaveGapSeconds;

        /// <summary>Seconds of stagger between enemies within one wave.</summary>
        public float IntraWaveStaggerSeconds => _intraWaveStaggerSeconds;

        /// <summary>Default formation for a wave.</summary>
        public SpawnLayout DefaultLayout => _defaultLayout;

        /// <summary>Playfield width the layout spreads across.</summary>
        public float FieldWidth => _fieldWidth;

        /// <summary>World Y the wave spawns at.</summary>
        public float SpawnY => _spawnY;

        /// <summary>Vertical spacing for Column layout.</summary>
        public float ColumnSpacing => _columnSpacing;

        private void OnValidate()
        {
            if (_waveIntervalSeconds <= 0f)
                Debug.LogError($"[WaveTimingConfig] '{name}': WaveIntervalSeconds must be > 0. Current: {_waveIntervalSeconds}.", this);
            if (_columnSpacing <= 0f)
                Debug.LogError($"[WaveTimingConfig] '{name}': ColumnSpacing must be > 0. Current: {_columnSpacing}.", this);
            if (_maxWaveWaitSeconds <= 0f)
                Debug.LogError($"[WaveTimingConfig] '{name}': MaxWaveWaitSeconds must be > 0. Current: {_maxWaveWaitSeconds}.", this);
            if (_minWaveGapSeconds <= 0f)
                Debug.LogError($"[WaveTimingConfig] '{name}': MinWaveGapSeconds must be > 0. Current: {_minWaveGapSeconds}.", this);
        }
    }
}
