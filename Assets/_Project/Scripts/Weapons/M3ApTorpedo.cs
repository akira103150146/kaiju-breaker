using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// M3 穿甲魚雷 (AP Torpedo) — secondary-pool weapon. Fires one torpedo per shot; on impact it
    /// queries <see cref="IPartStateQuery.GetHeatState"/> for the struck part SAME-FRAME (never
    /// cached — weapon-system.md E.1 / story-007 AC-4) to decide the heat-shock gate:
    /// <see cref="HeatState.Softened"/> detonates for
    /// <c>M3DmgUnsoftenedMult × M3HeatShockFillMult × D0 × buPerD0</c> BU (6000 at defaults); any
    /// other state deposits the base <c>M3DmgUnsoftenedMult × D0 × buPerD0</c> BU (3000 at
    /// defaults) — KaijuParts still applies <c>B_unsoftened_mult</c> downstream, this class never
    /// applies state multipliers itself. Magazine 3 torpedoes, reload
    /// <see cref="WeaponDef.M3ReloadTime"/> (4s).
    ///
    /// M3's Tier-3 "AP Chain" mechanic is intentionally NOT implemented here — it is fully owned by
    /// KaijuParts (<c>PartStateSystem.OnPartBroke → ApplyM3Chain</c>), which already subscribes to
    /// <see cref="PartBroke"/> and reads <c>PartSystemConfig.M3T3*</c> knobs. If this class also
    /// emitted chain <see cref="MissileHit"/>s on a neighbour break it would double-count break
    /// fill (production/epics/weapons/story-009 out-of-scope note). This class therefore has no
    /// Tier-3 branch and does not subscribe to <see cref="PartBroke"/> beyond the inherited
    /// no-op <see cref="WeaponBehaviourBase.ClearCollider"/> hook.
    ///
    /// design/gdd/weapon-system.md C.5 M3, E.1, G.3 · production/epics/weapons/story-007.
    /// </summary>
    public sealed class M3ApTorpedo : MissileWeaponBase
    {
        /// <inheritdoc cref="WeaponBehaviourBase(IEventBus, IWeaponTierQuery, IPartStateQuery, WeaponBalanceConfig, WeaponDef)"/>
        public M3ApTorpedo(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def) : base(bus, tierQuery, partQuery, balance, def) { }

        /// <summary>Magazine capacity (torpedoes). weapon-system.md G.3 m3_mag_size.</summary>
        protected override int MagCapacity => Def.M3MagSize;

        /// <summary>Reload duration (s). weapon-system.md G.3 m3_reload_time.</summary>
        protected override float ReloadTime => Def.M3ReloadTime;

        /// <summary>
        /// Fire one torpedo at <paramref name="targetPartId"/> (the scene shell's resolved
        /// straight-line pierce hit). Consumes 1 round from the 3-round magazine; returns false (no
        /// state change) while reloading or with an empty magazine. Queries the target's heat state
        /// fresh on every call — no caching across shots (story-007 AC-4).
        /// </summary>
        public bool TryFire(int targetPartId, int kaijuId)
        {
            if (!TryConsumeShot(1)) return false;

            HeatState heat = PartQuery.GetHeatState(targetPartId);
            float breakDeltaBase = heat == HeatState.Softened
                ? Def.M3DmgUnsoftenedMult * Def.M3HeatShockFillMult * Balance.BuPerD0
                : Def.M3DmgUnsoftenedMult * Balance.BuPerD0;

            EmitMissileHit(targetPartId, kaijuId, breakDeltaBase);
            return true;
        }
    }
}
