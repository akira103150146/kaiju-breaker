using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// M2 蜂群飛彈 (Swarm Launcher) — secondary-pool weapon. One <see cref="TryFire"/> call launches
    /// the whole magazine as a single salvo: <see cref="WeaponDef.M2MicroCount"/> (8) micro-missiles
    /// at Tier 0–2, each depositing <c>(D0 / M2MicroCount) × buPerD0</c> BU. At
    /// <see cref="WeaponBehaviourBase.CurrentTier"/> == 3 the magazine grows to
    /// <see cref="WeaponDef.M2T3MagCount"/> (12) and the salvo auto-splits into two half-mag bursts
    /// separated by <see cref="WeaponDef.M2T3BurstMicroCd"/> (1s): the first burst fires
    /// synchronously inside <see cref="TryFire"/>, the second fires automatically once the
    /// inter-burst cooldown elapses in <see cref="Tick"/>. A <see cref="TryFire"/> call issued while
    /// the cooldown is pending is a no-op — it does not interrupt or reset the pending burst.
    ///
    /// Pure C#: the scene shell resolves which part each micro-missile actually lands on (via
    /// Physics2D.OverlapCircleNonAlloc / a raycast fan) and passes the resolved id list in; missed
    /// missiles are simply absent from the list — no <see cref="MissileHit"/> is published for them.
    ///
    /// design/gdd/weapon-system.md C.5 M2, G.3 · production/epics/weapons/story-006 (base fire),
    /// story-009 AC-2 (Tier-3 burst split).
    /// </summary>
    public sealed class M2SwarmLauncher : MissileWeaponBase
    {
        private float _burstCooldownRemaining;
        private IReadOnlyList<int> _pendingBurstBTargets;
        private int _pendingBurstBKaijuId;

        /// <inheritdoc cref="WeaponBehaviourBase(IEventBus, IWeaponTierQuery, IPartStateQuery, WeaponBalanceConfig, WeaponDef)"/>
        public M2SwarmLauncher(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def) : base(bus, tierQuery, partQuery, balance, def) { }

        /// <summary>8 at Tier 0–2, 12 at Tier-3 (weapon-system.md G.3 m2_micro_count / m2_t3_mag_count).</summary>
        protected override int MagCapacity => CurrentTier == 3 ? Def.M2T3MagCount : Def.M2MicroCount;

        /// <summary>Reload duration (s) — unchanged at Tier-3. weapon-system.md G.3 m2_reload_time.</summary>
        protected override float ReloadTime => Def.M2ReloadTime;

        /// <summary>True while the Tier-3 inter-burst cooldown is running (the second half of the salvo is pending).</summary>
        public bool IsBurstCoolingDown => _burstCooldownRemaining > 0f;

        private int BurstSize => Def.M2T3MagCount / 2;

        private float PerMicroBreakDelta => (Balance.D0Reference / Def.M2MicroCount) * Balance.BuPerD0;

        /// <summary>
        /// Fire one salvo. <paramref name="hitPartIds"/> is the scene shell's already-resolved list
        /// of parts each landed micro-missile struck (may be shorter than the fired count — misses
        /// are simply absent). At Tier 0–2 this consumes the full 8-round magazine and emits one
        /// <see cref="MissileHit"/> per entry immediately. At Tier-3 it consumes and emits only the
        /// first 6-round burst; <paramref name="hitPartIds"/> is cached and replayed for the second
        /// burst once <see cref="Tick"/> clears the inter-burst cooldown. Returns false (no state
        /// change) while reloading, with insufficient ammo, or while a Tier-3 inter-burst cooldown
        /// is pending.
        /// </summary>
        public bool TryFire(IReadOnlyList<int> hitPartIds, int kaijuId)
        {
            if (IsBurstCoolingDown) return false;

            bool tier3 = CurrentTier == 3;
            int burstCount = tier3 ? BurstSize : Def.M2MicroCount;
            if (!TryConsumeShot(burstCount)) return false;

            EmitHits(hitPartIds, kaijuId);

            if (tier3 && Ammo > 0)
            {
                _burstCooldownRemaining = Def.M2T3BurstMicroCd;
                _pendingBurstBTargets = hitPartIds;
                _pendingBurstBKaijuId = kaijuId;
            }

            return true;
        }

        /// <summary>
        /// Advances both the shared reload timer (<see cref="MissileWeaponBase.Tick"/>, called
        /// first) and the Tier-3 inter-burst cooldown, auto-firing the pending second burst once the
        /// cooldown elapses. Overrides the virtual base <c>Tick</c>, so the burst advances even when
        /// the weapon is held/ticked as a <see cref="MissileWeaponBase"/>.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (_burstCooldownRemaining <= 0f) return;
            _burstCooldownRemaining -= deltaTime;
            if (_burstCooldownRemaining > 0f) return;

            _burstCooldownRemaining = 0f;
            IReadOnlyList<int> targets = _pendingBurstBTargets;
            int kaijuId = _pendingBurstBKaijuId;
            _pendingBurstBTargets = null;

            TryConsumeShot(BurstSize);
            EmitHits(targets, kaijuId);
        }

        private void EmitHits(IReadOnlyList<int> hitPartIds, int kaijuId)
        {
            if (hitPartIds == null) return;
            float perMicro = PerMicroBreakDelta;
            for (int i = 0; i < hitPartIds.Count; i++)
                EmitMissileHit(hitPartIds[i], kaijuId, perMicro);
        }
    }
}
