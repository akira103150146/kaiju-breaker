using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// L2 集束雷射 (Focus Beam) — a single narrow hold-to-fire beam with a hard in/out cutoff:
    /// the caller (scene shell) resolves whether the beam's tight collider is currently
    /// overlapping a part and passes that resolved id IN each frame (Story 004, pure C#). No
    /// falloff at the judgment edge — either the passed target is valid or it is not.
    ///
    /// Tier-3 "破點漣漪" (weapon-system.md C.4 / G.2):
    ///   • Auto-track gate — <see cref="IsAutoTrackActive"/> exposes (as a pure, testable
    ///     decision) whether the current target's heat has crossed
    ///     <see cref="WeaponDef.L2T3AutotrackHeatPct"/>; the scene shell applies the actual
    ///     ±<see cref="WeaponDef.L2T3AutotrackRangePx"/> aim assist when building its raycast.
    ///   • Break ripple — on <see cref="PartBroke"/>, while Tier 3, deposits heat on every live
    ///     neighbour in <see cref="PartBroke.AdjacencyIds"/> equal to
    ///     <c>PartSystemConfig.L2T3AdjacentHeatPct * neighbour.MaxHeat</c> (the canonical source
    ///     per the story handoff — NOT <see cref="WeaponDef.L2T3AdjacentHeatPct"/>, which is a
    ///     dead duplicate field).
    ///
    /// <see cref="WeaponBehaviourBase.Enable"/>/<see cref="WeaponBehaviourBase.Disable"/> are not
    /// virtual, so this class HIDES them (<c>new</c>) to layer its own extra
    /// <see cref="PartBroke"/> subscription on top of the base ClearCollider hook. Callers must
    /// hold an <c>L2FocusBeam</c>-typed reference (not a <see cref="LaserWeaponBase"/> /
    /// <see cref="WeaponBehaviourBase"/> base-typed one) for the ripple handler to be wired up.
    /// </summary>
    public sealed class L2FocusBeam : LaserWeaponBase
    {
        private readonly PartSystemConfig _partSystemConfig;
        private readonly Action<PartBroke> _onPartBrokeRipple;
        private bool _rippleSubscribed;

        /// <param name="partSystemConfig">
        /// Canonical source of the Tier-3 ripple heat percentage (L2T3AdjacentHeatPct). Required
        /// only by L2 among the laser family — no other laser needs part-system-owned knobs.
        /// </param>
        public L2FocusBeam(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def, PartSystemConfig partSystemConfig)
            : base(bus, tierQuery, partQuery, balance, def)
        {
            _partSystemConfig = partSystemConfig ? partSystemConfig : throw new ArgumentNullException(nameof(partSystemConfig));
            _onPartBrokeRipple = OnPartBrokeRipple;
        }

        /// <summary>
        /// Fire one frame. Hard cutoff: emits a <see cref="LaserHit"/> only when
        /// <paramref name="hold"/> is true AND <paramref name="targetPartId"/> is a valid
        /// (non-negative) already-resolved hit; otherwise this is a no-op (no partial credit at
        /// the beam edge).
        /// </summary>
        public void FireFrame(float deltaTime, int kaijuId, bool hold, int targetPartId)
        {
            if (!hold || targetPartId < 0 || deltaTime <= 0f) return;
            EmitLaserHit(targetPartId, kaijuId, Def.L2HRate * deltaTime);
        }

        /// <summary>
        /// Tier-3 auto-track gate: true when this weapon is Tier 3 AND
        /// <paramref name="partId"/>'s current heat has crossed
        /// <see cref="WeaponDef.L2T3AutotrackHeatPct"/> of its max. Pure decision method (no
        /// Physics2D/UnityEngine.Input) — the scene shell applies the actual
        /// ±<see cref="AutoTrackRangePx"/> aim-assist offset when it is true.
        /// </summary>
        public bool IsAutoTrackActive(int partId)
        {
            if (CurrentTier != 3) return false;
            float maxHeat = PartQuery.GetMaxHeat(partId);
            if (maxHeat <= 0f) return false;
            float heatPct = PartQuery.GetCurrentHeat(partId) / maxHeat;
            return heatPct >= Def.L2T3AutotrackHeatPct;
        }

        /// <summary>Max Tier-3 auto-track aim-assist offset (±px), applicable when <see cref="IsAutoTrackActive"/> is true.</summary>
        public float AutoTrackRangePx => Def.L2T3AutotrackRangePx;

        /// <summary>
        /// Begin listening: the base PartBroke→ClearCollider hook PLUS this weapon's own
        /// Tier-3 ripple handler. Idempotent. See class doc re: method hiding.
        /// </summary>
        public new void Enable()
        {
            base.Enable();
            if (_rippleSubscribed) return;
            Bus.Subscribe(_onPartBrokeRipple);
            _rippleSubscribed = true;
        }

        /// <summary>Stop listening: unsubscribes both the ripple handler and the base hook. Idempotent, symmetric with <see cref="Enable"/>.</summary>
        public new void Disable()
        {
            if (_rippleSubscribed)
            {
                Bus.Unsubscribe(_onPartBrokeRipple);
                _rippleSubscribed = false;
            }
            base.Disable();
        }

        private void OnPartBrokeRipple(PartBroke evt)
        {
            if (CurrentTier != 3) return;
            var neighbours = evt.AdjacencyIds;
            if (neighbours == null) return;

            for (int i = 0; i < neighbours.Length; i++)
            {
                int neighbourId = neighbours[i];
                float heatDelta = _partSystemConfig.L2T3AdjacentHeatPct * PartQuery.GetMaxHeat(neighbourId);
                EmitLaserHit(neighbourId, evt.KaijuId, heatDelta);
            }
        }
    }
}
