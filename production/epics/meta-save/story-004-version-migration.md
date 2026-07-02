# Story 004: Save Versioning & Migration Chain

> **Epic**: 元進度與存檔系統
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/meta-progression-system.md`
**Requirement**: `TR-meta-006`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time. Note: tr-registry.yaml not yet formalized; TR-ID inferred from GDD §H.6.)*

**ADR Governing Implementation**: ADR-0004: 存檔系統（JSON + 原子寫入 + CRC32）
**ADR Decision Summary**: `version` is a positive integer; `CURRENT_VERSION = 1` (GDD §C.4). On load, if `version > CURRENT`: reject with `VersionTooNew`. If `version < CURRENT`: apply `MIGRATIONS[v+1]` pure functions in sequence until version matches. Max gap = `save_max_migration_generations` (default 3 from `SaveConfig`); exceeding it shows "save too old". After a successful migration, autosave the migrated data immediately (via `EnqueueSave`) to avoid re-running the chain on every subsequent launch. Missing fields in old saves are filled with `SaveConfig` new-game defaults.

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**:
- No engine-specific risk for this story — pure C# logic. The `MIGRATIONS` dictionary / array is a compile-time registry; no Unity API is involved.
- If future migrations involve asset GUIDs or `Resources.Load` paths, revisit engine compatibility at that time.

**Control Manifest Rules (Feature layer — Meta)**:
- Required: Each migration function must be a pure function (same input → same output, no side effects, no I/O calls)
- Required: Missing fields are filled from `SaveConfig` defaults (not hardcoded default values in migration code)
- Required: Migration functions registered in a static `Dictionary<int, Func<SaveData, SaveData>> MIGRATIONS` keyed by target version
- Forbidden: Migration code must not call `EnqueueSave` or `File.Write` — it is a pure transform; the caller triggers the post-migration autosave
- Forbidden: No hardcoded default values inside migration functions — always read from injected `SaveConfig`

---

## Acceptance Criteria

*From GDD `design/gdd/meta-progression-system.md` §C.4 + §H.6, scoped to this story:*

- [ ] `SaveMigrator.Migrate(SaveData raw, SaveConfig config)` applies the migration chain: while `raw.version < CURRENT_VERSION`, call `MIGRATIONS[raw.version + 1](raw)` and increment `raw.version`; returns the migrated `SaveData`
- [ ] `CURRENT_VERSION = 1` constant defined in a single location; all version comparisons reference it (no magic number `1` elsewhere)
- [ ] `save.version > CURRENT_VERSION` path handled in Story 003 (load); `SaveMigrator.Migrate` is only called when `version <= CURRENT_VERSION` — this contract is enforced by guard assertion
- [ ] Migration gap > `config.SaveMaxMigrationGenerations`: `Migrate` returns a `MigrationResult.TooOld` result; no migration functions are applied; save file is not modified
- [ ] After a successful migration (`version` was < CURRENT), the caller (load flow wiring in this story) calls `EnqueueSave(migratedData)` — this ensures the migrated version is persisted so the chain does not re-run on next launch
- [ ] `v1` → `v1` (no migration needed): `Migrate` returns `MigrationResult.NotNeeded`; `SaveData` is returned unchanged; no autosave is triggered
- [ ] Each migration function receives the full `SaveData` and returns a new `SaveData` with all missing fields populated from `SaveConfig` new-game defaults (fields that do not exist in the old version receive their default values, existing fields are preserved)
- [ ] `MIGRATIONS` registry is empty at `v1` (initial version has no migration function); the registry is extensible — adding a `MIGRATIONS[2]` function in future requires no structural changes to `SaveMigrator`

---

## Implementation Notes

*Derived from ADR-0004 Decision §4 and GDD §C.4:*

**Migration chain pattern (GDD §C.4)**:
```csharp
// Pure function registry (populated at module initialization):
static readonly Dictionary<int, Func<SaveData, SaveConfig, SaveData>> MIGRATIONS = new() {
    // v1 is the initial version; no migration needed to reach v1
    // Future: { 2, MigrateV1ToV2 }
};

