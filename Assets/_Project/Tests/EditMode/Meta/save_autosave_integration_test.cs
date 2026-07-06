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
    /// Meta Story 006 — autosave-on-bank integration (meta-progression-system.md §C.5/§C.6/§D.1/§H.1/§H.2;
    /// ADR-0004/0002). <see cref="MetaSaveService"/> is the real <see cref="ISaveService"/> +
    /// <see cref="IWeaponTierQuery"/> backend; every persistent mutation enqueues an autosave same-frame.
    ///
    /// <para><b>Reconciliation vs story text (important):</b> the committed <see cref="PartBroke"/> /
    /// <see cref="HuntEnded"/> events do NOT carry material yields — per the economy epic's committed design
    /// (ADR-0002 §3) <b>Economy</b> computes yields and calls <see cref="ISaveService.CreditMaterials"/>;
    /// Meta persists. So the "Meta reads ShardYield/CoreYield from PartBroke" ACs are replaced by testing the
    /// real path: CreditMaterials accumulation + same-frame autosave. Per-kaiju / per-difficulty record
    /// updates (parts_ever_broken, full_clear_count-per-kaiju, best_time) need an int→string kaiju-id map and
    /// richer events (difficulty/time), neither committed yet — deferred; Meta updates the GLOBAL stats it
    /// can derive from the current events. The OnApplicationPause/Quit path is manual QA
    /// (`production/qa/evidence/save-autosave-suspend-evidence.md`).</para>
    /// </summary>
    [TestFixture]
    public sealed class SaveAutosaveIntegrationTests
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
            _dir = Path.Combine(Path.GetTempPath(), "kaiju_autosave_test");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);

            _config = ScriptableObject.CreateInstance<SaveConfig>();
            _ser = new CanonicalJsonSerializer();
            _bus = new RecordingEventBus();
            _writer = new AtomicSaveWriter(_config, _dir, _ser);
            _worker = new SaveWorker(_writer, _config.SaveWorkerIdleMs);
            _loader = new SaveLoader(_config, _ser, _bus, _writer);
            _service = new MetaSaveService(_config, _bus, _loader, SaveMigrator.Default(), _worker);
            _service.Initialize(); // fresh new game, all materials 0
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // ── ISaveService material crediting (the real Economy → Meta path) ────

        [Test]
        public void test_credit_materials_accumulates_and_autosaves_same_frame()
        {
            int enqueuesBefore = _worker.EnqueueCount;

            _service.CreditMaterials(MaterialId.ShardCommon, 3);
            _service.CreditMaterials(MaterialId.CoreCarapace, 1);

            Assert.AreEqual(3, _service.GetMaterialCount(MaterialId.ShardCommon));
            Assert.AreEqual(1, _service.GetMaterialCount(MaterialId.CoreCarapace));
            Assert.AreEqual(enqueuesBefore + 2, _worker.EnqueueCount, "each credit enqueues an autosave synchronously");
        }

        [Test]
        public void test_repeated_credits_do_not_overflow()
        {
            // Long-backed storage: crediting beyond int.MaxValue must not wrap.
            for (int i = 0; i < 5; i++) _service.CreditMaterials(MaterialId.ShardCommon, 1_000_000_000);
            // 5e9 exceeds int.MaxValue → GetMaterialCount clamps to int.MaxValue rather than wrapping negative.
            Assert.AreEqual(int.MaxValue, _service.GetMaterialCount(MaterialId.ShardCommon));
        }

        [Test]
        public void test_spend_materials_deducts()
        {
            _service.CreditMaterials(MaterialId.CoreLimb, 10);
            _service.SpendMaterials(MaterialId.CoreLimb, 4);
            Assert.AreEqual(6, _service.GetMaterialCount(MaterialId.CoreLimb));
        }

        [Test]
        public void test_set_and_get_weapon_tier()
        {
            Assert.AreEqual(0, _service.GetTier(WeaponId.L1));
            _service.SetWeaponTier(WeaponId.L1, 2);
            Assert.AreEqual(2, _service.GetTier(WeaponId.L1));
        }

        [Test]
        public void test_get_initial_loadout_reflects_persisted_loadout()
        {
            var loadout = _service.GetInitialLoadout();
            Assert.IsTrue(loadout.HasValue);
            Assert.AreEqual(WeaponId.L1, loadout.Value.Primary);
            Assert.AreEqual(WeaponId.M1, loadout.Value.Secondary);
        }

        // ── Event-driven stats + autosave ─────────────────────────────────────

        [Test]
        public void test_part_broke_increments_stat_and_autosaves()
        {
            int enqueuesBefore = _worker.EnqueueCount;
            long brokenBefore = _service.State.Stats.TotalPartsBroken;

            _bus.Publish(MakePartBroke(1, 1));

            Assert.AreEqual(brokenBefore + 1, _service.State.Stats.TotalPartsBroken);
            Assert.AreEqual(enqueuesBefore + 1, _worker.EnqueueCount, "PartBroke autosaves same-frame");
        }

        [Test]
        public void test_hunt_ended_full_clear_updates_run_and_clear_stats()
        {
            long runsBefore = _service.State.Stats.TotalRunsCompleted;
            long clearsBefore = _service.State.Stats.TotalFullClears;

            _bus.Publish(new HuntEnded(isAllPartsBroken: true));

            Assert.AreEqual(runsBefore + 1, _service.State.Stats.TotalRunsCompleted);
            Assert.AreEqual(clearsBefore + 1, _service.State.Stats.TotalFullClears);
        }

        [Test]
        public void test_hunt_ended_partial_updates_runs_but_not_clears()
        {
            long clearsBefore = _service.State.Stats.TotalFullClears;

            _bus.Publish(new HuntEnded(isAllPartsBroken: false));

            Assert.AreEqual(1, _service.State.Stats.TotalRunsCompleted);
            Assert.AreEqual(clearsBefore, _service.State.Stats.TotalFullClears, "partial clear grants no full-clear stat");
        }

        // ── FlushSync persists to disk ────────────────────────────────────────

        [Test]
        public void test_flush_sync_writes_latest_state_to_disk()
        {
            _service.CreditMaterials(MaterialId.EssenceKaiju, 7);

            _service.FlushSync();

            var onDisk = _ser.Deserialize(File.ReadAllText(_writer.SavePath));
            Assert.AreEqual(7, onDisk.Materials["essence_kaiju"]);
        }

        [Test]
        public void test_flush_sync_now_persists_after_stopping_worker()
        {
            _service.CreditMaterials(MaterialId.CoreEnergy, 4);

            _service.FlushSyncNow(); // stop worker + blocking write

            var onDisk = _ser.Deserialize(File.ReadAllText(_writer.SavePath));
            Assert.AreEqual(4, onDisk.Materials["core_energy"]);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static PartBroke MakePartBroke(int partId, int kaijuId) =>
            new PartBroke(partId, kaijuId, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false);
    }
}
