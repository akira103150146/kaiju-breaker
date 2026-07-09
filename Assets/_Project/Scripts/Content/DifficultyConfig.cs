using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Difficulty scaling knobs. Single source of truth for all D1–D4 multipliers
    /// and UI behaviour settings. No other SO may duplicate these values (ADR-0003 §3,
    /// TR-content-004). StageDef (Story 006) reads this SO via ContentRegistry.
    /// See difficulty-system.md G.1, G.2 and ADR-0003.
    ///
    /// Array indexing: index 0 = D1, 1 = D2, 2 = D3, 3 = D4.
    /// Usage example: EnemyCountMult[(int)DifficultyTier.D3]  // = index 2
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/DifficultyConfig", fileName = "DifficultyConfig")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        [Header("Enemy Count Multipliers — D1..D4 (index 0–3)")]
        [Tooltip("Per-wave enemy count multipliers for D1–D4. Length must be exactly 4. " +
                 "Index 0 (D1) must equal 1.0 (baseline). " +
                 "Formula: actual_count = ceil(base_count × EnemyCountMult[tier]). " +
                 "difficulty-system.md G.1.")]
        [SerializeField] private float[] _enemyCountMult = { 1.00f, 1.25f, 1.50f, 1.75f };

        [Header("Bullet Density Multipliers — D1..D4 (index 0–3)")]
        [Tooltip("Per-shot bullet count multipliers for D1–D4. Length must be exactly 4. " +
                 "Index 0 (D1) must equal 1.0 (baseline). COUNT-FIRST difficulty model (director 2026-07): the " +
                 "emitter base counts ARE the sparse D1 pattern (aimed ≈ 1 bullet), and difficulty scales the " +
                 "per-volley count UP steeply (D4 = 4× the sparse base) — the pattern shape never changes. " +
                 "Formula: actual_bullets = ceil(base_bullets × BulletDensityMult[tier]). difficulty-system.md G.1.")]
        [SerializeField] private float[] _bulletDensityMult = { 1.00f, 2.00f, 3.00f, 4.00f };

        [Header("UI Behaviour")]
        [Tooltip("Difficulty shown on first game launch. Must be D1 (D1 Promise — difficulty-system.md C.6).")]
        [SerializeField] private DifficultyTier _defaultDifficultyOnFirstLaunch = DifficultyTier.D1;

        [Tooltip("After a run ends, pre-fill the next run's difficulty selection with the last used tier. " +
                 "difficulty-system.md G.2 remember_last_difficulty.")]
        [SerializeField] private bool _rememberLastDifficulty = true;

        [Tooltip("Allow the player to change difficulty mid-run. Must remain false — see difficulty-system.md E.1.")]
        [SerializeField] private bool _midRunDifficultyChangeAllowed = false;

        [Tooltip("Maximum simultaneous enemies on screen. Caps D.1 formula output. Safe range [1, 50]. " +
                 "difficulty-system.md G.2 enemy_cap_per_scene.")]
        [SerializeField] private int _enemyCapPerScene = 20;

        // ── Public read-only properties ──────────────────────────────────────

        /// <summary>
        /// Enemy count multiplier per difficulty tier (length 4, index 0 = D1).
        /// Access: EnemyCountMult[(int)tier]. difficulty-system.md G.1.
        /// </summary>
        public float[] EnemyCountMult => _enemyCountMult;

        /// <summary>
        /// Bullet density multiplier per difficulty tier (length 4, index 0 = D1).
        /// Access: BulletDensityMult[(int)tier]. difficulty-system.md G.1.
        /// </summary>
        public float[] BulletDensityMult => _bulletDensityMult;

        /// <summary>Default difficulty on first game launch. Always D1. difficulty-system.md G.2.</summary>
        public DifficultyTier DefaultDifficultyOnFirstLaunch => _defaultDifficultyOnFirstLaunch;

        /// <summary>Pre-fill next run's difficulty with last used tier. difficulty-system.md G.2.</summary>
        public bool RememberLastDifficulty => _rememberLastDifficulty;

        /// <summary>
        /// Whether difficulty may change mid-run. Always false. difficulty-system.md G.2, E.1.
        /// </summary>
        public bool MidRunDifficultyChangeAllowed => _midRunDifficultyChangeAllowed;

        /// <summary>
        /// Hard cap on simultaneous enemies per scene. difficulty-system.md G.2 enemy_cap_per_scene.
        /// </summary>
        public int EnemyCapPerScene => _enemyCapPerScene;

        // ── Convenience indexer ───────────────────────────────────────────────

        /// <summary>
        /// Returns the enemy count multiplier for <paramref name="tier"/>.
        /// Equivalent to EnemyCountMult[(int)tier].
        /// </summary>
        public float GetEnemyCountMult(DifficultyTier tier) =>
            _enemyCountMult != null && _enemyCountMult.Length == 4
                ? _enemyCountMult[(int)tier]
                : 1f;

        /// <summary>
        /// Returns the bullet density multiplier for <paramref name="tier"/>.
        /// Equivalent to BulletDensityMult[(int)tier].
        /// </summary>
        public float GetBulletDensityMult(DifficultyTier tier) =>
            _bulletDensityMult != null && _bulletDensityMult.Length == 4
                ? _bulletDensityMult[(int)tier]
                : 1f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateArray("EnemyCountMult",  _enemyCountMult);
            ValidateArray("BulletDensityMult", _bulletDensityMult);

            if (_enemyCountMult != null && _enemyCountMult.Length == 4 && _enemyCountMult[0] != 1.0f)
                Debug.LogError(
                    $"[DifficultyConfig] EnemyCountMult[0] (D1 baseline) must equal 1.0, " +
                    $"but is {_enemyCountMult[0]}. D1 is the unscaled reference — do not reduce it.", this);

            if (_bulletDensityMult != null && _bulletDensityMult.Length == 4 && _bulletDensityMult[0] != 1.0f)
                Debug.LogError(
                    $"[DifficultyConfig] BulletDensityMult[0] (D1 baseline) must equal 1.0, " +
                    $"but is {_bulletDensityMult[0]}. D1 is the unscaled reference — do not reduce it.", this);

            // D2–D4 密度乘數安全範圍（difficulty-system.md §G.1）— 越界為警告，不阻斷：
            // 值仍會載入，但設計師應確認是有意的調整。D1 是上方的硬閘門（LogError）。
            if (_enemyCountMult != null && _enemyCountMult.Length == 4)
            {
                WarnRange("EnemyCountMult[D2]", _enemyCountMult[1], 1.10f, 1.50f);
                WarnRange("EnemyCountMult[D3]", _enemyCountMult[2], 1.25f, 1.75f);
                WarnRange("EnemyCountMult[D4]", _enemyCountMult[3], 1.50f, 2.00f);
            }
            if (_bulletDensityMult != null && _bulletDensityMult.Length == 4)
            {
                // Bands widened for the count-first difficulty model: emitter base counts are now the D1 (sparse,
                // ~1-bullet) baseline and density scales UP steeply per tier (design: 難度只改單發子彈數量).
                WarnRange("BulletDensityMult[D2]", _bulletDensityMult[1], 1.50f, 2.50f);
                WarnRange("BulletDensityMult[D3]", _bulletDensityMult[2], 2.00f, 3.50f);
                WarnRange("BulletDensityMult[D4]", _bulletDensityMult[3], 2.50f, 5.00f);
            }

            if (_defaultDifficultyOnFirstLaunch != DifficultyTier.D1)
                Debug.LogError(
                    "[DifficultyConfig] DefaultDifficultyOnFirstLaunch must be D1 " +
                    "(D1 Promise — difficulty-system.md C.6).", this);

            if (_midRunDifficultyChangeAllowed)
                Debug.LogError(
                    "[DifficultyConfig] MidRunDifficultyChangeAllowed must remain false " +
                    "(difficulty-system.md E.1 — mid-run change is a design violation).", this);

            if (_enemyCapPerScene < 1 || _enemyCapPerScene > 50)
                Debug.LogError(
                    $"[DifficultyConfig] EnemyCapPerScene = {_enemyCapPerScene} " +
                    "is outside safe range [1, 50].", this);
        }

        private void ValidateArray(string name, float[] arr)
        {
            if (arr == null || arr.Length != 4)
                Debug.LogError(
                    $"[DifficultyConfig] {name} must have exactly 4 elements (D1–D4). " +
                    $"Current length: {arr?.Length ?? 0}.", this);
        }

        /// <summary>
        /// Editor-time warning when a D2–D4 multiplier falls outside its difficulty-system.md §G.1
        /// safe band. Non-blocking (LogWarning): the value still loads, but the designer should
        /// confirm the deviation is intentional. D1 baselines are hard gates (LogError) elsewhere.
        /// </summary>
        private void WarnRange(string name, float value, float lo, float hi)
        {
            if (value < lo || value > hi)
                Debug.LogWarning(
                    $"[DifficultyConfig] {name} = {value} is outside the difficulty-system.md §G.1 " +
                    $"safe range [{lo}, {hi}]. Confirm this is intentional.", this);
        }
#endif
    }
}
