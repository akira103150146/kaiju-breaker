namespace KaijuBreaker.Core
{
    /// <summary>
    /// The four player-selected difficulty tiers, per difficulty-system.md.
    /// Tiers scale ONLY bullet density and enemy count — part TTB, weapon output,
    /// drop rates, materials, and content availability are difficulty-invariant
    /// (pillar 難度是門，不是牆). See <see cref="IDifficultyProvider"/>.
    /// </summary>
    public enum DifficultyTier
    {
        /// <summary>D1 普通 — new-player baseline / low-pressure farming entry.</summary>
        D1 = 0,

        /// <summary>D2 困難.</summary>
        D2 = 1,

        /// <summary>D3 極限.</summary>
        D3 = 2,

        /// <summary>D4 惡夢 — highest bullet density; still no content gating.</summary>
        D4 = 3
    }
}
