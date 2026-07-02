using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// L3 波動砲 (Wave Cannon) — dual-mode tap/charge weapon with an explicit two-state FSM
    /// (Idle/Charging) plus an independent cooldown timer, driven entirely by an injected
    /// <c>deltaTime</c> (never <c>Time.deltaTime</c> internally) so a <c>timeScale = 0</c> pause
    /// falls out for free: the caller simply stops advancing time (Story 005).
    ///
    /// • Release held &lt; <see cref="WeaponDef.L3ChargeTime"/> → TAP: one <see cref="LaserHit"/>
    ///   of <c>L3TapOutputMult * WeaponBalanceConfig.HuPerD0 * heldTime</c> HU; no cooldown, can
    ///   repeat immediately.
    /// • Release held ≥ <see cref="WeaponDef.L3ChargeTime"/> (and not already cooling down) →
    ///   CHARGE: one flat <see cref="LaserHit"/> of <c>L3ChargeOutputMult * HuPerD0</c> HU PLUS a
    ///   same-frame <see cref="WaveHit"/>, then <see cref="WeaponDef.L3ChargeCooldown"/> seconds
    ///   of cooldown during which a long hold can no longer trigger a second charge — it instead
    ///   resolves as a TAP on release (weapon-system.md E.4 / story-005 AC-3's "cooldown still
    ///   allows tap" rule), so a single hold/release cycle never falls through with zero effect.
    /// • Tier-3 "共鳴擴散": on a successful charge release, publishes a SECOND same-frame
    ///   <see cref="LaserHit"/> of <c>L3T3HeatInjectPct * partMaxHeat</c> HU, in addition to the
    ///   normal charge hit (weapon-system.md C.4 / G.2, Story 008).
    ///
    /// <see cref="WaveHit"/> carries only (PartId, KaijuId) — no StaggerDuration payload field;
    /// the stagger window length is <see cref="WeaponBalanceConfig.StaggerDuration"/>, read and
    /// applied by KaijuParts, not carried on this event (per the story-004/005 handoff decision
    /// overriding story-005's original AC-2 payload text).
    /// </summary>
    public sealed class L3WaveCannon : LaserWeaponBase
    {
        private enum State { Idle, Charging }

        private State _state = State.Idle;
        private float _heldTime;
        private float _cooldownRemaining;

        /// <inheritdoc cref="WeaponBehaviourBase(IEventBus, IWeaponTierQuery, IPartStateQuery, WeaponBalanceConfig, WeaponDef)"/>
        public L3WaveCannon(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def)
            : base(bus, tierQuery, partQuery, balance, def) { }

        /// <summary>True while a press is being held and has not yet resolved into a tap or charge.</summary>
        public bool IsCharging => _state == State.Charging;

        /// <summary>True while a prior charge release is still on cooldown (a new charge cannot trigger yet).</summary>
        public bool IsOnCooldown => _cooldownRemaining > 0f;

        /// <summary>Seconds left on the charge cooldown (0 when not cooling down). For HUD.</summary>
        public float CooldownRemaining => _cooldownRemaining > 0f ? _cooldownRemaining : 0f;

        /// <summary>Seconds accumulated in the current hold (0 when Idle). For HUD charge-progress indicators.</summary>
        public float HeldTime => _heldTime;

        /// <summary>
        /// Advance the state machine by one injected time step. <paramref name="targetPartId"/> is
        /// the already-resolved (by the caller) part currently under the crosshair — it is only
        /// consumed at the moment of release (tap or charge fire); a negative value means "no
        /// target," which still runs the state transition but emits no events.
        /// </summary>
        public void UpdateFrame(float deltaTime, bool isHeld, int targetPartId, int kaijuId)
        {
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= deltaTime;
                if (_cooldownRemaining < 0f) _cooldownRemaining = 0f;
            }

            switch (_state)
            {
                case State.Idle:
                    if (isHeld)
                    {
                        _state = State.Charging;
                        _heldTime = deltaTime;
                    }
                    break;

                case State.Charging:
                    if (isHeld)
                    {
                        _heldTime += deltaTime;
                    }
                    else
                    {
                        bool canCharge = _heldTime >= Def.L3ChargeTime && _cooldownRemaining <= 0f;
                        if (canCharge) FireCharge(targetPartId, kaijuId);
                        else FireTap(targetPartId, kaijuId, _heldTime);

                        _heldTime = 0f;
                        _state = State.Idle;
                    }
                    break;
            }
        }

        private void FireTap(int partId, int kaijuId, float heldTime)
        {
            if (partId < 0 || heldTime <= 0f) return;
            float heatDelta = Def.L3TapOutputMult * Balance.HuPerD0 * heldTime;
            EmitLaserHit(partId, kaijuId, heatDelta);
        }

        private void FireCharge(int partId, int kaijuId)
        {
            _cooldownRemaining = Def.L3ChargeCooldown;
            if (partId < 0) return;

            float chargeHeatDelta = Def.L3ChargeOutputMult * Balance.HuPerD0;
            EmitLaserHit(partId, kaijuId, chargeHeatDelta);
            Bus.Publish(new WaveHit(partId, kaijuId));

            if (CurrentTier == 3)
            {
                float injectDelta = Def.L3T3HeatInjectPct * PartQuery.GetMaxHeat(partId);
                EmitLaserHit(partId, kaijuId, injectDelta);
            }
        }
    }
}
