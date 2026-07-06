namespace KaijuBreaker.Meta
{
    /// <summary>Outcome of <see cref="SaveMigrator.Migrate"/> (meta-progression-system.md §C.4).</summary>
    public enum MigrationStatus
    {
        /// <summary>Save is already at the current version — returned unchanged, no autosave needed.</summary>
        NotNeeded,

        /// <summary>Save was older and was migrated forward — the caller should persist it once.</summary>
        Migrated,

        /// <summary>Save is older than <c>SaveMaxMigrationGenerations</c> allows — refuse; file left untouched.</summary>
        TooOld
    }

    /// <summary>Result of a migration attempt: the (possibly migrated) data plus the outcome category.</summary>
    public sealed class MigrationResult
    {
        public MigrationStatus Status { get; }

        /// <summary>The current/migrated save data (null for <see cref="MigrationStatus.TooOld"/>).</summary>
        public SaveData Data { get; }

        private MigrationResult(MigrationStatus status, SaveData data)
        {
            Status = status;
            Data = data;
        }

        public static MigrationResult NotNeeded(SaveData data) => new MigrationResult(MigrationStatus.NotNeeded, data);
        public static MigrationResult Migrated(SaveData data) => new MigrationResult(MigrationStatus.Migrated, data);
        public static MigrationResult TooOld() => new MigrationResult(MigrationStatus.TooOld, null);
    }
}
