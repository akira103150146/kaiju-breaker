using UnityEngine;

namespace KaijuBreaker.Core
{
    // Weapon-pod + elite lifecycle events (stage-system.md §F). Published by the Stage system (elite death,
    // pod-drop tracker); consumed by the pod spawner (Story 005) and Economy (elite shard bonus).

    /// <summary>
    /// on_elite_killed — an elite mob died. Carries the segment's requested pod pool
    /// (<see cref="PodPoolPreference"/>) and the world position to drop at. The <c>PodDropTracker</c>
    /// subscribes to decide which pod (if any) to request (stage-system.md §F.3).
    /// </summary>
    public readonly struct EliteKilled : IGameEvent
    {
        public readonly PodPoolPreference PodPoolPreference;
        public readonly Vector2 WorldPosition;

        public EliteKilled(PodPoolPreference podPoolPreference, Vector2 worldPosition)
        {
            PodPoolPreference = podPoolPreference;
            WorldPosition = worldPosition;
        }
    }

    /// <summary>
    /// on_pod_spawn_requested — a cycling weapon pod should be spawned (stage-system.md §F.2). Published by
    /// the <c>PodDropTracker</c> (elite drop, end-of-stage guarantee, pre-boss lull); consumed by the pod
    /// spawner (Story 005) which instantiates the pod object. <see cref="IsGuaranteed"/> marks the forced
    /// end-of-stage fill that upholds the "≥1 primary + ≥1 secondary per stage" promise (§L.2).
    /// </summary>
    public readonly struct PodSpawnRequested : IGameEvent
    {
        public readonly PodType PodType;
        public readonly bool IsGuaranteed;
        public readonly Vector2 WorldPosition;

        public PodSpawnRequested(PodType podType, bool isGuaranteed, Vector2 worldPosition)
        {
            PodType = podType;
            IsGuaranteed = isGuaranteed;
            WorldPosition = worldPosition;
        }
    }

    /// <summary>
    /// on_elite_shards_dropped — an elite death grants a shard bonus (stage-system.md §F.3;
    /// <c>EnemyDef.EliteShardBonus</c>). Economy subscribes to bank the bonus. Published alongside
    /// <see cref="EliteKilled"/>.
    /// </summary>
    public readonly struct EliteShardsDropped : IGameEvent
    {
        public readonly int Amount;

        public EliteShardsDropped(int amount)
        {
            Amount = amount;
        }
    }
}
