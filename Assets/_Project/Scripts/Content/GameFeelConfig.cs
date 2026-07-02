using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Game-feel tuning knobs: screen shake, slow-motion, hitstop, SOFTENED visuals,
    /// white flash, and visual timing upper bounds. Single owner of
    /// SoftenedVisualOnsetMaxSeconds and StaggerVisualOnsetMaxSeconds per TR-content-004
    /// (these are referenced by kaiju-part-system.md G.3 but owned only here).
    /// No runtime Time.timeScale calls — this is a pure data container.
    /// See game-feel.md G.1–G.5 and ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/GameFeelConfig", fileName = "GameFeelConfig")]
    public sealed class GameFeelConfig : ScriptableObject
    {
        // ── G.1 Screen Shake ─────────────────────────────────────────────────

        [Header("G.1 Screen Shake — Event Magnitudes (px)")]
        [Tooltip("Shake magnitude when a part enters SOFTENED state (px). Safe range [0, 6]. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagSoften = 3f;

        [Tooltip("Shake magnitude on ARMORED part armor strip (px). Safe range [2, 8]. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagArmorStrip = 5f;

        [Tooltip("Shake magnitude on L3 Wave Cannon shockwave release (px). Safe range [8, 18]. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagL3Shockwave = 14f;

        [Tooltip("Shake magnitude on M3 torpedo impact (px). Safe range [5, 12]. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagM3TorpedoHit = 9f;

        [Tooltip("Additional shake magnitude on M3 heat-shock detonation — taken as max with torpedo hit. " +
                 "Safe range [4, 12]. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagM3HeatShock = 8f;

        [Tooltip("Shake magnitude on M4 cluster bomb detonation (px). Safe range [4, 10]. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagM4Cluster = 7f;

        [Tooltip("Base shake magnitude on part break (px). Safe range [8, 16]. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagPartBreakBase = 11f;

        [Tooltip("Additional magnitude per already-broken part at time of new break (px/part). " +
                 "Safe range [0, 1.5]. Capped by ShakeMagnitudeCap. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagPartBreakEscalation = 0.7f;

        [Tooltip("Shake magnitude on Boss Core break / boss death (px). Safe range [18, 24]. " +
                 "Must not exceed ShakeMagnitudeCap. game-feel.md G.1.")]
        [SerializeField] private float _shakeMagBossDeath = 24f;

        [Header("G.1 Screen Shake — Global Controls")]
        [Tooltip("Hard readability guardrail: no single event may produce shake above this value (px). " +
                 "Default 24px — exceeding this pushes enemies off screen edge. game-feel.md C.6, G.1.")]
        [SerializeField] private float _shakeMagnitudeCap = 24f;

        [Tooltip("Linear decay rate of current shake (px/s). Safe range [25, 60]. game-feel.md G.1.")]
        [SerializeField] private float _shakeDecayRate = 42f;

        [Tooltip("Shake below this value is clamped to zero (px). game-feel.md G.1.")]
        [SerializeField] private float _shakeThreshold = 0.3f;

        [Tooltip("Accessibility multiplier for all shake magnitudes. " +
                 "Reduce-Motion mode sets this to 0.25. Range [0.0, 1.0]. game-feel.md G.1, H.1.")]
        [SerializeField] private float _shakeAccessibilityMult = 1.0f;

        // ── G.2 Slow-Motion ──────────────────────────────────────────────────

        [Header("G.2 Slow-Motion")]
        [Tooltip("Minimum timescale during part-break slow-mo. Range (0, 1]. Safe range [0.08, 0.25]. " +
                 "game-feel.md G.2.")]
        [SerializeField] private float _slowmoPartBreakTimescale = 0.12f;

        [Tooltip("Duration the timescale holds at the minimum before ramping back (s). " +
                 "Safe range [0.4, 0.9]. game-feel.md G.2.")]
        [SerializeField] private float _slowmoPartBreakHoldSeconds = 0.65f;

        [Tooltip("Minimum timescale during Boss death slow-mo. Range (0, 1]. Safe range [0.03, 0.12]. " +
                 "game-feel.md G.2.")]
        [SerializeField] private float _slowmoBossDeathTimescale = 0.05f;

        [Tooltip("Duration the timescale holds at the minimum before ramping back (s). " +
                 "Safe range [0.8, 1.6]. game-feel.md G.2.")]
        [SerializeField] private float _slowmoBossDeathHoldSeconds = 1.20f;

        [Tooltip("Linear rate at which timescale returns to 1.0 after hold ends (units/s). " +
                 "Safe range [2.5, 5.5]. game-feel.md G.2.")]
        [SerializeField] private float _slowmoRampRate = 3.8f;

        [Tooltip("Accessibility multiplier for slow-motion effect. " +
                 "Reduce-Motion mode sets this to 0.0 (completely disabled). Range [0.0, 1.0]. " +
                 "game-feel.md G.2, H.1.")]
        [SerializeField] private float _slowmoAccessibilityMult = 1.0f;

        // ── G.3 Hitstop ──────────────────────────────────────────────────────

        [Header("G.3 Hitstop (milliseconds)")]
        [Tooltip("Hitstop freeze duration on part break (ms). Safe range [50, 300]. " +
                 "Do not exceed 150ms for non-boss events — above 150ms is perceived as lag. " +
                 "game-feel.md G.3.")]
        [SerializeField] private float _hitstopPartBreakMs = 115f;

        [Tooltip("Hitstop freeze duration on Boss death (ms). Safe range [160, 280]. " +
                 "Can be longer — player need not dodge during Boss death. game-feel.md G.3.")]
        [SerializeField] private float _hitstopBossDeathMs = 220f;

        [Tooltip("Accessibility multiplier for hitstop duration. " +
                 "Reduce-Motion mode sets this to 0.5. Range [0.0, 1.0]. game-feel.md G.3, H.1.")]
        [SerializeField] private float _hitstopAccessibilityMult = 1.0f;

        // ── G.4 SOFTENED Visual ───────────────────────────────────────────────

        [Header("G.4 SOFTENED Visual")]
        [Tooltip("Colour tint applied to parts in the SOFTENED state. Default #FF6600 (orange). " +
                 "Must satisfy the 'bullet-always-readable' visual law (cold = player, warm = threat/opportunity). " +
                 "game-feel.md G.4.")]
        [SerializeField] private Color _softenedColorHue = new Color(1f, 102f / 255f, 0f, 1f);

        [Tooltip("Pulsing glow ring blink rate (Hz). Safe range [1.5, 3.0]. game-feel.md G.4.")]
        [SerializeField] private float _softenedPulseFrequencyHz = 2.0f;

        [Tooltip("Outer glow radius as a fraction of the part's art width. Safe range [0.15, 0.40]. " +
                 "game-feel.md G.4.")]
        [SerializeField] private float _softenedGlowRadiusPct = 0.25f;

        [Tooltip("BLOCKING acceptance gate: SOFTENED colour shift + glow must appear within this many " +
                 "seconds of on_part_softened. Blocks Alpha milestone. " +
                 "Range (0, 1]. SINGLE SOURCE (TR-content-004) — kaiju-part-system.md G.3 references this. " +
                 "game-feel.md G.4.")]
        [SerializeField] private float _softenedVisualOnsetMaxSeconds = 0.5f;

        [Tooltip("Maximum simultaneous SOFTENED sfxSoften sounds in a single frame. Safe range [1, 4]. " +
                 "Prevents audio crowding when multiple parts soften at once. game-feel.md G.4, E.4.")]
        [SerializeField] private int _softenedSfxMaxPerFrame = 2;

        [Tooltip("Whether the optional flame icon above SOFTENED parts is shown by default. " +
                 "Players may override via accessibility menu. game-feel.md G.4.")]
        [SerializeField] private bool _softenedIconEnabled = true;

        // ── G.5 White Flash ───────────────────────────────────────────────────

        [Header("G.5 White Flash")]
        [Tooltip("Linear decay rate of screen flash intensity (units/s). Safe range [1.5, 4.0]. " +
                 "At 2.6/s: flash=1.0 fades in ~0.38s. game-feel.md G.5, D.3.")]
        [SerializeField] private float _flashDecayRate = 2.6f;

        [Tooltip("Maximum flash opacity (0–1). Safe range [0.6, 1.0]. Below 1.0 ensures bullets remain " +
                 "visible at peak flash. game-feel.md G.5, D.3.")]
        [SerializeField] private float _flashMaxAlpha = 0.85f;

        [Tooltip("Accessibility multiplier for flash intensity. " +
                 "Photosensitivity / Reduce-Motion mode sets this to 0.0. Range [0.0, 1.0]. " +
                 "game-feel.md G.5, H.1.")]
        [SerializeField] private float _flashAccessibilityMult = 1.0f;

        // ── Visual Timing — TR-content-004 single-source knobs ───────────────

        [Header("Visual Timing (TR-content-004 — single source)")]
        [Tooltip("Maximum allowed delay between on_part_staggered (armor_stripped=true) and the " +
                 "ARMOR_STRIPPED weak-point frame appearing on screen (s). Safe range [0.1, 0.5]. " +
                 "SINGLE SOURCE (TR-content-004) — kaiju-part-system.md G.3 stagger_visual_onset_max_s " +
                 "references this. game-feel.md (visual timing).")]
        [SerializeField] private float _staggerVisualOnsetMaxSeconds = 0.3f;

        // ── Public read-only properties ──────────────────────────────────────

        // G.1 Shake
        /// <summary>Shake magnitude on SOFTENED (px). game-feel.md G.1.</summary>
        public float ShakeMagSoften => _shakeMagSoften;
        /// <summary>Shake magnitude on armor strip (px). game-feel.md G.1.</summary>
        public float ShakeMagArmorStrip => _shakeMagArmorStrip;
        /// <summary>Shake magnitude on L3 shockwave (px). game-feel.md G.1.</summary>
        public float ShakeMagL3Shockwave => _shakeMagL3Shockwave;
        /// <summary>Shake magnitude on M3 torpedo impact (px). game-feel.md G.1.</summary>
        public float ShakeMagM3TorpedoHit => _shakeMagM3TorpedoHit;
        /// <summary>Additional shake on M3 heat-shock detonation (max with torpedo hit). game-feel.md G.1.</summary>
        public float ShakeMagM3HeatShock => _shakeMagM3HeatShock;
        /// <summary>Shake magnitude on M4 cluster detonation (px). game-feel.md G.1.</summary>
        public float ShakeMagM4Cluster => _shakeMagM4Cluster;
        /// <summary>Base shake magnitude on part break (px). game-feel.md G.1.</summary>
        public float ShakeMagPartBreakBase => _shakeMagPartBreakBase;
        /// <summary>Escalation per broken part added to ShakeMagPartBreakBase (px). game-feel.md G.1.</summary>
        public float ShakeMagPartBreakEscalation => _shakeMagPartBreakEscalation;
        /// <summary>Shake magnitude on Boss death (px). Must not exceed ShakeMagnitudeCap. game-feel.md G.1.</summary>
        public float ShakeMagBossDeath => _shakeMagBossDeath;
        /// <summary>Hard readability cap — no event may exceed this shake magnitude (px). game-feel.md G.1, C.6.</summary>
        public float ShakeMagnitudeCap => _shakeMagnitudeCap;
        /// <summary>Linear shake decay rate (px/s). game-feel.md G.1.</summary>
        public float ShakeDecayRate => _shakeDecayRate;
        /// <summary>Shake below this value is zeroed (px). game-feel.md G.1.</summary>
        public float ShakeThreshold => _shakeThreshold;
        /// <summary>Accessibility scale for all shake magnitudes [0, 1]. game-feel.md G.1.</summary>
        public float ShakeAccessibilityMult => _shakeAccessibilityMult;

        // G.2 Slow-mo
        /// <summary>Part-break slow-mo minimum timescale. game-feel.md G.2.</summary>
        public float SlowmoPartBreakTimescale => _slowmoPartBreakTimescale;
        /// <summary>Duration at minimum timescale for part break (s). game-feel.md G.2.</summary>
        public float SlowmoPartBreakHoldSeconds => _slowmoPartBreakHoldSeconds;
        /// <summary>Boss-death slow-mo minimum timescale. game-feel.md G.2.</summary>
        public float SlowmoBossDeathTimescale => _slowmoBossDeathTimescale;
        /// <summary>Duration at minimum timescale for boss death (s). game-feel.md G.2.</summary>
        public float SlowmoBossDeathHoldSeconds => _slowmoBossDeathHoldSeconds;
        /// <summary>Timescale linear ramp-back rate (units/s). game-feel.md G.2.</summary>
        public float SlowmoRampRate => _slowmoRampRate;
        /// <summary>Accessibility scale for slow-motion [0, 1]. 0 = fully disabled. game-feel.md G.2.</summary>
        public float SlowmoAccessibilityMult => _slowmoAccessibilityMult;

        // G.3 Hitstop
        /// <summary>Hitstop freeze duration on part break (ms). game-feel.md G.3.</summary>
        public float HitstopPartBreakMs => _hitstopPartBreakMs;
        /// <summary>Hitstop freeze duration on Boss death (ms). game-feel.md G.3.</summary>
        public float HitstopBossDeathMs => _hitstopBossDeathMs;
        /// <summary>Accessibility scale for hitstop duration [0, 1]. game-feel.md G.3.</summary>
        public float HitstopAccessibilityMult => _hitstopAccessibilityMult;

        // G.4 SOFTENED
        /// <summary>Colour tint for SOFTENED parts. Default #FF6600 (orange). game-feel.md G.4.</summary>
        public Color SoftenedColorHue => _softenedColorHue;
        /// <summary>SOFTENED glow pulse frequency (Hz). game-feel.md G.4.</summary>
        public float SoftenedPulseFrequencyHz => _softenedPulseFrequencyHz;
        /// <summary>SOFTENED outer glow radius as fraction of part width. game-feel.md G.4.</summary>
        public float SoftenedGlowRadiusPct => _softenedGlowRadiusPct;
        /// <summary>
        /// Max delay for SOFTENED visual to appear after on_part_softened (s).
        /// Blocking Alpha milestone gate. TR-content-004 single source.
        /// game-feel.md G.4.
        /// </summary>
        public float SoftenedVisualOnsetMaxSeconds => _softenedVisualOnsetMaxSeconds;
        /// <summary>Max simultaneous SOFTENED SFX in one frame. game-feel.md G.4.</summary>
        public int SoftenedSfxMaxPerFrame => _softenedSfxMaxPerFrame;
        /// <summary>Whether the flame icon above SOFTENED parts is shown by default. game-feel.md G.4.</summary>
        public bool SoftenedIconEnabled => _softenedIconEnabled;

        // G.5 Flash
        /// <summary>White flash linear decay rate (units/s). game-feel.md G.5.</summary>
        public float FlashDecayRate => _flashDecayRate;
        /// <summary>Maximum flash opacity [0, 1]. game-feel.md G.5.</summary>
        public float FlashMaxAlpha => _flashMaxAlpha;
        /// <summary>Accessibility scale for flash [0, 1]. 0 = fully disabled. game-feel.md G.5.</summary>
        public float FlashAccessibilityMult => _flashAccessibilityMult;

        // Visual timing (TR-content-004)
        /// <summary>
        /// Max delay for ARMOR_STRIPPED weak-point frame to appear after on_part_staggered (s).
        /// TR-content-004 single source; kaiju-part-system.md G.3 stagger_visual_onset_max_s
        /// references this field.
        /// </summary>
        public float StaggerVisualOnsetMaxSeconds => _staggerVisualOnsetMaxSeconds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // ShakeMagnitudeCap guardrail: cap must be >= all individual shake magnitudes
            float maxShakeMag = Mathf.Max(
                _shakeMagSoften,
                _shakeMagArmorStrip,
                _shakeMagL3Shockwave,
                _shakeMagM3TorpedoHit,
                _shakeMagM3HeatShock,
                _shakeMagM4Cluster,
                _shakeMagPartBreakBase,
                _shakeMagBossDeath
            );
            if (_shakeMagnitudeCap < maxShakeMag)
                Debug.LogError(
                    $"[GameFeelConfig] ShakeMagnitudeCap ({_shakeMagnitudeCap} px) is less than " +
                    $"the largest individual shake magnitude ({maxShakeMag} px). " +
                    "Raise ShakeMagnitudeCap or reduce the offending ShakeMag* field. " +
                    "game-feel.md C.6 readability guardrail.", this);

            // Slow-mo timescales must be in (0, 1]
            if (_slowmoPartBreakTimescale <= 0f || _slowmoPartBreakTimescale > 1f)
                Debug.LogError(
                    $"[GameFeelConfig] SlowmoPartBreakTimescale = {_slowmoPartBreakTimescale} " +
                    "must be in range (0.0, 1.0]. game-feel.md G.2.", this);

            if (_slowmoBossDeathTimescale <= 0f || _slowmoBossDeathTimescale > 1f)
                Debug.LogError(
                    $"[GameFeelConfig] SlowmoBossDeathTimescale = {_slowmoBossDeathTimescale} " +
                    "must be in range (0.0, 1.0]. game-feel.md G.2.", this);

            // Hitstop
            if (_hitstopPartBreakMs < 50f || _hitstopPartBreakMs > 300f)
                Debug.LogError(
                    $"[GameFeelConfig] HitstopPartBreakMs = {_hitstopPartBreakMs} " +
                    "is outside safe range [50, 300]. game-feel.md G.3.", this);

            if (_hitstopBossDeathMs < 160f || _hitstopBossDeathMs > 280f)
                Debug.LogError(
                    $"[GameFeelConfig] HitstopBossDeathMs = {_hitstopBossDeathMs} " +
                    "is outside safe range [160, 280]. game-feel.md G.3.", this);

            // SOFTENED visual onset
            if (_softenedVisualOnsetMaxSeconds <= 0f || _softenedVisualOnsetMaxSeconds > 1f)
                Debug.LogError(
                    $"[GameFeelConfig] SoftenedVisualOnsetMaxSeconds = {_softenedVisualOnsetMaxSeconds} " +
                    "must be in range (0.0, 1.0]. This is the Alpha-blocking UX gate. game-feel.md G.4.", this);

            // Flash
            if (_flashMaxAlpha < 0f || _flashMaxAlpha > 1f)
                Debug.LogError(
                    $"[GameFeelConfig] FlashMaxAlpha = {_flashMaxAlpha} " +
                    "must be in range [0.0, 1.0]. game-feel.md G.5.", this);

            // Accessibility multipliers must be in [0, 1]
            ValidateA11yMult("ShakeAccessibilityMult",    _shakeAccessibilityMult);
            ValidateA11yMult("SlowmoAccessibilityMult",   _slowmoAccessibilityMult);
            ValidateA11yMult("HitstopAccessibilityMult",  _hitstopAccessibilityMult);
            ValidateA11yMult("FlashAccessibilityMult",    _flashAccessibilityMult);

            // Stagger visual onset
            if (_staggerVisualOnsetMaxSeconds <= 0f || _staggerVisualOnsetMaxSeconds > 1f)
                Debug.LogError(
                    $"[GameFeelConfig] StaggerVisualOnsetMaxSeconds = {_staggerVisualOnsetMaxSeconds} " +
                    "must be in range (0.0, 1.0]. game-feel.md visual timing.", this);
        }

        private void ValidateA11yMult(string field, float value)
        {
            if (value < 0f || value > 1f)
                Debug.LogError(
                    $"[GameFeelConfig] {field} = {value} must be in range [0.0, 1.0].", this);
        }
#endif
    }
}
