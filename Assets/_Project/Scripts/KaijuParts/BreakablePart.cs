using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.KaijuParts
{
    /// <summary>
    /// Runtime state for a single breakable kaiju part (kaiju-part-system.md C.1).
    /// A plain mutable C# class owned by <see cref="PartStateSystem"/> (not a MonoBehaviour) —
    /// it is authored data (from <see cref="Content.KaijuDef"/>) plus the two live bars
    /// (heat H / break B) and their derived state machines.
    ///
    /// IDs are ints: <see cref="Id"/> / <see cref="KaijuId"/> / <see cref="AdjacencyIds"/> /
    /// <see cref="DropTableId"/> are the stable ints carried on the event bus; <see cref="Name"/>
    /// and <see cref="DropTableName"/> keep the original string ids from the SO for the guard
    /// check and debugging. The string→int mapping is built by <see cref="PartStateSystem"/>
    /// at load (weapon/part events are int-keyed for zero-GC dispatch).
    /// </summary>
    public sealed class BreakablePart
    {
        // ── Identity (immutable for the part's lifetime) ─────────────────────────
        public readonly int Id;
        public readonly string Name;
        public readonly int KaijuId;
        public readonly PartType PartType;
        public readonly float HMax;
        public readonly float BMax;
        public readonly int DropTableId;
        public readonly string DropTableName;

        /// <summary>Raw one-way adjacency names as declared in the KaijuDef (before graph resolution).</summary>
        public readonly string[] AdjacencyNames;

        // ── Live state ───────────────────────────────────────────────────────────
        public float HCurrent;
        public float BCurrent;
        public HeatState HeatState;
        public ArmorState ArmorState;
        public float StaggerTimer;
        public BreakState BreakState;
        public Vector2 WorldPosition;

        /// <summary>
        /// Bidirectional neighbour ints, resolved by <see cref="PartStateSystem"/> after the
        /// adjacency graph is built. Sorted ascending for deterministic chain-target ordering.
        /// Carried in the <see cref="PartBroke"/> payload for L2/M3 Tier-3 chain consumers.
        /// </summary>
        public int[] AdjacencyIds;

        public BreakablePart(
            int id, string name, int kaijuId, PartType partType,
            float hMax, float bMax, int dropTableId, string dropTableName,
            string[] adjacencyNames)
        {
            Id = id;
            Name = name;
            KaijuId = kaijuId;
            PartType = partType;
            HMax = hMax;
            BMax = bMax;
            DropTableId = dropTableId;
            DropTableName = dropTableName;
            AdjacencyNames = adjacencyNames ?? System.Array.Empty<string>();
            AdjacencyIds = System.Array.Empty<int>();
            ResetLiveState();
        }

        /// <summary>
        /// Reset the mutable bars/states to a fresh ALIVE part (start of a run).
        /// ARMORED parts start ARMOR_INTACT; all others also report Intact (they have no gate).
        /// kaiju-part-system.md H.5 — parts never regenerate mid-run, but a new round resets cleanly.
        /// </summary>
        public void ResetLiveState()
        {
            HCurrent = 0f;
            BCurrent = 0f;
            HeatState = HeatState.Intact;
            ArmorState = ArmorState.Intact;
            StaggerTimer = 0f;
            BreakState = BreakState.Alive;
        }
    }
}
