using System;
using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Test double for <see cref="IEventBus"/> that records every published event (in order,
    /// boxed) while still delivering synchronously to subscribers via a real
    /// <see cref="TypedEventBus"/>. Real delivery is required for KaijuParts' own re-entrant
    /// paths (M3 chain subscribes to PartBroke; L2 ripple re-publishes LaserHit), so a
    /// record-only fake would break those flows. Assert on <see cref="Recorded"/> order and
    /// use <see cref="Events{T}"/> / <see cref="CountOf{T}"/> for payload checks.
    /// </summary>
    public sealed class RecordingEventBus : IEventBus
    {
        private readonly TypedEventBus _inner = new TypedEventBus();
        private readonly List<IGameEvent> _recorded = new List<IGameEvent>();

        /// <summary>All published events in publication order (boxed).</summary>
        public IReadOnlyList<IGameEvent> Recorded => _recorded;

        public void Clear() => _recorded.Clear();

        public void Publish<TEvent>(in TEvent evt) where TEvent : struct, IGameEvent
        {
            _recorded.Add(evt); // box once for recording; hot-path GC is not a concern in EditMode tests
            _inner.Publish(in evt);
        }

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent
            => _inner.Subscribe(handler);

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent
            => _inner.Unsubscribe(handler);

        /// <summary>All recorded events of type <typeparamref name="T"/>, in order.</summary>
        public List<T> Events<T>() where T : struct, IGameEvent
        {
            var result = new List<T>();
            for (int i = 0; i < _recorded.Count; i++)
                if (_recorded[i] is T t) result.Add(t);
            return result;
        }

        /// <summary>Count of recorded events of type <typeparamref name="T"/>.</summary>
        public int CountOf<T>() where T : struct, IGameEvent
        {
            int n = 0;
            for (int i = 0; i < _recorded.Count; i++)
                if (_recorded[i] is T) n++;
            return n;
        }
    }
}
