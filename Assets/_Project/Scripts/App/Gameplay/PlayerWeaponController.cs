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
        private float _waveCharge; // 波動 L3 charge accumulator (seconds); released as a stronger wave when it hits the cap
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

        private SfxPlayer _sfx;

        private void Awake() => _input = GetComponent<IPlayerInput>();

        /// <summary>Enable/disable fire (pause between phases / on defeat).</summary>
        public void SetFiring(bool firing) => _firing = firing;

        /// <summary>Inject the shared SFX sink so primary fire plays a (throttled) shoot blip.</summary>
        public void SetSfx(SfxPlayer sfx) => _sfx = sfx;

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

            // 波動 L3 is a charge weapon (fills over time, releases a stronger wave when full); every other
            // primary is the standard interval auto-fire.
            if (_primaryType == WeaponId.L3) TickWaveCharge(dt);
            else
            {
                _waveCharge = 0f;
                _primaryCooldown -= dt;
                if (_primaryCooldown <= 0f) { FirePrimary(); _primaryCooldown = _config.PrimaryFireInterval * _fireIntervalMult; }
            }

            _secondaryCooldown -= dt;
            if (_secondaryCooldown <= 0f && _input != null && _input.SecondaryPressedThisFrame)
            { FireSecondary(); _secondaryCooldown = _config.SecondaryCooldown * _secondaryCooldownMult; }
        }

        // Firepower growth per the director's explicit per-weapon rules — each type grows differently so the
        // four primaries feel distinct (not "everyone just fans out more bullets"):
        //   L1 散波 — MORE spread bullets (count grows, fan widens)
        //   L2 集束 — count stays 1; grows only a LITTLE bigger, damage much higher
        //   L4 穿透 — MORE parallel straight shots (no fan) + more pierce-through
        //   L3 波動 — a charge weapon handled in TickWaveCharge/FireWave (cap grows → full charge hits harder)
        // t = normalised firepower in [0,1] (lvl1 → max). Per-type multipliers are placeholder-tunable.
        private void FirePrimary()
        {
            int max = Mathf.Max(1, _config.MaxWeaponPower);
            int p = Mathf.Clamp(_weaponPower, 1, max);
            float t = max > 1 ? (p - 1f) / (max - 1f) : 0f;
            float baseSpeed = _config.PrimaryProjectileSpeed, baseDmg = _config.PrimaryDamage, baseHeat = _config.PrimaryHeatDelta;
            float per = _config.PrimarySpreadPerBulletDeg;
            _sfx?.PlayShoot();

            switch (_primaryType)
            {
                case WeaponId.L2: // 集束 — one bolt, count NEVER changes; grows a little bigger, hits much harder.
                {
                    float dmg = baseDmg * (1.6f + 3.0f * t);
                    float heat = baseHeat * (1.6f + 2.0f * t);
                    Vector2 size = new Vector2(1.3f + 0.5f * t, 1.3f + 0.5f * t); // 變大一點點
                    FireFan(1, 0f, 0f, baseSpeed * 1.35f, dmg, heat, 0f, false, 0, WeaponId.L2, size);
                    break;
                }
                case WeaponId.L4: // 穿透 — more PARALLEL straight shots (never a fan) + more pierce-through per shot.
                {
                    int count = Mathf.Clamp(1 + p / 2, 1, _config.MaxPrimaryBullets);
                    int pierce = 1 + p / 2;                                   // enemies each shot passes through grows
                    float dmg = baseDmg * 1.3f;
                    Vector2 size = new Vector2(0.8f, 1.8f);                    // tall lance
                    FireFan(count, 0f, 0.30f, baseSpeed * 1.25f, dmg, baseHeat, 0f, false, pierce, WeaponId.L4, size);
                    break;
                }
                default: // L1 散波 — the spread weapon: MORE + WIDER thin bullets as power grows.
                {
                    int count = Mathf.Clamp(p, 1, _config.MaxPrimaryBullets);
                    FireFan(count, per, 0f, baseSpeed, baseDmg, baseHeat, 0f, false, 0, WeaponId.L1, Vector2.one);
                    break;
                }
            }
        }

        // 波動 L3: charge fills over time; power raises the CAP so a full charge releases a stronger, wider wave
        // (director rule: 集氣上限增加→集滿傷害更高). The faster-fire meta upgrade shortens the fill time.
        private void TickWaveCharge(float dt)
        {
            int max = Mathf.Max(1, _config.MaxWeaponPower);
            int p = Mathf.Clamp(_weaponPower, 1, max);
            float t = max > 1 ? (p - 1f) / (max - 1f) : 0f;
            float cap = Mathf.Lerp(0.55f, 1.6f, t);                          // charge-seconds cap grows with power
            _waveCharge += dt / Mathf.Max(0.2f, _fireIntervalMult);
            if (_waveCharge < cap) return;
            _waveCharge = 0f;
            _sfx?.PlayShoot();
            FireWave(cap);
        }

        private void FireWave(float cap)
        {
            // A full charge to a higher cap = a stronger, wider wave.
            float dmg = _config.PrimaryDamage * (1.4f + 3.2f * cap);
            float heat = _config.PrimaryHeatDelta * (1.4f + 2.0f * cap);
            float speed = _config.PrimaryProjectileSpeed * 0.9f;
            Vector2 size = new Vector2(2.4f + 2.2f * cap, 1.2f + 0.4f * cap); // big wide pulse; wider at higher cap
            FireFan(1, 0f, 0f, speed, dmg, heat, 0f, false, 0, WeaponId.L3, size);
        }

        // 副武器 M1–M4: firepower simply adds MORE missiles + MORE damage (uniform rule across all four types).
        private void FireSecondary()
        {
            int maxp = Mathf.Max(1, _config.MaxMissilePower);
            int mp = Mathf.Clamp(_missilePower, 1, maxp);
            float t = maxp > 1 ? (mp - 1f) / (maxp - 1f) : 0f;
            int count = Mathf.Clamp(1 + (mp - 1) / 2, 1, _config.MaxSecondaryMissiles);
            float dmg = _config.SecondaryTrashDamage * (1f + 0.9f * t);      // damage grows with missile level
            float breakDmg = _config.SecondaryBreakDamage * (1f + 0.9f * t);
            FireFan(count, 9f, 0f, _config.SecondaryProjectileSpeed, dmg, 0f, breakDmg, true, 0, _secondaryType, new Vector2(2f, 2f));
        }

        // Spawn `count` shots. lateralSpacing > 0 => PARALLEL shots offset horizontally, all straight up (穿透);
        // lateralSpacing == 0 => an angular fan spaced perDeg, centred straight up. Each shot scaled by `size`.
        private void FireFan(int count, float perDeg, float lateralSpacing, float speed, float dmg, float heat,
                             float breakDmg, bool isMissile, int pierceCount, WeaponId weaponId, Vector2 size)
        {
            Vector3 origin = transform.position + (Vector3)_muzzleOffset;
            if (lateralSpacing > 0f)
            {
                float x0 = -(count - 1) * lateralSpacing * 0.5f;
                Vector2 up = new Vector2(0f, speed);
                for (int i = 0; i < count; i++)
                {
                    Vector3 o = origin + new Vector3(x0 + i * lateralSpacing, 0f, 0f);
                    Rent().Launch(o, up, dmg, heat, breakDmg, isMissile, pierceCount, weaponId, size, Return);
                }
                return;
            }
            float total = (count - 1) * perDeg;
            float start = 90f - total * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float ang = (count == 1 ? 90f : start + i * perDeg) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;
                Rent().Launch(origin, vel, dmg, heat, breakDmg, isMissile, pierceCount, weaponId, size, Return);
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
