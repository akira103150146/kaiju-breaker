namespace KaijuBreaker.Core
{
    /// <summary>
    /// A part's heat (soften-track) state, per kaiju-part-system.md C.2.
    /// Lasers fill heat; at θ_S the part becomes <see cref="Softened"/> (missiles then
    /// deal full break damage). Returned by <see cref="IPartStateQuery"/> and consumed
    /// by GameFeel (the SOFTENED signature) and Weapons (M1 highest-heat targeting).
    /// </summary>
    public enum HeatState
    {
        /// <summary>Below the soften threshold — missiles deal reduced (unsoftened) damage.</summary>
        Intact = 0,

        /// <summary>Heat ≥ θ_S — softened; missiles deal full break damage.</summary>
        Softened = 1
    }
}
