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

        /// <summary>Boss core — breaking it ends the fight (win condition, emits BossCoreBroke).</summary>
        BossCore = 2,

        /// <summary>
        /// Mid-tier encounter core — breaking it ends a MID enemy encounter (emits MidCoreBroke),
        /// NOT the whole run. Distinct from BossCore so a mid mini-boss never triggers run-victory
        /// (enemy-tier-system.md). Uses Normal heat/break caps unless the part overrides them.
        /// </summary>
        MidCore = 3
    }
}
