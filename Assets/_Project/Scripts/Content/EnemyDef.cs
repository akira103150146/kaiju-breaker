using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Hit-point tier for trash enemies. Defines rough time-to-kill bands.
    /// T1 = destroyed by 2–3 L1 Spread Laser hits (light group type).
    /// T2 = destroyed by 4–6 L1 hits (medium priority target).
    /// Elite HP = base tier HP × <see cref="EnemyDef.EliteHpMult"/>.
    /// See stage-system.md §E.1.
    /// </summary>
    public enum HpTier
    {
        T1,
        T2
    }

    /// <summary>
    /// Data definition for a single trash-enemy type or its elite variant.
    /// Bundles base stats with typed references to shared
    /// <see cref="MovementPatternSO"/> and <see cref="EmitterPatternSO"/> assets.
    /// <para>
    /// Elite variants share the same pattern SOs as their base type
    /// (identical movement and emitter shape) but set <see cref="IsElite"/> true
    /// and override HP / density / shard multipliers in this asset.
    /// No duplicate SO assets are needed per stage-system.md §E.3.
    /// </para>
    /// <para>
    /// <b>Pure static data container</b> — all runtime behaviour (spawning,
    /// movement execution, bullet emission) lives in <c>KaijuBreaker.Stage</c>.
    /// </para>
    /// See stage-system.md §E.0, §E.3, §K.4, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/EnemyDef", fileName = "NewEnemyDef")]
    public sealed class EnemyDef : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable string ID matching stage-system.md §E.1 roster (e.g. 'ram_grub', 'tri_shot_elite'). " +
                 "Used by WaveBuilder and ContentRegistry for look-up. Must not be empty.")]
        [SerializeField] private string _enemyId = string.Empty;

        [Header("Base Stats")]
        [Tooltip("HP tier. T1 = light (2–3 L1 hits); T2 = medium (4–6 L1 hits). " +
                 "Actual HP value resolved at runtime from this tier. See stage-system.md §E.1.")]
        [SerializeField] private HpTier _hpTier = HpTier.T1;

        [Tooltip("Damage dealt to the player on contact collision. Must be > 0.")]
        [SerializeField] private float _contactDamage = 10f;

        [Tooltip("Score points awarded to the player on kill.")]
        [SerializeField] private int _pointValue = 10;

        [Header("Elite Override")]
        [Tooltip("True when this asset represents an elite variant. " +
                 "Enables EliteHpMult, EliteDensityMult, EliteShardBonus, and EliteAuraColor. " +
                 "Elite enemies drop a Cycling Weapon Pod on death.")]
        [SerializeField] private bool _isElite = false;

        [Tooltip("HP multiplier over the base HpTier value applied when IsElite is true. " +
                 "stage-system.md §E.3 default: 2.5. Must be >= 1.0 when IsElite is true.")]
        [SerializeField] private float _eliteHpMult = 1.0f;

        [Tooltip("Bullet-density / fire-rate multiplier applied to the shared EmitterPatternSO " +
                 "when IsElite is true. stage-system.md §E.3 default: 1.5.")]
        [SerializeField] private float _eliteDensityMult = 1.0f;

        [Tooltip("Extra Common Shards awarded on elite kill (in addition to normal shard yield). " +
                 "stage-system.md §E.3 default: +3.")]
        [SerializeField] private int _eliteShardBonus = 0;

        [Tooltip("Sprite aura colour for visual elite identification. " +
                 "Rendered as a pixel glow ring around the sprite. " +
                 "stage-system.md §E.3 default: #FFAA33 (warm amber).")]
        [SerializeField] private Color _eliteAuraColor = new Color(1.0f, 0.6667f, 0.2f, 1.0f); // #FFAA33

        [Header("Pattern References")]
        [Tooltip("Movement behaviour for this enemy. " +
                 "Required — must not be null. Shared with elite variant.")]
        [SerializeField] private MovementPatternSO _movementPattern;

        [Tooltip("Bullet emitter behaviour for this enemy. " +
                 "MAY be null for contact-only enemies that do not shoot (e.g. RamGrub). " +
                 "OnValidate does not error on null here — Stage system handles null safely.")]
        [SerializeField] private EmitterPatternSO _emitterPattern;

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>Stable string identifier (e.g. "ram_grub"). Used for wave and registry look-up.</summary>
        public string EnemyId => _enemyId;

        /// <summary>HP tier. T1 = light; T2 = medium. Actual HP determined at runtime.</summary>
        public HpTier HpTier => _hpTier;

        /// <summary>Contact damage dealt on player collision.</summary>
        public float ContactDamage => _contactDamage;

        /// <summary>Score points awarded on kill.</summary>
        public int PointValue => _pointValue;

        /// <summary>
        /// True when this EnemyDef represents an elite variant.
        /// Elites drop a Cycling Pod on death per stage-system.md §E.3.
        /// </summary>
        public bool IsElite => _isElite;

        /// <summary>HP multiplier over the tier base when IsElite is true. Default 2.5.</summary>
        public float EliteHpMult => _eliteHpMult;

        /// <summary>Emitter density multiplier for elite bullet density. Default 1.5.</summary>
        public float EliteDensityMult => _eliteDensityMult;

        /// <summary>Extra Common Shards dropped on elite kill.</summary>
        public int EliteShardBonus => _eliteShardBonus;

        /// <summary>Warm amber aura colour (#FFAA33) for visual elite recognition.</summary>
        public Color EliteAuraColor => _eliteAuraColor;

        /// <summary>
        /// Shared movement behaviour SO. Must not be null.
        /// Elites share this reference with their base variant (no duplicate SOs needed).
        /// </summary>
        public MovementPatternSO MovementPattern => _movementPattern;

        /// <summary>
        /// Bullet emitter behaviour SO. <b>Nullable</b> — null is valid for contact-only
        /// enemies such as RamGrub that have no shooting behaviour.
        /// The Stage system checks for null before activating emitter logic.
        /// </summary>
        public EmitterPatternSO EmitterPattern => _emitterPattern;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_enemyId))
                Debug.LogError(
                    $"[EnemyDef] '{name}': EnemyId must not be empty.", this);

            if (_contactDamage <= 0f)
                Debug.LogError(
                    $"[EnemyDef] '{name}': ContactDamage must be > 0. " +
                    $"Current: {_contactDamage}.", this);

            if (_isElite && _eliteHpMult < 1.0f)
                Debug.LogError(
                    $"[EnemyDef] '{name}': EliteHpMult must be >= 1.0 when IsElite is true. " +
                    $"Current: {_eliteHpMult}.", this);

            if (_movementPattern == null)
                Debug.LogError(
                    $"[EnemyDef] '{name}': MovementPattern must not be null.", this);

            // EmitterPattern null is intentional for contact-only enemies (e.g. RamGrub) — no error.
        }
    }
}
