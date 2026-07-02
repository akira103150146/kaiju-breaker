namespace KaijuBreaker.Core
{
    /// <summary>
    /// Classification of a breakable kaiju part, per kaiju-part-system.md C.3.
    /// Governs armor gating and win condition — NOT the kaiju's material theme
    /// (that is a separate concern owned by Economy). Carried in the PartBroke event.
    /// </summary>
    public enum PartType
    {
        /// <summary>Normal part — weak point always exposed; soften → break.</summary>
        Normal = 0,

        /// <summary>Armored part — weak point hidden until L3 Wave Cannon strips the armor.</summary>
        Armored = 1,

        /// <summary>Boss core — breaking it ends the fight (win condition).</summary>
        BossCore = 2
    }
}
