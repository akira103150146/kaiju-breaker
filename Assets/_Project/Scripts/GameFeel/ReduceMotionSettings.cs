namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Mutable runtime motion-intensity multipliers for the reduce-motion accessibility option
    /// (game-feel.md §I.7). Lives in the settings/save layer (NOT the read-only GameFeelConfig SO) so it can
    /// change mid-session. Each feel system multiplies its effect by the matching multiplier; the
    /// <see cref="ReduceMotionController"/> flips them between full (1.0) and the reduced profile.
    ///
    /// <para>Reduced profile keeps SOME feedback: shake at 25% (not 0, hit feel preserved) and hitstop at 50%
    /// duration, while slow-mo and the full-screen flash are disabled outright.</para>
    /// </summary>
    public sealed class ReduceMotionSettings
    {
        /// <summary>Shake magnitude multiplier (1.0 full, 0.25 reduced).</summary>
        public float ShakeMult { get; private set; } = 1f;

        /// <summary>Full-screen flash multiplier (1.0 full, 0.0 reduced/off).</summary>
        public float FlashMult { get; private set; } = 1f;

        /// <summary>Slow-motion multiplier (1.0 full, 0.0 reduced/off).</summary>
        public float SlowmoMult { get; private set; } = 1f;

        /// <summary>Hitstop duration multiplier (1.0 full, 0.5 reduced).</summary>
        public float HitstopMult { get; private set; } = 1f;

        /// <summary>Whether reduce-motion is currently on.</summary>
        public bool ReduceMotion { get; private set; }

        /// <summary>Apply (true) or clear (false) the reduce-motion profile. Takes effect immediately.</summary>
        public void SetReduceMotion(bool on)
        {
            ReduceMotion = on;
            ShakeMult = on ? 0.25f : 1f;
            FlashMult = on ? 0f : 1f;
            SlowmoMult = on ? 0f : 1f;
            HitstopMult = on ? 0.5f : 1f;
        }
    }
}
