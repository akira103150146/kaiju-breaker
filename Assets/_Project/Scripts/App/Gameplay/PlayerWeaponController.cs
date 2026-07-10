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
        private float _fireIntervalMult = 1f; // meta utility upgrade (lower = faster)
        private float _secondaryCooldownMult = 1f; // Swarm-core meta utility (lower = faster missiles; count unchanged)
        private bool _firing = true;

        /// <summary>Apply the meta faster-fire upgrade multiplier to the primary interval (1 = none).</summary>
        public void SetFireIntervalMult(float mult) => _fireIntervalMult = Mathf.Max(0.2f, mult);

        /// <summary>Apply the Swarm-core secondary-cooldown multiplier (1 = none; lower fires missiles more often).</summary>
        public void SetSecondaryCooldownMult(float mult) => _secondaryCooldownMult = Mathf.Max(0.2f, mult);

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
        /// <summary>
        /// Reset the arsenal to the chosen loadout types at run start. <paramref name="startPower"/> is the Void-core
        /// head-start: the run begins at firepower 1 + startPower (clamped to the ceiling) instead of 1. It only moves
        /// the STARTING point — the in-run ceiling (MaxWeaponPower) is unchanged.
        /// </summary>
        public void ResetArsenal(WeaponId primary, WeaponId secondary, int startPower = 0)
        {
            int max = _config != null ? _config.MaxWeaponPower : 1 + startPower;
            _weaponPower = Mathf.Clamp(1 + Mathf.Max(0, startPower), 1, max);
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

        /// <summary>Equip the exact weapon a dwell-cycle pod granted (laser id → primary, missile id → secondary).</summary>
        public void SetWeapon(WeaponId w) { if ((int)w < 4) _primaryType = w; else _secondaryType = w; }

        private void Update()
        {
            if (_config == null || _projectilePrefab == null || !_firing) return;
            float dt = Time.deltaTime;

            _primaryCooldown -= dt;
            if (_primaryCooldown <= 0f) { FirePrimary(); _primaryCooldown = _config.PrimaryFireInterval * _fireIntervalMult; }

            _secondaryCooldown -= dt;
            if (_secondaryCooldown <= 0f && _input != null && _input.SecondaryPressedThisFrame)
            { FireSecondary(); _secondaryCooldown = _config.SecondaryCooldown * _secondaryCooldownMult; }
        }

        // Each primary type expresses firepower growth ALONG ITS OWN IDENTITY, not "more spread for everyone":
        //   L1 散彈  — more + wider thin bullets (crowd clear)   → count grows
        //   L2 集束  — ONE bolt that grows fatter + far stronger  → size & damage grow, count stays 1
        //   L3 波動  — a few WIDE pulses that grow wider          → width grows, count barely
        //   L4 穿透  — a long piercing lance that grows longer    → length & damage grow, count stays ~1
        // t = normalised firepower in [0,1] (lvl1 → max). Per-type flavour multipliers are placeholder-tunable.
        private void FirePrimary()
        {
            int max = Mathf.Max(1, _config.MaxWeaponPower);
            int p = Mathf.Clamp(_weaponPower, 1, max);
            float t = max > 1 ? (p - 1f) / (max - 1f) : 0f;
            float baseSpeed = _config.PrimaryProjectileSpeed, baseDmg = _config.PrimaryDamage, baseHeat = _config.PrimaryHeatDelta;
            float per = _config.PrimarySpreadPerBulletDeg;
            int count; float spreadPer, speed, dmg, heat; bool pierce = false; Vector2 size;

            switch (_primaryType)
            {
                case WeaponId.L2: // 集束 — a single concentrated bolt: grows BIGGER + much stronger, NEVER fans out.
                    count = 1; spreadPer = 0f; speed = baseSpeed * 1.35f;
                    dmg = baseDmg * (1.6f + 2.6f * t); heat = baseHeat * (1.5f + 1.6f * t);
                    size = new Vector2(1.5f + 1.4f * t, 1.5f + 1.4f * t);      // fat round bolt, grows with power
                    break;
                case WeaponId.L3: // 波動 — a few WIDE wave pulses (big & wide), distinct from L1's many thin bullets.
                    count = Mathf.Clamp(1 + p / 3, 1, 3); spreadPer = per * 2.4f; speed = baseSpeed * 0.9f;
                    dmg = baseDmg * (1.2f + 0.7f * t); heat = baseHeat * 1.25f;
                    size = new Vector2(2.2f + 1.6f * t, 1.15f);               // wide pulse, widens with power
                    break;
                case WeaponId.L4: // 穿透 — a long piercing lance: stays 1 (→2 at max), grows longer + stronger.
                    count = p >= max ? 2 : 1; spreadPer = per * 0.5f; speed = baseSpeed * 1.25f;
                    dmg = baseDmg * (1.4f + 1.8f * t); heat = baseHeat * (1.2f + 0.9f * t); pierce = true;
                    size = new Vector2(0.85f, 1.9f + 1.8f * t);              // tall lance, lengthens with power
                    break;
                default: // L1 散彈 — the spread weapon: MORE + WIDER thin bullets as power grows.
                    count = Mathf.Clamp(p, 1, _config.MaxPrimaryBullets); spreadPer = per;
                    speed = baseSpeed; dmg = baseDmg; heat = baseHeat; size = Vector2.one;
                    break;
            }
            FireFan(count, spreadPer, speed, dmg, heat, 0f, false, pierce, _primaryType, size);
        }

        private void FireSecondary()
        {
            int mp = Mathf.Clamp(_missilePower, 1, _config.MaxMissilePower);
            int count = Mathf.Clamp(1 + (mp - 1) / 2, 1, _config.MaxSecondaryMissiles);
            FireFan(count, 9f, _config.SecondaryProjectileSpeed, _config.SecondaryTrashDamage, 0f,
                    _config.SecondaryBreakDamage, true, false, _secondaryType, new Vector2(2f, 2f)); // missiles stay chunky
        }

        // Spawn `count` shots in a fan centred straight up (90°), each scaled by `size` (per-axis).
        private void FireFan(int count, float perDeg, float speed, float dmg, float heat, float breakDmg,
                             bool isMissile, bool pierce, WeaponId weaponId, Vector2 size)
        {
            Vector3 origin = transform.position + (Vector3)_muzzleOffset;
            float total = (count - 1) * perDeg;
            float start = 90f - total * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float ang = (count == 1 ? 90f : start + i * perDeg) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;
                Rent().Launch(origin, vel, dmg, heat, breakDmg, isMissile, pierce, weaponId, size, Return);
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
