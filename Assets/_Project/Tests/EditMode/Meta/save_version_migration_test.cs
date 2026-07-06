using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Meta;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Meta
{
    /// <summary>
    /// Meta Story 004 — save version migration chain (meta-progression-system.md §C.4/§H.6; ADR-0004).
    /// The executor is pure and the current-version + registry are injected, so a synthetic future version
    /// and a test-only migration function exercise the chain without shipping a real v2.
    ///
    /// <para><b>Reconciliation:</b> AC-5 (post-migration autosave fires exactly once) is a
    /// MetaSaveService.Initialize wiring concern (Story 006) — that composition point does not exist yet.
    /// This suite verifies the DECISION the orchestrator keys off (Migrated vs NotNeeded status); the actual
    /// EnqueueSave-once call is asserted in Story 006.</para>
    /// </summary>
    [TestFixture]
    public sealed class SaveVersionMigrationTests
    {
        private SaveConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<SaveConfig>(); // SaveMaxMigrationGenerations = 3

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        private static SaveData SaveAtVersion(int version)
        {
            var d = new SaveData { Version = version };
            d.Stats.TotalPartsBroken = 87; // a pre-existing field to prove preservation
            return d;
        }

        // ── AC-1: v1 in v1 app → NotNeeded, same object ───────────────────────

        [Test]
        public void test_current_version_save_needs_no_migration()
        {
            var data = SaveAtVersion(SaveData.CurrentVersion);
            var migrator = SaveMigrator.Default();

            var result = migrator.Migrate(data, _config);

            Assert.AreEqual(MigrationStatus.NotNeeded, result.Status);
            Assert.AreSame(data, result.Data, "NotNeeded returns the same object, no copy");
            Assert.AreEqual(87, result.Data.Stats.TotalPartsBroken);
        }

        // ── AC-2: gap exceeds SaveMaxMigrationGenerations → TooOld ─────────────

        [Test]
        public void test_gap_beyond_max_generations_is_too_old()
        {
            // CURRENT = 5, save at v1 → gap 4 > config default 3
            var migrator = new SaveMigrator(currentVersion: 5, NoMigrations());
            var data = SaveAtVersion(1);

            var result = migrator.Migrate(data, _config);

            Assert.AreEqual(MigrationStatus.TooOld, result.Status);
            Assert.IsNull(result.Data, "no data returned for TooOld");
            Assert.AreEqual(1, data.Version, "the input save is not modified");
        }

        [Test]
        public void test_gap_exactly_at_max_generations_migrates()
        {
            // CURRENT = 4, save v1 → gap 3 == max → allowed. Provide identity migrations for 2,3,4.
            var migrations = new Dictionary<int, Func<SaveData, SaveConfig, SaveData>>
            {
                { 2, Identity }, { 3, Identity }, { 4, Identity },
            };
            var migrator = new SaveMigrator(currentVersion: 4, migrations);

            var result = migrator.Migrate(SaveAtVersion(1), _config);

            Assert.AreEqual(MigrationStatus.Migrated, result.Status);
            Assert.AreEqual(4, result.Data.Version);
            Assert.AreEqual(87, result.Data.Stats.TotalPartsBroken, "original fields preserved through the chain");
        }

        // ── AC-3: test-only v2 migration applies + preserves ──────────────────

        [Test]
        public void test_v1_to_v2_migration_fills_new_field_and_preserves_existing()
        {
            // A test-only migration standing in for a future one: it fills a "new" field (TotalPlayTimeSeconds)
            // from a default while preserving all existing data.
            var migrations = new Dictionary<int, Func<SaveData, SaveConfig, SaveData>>
            {
                { 2, (data, cfg) =>
                    {
                        var next = data.DeepCopy();          // pure: never mutate the input
                        next.Stats.TotalPlayTimeSeconds = 0; // "new field" default
                        return next;
                    }
                },
            };
            var migrator = new SaveMigrator(currentVersion: 2, migrations);

            var result = migrator.Migrate(SaveAtVersion(1), _config);

            Assert.AreEqual(MigrationStatus.Migrated, result.Status);
            Assert.AreEqual(2, result.Data.Version);
            Assert.AreEqual(0, result.Data.Stats.TotalPlayTimeSeconds);
            Assert.AreEqual(87, result.Data.Stats.TotalPartsBroken, "existing field preserved");
        }

        // ── AC-4: migration function purity ───────────────────────────────────

        [Test]
        public void test_migration_function_is_pure_and_does_not_mutate_input()
        {
            Func<SaveData, SaveConfig, SaveData> fn = (data, cfg) =>
            {
                var next = data.DeepCopy();
                next.Stats.TotalFullClears = 3;
                return next;
            };
            var input = SaveAtVersion(1);

            var out1 = fn(input, _config);
            var out2 = fn(input, _config);

            Assert.AreEqual(out1.Stats.TotalFullClears, out2.Stats.TotalFullClears, "same input → same output");
            Assert.AreEqual(0, input.Stats.TotalFullClears, "input must not be mutated by the migration");
        }

        // ── AC-5 (decision only): status drives the orchestrator's save call ──

        [Test]
        public void test_status_signals_whether_orchestrator_should_persist()
        {
            var noMigration = SaveMigrator.Default().Migrate(SaveAtVersion(SaveData.CurrentVersion), _config);
            Assert.AreEqual(MigrationStatus.NotNeeded, noMigration.Status,
                "NotNeeded → orchestrator must NOT autosave (Story 006 wiring keys off this)");

            var migrator = new SaveMigrator(2, new Dictionary<int, Func<SaveData, SaveConfig, SaveData>> { { 2, Identity } });
            var migrated = migrator.Migrate(SaveAtVersion(1), _config);
            Assert.AreEqual(MigrationStatus.Migrated, migrated.Status,
                "Migrated → orchestrator autosaves exactly once");
        }

        // ── Baseline: v1 registry is empty ────────────────────────────────────

        [Test]
        public void test_default_registry_is_empty_at_v1()
        {
            Assert.AreEqual(0, SaveMigrator.Registry.Count,
                "MIGRATIONS must be empty at v1 — a non-empty registry signals an unreviewed version bump");
        }

        [Test]
        public void test_newer_than_current_save_throws_contract_violation()
        {
            var migrator = SaveMigrator.Default(); // current = 1
            Assert.Throws<InvalidOperationException>(() => migrator.Migrate(SaveAtVersion(99), _config));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static SaveData Identity(SaveData data, SaveConfig cfg) => data.DeepCopy();

        private static Dictionary<int, Func<SaveData, SaveConfig, SaveData>> NoMigrations() =>
            new Dictionary<int, Func<SaveData, SaveConfig, SaveData>>();
    }
}
