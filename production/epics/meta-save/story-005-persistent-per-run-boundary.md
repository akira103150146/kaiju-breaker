# Story 005: Persistent vs Per-Run State Boundary & New Game Initialization

> **Epic**: 元進度與存檔系統
> **Status**: ✅ Complete (2026-07-06 — 10/10 EditMode GREEN, part of 340-case suite)
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: 2026-07-06

## Context

**GDD**: `design/gdd/meta-progression-system.md`
**Requirement**: `TR-meta-007`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time. Note: tr-registry.yaml not yet formalized; TR-ID inferred from GDD §H.7 + §H.8.)*

**ADR Governing Implementation**: ADR-0004: 存檔系統（JSON + 原子寫入 + CRC32）
**ADR Decision Summary**: `SaveData` contains only persistent fields (weapons, materials, kaiju_records, meta, settings, stats); per-run fields (current loadout in-run, run score/time, H/B part progress, in-flight animations) have no representation in `SaveData` and are never written to disk. On load, `meta.last_loadout` and `meta.last_selected_difficulty` are validated and, if they reference invalid state, fallback to safe defaults. New-game initialization builds `SaveData` from `SaveConfig` defaults and writes synchronously.

Secondary ADR: ADR-0002 (event bus — `on_loadout_confirmed` triggers the last_loadout write)

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**:
- No engine-specific risk for the state boundary definition itself (pure C# model).
- `Application.persistentDataPath` is the correct path for the initial sync write [已在 Story 002 驗證].
- `on_loadout_confirmed` event: the `LoadoutConfirmed` event struct (defined in `Core`) is published by the Stage/UI system; Meta subscribes to it here.

**Control Manifest Rules (Feature layer — Meta)**:
- Required: `SaveData` struct/class must contain NO per-run state fields; static analysis test must confirm absent field names
- Required: `last_loadout.primary` fallback guarantees `L1` (always owned); `last_loadout.secondary` fallback guarantees `M1` (always owned)
- Required: Default initialization uses `SaveConfig.StartingOwnedWeapons` — no hardcoded `["L1", "M1"]` in code
- Required: New-game first sync write goes through Story 002's `SyncWrite` path (not a new code path)
- Forbidden: Per-run score, timer, or part-heat-progress must never appear in `SaveData` definition

---

## Acceptance Criteria

*From GDD `design/gdd/meta-progression-system.md` §C.1 + §C.7 + §C.8 + §E.5 + §H.7 + §H.8, scoped to this story:*

- [ ] `SaveData` class/record has no fields for: run score, run time, current loadout in-run (separate from `last_loadout`), H/B part damage progress, or in-flight material animations — confirmed by a static inspection test (use reflection or a compile-time attribute check)
- [ ] New-game initialization (`MetaSaveService.InitializeNewGame()`): builds `SaveData` from `SaveConfig` defaults — weapons owned state from `SaveConfig.StartingOwnedWeapons` (default `["L1", "M1"]`), all material counts 0, kaiju_records zeroed for all `SaveConfig.ActiveKaijuIds`, `meta.last_selected_difficulty = "D1"`, `meta.last_loadout = {primary: L1, secondary: M1}`, `meta.first_launch_complete = false`, all stats 0 — then calls `SyncWrite` to persist immediately
- [ ] `MetaSaveService.ValidateLastLoadout(SaveData data, SaveConfig config)`: if `data.meta.last_loadout.primary` has `weapons[id].owned == false`, replace with `first weapon in LASER pool where owned == true` (guaranteed to be L1 from starting state); same logic for `secondary` in MISSILE pool
- [ ] First launch (Story 003 returns `NewGame`): `InitializeNewGame()` is called; `meta.first_launch_complete == false`; `last_selected_difficulty == "D1"` (aligns with `difficulty-system.md` G.2 `default_difficulty_on_first_launch = D1`)
- [ ] After one completed run with difficulty D2: `GetLastDifficulty()` returns D2 on the next startup
- [ ] `LoadoutConfirmed` event subscription: when received, writes `meta.last_loadout` and `meta.last_selected_difficulty` from the event payload and calls `EnqueueSave`
- [ ] `ValidateLastLoadout` is called during `MetaSaveService.Initialize()` after load + migration — before any `ISaveService` query methods become callable

---

## Implementation Notes

*Derived from ADR-0004 Decision §6 + GDD §C.1 + §E.5:*

**Persistent vs per-run boundary (authoritative GDD §C.1)**:

| Persistent (in SaveData) | Per-Run (never in SaveData) |
|---|---|
| `weapons[*].tier`, `weapons[*].owned` | current in-run weapon loadout |
| `materials[*]` counts | run score, run timer |
| `kaiju_records[*]` (parts_ever_broken, counts, best_times) | H/B part damage progress |
| `meta.last_selected_difficulty` | in-flight material ball animations |
| `meta.last_loadout` | field-dropped weapon (if not first pickup) |
| `meta.first_launch_complete` | |
| `settings.*`, `stats.*` | |

The Stage system owns run-time per-run state. Meta never receives or persists it.

**New-game default build pattern**:
```csharp
SaveData InitializeNewGame(SaveConfig config) {
    var data = new SaveData { version = SaveConstants.CurrentVersion };
    foreach (var id in config.ActiveWeaponIds)
        data.weapons[id] = new WeaponSaveData {
            tier  = 0,
            owned = config.StartingOwnedWeapons.Contains(id)
        };
    foreach (var id in config.ActiveKaijuIds)
        data.kaiju_records[id] = KaijuRecordData.Zero;
    foreach (var material in MaterialType.All)
        data.materials[material] = 0;
    data.meta = new MetaSaveData {
        last_selected_difficulty = "D1",
        last_loadout = new LastLoadout { primary = "L1", secondary = "M1" },
        first_launch_complete    = false
    };
    data.settings = SettingsData.Defaults;
    data.stats    = StatsData.Zero;
    return data;
}
```

**Loadout fallback (GDD §E.5)**:
```csharp
ValidateLastLoadout(SaveData data, SaveConfig config) {
    if (!data.weapons[data.meta.last_loadout.primary].owned)
        data.meta.last_loadout.primary =
            config.ActiveWeaponIds
                  .Where(id => id.StartsWith("L") && data.weapons[id].owned)
                  .OrderBy(id => id)
                  .First();   // "L1" guaranteed owned from starting state

    if (!data.weapons[data.meta.last_loadout.secondary].owned)
        data.meta.last_loadout.secondary =
            config.ActiveWeaponIds
                  .Where(id => id.StartsWith("M") && data.weapons[id].owned)
                  .OrderBy(id => id)
                  .First();   // "M1" guaranteed owned
}
```

**Initialization order in `MetaSaveService.Initialize()`**:
1. `LoadOrDefault()` (Story 003) → result
2. If `NewGame`: `InitializeNewGame()` → `SyncWrite()`
3. If `Success` / `RestoredFromBackup` / `Migrated`: call `ValidateLastLoadout()`
4. Mark service as initialized — query methods now callable
5. Publish `SaveReadyEvent` on `IEventBus` (for UI to know data is available)

**`LoadoutConfirmed` event subscription**:
```csharp
// Subscribe in Initialize():
_eventBus.Subscribe<LoadoutConfirmed>(OnLoadoutConfirmed);

void OnLoadoutConfirmed(in LoadoutConfirmed evt) {
    _state.meta.last_loadout             = evt.Loadout;
    _state.meta.last_selected_difficulty = evt.Difficulty;
    EnqueueSave(DeepCopy(_state));
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `SaveData` class definition (this story only validates its contents)
- Story 002: `SyncWrite` / `EnqueueSave` implementation
- Story 003: Load flow, corruption handling
- Story 004: Version migration loop
- Story 007: Weapon ownership state machine (`weapons[id].owned = true` on first pickup)
- Stage system: per-run state management (run score, H/B progress — not in Meta's scope)
- UI system: rendering last_difficulty/last_loadout as pre-filled selections in UI

---

## QA Test Cases

*Authored by lead-programmer (lean mode). Developer implements against these.*

**AC-1**: New-game defaults match GDD §C.7 exactly (all fields checked)

- Given: `SaveConfig` with defaults (`StartingOwnedWeapons = ["L1","M1"]`, `ActiveKaijuIds = ["CARAPEX","LACERA","VOLTWYRM"]`)
- When: `MetaSaveService.InitializeNewGame(config)` called
- Then: L1.owned=true, M1.owned=true; L2/L3/L4/M2/M3/M4.owned=false; all 5 material counts = 0; all 3 kaiju records: parts_ever_broken=[], full_clear_count=0, hunt_count all 0, best_time all null; `meta.last_selected_difficulty="D1"`; `meta.last_loadout={primary:"L1",secondary:"M1"}`; `meta.first_launch_complete=false`; all stats = 0
- Edge cases: run `InitializeNewGame` twice → second call produces an equivalent (not shared) object

**AC-2**: last_difficulty persists across load

- Given: a save with `meta.last_selected_difficulty = "D2"`
- When: saved, then `LoadOrDefault()` + `MetaSaveService.Initialize()` run
- Then: `ISaveService.GetLastDifficulty()` returns `"D2"`
- Edge cases: D1, D2, D3, D4 all round-trip correctly

**AC-3**: last_loadout primary fallback when unowned

- Given: `SaveData` loaded with `meta.last_loadout.primary = "L3"` and `weapons["L3"].owned = false`
- When: `ValidateLastLoadout(data, config)` called (L1 is owned)
- Then: `data.meta.last_loadout.primary = "L1"`; `ISaveService.GetLastLoadout().Primary == "L1"`
- Edge cases: multiple LASER weapons owned (L1 and L2 both owned) → primary fallback to L1 (alphabetically first); all LASER weapons unowned → not a valid game state (L1 is always owned from new game)

**AC-4**: last_loadout secondary fallback when unowned

- Given: `SaveData` with `last_loadout.secondary = "M3"` and `weapons["M3"].owned = false`; `weapons["M1"].owned = true`
- When: `ValidateLastLoadout(data, config)` called
- Then: `last_loadout.secondary = "M1"`
- Edge cases: secondary points to a LASER weapon ID (schema corruption) → fallback to M1

**AC-5**: `SaveData` has no per-run state fields (static contract test)

- Given: `SaveData` type definition via reflection
- When: field names are enumerated
- Then: none of the following names appear: `runScore`, `runTime`, `currentHeat`, `breakProgress`, `inFlightAnimations`, `currentInRunLoadout` (or any variant)
- Edge cases: nested types (WeaponSaveData, KaijuRecordData, etc.) also checked for absence of per-run fields

**AC-6**: `LoadoutConfirmed` event triggers last_loadout + last_difficulty write

- Given: `MetaSaveService` initialized with a default save; fake `IEventBus`
- When: `LoadoutConfirmed` event published with `Loadout={primary:"L2",secondary:"M2"}`, `Difficulty="D3"`
- Then: `GetLastLoadout()` returns L2/M2; `GetLastDifficulty()` returns D3; `EnqueueSave` called once
- Edge cases: multiple `LoadoutConfirmed` events in sequence — each updates; each triggers one `EnqueueSave`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/Meta/save_state_boundary_test.cs` — must exist and all tests pass
*(ADR-0005: EditMode test assembly. All tests use injected fake `IEventBus` and `SaveConfig` SO fixture.)*

**Status**: [x] ✅ 10/10 GREEN (`Assets/_Project/Tests/EditMode/Meta/save_state_boundary_test.cs`, Unity MCP, 2026-07-06). Covers AC-1 (new-game defaults + independent objects), AC-2 (difficulty persists across load), AC-3/4 (loadout primary/secondary fallback incl secondary-points-at-laser), AC-5 (reflection: no per-run fields), AC-6 (LoadoutConfirmed → state + 1 EnqueueSave) + pre-init throws + SaveReady published.

**Reconciliations vs story text** (surfaced for review):
1. **`LoadoutConfirmed` gained a payload** (`Primary`/`Secondary`/`Difficulty`) — it was an empty struct from stage-001; RunController ignores the payload (still transitions), Meta reads it. `SaveReady` event added to Core.
2. **`MetaSaveService` created here** (was implied by stories 005+006). Read queries `GetLastDifficulty`/`GetLastLoadout` are **concrete on the service, not on the committed `ISaveService`** — final ISaveService surface is Story 006's decision. `InitializeNewGame` uses the shared `NewGameFactory` + Story 002 `SyncWrite`.
3. Initialize order: LoadOrDefault → (NewGame/Corrupted/VersionTooNew → fresh new game) | (Success/Backup → migrate, autosave-once if Migrated, else NotNeeded → validate last loadout) → subscribe LoadoutConfirmed → ready → publish SaveReady. `SaveWorker.EnqueueCount` added for the AC-6 once-assertion.
4. Full `ISaveService`/`IWeaponTierQuery` impl + material crediting = Story 006; weapon-ownership persistence = Story 007.

---

## Dependencies

- Depends on: Story 001 (SaveData DTO), Story 002 (SyncWrite/EnqueueSave), Story 003 (LoadOrDefault flow), Story 004 (migration runs before ValidateLastLoadout)
- Unlocks: Story 006 (autosave handler needs initialized state), Story 007 (ownership checks depend on initialized `weapons` map)
