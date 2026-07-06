namespace KaijuBreaker.Core
{
    // Save/persistence lifecycle events. Published by Meta; consumed by the UI layer (error screen) and
    // diagnostics. Meta never calls UI directly (control-manifest §3 Meta) — it publishes here instead.

    /// <summary>
    /// on_save_corrupted — both the primary save and its backup failed CRC32 integrity validation (or were
    /// unreadable/malformed) during load (meta-progression-system.md §E.2, §H.5). The UI layer subscribes to
    /// present a non-crashing recovery screen (reset to new game, or continue with a corruption warning).
    /// <see cref="BackupAlsoFailed"/> distinguishes "primary bad, no usable backup" for diagnostics.
    /// </summary>
    public readonly struct SaveCorrupted : IGameEvent
    {
        /// <summary>True if a backup file existed but also failed validation (vs. no backup present at all).</summary>
        public readonly bool BackupAlsoFailed;

        public SaveCorrupted(bool backupAlsoFailed)
        {
            BackupAlsoFailed = backupAlsoFailed;
        }
    }

    /// <summary>
    /// on_save_ready — the Meta save service finished initialising (load → migrate → validate) and its
    /// query methods are now callable (meta-progression-system.md §C.8; Story 005). UI/meta-hub subscribe to
    /// know the persisted loadout/difficulty/materials are available to read.
    /// </summary>
    public readonly struct SaveReady : IGameEvent
    {
    }
}
