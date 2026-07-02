namespace KaijuBreaker.Core
{
    /// <summary>
    /// The high-level run flow state machine, per stage-system.md + architecture.md §4.
    /// Loadout → Stage → Boss → Results. Owned/driven by the Stage/Run controller;
    /// shared here so UI and other systems can react to run-state changes.
    /// </summary>
    public enum RunState
    {
        /// <summary>Pre-run meta: pick primary + secondary + difficulty + boss.</summary>
        Loadout = 0,

        /// <summary>Trash-wave segments + weapon-pod pickup.</summary>
        Stage = 1,

        /// <summary>Boss encounter (break parts).</summary>
        Boss = 2,

        /// <summary>Results / settlement (materials banked, records updated).</summary>
        Results = 3
    }
}
