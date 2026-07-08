using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Firing-shape category for an <see cref="EmitterPatternSO"/>.
    /// Determines how bullet angles are distributed each volley.
    /// Aligned with stage-system.md §E.0 and bullet-system.md §4.2.
    /// </summary>
    public enum EmitterPatternType
    {
        /// <summary>Fixed-direction burst (wall / downward spray). FIXED_DIR aim mode.</summary>
        Linear,
        /// <summary>Even radial spread — ring or spiral arms. RADIAL aim mode.</summary>
        Radial,
        /// <summary>Fan centred on the player position at fire time. AIM_AT_PLAYER.</summary>
        Aimed,
        /// <summary>Simultaneous omnidirectional burst on death or trigger. RADIAL, dense count.</summary>
        RingBurst,
        /// <summary>Rotating radial arms — a ring whose emission angle sweeps over time. Reads <see cref="EmitterPatternSO.SpinRateDegPerSec"/>.</summary>
        Spiral
    }

    /// <summary>
    /// Authoring-layer ScriptableObject that describes a single bullet emitter pattern.
    /// This is a <b>pure static data container</b> — it holds no runtime logic and no DOTS types.
    /// The BulletSim back-end bakes this to a Burst-friendly Blob at load time once
    /// ADR-0001 is Accepted; until then the Stage system reads fields directly.
    /// Shared across base and elite variants of an enemy type; elite density scaling is
    /// applied at runtime using <see cref="EliteDensityMult"/>.
    /// See bullet-system.md §4, stage-system.md §E.0, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/EmitterPatternSO", fileName = "NewEmitterPattern")]
    public sealed class EmitterPatternSO : ScriptableObject
    {
        [Header("Pattern Shape")]
        [Tooltip("Firing-shape category. Aimed = fan toward player; Radial = ring / spiral; " +
                 "Linear = fixed-direction wall; RingBurst = death-trigger omnidirectional burst.")]
        [SerializeField] private EmitterPatternType _patternType = EmitterPatternType.Aimed;

        [Header("Bullet Parameters")]
        [Tooltip("Bullets fired per volley (base value before difficulty density scaling). " +
                 "At runtime: actual_count = ceil(BulletCountBase × bullet_density_mult[tier]). Range: [1, 20].")]
        [SerializeField] private int _bulletCountBase = 1;

        [Tooltip("Seconds between volleys. Difficulty may scale fire rate but never below telegraph_min_s. " +
                 "Must be > 0.")]
        [SerializeField] private float _fireIntervalSeconds = 3.0f;

        [Tooltip("Bullet travel speed in game-space pixels per second. " +
                 "NEVER scaled by difficulty — invariant per bullet-system.md §4.4. Must be > 0.")]
        [SerializeField] private float _bulletSpeedPxPerSec = 100f;

        [Tooltip("Total angular spread in degrees (fan half-width × 2 for Aimed; ring spacing reference for Radial). " +
                 "Difficulty-invariant. Range: [0, 360].")]
        [SerializeField] private float _spreadAngleDeg = 30f;

        [Tooltip("Seconds before a bullet auto-despawns after spawn. Must be > 0.")]
        [SerializeField] private float _bulletLifetimeSeconds = 5f;

        [Tooltip("Spiral only: angular speed (deg/s) the emission ring rotates each volley. " +
                 "Ignored by other pattern types. Positive = clockwise. Range: [-720, 720].")]
        [SerializeField] private float _spinRateDegPerSec = 90f;

        [Header("Elite Density Hook")]
        [Tooltip("Bullet-count / fire-rate multiplier applied when this emitter is owned by an elite enemy. " +
                 "The Stage system multiplies BulletCountBase by this value for elite variants. " +
                 "Default 1.0 (identity). Range: [1.0, 3.0].")]
        [SerializeField] private float _eliteDensityMult = 1.0f;

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>Firing-shape category (Aimed / Radial / Linear / RingBurst).</summary>
        public EmitterPatternType PatternType => _patternType;

        /// <summary>
        /// Base bullet count per volley.
        /// Runtime count = <c>ceil(BulletCountBase × bullet_density_mult[tier])</c>.
        /// </summary>
        public int BulletCountBase => _bulletCountBase;

        /// <summary>Seconds between volleys. Difficulty-scalable but floored by telegraph_min_s.</summary>
        public float FireIntervalSeconds => _fireIntervalSeconds;

        /// <summary>Bullet speed in px/s. Difficulty-invariant per bullet-system.md §4.4.</summary>
        public float BulletSpeedPxPerSec => _bulletSpeedPxPerSec;

        /// <summary>Total fan / spread angle in degrees. Difficulty-invariant.</summary>
        public float SpreadAngleDeg => _spreadAngleDeg;

        /// <summary>Bullet lifetime in seconds before auto-despawn.</summary>
        public float BulletLifetimeSeconds => _bulletLifetimeSeconds;

        /// <summary>Spiral-only rotation speed in deg/s of the emission ring (positive = clockwise). Ignored by other types.</summary>
        public float SpinRateDegPerSec => _spinRateDegPerSec;

        /// <summary>
        /// Elite density multiplier applied to bullet count when the owning enemy is an elite.
        /// Default 1.0 — no scaling for non-elite contexts.
        /// </summary>
        public float EliteDensityMult => _eliteDensityMult;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_bulletCountBase < 1 || _bulletCountBase > 20)
                Debug.LogError(
                    $"[EmitterPatternSO] '{name}': BulletCountBase must be in [1, 20]. " +
                    $"Current: {_bulletCountBase}.", this);

            if (_fireIntervalSeconds <= 0f)
                Debug.LogError(
                    $"[EmitterPatternSO] '{name}': FireIntervalSeconds must be > 0. " +
                    $"Current: {_fireIntervalSeconds}.", this);

            if (_spreadAngleDeg < 0f || _spreadAngleDeg > 360f)
                Debug.LogError(
                    $"[EmitterPatternSO] '{name}': SpreadAngleDeg must be in [0, 360]. " +
                    $"Current: {_spreadAngleDeg}.", this);

            if (_eliteDensityMult < 1.0f || _eliteDensityMult > 3.0f)
                Debug.LogError(
                    $"[EmitterPatternSO] '{name}': EliteDensityMult must be in [1.0, 3.0]. " +
                    $"Current: {_eliteDensityMult}.", this);

            if (_bulletLifetimeSeconds <= 0f)
                Debug.LogError(
                    $"[EmitterPatternSO] '{name}': BulletLifetimeSeconds must be > 0. " +
                    $"Current: {_bulletLifetimeSeconds}.", this);
        }
    }
}
