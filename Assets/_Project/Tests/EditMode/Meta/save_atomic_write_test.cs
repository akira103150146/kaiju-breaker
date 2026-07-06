using System.IO;
using KaijuBreaker.Content;
using KaijuBreaker.Meta;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Meta
{
    /// <summary>
    /// Meta Story 002 — Atomic temp-then-rename write + backup + depth-1 save worker
    /// (meta-progression-system.md §C.5.2/§H.4; ADR-0004). Uses a real temp directory (deterministic path,
    /// cleaned each test) and drives the worker's <see cref="SaveWorker.DrainOnce"/> seam so queue/deep-copy
    /// behaviour is verified without racing the background thread; one test exercises the live thread.
    /// </summary>
    [TestFixture]
    public sealed class SaveAtomicWriteTests
    {
        private string _dir;
        private SaveConfig _config;
        private CanonicalJsonSerializer _ser;
        private AtomicSaveWriter _writer;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "kaiju_atomic_test");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);

            _config = ScriptableObject.CreateInstance<SaveConfig>(); // defaults: player_save.json / .bak / .tmp, backup on
            _ser = new CanonicalJsonSerializer();
            _writer = new AtomicSaveWriter(_config, _dir, _ser);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // ── Fixtures ──────────────────────────────────────────────────────────

        private static SaveData SaveWithShards(long shards)
        {
            var d = new SaveData { Version = 1 };
            d.Materials["shard_common"] = shards;
            return d;
        }

        private SaveData ReadSave() => _ser.Deserialize(File.ReadAllText(_writer.SavePath));

        // ── AC-1: kill between temp write and rename ──────────────────────────

        [Test]
        public void test_kill_before_rename_leaves_prior_save_intact()
        {
            // Arrange — a committed v1 save on disk
            _writer.AtomicWrite(SaveWithShards(10));
            Assert.AreEqual(10, ReadSave().Materials["shard_common"]);

            // Act — write the new snapshot's temp file but "die" before the atomic rename
            _writer.WriteTempFile(SaveWithShards(999));

            // Assert — save.json is still the valid v1 file, never a partial write
            Assert.AreEqual(10, ReadSave().Materials["shard_common"], "live save must remain the prior complete file");
            Assert.IsTrue(File.Exists(_writer.TempPath), "temp file is left behind by the simulated kill");

            // A subsequent full write recovers cleanly, overwriting the stale temp
            _writer.AtomicWrite(SaveWithShards(999));
            Assert.AreEqual(999, ReadSave().Materials["shard_common"]);
        }

        // ── AC-2: queue overwrite (depth 1) ───────────────────────────────────

        [Test]
        public void test_queue_overwrite_persists_only_latest_snapshot()
        {
            var worker = new SaveWorker(_writer, _config.SaveWorkerIdleMs);

            worker.EnqueueSave(SaveWithShards(1));
            worker.EnqueueSave(SaveWithShards(2));
            worker.EnqueueSave(SaveWithShards(3)); // three rapid enqueues before any drain
            bool wrote = worker.DrainOnce();

            Assert.IsTrue(wrote);
            Assert.AreEqual(3, ReadSave().Materials["shard_common"], "only the newest snapshot survives");
            Assert.IsFalse(worker.HasPending, "queue is empty after drain");
        }

        // ── AC-3: deep-copy isolation ─────────────────────────────────────────

        [Test]
        public void test_enqueue_deep_copies_snapshot()
        {
            var worker = new SaveWorker(_writer, _config.SaveWorkerIdleMs);
            var live = SaveWithShards(10);
            live.KaijuRecords["CARAPEX"] = new KaijuRecordData();

            worker.EnqueueSave(live);
            // Mutate the original AFTER enqueue — must not affect the queued write
            live.Materials["shard_common"] = 99;
            live.KaijuRecords["CARAPEX"].PartsEverBroken.Add("boss_core");

            worker.DrainOnce();

            var onDisk = ReadSave();
            Assert.AreEqual(10, onDisk.Materials["shard_common"], "value at enqueue time, not the later mutation");
            CollectionAssert.IsEmpty(onDisk.KaijuRecords["CARAPEX"].PartsEverBroken, "nested list mutation must not leak");
        }

        // ── AC-4: SyncWrite blocks + valid CRC ────────────────────────────────

        [Test]
        public void test_sync_write_completes_with_valid_integrity_hash()
        {
            var worker = new SaveWorker(_writer, _config.SaveWorkerIdleMs);

            worker.SyncWrite(SaveWithShards(42));

            Assert.IsTrue(File.Exists(_writer.SavePath));
            var onDisk = ReadSave();
            Assert.AreEqual(42, onDisk.Materials["shard_common"]);
            // The stored hash must equal a recompute over the canonical body (integrity is self-consistent)
            string expected = CRC32Calculator.Compute(_ser.SerializeWithoutIntegrity(onDisk));
            Assert.AreEqual(expected, onDisk.IntegrityHash);
        }

        // ── AC-5: backup mirrors save.json ────────────────────────────────────

        [Test]
        public void test_backup_is_byte_identical_after_write()
        {
            _writer.AtomicWrite(SaveWithShards(7));

            Assert.IsTrue(File.Exists(_writer.BackupPath));
            Assert.AreEqual(
                File.ReadAllText(_writer.SavePath),
                File.ReadAllText(_writer.BackupPath),
                "backup must be byte-for-byte identical to the live save");
        }

        // ── Live worker thread integration ────────────────────────────────────

        [Test]
        public void test_worker_thread_writes_enqueued_snapshot_then_stops_cleanly()
        {
            var worker = new SaveWorker(_writer, 20);
            worker.Start();
            try
            {
                worker.EnqueueSave(SaveWithShards(55));

                // Poll (bounded) for the background write — no time-based assertion, just a completion wait.
                bool written = false;
                for (int i = 0; i < 200 && !written; i++)
                {
                    if (File.Exists(_writer.SavePath) && ReadSave().Materials["shard_common"] == 55) written = true;
                    else System.Threading.Thread.Sleep(5);
                }
                Assert.IsTrue(written, "background worker wrote the enqueued snapshot");
            }
            finally
            {
                worker.EnqueueSave(SaveWithShards(56)); // enqueue right before stop → must be flushed
                worker.Stop();
            }

            Assert.AreEqual(56, ReadSave().Materials["shard_common"], "Stop() flushes the last pending snapshot");
        }
    }
}
