using System.Collections.Generic;
using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Drives the player's fire: the always-on primary auto-fire that clears trash and heats boss parts,
    /// spawning <see cref="PlayerProjectile"/>s from a self-managed pool (production path — no per-shot
    /// Instantiate/Destroy churn). Cadence, speed, damage, and heat all come from <see cref="PlayerShipConfig"/>
    /// (ADR-0003). Secondary (missile) fire hooks into <see cref="IPlayerInput.SecondaryPressedThisFrame"/> and
    /// is fleshed out alongside the boss fight; the pool + muzzle plumbing is shared.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [Tooltip("Ship tuning (fire interval, projectile speed/damage/heat). Required.")]
        [SerializeField] private PlayerShipConfig _config;

        [Tooltip("Pooled player projectile prefab (PlayerProjectile + trigger collider + kinematic Rigidbody2D). Required.")]
        [SerializeField] private PlayerProjectile _projectilePrefab;

        [Tooltip("Muzzle offset from the ship origin (world units) — projectiles spawn from here, travelling up.")]
        [SerializeField] private Vector2 _muzzleOffset = new Vector2(0f, 0.4f);

        [Tooltip("Y above which projectiles are culled (just past the top of the playfield).")]
        [SerializeField] private float _cullY = 7.5f;

        [Tooltip("Parent for pooled projectiles (keeps the hierarchy tidy). Optional.")]
        [SerializeField] private Transform _projectileParent;

        private readonly List<PlayerProjectile> _pool = new List<PlayerProjectile>();
        private float _primaryCooldown;
        private bool _firing = true;

        /// <summary>Enable/disable primary auto-fire (e.g. pause between phases or on defeat).</summary>
        public void SetFiring(bool firing) => _firing = firing;

        private void Update()
        {
            if (_config == null || _projectilePrefab == null) return;
            if (!_firing) return;

            _primaryCooldown -= Time.deltaTime;
            if (_primaryCooldown <= 0f)
            {
                FirePrimary();
                _primaryCooldown = _config.PrimaryFireInterval;
            }
        }

        private void FirePrimary()
        {
            Vector3 origin = transform.position + (Vector3)_muzzleOffset;
            var p = Rent();
            p.Launch(origin, _config.PrimaryProjectileSpeed, _config.PrimaryDamage, _config.PrimaryHeatDelta,
                     _cullY, Return);
        }

        private PlayerProjectile Rent()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (!_pool[i].gameObject.activeSelf) return _pool[i];

            var created = Instantiate(_projectilePrefab, _projectileParent);
            created.gameObject.SetActive(false);
            _pool.Add(created);
            return created;
        }

        private void Return(PlayerProjectile p) { /* deactivated by the projectile; pool reuses it on next Rent */ }
    }
}
