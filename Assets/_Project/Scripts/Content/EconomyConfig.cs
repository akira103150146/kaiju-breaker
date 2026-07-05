using System;
using KaijuBreaker.Core;
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

        [Header("Full-Clear Settlement Knobs (G.1)")]
        [Tooltip("Kaiju Essence awarded at hunt settlement when ALL breakable parts were destroyed. " +
                 "material-economy.md §G.1 essence_per_full_clear default: 1 (safe range [1, 2]). Must be >= 1.")]
        [SerializeField] private int _essencePerFullClear = 1;

        [Tooltip("Extra Common Shards awarded at hunt settlement on a full clear (on top of per-break shards). " +
                 "material-economy.md §G.1 shard_completeness_bonus default: 5 (safe range [3, 10]). Must be >= 0.")]
        [SerializeField] private int _shardCompletenessBonus = 5;

        [Header("Core Yield Knobs (G.1)")]
        [Tooltip("Kaiju-theme core count awarded when the Boss Core part is broken (Standard / Precision quality). " +
                 "Perfect quality triggers double-drop via core_perfect_double_drop flag (Economy system). " +
                 "Default: 1. NOTE: superseded by the theme model — EVERY part now yields 1 theme core " +
                 "(2 on Perfect), not just the Boss Core. Kept for back-compat; Economy no longer reads it.")]
        [SerializeField] private int _coreYieldBossCore = 1;

        [Tooltip("When TRUE, a Perfect-quality break (SOFTENED + STAGGERED) drops 2 theme cores instead of 1. " +
                 "material-economy.md §G.1 core_perfect_double_drop default: TRUE. FALSE = always 1 core.")]
        [SerializeField] private bool _corePerfectDoubleDrop = true;

        [Header("Kaiju Theme -> Core Map (D.1)")]
        [Tooltip("Core material dropped by every part of a Carapace-theme kaiju (CARAPEX). Default: CoreCarapace.")]
        [SerializeField] private MaterialId _coreForCarapace = MaterialId.CoreCarapace;

        [Tooltip("Core material dropped by every part of a Limb-theme kaiju (LACERA). Default: CoreLimb.")]
        [SerializeField] private MaterialId _coreForLimb = MaterialId.CoreLimb;

        [Tooltip("Core material dropped by every part of an Energy-theme kaiju (VOLTWYRM). Default: CoreEnergy.")]
        [SerializeField] private MaterialId _coreForEnergy = MaterialId.CoreEnergy;

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
        /// Kaiju Essence awarded at hunt settlement on a full clear (all parts broken).
        /// material-economy.md §G.1 essence_per_full_clear (default 1, safe range [1, 2]).
        /// </summary>
        public int EssencePerFullClear => _essencePerFullClear;

        /// <summary>
        /// Extra Common Shards awarded at hunt settlement on a full clear, on top of per-break shards.
        /// material-economy.md §G.1 shard_completeness_bonus (default 5, safe range [3, 10]).
        /// </summary>
        public int ShardCompletenessBonus => _shardCompletenessBonus;

        /// <summary>
        /// Kaiju-theme core yield when the Boss Core part is destroyed
        /// (Standard and Precision quality). Economy system doubles this on Perfect
        /// quality when core_perfect_double_drop is enabled.
        /// </summary>
        [Obsolete("Superseded by the theme model: every part yields 1 core (2 on Perfect via CorePerfectDoubleDrop). Economy no longer reads this.")]
        public int CoreYieldBossCore => _coreYieldBossCore;

        /// <summary>
        /// When true, a Perfect-quality break (SOFTENED + STAGGERED) yields 2 theme cores instead of 1.
        /// material-economy.md §G.1 core_perfect_double_drop (default TRUE). Standard/Precision always yield 1.
        /// </summary>
        public bool CorePerfectDoubleDrop => _corePerfectDoubleDrop;

        /// <summary>
        /// The Common-Shard yield multiplier for a break quality (material-economy.md §D.1
        /// quality_shard_mult): Normal = 1.0, Softened = <see cref="ShardYieldSoftenedMult"/>,
        /// SoftenedStaggered = <see cref="ShardYieldSoftenedStaggeredMult"/>.
        /// </summary>
        public float QualityShardMult(BreakQuality quality)
        {
            switch (quality)
            {
                case BreakQuality.Normal: return 1.0f;
                case BreakQuality.Softened: return _shardYieldSoftenedMult;
                case BreakQuality.SoftenedStaggered: return _shardYieldSoftenedStaggeredMult;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(quality), quality, "Unhandled BreakQuality in QualityShardMult.");
            }
        }

        /// <summary>
        /// The core material dropped by any part of a kaiju with the given theme
        /// (material-economy.md §D.1 kaiju_theme_to_core_map). Theme→core is the sole
        /// economy-owned mapping; the kaijuId→theme step is external (IKaijuThemeQuery).
        /// </summary>
        public MaterialId GetCoreForTheme(KaijuTheme theme)
        {
            switch (theme)
            {
                case KaijuTheme.Carapace: return _coreForCarapace;
                case KaijuTheme.Limb: return _coreForLimb;
                case KaijuTheme.Energy: return _coreForEnergy;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(theme), theme, "Unhandled KaijuTheme in GetCoreForTheme.");
            }
        }

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

            if (_essencePerFullClear < 1)
                Debug.LogError(
                    $"[EconomyConfig] '{name}': EssencePerFullClear must be >= 1. " +
                    $"Current: {_essencePerFullClear}. (material-economy.md §G.1 safe range [1, 2].)", this);

            if (_shardCompletenessBonus < 0)
                Debug.LogError(
                    $"[EconomyConfig] '{name}': ShardCompletenessBonus must be >= 0. " +
                    $"Current: {_shardCompletenessBonus}. (material-economy.md §G.1 safe range [3, 10].)", this);

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
