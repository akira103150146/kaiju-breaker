using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// M4 叢集炸彈 (Cluster Bomb) — secondary-pool weapon. One <see cref="TryFire"/> call drops one
    /// bomb; the scene shell resolves the AoE overlap (Physics2D.OverlapCircleNonAlloc, non-alloc)
    /// and passes in the resulting part-id list. At Tier 0–2 the AoE's total output is piecewise by
    /// target count N — a design decision that overrides the GDD's prose min() formula, which does
    /// not match its own story-006 AC-3 example:
    /// <list type="bullet">
    /// <item>N == 1 → <c>M4SingleTargetMult × D0 × buPerD0</c> BU (2000 at defaults; weapon-system.md
    /// E.5 "lucky hit")</item>
    /// <item>N &gt; 1 → each target gets <c>(M4TotalOutputCapMult × D0 / N) × buPerD0</c> BU (500 each
    /// at N=2)</item>
    /// </list>
    /// Magazine 4 bombs, reload <see cref="WeaponDef.M4ReloadTime"/> (3.5s).
    ///
    /// At <see cref="WeaponBehaviourBase.CurrentTier"/> == 3 the mother bomb instead splits into
    /// <see cref="WeaponDef.M4T3ChildCount"/> (6) star-pattern children, each depositing a flat
    /// <c>M4T3ChildDmgPct × D0 × buPerD0</c> BU (200 at defaults) on its own resolved target — the
    /// scene shell resolves each child's 45°-interval direction and passes in the resulting hit list.
    ///
    /// design/gdd/weapon-system.md C.5 M4, E.5, G.3 · production/epics/weapons/story-006 (base AoE),
    /// story-009 AC-4 (Tier-3 children).
    /// </summary>
    public sealed class M4ClusterBomb : MissileWeaponBase
    {
        /// <inheritdoc cref="WeaponBehaviourBase(IEventBus, IWeaponTierQuery, IPartStateQuery, WeaponBalanceConfig, WeaponDef)"/>
        public M4ClusterBomb(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def) : base(bus, tierQuery, partQuery, balance, def) { }

        /// <summary>Magazine capacity (bombs). weapon-system.md G.3 m4_mag_size.</summary>
        protected override int MagCapacity => Def.M4MagSize;

        /// <summary>Reload duration (s). weapon-system.md G.3 m4_reload_time.</summary>
        protected override float ReloadTime => Def.M4ReloadTime;

        /// <summary>
        /// Drop one bomb. <paramref name="targetPartIds"/> is the scene shell's already-resolved hit
        /// list: at Tier 0–2 the AoE overlap set (piecewise output by count), at Tier-3 the
        /// per-child star-pattern hit set (flat output per child). Consumes 1 round from the
        /// 4-round magazine; returns false (no state change) while reloading or with an empty
        /// magazine. An empty <paramref name="targetPartIds"/> list still consumes the round (bomb
        /// fizzled — nothing in range) but emits no <see cref="MissileHit"/>.
        /// </summary>
        public bool TryFire(IReadOnlyList<int> targetPartIds, int kaijuId)
        {
            if (!TryConsumeShot(1)) return false;

            if (CurrentTier == 3)
                EmitChildren(targetPartIds, kaijuId);
            else
                EmitAoe(targetPartIds, kaijuId);

            return true;
        }

        private void EmitAoe(IReadOnlyList<int> targetPartIds, int kaijuId)
        {
            int n = targetPartIds?.Count ?? 0;
            if (n <= 0) return;

            float breakDeltaBase = n == 1
                ? Def.M4SingleTargetMult * Balance.BuPerD0
                : (Def.M4TotalOutputCapMult / n) * Balance.BuPerD0;

            for (int i = 0; i < n; i++)
                EmitMissileHit(targetPartIds[i], kaijuId, breakDeltaBase);
        }

        private void EmitChildren(IReadOnlyList<int> targetPartIds, int kaijuId)
        {
            if (targetPartIds == null) return;

            float perChild = Def.M4T3ChildDmgPct * Balance.BuPerD0;
            for (int i = 0; i < targetPartIds.Count; i++)
                EmitMissileHit(targetPartIds[i], kaijuId, perChild);
        }
    }
}
