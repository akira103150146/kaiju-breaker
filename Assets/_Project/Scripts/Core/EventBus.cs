// SCAFFOLD STUB — structural placeholder only, not functional.
// Full event contract (payloads, all event types) is defined in ADR-0002.
// See docs/architecture/adr/0002-event-bus-architecture.md for the authoritative source.
using System;

namespace KaijuBreaker.Core
{
    /// <summary>
    /// Typed, struct-safe event bus (ADR-0002).
    /// All cross-system communication goes through this interface — no direct
    /// system-to-system assembly references are permitted (ADR-0005).
    /// Publish is safe to call from any thread; Subscribe/Unsubscribe must be called
    /// on the main thread during system initialisation.
    /// </summary>
    public interface IEventBus
    {
        void Publish<TEvent>(in TEvent evt) where TEvent : struct;
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;
    }

    // ---------------------------------------------------------------------------
    // Placeholder event structs — expand fields when GDD payloads are finalised.
    // Authority: weapon-system.md F.1, kaiju-part-system.md C.5 (ADR-0002).
    // ---------------------------------------------------------------------------

    public readonly struct LaserHitEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public readonly float HeatDelta;

        public LaserHitEvent(int partId, int kaijuId, float heatDelta)
        {
            PartId = partId; KaijuId = kaijuId; HeatDelta = heatDelta;
        }
    }

    public readonly struct MissileHitEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public readonly float BreakDeltaBase;
        // TODO: add weapon_id per weapon-system.md F.1
    }

    public readonly struct PartBreakEvent
    {
        public readonly int PartId;
        // TODO: add part_type, break_quality, world_pos, drop_table_id,
        //       adjacency mask, is_chain_break per kaiju-part-system.md C.5
    }

    public readonly struct BossCoreBreakEvent
    {
        public readonly int KaijuId;
    }
}
