using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Movement-behaviour category for trash-enemy movement patterns.
    /// Each value describes the overall locomotion archetype; fine-grained
    /// parameters (speed, amplitude, frequency) live in <see cref="MovementPatternSO"/>.
    /// See stage-system.md §E.1 – §E.2 enemy descriptions.
    /// </summary>
    public enum MovementType
    {
        /// <summary>High-speed straight-line rush toward initial entry position (e.g. RamGrub 220 px/s).</summary>
        StraightRush,
        /// <summary>Slow horizontal drift into screen, optional gentle downward drift thereafter (e.g. TriShot).</summary>
        HorizontalDrift,
        /// <summary>Drifts to a target Y position then holds station (e.g. AimedGun hover at 30 % height).</summary>
        Hover,
        /// <summary>Slow descent that reverses into an upward U-turn arc (e.g. RingBurst, ShieldFlier).</summary>
        UTurn,
        /// <summary>
        /// Lateral sinusoidal oscillation while descending.
        /// <see cref="MovementPatternSO.AmplitudePx"/> and <see cref="MovementPatternSO.FrequencyHz"/> apply.
        /// </summary>
        Sinusoidal
    }

    /// <summary>
    /// Authoring-layer ScriptableObject describing how a trash-enemy moves.
    /// <b>Pure static data container</b> — no runtime AI or steering logic.
    /// Shared between a base enemy and its elite variant (elites reuse the same SO;
    /// only <see cref="EnemyDef"/> distinguishes them via stat overrides).
    /// See stage-system.md §E.0, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/MovementPatternSO", fileName = "NewMovementPattern")]
    public sealed class MovementPatternSO : ScriptableObject
    {
        [Header("Movement Type")]
        [Tooltip("Locomotion archetype. Selects which parameters the Stage movement system reads.")]
        [SerializeField] private MovementType _movementType = MovementType.StraightRush;

        [Header("Speed Parameters")]
        [Tooltip("Steady-state movement speed in game-space pixels per second. " +
                 "Difficulty-invariant per stage-system.md §I.2. Must be > 0.")]
        [SerializeField] private float _moveSpeedPxPerSec = 100f;

        [Tooltip("Speed multiplier applied only during the intro / entry phase. " +
                 "Example: ram_grub_intro_speed_mult = 0.7 on Stage 1 W1. " +
                 "1.0 = no special intro speed. Range: (0.0, 2.0].")]
        [SerializeField] private float _introSpeedMult = 1.0f;

        [Header("Sinusoidal Parameters")]
        [Tooltip("Half-amplitude of lateral sinusoidal oscillation in pixels. " +
                 "Only meaningful for the Sinusoidal type; should be 0 for StraightRush. " +
                 "Must be >= 0.")]
        [SerializeField] private float _amplitudePx = 0f;

        [Tooltip("Oscillation frequency in Hertz for the Sinusoidal movement type. " +
                 "Ignored by other types.")]
        [SerializeField] private float _frequencyHz = 1f;

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>Core locomotion archetype (StraightRush / HorizontalDrift / Hover / UTurn / Sinusoidal).</summary>
        public MovementType MovementType => _movementType;

        /// <summary>Steady-state speed in px/s. Difficulty-invariant per stage-system.md §I.2.</summary>
        public float MoveSpeedPxPerSec => _moveSpeedPxPerSec;

        /// <summary>
        /// Entry-phase speed multiplier.
        /// Applied by the Stage system only during the first approach of the enemy.
        /// 1.0 means no intro adjustment.
        /// </summary>
        public float IntroSpeedMult => _introSpeedMult;

        /// <summary>Half-amplitude of lateral sinusoidal oscillation in px. Zero for non-sinusoidal patterns.</summary>
        public float AmplitudePx => _amplitudePx;

        /// <summary>Oscillation frequency in Hz. Only relevant for Sinusoidal movement type.</summary>
        public float FrequencyHz => _frequencyHz;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_moveSpeedPxPerSec <= 0f)
                Debug.LogError(
                    $"[MovementPatternSO] '{name}': MoveSpeedPxPerSec must be > 0. " +
                    $"Current: {_moveSpeedPxPerSec}.", this);

            if (_introSpeedMult <= 0f || _introSpeedMult > 2.0f)
                Debug.LogError(
                    $"[MovementPatternSO] '{name}': IntroSpeedMult must be in (0.0, 2.0]. " +
                    $"Current: {_introSpeedMult}.", this);

            if (_amplitudePx < 0f)
                Debug.LogError(
                    $"[MovementPatternSO] '{name}': AmplitudePx must be >= 0. " +
                    $"Current: {_amplitudePx}.", this);
        }
    }
}
