using System;
using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// Identifies which Tier-3 residual-heat source registered a per-part timer, so two
    /// independent sources (e.g. L1's residual flame and L4's afterimage) can co-exist on the
    /// same part without one overwriting the other. See <see cref="ResidualHeatTracker"/>.
    /// </summary>
    public enum ResidualChannel
    {
        /// <summary>L1 Tier-3 "全幅掃蕩" residual flame. weapon-system.md C.4 / G.2.</summary>
        L1Residual = 0,

        /// <summary>L4 Tier-3 "熱殘影" afterimage. weapon-system.md C.4 / G.2.</summary>
        L4Afterimage = 1
    }

    /// <summary>
    /// Shared per-part registry of time-limited residual heat effects (Story 008 — L1 Tier-3
    /// residual flame, L4 Tier-3 heat afterimage). A single instance is meant to be constructed
    /// once by the composition root and injected into both <see cref="L1SpreadLaser"/> and
    /// <see cref="L4PierceBeam"/>, and <see cref="Tick"/>d once per frame by the composition
    /// root — NOT by the individual weapons — so E.6's "same part, take the max rate, do not
    /// sum" rule can be enforced across sources.
    ///
    /// Plain C# (no MonoBehaviour/Physics2D): pure per-part timer bookkeeping plus a bus
    /// publish, fully EditMode-testable. weapon-system.md E.6 / G.2 (l1_t3_residual_*,
    /// l4_t3_afterimage_*).
    /// </summary>
    public sealed class ResidualHeatTracker
    {
        private readonly struct Key : IEquatable<Key>
        {
            public readonly int PartId;
            public readonly ResidualChannel Channel;
            public Key(int partId, ResidualChannel channel) { PartId = partId; Channel = channel; }
            public bool Equals(Key other) => PartId == other.PartId && Channel == other.Channel;
            public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode() => (PartId * 397) ^ (int)Channel;
        }

        private struct Entry
        {
            public int KaijuId;
            public float Rate;
            public float Remaining;
        }

        /// <summary>Below this many seconds remaining, an entry is treated as expired.</summary>
        private const float Epsilon = 1e-4f;

        private readonly IEventBus _bus;
        private readonly Dictionary<Key, Entry> _entries = new Dictionary<Key, Entry>();

        // Reused scratch buffers so steady-state Tick() does not allocate every frame.
        private readonly List<Key> _liveKeysScratch = new List<Key>();
        private readonly List<Key> _expiredKeysScratch = new List<Key>();
        private readonly Dictionary<int, (int KaijuId, float MaxRate)> _perPartMaxScratch =
            new Dictionary<int, (int KaijuId, float MaxRate)>();

        /// <param name="bus">Typed event bus this tracker publishes <see cref="LaserHit"/> onto (required).</param>
        public ResidualHeatTracker(IEventBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>
        /// (Re)start a residual timer for <paramref name="partId"/> on <paramref name="channel"/>.
        /// Re-registering the same (part, channel) pair RESETS its rate/duration — it does not
        /// stack or extend additively (weapon-system.md G.2: "re-hit resets, not additive").
        /// A different channel on the same part is tracked independently (E.6).
        /// </summary>
        public void Register(int partId, int kaijuId, ResidualChannel channel, float rate, float duration)
        {
            if (rate <= 0f || duration <= 0f) return;
            _entries[new Key(partId, channel)] = new Entry { KaijuId = kaijuId, Rate = rate, Remaining = duration };
        }

        /// <summary>
        /// Advance every active residual timer by <paramref name="deltaTime"/>. For each part with
        /// at least one still-active channel this frame, publishes exactly ONE <see cref="LaserHit"/>
        /// at <c>max(activeRatesForThatPart) * deltaTime</c> — never the sum (E.6 guardrail).
        /// Expired entries are removed after the pass.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || _entries.Count == 0) return;

            _liveKeysScratch.Clear();
            foreach (var key in _entries.Keys) _liveKeysScratch.Add(key);

            _expiredKeysScratch.Clear();
            _perPartMaxScratch.Clear();

            for (int i = 0; i < _liveKeysScratch.Count; i++)
            {
                var key = _liveKeysScratch[i];
                var entry = _entries[key];

                if (entry.Remaining <= Epsilon)
                {
                    _expiredKeysScratch.Add(key);
                    continue;
                }

                // Still active at the start of this tick — contributes to this frame's emission
                // even if it fully depletes by the end of this same tick.
                if (!_perPartMaxScratch.TryGetValue(key.PartId, out var current) || entry.Rate > current.MaxRate)
                    _perPartMaxScratch[key.PartId] = (entry.KaijuId, entry.Rate);

                entry.Remaining -= deltaTime;
                if (entry.Remaining <= Epsilon) _expiredKeysScratch.Add(key);
                else _entries[key] = entry;
            }

            for (int i = 0; i < _expiredKeysScratch.Count; i++) _entries.Remove(_expiredKeysScratch[i]);

            foreach (var kvp in _perPartMaxScratch)
            {
                float heatDelta = kvp.Value.MaxRate * deltaTime;
                if (heatDelta <= 0f) continue;
                _bus.Publish(new LaserHit(kvp.Key, kvp.Value.KaijuId, heatDelta));
            }
        }

        /// <summary>True if any channel is currently active for <paramref name="partId"/>. For tests/HUD.</summary>
        public bool HasActiveResidual(int partId)
        {
            foreach (var key in _entries.Keys)
                if (key.PartId == partId) return true;
            return false;
        }
    }
}
