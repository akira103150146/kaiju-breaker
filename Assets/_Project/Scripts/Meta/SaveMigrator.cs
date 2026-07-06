using System;
using System.Collections.Generic;
using KaijuBreaker.Content;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Applies the pure-function version-migration chain to an older save (meta-progression-system.md §C.4;
    /// ADR-0004 §4). While <c>data.Version &lt; CurrentVersion</c>, it runs <c>Migrations[version+1]</c> and
    /// bumps the version, one generation at a time, until the schema matches. Saves older than
    /// <see cref="SaveConfig.SaveMaxMigrationGenerations"/> are refused (<see cref="MigrationStatus.TooOld"/>).
    ///
    /// <para>Each migration function is a PURE transform <c>(SaveData, SaveConfig) → SaveData</c> — no I/O,
    /// no event publishing, missing fields filled from <see cref="SaveConfig"/> defaults (not hardcoded).
    /// The post-migration autosave is the orchestrator's job (MetaSaveService.Initialize, Story 006), never
    /// this class's. The v1 registry is intentionally empty; adding <c>Migrations[2]</c> later needs no
    /// change here.</para>
    ///
    /// <para><see cref="CurrentVersion"/> and the migration registry are injected so the chain executor is
    /// unit-testable with a synthetic future version and test-only migration functions.</para>
    /// </summary>
    public sealed class SaveMigrator
    {
        private readonly IReadOnlyDictionary<int, Func<SaveData, SaveConfig, SaveData>> _migrations;

        /// <summary>The schema version this migrator targets (production: <see cref="SaveData.CurrentVersion"/>).</summary>
        public int CurrentVersion { get; }

        public SaveMigrator(int currentVersion, IReadOnlyDictionary<int, Func<SaveData, SaveConfig, SaveData>> migrations)
        {
            if (currentVersion < 1) throw new ArgumentOutOfRangeException(nameof(currentVersion));
            CurrentVersion = currentVersion;
            _migrations = migrations ?? new Dictionary<int, Func<SaveData, SaveConfig, SaveData>>();
        }

        /// <summary>The production migrator: current schema version + the (currently empty) real registry.</summary>
        public static SaveMigrator Default() =>
            new SaveMigrator(SaveData.CurrentVersion, Registry);

        /// <summary>The real, shipping migration registry. Empty at v1; future entries: <c>{ 2, MigrateV1ToV2 }</c>.</summary>
        public static readonly IReadOnlyDictionary<int, Func<SaveData, SaveConfig, SaveData>> Registry =
            new Dictionary<int, Func<SaveData, SaveConfig, SaveData>>();

        /// <summary>
        /// Migrate <paramref name="raw"/> forward to <see cref="CurrentVersion"/> if needed.
        /// Caller must have already rejected newer-than-current saves (Story 003 VersionTooNew) — passing one
        /// here is a contract violation and throws.
        /// </summary>
        public MigrationResult Migrate(SaveData raw, SaveConfig config)
        {
            if (raw == null) throw new ArgumentNullException(nameof(raw));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (raw.Version > CurrentVersion)
                throw new InvalidOperationException(
                    $"Migrate called with a newer-than-current save (v{raw.Version} > v{CurrentVersion}); " +
                    "the caller must handle VersionTooNew first.");

            if (raw.Version == CurrentVersion)
                return MigrationResult.NotNeeded(raw);

            int gap = CurrentVersion - raw.Version;
            if (gap > config.SaveMaxMigrationGenerations)
                return MigrationResult.TooOld();

            SaveData data = raw;
            while (data.Version < CurrentVersion)
            {
                int target = data.Version + 1;
                if (!_migrations.TryGetValue(target, out var migrate))
                    throw new InvalidOperationException($"No migration function registered for v{target}.");

                data = migrate(data, config);   // pure transform → new SaveData at v(target) schema
                data.Version = target;          // version bump owned by the executor, not the function
            }
            return MigrationResult.Migrated(data);
        }
    }
}
