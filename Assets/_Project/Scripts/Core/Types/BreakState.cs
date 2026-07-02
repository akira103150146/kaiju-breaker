namespace KaijuBreaker.Core
{
    /// <summary>
    /// A part's irreversible break lifecycle, per kaiju-part-system.md C.1 / E.7.
    /// Parts start <see cref="Alive"/> and transition once to <see cref="Broken"/> when
    /// their break bar reaches the destruction threshold. BROKEN is a terminal state:
    /// the part never regenerates within a run (part_regen_enabled is always false) and
    /// ignores all further hit events. A new round re-initialises every part to Alive.
    /// Distinct from <see cref="HeatState"/> / <see cref="ArmorState"/> (transient states)
    /// and from <see cref="BreakQuality"/> (the reward tier recorded at the break frame).
    /// </summary>
    public enum BreakState
    {
        /// <summary>Part is intact and processing hits normally.</summary>
        Alive = 0,

        /// <summary>Part has broken — terminal, irreversible for the rest of the run.</summary>
        Broken = 1
    }
}
