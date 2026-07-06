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

        [Tooltip("Seconds between the start of consecutive waves within a segment. Must be > 0.")]
        [SerializeField] private float _waveIntervalSeconds = 6f;

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

        /// <summary>Seconds between consecutive waves in a segment.</summary>
        public float WaveIntervalSeconds => _waveIntervalSeconds;

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
        }
    }
}
