using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Drives the player's fire AND holds the in-run arsenal (Raiden-style): a firepower level and a missile
    /// level raised by collecting P / M items, plus the current primary/secondary weapon type switched by W
    /// pods. Higher firepower = more killing power (wider/denser primary spread, more missiles) — it never
    /// changes utility stats (those are the separate meta upgrades). All of this RESETS each run. Cadence and
    /// base speed/damage/heat come from <see cref="PlayerShipConfig"/>; per-type flavour (spread / focus /
    /// wide / pierce) is a light placeholder table until the real WeaponBehaviour classes drive fire.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private PlayerShipConfig _config;
        [Tooltip("Pooled player projectile prefab (PlayerProjectile + trigger collider + kinematic Rigidbody2D). Required.")]
        [SerializeField] private PlayerProjectile _projectilePrefab;
        [Tooltip("Muzzle offset from the ship origin (world units) — shots spawn from here.")]
        [SerializeField] private Vector2 _muzzleOffset = new Vector2(0f, 0.42f);
        [Tooltip("Parent for pooled projectiles. Optional.")]
        [SerializeField] private Transform _projectileParent;

        private readonly List<PlayerProjectile> _pool = new List<PlayerProjectile>();
        private IPlayerInput _input;
        private float _primaryCooldown;
        private float _secondaryCooldown;
        private bool _firing = true;

        // ── In-run arsenal (resets each run) ──────────────────────────────────────────
        private int _weaponPower = 1;
        private int _missilePower = 1;
        private WeaponId _primaryType = WeaponId.L1;
        private WeaponId _secondaryType = WeaponId.M1;

        /// <summary>Current primary firepower level (1..MaxWeaponPower).</summary>
        public int WeaponPower => _weaponPower;
        /// <summary>Current missile level (1..MaxMissilePower).</summary>
        public int MissilePower => _missilePower;
        /// <summary>Current primary (laser-family) weapon type.</summary>
        public WeaponId PrimaryType => _primaryType;
        /// <summary>Current secondary (missile-family) weapon type.</summary>
        public WeaponId SecondaryType => _secondaryType;

        private void Awake() => _input = GetComponent<IPlayerInput>();

        /// <summary>Enable/disable fire (pause between phases / on defeat).</summary>
        public void SetFiring(bool firing) => _firing = firing;

        /// <summary>Reset the arsenal to level 1 with the chosen loadout types (call at run start).</summary>
        public void ResetArsenal(WeaponId primary, WeaponId secondary)
        {
            _weaponPower = 1;
            _missilePower = 1;
            _primaryType = primary;
            _secondaryType = secondary;
        }

        /// <summary>Collect a P item: +1 firepower (clamped).</summary>
        public void AddWeaponPower() { if (_config != null) _weaponPower = Mathf.Min(_weaponPower + 1, _config.MaxWeaponPower); }
        /// <summary>Collect an M item: +1 missile level (clamped).</summary>
        public void AddMissilePower() { if (_config != null) _missilePower = Mathf.Min(_missilePower + 1, _config.MaxMissilePower); }
        /// <summary>Collect a laser W pod: cycle the primary among L1→L4.</summary>
        public void CyclePrimary() => _primaryType = (WeaponId)(((int)_primaryType + 1) % 4);
        /// <summary>Collect a missile W pod: cycle the secondary among M1→M4.</summary>
        public void CycleSecondary() => _secondaryType = (WeaponId)(4 + (((int)_secondaryType - 4 + 1) % 4));

        private void Update()
        {
            if (_config == null || _projectilePrefab == null || !_firing) return;
            float dt = Time.deltaTime;

            _primaryCooldown -= dt;
            if (_primaryCooldown <= 0f) { FirePrimary(); _primaryCooldown = _config.PrimaryFireInterval; }

            _secondaryCooldown -= dt;
            if (_secondaryCooldown <= 0f && _input != null && _input.SecondaryPressedThisFrame)
            { FireSecondary(); _secondaryCooldown = _config.SecondaryCooldown; }
        }

        private void FirePrimary()
        {
            int p = Mathf.Clamp(_weaponPower, 1, _config.MaxWeaponPower);
            float baseSpeed = _config.PrimaryProjectileSpeed, baseDmg = _config.PrimaryDamage, baseHeat = _config.PrimaryHeatDelta;
            float per = _config.PrimarySpreadPerBulletDeg;
            int count; float spreadPer, speed, dmg, heat; bool pierce = false;

            switch (_primaryType)
            {
                case WeaponId.L2: // 集束 — focused: fewer, faster, stronger
                    count = Mathf.Clamp(1 + p / 2, 1, 4); spreadPer = per * 0.4f; speed = baseSpeed * 1.35f; dmg = baseDmg * 1.5f; heat = baseHeat * 1.4f; break;
                case WeaponId.L3: // 波動 — wide slow spray
                    count = Mathf.Clamp(p, 1, _config.MaxPrimaryBullets); spreadPer = per * 1.8f; speed = baseSpeed * 0.85f; dmg = baseDmg * 1.1f; heat = baseHeat * 1.2f; break;
                case WeaponId.L4: // 穿透 — piercing lance
                    count = Mathf.Clamp(1 + p / 2, 1, 5); spreadPer = per * 0.6f; speed = baseSpeed * 1.2f; dmg = baseDmg * 1.3f; heat = baseHeat; pierce = true; break;
                default: // L1 散波 — spread grows with power
                    count = Mathf.Clamp(p, 1, _config.MaxPrimaryBullets); spreadPer = per; speed = baseSpeed; dmg = baseDmg; heat = baseHeat; break;
            }
            FireFan(count, spreadPer, speed, dmg, heat, 0f, false, pierce, _primaryType);
        }

        private void FireSecondary()
        {
            int mp = Mathf.Clamp(_missilePower, 1, _config.MaxMissilePower);
            int count = Mathf.Clamp(1 + (mp - 1) / 2, 1, _config.MaxSecondaryMissiles);
            FireFan(count, 9f, _config.SecondaryProjectileSpeed, _config.SecondaryTrashDamage, 0f,
                    _config.SecondaryBreakDamage, true, false, _secondaryType);
        }

        // Spawn `count` shots in a fan centred straight up (90°).
        private void FireFan(int count, float perDeg, float speed, float dmg, float heat, float breakDmg,
                             bool isMissile, bool pierce, WeaponId weaponId)
        {
            Vector3 origin = transform.position + (Vector3)_muzzleOffset;
            float total = (count - 1) * perDeg;
            float start = 90f - total * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float ang = (count == 1 ? 90f : start + i * perDeg) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;
                Rent().Launch(origin, vel, dmg, heat, breakDmg, isMissile, pierce, weaponId, Return);
            }
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

        private void Return(PlayerProjectile p) { }
    }
}
