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
    public sealed class MetaSaveService : ISaveService, IWeaponTierQuery
    {
        private readonly SaveConfig _config;
        private readonly IEventBus _bus;
        private readonly SaveLoader _loader;
        private readonly SaveMigrator _migrator;
        private readonly SaveWorker _worker;
        private readonly Action<LoadoutConfirmed> _onLoadoutConfirmed;
        private readonly Action<PartBroke> _onPartBroke;
        private readonly Action<HuntEnded> _onHuntEnded;

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
            _onPartBroke = OnPartBroke;
            _onHuntEnded = OnHuntEnded;
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
            _bus.Subscribe(_onPartBroke);
            _bus.Subscribe(_onHuntEnded);
            _initialized = true;
            _bus.Publish(new SaveReady());
        }

        /// <summary>Unsubscribe on teardown (App owns lifetime).</summary>
        public void Dispose()
        {
            _bus.Unsubscribe(_onLoadoutConfirmed);
            _bus.Unsubscribe(_onPartBroke);
            _bus.Unsubscribe(_onHuntEnded);
        }

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

        // ── ISaveService (the real persistence backend; Economy/Weapons call these) ──

        /// <inheritdoc/>
        public void CreditMaterials(MaterialId id, int amount)
        {
            string key = MaterialKeys.ToKey(id);
            _state.Materials.TryGetValue(key, out long current);
            _state.Materials[key] = current + amount; // accumulate, never overwrite (long → no int32 wrap)
            EnqueueAutosave();
        }

        /// <inheritdoc/>
        public int GetMaterialCount(MaterialId id)
        {
            _state.Materials.TryGetValue(MaterialKeys.ToKey(id), out long v);
            return v > int.MaxValue ? int.MaxValue : (int)v;
        }

        /// <inheritdoc/>
        public void SpendMaterials(MaterialId id, int amount)
        {
            string key = MaterialKeys.ToKey(id);
            _state.Materials.TryGetValue(key, out long current);
            _state.Materials[key] = current - amount; // caller (Economy) verified affordability
            EnqueueAutosave();
        }

        /// <inheritdoc/>
        public void SetWeaponTier(WeaponId weapon, int tier)
        {
            string key = weapon.ToString();
            if (_state.Weapons.TryGetValue(key, out var w)) w.Tier = tier;
            else _state.Weapons[key] = new WeaponSaveData(tier, owned: true);
            EnqueueAutosave();
        }

        /// <inheritdoc/>
        public void EnqueueAutosave() => _worker.EnqueueSave(_state); // worker deep-copies internally

        /// <inheritdoc/>
        public void FlushSync() => _worker.SyncWrite(_state);

        /// <inheritdoc/>
        public (WeaponId Primary, WeaponId Secondary)? GetInitialLoadout()
        {
            var lo = _state?.Meta?.LastLoadout;
            if (lo == null) return null;
            if (Enum.TryParse(lo.Primary, out WeaponId primary) && Enum.TryParse(lo.Secondary, out WeaponId secondary))
                return (primary, secondary);
            return null;
        }

        /// <inheritdoc/>
        public int GetTier(WeaponId weapon) =>
            _state.Weapons.TryGetValue(weapon.ToString(), out var w) ? w.Tier : 0;

        /// <summary>True if a weapon is permanently owned (Story 007 flips this on first pickup).</summary>
        public bool IsWeaponOwned(WeaponId weapon) =>
            _state.Weapons.TryGetValue(weapon.ToString(), out var w) && w.Owned;

        /// <summary>
        /// Blocking flush of the latest state for the app-suspend/quit safety net (meta-progression-system.md
        /// §C.5.1). Stops the async worker (draining any pending write) then writes synchronously.
        /// </summary>
        public void FlushSyncNow()
        {
            _worker.Stop();          // drains any pending snapshot
            _worker.SyncWrite(_state);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        /// <summary>
        /// on_part_break (§C.6.3): Meta persists the RECORDS side of a break — lifetime part-broken stat +
        /// autosave. Material yields are NOT read here: Economy computes them and calls
        /// <see cref="CreditMaterials"/> (committed division of labour, ADR-0002 §3) — Meta must not recompute.
        /// </summary>
        private void OnPartBroke(PartBroke evt)
        {
            _state.Stats.TotalPartsBroken += 1;
            EnqueueAutosave();
        }

        /// <summary>
        /// on_hunt_end: update lifetime run stats + autosave. Full-clear grants the global full-clear stat;
        /// the essence/shard bonus itself is credited by Economy via <see cref="CreditMaterials"/>.
        /// </summary>
        private void OnHuntEnded(HuntEnded evt)
        {
            _state.Stats.TotalRunsCompleted += 1;
            if (evt.IsAllPartsBroken) _state.Stats.TotalFullClears += 1;
            EnqueueAutosave();
        }

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
