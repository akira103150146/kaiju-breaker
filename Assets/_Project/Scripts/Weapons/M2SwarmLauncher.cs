using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// M2 蜂群飛彈 (Swarm Launcher) — secondary-pool "Chain Hive" (feedback point 3, Option B). Keeps
    /// the "many small missiles" identity: one <see cref="TryFire"/> launches a burst of
    /// <see cref="WeaponDef.M2SalvoCount"/> salvos (default 3), each <see cref="WeaponDef.M2MicroCount"/>
    /// (8) micro-missiles depositing <c>M2DmgPerMissileMult × buPerD0</c> BU. The first salvo fires
    /// synchronously inside <see cref="TryFire"/>; the rest auto-fire once <see cref="Tick"/> clears
    /// each <see cref="WeaponDef.M2InterSalvoInterval"/>. The magazine (salvoCount × microCount) and
    /// per-missile output are IDENTICAL at every tier, so Sustained_Output is tier-invariant by
    /// construction (trivially satisfies H.7).
    ///
    /// Tier-3 "飽和點名 (saturation callout)" changes only TARGET SELECTION, not numbers: while a part
    /// is SOFTENED, every micro-missile of the salvo is redirected onto the hottest softened part
    /// (<see cref="IPartStateQuery.GetHottestSoftenedPartId"/>) to saturate it — same hit count, same
    /// per-missile break, so equal-power is preserved.
    ///
    /// Pure C#: the scene shell resolves which part each micro-missile lands on and passes the id list
    /// in; misses are simply absent. A <see cref="TryFire"/> issued mid-burst is a no-op.
    ///
    /// design/gdd/weapon-tiering-and-equal-power.md (Chain Hive) · weapon-system.md C.5 M2.
    /// </summary>
    public sealed class M2SwarmLauncher : MissileWeaponBase
    {
        private int _salvosRemaining;         // salvos left in the current burst (after the first)
        private float _interSalvoRemaining;   // time to the next salvo
        private IReadOnlyList<int> _pendingTargets;
        private int _pendingKaijuId;

        /// <inheritdoc cref="WeaponBehaviourBase(IEventBus, IWeaponTierQuery, IPartStateQuery, WeaponBalanceConfig, WeaponDef)"/>
        public M2SwarmLauncher(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def) : base(bus, tierQuery, partQuery, balance, def) { }

        /// <summary>Whole burst = salvoCount × microCount, identical at every tier (Chain Hive).</summary>
        protected override int MagCapacity => Def.M2SalvoCount * Def.M2MicroCount;

        /// <summary>Reload duration (s). weapon-system.md G.3 m2_reload_time.</summary>
        protected override float ReloadTime => Def.M2ReloadTime;

        /// <summary>True while later salvos of the current Chain Hive burst are still pending.</summary>
        public bool IsBurstInProgress => _salvosRemaining > 0;

        private float PerMicroBreakDelta => Def.M2DmgPerMissileMult * Balance.BuPerD0;

        /// <summary>
        /// Begin a Chain Hive burst: fire the first salvo now and schedule the rest.
        /// <paramref name="hitPartIds"/> is the shell's resolved landed-hit list for a salvo (misses
        /// absent). Returns false (no state change) while reloading, mid-burst, or with insufficient
        /// ammo for a salvo.
        /// </summary>
        public bool TryFire(IReadOnlyList<int> hitPartIds, int kaijuId)
        {
            if (IsBurstInProgress) return false;
            if (!TryConsumeShot(Def.M2MicroCount)) return false;

            EmitSalvo(hitPartIds, kaijuId);

            _salvosRemaining = Def.M2SalvoCount - 1;
            if (_salvosRemaining > 0)
            {
                _interSalvoRemaining = Def.M2InterSalvoInterval;
                _pendingTargets = hitPartIds;
                _pendingKaijuId = kaijuId;
            }
            return true;
        }

        /// <summary>
        /// Advance the reload timer (base) and the inter-salvo timer, auto-firing the next salvo when
        /// it elapses. Overrides the virtual base <c>Tick</c> so the burst advances even when held as
        /// a <see cref="MissileWeaponBase"/>.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (_salvosRemaining <= 0) return;
            _interSalvoRemaining -= deltaTime;
            if (_interSalvoRemaining > 0f) return;

            TryConsumeShot(Def.M2MicroCount); // consume this salvo's rounds from the burst magazine
            EmitSalvo(_pendingTargets, _pendingKaijuId);
            _salvosRemaining--;

            if (_salvosRemaining > 0)
                _interSalvoRemaining = Def.M2InterSalvoInterval;
            else
                _pendingTargets = null;
        }

        private void EmitSalvo(IReadOnlyList<int> hitPartIds, int kaijuId)
        {
            if (hitPartIds == null) return;
            // Tier-3 saturation callout: redirect the whole salvo onto the hottest softened part.
            int redirect = CurrentTier == 3 ? PartQuery.GetHottestSoftenedPartId() : -1;
            float perMicro = PerMicroBreakDelta;
            for (int i = 0; i < hitPartIds.Count; i++)
            {
                int target = redirect >= 0 ? redirect : hitPartIds[i];
                EmitMissileHit(target, kaijuId, perMicro);
            }
        }
    }
}
