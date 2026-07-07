using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Shared, data-driven HP baseline for trash/elite enemies, keyed by <see cref="HpTier"/>
    /// (enemy-tier-system.md §C, bullet-pattern-diversity.md — <c>HP_base[hp_tier]</c>). Trash enemies
    /// carry no part system, so their effective HP is <c>HP_base[tier] × (isElite ? EnemyDef.EliteHpMult : 1)</c>.
    /// Keeping the tier baselines in one SO (ADR-0003) means a designer retunes the whole roster's toughness
    /// from a single place instead of per-enemy magic numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/EnemyTierBalanceConfig", fileName = "EnemyTierBalanceConfig")]
    public sealed class EnemyTierBalanceConfig : ScriptableObject
    {
        [Tooltip("Base HP for an HpTier.T1 (light) trash enemy — killed by a few L1 hits. Frozen default: 30.")]
        [SerializeField] private int _hpBaseT1 = 30;

        [Tooltip("Base HP for an HpTier.T2 (medium) trash enemy. Frozen default: 70.")]
        [SerializeField] private int _hpBaseT2 = 70;

        /// <summary>Base HP for a T1 (light) trash enemy before any elite multiplier.</summary>
        public int HpBaseT1 => _hpBaseT1;

        /// <summary>Base HP for a T2 (medium) trash enemy before any elite multiplier.</summary>
        public int HpBaseT2 => _hpBaseT2;

        /// <summary>Resolve the base HP for a tier (before the elite multiplier).</summary>
        public int BaseHpFor(HpTier tier) => tier == HpTier.T2 ? _hpBaseT2 : _hpBaseT1;

        private void OnValidate()
        {
            if (_hpBaseT1 < 1) Debug.LogError($"[EnemyTierBalanceConfig] '{name}': HpBaseT1 must be >= 1.", this);
            if (_hpBaseT2 < 1) Debug.LogError($"[EnemyTierBalanceConfig] '{name}': HpBaseT2 must be >= 1.", this);
        }
    }
}
