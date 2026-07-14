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

        private float _waveChargeCap = 1f; // current 波動 charge ceiling (grows with firepower) — for the HUD bar
        /// <summary>True while the equipped primary is the 波動 charge weapon (L3) — the HUD shows its charge bar then.</summary>
        public bool ChargeActive => _primaryType == WeaponId.L3;
        /// <summary>波動 charge fill in [0,1] (current hold vs the power-scaled cap); 0 for every non-charge primary.</summary>
        public float ChargeFraction01 => ChargeActive && _waveChargeCap > 0f ? Mathf.Clamp01(_waveCharge / _waveChargeCap) : 0f;

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

        /// <summary>
        /// Collect a strengthen chip: raise the player's CURRENT arsenal by one step — both the primary firepower
        /// and the secondary (missile) level at once. Director rule (session 15): there is one generic strengthen
        /// pickup that boosts whatever loadout the player is running, so no pickup is ever "the wrong weapon".
        /// </summary>
        public void AddArsenalPower() { AddWeaponPower(); AddMissilePower(); }
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
        //   L4 穿透 — the ONLY piercing primary: fast, narrow parallel lances that punch through a whole column of
        //             enemies (pierce count grows with power) — a sustained single-lane DPS weapon (director).
        //   L3 波動 — a charge burst handled in TickWaveCharge/FireWave: hold to build a huge WIDE wave; it does NOT
        //             pierce (its raw power is already high — piercing too would break balance, per the director).
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
                case WeaponId.L4: // 穿透 — the ONLY piercing primary: fast, narrow parallel lances that punch through
                {                 // a whole column. Pierce depth scales hard with power (the weapon's identity).
                    int count = Mathf.Clamp(1 + p / 2, 1, _config.MaxPrimaryBullets);
                    int pierce = 1 + p;                                       // enemies each lance passes through grows fast
                    float dmg = baseDmg * 1.25f;
                    Vector2 size = new Vector2(0.7f, 2.0f);                    // tall thin lance
                    FireFan(count, 0f, 0.28f, baseSpeed * 1.4f, dmg, baseHeat, 0f, false, pierce, WeaponId.L4, size);
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

        private const float MinWaveCharge = 0.12f; // below this a release is treated as a stray tap → no shot

        // 波動 L3 is a MANUAL charge weapon (director, session 15): HOLD the charge input (集氣 button / left-mouse /
        // J) to build charge, RELEASE to unleash the wave — the longer the hold, the stronger/wider the wave, up to
        // a power-scaled cap (集氣上限隨 power 增加). The faster-fire meta upgrade builds charge quicker.
        private void TickWaveCharge(float dt)
        {
            int max = Mathf.Max(1, _config.MaxWeaponPower);
            int p = Mathf.Clamp(_weaponPower, 1, max);
            float t = max > 1 ? (p - 1f) / (max - 1f) : 0f;
            float cap = Mathf.Lerp(0.55f, 1.6f, t);                          // charge-seconds cap grows with power
            _waveChargeCap = cap;                                            // expose to the HUD charge bar

            bool held = _input != null && _input.PrimaryHeld;
            if (held)
            {
                _waveCharge = Mathf.Min(cap, _waveCharge + dt / Mathf.Max(0.2f, _fireIntervalMult));
                return;                                                      // keep charging while held
            }
            if (_waveCharge <= 0f) return;                                   // idle, nothing charged
            float charge = _waveCharge;                                      // released → fire what was built
            _waveCharge = 0f;
            if (charge < MinWaveCharge) return;
            _sfx?.PlayShoot();
            FireWave(charge);
        }

        private void FireWave(float charge)
        {
            // The longer the hold (more charge) the stronger + WIDER the wave. It is deliberately a slow, huge, wide
            // wall of energy that does NOT pierce (pierce = 0) — a big charged burst, the opposite of L4's fast thin
            // piercing lances. Raw power is high; piercing on top would be overtuned (director).
            float dmg = _config.PrimaryDamage * (1.6f + 3.4f * charge);
            float heat = _config.PrimaryHeatDelta * (1.5f + 2.2f * charge);
            float speed = _config.PrimaryProjectileSpeed * 0.8f;                     // slow, heavy wall
            Vector2 size = new Vector2(3.0f + 3.0f * charge, 1.3f + 0.5f * charge);   // very wide pulse; wider at full charge
            FireFan(1, 0f, 0f, speed, dmg, heat, 0f, false, 0, WeaponId.L3, size);
        }

        // 副武器 M1–M4 — each type is a DIFFERENT mechanic (director), not just a recolour:
        //   M1 追蹤飛彈 — few missiles that STEER toward the nearest target (homing).
        //   M2 蜂群飛彈 — many small missiles firing STRAIGHT forward (no homing) in a tight column.
        //   M3 穿甲魚雷 — ONE big slow torpedo that PIERCES a whole column, huge break damage vs boss parts.
        //   M4 叢集炸彈 — a shell that, on its first hit, bursts into a small ring of fragments (a mini-explosion).
        // Missile level (mp) scales the payload of whichever type is equipped.
        private void FireSecondary()
        {
            int maxp = Mathf.Max(1, _config.MaxMissilePower);
            int mp = Mathf.Clamp(_missilePower, 1, maxp);
            float t = maxp > 1 ? (mp - 1f) / (maxp - 1f) : 0f;
            float baseDmg = _config.SecondaryTrashDamage, baseBreak = _config.SecondaryBreakDamage;
            float spd = _config.SecondaryProjectileSpeed;

            switch (_secondaryType)
            {
                case WeaponId.M2: // 蜂群 — MANY small straight missiles in a tight fast column (no homing).
                {
                    int count = Mathf.Clamp(2 + mp, 2, _config.MaxSecondaryMissiles);
                    float dmg = baseDmg * (0.7f + 0.5f * t), brk = baseBreak * (0.7f + 0.5f * t);
                    FireFan(count, 0f, 0.32f, spd * 1.25f, dmg, 0f, brk, true, 0, WeaponId.M2, new Vector2(1.3f, 1.6f));
                    break;
                }
                case WeaponId.M3: // 穿甲魚雷 — ONE big slow torpedo, pierces a whole column, massive break damage.
                {
                    int pierce = 3 + mp;                                       // punches through the column
                    float dmg = baseDmg * (1.6f + 1.4f * t), brk = baseBreak * (2.2f + 2.0f * t);
                    FireFan(1, 0f, 0f, spd * 0.8f, dmg, 0f, brk, true, pierce, WeaponId.M3, new Vector2(2.6f, 3.2f));
                    break;
                }
                case WeaponId.M4: // 叢集炸彈 — bursts into a fragment ring on its first hit (mini-explosion).
                {
                    int count = Mathf.Clamp(1 + (mp - 1) / 2, 1, _config.MaxSecondaryMissiles);
                    float dmg = baseDmg * (1f + 0.8f * t), brk = baseBreak * (1f + 0.8f * t);
                    FireFan(count, 9f, 0f, spd, dmg, 0f, brk, true, 0, WeaponId.M4, new Vector2(2.2f, 2.2f),
                            pr => pr.EnableCluster(SpawnClusterFragments));
                    break;
                }
                default: // M1 追蹤 — a few homing missiles that steer to the nearest target.
                {
                    int count = Mathf.Clamp(1 + (mp - 1) / 2, 1, _config.MaxSecondaryMissiles);
                    float dmg = baseDmg * (1f + 0.9f * t), brk = baseBreak * (1f + 0.9f * t);
                    FireFan(count, 16f, 0f, spd, dmg, 0f, brk, true, 0, WeaponId.M1, new Vector2(2f, 2f),
                            pr => pr.EnableHoming(_secondaryHomingTurnDeg));
                    break;
                }
            }
        }

        private const float _secondaryHomingTurnDeg = 220f; // M1 homing turn rate (deg/s) — tunable feel knob

        // M4 叢集: spawn a small ring of short-lived fragment shots at the burst point. Fragments are plain missiles
        // (no homing, no further cluster) so there is no chain reaction; they carry a fraction of the shell's payload.
        private void SpawnClusterFragments(Vector3 at)
        {
            if (_config == null) return;
            const int frags = 6;
            float dmg = _config.SecondaryTrashDamage * 0.6f, brk = _config.SecondaryBreakDamage * 0.6f;
            float spd = _config.SecondaryProjectileSpeed * 0.9f;
            for (int i = 0; i < frags; i++)
            {
                float ang = (90f + i * (360f / frags)) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
                var pr = Rent();
                pr.Launch(at, vel, dmg, 0f, brk, true, 0, WeaponId.M4, new Vector2(1.1f, 1.1f), Return);
            }
        }

        // Spawn `count` shots. lateralSpacing > 0 => PARALLEL shots offset horizontally, all straight up (穿透);
        // lateralSpacing == 0 => an angular fan spaced perDeg, centred straight up. Each shot scaled by `size`.
        // `configure` (optional) runs on each spawned shot right after Launch — used to opt a shot into homing (M1)
        // or cluster-burst (M4) without widening the hot Launch signature.
        private void FireFan(int count, float perDeg, float lateralSpacing, float speed, float dmg, float heat,
                             float breakDmg, bool isMissile, int pierceCount, WeaponId weaponId, Vector2 size,
                             System.Action<PlayerProjectile> configure = null)
        {
            Vector3 origin = transform.position + (Vector3)_muzzleOffset;
            if (lateralSpacing > 0f)
            {
                float x0 = -(count - 1) * lateralSpacing * 0.5f;
                Vector2 up = new Vector2(0f, speed);
                for (int i = 0; i < count; i++)
                {
                    Vector3 o = origin + new Vector3(x0 + i * lateralSpacing, 0f, 0f);
                    var pr = Rent();
                    pr.Launch(o, up, dmg, heat, breakDmg, isMissile, pierceCount, weaponId, size, Return);
                    configure?.Invoke(pr);
                }
                return;
            }
            float total = (count - 1) * perDeg;
            float start = 90f - total * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float ang = (count == 1 ? 90f : start + i * perDeg) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;
                var pr = Rent();
                pr.Launch(origin, vel, dmg, heat, breakDmg, isMissile, pierceCount, weaponId, size, Return);
                configure?.Invoke(pr);
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
