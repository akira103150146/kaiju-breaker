using System;
using KaijuBreaker.Stage;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// A pooled player projectile: kinematic straight-up travel, trigger-overlap hit resolution, and a despawn
    /// callback back to its pool (<see cref="PlayerWeaponController"/>). Against a trash enemy it applies HP
    /// damage; against a boss part it reports the hit so the weapon can publish the real <c>LaserHit</c> /
    /// <c>MissileHit</c> event (boss wiring, Phase D). Bullets are cold-palette per the readability rule
    /// (enemy bullets are warm) so the player's own shots never read as incoming fire.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerProjectile : MonoBehaviour
    {
        private float _speed;
        private float _damage;
        private float _heatDelta;
        private float _life;
        private float _cullY;
        private bool _active;
        private Action<PlayerProjectile> _onDespawn;

        /// <summary>Heat contribution this projectile carries to a boss part (soften track). Read in Phase D.</summary>
        public float HeatDelta => _heatDelta;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true; // fire triggers against kinematic enemies
        }

        /// <summary>Arm this projectile for flight. Called by the pool on spawn.</summary>
        public void Launch(Vector3 position, float speed, float damage, float heatDelta, float cullY,
                           Action<PlayerProjectile> onDespawn)
        {
            transform.position = position;
            _speed = speed;
            _damage = damage;
            _heatDelta = heatDelta;
            _cullY = cullY;
            _life = 4f;
            _onDespawn = onDespawn;
            _active = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active) return;
            float dt = Time.deltaTime;
            transform.position += Vector3.up * (_speed * dt);
            _life -= dt;
            if (_life <= 0f || transform.position.y > _cullY) Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active) return;
            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                Despawn();
            }
            // Boss-part branch (publish LaserHit via the weapon) is added in Phase D.
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
