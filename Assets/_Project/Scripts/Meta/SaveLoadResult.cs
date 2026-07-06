namespace KaijuBreaker.Meta
{
    /// <summary>Outcome category of a <see cref="SaveLoader.LoadOrDefault"/> call (meta-progression-system.md §C.4/§E.2/§E.3).</summary>
    public enum SaveLoadStatus
    {
        /// <summary>Primary save loaded and passed CRC32 integrity.</summary>
        Success,

        /// <summary>Primary failed; the backup loaded and passed integrity.</summary>
        RestoredFromBackup,

        /// <summary>No save file present — a fresh new-game default was built (not yet written to disk).</summary>
        NewGame,

        /// <summary>Both primary and backup failed — recovery required (a <see cref="Core.SaveCorrupted"/> event was published).</summary>
        Corrupted,

        /// <summary>The save came from a newer app version than this build supports — refuse to load or migrate.</summary>
        VersionTooNew
    }

    /// <summary>
    /// The result of loading the save (meta-progression-system.md §C.4 discriminated union). Carries the
    /// loaded/defaulted <see cref="Data"/> for the successful/new-game paths, the offending version for
    /// <see cref="SaveLoadStatus.VersionTooNew"/>, and a runtime <see cref="IntegrityWarning"/> flag set when
    /// the player chooses to continue past corruption.
    /// </summary>
    public sealed class SaveLoadResult
    {
        public SaveLoadStatus Status { get; }

        /// <summary>The usable save data (null for <see cref="SaveLoadStatus.Corrupted"/> / <see cref="SaveLoadStatus.VersionTooNew"/>).</summary>
        public SaveData Data { get; }

        /// <summary>The save's version, meaningful only for <see cref="SaveLoadStatus.VersionTooNew"/>.</summary>
        public int SaveVersion { get; }

        /// <summary>True when the data was force-loaded past a corruption warning (Continue-with-corruption path).</summary>
        public bool IntegrityWarning { get; internal set; }

        private SaveLoadResult(SaveLoadStatus status, SaveData data, int saveVersion)
        {
            Status = status;
            Data = data;
            SaveVersion = saveVersion;
        }

        public static SaveLoadResult Success(SaveData data) => new SaveLoadResult(SaveLoadStatus.Success, data, data.Version);
        public static SaveLoadResult RestoredFromBackup(SaveData data) => new SaveLoadResult(SaveLoadStatus.RestoredFromBackup, data, data.Version);
        public static SaveLoadResult NewGame(SaveData data) => new SaveLoadResult(SaveLoadStatus.NewGame, data, data.Version);
        public static SaveLoadResult Corrupted() => new SaveLoadResult(SaveLoadStatus.Corrupted, null, 0);
        public static SaveLoadResult VersionTooNew(int saveVersion) => new SaveLoadResult(SaveLoadStatus.VersionTooNew, null, saveVersion);
    }
}
