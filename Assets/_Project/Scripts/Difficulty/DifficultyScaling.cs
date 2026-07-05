using UnityEngine;

namespace KaijuBreaker.Difficulty
{
    /// <summary>
    /// The single home for the difficulty density-scaling formulas (difficulty-system.md §D.1/§D.2).
    /// Stage calls <see cref="ScaledEnemyCount"/> before each wave; the BulletSim Mono bridge calls
    /// <see cref="ScaledBulletCount"/> before each enemy shot. Both take the multiplier as a plain
    /// <see cref="float"/> (read from <see cref="KaijuBreaker.Core.IDifficultyProvider"/>) so no consumer
    /// re-implements the ceil/cap math and no difficulty VALUE is duplicated (ADR-0003 single source).
    ///
    /// Pure static math — deterministic, no state, EditMode-testable, and safe to call across the
    /// DOTS↔Mono boundary (value-typed args only, control-manifest §3 BulletSim).
    /// </summary>
    public static class DifficultyScaling
    {
        /// <summary>
        /// Difficulty-scaled enemy count for a wave: <c>min(ceil(baseCount × mult), cap)</c>
        /// (difficulty-system.md §D.1). Ceil ensures a ≥1.0 multiplier never rounds a real wave DOWN;
        /// the per-scene cap bounds on-screen density. A <paramref name="baseCount"/> of 0 stays 0 —
        /// an empty wave spawns nothing (we never fabricate a phantom enemy).
        /// </summary>
        /// <param name="baseCount">Design-authored base enemy count for the wave (≥0).</param>
        /// <param name="mult">Enemy-count multiplier for the active tier (D1 = 1.0).</param>
        /// <param name="cap">Hard on-screen enemy cap (difficulty-system.md §G.2 enemy_cap_per_scene).</param>
        public static int ScaledEnemyCount(int baseCount, float mult, int cap)
        {
            if (baseCount <= 0) return 0;
            int scaled = Mathf.CeilToInt(baseCount * mult);
            return Mathf.Min(scaled, cap);
        }

        /// <summary>
        /// Difficulty-scaled bullet count for one enemy shot: <c>ceil(baseBullets × mult)</c>
        /// (difficulty-system.md §D.2). Density scales ONLY — bullet speed and shape are tier-invariant
        /// (§C.2, MUST NOT scale). No cap here; on-screen bullet limits are a BulletSim concern.
        /// A <paramref name="baseBullets"/> of 0 stays 0.
        /// </summary>
        /// <param name="baseBullets">Design-authored base bullet count for the shot (≥0).</param>
        /// <param name="mult">Bullet-density multiplier for the active tier (D1 = 1.0).</param>
        public static int ScaledBulletCount(int baseBullets, float mult)
        {
            if (baseBullets <= 0) return 0;
            return Mathf.CeilToInt(baseBullets * mult);
        }
    }
}
