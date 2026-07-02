namespace KaijuBreaker.BulletSim.PerfSpike
{
    /// <summary>
    /// ADR-0001 spike tunables. Edit a value, save, and re-enter Play mode to re-run the test.
    /// THROWAWAY: delete this whole PerfSpike folder once ADR-0001 is decided.
    /// </summary>
    public static class BulletPerfSpikeConfig
    {
        /// <summary>How many bullets to spawn. Target: 1000 @ 60fps, 0 GC/frame on a mid-tier phone.
        /// Bump to 2000 / 4000 to find the ceiling once 1000 passes.</summary>
        public const int Count = 1000;

        /// <summary>Bullet speed in world units per second.</summary>
        public const float Speed = 4f;

        /// <summary>Half-extent of the wrap-around box, so bullets stay near origin for the whole test.</summary>
        public const float Bound = 10f;

        /// <summary>Master switch. Set false to disable the spike without deleting the files.</summary>
        public const bool Enabled = true;
    }
}