MigrationResult Migrate(SaveData raw, SaveConfig config) {
    if (raw.version == CURRENT_VERSION) return MigrationResult.NotNeeded(raw);

    int gap = CURRENT_VERSION - raw.version;
    if (gap > config.SaveMaxMigrationGenerations) return MigrationResult.TooOld;

    while (raw.version < CURRENT_VERSION) {
        int targetVersion = raw.version + 1;
        raw = MIGRATIONS[targetVersion](raw, config);
        raw = raw with { version = targetVersion };  // C# record with-expression or manual
    }
    return MigrationResult.Migrated(raw);
}
```

**Migration function contract (for future implementers)**:
- Receives: `SaveData` at `v(n)` schema; `SaveConfig` for defaults
- Returns: new `SaveData` at `v(n+1)` schema; `version` field is updated by the caller (not inside the function)
- New fields: initialize from `config` defaults, not hardcoded values
- Existing fields: preserve exactly
- No I/O, no logging, no event publishing — pure transform only

**`MigrationResult` type**:
```csharp
abstract record MigrationResult;
record NotNeeded(SaveData Data)  : MigrationResult;
record Migrated(SaveData Data)   : MigrationResult;
record TooOld                    : MigrationResult;
```

**Load flow wiring** — after Story 003's `LoadOrDefault` returns `Success` or `RestoredFromBackup`, the orchestrating `MetaSaveService` calls `SaveMigrator.Migrate`. If result is `Migrated`, it calls `EnqueueSave(result.Data)` to persist the migration immediately. This wiring lives in `MetaSaveService.Initialize()` (the composition point), not inside `SaveLoader` or `SaveMigrator` themselves.

**v1 baseline**: The `MIGRATIONS` dict is intentionally empty at v1. A unit test that calls `Migrate` on a v1 save must confirm it returns `NotNeeded` without touching the MIGRATIONS dict. This serves as a regression baseline for detecting accidental version bumps.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 003: `save.version > CURRENT_VERSION` rejection (VersionTooNew path)
- Story 001: `SaveData` DTO, schema definition
- Story 002: `EnqueueSave` / `AtomicWrite` (called by the orchestrator after migration, not by `SaveMigrator`)
- Future developers: implementing `MigrateV1ToV2`, `MigrateV2ToV3`, etc. — this story only establishes the registry pattern and the chain executor

---

## QA Test Cases

*Authored by lead-programmer (lean mode). Developer implements against these.*

**AC-1**: v1 save in v1 app — no migration, no autosave triggered

- Given: `SaveData` with `version = 1` (== CURRENT_VERSION)
- When: `SaveMigrator.Migrate(data, config)` called
- Then: returns `MigrationResult.NotNeeded`; returned `Data` is the same object (no copy); `EnqueueSave` is NOT called by the orchestrator
- Edge cases: a v1 save with all fields populated — all field values unchanged after `Migrate`

**AC-2**: Migration gap exceeds `SaveMaxMigrationGenerations` → TooOld

- Given: `SaveData.version = 0` (hypothetical) and `config.SaveMaxMigrationGenerations = 3`; `CURRENT_VERSION = 4` (gap = 4 > 3)
- When: `SaveMigrator.Migrate(data, config)` called
- Then: returns `MigrationResult.TooOld`; no migration functions executed; `SaveData` object not modified
- Edge cases: gap exactly equal to `SaveMaxMigrationGenerations` (3) — should migrate successfully; gap of 4 — TooOld

**AC-3**: Future migration (structural test — using a test-only v2 migration function)

- Given: a `SaveData` with `version = 1`; a test-only migration function `MigrateV1ToV2` registered in a test-scope `MIGRATIONS` dict that adds a hypothetical new field `stats.total_damage_dealt = 0` (from config default)
- When: `SaveMigrator.Migrate(data, config)` called with `CURRENT_VERSION = 2` (test override)
- Then: returns `MigrationResult.Migrated`; result `Data.version = 2`; `stats.total_damage_dealt = 0`; all original fields preserved
- Edge cases: original field `stats.total_parts_broken = 87` remains 87 after migration

**AC-4**: Migration function is a pure function (determinism)

- Given: a `SaveData` at `v(n)` and a migration function in `MIGRATIONS[n+1]`
- When: the migration function is called twice with the same input
- Then: both outputs are structurally identical (same field values)
- Edge cases: calling the migration function does not modify the input `SaveData` (pure — no mutation of argument)

**AC-5**: Post-migration autosave is triggered exactly once

- Given: `SaveData.version = 1` (no migration) and a second case where migration does occur
- When: `MetaSaveService.Initialize()` is called with each save
- Then: case 1 (no migration) — `EnqueueSave` NOT called; case 2 (migration) — `EnqueueSave` called exactly once with the migrated data
- Edge cases: verify `EnqueueSave` is not called redundantly if migration returns `NotNeeded`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/Meta/save_version_migration_test.cs` — must exist and all tests pass
*(ADR-0005: EditMode test assembly. `SaveMigrator` is pure C# with no Unity API calls; fully testable without a scene.)*

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 must be DONE (SaveData DTO), Story 003 must be DONE (load flow calls Migrate after successful load)
- Unlocks: Story 005 (initialization flow completes only after migration is wired), Story 006 (autosave wiring assumes migration ran at startup)
