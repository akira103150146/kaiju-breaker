using KaijuBreaker.Core;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Pure evaluation of a per-part emitter's <see cref="PartFireGate"/> against the emitter owner
    /// part's live heat/armor state (per-part-firing-schema.md §3). Extracted from
    /// <c>BossController.GateOpen</c> so the gate truth-table is EditMode-testable in isolation, with
    /// no Play harness. Owner-alive gating (AliveOnly silences on break) is handled by the caller.
    /// </summary>
    public static class EmitterGateEval
    {
        /// <summary>
        /// True when the emitter is permitted to fire this frame.
        /// </summary>
        /// <param name="gate">The emitter's configured fire gate.</param>
        /// <param name="ownerHeat">Heat state of the part that owns the emitter.</param>
        /// <param name="ownerArmor">Armor state of the part that owns the emitter.</param>
        /// <param name="gatePartBroken">
        /// For <see cref="PartFireGate.RequireGatePartBroken"/> only: whether the referenced gate part
        /// has broken. Ignored by the other gates.
        /// </param>
        public static bool IsOpen(PartFireGate gate, HeatState ownerHeat, ArmorState ownerArmor, bool gatePartBroken)
        {
            switch (gate)
            {
                case PartFireGate.SilenceWhenSoftened:
                    return ownerHeat != HeatState.Softened;
                case PartFireGate.SilenceWhenSoftenedOrStripped:
                    // Either compromises the crystal shell -> the facet stops refracting (and firing).
                    return ownerHeat != HeatState.Softened && ownerArmor != ArmorState.Stripped;
                case PartFireGate.RequireArmorStripped:
                    return ownerArmor == ArmorState.Stripped;
                case PartFireGate.RequireGatePartBroken:
                    return gatePartBroken;
                default:
                    return true; // AliveOnly — owner-alive already checked by the caller.
            }
        }
    }
}
