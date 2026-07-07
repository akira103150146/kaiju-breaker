using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Stage 1 first-run onboarding tuning (stage-system.md §H.2; ADR-0003). Holds the knobs the
    /// <c>OnboardingController</c> reads — intro slow-down, the one-time pod tooltip — so no onboarding value
    /// is hardcoded. Only applied when the run is <c>stage_01</c>; balance values are placeholder-tunable.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/OnboardingConfig", fileName = "OnboardingConfig")]
    public sealed class OnboardingConfig : ScriptableObject
    {
        [Header("Intro Segment Slow-Down (H.2 rule 1)")]
        [Tooltip("Multiplier applied to the ram_grub move speed in the intro segment's first wave at D1 " +
                 "(stage-system.md §H.2: 0.70). Range (0, 1].")]
        [SerializeField, Range(0.1f, 1f)] private float _ramGrubIntroSpeedMult = 0.70f;

        [Header("First Pod Pickup Tooltip (H.2 rule 3)")]
        [Tooltip("Tooltip text (or localization key) shown once on the player's first-ever weapon-pod pickup.")]
        [SerializeField] private string _tooltipText = "拾取武器莢艙以替換當前武器";

        [Tooltip("How long the first-pickup tooltip stays on screen, seconds. Must be > 0.")]
        [SerializeField] private float _tooltipDurationSec = 3.0f;

        /// <summary>ram_grub intro-wave speed multiplier at D1 (§H.2 rule 1).</summary>
        public float RamGrubIntroSpeedMult => _ramGrubIntroSpeedMult;

        /// <summary>Text/key for the one-time first-pod-pickup tooltip.</summary>
        public string TooltipText => _tooltipText;

        /// <summary>Duration (seconds) the first-pickup tooltip is shown.</summary>
        public float TooltipDurationSec => _tooltipDurationSec;

        private void OnValidate()
        {
            if (_tooltipDurationSec <= 0f)
                Debug.LogError($"[OnboardingConfig] '{name}': TooltipDurationSec must be > 0. Current: {_tooltipDurationSec}.", this);
        }
    }
}
