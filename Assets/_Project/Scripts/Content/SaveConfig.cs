using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Integrity checksum algorithm for the save file (meta-progression-system.md §G.3).
    /// CRC32 is the shipping default; SHA1 is reserved as a future upgrade option.
    /// </summary>
    public enum IntegrityAlgorithm
    {
        /// <summary>IEEE 802.3 CRC-32 — fast, detects accidental disk corruption (2⁻³² collision).</summary>
        Crc32,

        /// <summary>SHA-1 — reserved for a future upgrade; not implemented in the MVP.</summary>
        Sha1
    }

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

        [Tooltip("Integrity checksum algorithm (meta-progression-system.md §G.3). CRC32 is the shipping default.")]
        [SerializeField] private IntegrityAlgorithm _integrityAlgorithm = IntegrityAlgorithm.Crc32;

        [Header("Async Write Behaviour (G.2)")]
        [Tooltip("Async write queue depth. 1 = latest-snapshot overwrite (safest for mobile). Range [1, 3]. " +
                 "meta-progression-system.md §G.2 save_async_queue_depth.")]
        [SerializeField] private int _saveAsyncQueueDepth = 1;

        [Tooltip("Background save worker idle poll interval, milliseconds. Range [50, 500]. " +
                 "meta-progression-system.md §G.2 save_worker_idle_ms.")]
        [SerializeField] private int _saveWorkerIdleMs = 100;

        [Tooltip("Whether the backup copy save.bak.json is maintained (meta-progression-system.md §G.2 save_backup_enabled).")]
        [SerializeField] private bool _saveBackupEnabled = true;

        [Header("Migration (G.2)")]
        [Tooltip("Maximum version gap the migration chain will bridge. Range [2, 5]. Older saves are refused. " +
                 "meta-progression-system.md §G.2 save_max_migration_generations.")]
        [SerializeField] private int _saveMaxMigrationGenerations = 3;

        [Header("New Game / Tracked Content (G.1, G.4)")]
        [Tooltip("Weapons owned from a fresh save (meta-progression-system.md §G.1 starting_owned_weapons). Default: L1, M1.")]
        [SerializeField] private WeaponId[] _startingOwnedWeapons = { WeaponId.L1, WeaponId.M1 };

        [Tooltip("Weapon ids tracked in the save's weapons{} map (meta-progression-system.md §G.4 active_weapon_ids). " +
                 "Full version = all 8; a fresh save initialises every tracked weapon (owned only if in StartingOwnedWeapons).")]
        [SerializeField] private WeaponId[] _activeWeaponIds =
            { WeaponId.L1, WeaponId.L2, WeaponId.L3, WeaponId.L4, WeaponId.M1, WeaponId.M2, WeaponId.M3, WeaponId.M4 };

        [Tooltip("Kaiju ids initialised in the save's kaiju_records{} map (meta-progression-system.md §G.4 active_kaiju_ids).")]
        [SerializeField] private string[] _activeKaijuIds = { "CARAPEX", "LACERA", "VOLTWYRM" };

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

        /// <summary>Integrity checksum algorithm (meta-progression-system.md §G.3). Default CRC32.</summary>
        public IntegrityAlgorithm IntegrityAlgorithm => _integrityAlgorithm;

        /// <summary>Async write queue depth (meta-progression-system.md §G.2). Range [1, 3].</summary>
        public int SaveAsyncQueueDepth => _saveAsyncQueueDepth;

        /// <summary>Background save worker idle poll interval in ms (meta-progression-system.md §G.2). Range [50, 500].</summary>
        public int SaveWorkerIdleMs => _saveWorkerIdleMs;

        /// <summary>Whether the backup copy is maintained (meta-progression-system.md §G.2 save_backup_enabled).</summary>
        public bool SaveBackupEnabled => _saveBackupEnabled;

        /// <summary>Maximum migration version gap the chain will bridge (meta-progression-system.md §G.2). Range [2, 5].</summary>
        public int SaveMaxMigrationGenerations => _saveMaxMigrationGenerations;

        /// <summary>Weapons owned from a fresh save (meta-progression-system.md §G.1 starting_owned_weapons).</summary>
        public WeaponId[] StartingOwnedWeapons => _startingOwnedWeapons;

        /// <summary>Weapon ids tracked in the save's weapons{} map (meta-progression-system.md §G.4 active_weapon_ids).</summary>
        public WeaponId[] ActiveWeaponIds => _activeWeaponIds;

        /// <summary>Kaiju ids initialised in the save's kaiju_records{} map (meta-progression-system.md §G.4 active_kaiju_ids).</summary>
        public string[] ActiveKaijuIds => _activeKaijuIds;

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
                    $"[SaveConfig] '{name}': BackupWriteEveryN must be >= 1. Current: {_backupWriteEveryN}.", this);

            if (_saveAsyncQueueDepth < 1 || _saveAsyncQueueDepth > 3)
                Debug.LogError(
                    $"[SaveConfig] '{name}': SaveAsyncQueueDepth must be in [1, 3]. Current: {_saveAsyncQueueDepth}.", this);

            if (_saveWorkerIdleMs < 50 || _saveWorkerIdleMs > 500)
                Debug.LogError(
                    $"[SaveConfig] '{name}': SaveWorkerIdleMs must be in [50, 500]. Current: {_saveWorkerIdleMs}.", this);

            if (_saveMaxMigrationGenerations < 2 || _saveMaxMigrationGenerations > 5)
                Debug.LogError(
                    $"[SaveConfig] '{name}': SaveMaxMigrationGenerations must be in [2, 5]. " +
                    $"Current: {_saveMaxMigrationGenerations}.", this);
        }
    }
}
