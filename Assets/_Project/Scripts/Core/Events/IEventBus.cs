using System;

namespace KaijuBreaker.Core
{
    /// <summary>
    /// Marker interface for all typed bus events. Every event is a
    /// <c>readonly struct</c> implementing this — the struct type IS the event
    /// (dispatch is by type), so names are noun-phrases with no <c>On</c> prefix
    /// (e.g. <c>PartBroke</c>, <c>LaserHit</c>). See ADR-0002 and technical-preferences.
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// Typed, struct-based event bus (ADR-0002). The single cross-system
    /// communication channel — systems never reference each other directly (ADR-0005);
    /// they publish/subscribe to Core-owned event structs here.
    ///
    /// Dispatch is SYNCHRONOUS and same-frame (required by the GDD: on_part_break
    /// same-frame banking, L2 Tier-3 ripple, fixed on_part_break→on_boss_core_break order).
    /// Events are value-type structs passed by <c>in</c> → steady-state zero GC.
    ///
    /// Subscribe/Unsubscribe are main-thread only (call during system init/teardown).
    /// Re-entrant Publish (a handler publishing while dispatching) is supported;
    /// Subscribe/Unsubscribe issued during a dispatch are deferred until it unwinds.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>Synchronously dispatch <paramref name="evt"/> to all current subscribers of <typeparamref name="TEvent"/>.</summary>
        void Publish<TEvent>(in TEvent evt) where TEvent : struct, IGameEvent;

        /// <summary>Register <paramref name="handler"/> to receive <typeparamref name="TEvent"/> publications.</summary>
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent;

        /// <summary>Remove a previously-registered <paramref name="handler"/>.</summary>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent;
    }
}
