namespace KaijuBreaker.Core
{
    /// <summary>
    /// The quality tier of a part-break, computed by KaijuParts at the moment of
    /// break from the part's heat + stagger state, and carried in the PartBroke event.
    /// Economy reads it to scale material yield (Normal=1× / Softened=1.5× /
    /// SoftenedStaggered=2× + double core). See material-economy.md C.2 and ADR-0002 §3.
    /// This is the reward tier — distinct from <see cref="HeatState"/> (the ongoing state)
    /// and from the ALIVE/BROKEN lifecycle.
    /// </summary>
    public enum BreakQuality
    {
        /// <summary>Broken while unsoftened — base yield (no heat bonus, no stagger).</summary>
        Normal = 0,

        /// <summary>Broken while SOFTENED — heat-bonus yield.</summary>
        Softened = 1,

        /// <summary>Broken while SOFTENED + STAGGERED — highest yield (double core).</summary>
        SoftenedStaggered = 2
    }
}
