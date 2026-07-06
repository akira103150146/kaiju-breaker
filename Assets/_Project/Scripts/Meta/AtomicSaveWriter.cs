using System;
using System.IO;
using System.Text;
using KaijuBreaker.Content;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Performs the atomic temp-then-rename save write (meta-progression-system.md §C.5.2; ADR-0004 §2):
    /// canonical-serialize without hash → CRC32 → embed hash → serialize final → write <c>.tmp</c> →
    /// flush to disk → rename onto <c>save.json</c> → refresh <c>save.bak.json</c>. Because the live file is
    /// only ever replaced by an atomic rename of a fully-written temp file, the disk never holds a partial
    /// or corrupt save — a kill mid-write leaves the previous complete file intact (§H.4).
    ///
    /// <para>Pure I/O with no Unity API: the save directory is injected (production passes
    /// <c>Application.persistentDataPath</c>; tests pass a temp dir), and every file name comes from
    /// <see cref="SaveConfig"/> — no hardcoded path literals (control-manifest §3 Meta).</para>
    /// </summary>
    public sealed class AtomicSaveWriter
    {
        private readonly SaveConfig _config;
        private readonly ICanonicalSerializer _serializer;
        private readonly string _saveDirectory;

        /// <summary>Full path to the live save file (<c>{dir}/{SaveFileName}</c>).</summary>
        public string SavePath { get; }

        /// <summary>Full path to the backup copy (<c>{dir}/{BackupFileName}</c>).</summary>
        public string BackupPath { get; }

        /// <summary>Full path to the intermediate temp file (<c>{dir}/{TempFileName}</c>).</summary>
        public string TempPath { get; }

        public AtomicSaveWriter(SaveConfig config, string saveDirectory, ICanonicalSerializer serializer)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _saveDirectory = saveDirectory ?? throw new ArgumentNullException(nameof(saveDirectory));

            SavePath = Path.Combine(_saveDirectory, _config.SaveFileName);
            BackupPath = Path.Combine(_saveDirectory, _config.BackupFileName);
            TempPath = Path.Combine(_saveDirectory, _config.TempFileName);
        }

        /// <summary>
        /// Execute the full atomic write for <paramref name="snapshot"/>. Mutates only the snapshot's
        /// <see cref="SaveData.IntegrityHash"/> — callers should pass a copy (the save worker does).
        /// </summary>
        public void AtomicWrite(SaveData snapshot)
        {
            WriteTempFile(snapshot);
            PromoteTempToSave();
        }

        /// <summary>
        /// Steps 1–6: embed the CRC32 and write the final canonical JSON to the temp file, flushed to disk.
        /// Returns the temp path. Split out so tests can simulate a process kill BEFORE the atomic rename.
        /// </summary>
        public string WriteTempFile(SaveData snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            Directory.CreateDirectory(_saveDirectory);

            string body = _serializer.SerializeWithoutIntegrity(snapshot); // 1
            snapshot.IntegrityHash = CRC32Calculator.Compute(body);         // 2 + 3
            string finalJson = _serializer.Serialize(snapshot);             // 4

            byte[] bytes = Encoding.UTF8.GetBytes(finalJson);
            using (var fs = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(bytes, 0, bytes.Length);       // 5
                fs.Flush(flushToDisk: true);            // 6 — fsync/FlushFileBuffers on desktop
            }
            return TempPath;
        }

        /// <summary>Steps 7–8: atomically replace the live save with the temp file, then refresh the backup.</summary>
        public void PromoteTempToSave()
        {
            // 7 — atomic within one volume. File.Replace (ReplaceFile/rename) atomically swaps an EXISTING
            // target; the first-ever write has no target, so a plain Move is correct and equally safe.
            // (Unity's .NET Standard runtime lacks the File.Move(…, overwrite) overload.)
            if (File.Exists(SavePath))
                File.Replace(TempPath, SavePath, destinationBackupFileName: null);
            else
                File.Move(TempPath, SavePath);

            if (_config.SaveBackupEnabled)
                File.Copy(SavePath, BackupPath, overwrite: true); // 8
        }
    }
}
