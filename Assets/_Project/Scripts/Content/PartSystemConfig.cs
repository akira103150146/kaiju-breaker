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
