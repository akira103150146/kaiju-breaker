using System;
using System.IO;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Loads the save with CRC32 integrity verification and corruption recovery (meta-progression-system.md
    /// §C.4/§D.2/§E.2/§E.3; ADR-0004 §3). Order: primary → backup → new-game/corrupted. Never throws out of
    /// <see cref="LoadOrDefault"/> — any unexpected error resolves to <see cref="SaveLoadStatus.Corrupted"/>
    /// and publishes a <see cref="SaveCorrupted"/> event so the UI can present a non-crashing recovery screen
    /// (Meta never calls UI directly, control-manifest §3).
    /// </summary>
    public sealed class SaveLoader
    {
        private readonly SaveConfig _config;
        private readonly ICanonicalSerializer _serializer;
        private readonly IEventBus _bus;
        private readonly AtomicSaveWriter _writer;

        public SaveLoader(SaveConfig config, ICanonicalSerializer serializer, IEventBus bus, AtomicSaveWriter writer)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        private string SavePath => _writer.SavePath;
        private string BackupPath => _writer.BackupPath;

        /// <summary>
        /// Load the save, verify integrity, and recover as needed. See the class summary for the resolution
        /// order. The returned <see cref="SaveLoadResult"/> is always non-null and the call never throws.
        /// </summary>
        public SaveLoadResult LoadOrDefault()
        {
            try
            {
                bool primaryExists = File.Exists(SavePath);
                bool backupExists = File.Exists(BackupPath);

                if (!primaryExists && !backupExists)
                    return SaveLoadResult.NewGame(NewGameFactory.Create(_config));

                if (primaryExists)
                {
                    var primary = TryLoadVerified(SavePath);
                    if (primary != null)
                        return primary.Version > SaveData.CurrentVersion
                            ? SaveLoadResult.VersionTooNew(primary.Version)
                            : SaveLoadResult.Success(primary);
                }

                if (backupExists)
                {
                    var backup = TryLoadVerified(BackupPath);
                    if (backup != null)
                        return backup.Version > SaveData.CurrentVersion
                            ? SaveLoadResult.VersionTooNew(backup.Version)
                            : SaveLoadResult.RestoredFromBackup(backup);
                }

                // Something existed but nothing validated → corruption.
                _bus.Publish(new SaveCorrupted(backupAlsoFailed: backupExists));
                return SaveLoadResult.Corrupted();
            }
            catch (Exception)
            {
                // Non-crash guarantee: any I/O/parse error still resolves to a recoverable Corrupted result.
                _bus.Publish(new SaveCorrupted(backupAlsoFailed: false));
                return SaveLoadResult.Corrupted();
            }
        }

        /// <summary>Read + deserialize + integrity-verify a file; null if missing, malformed, or hash-mismatched.</summary>
        private SaveData TryLoadVerified(string path)
        {
            if (!File.Exists(path)) return null;
            SaveData data;
            try
            {
                data = _serializer.Deserialize(File.ReadAllText(path));
            }
            catch (Exception)
            {
                return null; // malformed / truncated JSON
            }
            return VerifyIntegrity(data) ? data : null;
        }

        /// <summary>
        /// Recompute <c>CRC32(canonical_json(D \ {integrity_hash}))</c> and compare to the stored hash
        /// (case-insensitive hex) — the §D.2 validation. The hash field is excluded via the serializer,
        /// never by string manipulation.
        /// </summary>
        public bool VerifyIntegrity(SaveData data)
        {
            if (data == null) return false;
            string computed = CRC32Calculator.Compute(_serializer.SerializeWithoutIntegrity(data));
            return string.Equals(computed, data.IntegrityHash, StringComparison.OrdinalIgnoreCase);
        }

        // ── Recovery paths (called by the UI after a Corrupted result) ─────────

        /// <summary>
        /// Delete both save files, build new-game defaults, and synchronously write them
        /// (meta-progression-system.md §E.2 reset path). Returns the fresh <see cref="SaveLoadStatus.NewGame"/> result.
        /// </summary>
        public SaveLoadResult ResetToNewGame()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);

            var fresh = NewGameFactory.Create(_config);
            _writer.AtomicWrite(fresh.DeepCopy()); // synchronous initial write
            return SaveLoadResult.NewGame(fresh);
        }

        /// <summary>
        /// Force-continue with best-effort data past a corruption warning (§E.2 continue path). Flags
        /// <see cref="SaveLoadResult.IntegrityWarning"/> so downstream systems/UI know the data is suspect.
        /// </summary>
        public SaveLoadResult ContinueWithCorruption(SaveData forcedData)
        {
            var result = SaveLoadResult.Success(forcedData ?? NewGameFactory.Create(_config));
            result.IntegrityWarning = true;
            return result;
        }
    }
}
