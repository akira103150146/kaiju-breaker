using System.Collections.Generic;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// A simple grow-on-demand pool for <see cref="EnemyBullet"/>s (no per-shot Instantiate/Destroy churn — the
    /// control-manifest pooling guardrail). One pool serves every enemy emitter in the run; the composition/
    /// scene director owns it and hands it to the wave path so enemies never load or construct bullets
    /// themselves. Reused bullets are deactivated in the hierarchy and re-armed via <see cref="Spawn"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyBulletPool : MonoBehaviour
    {
        [Tooltip("Pooled enemy bullet prefab (EnemyBullet + trigger collider + kinematic Rigidbody2D). Required.")]
        [SerializeField] private EnemyBullet _bulletPrefab;

        private readonly List<EnemyBullet> _pool = new List<EnemyBullet>();

        /// <summary>Assign the bullet prefab at runtime (when the pool GameObject is created by the scene director).</summary>
        public void Configure(EnemyBullet bulletPrefab) => _bulletPrefab = bulletPrefab;

        /// <summary>Spawn one bullet travelling at <paramref name="velocity"/>, tinted <paramref name="tint"/>. No-op (null) if no prefab.</summary>
        public EnemyBullet Spawn(Vector2 position, Vector2 velocity, float damage, float lifetime, Color tint)
        {
            if (_bulletPrefab == null) return null;
            var b = Rent();
            b.Launch(position, velocity, damage, lifetime, tint, Return);
            return b;
        }

        private EnemyBullet Rent()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (!_pool[i].gameObject.activeSelf) return _pool[i];

            var created = Instantiate(_bulletPrefab, transform);
            created.gameObject.SetActive(false);
            _pool.Add(created);
            return created;
        }

        private void Return(EnemyBullet b) { /* deactivated by the bullet; reused on next Rent */ }
    }
}
