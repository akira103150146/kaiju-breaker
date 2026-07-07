using System;
using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Full-screen white flash for break/boss-death impact (game-feel.md §D.3, part of the §D.4 payoff). A
    /// decaying intensity raised with <c>max</c> (never additive) that decays linearly on UNSCALED time; the
    /// rendered overlay alpha is <c>intensity × FlashMaxAlpha</c> so a peak flash never fully whites out the
    /// screen (the player's 1px hitpoint stays identifiable — §I.5 readability guardrail).
    ///
    /// <para>Pure C# model — the payoff sequencer (Story 006) calls <see cref="Trigger"/>; the canvas adapter
    /// reads <see cref="Alpha"/>. The reduce-motion flash multiplier (0 in reduced mode) disables it entirely.</para>
    /// </summary>
    public sealed class FlashSystem
    {
        private readonly GameFeelConfig _config;
        private readonly ReduceMotionSettings _motion; // optional (may be null)
        private float _intensity;

        /// <summary>Current flash intensity [0, 1].</summary>
        public float Intensity => _intensity;

        /// <summary>Rendered overlay alpha = intensity × FlashMaxAlpha (never a full white-out).</summary>
        public float Alpha => _intensity * _config.FlashMaxAlpha;

        public FlashSystem(GameFeelConfig config, ReduceMotionSettings motion = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _motion = motion;
        }

        /// <summary>Raise the flash to <paramref name="value"/> (max, not add), scaled by the a11y multipliers.</summary>
        public void Trigger(float value)
        {
            float v = Mathf.Clamp01(value) * _config.FlashAccessibilityMult * (_motion?.FlashMult ?? 1f);
            _intensity = Mathf.Max(_intensity, v);
        }

        /// <summary>Decay the flash on <paramref name="unscaledDeltaSeconds"/> (linear, floored at 0).</summary>
        public void Tick(float unscaledDeltaSeconds)
        {
            _intensity = Mathf.Max(0f, _intensity - _config.FlashDecayRate * unscaledDeltaSeconds);
        }
    }
}
