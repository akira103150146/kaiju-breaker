using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Per-run tracker that upholds the weapon-pod guarantee (stage-system.md §F.3/§L.2): every stage drops
    /// at least one Primary (laser) and one Secondary (missile) pod before the boss. It subscribes to
    /// <see cref="EliteKilled"/>, decides which pod to request based on the segment's
    /// <see cref="PodPoolPreference"/> and what has already dropped, and publishes <see cref="PodSpawnRequested"/>
    /// (the actual pod object is spawned by Story 005). <see cref="FlushGuaranteed"/> forces any missing pool
    /// at the end of the escalating waves; <see cref="SpawnPreBossLullPods"/> tops up during the lull.
    ///
    /// <para>Pure C# — constructed fresh each run (state resets in the ctor, never stored on the read-only SO,
    /// ADR-0003) and fully EditMode-testable with a fake bus. <c>Random</c> pod-type selection is left to the
    /// pod spawner (Story 005) at spawn time; this tracker only emits the request.</para>
    /// </summary>
    public sealed class PodDropTracker
    {
        private readonly IEventBus _bus;
        private readonly PodDropConfig _config;
        private readonly Action<EliteKilled> _onEliteKilled;

        /// <summary>True once a Primary (laser) pod has been requested this run.</summary>
        public bool PrimarySpawned { get; private set; }

        /// <summary>True once a Secondary (missile) pod has been requested this run.</summary>
        public bool SecondarySpawned { get; private set; }

        public PodDropTracker(IEventBus bus, PodDropConfig config)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _onEliteKilled = OnEliteKilled;
            _bus.Subscribe(_onEliteKilled);
        }

        /// <summary>Unsubscribe on run teardown.</summary>
        public void Dispose() => _bus.Unsubscribe(_onEliteKilled);

        private void OnEliteKilled(EliteKilled evt)
        {
            PodType type = Resolve(evt.PodPoolPreference);
            RequestPod(type, isGuaranteed: false, evt.WorldPosition);
        }

        /// <summary>
        /// End of the escalating waves: force a pod for any pool that has not dropped yet so the stage's
        /// ≥1-primary + ≥1-secondary promise holds (§L.2). No-op if both pools already dropped.
        /// </summary>
        public void FlushGuaranteed()
        {
            if (!PrimarySpawned) RequestPod(PodType.Primary, isGuaranteed: true, Vector2.zero);
            if (!SecondarySpawned) RequestPod(PodType.Secondary, isGuaranteed: true, Vector2.zero);
        }

        /// <summary>
        /// Pre-boss lull top-up: publish <see cref="PodDropConfig.PreBossLullPodCount"/> pods, each filling a
        /// remaining pool gap (Primary first) or Random once both are covered (stage-system.md §F.2.4).
        /// </summary>
        public void SpawnPreBossLullPods()
        {
            for (int i = 0; i < _config.PreBossLullPodCount; i++)
            {
                PodType type = !PrimarySpawned ? PodType.Primary
                             : !SecondarySpawned ? PodType.Secondary
                             : PodType.Random;
                RequestPod(type, isGuaranteed: false, Vector2.zero);
            }
        }

        // ── Core ──────────────────────────────────────────────────────────────

        /// <summary>Map a segment preference to a concrete pod type given what has already dropped.</summary>
        private PodType Resolve(PodPoolPreference preference)
        {
            switch (preference)
            {
                case PodPoolPreference.Primary: return PodType.Primary;
                case PodPoolPreference.Secondary: return PodType.Secondary;
                default: // Auto — fill the gap, Primary first
                    if (!PrimarySpawned) return PodType.Primary;
                    if (!SecondarySpawned) return PodType.Secondary;
                    return PodType.Random;
            }
        }

        /// <summary>
        /// Publish a pod request unless that pool has already dropped (monotonic per pool). Random always
        /// publishes (both pools are already covered, so it is a bonus, not a guarantee) and sets no flag.
        /// </summary>
        private void RequestPod(PodType type, bool isGuaranteed, Vector2 position)
        {
            if (type == PodType.Primary)
            {
                if (PrimarySpawned) return; // already dropped — no duplicate
                PrimarySpawned = true;
            }
            else if (type == PodType.Secondary)
            {
                if (SecondarySpawned) return;
                SecondarySpawned = true;
            }
            _bus.Publish(new PodSpawnRequested(type, isGuaranteed, position));
        }
    }
}
