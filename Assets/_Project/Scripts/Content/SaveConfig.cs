using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Policy applied when the primary save file fails integrity validation (CRC32 mismatch).
    /// See meta-progression-system.md §E.2, §G.3.
    /// </summary>
    public enum CorruptionPolicy
    {
        /// <summary>
        /// Attempt to restore from the <c>.bak</c> backup file.
        /// If the backup also fails validation, fall through to <see cref="AlertUser"/>.
        /// Recommended default — aligns with meta-progression-system.md §G.3 integrity_fail_action.
        /// </summary>
        UseBackup,

        /// <summary>
        /// Silently clear all player progress and initialise a fresh save file.
        /// Suitable for demo / kiosk builds where continuity is not required.
        /// </summary>
        WipeAndRestart,

        /// <summary>
        /// Display an in-game error dialog and let the player choose:
        /// restore from backup, wipe and restart, or continue with the corrupted data at own risk.
        /// </summary>
        AlertUser
    }

    /// <summary>
    /// Static configuration for the save-file system: file names, backup strategy,
    /// and corruption-handling policy.
    /// <para>
    /// <b>TR-content-003 enforcement</b>: this SO describes <em>how</em> to save
    /// (file names, atomic-write strategy, corruption policy) and contains
    /// <b>no mutable player data</b>. Weapon tiers, materials, records, and settings
    /// live in the JSON save file on <c>Application.persistentDataPath</c> (ADR-0004).
    /// </para>
    /// <para>
    /// The Persistence system constructs the full path at runtime as:
    /// <c>Application.persistentDataPath + "/" + SaveFileName</c>.
    /// File names must be plain identifiers with no directory separators.
    /// </para>
    /// <para>
    /// <b>Pure static data container</b> — no read/write or file I/O logic.
    /// </para>
    /// See meta-progression-system.md §C.3, §C.5.2, §G.2, §G.3, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/SaveConfig", fileName = "SaveConfig")]
    public sealed class SaveConfig : ScriptableObject
    {
        [Header("File Names")]
        [Tooltip("Plain file name for the active player save (no path separators). " +
                 "Full path at runtime: Application.persistentDataPath + '/' + SaveFileName. " +
                 "meta-progression-system.md §C.3 default: 'player_save.json'.")]
        [SerializeField] private string _saveFileName = "player_save.json";

        [Tooltip("Plain file name for the rotating backup copy. Updated after every successful primary save " +
                 "(when BackupWriteEveryN counter is satisfied). " +
                 "meta-progression-system.md §C.3 default: 'player_save.bak.json'.")]
        [SerializeField] private string _backupFileName = "player_save.bak.json";

        [Tooltip("Plain file name used as the intermediate temp file during atomic write-then-rename. " +
                 "This file is written first, then renamed to SaveFileName (same file-system, atomic op). " +
                 "meta-progression-system.md §C.5.2 default: 'player_save.tmp.json'.")]
        [SerializeField] private string _tempFileName = "player_save.tmp.json";

        [Header("Backup Strategy")]
        [Tooltip("The backup copy is refreshed every N successful primary-save writes. " +
                 "1 = update backup on every save (safest; recommended for mobile). Must be >= 1. " +
                 "Aligns with meta-progression-system.md §G.2 save_backup_enabled = true.")]
        [SerializeField] private int _backupWriteEveryN = 1;

        [Header("Corruption Handling")]
        [Tooltip("Policy applied when CRC32 integrity validation of the primary save file fails. " +
                 "UseBackup (default): try the .bak file before alerting the player. " +
                 "meta-progression-system.md §G.3 integrity_fail_action = 'try_backup'.")]
        [SerializeField] private CorruptionPolicy _corruptionHandlingPolicy = CorruptionPolicy.UseBackup;

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>
        /// Plain file name for the active save file (no path separators).
        /// Combined with <c>Application.persistentDataPath</c> at runtime by the Persistence system.
        /// OnValidate guarantees this is non-empty and path-separator-free.
        /// </summary>
        public string SaveFileName => _saveFileName;

        /// <summary>
        /// Plain file name for the backup copy updated after every BackupWriteEveryN saves.
        /// </summary>
        public string BackupFileName => _backupFileName;

        /// <summary>
        /// Temporary file name used during atomic write-then-rename.
        /// Must be on the same file system as SaveFileName for rename to be atomic.
        /// </summary>
        public string TempFileName => _tempFileName;

        /// <summary>
        /// Backup copy is refreshed every N successful primary-save writes.
        /// 1 = every save triggers a backup refresh.
        /// </summary>
        public int BackupWriteEveryN => _backupWriteEveryN;

        /// <summary>
        /// Determines system behaviour when the primary save file fails CRC32 integrity validation.
        /// Default: <see cref="CorruptionPolicy.UseBackup"/>.
        /// </summary>
        public CorruptionPolicy CorruptionHandlingPolicy => _corruptionHandlingPolicy;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_saveFileName))
            {
                Debug.LogError(
                    $"[SaveConfig] '{name}': SaveFileName must not be empty.", this);
            }
            else if (_saveFileName.IndexOf('/') >= 0 || _saveFileName.IndexOf('\\') >= 0)
            {
                Debug.LogError(
                    $"[SaveConfig] '{name}': SaveFileName must be a plain file name with no path separators " +
                    $"('/' or '\\'). Actual path is constructed at runtime from Application.persistentDataPath. " +
                    $"Current value: '{_saveFileName}'.", this);
            }

            if (_backupWriteEveryN < 1)
                Debug.LogError(
                    $"[SaveConfig] '{name}': BackupWriteEveryN must be >= 1. " +
                    $"Current: {_backupWriteEveryN}.", this);
        }
    }
}
