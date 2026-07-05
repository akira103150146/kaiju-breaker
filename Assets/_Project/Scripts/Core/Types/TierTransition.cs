namespace KaijuBreaker.Core
{
    /// <summary>
    /// A single permanent weapon-upgrade step (material-economy.md §C.3, §C.4). Upgrades are one-way
    /// and sequential — a weapon at tier N can only take the <c>TierN To N+1</c> transition. The enum
    /// value equals the FROM tier, so <c>(int)transition</c> is the required current tier and
    /// <c>(int)transition + 1</c> is the resulting tier (see <see cref="TierTransitionExtensions"/>).
    /// </summary>
    public enum TierTransition
    {
        /// <summary>Tier 0 → 1 (Minor Enhancement): shards only.</summary>
        Tier0To1 = 0,

        /// <summary>Tier 1 → 2 (Identity Deepening): shards + weapon-theme core.</summary>
        Tier1To2 = 1,

        /// <summary>Tier 2 → 3 (Mechanic Unlock): shards + core + one essence.</summary>
        Tier2To3 = 2
    }

    /// <summary>Tier arithmetic helpers for <see cref="TierTransition"/>.</summary>
    public static class TierTransitionExtensions
    {
        /// <summary>The tier a weapon must currently be at to take this transition.</summary>
        public static int FromTier(this TierTransition t) => (int)t;

        /// <summary>The tier a weapon reaches after this transition succeeds.</summary>
        public static int ToTier(this TierTransition t) => (int)t + 1;
    }
}
