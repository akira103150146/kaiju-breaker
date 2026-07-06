using System.IO;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Meta;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Meta
{
    /// <summary>
    /// Meta Story 007 — weapon ownership + unlock persistence (meta-progression-system.md §C.2/§H.3; ADR-0004).
    /// <c>owned</c> is monotonic (false→true only); first pickup unlocks + autosaves + publishes
    /// <see cref="WeaponUnlocked"/>; a second pickup is a no-op; tier upgrades never grant ownership.
    ///
    /// <para><b>Reconciliation:</b> the committed <see cref="WeaponPodGrabbed"/> event (from stage-001) is used
    /// as the pickup signal — it carries the WeaponId and Meta does its own owned-check (the story's separate
    /// <c>WeaponPodPickup{IsFirstTime}</c> is not needed; the story explicitly allows Meta's own check). AC-5's
    /// tier path is exercised via the real <see cref="ISaveService.SetWeaponTier"/> (no WeaponUpgradeConfirmed
    /// event is committed).</para>
    /// </summary>
    [TestFixture]
    public sealed class SaveWeaponOwnershipTests
    {
        private string _dir;
        private SaveConfig _config;
        private CanonicalJsonSerializer _ser;
        private RecordingEventBus _bus;
        private AtomicSaveWriter _writer;
        private SaveWorker _worker;
        private SaveLoader _loader;
        private MetaSaveService _service;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "kaiju_ownership_test");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);

            _config = ScriptableObject.CreateInstance<SaveConfig>();
            _ser = new CanonicalJsonSerializer();
            _bus = new RecordingEventBus();
            _writer = new AtomicSaveWriter(_config, _dir, _ser);
            _worker = new SaveWorker(_writer, _config.SaveWorkerIdleMs);
            _loader = new SaveLoader(_config, _ser, _bus, _writer);
            _service = new MetaSaveService(_config, _bus, _loader, SaveMigrator.Default(), _worker);
            _service.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // ── AC-1: new-game starting ownership ─────────────────────────────────

        [Test]
        public void test_new_game_starting_ownership_matches_config()
        {
            Assert.IsTrue(_service.IsWeaponOwned(WeaponId.L1));
            Assert.IsTrue(_service.IsWeaponOwned(WeaponId.M1));
            foreach (var w in new[] { WeaponId.L2, WeaponId.L3, WeaponId.L4, WeaponId.M2, WeaponId.M3, WeaponId.M4 })
                Assert.IsFalse(_service.IsWeaponOwned(w), $"{w} locked from a fresh save");
        }

        // ── AC-2: first pickup unlocks + saves + event ────────────────────────

        [Test]
        public void test_first_pickup_unlocks_saves_and_publishes_event()
        {
            int enqueuesBefore = _worker.EnqueueCount;

            _bus.Publish(new WeaponPodGrabbed(WeaponId.L2));

            Assert.IsTrue(_service.IsWeaponOwned(WeaponId.L2));
            Assert.AreEqual(enqueuesBefore + 1, _worker.EnqueueCount, "one autosave on first unlock");
            Assert.AreEqual(1, _bus.CountOf<WeaponUnlocked>());
            Assert.AreEqual(WeaponId.L2, _bus.Events<WeaponUnlocked>()[0].Weapon);
            Assert.IsFalse(_service.IsWeaponOwned(WeaponId.M3), "unrelated weapon unaffected");
        }

        // ── AC-3: second pickup is a no-op ────────────────────────────────────

        [Test]
        public void test_second_pickup_does_not_resave_or_republish()
        {
            _bus.Publish(new WeaponPodGrabbed(WeaponId.L2)); // first unlock
            int enqueuesAfterFirst = _worker.EnqueueCount;

            _bus.Publish(new WeaponPodGrabbed(WeaponId.L2)); // duplicate pickup

            Assert.IsTrue(_service.IsWeaponOwned(WeaponId.L2));
            Assert.AreEqual(enqueuesAfterFirst, _worker.EnqueueCount, "no redundant autosave on second pickup");
            Assert.AreEqual(1, _bus.CountOf<WeaponUnlocked>(), "no duplicate WeaponUnlocked event");
        }

        // ── AC-4: ownership round-trips through save + load ───────────────────

        [Test]
        public void test_ownership_persists_across_save_and_reload()
        {
            _bus.Publish(new WeaponPodGrabbed(WeaponId.L3));
            _service.FlushSync();

            // A fresh service loading the written file must see L3 owned (CRC32 integrity intact).
            var bus2 = new RecordingEventBus();
            var writer2 = new AtomicSaveWriter(_config, _dir, _ser);
            var worker2 = new SaveWorker(writer2, _config.SaveWorkerIdleMs);
            var loader2 = new SaveLoader(_config, _ser, bus2, writer2);
            var service2 = new MetaSaveService(_config, bus2, loader2, SaveMigrator.Default(), worker2);
            service2.Initialize();

            Assert.IsTrue(service2.IsWeaponOwned(WeaponId.L3), "unlock survived the CRC32 write/read round-trip");
            Assert.IsFalse(service2.IsWeaponOwned(WeaponId.L2), "L2 unaffected by the L3 unlock");
            service2.Dispose();
        }

        // ── AC-5: tier upgrade does NOT grant ownership ───────────────────────

        [Test]
        public void test_tier_upgrade_does_not_grant_ownership()
        {
            Assert.IsFalse(_service.IsWeaponOwned(WeaponId.L3));

            _service.SetWeaponTier(WeaponId.L3, 1);

            Assert.AreEqual(1, _service.GetTier(WeaponId.L3));
            Assert.IsFalse(_service.IsWeaponOwned(WeaponId.L3), "tier upgrade must not flip owned");
        }

        [Test]
        public void test_tier_upgrade_on_owned_weapon_keeps_ownership()
        {
            _service.SetWeaponTier(WeaponId.L1, 2);
            Assert.IsTrue(_service.IsWeaponOwned(WeaponId.L1), "owned stays true through a tier change");
            Assert.AreEqual(2, _service.GetTier(WeaponId.L1));
        }

        // ── AC-6: independent concurrent unlocks ──────────────────────────────

        [Test]
        public void test_multiple_independent_unlocks_each_save_once()
        {
            int before = _worker.EnqueueCount;

            _bus.Publish(new WeaponPodGrabbed(WeaponId.L2));
            _bus.Publish(new WeaponPodGrabbed(WeaponId.L3));
            _bus.Publish(new WeaponPodGrabbed(WeaponId.M2));

            Assert.IsTrue(_service.IsWeaponOwned(WeaponId.L2));
            Assert.IsTrue(_service.IsWeaponOwned(WeaponId.L3));
            Assert.IsTrue(_service.IsWeaponOwned(WeaponId.M2));
            Assert.IsFalse(_service.IsWeaponOwned(WeaponId.L4));
            Assert.IsFalse(_service.IsWeaponOwned(WeaponId.M3));
            Assert.IsFalse(_service.IsWeaponOwned(WeaponId.M4));
            Assert.AreEqual(before + 3, _worker.EnqueueCount, "one save per new unlock");

            _bus.Publish(new WeaponPodGrabbed(WeaponId.L2)); // duplicate → no 4th save
            Assert.AreEqual(before + 3, _worker.EnqueueCount);
        }
    }
}
