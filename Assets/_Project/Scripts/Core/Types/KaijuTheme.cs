namespace KaijuBreaker.Core
{
    /// <summary>
    /// A kaiju's material theme — the stable identity that determines which core its parts drop
    /// (material-economy.md §C.1 層級二). Every breakable part of a kaiju yields its theme core,
    /// regardless of <see cref="PartType"/>. Distinct from the runtime int kaiju id: many kaiju may
    /// share a theme. Economy resolves theme via <see cref="IKaijuThemeQuery"/>, then maps
    /// theme → core through <c>EconomyConfig</c> (theme→core is the sole economy-owned mapping).
    /// </summary>
    public enum KaijuTheme
    {
        /// <summary>甲殼系 — armored/carapace kaiju (e.g. CARAPEX). Parts drop <see cref="MaterialId.CoreCarapace"/>.</summary>
        Carapace = 0,

        /// <summary>肢體系 — limb/agile kaiju (e.g. LACERA). Parts drop <see cref="MaterialId.CoreLimb"/>.</summary>
        Limb = 1,

        /// <summary>能量系 — energy/shield kaiju (e.g. VOLTWYRM). Parts drop <see cref="MaterialId.CoreEnergy"/>.</summary>
        Energy = 2
    }
}
