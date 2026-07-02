using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// Core for the primary pool (laser family L1–L4). Lasers fill the heat gauge: a fire tick
    /// deposits <c>heat_delta</c> HU on each part its beam(s) cover, published as <see cref="LaserHit"/>.
    /// Concrete lasers (L1/L2/L4 in Story 004, L3 in Story 005, Tier-3 in Story 008) supply the
    /// beam geometry and per-tick rate; this base only owns the publish path (Story 003).
    /// </summary>
    public abstract class LaserWeaponBase : WeaponBehaviourBase
    {
        /// <inheritdoc cref="WeaponBehaviourBase(IEventBus, IWeaponTierQuery, IPartStateQuery, WeaponBalanceConfig, WeaponDef)"/>
        protected LaserWeaponBase(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def)
            : base(bus, tierQuery, partQuery, balance, def) { }

        /// <summary>
        /// Publish one laser heat tick onto <paramref name="partId"/>. Non-positive deltas are
        /// dropped (KaijuParts ignores them anyway) so callers can pass an unclamped rate×dt.
        /// </summary>
        protected void EmitLaserHit(int partId, int kaijuId, float heatDelta)
        {
            if (heatDelta <= 0f) return;
            Bus.Publish(new LaserHit(partId, kaijuId, heatDelta));
        }
    }
}
