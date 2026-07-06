using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Meta;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Meta
{
    /// <summary>
    /// Meta Story 005 — persistent/per-run boundary + new-game init + last-loadout fallback
    /// (meta-progression-system.md §C.1/§C.7/§C.8/§E.5/§H.7/§H.8; ADR-0004). Drives the real
    /// <see cref="MetaSaveService"/> over a temp dir with a <see cref="RecordingEventBus"/>.
    ///
    /// <para><b>Reconciliation:</b> <c>LoadoutConfirmed</c> gained a payload (Primary/Secondary/Difficulty);
    /// read queries live on <see cref="MetaSaveService"/> (not the committed ISaveService) — final ISaveService
    /// surface is Story 006's decision.</para>
    /// </summary>
    [TestFixture]
    public sealed class SaveStateBoundaryTests
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
            _dir = Path.Combine(Path.GetTempPath(), "kaiju_boundary_test");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);

            _config = ScriptableObject.CreateInstance<SaveConfig>();
            _ser = new CanonicalJsonSerializer();
            _bus = new RecordingEventBus();
            _writer = new AtomicSaveWriter(_config, _dir, _ser);
            _worker = new SaveWorker(_writer, _config.SaveWorkerIdleMs);
            _loader = new SaveLoader(_config, _ser, _bus, _writer);
            _service = new MetaSaveService(_config, _bus, _loader, SaveMigrator.Default(), _worker);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // ── AC-1: new-game defaults ───────────────────────────────────────────

        [Test]
        public void test_initialize_new_game_matches_default_schema()
        {
            var data = _service.InitializeNewGame();

            Assert.IsTrue(data.Weapons["L1"].Owned);
            Assert.IsTrue(data.Weapons["M1"].Owned);
            foreach (var id in new[] { "L2", "L3", "L4", "M2", "M3", "M4" })
                Assert.IsFalse(data.Weapons[id].Owned, $"{id} unowned on a fresh save");

            foreach (var key in new[] { "shard_common", "core_carapace", "core_limb", "core_energy", "essence_kaiju" })
                Assert.AreEqual(0, data.Materials[key]);

            foreach (var kaiju in new[] { "CARAPEX", "LACERA", "VOLTWYRM" })
            {
                var rec = data.KaijuRecords[kaiju];
                CollectionAssert.IsEmpty(rec.PartsEverBroken);
                Assert.AreEqual(0, rec.FullClearCount);
                Assert.AreEqual(0, rec.HuntCountPerDifficulty["D1"]);
                Assert.IsNull(rec.BestTimePerDifficulty["D1"]);
            }

            Assert.AreEqual("D1", data.Meta.LastSelectedDifficulty);
            Assert.AreEqual("L1", data.Meta.LastLoadout.Primary);
            Assert.AreEqual("M1", data.Meta.LastLoadout.Secondary);
            Assert.IsFalse(data.Meta.FirstLaunchComplete);
            Assert.AreEqual(0, data.Stats.TotalRunsStarted);
        }

        [Test]
        public void test_initialize_new_game_twice_yields_independent_objects()
        {
            var a = _service.InitializeNewGame();
            var b = _service.InitializeNewGame();
            Assert.AreNotSame(a, b);
            a.Materials["shard_common"] = 500;
            Assert.AreEqual(0, b.Materials["shard_common"], "second instance is not shared with the first");
        }

        // ── AC-2: last_difficulty persists across load ────────────────────────

        [Test]
        public void test_last_difficulty_persists_across_load()
        {
            var seed = NewGameFactory.Create(_config);
            seed.Meta.LastSelectedDifficulty = "D2";
            _writer.AtomicWrite(seed);

            _service.Initialize();

            Assert.AreEqual("D2", _service.GetLastDifficulty());
        }

        // ── AC-3 / AC-4: loadout fallback when unowned ────────────────────────

        [Test]
        public void test_last_loadout_primary_falls_back_to_owned_laser()
        {
            var data = NewGameFactory.Create(_config); // L1/M1 owned; L3 not owned
            data.Meta.LastLoadout = new LoadoutData("L3", "M1");

            _service.ValidateLastLoadout(data, _config);

            Assert.AreEqual("L1", data.Meta.LastLoadout.Primary, "unowned L3 → first owned laser L1");
            Assert.AreEqual("M1", data.Meta.LastLoadout.Secondary, "owned M1 unchanged");
        }

        [Test]
        public void test_last_loadout_secondary_falls_back_to_owned_missile()
        {
            var data = NewGameFactory.Create(_config);
            data.Meta.LastLoadout = new LoadoutData("L1", "M3"); // M3 not owned

            _service.ValidateLastLoadout(data, _config);

            Assert.AreEqual("M1", data.Meta.LastLoadout.Secondary, "unowned M3 → first owned missile M1");
        }

        [Test]
        public void test_last_loadout_secondary_pointing_at_laser_id_falls_back_to_missile()
        {
            var data = NewGameFactory.Create(_config);
            data.Meta.LastLoadout = new LoadoutData("L1", "L2"); // schema corruption: secondary is a laser id

            _service.ValidateLastLoadout(data, _config);

            Assert.AreEqual("M1", data.Meta.LastLoadout.Secondary);
        }

        // ── AC-5: SaveData has no per-run fields ──────────────────────────────

        [Test]
        public void test_save_data_has_no_per_run_fields()
        {
            string[] forbidden = { "score", "runtime", "currentheat", "breakprogress", "inflight", "currentinrun", "timer" };
            Type[] types = { typeof(SaveData), typeof(WeaponSaveData), typeof(KaijuRecordData),
                             typeof(MetaBlock), typeof(LoadoutData), typeof(SettingsData), typeof(StatsData) };

            foreach (var t in types)
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    string lower = f.Name.ToLowerInvariant();
                    foreach (var bad in forbidden)
                        Assert.IsFalse(lower.Contains(bad),
                            $"per-run field '{f.Name}' must not exist on persistent type {t.Name}");
                }
        }

        // ── AC-6: LoadoutConfirmed writes last_loadout + last_difficulty ──────

        [Test]
        public void test_loadout_confirmed_updates_state_and_enqueues_once()
        {
            _service.Initialize();
            int enqueuesBefore = _worker.EnqueueCount;

            _bus.Publish(new LoadoutConfirmed(WeaponId.L2, WeaponId.M2, DifficultyTier.D3));

            Assert.AreEqual("L2", _service.GetLastLoadout().Primary);
            Assert.AreEqual("M2", _service.GetLastLoadout().Secondary);
            Assert.AreEqual("D3", _service.GetLastDifficulty());
            Assert.AreEqual(enqueuesBefore + 1, _worker.EnqueueCount, "one LoadoutConfirmed → one EnqueueSave");
        }

        [Test]
        public void test_query_before_initialize_throws()
        {
            Assert.Throws<InvalidOperationException>(() => _service.GetLastDifficulty());
        }

        [Test]
        public void test_initialize_on_first_launch_publishes_save_ready()
        {
            _service.Initialize();
            Assert.IsTrue(_service.IsInitialized);
            Assert.AreEqual(1, _bus.CountOf<SaveReady>());
            Assert.IsFalse(_service.State.Meta.FirstLaunchComplete, "first launch flag not yet set");
        }
    }
}
