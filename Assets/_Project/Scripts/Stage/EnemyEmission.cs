using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Turns an <see cref="EmitterPatternType"/> volley into concrete per-bullet velocity vectors — the frozen
    /// firing shapes (Aimed fan / Linear downward wall / Radial ring / RingBurst death-ring), bullet-system.md
    /// §4.2. Pure math (no Unity objects, no spawning) so the pattern geometry is EditMode-testable. Speed is
    /// passed in already converted to world units and is NEVER scaled by difficulty (§4.4 — only bullet COUNT
    /// scales); the emitter aims at fire time only (non-tracking readability rule).
    /// </summary>
    public static class EnemyEmission
    {
        /// <summary>
        /// Velocities for one volley of <paramref name="count"/> bullets. <paramref name="aimDir"/> is the
        /// (unnormalised) direction to the player for <see cref="EmitterPatternType.Aimed"/>; ignored by the
        /// fixed-direction and radial shapes.
        /// </summary>
        public static Vector2[] Velocities(EmitterPatternType type, int count, float spreadDeg, float speed, Vector2 aimDir,
                                           float spinPhaseDeg = 0f)
        {
            count = Mathf.Max(1, count);
            switch (type)
            {
                case EmitterPatternType.Aimed:
                    return Fan(AngleDeg(aimDir), spreadDeg, count, speed);
                case EmitterPatternType.Linear:
                    return Fan(-90f, spreadDeg, count, speed); // -90° = straight down (a fixed wall)
                case EmitterPatternType.Spiral:
                    // Rotating radial arms: a ring whose start angle advances by the caller-accumulated phase.
                    return Ring(count, speed, spinPhaseDeg);
                case EmitterPatternType.Radial:
                case EmitterPatternType.RingBurst:
                default:
                    return Ring(count, speed);
            }
        }

        // Evenly split a fan of total width spreadDeg centred on centreDeg; count==1 fires dead-centre.
        private static Vector2[] Fan(float centreDeg, float spreadDeg, int count, float speed)
        {
            var result = new Vector2[count];
            if (count == 1) { result[0] = Dir(centreDeg) * speed; return result; }
            float start = centreDeg - spreadDeg * 0.5f;
            float step = spreadDeg / (count - 1);
            for (int i = 0; i < count; i++) result[i] = Dir(start + i * step) * speed;
            return result;
        }

        // Evenly spaced full-circle ring, optionally rotated by startDeg (Spiral uses the accumulated phase).
        private static Vector2[] Ring(int count, float speed, float startDeg = 0f)
        {
            var result = new Vector2[count];
            float step = 360f / count;
            for (int i = 0; i < count; i++) result[i] = Dir(startDeg + i * step) * speed;
            return result;
        }

        private static Vector2 Dir(float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        private static float AngleDeg(Vector2 v)
        {
            if (v.sqrMagnitude < 1e-6f) return -90f; // degenerate → straight down
            return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        }
    }

    /// <summary>
    /// Run-scoped combat context handed to each spawned enemy: the shared bullet pool it fires from and the
    /// player transform its aimed shots target. Injected through the wave path (no singletons — ADR-0005).
    /// </summary>
    public sealed class EnemyCombatContext
    {
        public readonly EnemyBulletPool BulletPool;
        public readonly Transform PlayerTarget;
        /// <summary>Invoked when an enemy is killed by damage: (worldPosition, isElite) — the scene rolls drops.</summary>
        public readonly System.Action<Vector3, bool> OnEnemyKilled;
        /// <summary>Difficulty bullet-density multiplier for the run (D1 = 1.0). Scales each mob's shot count.</summary>
        public readonly float BulletDensityMult;

        public EnemyCombatContext(EnemyBulletPool bulletPool, Transform playerTarget,
                                  System.Action<Vector3, bool> onEnemyKilled = null, float bulletDensityMult = 1f)
        {
            BulletPool = bulletPool;
            PlayerTarget = playerTarget;
            OnEnemyKilled = onEnemyKilled;
            BulletDensityMult = bulletDensityMult > 0f ? bulletDensityMult : 1f;
        }
    }
}
