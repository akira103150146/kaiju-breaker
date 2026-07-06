using System;
using System.Linq;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Composition + lifecycle owner of the persistent save (meta-progression-system.md §C.1/§C.7/§C.8;
    /// ADR-0004). Holds the single in-memory persistent <see cref="SaveData"/> and orchestrates load →
    /// migrate → validate → ready. Per-run state (score, timers, in-run heat/break progress) NEVER lives
    /// here — that is the Stage system's, and it is never persisted (§C.1 boundary).
    ///
    /// <para>Story 005 scope: new-game init, last-loadout fallback, difficulty/loadout persistence via the
    /// <see cref="LoadoutConfirmed"/> event, and the initialisation ordering. The full
    /// <see cref="ISaveService"/> / <see cref="IWeaponTierQuery"/> surface + material crediting arrive in
    /// Story 006; weapon-ownership persistence in Story 007.</para>
    ///
    /// <para><b>Reconciliation:</b> the read methods here (<see cref="GetLastDifficulty"/> /
    /// <see cref="GetLastLoadout"/>) are concrete on this service, NOT added to the committed
    /// <see cref="ISaveService"/> interface (owned by the economy epic). Deciding the final ISaveService
    /// surface is Story 006's job.</para>
    /// </summary>
    public sealed class MetaSaveService
    {
        private readonly SaveConfig _config;
        private readonly IEventBus _bus;
        private readonly SaveLoader _loader;
        private readonly SaveMigrator _migrator;
        private readonly SaveWorker _worker;
        private readonly Action<LoadoutConfirmed> _onLoadoutConfirmed;

        private SaveData _state;
        private bool _initialized;

        public MetaSaveService(SaveConfig config, IEventBus bus, SaveLoader loader, SaveMigrator migrator, SaveWorker worker)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));
            _worker = worker ?? throw new ArgumentNullException(nameof(worker));
            _onLoadoutConfirmed = OnLoadoutConfirmed;
        }

        /// <summary>True once <see cref="Initialize"/> has completed and query methods are callable.</summary>
        public bool IsInitialized => _initialized;

        /// <summary>The live persistent state (for Story 006/007 to extend + tests). Null before init.</summary>
        public SaveData State => _state;

        /// <summary>
        /// Load → migrate → validate the save and mark the service ready. Order (meta-progression-system.md
        /// §C.8): first launch / corruption / newer-version → fresh new game; otherwise migrate (autosaving
        /// once if it changed) then validate the last loadout. Subscribes <see cref="LoadoutConfirmed"/> and
        /// publishes <see cref="SaveReady"/>.
        /// </summary>
        public void Initialize()
        {
            var result = _loader.LoadOrDefault();
            switch (result.Status)
            {
                case SaveLoadStatus.NewGame:
                case SaveLoadStatus.Corrupted:     // loader already published SaveCorrupted; keep the game runnable
                case SaveLoadStatus.VersionTooNew: // save from a newer build — cannot use it; start fresh
                    _state = InitializeNewGame();
                    break;

                case SaveLoadStatus.Success:
                case SaveLoadStatus.RestoredFromBackup:
                    var migration = _migrator.Migrate(result.Data, _config);
                    if (migration.Status == MigrationStatus.Migrated)
                    {
                        _state = migration.Data;
                        _worker.EnqueueSave(_state); // persist the migrated version once (§C.4)
                    }
                    else if (migration.Status == MigrationStatus.TooOld)
                    {
                        _state = InitializeNewGame(); // beyond the migration window — safe reset
                    }
                    else // NotNeeded
                    {
                        _state = result.Data;
                    }
                    ValidateLastLoadout(_state, _config);
                    break;
            }

            _bus.Subscribe(_onLoadoutConfirmed);
            _initialized = true;
            _bus.Publish(new SaveReady());
        }

        /// <summary>Unsubscribe on teardown (App owns lifetime).</summary>
        public void Dispose() => _bus.Unsubscribe(_onLoadoutConfirmed);

        /// <summary>
        /// Build a fresh new-game <see cref="SaveData"/> from <see cref="SaveConfig"/> defaults and write it
        /// synchronously (meta-progression-system.md §C.7). Returns the new state.
        /// </summary>
        public SaveData InitializeNewGame()
        {
            var data = NewGameFactory.Create(_config);
            _worker.SyncWrite(data); // initial persist through Story 002's sync path
            return data;
        }

        /// <summary>
        /// Repair a loadout that points at an unowned weapon (meta-progression-system.md §E.5): the primary
        /// falls back to the first owned laser (L1 from the starting state), the secondary to the first owned
        /// missile (M1). Mutates <paramref name="data"/>'s loadout in place.
        /// </summary>
        public void ValidateLastLoadout(SaveData data, SaveConfig config)
        {
            var loadout = data.Meta.LastLoadout;
            if (!IsOwned(data, loadout.Primary))
                loadout.Primary = FirstOwnedInPool(data, config, "L", fallback: "L1");
            if (!IsOwned(data, loadout.Secondary))
                loadout.Secondary = FirstOwnedInPool(data, config, "M", fallback: "M1");
        }

        // ── Persisted read queries (concrete; ISaveService surface decided in Story 006) ──

        /// <summary>Last selected difficulty ("D1".."D4") to prefill the loadout screen.</summary>
        public string GetLastDifficulty() { EnsureReady(); return _state.Meta.LastSelectedDifficulty; }

        /// <summary>Last confirmed loadout (already validated against ownership).</summary>
        public LoadoutData GetLastLoadout() { EnsureReady(); return _state.Meta.LastLoadout; }

        // ── Internals ─────────────────────────────────────────────────────────

        private void OnLoadoutConfirmed(LoadoutConfirmed evt)
        {
            _state.Meta.LastLoadout = new LoadoutData(evt.Primary.ToString(), evt.Secondary.ToString());
            _state.Meta.LastSelectedDifficulty = evt.Difficulty.ToString();
            _worker.EnqueueSave(_state); // worker deep-copies internally
        }

        private static bool IsOwned(SaveData data, string weaponId) =>
            data.Weapons.TryGetValue(weaponId, out var w) && w.Owned;

        private static string FirstOwnedInPool(SaveData data, SaveConfig config, string poolPrefix, string fallback)
        {
            var owned = (config.ActiveWeaponIds ?? Array.Empty<WeaponId>())
                .Select(w => w.ToString())
                .Where(id => id.StartsWith(poolPrefix, StringComparison.Ordinal) && IsOwned(data, id))
                .OrderBy(id => id, StringComparer.Ordinal)
                .FirstOrDefault();
            return owned ?? fallback;
        }

        private void EnsureReady()
        {
            if (!_initialized)
                throw new InvalidOperationException("MetaSaveService.Initialize() must complete before querying save state.");
        }
    }
}
