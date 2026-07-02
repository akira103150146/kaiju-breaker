using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// Core for the secondary pool (missile family M1–M4). Missiles fill the armor-break gauge:
    /// each missile deposits <c>break_delta_base</c> BU (pre-multiplier — KaijuParts applies the
    /// softened/armor/stagger multipliers), published as <see cref="MissileHit"/>. Owns the shared
    /// magazine → reload state machine (Story 003): READY → (fire consumes ammo) → when empty →
    /// RELOADING for <see cref="ReloadTime"/> → refilled → READY. Concrete missiles (Story 006/007,
    /// Tier-3 Story 009) supply capacity, reload, targeting geometry, and per-missile output.
    ///
    /// Time is injected as a scaled delta into <see cref="Tick"/> so the reload timer is testable
    /// without a live scene or <c>WaitForSeconds</c>.
    /// </summary>
    public abstract class MissileWeaponBase : WeaponBehaviourBase
    {
        private int _ammo;
        private float _reloadTimer;

        /// <inheritdoc cref="WeaponBehaviourBase(IEventBus, IWeaponTierQuery, IPartStateQuery, WeaponBalanceConfig, WeaponDef)"/>
        protected MissileWeaponBase(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def)
            : base(bus, tierQuery, partQuery, balance, def)
        {
            _ammo = MagCapacity;
        }

        /// <summary>Magazine capacity in individual missiles (tier-aware; read from <see cref="WeaponBehaviourBase.Def"/>).</summary>
        protected abstract int MagCapacity { get; }

        /// <summary>Reload duration in seconds (read from <see cref="WeaponBehaviourBase.Def"/>).</summary>
        protected abstract float ReloadTime { get; }

        /// <summary>Missiles remaining in the current magazine.</summary>
        public int Ammo => _ammo;

        /// <summary>True while the reload timer is running (weapon cannot fire).</summary>
        public bool IsReloading => _reloadTimer > 0f;

        /// <summary>True when the weapon can fire at least one missile right now.</summary>
        public bool CanFire => !IsReloading && _ammo > 0;

        /// <summary>Seconds left on the current reload (0 when not reloading). For HUD.</summary>
        public float ReloadRemaining => Mathf.Max(_reloadTimer, 0f);

        /// <summary>
        /// Attempt to consume <paramref name="missileCount"/> missiles for one shot. Returns false
        /// (no state change) while reloading or if fewer than <paramref name="missileCount"/> remain.
        /// On success, decrements ammo and auto-starts the reload when the magazine empties.
        /// Subclasses call this at the top of their fire method, then emit the missiles on success.
        /// </summary>
        protected bool TryConsumeShot(int missileCount)
        {
            if (missileCount <= 0) throw new ArgumentOutOfRangeException(nameof(missileCount));
            if (IsReloading || _ammo < missileCount) return false;

            _ammo -= missileCount;
            if (_ammo <= 0) StartReload();
            return true;
        }

        /// <summary>Force a reload (e.g. manual reload input). No-op while already reloading or when the magazine is full.</summary>
        public void StartReload()
        {
            if (IsReloading || _ammo >= MagCapacity) return;
            _reloadTimer = ReloadTime;
        }

        /// <summary>
        /// Advance the reload timer by one scaled-time frame. When it elapses, the magazine is
        /// refilled to <see cref="MagCapacity"/> and the weapon returns to READY.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_reloadTimer <= 0f) return;
            _reloadTimer -= deltaTime;
            if (_reloadTimer <= 0f)
            {
                _reloadTimer = 0f;
                _ammo = MagCapacity;
            }
        }

        /// <summary>Publish one missile's base break deposit onto a part (softened/armor multipliers applied downstream by KaijuParts).</summary>
        protected void EmitMissileHit(int partId, int kaijuId, float breakDeltaBase)
        {
            if (breakDeltaBase <= 0f) return;
            Bus.Publish(new MissileHit(partId, kaijuId, breakDeltaBase, WeaponId));
        }
    }
}
