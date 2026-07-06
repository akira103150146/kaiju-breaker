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
    /// Meta Story 003 — CRC32 integrity load + backup recovery + version guard (meta-progression-system.md
    /// §D.2/§E.2/§E.3/§H.5; ADR-0004). Real temp-dir I/O; a <see cref="RecordingEventBus"/> captures the
    /// <see cref="SaveCorrupted"/> event. New-game defaults come from <see cref="NewGameFactory"/> (shared
    /// with Story 005).
    /// </summary>
    [TestFixture]
    public sealed class SaveIntegrityLoadTests
    {
        private string _dir;
        private SaveConfig _config;
        private CanonicalJsonSerializer _ser;
        private RecordingEventBus _bus;
        private AtomicSaveWriter _writer;
        private SaveLoader _loader;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "kaiju_load_test");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);

            _config = ScriptableObject.CreateInstance<SaveConfig>();
            _ser = new CanonicalJsonSerializer();
            _bus = new RecordingEventBus();
            _writer = new AtomicSaveWriter(_config, _dir, _ser);
            _loader = new SaveLoader(_config, _ser, _bus, _writer);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static SaveData SaveWithShards(long shards, int version = 1)
        {
            var d = new SaveData { Version = version };
            d.Materials["shard_common"] = shards;
            return d;
        }

        // ── AC-1: valid load ──────────────────────────────────────────────────

        [Test]
        public void test_valid_save_loads_success()
        {
            _writer.AtomicWrite(SaveWithShards(10));

            var result = _loader.LoadOrDefault();

            Assert.AreEqual(SaveLoadStatus.Success, result.Status);
            Assert.AreEqual(10, result.Data.Materials["shard_common"]);
        }

        // ── AC-2: corrupt primary → restore from backup ───────────────────────

        [Test]
        public void test_corrupt_primary_restores_from_backup()
        {
            _writer.AtomicWrite(SaveWithShards(10)); // save.json + save.bak.json both valid @10

            // Tamper the primary only: change the value so its stored hash no longer matches.
            string tampered = File.ReadAllText(_writer.SavePath).Replace("\"shard_common\":10", "\"shard_common\":11");
            File.WriteAllText(_writer.SavePath, tampered);

            var result = _loader.LoadOrDefault();

            Assert.AreEqual(SaveLoadStatus.RestoredFromBackup, result.Status);
            Assert.AreEqual(10, result.Data.Materials["shard_common"], "backup value, not the tampered 11");
            Assert.AreEqual(0, _bus.CountOf<SaveCorrupted>(), "backup recovery is not a corruption event");
        }

        // ── AC-3: both corrupt → Corrupted + event, no crash ──────────────────

        [Test]
        public void test_both_files_corrupt_returns_corrupted_and_publishes_event()
        {
            _writer.AtomicWrite(SaveWithShards(10));
            File.WriteAllText(_writer.SavePath, "{ this is not valid json");
            File.WriteAllText(_writer.BackupPath, "");   // zero-byte backup

            var result = _loader.LoadOrDefault();

            Assert.AreEqual(SaveLoadStatus.Corrupted, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual(1, _bus.CountOf<SaveCorrupted>(), "UI is notified via a SaveCorrupted event");
            Assert.IsTrue(_bus.Events<SaveCorrupted>()[0].BackupAlsoFailed);
        }

        // ── AC-4: first launch → NewGame defaults ─────────────────────────────

        [Test]
        public void test_first_launch_returns_new_game_defaults()
        {
            var result = _loader.LoadOrDefault(); // no files on disk

            Assert.AreEqual(SaveLoadStatus.NewGame, result.Status);
            Assert.AreEqual(1, result.Data.Version);
            Assert.IsTrue(result.Data.Weapons["L1"].Owned, "L1 owned from a fresh save");
            Assert.IsTrue(result.Data.Weapons["M1"].Owned, "M1 owned from a fresh save");
            Assert.IsFalse(result.Data.Weapons["L2"].Owned, "L2 not owned from a fresh save");
            Assert.AreEqual(0, result.Data.Materials["shard_common"]);
            Assert.IsFalse(result.Data.Meta.FirstLaunchComplete);
        }

        [Test]
        public void test_backup_only_present_is_recovered_before_new_game()
        {
            _writer.AtomicWrite(SaveWithShards(20));
            File.Delete(_writer.SavePath); // only the backup survives

            var result = _loader.LoadOrDefault();

            Assert.AreEqual(SaveLoadStatus.RestoredFromBackup, result.Status);
            Assert.AreEqual(20, result.Data.Materials["shard_common"]);
        }

        // ── AC-5: VerifyIntegrity over the correct subset ─────────────────────

        [Test]
        public void test_verify_integrity_true_for_matching_hash_false_when_altered()
        {
            var data = SaveWithShards(5);
            data.IntegrityHash = CRC32Calculator.Compute(_ser.SerializeWithoutIntegrity(data));
            Assert.IsTrue(_loader.VerifyIntegrity(data));

            data.IntegrityHash = "DEADBEEF";
            Assert.IsFalse(_loader.VerifyIntegrity(data));
        }

        // ── AC-6: version too new → refuse, file untouched ────────────────────

        [Test]
        public void test_version_too_new_refuses_and_leaves_file_untouched()
        {
            _writer.AtomicWrite(SaveWithShards(10, version: 99)); // valid CRC for a v99 file
            string before = File.ReadAllText(_writer.SavePath);

            var result = _loader.LoadOrDefault();

            Assert.AreEqual(SaveLoadStatus.VersionTooNew, result.Status);
            Assert.AreEqual(99, result.SaveVersion);
            Assert.AreEqual(before, File.ReadAllText(_writer.SavePath), "load must not modify the save file");
        }

        // ── Recovery paths ────────────────────────────────────────────────────

        [Test]
        public void test_reset_to_new_game_clears_and_writes_defaults()
        {
            _writer.AtomicWrite(SaveWithShards(500));

            var result = _loader.ResetToNewGame();

            Assert.AreEqual(SaveLoadStatus.NewGame, result.Status);
            Assert.AreEqual(0, result.Data.Materials["shard_common"]);
            // The fresh defaults were synchronously written and re-load cleanly.
            var reloaded = _loader.LoadOrDefault();
            Assert.AreEqual(SaveLoadStatus.Success, reloaded.Status);
            Assert.IsTrue(reloaded.Data.Weapons["L1"].Owned);
        }

        [Test]
        public void test_continue_with_corruption_flags_integrity_warning()
        {
            var forced = SaveWithShards(3);
            var result = _loader.ContinueWithCorruption(forced);

            Assert.AreEqual(SaveLoadStatus.Success, result.Status);
            Assert.IsTrue(result.IntegrityWarning);
            Assert.AreEqual(3, result.Data.Materials["shard_common"]);
        }
    }
}
