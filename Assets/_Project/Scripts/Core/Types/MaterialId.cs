namespace KaijuBreaker.Core
{
    /// <summary>
    /// Stable identifier for every economy material. Maps 1:1 to material-economy.md §C.1's
    /// three-tier taxonomy: one universal shard, three kaiju-theme cores, and the full-clear essence.
    /// Economy computes yields per <see cref="BreakQuality"/> and banks them through
    /// <see cref="ISaveService.CreditMaterials"/>; Meta persists the inventory (material-economy.md §F.5).
    /// </summary>
    public enum MaterialId
    {
        /// <summary>通用碎片 — dropped by ANY part break; used by every weapon upgrade tier (§C.1 層級一).</summary>
        ShardCommon = 0,

        /// <summary>甲殼核心 — dropped by any part of a Carapace-theme kaiju (CARAPEX). Upgrades L1/M2/M4.</summary>
        CoreCarapace = 1,

        /// <summary>四肢核心 — dropped by any part of a Limb-theme kaiju (LACERA). Upgrades L2/L4/M1.</summary>
        CoreLimb = 2,

        /// <summary>能量核心 — dropped by any part of an Energy-theme kaiju (VOLTWYRM). Upgrades L3/M3.</summary>
        CoreEnergy = 3,

        /// <summary>巨獸精魄 — awarded only on a full-clear hunt settlement; each weapon's Tier 2→3 needs one (§C.1 層級三).</summary>
        EssenceKaiju = 4
    }
}
