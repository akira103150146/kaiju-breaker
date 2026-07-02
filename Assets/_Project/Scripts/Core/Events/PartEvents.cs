using UnityEngine;

namespace KaijuBreaker.Core
{
    // KaijuParts state + break events. KaijuParts OWNS and emits these
    // (kaiju-part-system.md C.5); downstream: Economy, Meta, GameFeel, UI, RunController.

    /// <summary>on_part_softened — part crossed θ_S into SOFTENED.</summary>
    public readonly struct PartSoftened : IGameEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public PartSoftened(int partId, int kaijuId) { PartId = partId; KaijuId = kaijuId; }
    }

    /// <summary>on_part_softened_exit — heat fell below θ_S_exit; back to INTACT.</summary>
    public readonly struct PartSoftenedExit : IGameEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public PartSoftenedExit(int partId, int kaijuId) { PartId = partId; KaijuId = kaijuId; }
    }

    /// <summary>on_part_staggered — L3 shockwave opened the stagger/armor window.</summary>
    public readonly struct PartStaggered : IGameEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public PartStaggered(int partId, int kaijuId) { PartId = partId; KaijuId = kaijuId; }
    }

    /// <summary>on_part_stagger_end — the stagger window expired; armor restored.</summary>
    public readonly struct PartStaggerEnd : IGameEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public PartStaggerEnd(int partId, int kaijuId) { PartId = partId; KaijuId = kaijuId; }
    }

    /// <summary>
    /// on_part_break — a part broke. Carries everything downstream needs so no
    /// consumer must re-query (ADR-0002 §3): world position (drop spawn), drop table,
    /// break quality (Economy yield scaling), adjacency (L2/M3 Tier-3 chains), and the
    /// chain-break flag (prevents recursive chaining, kaiju-part-system.md E.4).
    /// Note: shard_yield/core_yield are NOT here — Economy computes them from BreakQuality.
    /// </summary>
    public readonly struct PartBroke : IGameEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public readonly PartType Type;
        public readonly Vector2 WorldPosition;
        public readonly int DropTableId;
        public readonly BreakQuality Quality;

        /// <summary>Surviving adjacent part ids (for Tier-3 chain consumers). May be null/empty.
        /// Part-breaks are low-frequency, so this per-break array is acceptable vs the bullet-field zero-GC target.</summary>
        public readonly int[] AdjacencyIds;

        /// <summary>True if this break was caused by an M3 Tier-3 chain (chains must not chain again).</summary>
        public readonly bool IsChainBreak;

        public PartBroke(int partId, int kaijuId, PartType type, Vector2 worldPosition,
                         int dropTableId, BreakQuality quality, int[] adjacencyIds, bool isChainBreak)
        {
            PartId = partId;
            KaijuId = kaijuId;
            Type = type;
            WorldPosition = worldPosition;
            DropTableId = dropTableId;
            Quality = quality;
            AdjacencyIds = adjacencyIds;
            IsChainBreak = isChainBreak;
        }
    }

    /// <summary>
    /// on_boss_core_break — the boss core broke. Emitted in the SAME frame,
    /// immediately AFTER the core's PartBroke (fixed order per kaiju-part-system.md E.6),
    /// so Economy banks the core drop before RunController runs the victory sequence.
    /// </summary>
    public readonly struct BossCoreBroke : IGameEvent
    {
        public readonly int KaijuId;
        public readonly Vector2 WorldPosition;
        public BossCoreBroke(int kaijuId, Vector2 worldPosition) { KaijuId = kaijuId; WorldPosition = worldPosition; }
    }
}
