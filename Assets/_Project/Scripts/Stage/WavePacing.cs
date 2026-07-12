namespace KaijuBreaker.Stage
{
    /// <summary>What the <see cref="WaveSpawner"/> should do with the current wave this frame.</summary>
    public enum WaveAdvance
    {
        /// <summary>Keep fighting the current wave — do nothing yet.</summary>
        Hold = 0,

        /// <summary>Release the next wave now.</summary>
        ReleaseNext = 1,

        /// <summary>The wave's time limit expired with enemies still alive — order the leftovers to retreat
        /// (they flee off the top); the next wave only releases once they have all cleared.</summary>
        RetreatLeftovers = 2
    }

    /// <summary>
    /// Pure decision for the timed, clear-gated wave pacing used by <see cref="WaveSpawner"/>. Director rule
    /// (session 15): each wave has a TIME LIMIT. If the player clears it (alive ≤ threshold) before the limit,
    /// the next wave releases immediately. If the limit expires with enemies still alive, those leftovers must
    /// LEAVE (retreat off the top) before the next wave enters — a fresh wave never stacks on an unkilled one.
    /// A minimum gap floors how fast consecutive waves can start. Extracted as pure static math so the gating
    /// rule is EditMode-testable without a scene / Time.deltaTime.
    /// </summary>
    public static class WavePacing
    {
        /// <param name="aliveCount">Currently-alive spawned enemies across the field.</param>
        /// <param name="aliveThreshold">Release early once alive ≤ this (field mostly clear).</param>
        /// <param name="waveElapsed">Seconds since the current wave started.</param>
        /// <param name="minGapSeconds">Minimum gap between wave starts (a floor).</param>
        /// <param name="timeLimitSeconds">Per-wave time budget; when it expires with enemies alive they retreat.</param>
        /// <param name="retreating">True once the leftovers have already been ordered to retreat — then we only
        /// wait for the field to fully clear before releasing the next wave.</param>
        public static WaveAdvance Decide(int aliveCount, int aliveThreshold, float waveElapsed,
                                         float minGapSeconds, float timeLimitSeconds, bool retreating)
        {
            // Already retreating: hold until every leftover has fled off the field, then release the next wave.
            if (retreating) return aliveCount <= 0 ? WaveAdvance.ReleaseNext : WaveAdvance.Hold;

            if (waveElapsed < minGapSeconds) return WaveAdvance.Hold;    // never sooner than the floor
            if (aliveCount <= aliveThreshold) return WaveAdvance.ReleaseNext; // cleared it → next wave now
            if (waveElapsed >= timeLimitSeconds) return WaveAdvance.RetreatLeftovers; // time up → they leave
            return WaveAdvance.Hold;
        }
    }
}
