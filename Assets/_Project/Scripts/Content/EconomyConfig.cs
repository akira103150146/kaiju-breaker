using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Global tuning knobs for the material economy: Common Shard yield multipliers,
    /// elite shard bonus, kaiju-core yield from Boss Core parts, and weapon upgrade
    /// shard cost skeleton.
    /// <para>
    /// <b>Single source of truth</b> for all economy balance parameters (ADR-0003 §3).
    /// Difficulty does NOT affect material yield — see material-economy.md §F.3 and
    /// the pillar 「難度是門，不是牆」.
    /// </para>
    /// <para>
    /// <b>Upgrade cost note</b>: <see cref="WeaponUpgradeCostT1ToT2"/> and
    /// <see cref="WeaponUpgradeCostT2ToT3"/> represent the Common Shard cost component only.
    /// Kaiju-core and Essence costs for each tier transition are looked up by the
    /// Economy system from material-economy.md §C.4 data, not duplicated here.
    /// </para>
    /// <para>
    /// <b>Pure static data container</b> — no runtime economy logic.
    /// </para>
    /// See material-economy.md §G.1, §G.2, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/EconomyConfig", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Header("Shard Yield Knobs (G.1)")]
        [Tooltip("Base Common Shards awarded per part break, before break-quality multiplier. " +
                 "material-economy.md §G.1/D.3 shard_base = 2 (safe range [1, 4]). Must be >= 1.")]
        [SerializeField] private int _shardYieldBase = 2;

        [Tooltip("Shard multiplier for Precision break quality (SOFTENED state at break time). " +
                 "material-economy.md §G.1 shard_precision_mult default: 1.5. Must be >= 1.0.")]
        [SerializeField] private float _shardYieldSoftenedMult = 1.5f;

        [Tooltip("Shard multiplier for Perfect break quality (SOFTENED + STAGGERED state at break time). " +
                 "material-economy.md §G.1 shard_perfect_mult default: 2.0. " +
                 "Must be >= ShardYieldSoftenedMult.")]
        [SerializeField] private float _shardYieldSoftenedStaggeredMult = 2.0f;

        [Tooltip("Extra Common Shards awarded when an elite enemy is killed, on top of normal shard yield. " +
                 "Per-enemy override lives in EnemyDef.EliteShardBonus; this is the global fallback / reference. " +
                 "material-economy.md §E.3 elite_shard_bonus default: 3.")]
        [SerializeField] private int _eliteShardBonus = 3;

        [Header("Core Yield Knobs (G.1)")]
        [Tooltip("Kaiju-theme core count awarded when the Boss Core part is broken (Standard / Precision quality). " +
                 "Perfect quality triggers double-drop via core_perfect_double_drop flag (Economy system). " +
                 "Default: 1.")]
        [SerializeField] private int _coreYieldBossCore = 1;

        [Header("Upgrade Cost Skeleton (G.2)")]
        [Tooltip("Common Shard cost to upgrade a weapon from Tier 1 to Tier 2. " +
                 "material-economy.md §C.4: 12 shards. Set to 0 to emit a LogWarning (pending confirmation).")]
        [SerializeField] private int _weaponUpgradeCostT1ToT2 = 12;

        [Tooltip("Common Shard cost to upgrade a weapon from Tier 2 to Tier 3. " +
                 "material-economy.md §C.4: 25 shards. Set to 0 to emit a LogWarning (pending confirmation).")]
        [SerializeField] private int _weaponUpgradeCostT2ToT3 = 25;

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>
        /// Base Common Shard count per part break (before break-quality multiplier).
        /// material-economy.md §G.1/D.3 shard_base. Default 2.
        /// </summary>
        public int ShardYieldBase => _shardYieldBase;

        /// <summary>
        /// Shard multiplier for Precision (SOFTENED) break quality.
        /// <c>shard_yield = floor(ShardYieldBase × ShardYieldSoftenedMult)</c>.
        /// </summary>
        public float ShardYieldSoftenedMult => _shardYieldSoftenedMult;

        /// <summary>
        /// Shard multiplier for Perfect (SOFTENED + STAGGERED) break quality.
        /// Guaranteed >= <see cref="ShardYieldSoftenedMult"/> by OnValidate.
        /// </summary>
        public float ShardYieldSoftenedStaggeredMult => _shardYieldSoftenedStaggeredMult;

        /// <summary>
        /// Extra Common Shards dropped on elite enemy kill (global reference default).
        /// Per-enemy EnemyDef.EliteShardBonus may override this in the Economy system.
        /// </summary>
        public int EliteShardBonus => _eliteShardBonus;

        /// <summary>
        /// Kaiju-theme core yield when the Boss Core part is destroyed
        /// (Standard and Precision quality). Economy system doubles this on Perfect
        /// quality when core_perfect_double_drop is enabled.
        /// </summary>
        public int CoreYieldBossCore => _coreYieldBossCore;

        /// <summary>
        /// Common Shard cost component for Tier 1 → Tier 2 weapon upgrade.
        /// material-economy.md §C.4 value: 12. Kaiju-core cost (5) is Economy-system data.
        /// </summary>
        public int WeaponUpgradeCostT1ToT2 => _weaponUpgradeCostT1ToT2;

        /// <summary>
        /// Common Shard cost component for Tier 2 → Tier 3 weapon upgrade.
        /// material-economy.md §C.4 value: 25. Core (8) and Essence (1) costs are Economy-system data.
        /// </summary>
        public int WeaponUpgradeCostT2ToT3 => _weaponUpgradeCostT2ToT3;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_shardYieldBase < 1)
                Debug.LogError(
                    $"[EconomyConfig] '{name}': ShardYieldBase must be >= 1. " +
                    $"Current: {_shardYieldBase}.", this);

            if (_shardYieldSoftenedMult < 1.0f)
                Debug.LogError(
                    $"[EconomyConfig] '{name}': ShardYieldSoftenedMult must be >= 1.0. " +
                    $"Current: {_shardYieldSoftenedMult}.", this);

            if (_shardYieldSoftenedStaggeredMult < _shardYieldSoftenedMult)
                Debug.LogError(
                    $"[EconomyConfig] '{name}': ShardYieldSoftenedStaggeredMult ({_shardYieldSoftenedStaggeredMult}) " +
                    $"must not be less than ShardYieldSoftenedMult ({_shardYieldSoftenedMult}). " +
                    $"Stacked quality multiplier cannot be lower than the base quality multiplier.", this);

            // Warn (not error) for zero costs — indicates values are pending GDD confirmation.
            if (_weaponUpgradeCostT1ToT2 == 0)
                Debug.LogWarning(
                    $"[EconomyConfig] '{name}': WeaponUpgradeCostT1ToT2 is 0 — " +
                    $"pending confirmation from material-economy.md §C.4. Expected value: 12.", this);

            if (_weaponUpgradeCostT2ToT3 == 0)
                Debug.LogWarning(
                    $"[EconomyConfig] '{name}': WeaponUpgradeCostT2ToT3 is 0 — " +
                    $"pending confirmation from material-economy.md §C.4. Expected value: 25.", this);
        }
    }
}
