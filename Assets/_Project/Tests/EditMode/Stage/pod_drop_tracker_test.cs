using System;
using System.Linq;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Stage Story 004 — weapon-pod guarantee tracker (stage-system.md §F.3/§L.2; TR-stage-002). The
    /// <see cref="PodDropTracker"/> is pure event-driven C#, so this is an EditMode Logic test (the story
    /// labelled it PlayMode, but nothing here touches the scene) using a <see cref="RecordingEventBus"/>.
    ///
    /// <para><b>Reconciliation:</b> elite specs live on <see cref="EnemyDef"/> (EliteHpMult/AuraColor/
    /// ShardBonus), not a separate EnemyConfig; the segment's pool preference is a new
    /// <see cref="SegmentDef.PodPoolPreference"/> field. Emitting <see cref="EliteKilled"/> on actual elite
    /// death is a combat/damage concern (no damage system yet — bullets blocked by ADR-0001); this suite
    /// drives the tracker with the event directly, which is exactly what the ACs specify. Random pod-type
    /// resolution at spawn is Story 005's.</para>
    /// </summary>
    [TestFixture]
    public sealed class PodDropTrackerTests
    {
        private RecordingEventBus _bus;
        private PodDropConfig _config;
        private PodDropTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _config = ContentTestFactory.Create<PodDropConfig>(
                ("_guaranteedPrimaryPerStage", 1), ("_guaranteedSecondaryPerStage", 1), ("_preBossLullPodCount", 1));
            _tracker = new PodDropTracker(_bus, _config);
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private static EliteKilled Elite(PodPoolPreference pref) => new EliteKilled(pref, Vector2.zero);

        // ── AC-1: elite kill → track + request; duplicate no-op ───────────────

        [Test]
        public void test_elite_kill_requests_primary_pod_and_dedupes()
        {
            _bus.Publish(Elite(PodPoolPreference.Primary));

            Assert.IsTrue(_tracker.PrimarySpawned);
            Assert.AreEqual(1, _bus.CountOf<PodSpawnRequested>());
            Assert.AreEqual(PodType.Primary, _bus.Events<PodSpawnRequested>()[0].PodType);

            _bus.Publish(Elite(PodPoolPreference.Primary)); // second primary elite → no duplicate pod
            Assert.AreEqual(1, _bus.CountOf<PodSpawnRequested>());
        }

        // ── AC-2: auto fills the gap; random when both covered ────────────────

        [Test]
        public void test_auto_preference_fills_the_missing_pool()
        {
            _bus.Publish(Elite(PodPoolPreference.Primary)); // Primary now covered
            _bus.Publish(Elite(PodPoolPreference.Auto));    // Auto → should fill Secondary

            Assert.IsTrue(_tracker.SecondarySpawned);
            var reqs = _bus.Events<PodSpawnRequested>();
            Assert.AreEqual(PodType.Secondary, reqs[1].PodType);
        }

        [Test]
        public void test_auto_preference_requests_random_when_both_covered()
        {
            _bus.Publish(Elite(PodPoolPreference.Primary));
            _bus.Publish(Elite(PodPoolPreference.Secondary));
            _bus.Clear();

            _bus.Publish(Elite(PodPoolPreference.Auto)); // both covered → Random bonus

            Assert.AreEqual(1, _bus.CountOf<PodSpawnRequested>());
            Assert.AreEqual(PodType.Random, _bus.Events<PodSpawnRequested>()[0].PodType);
        }

        // ── AC-3: end-of-stage guaranteed fill ────────────────────────────────

        [Test]
        public void test_flush_guaranteed_forces_missing_pool()
        {
            _bus.Publish(Elite(PodPoolPreference.Primary)); // only Primary dropped
            _bus.Clear();

            _tracker.FlushGuaranteed();

            var reqs = _bus.Events<PodSpawnRequested>();
            Assert.AreEqual(1, reqs.Count);
            Assert.AreEqual(PodType.Secondary, reqs[0].PodType);
            Assert.IsTrue(reqs[0].IsGuaranteed);
            Assert.IsTrue(_tracker.PrimarySpawned && _tracker.SecondarySpawned);
        }

        [Test]
        public void test_flush_guaranteed_noop_when_both_covered()
        {
            _bus.Publish(Elite(PodPoolPreference.Primary));
            _bus.Publish(Elite(PodPoolPreference.Secondary));
            _bus.Clear();

            _tracker.FlushGuaranteed();
            Assert.AreEqual(0, _bus.CountOf<PodSpawnRequested>());
        }

        // ── AC-4: 200 seeds — guarantee always holds after flush ──────────────

        [Test]
        public void test_guarantee_holds_across_200_runs()
        {
            var prefs = new[] { PodPoolPreference.Primary, PodPoolPreference.Secondary, PodPoolPreference.Auto };
            for (int seed = 0; seed < 200; seed++)
            {
                var bus = new RecordingEventBus();
                var tracker = new PodDropTracker(bus, _config);
                var rng = new System.Random(seed);

                // Simulate a random handful of elite kills this run (0..4), any preference.
                int elites = rng.Next(0, 5);
                for (int i = 0; i < elites; i++)
                    bus.Publish(new EliteKilled(prefs[rng.Next(prefs.Length)], Vector2.zero));

                tracker.FlushGuaranteed(); // end-of-escalation guarantee

                Assert.IsTrue(tracker.PrimarySpawned && tracker.SecondarySpawned,
                    $"seed {seed}: both pools guaranteed after flush (elites={elites})");
                tracker.Dispose();
            }
        }

        // ── AC-5: pre-boss lull top-up ────────────────────────────────────────

        [Test]
        public void test_pre_boss_lull_spawns_random_when_both_covered()
        {
            _bus.Publish(Elite(PodPoolPreference.Primary));
            _bus.Publish(Elite(PodPoolPreference.Secondary));
            _bus.Clear();

            _tracker.SpawnPreBossLullPods(); // count = 1

            Assert.AreEqual(1, _bus.CountOf<PodSpawnRequested>());
            Assert.AreEqual(PodType.Random, _bus.Events<PodSpawnRequested>()[0].PodType);
        }

        [Test]
        public void test_pre_boss_lull_count_two_spawns_two()
        {
            var cfg = ContentTestFactory.Create<PodDropConfig>(("_preBossLullPodCount", 2));
            var bus = new RecordingEventBus();
            var tracker = new PodDropTracker(bus, cfg);
            bus.Publish(Elite(PodPoolPreference.Primary));
            bus.Publish(Elite(PodPoolPreference.Secondary));
            bus.Clear();

            tracker.SpawnPreBossLullPods();

            Assert.AreEqual(2, bus.CountOf<PodSpawnRequested>());
            tracker.Dispose();
            UnityEngine.Object.DestroyImmediate(cfg);
        }

        [Test]
        public void test_pre_boss_lull_fills_gaps_before_random()
        {
            // Nothing dropped yet → lull count 1 fills Primary first (the gap), not Random.
            _tracker.SpawnPreBossLullPods();
            Assert.AreEqual(PodType.Primary, _bus.Events<PodSpawnRequested>()[0].PodType);
        }
    }
}
