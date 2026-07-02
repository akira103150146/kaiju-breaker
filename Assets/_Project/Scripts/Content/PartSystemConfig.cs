using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Part-system-exclusive tuning knobs. Contains ONLY the knobs in
    /// kaiju-part-system.md G.3 that are not owned by WeaponBalanceConfig.
    /// Heat/break capacities, theta_S, stagger_duration, and B_unsoftened_mult
    /// are all owned by <see cref="WeaponBalanceConfig"/> (single source — ADR-0003 §3).
    /// Visual timing knobs (softened_visual_onset_max_s, stagger_visual_onset_max_s)
    /// are owned by <see cref="GameFeelConfig"/> (TR-content-004).
    /// See kaiju-part-system.md G.3 and ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/PartSystemConfig", fileName = "PartSystemConfig")]
    public sealed class PartSystemConfig : ScriptableObject
    {
        [Header("Part Lifecycle")]
        [Tooltip("Parts never regenerate within a run. Must remain false — any true path is a design violation. " +
                 "kaiju-part-system.md G.3 part_regen_enabled.")]
        [SerializeField] private bool _partRegenEnabled = false;

        [Header("Chain-Break")]
        [Tooltip("M3 T3 AP chain is non-recursive. Must remain false — true would enable infinite chains (E.4). " +
                 "kaiju-part-system.md G.3 chain_break_is_recursive.")]
        [SerializeField] private bool _chainBreakIsRecursive = false;

        [Tooltip("M3 T3 chain break-fill multiplier applied to each adjacent target. Safe range [1.0, 2.0]. " +
                 "kaiju-part-system.md G.3 m3_t3_chain_dmg_mult.")]
        [SerializeField] private float _m3T3ChainDmgMult = 1.5f;

        [Tooltip("Max adjacent targets an M3 T3 chain break may propagate to. Safe range {1, 2}. " +
                 "kaiju-part-system.md G.3 m3_t3_chain_max_targets.")]
        [SerializeField] private int _m3T3ChainMaxTargets = 2;

        [Tooltip("Base break-fill (BU) applied to a chain target before the state multiplier. Safe range [5, 30]. " +
                 "kaiju-part-system.md D.6 chain_damage_base — B_chain = m3_t3_chain_dmg_mult × base × M_state_mult(target).")]
        [SerializeField] private float _m3ChainDamageBase = 10f;

        [Tooltip("L2 T3 heat-ripple: fraction of an adjacent part's H_max deposited as heat when a neighbour breaks. " +
                 "Safe range [0.20, 0.50]. Consumed by the Weapons system; owned here per kaiju-part-system.md G.3 l2_t3_adjacent_heat_pct.")]
        [SerializeField] private float _l2T3AdjacentHeatPct = 0.30f;

        [Header("Adjacency Graph")]
        [Tooltip("Maximum neighbours a single part may declare in its adjacency list. Safe range [1, 8]. " +
                 "Prevents chain effects from spreading too broadly. kaiju-part-system.md G.3.")]
        [SerializeField] private int _adjacencyMaxNeighbors = 4;

        [Header("Hitbox Size Multipliers")]
        [Tooltip("NORMAL part hitbox size relative to art bounds (1.0 = exact fit). Safe range [0.5, 2.0]. " +
                 "kaiju-part-system.md G.3 hitbox_size_multiplier_normal.")]
        [SerializeField] private float _hitboxSizeMultiplierNormal = 1.0f;

        [Tooltip("ARMORED part weak-point hitbox size relative to art bounds. Safe range [0.5, 2.0]. " +
                 "Smaller than Normal — raises the skill floor for hitting the weak point. " +
                 "kaiju-part-system.md G.3 hitbox_size_multiplier_armored.")]
        [SerializeField] private float _hitboxSizeMultiplierArmored = 0.8f;

        [Tooltip("BOSS_CORE hitbox size relative to art bounds. Safe range [0.5, 2.0]. " +
                 "Larger than Normal — makes the final target easy to aim at. " +
                 "kaiju-part-system.md G.3 hitbox_size_multiplier_core.")]
        [SerializeField] private float _hitboxSizeMultiplierCore = 1.2f;

        // ── Public read-only properties ──────────────────────────────────────

        /// <summary>
        /// Parts never regenerate within a run. Always false.
        /// kaiju-part-system.md G.3 part_regen_enabled.
        /// </summary>
        public bool PartRegenEnabled => _partRegenEnabled;

        /// <summary>
        /// M3 T3 chain is non-recursive. Always false.
        /// kaiju-part-system.md G.3 chain_break_is_recursive.
        /// </summary>
        public bool ChainBreakIsRecursive => _chainBreakIsRecursive;

        /// <summary>M3 T3 chain break-fill multiplier per target. kaiju-part-system.md G.3.</summary>
        public float M3T3ChainDmgMult => _m3T3ChainDmgMult;

        /// <summary>Max adjacent targets an M3 T3 chain may break (≤ 2). kaiju-part-system.md G.3.</summary>
        public int M3T3ChainMaxTargets => _m3T3ChainMaxTargets;

        /// <summary>Base chain break-fill (BU) before the target's state multiplier. kaiju-part-system.md D.6.</summary>
        public float M3ChainDamageBase => _m3ChainDamageBase;

        /// <summary>L2 T3 heat-ripple fraction of a neighbour's H_max (consumed by Weapons). kaiju-part-system.md G.3.</summary>
        public float L2T3AdjacentHeatPct => _l2T3AdjacentHeatPct;

        /// <summary>
        /// Max neighbours per part in the adjacency graph.
        /// kaiju-part-system.md G.3 adjacency_max_neighbors.
        /// </summary>
        public int AdjacencyMaxNeighbors => _adjacencyMaxNeighbors;

        /// <summary>Normal part hitbox multiplier. kaiju-part-system.md G.3.</summary>
        public float HitboxSizeMultiplierNormal => _hitboxSizeMultiplierNormal;

        /// <summary>Armored part weak-point hitbox multiplier. kaiju-part-system.md G.3.</summary>
        public float HitboxSizeMultiplierArmored => _hitboxSizeMultiplierArmored;

        /// <summary>Boss Core hitbox multiplier. kaiju-part-system.md G.3.</summary>
        public float HitboxSizeMultiplierCore => _hitboxSizeMultiplierCore;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_partRegenEnabled)
                Debug.LogError(
                    "[PartSystemConfig] PartRegenEnabled must always be false. " +
                    "In-run part regeneration is a design violation (kaiju-part-system.md C.7).", this);

            if (_chainBreakIsRecursive)
                Debug.LogError(
                    "[PartSystemConfig] ChainBreakIsRecursive must always be false. " +
                    "Recursive chains are a design violation (kaiju-part-system.md E.4).", this);

            if (_adjacencyMaxNeighbors < 1 || _adjacencyMaxNeighbors > 8)
                Debug.LogError(
                    $"[PartSystemConfig] AdjacencyMaxNeighbors = {_adjacencyMaxNeighbors} " +
                    "is outside safe range [1, 8].", this);

            if (_m3T3ChainDmgMult < 1.0f || _m3T3ChainDmgMult > 2.0f)
                Debug.LogError(
                    $"[PartSystemConfig] M3T3ChainDmgMult = {_m3T3ChainDmgMult} is outside safe range [1.0, 2.0].", this);

            if (_m3T3ChainMaxTargets < 1 || _m3T3ChainMaxTargets > 2)
                Debug.LogError(
                    $"[PartSystemConfig] M3T3ChainMaxTargets = {_m3T3ChainMaxTargets} must be 1 or 2 " +
                    "(GDD G.3 constrains T3 chain to ≤ 2 targets).", this);

            if (_m3ChainDamageBase < 5f || _m3ChainDamageBase > 30f)
                Debug.LogError(
                    $"[PartSystemConfig] M3ChainDamageBase = {_m3ChainDamageBase} is outside safe range [5, 30].", this);

            if (_l2T3AdjacentHeatPct < 0.20f || _l2T3AdjacentHeatPct > 0.50f)
                Debug.LogError(
                    $"[PartSystemConfig] L2T3AdjacentHeatPct = {_l2T3AdjacentHeatPct} is outside safe range [0.20, 0.50].", this);

            ValidateMultiplier("HitboxSizeMultiplierNormal",  _hitboxSizeMultiplierNormal);
            ValidateMultiplier("HitboxSizeMultiplierArmored", _hitboxSizeMultiplierArmored);
            ValidateMultiplier("HitboxSizeMultiplierCore",    _hitboxSizeMultiplierCore);
        }

        private void ValidateMultiplier(string field, float value)
        {
            if (value < 0.5f || value > 2.0f)
                Debug.LogError(
                    $"[PartSystemConfig] {field} = {value} is outside safe range [0.5, 2.0].", this);
        }
#endif
    }
}
