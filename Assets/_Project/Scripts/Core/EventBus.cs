using System;
using System.Collections.Generic;

namespace KaijuBreaker.Core
{
    /// <summary>
    /// Default <see cref="IEventBus"/> implementation (ADR-0002 §1).
    ///
    /// Dispatch is synchronous and same-frame. Handlers are stored per event type in a
    /// typed list and invoked by index, so steady-state Publish allocates nothing
    /// (value-type events passed by <c>in</c>; no enumerator/boxing).
    ///
    /// Re-entrancy: a handler may Publish (same or other type) — nested dispatch runs to
    /// completion then the outer resumes. Subscribe/Unsubscribe issued *during* a dispatch
    /// are deferred and applied when that type's dispatch fully unwinds, so the live handler
    /// list is never mutated mid-iteration.
    /// </summary>
    public sealed class TypedEventBus : IEventBus
    {
        // Non-generic base so the bus can hold heterogeneous typed lists.
        private abstract class HandlerList { }

        private sealed class HandlerList<TEvent> : HandlerList where TEvent : struct, IGameEvent
        {
            private readonly List<Action<TEvent>> _handlers = new List<Action<TEvent>>(8);
            private readonly List<Action<TEvent>> _pendingAdd = new List<Action<TEvent>>();
            private readonly List<Action<TEvent>> _pendingRemove = new List<Action<TEvent>>();
            private int _dispatchDepth;

            public void Add(Action<TEvent> handler)
            {
                if (_dispatchDepth > 0) _pendingAdd.Add(handler);
                else if (!_handlers.Contains(handler)) _handlers.Add(handler);
            }

            public void Remove(Action<TEvent> handler)
            {
                if (_dispatchDepth > 0) _pendingRemove.Add(handler);
                else _handlers.Remove(handler);
            }

            public void Dispatch(in TEvent evt)
            {
                _dispatchDepth++;
                try
                {
                    // Index loop over the concrete List<T> — no enumerator allocation.
                    for (int i = 0; i < _handlers.Count; i++)
                        _handlers[i].Invoke(evt);
                }
                finally
                {
                    _dispatchDepth--;
                    if (_dispatchDepth == 0) ApplyPending();
                }
            }

            private void ApplyPending()
            {
                if (_pendingRemove.Count > 0)
                {
                    for (int i = 0; i < _pendingRemove.Count; i++) _handlers.Remove(_pendingRemove[i]);
                    _pendingRemove.Clear();
                }
                if (_pendingAdd.Count > 0)
                {
                    for (int i = 0; i < _pendingAdd.Count; i++)
                    {
                        var h = _pendingAdd[i];
                        if (!_handlers.Contains(h)) _handlers.Add(h);
                    }
                    _pendingAdd.Clear();
                }
            }
        }

        private readonly Dictionary<Type, HandlerList> _lists = new Dictionary<Type, HandlerList>(32);

        /// <inheritdoc/>
        public void Publish<TEvent>(in TEvent evt) where TEvent : struct, IGameEvent
        {
            if (_lists.TryGetValue(typeof(TEvent), out var list))
                ((HandlerList<TEvent>)list).Dispatch(in evt);
        }

        /// <inheritdoc/>
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetOrCreate<TEvent>().Add(handler);
        }

        /// <inheritdoc/>
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent
        {
            if (handler == null) return;
            if (_lists.TryGetValue(typeof(TEvent), out var list))
                ((HandlerList<TEvent>)list).Remove(handler);
        }

        private HandlerList<TEvent> GetOrCreate<TEvent>() where TEvent : struct, IGameEvent
        {
            var key = typeof(TEvent);
            if (!_lists.TryGetValue(key, out var list))
            {
                var typed = new HandlerList<TEvent>();
                _lists.Add(key, typed);
                return typed;
            }
            return (HandlerList<TEvent>)list;
        }
    }
}
