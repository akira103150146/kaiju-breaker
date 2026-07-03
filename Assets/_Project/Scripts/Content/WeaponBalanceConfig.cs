using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Global weapon balance knobs shared by the Weapons and KaijuParts systems.
    /// Single source of truth for D₀, heat track, break track, and stagger parameters.
    /// StaggerDuration is the single owner for both weapon-system.md G.2 l3_stagger_window
    /// and kaiju-part-system.md G.1 stagger_duration — those two GDD knobs map here.
    /// See weapon-system.md G.1 and ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/WeaponBalanceConfig", fileName = "WeaponBalanceConfig")]
    public sealed class WeaponBalanceConfig : ScriptableObject
    {
        [Header("Power Budget")]
        [Tooltip("Global power budget reference (PU/s). All weapons must sustain D0 ±10%. weapon-system.md G.1.")]
        [SerializeField] private float _d0Reference = 100f;

        [Tooltip("Break-track conversion: BU/s produced by 1 D0 of sustained output. weapon-system.md G.1 " +
                 "footnote (1 D0 ≙ 10 BU/s). Consumed by Weapons to derive break_delta_base and by the " +
                 "D0 balance suite. Safe range [5, 20].")]
        [SerializeField] private float _buPerD0 = 10f;

        [Tooltip("Heat-track conversion: HU/s produced by 1 D0 of sustained laser output (25 HU/s ≙ 1 D0). " +
                 "INFERRED from G.2 laser defaults (L1 full=25, L2=37.5=1.5×, L3 tap=15=0.6×) — pending design " +
                 "sign-off; the D0 balance suite's H.1 laser assertions depend on it. Safe range [15, 40].")]
        [SerializeField] private float _huPerD0 = 25f;

        [Header("Heat Track — Capacities (HU)")]
        [Tooltip("Heat capacity for Normal parts (HU). Safe range [80, 150].")]
        [SerializeField] private float _hMaxNormal = 100f;

        [Tooltip("Heat capacity for Armored parts (HU). Safe range [120, 200].")]
        [SerializeField] private float _hMaxArmored = 150f;

        [Tooltip("Heat capacity for Boss Core parts (HU). Safe range [160, 280].")]
        [SerializeField] private float _hMaxBossCore = 200f;

        [Header("Heat Track — Decay & Thresholds")]
        [Tooltip("Heat decay rate when no laser is landing (HU/s). Safe range [1, 8].")]
        [SerializeField] private float _hDecayRate = 3f;

        [Tooltip("Heat threshold to ENTER SOFTENED state (HU). Safe range [80, 120]. Recommended = HMaxNormal.")]
        [SerializeField] private float _thetaS = 100f;

        [Tooltip("Heat threshold to EXIT SOFTENED state (HU). Safe range [60, 90]. Must be < ThetaS.")]
        [SerializeField] private float _thetaSExit = 80f;

        [Header("Break Track — Capacities (BU)")]
        [Tooltip("Armor-break capacity for Normal parts (BU). Safe range [80, 150].")]
        [SerializeField] private float _bMaxNormal = 100f;

        [Tooltip("Armor-break capacity for Armored parts (BU). Safe range [120, 200].")]
        [SerializeField] private float _bMaxArmored = 150f;

        [Tooltip("Armor-break capacity for Boss Core parts (BU). Safe range [160, 280].")]
        [SerializeField] private float _bMaxBossCore = 200f;

        [Header("Break Track — Multipliers")]
        [Tooltip("Missile break-fill multiplier when part is NOT softened. Safe range [0.20, 0.50]. " +
                 "Gate: enforces the soften-first path. Below 0.20 hurts feel; above 0.50 weakens dual-track.")]
        [SerializeField] private float _bUnsoftenedMult = 0.35f;

        [Header("Break Track — Destruction Thresholds (BU)")]
        [Tooltip("BU required to break a Normal part. Safe range [80, 150]. Default = BMaxNormal.")]
        [SerializeField] private float _requiredBreakThresholdNormal = 100f;

        [Tooltip("BU required to break an Armored part. Safe range [120, 200]. Default = BMaxArmored.")]
        [SerializeField] private float _requiredBreakThresholdArmored = 150f;

        [Tooltip("BU required to break a Boss Core part. Safe range [160, 280]. Default = BMaxBossCore.")]
        [SerializeField] private float _requiredBreakThresholdBossCore = 200f;

        [Header("Default Loadout (first-playthrough fallback)")]
        [Tooltip("Primary-pool weapon a fresh save starts with when ISaveService has no stored loadout. " +
                 "MUST be a laser (L1–L4). weapon-system.md F.3 — data-driven default, not hardcoded in Weapons.")]
        [SerializeField] private WeaponId _defaultPrimary = WeaponId.L1;

        [Tooltip("Secondary-pool weapon a fresh save starts with when ISaveService has no stored loadout. " +
                 "MUST be a missile (M1–M4). weapon-system.md F.3.")]
        [SerializeField] private WeaponId _defaultSecondary = WeaponId.M1;

        [Header("Stagger (L3 Wave Cannon)")]
        [Tooltip("Duration of the STAGGERED overlay (s). Safe range [1.5, 3.0]. " +
                 "SINGLE SOURCE for weapon-system.md G.2 l3_stagger_window " +
                 "AND kaiju-part-system.md G.1 stagger_duration.")]
        [SerializeField] private float _staggerDuration = 2.0f;

        [Tooltip("Break-fill multiplier while a part is STAGGERED. Safe range [1.2, 2.0].")]
        [SerializeField] private float _staggerBreakMult = 1.5f;

        [Header("Equal-Power Verification (feedback point 3)")]
        [Tooltip("± band the H.1/H.2 equal-power tests allow around D₀ (0.10 = ±10%). Externalised per " +
                 "the no-hardcoded-balance rule so the gate itself is tunable. Safe range [0.05, 0.20]. " +
                 "weapon-tiering-and-equal-power.md.")]
        [SerializeField] private float _equalPowerBandTolerance = 0.10f;

        // ── Public read-only properties ──────────────────────────────────────────

        /// <summary>Global D₀ power budget reference (PU/s). weapon-system.md G.1.</summary>
        public float D0Reference => _d0Reference;

        /// <summary>BU/s produced by 1 D₀ of sustained output (1 D₀ ≙ 10 BU/s). weapon-system.md G.1.</summary>
        public float BuPerD0 => _buPerD0;

        /// <summary>HU/s produced by 1 D₀ of sustained laser output (25 HU/s ≙ 1 D₀, inferred). weapon-system.md G.2.</summary>
        public float HuPerD0 => _huPerD0;

        /// <summary>Heat capacity for Normal parts (HU). weapon-system.md G.1.</summary>
        public float HMaxNormal => _hMaxNormal;

        /// <summary>Heat capacity for Armored parts (HU). weapon-system.md G.1.</summary>
        public float HMaxArmored => _hMaxArmored;

        /// <summary>Heat capacity for Boss Core parts (HU). weapon-system.md G.1.</summary>
        public float HMaxBossCore => _hMaxBossCore;

        /// <summary>Heat decay rate when no laser lands (HU/s). weapon-system.md G.1.</summary>
        public float HDecayRate => _hDecayRate;

        /// <summary>SOFTENED entry threshold (HU). weapon-system.md G.1.</summary>
        public float ThetaS => _thetaS;

        /// <summary>SOFTENED exit threshold (HU). weapon-system.md G.1.</summary>
        public float ThetaSExit => _thetaSExit;

        /// <summary>Armor-break capacity for Normal parts (BU). weapon-system.md G.1.</summary>
        public float BMaxNormal => _bMaxNormal;

        /// <summary>Armor-break capacity for Armored parts (BU). weapon-system.md G.1.</summary>
        public float BMaxArmored => _bMaxArmored;

        /// <summary>Armor-break capacity for Boss Core parts (BU). weapon-system.md G.1.</summary>
        public float BMaxBossCore => _bMaxBossCore;

        /// <summary>Missile break-fill multiplier when part is unsoftened. weapon-system.md G.1.</summary>
        public float BUnsoftenedMult => _bUnsoftenedMult;

        /// <summary>Minimum BU to break a Normal part. weapon-system.md G.1.</summary>
        public float RequiredBreakThresholdNormal => _requiredBreakThresholdNormal;

        /// <summary>Minimum BU to break an Armored part. weapon-system.md G.1.</summary>
        public float RequiredBreakThresholdArmored => _requiredBreakThresholdArmored;

        /// <summary>Minimum BU to break a Boss Core part. weapon-system.md G.1.</summary>
        public float RequiredBreakThresholdBossCore => _requiredBreakThresholdBossCore;

        /// <summary>
        /// STAGGERED overlay duration (s). Single source for weapon-system.md G.2
        /// l3_stagger_window and kaiju-part-system.md G.1 stagger_duration.
        /// </summary>
        public float StaggerDuration => _staggerDuration;

        /// <summary>Break-fill multiplier while STAGGERED. weapon-system.md G.1.</summary>
        public float StaggerBreakMult => _staggerBreakMult;

        /// <summary>± tolerance band for the equal-power (H.1/H.2) tests (0.10 = ±10%). weapon-tiering-and-equal-power.md.</summary>
        public float EqualPowerBandTolerance => _equalPowerBandTolerance;

        /// <summary>Fresh-save default primary (laser) weapon. weapon-system.md F.3.</summary>
        public WeaponId DefaultPrimary => _defaultPrimary;

        /// <summary>Fresh-save default secondary (missile) weapon. weapon-system.md F.3.</summary>
        public WeaponId DefaultSecondary => _defaultSecondary;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Validate("D0Reference",                      _d0Reference,                      1f,    float.MaxValue);
            Validate("BuPerD0",                          _buPerD0,                          5f,    20f);
            Validate("HuPerD0",                          _huPerD0,                          15f,   40f);
            Validate("HMaxNormal",                       _hMaxNormal,                       80f,   150f);
            Validate("HMaxArmored",                      _hMaxArmored,                      120f,  200f);
            Validate("HMaxBossCore",                     _hMaxBossCore,                     160f,  280f);
            Validate("HDecayRate",                       _hDecayRate,                       1f,    8f);
            Validate("ThetaS",                           _thetaS,                           80f,   120f);
            Validate("ThetaSExit",                       _thetaSExit,                       60f,   90f);
            Validate("BMaxNormal",                       _bMaxNormal,                       80f,   150f);
            Validate("BMaxArmored",                      _bMaxArmored,                      120f,  200f);
            Validate("BMaxBossCore",                     _bMaxBossCore,                     160f,  280f);
            Validate("BUnsoftenedMult",                  _bUnsoftenedMult,                  0.20f, 0.50f);
            Validate("RequiredBreakThresholdNormal",     _requiredBreakThresholdNormal,     80f,   150f);
            Validate("RequiredBreakThresholdArmored",    _requiredBreakThresholdArmored,    120f,  200f);
            Validate("RequiredBreakThresholdBossCore",   _requiredBreakThresholdBossCore,   160f,  280f);
            Validate("StaggerDuration",                  _staggerDuration,                  1.5f,  3.0f);
            Validate("StaggerBreakMult",                 _staggerBreakMult,                 1.2f,  2.0f);

            if (_thetaSExit >= _thetaS)
                Debug.LogError(
                    $"[WeaponBalanceConfig] ThetaSExit ({_thetaSExit}) must be strictly less than " +
                    $"ThetaS ({_thetaS}) to maintain the softened hysteresis band.", this);

            if ((int)_defaultPrimary >= 4)
                Debug.LogError(
                    $"[WeaponBalanceConfig] DefaultPrimary ({_defaultPrimary}) must be a laser (L1–L4).", this);
            if ((int)_defaultSecondary < 4)
                Debug.LogError(
                    $"[WeaponBalanceConfig] DefaultSecondary ({_defaultSecondary}) must be a missile (M1–M4).", this);
        }

        private void Validate(string field, float value, float min, float max)
        {
            if (value < min || value > max)
                Debug.LogError(
                    $"[WeaponBalanceConfig] {field} = {value} is outside safe range [{min}, {max}].", this);
        }
#endif
    }
}
