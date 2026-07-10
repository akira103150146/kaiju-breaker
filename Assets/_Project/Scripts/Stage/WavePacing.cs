namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Pure decision for the clear-gated wave pacing used by <see cref="WaveSpawner"/>: given the live field
    /// state, may the NEXT wave release yet? A wave releases once the field is mostly clear (alive ≤ threshold)
    /// OR we have waited too long since it fully spawned (anti-stall for a straggler stuck at an edge) — but
    /// never before a minimum gap since the wave started. Extracted as pure static math so the gating rule is
    /// EditMode-testable without a scene / Time.deltaTime.
    /// </summary>
    public static class WavePacing
    {
        /// <param name="aliveCount">Currently-alive spawned enemies across the field.</param>
        /// <param name="aliveThreshold">Release early once alive ≤ this (field mostly clear).</param>
        /// <param name="sinceFullySpawned">Seconds since the current wave finished spawning.</param>
        /// <param name="maxWaitSeconds">Anti-stall cap: release after this long regardless of alive count.</param>
        /// <param name="waveElapsed">Seconds since the current wave started.</param>
        /// <param name="minGapSeconds">Minimum gap between wave starts (a floor).</param>
        public static bool ShouldReleaseNextWave(int aliveCount, int aliveThreshold, float sinceFullySpawned,
                                                 float maxWaitSeconds, float waveElapsed, float minGapSeconds)
        {
            if (waveElapsed < minGapSeconds) return false;         // never sooner than the floor
            bool clearEnough = aliveCount <= aliveThreshold;
            bool waitedTooLong = sinceFullySpawned >= maxWaitSeconds;
            return clearEnough || waitedTooLong;
        }
    }
}
