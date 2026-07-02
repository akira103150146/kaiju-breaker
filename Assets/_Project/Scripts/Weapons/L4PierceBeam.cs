using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// L4 穿透雷射 (Pierce Beam) — fires a piercing pulse every
    /// <see cref="WeaponDef.L4FireInterval"/> seconds; the caller (scene shell) resolves the
    /// already-sorted, already-truncated list of parts the pulse's raycast pierced and passes it
    /// IN each frame (Story 004, pure C#). The fire-interval timer accumulates the injected
    /// <c>deltaTime</c> (frame-rate independent) and fires AT MOST ONE volley per
    /// <see cref="UpdateFrame"/> call even when <c>deltaTime</c> overshoots the interval — it
    /// never loops to "catch up" within a single call.
    ///
    /// Tier-3 "熱殘影": each pierced part also (re)starts an afterimage timer on the shared
    /// <see cref="ResidualHeatTracker"/> — heat keeps ticking on that part for
    /// <see cref="WeaponDef.L4T3AfterimageRateMultDuration"/> even after the beam moves off it
    /// (weapon-system.md C.4 / G.2).
    /// </summary>
    public sealed class L4PierceBeam : LaserWeaponBase
    {
        /// <summary>Hard cap on parts a single pierce volley can hit (weapon-system.md story-004 AC-3).</summary>
        public const int MaxPiercedParts = 8;

        private readonly ResidualHeatTracker _residualTracker;
        private float _fireTimer;

        /// <param name="residualTracker">
        /// Shared Tier-3 residual-heat registry (also injected into <see cref="L1SpreadLaser"/> so
        /// E.6's "take the max, don't sum" rule can be enforced across both weapons). Required.
        /// </param>
        public L4PierceBeam(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def, ResidualHeatTracker residualTracker)
            : base(bus, tierQuery, partQuery, balance, def)
        {
            _residualTracker = residualTracker ?? throw new ArgumentNullException(nameof(residualTracker));
        }

        /// <summary>Seconds accumulated toward the next pierce pulse. For HUD/tests.</summary>
        public float FireTimer => _fireTimer;

        /// <summary>
        /// Advance the fire-interval timer by <paramref name="deltaTime"/>. If it has crossed
        /// <see cref="WeaponDef.L4FireInterval"/>, fires exactly one pierce volley against
        /// <paramref name="piercedPartIds"/> (truncated to <see cref="MaxPiercedParts"/>) and
        /// consumes one interval's worth of the timer, carrying any overshoot into the next call.
        /// Returns true iff a volley fired this call.
        /// </summary>
        public bool UpdateFrame(float deltaTime, int kaijuId, IReadOnlyList<int> piercedPartIds)
        {
            float interval = Def.L4FireInterval;
            if (interval <= 0f) return false;

            _fireTimer += deltaTime;
            if (_fireTimer < interval) return false;

            _fireTimer -= interval;
            Fire(kaijuId, piercedPartIds);
            return true;
        }

        private void Fire(int kaijuId, IReadOnlyList<int> piercedPartIds)
        {
            if (piercedPartIds == null || piercedPartIds.Count == 0) return;

            int count = Mathf.Min(piercedPartIds.Count, MaxPiercedParts);
            float heatDelta = Def.L4HRate * Def.L4FireInterval;
            bool tier3 = CurrentTier == 3;

            for (int i = 0; i < count; i++)
            {
                int partId = piercedPartIds[i];
                EmitLaserHit(partId, kaijuId, heatDelta);
                if (tier3) RegisterAfterimage(partId, kaijuId);
            }
        }

        private void RegisterAfterimage(int partId, int kaijuId)
        {
            _residualTracker.Register(partId, kaijuId, ResidualChannel.L4Afterimage,
                Def.L4T3AfterimageRateMult * Def.L4HRate, Def.L4T3AfterimageRateMultDuration);
        }
    }
}
