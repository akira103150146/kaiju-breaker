using System;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// A pooled player projectile: kinematic constant-velocity travel (any direction, so the primary can fire
    /// spread fans), trigger-overlap hit resolution, and a despawn callback back to its pool. Against a trash
    /// enemy it applies HP damage; against a boss part it reports laser heat or missile break (dual-track).
    /// Piercing shots pass through trash enemies (they still despawn on a boss-part hit or off-field). Cold
    /// palette per the readability rule (enemy bullets are warm).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerProjectile : MonoBehaviour
    {
        private Vector2 _velocity;
        private float _damage;      // vs trash-enemy HP
        private float _heatDelta;   // laser heat vs a boss part
        private float _breakDamage; // missile break units vs a boss part
        private bool _isMissile;
        private int _pierceRemaining; // extra trash enemies this shot can pass through before despawning (0 = none)
        private WeaponId _weaponId;
        private float _life;
        private bool _active;
        private Action<PlayerProjectile> _onDespawn;

        // ── Optional per-shot behaviours (secondary weapon identity) ──────────────────
        // M1 追蹤: steer toward the nearest enemy / boss part each frame. M4 叢集: on the shot's first hit, fire
        // an explosion callback at the hit point (the weapon controller spawns a small fragment ring there).
        private bool _homing;
        private float _homingTurnDeg;      // max turn rate (deg/s) while homing
        private Action<Vector3> _onExplode; // M4 cluster: invoked once at the hit position, then cleared
        private const float HomingScanRadius = 6.5f;
        private static readonly Collider2D[] HomingBuf = new Collider2D[24];

        /// <summary>M1: make this shot steer toward the nearest enemy/part at up to <paramref name="turnDeg"/> deg/s.</summary>
        public void EnableHoming(float turnDeg) { _homing = true; _homingTurnDeg = Mathf.Max(0f, turnDeg); }

        /// <summary>M4: on this shot's first hit, invoke <paramref name="onExplode"/> at the hit point (spawns fragments).</summary>
        public void EnableCluster(Action<Vector3> onExplode) { _onExplode = onExplode; }

        /// <summary>Heat contribution this projectile carries to a boss part (soften track).</summary>
        public float HeatDelta => _heatDelta;

        private SpriteRenderer _sr;
        private Vector3 _baseScale;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;

            _sr = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
        }

        // Cold-palette tints so the player can tell primary (laser) from secondary (missile) at a glance, and
        // even which laser type is equipped. All stay in the COLD family (enemy bullets are warm — readability rule).
        private static Color TintFor(bool isMissile, WeaponId w)
        {
            // Missiles = GREEN family (chunky), lasers = CYAN/BLUE family (slim) — a strong, unmistakable split
            // while both stay cold (enemy bullets are warm — readability rule).
            // ALL player shots stay COLD (enemy bullets are warm — the readability rule). Missiles = teal/aqua
            // (green-dominant cold), lasers = cyan/blue — a clear split while neither strays into the warm range.
            if (isMissile)
            {
                switch (w)
                {
                    case WeaponId.M2: return new Color(0.30f, 1f, 0.80f);   // swarm — bright aqua
                    case WeaponId.M3: return new Color(0.15f, 0.85f, 0.78f); // AP torpedo — teal
                    case WeaponId.M4: return new Color(0.40f, 1f, 0.92f);   // cluster — pale aqua
                    default:          return new Color(0.20f, 1f, 0.72f);   // M1 homing — spring teal
                }
            }
            switch (w)
            {
                case WeaponId.L2: return new Color(0.75f, 0.95f, 1f);       // focus — pale blue-white
                case WeaponId.L3: return new Color(0.20f, 0.85f, 1f);       // wave — sky blue
                case WeaponId.L4: return new Color(0.45f, 0.70f, 1f);       // pierce — deep blue
                default:          return new Color(0.20f, 0.97f, 1f);       // L1 spread — cyan
            }
        }

        /// <summary>
        /// Arm this projectile for flight along <paramref name="velocity"/>. Called by the pool on spawn.
        /// <paramref name="sizeScale"/> is a per-axis multiplier on the prefab's base scale so each primary
        /// weapon can express its power-up as a DIFFERENT shape — a round focus bolt that grows fatter, a wide
        /// wave pulse, a tall piercing lance — instead of every weapon just fanning out more bullets.
        /// </summary>
        public void Launch(Vector3 position, Vector2 velocity, float damage, float heatDelta, float breakDamage,
                           bool isMissile, int pierceCount, WeaponId weaponId, Vector2 sizeScale, Action<PlayerProjectile> onDespawn)
        {
            transform.position = position;
            _velocity = velocity;
            _damage = damage;
            _heatDelta = heatDelta;
            _breakDamage = breakDamage;
            _isMissile = isMissile;
            _pierceRemaining = Mathf.Max(0, pierceCount);
            _weaponId = weaponId;
            _life = 3f;
            _onDespawn = onDespawn;
            _active = true;
            _homing = false;        // opt-in per shot via EnableHoming (pooled reuse must not inherit)
            _onExplode = null;      // opt-in per shot via EnableCluster

            // Colour + size the shot so primary (laser) and secondary (missile) read differently AND so each
            // primary type's power growth has a distinct silhouette (focus = fat round, wave = wide, pierce = long
            // lance). Cold family throughout (enemy bullets are warm — readability rule).
            if (_sr != null) _sr.color = TintFor(isMissile, weaponId);
            transform.localScale = new Vector3(_baseScale.x * sizeScale.x, _baseScale.y * sizeScale.y, _baseScale.z);

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active) return;
            float dt = Time.deltaTime;
            if (_homing) SteerTowardNearest(dt);
            transform.position += (Vector3)_velocity * dt;
            _life -= dt;
            Vector3 p = transform.position;
            if (_life <= 0f || p.y > 7.5f || p.y < -8f || p.x < -6f || p.x > 6f) Despawn();
        }

        // M1 追蹤: rotate the velocity toward the nearest enemy / boss part (constant speed, capped turn rate). Uses a
        // non-alloc overlap so a handful of homing missiles cost no per-frame GC. Non-enemy colliders (player, other
        // shots, power-ups) are skipped, so the missile only ever chases valid targets.
        private void SteerTowardNearest(float dt)
        {
            Vector3 pos = transform.position;
            int n = Physics2D.OverlapCircleNonAlloc(pos, HomingScanRadius, HomingBuf);
            float best = float.MaxValue; Vector2 bestDir = Vector2.zero; bool found = false;
            for (int i = 0; i < n; i++)
            {
                var c = HomingBuf[i];
                if (c == null) continue;
                if (c.GetComponentInParent<EnemyController>() == null && c.GetComponentInParent<BossPart>() == null) continue;
                Vector2 to = (Vector2)c.transform.position - (Vector2)pos;
                float d = to.sqrMagnitude;
                if (d < best) { best = d; bestDir = to; found = true; }
            }
            if (!found || bestDir.sqrMagnitude < 1e-4f) return;
            float speed = _velocity.magnitude;
            float cur = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
            float tgt = Mathf.Atan2(bestDir.y, bestDir.x) * Mathf.Rad2Deg;
            float na = Mathf.MoveTowardsAngle(cur, tgt, _homingTurnDeg * dt) * Mathf.Deg2Rad;
            _velocity = new Vector2(Mathf.Cos(na), Mathf.Sin(na)) * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active) return;

            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                if (_pierceRemaining > 0) { _pierceRemaining--; return; } // pierce: pass through, one fewer left
                Explode();
                Despawn();
                return;
            }

            // Boss part — lasers feed the heat/soften track, missiles feed the break track (dual-track).
            var part = other.GetComponentInParent<BossPart>();
            if (part != null)
            {
                if (_isMissile) part.ReceiveMissile(_breakDamage, _weaponId);
                else part.ReceiveLaser(_heatDelta);
                Explode();
                Despawn();
            }
        }

        // M4 叢集: fire the explosion callback once at the hit point, then clear it (so a pooled reuse can't re-fire).
        private void Explode()
        {
            var cb = _onExplode;
            _onExplode = null;
            cb?.Invoke(transform.position);
        }

        private void Despawn()
        {
            if (!_active) return;
            _active = false;
            gameObject.SetActive(false);
            _onDespawn?.Invoke(this);
        }
    }
}
