using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// L1 散波雷射 (Spread Laser) — fires several beams in a fixed fan; each beam's already-
    /// resolved hit (or miss) is passed IN by the caller (Story 004: the scene shell runs the
    /// Physics2D raycasts, this class stays pure C#). Two heat modes, per weapon-system.md C.4 /
    /// D.2:
    ///   • Full spread — ALL beams land a hit: each hit beam deposits
    ///     <c>L1HRateFull * deltaTime / beamCount</c> HU on its own target (sums to the full rate
    ///     across beams).
    ///   • Center-only — the center beam (index 0 of the caller-supplied hit array) lands but at
    ///     least one other beam misses: the center beam alone deposits
    ///     <c>L1HRateCenter * deltaTime</c> HU (the GDD's dedicated "small precise weak point"
    ///     rate — not derived from L1HRateFull, so it is a distinct tunable).
    ///
    /// Beam count (3 at Tier &lt; 3, 4 at Tier 3 — "全幅掃蕩") is NOT read from a WeaponDef field
    /// (none exists for it); instead it is simply the length of the caller-supplied
    /// <c>beamHitPartIds</c> array, since the caller (scene shell) already must decide how many
    /// raycasts to fire based on <see cref="WeaponBehaviourBase.CurrentTier"/>. See the
    /// implementation-notes deviation call-out in the story handoff.
    ///
    /// Tier-3 "全幅掃蕩": any beam that lands a hit also (re)starts a residual-flame timer on
    /// <see cref="ResidualHeatTracker"/> — heat keeps ticking on that part for
    /// <see cref="WeaponDef.L1T3ResidualDuration"/> even after the beam moves away
    /// (weapon-system.md C.4 / G.2).
    /// </summary>
    public sealed class L1SpreadLaser : LaserWeaponBase
    {
        /// <summary>Beam index convention: slot 0 of the caller-supplied array is always the center beam.</summary>
        public const int CenterBeamIndex = 0;

        /// <summary>
        /// Beam count for the current upgrade tier: <see cref="WeaponDef.L1BaseBeamCount"/> + tier,
        /// i.e. 2 / 3 / 4 / 5 beams at Tier 0 / 1 / 2 / 3 (feedback point 3 散彈階梯). The scene shell
        /// fires this many raycasts; per-beam heat = L1HRateFull / beamCount keeps total heat constant.
        /// </summary>
        public int ExpectedBeamCount => Def.L1BaseBeamCount + CurrentTier;

        private readonly ResidualHeatTracker _residualTracker;

        /// <param name="residualTracker">
        /// Shared Tier-3 residual-heat registry (also injected into <see cref="L4PierceBeam"/> so
        /// E.6's "take the max, don't sum" rule can be enforced across both weapons). Required.
        /// </param>
        public L1SpreadLaser(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def, ResidualHeatTracker residualTracker)
            : base(bus, tierQuery, partQuery, balance, def)
        {
            _residualTracker = residualTracker ?? throw new ArgumentNullException(nameof(residualTracker));
        }

        /// <summary>
        /// Fire one frame's worth of beams. <paramref name="beamHitPartIds"/> has one slot per
        /// beam (index 0 = center); a negative value marks that beam as a miss. Emits one
        /// <see cref="LaserHit"/> per beam that lands, using the full-spread or center-only rate
        /// per the class doc. No-op (no events) if every beam misses or the array is empty.
        /// </summary>
        public void FireFrame(float deltaTime, int kaijuId, IReadOnlyList<int> beamHitPartIds)
        {
            if (beamHitPartIds == null || beamHitPartIds.Count == 0 || deltaTime <= 0f) return;

            int beamCount = beamHitPartIds.Count;
            bool allHit = true;
            for (int i = 0; i < beamCount; i++)
            {
                if (beamHitPartIds[i] < 0) { allHit = false; break; }
            }

            bool tier3 = CurrentTier == 3;

            if (allHit)
            {
                float perBeamDelta = Def.L1HRateFull * deltaTime / beamCount;
                for (int i = 0; i < beamCount; i++)
                {
                    int partId = beamHitPartIds[i];
                    EmitLaserHit(partId, kaijuId, perBeamDelta);
                    if (tier3) RegisterResidual(partId, kaijuId);
                }
            }
            else if (beamHitPartIds[CenterBeamIndex] >= 0)
            {
                int partId = beamHitPartIds[CenterBeamIndex];
                EmitLaserHit(partId, kaijuId, Def.L1HRateCenter * deltaTime);
                if (tier3) RegisterResidual(partId, kaijuId);
            }
            // Center misses and not all beams hit: no beam lands cleanly enough to fill heat.
        }

        private void RegisterResidual(int partId, int kaijuId)
        {
            _residualTracker.Register(partId, kaijuId, ResidualChannel.L1Residual,
                Def.L1T3ResidualRateMult * Def.L1HRateFull, Def.L1T3ResidualDuration);
        }
    }
}
