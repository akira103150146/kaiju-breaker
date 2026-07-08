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
        private bool _pierce;
        private WeaponId _weaponId;
        private float _life;
        private bool _active;
        private Action<PlayerProjectile> _onDespawn;

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
            if (isMissile)
            {
                switch (w)
                {
                    case WeaponId.M2: return new Color(0.55f, 0.85f, 1f);   // swarm — pale sky
                    case WeaponId.M3: return new Color(0.75f, 0.80f, 1f);   // AP torpedo — steel violet-blue
                    case WeaponId.M4: return new Color(0.60f, 0.70f, 1f);   // cluster — indigo-white
                    default:          return new Color(0.68f, 0.80f, 1f);   // M1 homing — blue-white
                }
            }
            switch (w)
            {
                case WeaponId.L2: return new Color(0.85f, 1f, 1f);          // focus — near-white hot
                case WeaponId.L3: return new Color(0.30f, 1f, 0.85f);       // wave — teal
                case WeaponId.L4: return new Color(0.55f, 0.90f, 1f);       // pierce — bright azure
                default:          return new Color(0.25f, 0.97f, 1f);       // L1 spread — cyan
            }
        }

        /// <summary>Arm this projectile for flight along <paramref name="velocity"/>. Called by the pool on spawn.</summary>
        public void Launch(Vector3 position, Vector2 velocity, float damage, float heatDelta, float breakDamage,
                           bool isMissile, bool pierce, WeaponId weaponId, Action<PlayerProjectile> onDespawn)
        {
            transform.position = position;
            _velocity = velocity;
            _damage = damage;
            _heatDelta = heatDelta;
            _breakDamage = breakDamage;
            _isMissile = isMissile;
            _pierce = pierce;
            _weaponId = weaponId;
            _life = 3f;
            _onDespawn = onDespawn;
            _active = true;

            // Colour + size the shot so primary (laser) and secondary (missile) read differently: missiles are
            // chunkier and blue-white, lasers slimmer and cyan/teal (cold family — enemy bullets are warm).
            if (_sr != null) _sr.color = TintFor(isMissile, weaponId);
            transform.localScale = isMissile ? _baseScale * 1.5f : _baseScale;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active) return;
            float dt = Time.deltaTime;
            transform.position += (Vector3)_velocity * dt;
            _life -= dt;
            Vector3 p = transform.position;
            if (_life <= 0f || p.y > 7.5f || p.y < -8f || p.x < -6f || p.x > 6f) Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active) return;

            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                if (!_pierce) Despawn(); // piercing shots continue through trash
                return;
            }

            // Boss part — lasers feed the heat/soften track, missiles feed the break track (dual-track).
            var part = other.GetComponentInParent<BossPart>();
            if (part != null)
            {
                if (_isMissile) part.ReceiveMissile(_breakDamage, _weaponId);
                else part.ReceiveLaser(_heatDelta);
                Despawn();
            }
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
