namespace KaijuBreaker.Core
{
    /// <summary>
    /// An ARMORED part's armor-gate state, per kaiju-part-system.md C.4.
    /// While <see cref="Intact"/> the weak point is hidden and missiles are deflected
    /// (break fill = 0); an L3 Wave Cannon shockwave transitions it to
    /// <see cref="Stripped"/> for the stagger window. Non-armored parts are always Intact.
    /// (STAGGERED is a separate time-limited overlay, not an armor state.)
    /// </summary>
    public enum ArmorState
    {
        /// <summary>Armor up — weak point hidden, missiles deflected.</summary>
        Intact = 0,

        /// <summary>Armor stripped (by L3 shockwave) — weak point open during the stagger window.</summary>
        Stripped = 1
    }
}
