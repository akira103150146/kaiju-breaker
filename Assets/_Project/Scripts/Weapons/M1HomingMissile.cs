using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// M1 追蹤飛彈 (Homing Missile) — secondary-pool weapon. Fires
    /// <see cref="WeaponDef.M1MissilesPerShot"/> (2) missiles per shot that each track toward a
    /// resolved target part; at <see cref="WeaponBehaviourBase.CurrentTier"/> == 3 a shot instead
    /// fires <see cref="WeaponDef.M1T3MissilesPerShot"/> (3) missiles — the first two track the
    /// passed-in target as before, the third auto-locks the current hottest alive part
    /// (<see cref="IPartStateQuery.GetHottestAlivePartId"/>, skipped — but still consumed from the
    /// magazine — if no part is alive).
    ///
    /// Pure C#: the scene shell resolves the tracked target's part id via Physics2D/steering each
    /// frame and passes it into <see cref="TryFire"/>; this class never touches
    /// Physics2D/Rigidbody2D (ADR-0001 kinematic-missile rule). <see cref="CanTrackTarget"/> is the
    /// pure tracking-cone check the shell can reuse every frame to decide whether to steer toward
    /// the lock or fly straight.
    ///
    /// design/gdd/weapon-system.md C.5 M1, G.3 · production/epics/weapons/story-006 (base fire),
    /// story-009 AC-1 (Tier-3 hottest-part lock).
    /// </summary>
    public sealed class M1HomingMissile : MissileWeaponBase
    {
        /// <inheritdoc cref="WeaponBehaviourBase(IEventBus, IWeaponTierQuery, IPartStateQuery, WeaponBalanceConfig, WeaponDef)"/>
        public M1HomingMissile(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def) : base(bus, tierQuery, partQuery, balance, def) { }

        /// <summary>Magazine capacity is fixed at <see cref="WeaponDef.M1MagSize"/> across all tiers — only missiles-per-shot changes at Tier-3.</summary>
        protected override int MagCapacity => Def.M1MagSize;

        /// <summary>Reload duration (s). weapon-system.md G.3 m1_reload_time.</summary>
        protected override float ReloadTime => Def.M1ReloadTime;

        private int MissilesPerShot => CurrentTier == 3 ? Def.M1T3MissilesPerShot : Def.M1MissilesPerShot;

        private float PerMissileBreakDelta => Def.M1DmgPerMissileMult * Balance.D0Reference * Balance.BuPerD0;

        /// <summary>
        /// Fire one shot at <paramref name="targetPartId"/> (the scene shell's resolved lock).
        /// Consumes the tier-appropriate missile count from the magazine (2, or 3 at Tier-3);
        /// returns false (no state change) while reloading or with insufficient ammo remaining. At
        /// Tier-3 the third missile is instead aimed at the current hottest alive part — it is
        /// skipped (while still being deducted from the magazine) if no part is alive.
        /// </summary>
        public bool TryFire(int targetPartId, int kaijuId)
        {
            int shotCount = MissilesPerShot;
            if (!TryConsumeShot(shotCount)) return false;

            bool tier3 = CurrentTier == 3;
            int trackedCount = tier3 ? shotCount - 1 : shotCount;

            for (int i = 0; i < trackedCount; i++)
                EmitMissileHit(targetPartId, kaijuId, PerMissileBreakDelta);

            if (tier3)
            {
                int hottest = PartQuery.GetHottestAlivePartId();
                if (hottest >= 0)
                    EmitMissileHit(hottest, kaijuId, PerMissileBreakDelta);
            }

            return true;
        }

        /// <summary>
        /// Pure tracking-cone check: true when <paramref name="targetWorldPos"/> lies within
        /// ±<see cref="WeaponDef.M1TrackingAngleDeg"/> of <paramref name="missileForward"/> as seen
        /// from <paramref name="missileWorldPos"/>. The scene shell calls this every frame to decide
        /// whether to steer the in-flight missile toward the lock or let it fly straight
        /// (weapon-system.md C.5 M1: no 180° reverse-lock support).
        /// </summary>
        public bool CanTrackTarget(Vector2 missileWorldPos, Vector2 missileForward, Vector2 targetWorldPos)
        {
            Vector2 toTarget = targetWorldPos - missileWorldPos;
            if (toTarget.sqrMagnitude <= 0f) return true;
            return Vector2.Angle(missileForward, toTarget) <= Def.M1TrackingAngleDeg;
        }

        /// <summary>Overload that resolves <paramref name="targetPartId"/>'s world position via <see cref="WeaponBehaviourBase.PartQuery"/>.</summary>
        public bool CanTrackTarget(Vector2 missileWorldPos, Vector2 missileForward, int targetPartId)
            => CanTrackTarget(missileWorldPos, missileForward, PartQuery.GetWorldPosition(targetPartId));
    }
}
