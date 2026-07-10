using System;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// A pooled enemy bullet: kinematic constant-velocity travel with trigger overlap, returned to its pool on
    /// lifetime/expiry or when it leaves the field. Enemy bullets are the MonoBehaviour side of the hybrid
    /// bullet backend (ADR-0001) — kinematic movement + trigger overlap, no rigidbody simulation — so the
    /// diverse patterns run now without waiting on the DOTS BulletSim perf gate. The player detects the hit
    /// (App sees Stage) and calls <see cref="Despawn"/>; bullets never touch player state themselves. Warm
    /// palette by rule (player shots are cold) so incoming fire always reads as a threat.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyBullet : MonoBehaviour
    {
        private Vector2 _velocity;
        private float _damage;
        private float _life;
        private bool _active;
        private Action<EnemyBullet> _onDespawn;
        private SpriteRenderer _sr;

        /// <summary>Contact damage this bullet deals to the player.</summary>
        public float Damage => _damage;

        /// <summary>True while in flight.</summary>
        public bool IsActive => _active;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;
            _sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Arm the bullet for flight. Called by <see cref="EnemyBulletPool"/> on spawn. <paramref name="tint"/>
        /// lets each emitter shape fire a differently-coloured bullet (all in the warm threat palette) so the
        /// player can read which attack is which at a glance.
        /// </summary>
        public void Launch(Vector2 position, Vector2 velocity, float damage, float lifetime, Color tint,
                           Action<EnemyBullet> onDespawn)
        {
            transform.position = position;
            _velocity = velocity;
            _damage = damage;
            _life = lifetime;
            _onDespawn = onDespawn;
            _active = true;
            if (_sr != null) _sr.color = tint;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active) return;
            float dt = Time.deltaTime;
            transform.position += (Vector3)_velocity * dt;
            _life -= dt;
            Vector3 p = transform.position;
            if (_life <= 0f || p.y < -8f || p.y > 8f || p.x < -6f || p.x > 6f) Despawn();
        }

        /// <summary>Deactivate and return to the pool (called on player hit or off-field).</summary>
        public void Despawn()
        {
            if (!_active) return;
            _active = false;
            gameObject.SetActive(false);
            _onDespawn?.Invoke(this);
        }
    }
}
