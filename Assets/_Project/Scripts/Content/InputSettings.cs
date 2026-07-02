using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Static authoring-layer configuration for all three input schemes:
    /// Touch (primary / mobile), Keyboard + Mouse, and Gamepad.
    /// <para>
    /// These are the designer-editable calibration defaults. Player remapping and
    /// per-device overrides are stored in the player save JSON (ADR-0004), not here.
    /// </para>
    /// <para>
    /// <b>L3ChargeHoldThresholdSeconds</b> is the <em>input system's</em> long-press
    /// recognition threshold, distinct from <c>weapon-system.md</c> <c>l3_charge_time</c>
    /// (the weapon's heat accumulation duration). See input-system.md §D.3, §K.1.
    /// </para>
    /// <para>
    /// Input system parameters are <b>difficulty-invariant</b> — four difficulty tiers
    /// change bullet density only; input responsiveness is never scaled.
    /// </para>
    /// <para>
    /// <b>Pure static data container</b> — no runtime input polling or binding logic.
    /// </para>
    /// See input-system.md §K, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/InputSettings", fileName = "InputSettings")]
    public sealed class InputSettings : ScriptableObject
    {
        [Header("Touch — Movement (K.1)")]
        [Tooltip("Global touch sensitivity multiplier applied to relative drag delta. " +
                 "1.0 = calibrated default (no scaling). Range: (0.0, 3.0].")]
        [SerializeField] private float _touchSensitivity = 1.0f;

        [Tooltip("Dead-zone radius in physical screen pixels. " +
                 "Finger movement within this radius from the anchor point does not move the ship, " +
                 "preventing jitter from a stationary finger. " +
                 "input-system.md §K.1 touch_dead_zone_px = 4 px.")]
        [SerializeField] private float _touchDeadzoneRadius = 4f;

        [Tooltip("Per-frame exponential follow lerp coefficient for relative drag smoothing " +
                 "(60 fps baseline). 0.92 = slight lag that absorbs jitter. " +
                 "Higher = more responsive; lower = smoother but more latent. Range: (0.0, 1.0]. " +
                 "input-system.md §K.1 touch_follow_lerp — primary playtest calibration target.")]
        [SerializeField] private float _relativeDragLerp = 0.92f;

        [Header("Gamepad — Movement (K.3)")]
        [Tooltip("Left-stick dead-zone as a normalised magnitude [0, 1]. " +
                 "Stick deflection below this value is treated as zero. " +
                 "input-system.md §K.3 gamepad_dead_zone = 0.12. Range: [0.0, 0.5].")]
        [SerializeField] private float _gamepadDeadzoneNormalized = 0.12f;

        [Header("L3 Charge — Input Recognition (K.1)")]
        [Tooltip("Minimum hold duration in seconds to be recognised as an L3 Wave Cannon " +
                 "full-power charge intent. The input system gates on this threshold; " +
                 "the weapon system accumulates actual charge heat independently. " +
                 "Distinct from weapon-system.md l3_charge_time (weapon heat accumulation). " +
                 "input-system.md §K.1 l3_charge_time = 1.5 s. Must be > 0.")]
        [SerializeField] private float _l3ChargeHoldThresholdSeconds = 1.5f;

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>
        /// Global touch sensitivity scale. 1.0 = default calibration.
        /// Applied to relative drag delta before ship position update.
        /// </summary>
        public float TouchSensitivity => _touchSensitivity;

        /// <summary>
        /// Touch dead-zone radius in physical screen pixels.
        /// Prevents micro-jitter when the finger is nominally at rest.
        /// </summary>
        public float TouchDeadzoneRadius => _touchDeadzoneRadius;

        /// <summary>
        /// Per-frame exponential lerp coefficient for relative drag follow smoothing (60 fps baseline).
        /// <c>ship_pos(t) = lerp(ship_pos(t-1), target_pos(t), RelativeDragLerp × dt_norm)</c>.
        /// Primary playtest calibration target per input-system.md §L.1.
        /// </summary>
        public float RelativeDragLerp => _relativeDragLerp;

        /// <summary>
        /// Gamepad left-stick dead-zone as a normalised stick magnitude [0.0, 0.5].
        /// Deflection below this value is suppressed to prevent stick drift.
        /// </summary>
        public float GamepadDeadzoneNormalized => _gamepadDeadzoneNormalized;

        /// <summary>
        /// Hold duration threshold for the input system to recognise an L3 charge intent (seconds).
        /// Distinct from the weapon system's charge heat accumulation time.
        /// Toggle mode for accessibility also respects this threshold.
        /// </summary>
        public float L3ChargeHoldThresholdSeconds => _l3ChargeHoldThresholdSeconds;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_touchSensitivity <= 0f || _touchSensitivity > 3.0f)
                Debug.LogError(
                    $"[InputSettings] '{name}': TouchSensitivity must be in (0.0, 3.0]. " +
                    $"Current: {_touchSensitivity}.", this);

            if (_relativeDragLerp <= 0f || _relativeDragLerp > 1.0f)
                Debug.LogError(
                    $"[InputSettings] '{name}': RelativeDragLerp must be in (0.0, 1.0]. " +
                    $"Current: {_relativeDragLerp}.", this);

            if (_gamepadDeadzoneNormalized < 0f || _gamepadDeadzoneNormalized > 0.5f)
                Debug.LogError(
                    $"[InputSettings] '{name}': GamepadDeadzoneNormalized must be in [0.0, 0.5]. " +
                    $"Current: {_gamepadDeadzoneNormalized}.", this);

            if (_l3ChargeHoldThresholdSeconds <= 0f)
                Debug.LogError(
                    $"[InputSettings] '{name}': L3ChargeHoldThresholdSeconds must be > 0. " +
                    $"Current: {_l3ChargeHoldThresholdSeconds}.", this);
        }
    }
}
