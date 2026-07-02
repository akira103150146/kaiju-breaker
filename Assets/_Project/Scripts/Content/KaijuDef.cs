using System;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Data for a single breakable part within a kaiju. Embedded directly in
    /// <see cref="KaijuDef"/> because part data shares the kaiju's asset lifecycle.
    /// H_max / B_max overrides use a value + bool pair because Unity Inspector
    /// does not natively support nullable floats. See kaiju-part-system.md C.6.
    /// </summary>
    [Serializable]
    public sealed class PartDef
    {
        [Tooltip("Unique identifier for this part within the kaiju (e.g. 'left_wing'). " +
                 "Must be non-empty and unique within this KaijuDef. kaiju-part-system.md C.1.")]
        [SerializeField] private string _partId = string.Empty;

        [Tooltip("Part classification: Normal / Armored / BossCore. Governs armor gating and win condition. " +
                 "kaiju-part-system.md C.3.")]
        [SerializeField] private PartType _partType = PartType.Normal;

        [Header("H_max Override (optional)")]
        [Tooltip("When HMaxUseOverride is true, use this value instead of WeaponBalanceConfig.HMaxNormal/Armored/BossCore. " +
                 "kaiju-part-system.md C.3 per-kaiju override.")]
        [SerializeField] private float _hMaxOverride = 0f;

        [Tooltip("Set true to apply HMaxOverride for this part instead of the global default.")]
        [SerializeField] private bool _hMaxUseOverride = false;

        [Header("B_max Override (optional)")]
        [Tooltip("When BMaxUseOverride is true, use this value instead of WeaponBalanceConfig.BMaxNormal/Armored/BossCore. " +
                 "kaiju-part-system.md C.3 per-kaiju override.")]
        [SerializeField] private float _bMaxOverride = 0f;

        [Tooltip("Set true to apply BMaxOverride for this part instead of the global default.")]
        [SerializeField] private bool _bMaxUseOverride = false;

        [Header("Graph & Drops")]
        [Tooltip("IDs of spatially adjacent parts. Adjacency is declared one-way and resolved to a " +
                 "bidirectional graph at load time. See kaiju-part-system.md C.6.")]
        [SerializeField] private string[] _adjacency = Array.Empty<string>();

        [Tooltip("Drop table ID passed to material-economy on part break. " +
                 "Must match a key in the material economy drop table registry. " +
                 "kaiju-part-system.md C.5 on_part_break.drop_table_id.")]
        [SerializeField] private string _dropTableId = string.Empty;

        // ── Public read-only properties ──────────────────────────────────────

        /// <summary>Unique part identifier within this kaiju. kaiju-part-system.md C.1.</summary>
        public string PartId => _partId;

        /// <summary>Part classification (Normal / Armored / BossCore). kaiju-part-system.md C.3.</summary>
        public PartType PartType => _partType;

        /// <summary>Per-part H_max override value (HU). Only used when HMaxUseOverride is true.</summary>
        public float HMaxOverride => _hMaxOverride;

        /// <summary>True if this part uses HMaxOverride instead of the global WeaponBalanceConfig default.</summary>
        public bool HMaxUseOverride => _hMaxUseOverride;

        /// <summary>Per-part B_max override value (BU). Only used when BMaxUseOverride is true.</summary>
        public float BMaxOverride => _bMaxOverride;

        /// <summary>True if this part uses BMaxOverride instead of the global WeaponBalanceConfig default.</summary>
        public bool BMaxUseOverride => _bMaxUseOverride;

        /// <summary>
        /// Adjacent part IDs (declared one-way; bidirectional graph built at load time).
        /// kaiju-part-system.md C.6.
        /// </summary>
        public string[] Adjacency => _adjacency;

        /// <summary>
        /// Drop table ID forwarded in the on_part_break event to material-economy.
        /// kaiju-part-system.md C.5.
        /// </summary>
        public string DropTableId => _dropTableId;
    }

    /// <summary>
    /// Kaiju definition: identity and the complete set of breakable parts with
    /// their adjacency graph declarations and optional H/B overrides.
    /// PartDef[] is serialised inline — parts share the kaiju asset lifecycle.
    /// Detailed per-part values are filled by kaiju content stories; this class
    /// provides the schema and skeleton assets (Carapex / Lacera / Voltwyrm).
    /// See kaiju-part-system.md C.6 and ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/KaijuDef", fileName = "KaijuDef")]
    public sealed class KaijuDef : ScriptableObject
    {
        [Tooltip("Stable kaiju identifier string. Must be non-empty and unique across all KaijuDef assets. " +
                 "Used by ContentRegistry and material-economy as a lookup key. " +
                 "kaiju-part-system.md C.6 kaiju_id.")]
        [SerializeField] private string _kaijuId = string.Empty;

        [Tooltip("All breakable parts for this kaiju. Must contain at least one BossCore part (win condition). " +
                 "Typical range: 2–5 parts (up to 8 for late-game bosses). " +
                 "kaiju-part-system.md C.6 and A (overview).")]
        [SerializeField] private PartDef[] _parts = Array.Empty<PartDef>();

        // ── Public read-only properties ──────────────────────────────────────

        /// <summary>Stable kaiju identifier. Used as a ContentRegistry and economy lookup key.</summary>
        public string KaijuId => _kaijuId;

        /// <summary>
        /// All breakable parts. At least one must have PartType == BossCore.
        /// kaiju-part-system.md C.1, C.3, C.6.
        /// </summary>
        public PartDef[] Parts => _parts;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_kaijuId))
                Debug.LogError(
                    $"[KaijuDef:{name}] KaijuId must not be empty. " +
                    "Set a unique stable ID matching the kaiju design document.", this);

            if (_parts == null || _parts.Length == 0)
            {
                Debug.LogError(
                    $"[KaijuDef:{_kaijuId}] Parts array must contain at least one part.", this);
                return;
            }

            bool hasBossCore = false;
            for (int i = 0; i < _parts.Length; i++)
            {
                PartDef part = _parts[i];
                if (part == null) continue;

                if (part.PartType == PartType.BossCore)
                    hasBossCore = true;

                if (string.IsNullOrWhiteSpace(part.PartId))
                    Debug.LogError(
                        $"[KaijuDef:{_kaijuId}] Parts[{i}].PartId is empty. " +
                        "Every part must have a unique non-empty ID.", this);

                if (part.HMaxUseOverride && part.HMaxOverride <= 0f)
                    Debug.LogError(
                        $"[KaijuDef:{_kaijuId}] Parts[{i}] ({part.PartId}): " +
                        "HMaxUseOverride is true but HMaxOverride <= 0. Set a positive override value.", this);

                if (part.BMaxUseOverride && part.BMaxOverride <= 0f)
                    Debug.LogError(
                        $"[KaijuDef:{_kaijuId}] Parts[{i}] ({part.PartId}): " +
                        "BMaxUseOverride is true but BMaxOverride <= 0. Set a positive override value.", this);
            }

            if (!hasBossCore)
                Debug.LogError(
                    $"[KaijuDef:{_kaijuId}] Parts array must contain at least one part with " +
                    "PartType == BossCore. The BossCore part is the win condition " +
                    "(kaiju-part-system.md C.3).", this);
        }
#endif
    }
}
