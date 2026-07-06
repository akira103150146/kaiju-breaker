# Story 003: CRC32 Integrity Check, Load & Corruption Repair

> **Epic**: 元進度與存檔系統
> **Status**: ✅ Complete (2026-07-06 — 9/9 EditMode GREEN, part of 322-case suite)
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: 2026-07-06

## Context

**GDD**: `design/gdd/meta-progression-system.md`
**Requirement**: `TR-meta-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time. Note: tr-registry.yaml not yet formalized; TR-ID inferred from GDD §H.5.)*

**ADR Governing Implementation**: ADR-0004: 存檔系統（JSON + 原子寫入 + CRC32）
**ADR Decision Summary**: On load, recompute `CRC32_hex(canonical_json(D \ {integrity_hash}))` and compare to the stored `integrity_hash`; mismatch triggers a read of `save.bak.json` with the same validation; if both fail, show a non-crashing error screen offering Reset (clear save) or Continue-with-corruption (force load, flag `save_integrity_warning = true`). First launch (no `save.json`) initializes from `SaveConfig` defaults. CRC32 intent is accidental-corruption detection only — not anti-cheat.

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**:
- `File.Exists()`, `File.ReadAllText()` behave as expected on desktop Unity 6.3. Mobile sandboxed paths (`Application.persistentDataPath`) confirmed accessible via standard `System.IO` — still [需查證 6.3 API] for any Unity 6-specific sandboxing changes.
- Error/reset screen is a UI concern (not implemented here); this story exposes a `SaveLoadResult` result type that the UI layer reads — no direct UI calls from `Meta`.

**Control Manifest Rules (Feature layer — Meta)**:
- Required: CRC32 check on every load; failure path reads backup before giving up
- Required: Application MUST NOT crash on any corruption path; always return a valid `SaveLoadResult`
- Required: UI is notified via event bus (`Core` event type) or `ISaveService` error state — Meta must NOT call UI directly
- Required: `ISaveService.IsWeaponOwned()` / `GetMaterialCount()` etc. must not be callable before `LoadOrDefault()` completes (enforce initialization ordering)
- Forbidden: Swallowing exceptions silently; all corruption paths must be detectable via `SaveLoadResult.Status`

---

## Acceptance Criteria

*From GDD `design/gdd/meta-progression-system.md` §D.2 + §E.2 + §E.3 + §H.5, scoped to this story:*

- [ ] `SaveLoader.LoadOrDefault(SaveConfig config)` method: reads `save.json` → deserializes → recomputes CRC32 over `canonical_json(D \ {integrity_hash})` → if hash matches, returns `SaveLoadResult.Success(saveData)`
- [ ] CRC32 mismatch on `save.json` → automatically attempts `save.bak.json` with the same deserialize + verify step → if backup valid, returns `SaveLoadResult.RestoredFromBackup(saveData)`
- [ ] Both `save.json` and `save.bak.json` fail CRC32 (or are malformed JSON) → returns `SaveLoadResult.Corrupted`; the application does NOT crash; a `SaveCorrupted` event is published on `IEventBus` for the UI layer to react to
- [ ] `SaveCorrupted` result exposes two recovery paths callable by the user: `ResetToNewGame()` (deletes both files, initializes new-game defaults, writes sync) and `ContinueWithCorruption(forcedData)` (sets `save_integrity_warning = true` on the loaded data)
- [ ] No `save.json` file found (first launch, §E.3): returns `SaveLoadResult.NewGame(defaultSaveData)` where `defaultSaveData` is built from `SaveConfig` defaults; the caller (Story 005's initialization flow) handles the initial sync write
- [ ] `save.version > CURRENT_VERSION` (§E.4): returns `SaveLoadResult.VersionTooNew`; file is not modified
- [ ] The `integrity_hash` field is excluded from the CRC32 input — only the remaining fields (canonical JSON of `D \ {integrity_hash}`) are hashed (matches GDD §D.2 formula exactly)

---

## Implementation Notes

*Derived from ADR-0004 Decision §3 + GDD §D.2 validation procedure:*

**Load flow (authoritative from GDD §C.4 + §D.2)**:
```
LoadOrDefault():
  if NOT File.Exists(SAVE_PATH): return NewGame(BuildDefaults(config))

  json = File.ReadAllText(SAVE_PATH)
  try:
    data = CanonicalSerializer.Deserialize(json)
  catch JsonException:
    data = null

  if data != null && VerifyIntegrity(data):
    return data.version > CURRENT_VERSION
           ? VersionTooNew
           : Success(data)

  // Primary failed — try backup
  if File.Exists(BACKUP_PATH):
    json = File.ReadAllText(BACKUP_PATH)
    try: data = CanonicalSerializer.Deserialize(json)
    catch: data = null
    if data != null && VerifyIntegrity(data):
      return RestoredFromBackup(data)

  return Corrupted

VerifyIntegrity(SaveData data):
  stored   = data.integrity_hash
  computed = CRC32Calculator.Compute(CanonicalSerializer.Serialize(data, excludeIntegrityHash: true))
  return stored == computed   // case-insensitive hex compare
```

**`SaveLoadResult` discriminated union** (C# record or sealed class hierarchy):
```csharp
abstract record SaveLoadResult;
record Success(SaveData Data)                  : SaveLoadResult;
record RestoredFromBackup(SaveData Data)       : SaveLoadResult;
record NewGame(SaveData Data)                  : SaveLoadResult;
record Corrupted                               : SaveLoadResult;
record VersionTooNew(int SaveVersion)          : SaveLoadResult;
```

**Error screen integration**: This story does NOT implement UI. It publishes a `SaveCorruptedEvent` struct (defined in `Core`) on `IEventBus` when returning `Corrupted`. The UI system subscribes independently. The `Meta` system exposes `ResetToNewGame()` and `ForceLoadCorrupted()` methods on `ISaveService` for the UI to call back.

**Integrity hash exclusion**: The `CanonicalSerializer.Serialize(data, excludeIntegrityHash: true)` overload (introduced in Story 001) is the canonical way to produce the hash input. Do not manually strip the field via string manipulation.

**Non-crash guarantee**: Wrap the entire `LoadOrDefault` method body in a top-level try/catch. Any unexpected exception (I/O permission error, OOM on deserialize) also returns `Corrupted` and publishes the event — the application never propagates an unhandled exception from the save-load path.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `CanonicalSerializer`, `CRC32Calculator` (required dependencies)
- Story 002: Write sequence, backup file maintenance
- Story 004: Version migration loop (triggered after a successful load when `version < CURRENT_VERSION`)
- Story 005: First-launch default sync write, `last_loadout` fallback validation
- UI system: Error screen rendering, user-facing "Save Corrupted" dialog

---

## QA Test Cases

*Authored by lead-programmer (lean mode). Developer implements against these.*

**AC-1**: Valid save.json with correct CRC32 loads successfully

- Given: `save.json` contains canonical JSON with a correct `integrity_hash`
- When: `SaveLoader.LoadOrDefault()` is called
- Then: returns `SaveLoadResult.Success`; `Data` fields match the written values
- Edge cases: `integrity_hash` in different position in the JSON (field order should not matter — hash is computed over canonical form)

**AC-2**: Corrupted save.json triggers backup attempt

- Given: `save.json` has one byte changed (e.g. `"tier":2` → `"tier":9`) causing CRC32 mismatch; `save.bak.json` is valid
- When: `LoadOrDefault()` is called
- Then: returns `SaveLoadResult.RestoredFromBackup`; `Data` matches `save.bak.json` content
- Edge cases: `save.json` is valid JSON but hash field is wrong (not a truncation); `save.bak.json` is checked as fallback

**AC-3**: Both files corrupted → Corrupted result, no crash

- Given: `save.json` and `save.bak.json` both contain invalid JSON (e.g. truncated files)
- When: `LoadOrDefault()` is called
- Then: returns `SaveLoadResult.Corrupted`; `SaveCorruptedEvent` published on `IEventBus`; no exception thrown; application remains in a runnable state
- Edge cases: files exist but are zero bytes; files contain valid JSON but both have wrong CRC32

**AC-4**: First launch (no save.json) returns NewGame defaults

- Given: neither `save.json` nor `save.bak.json` exist on disk
- When: `LoadOrDefault()` is called
- Then: returns `SaveLoadResult.NewGame`; `Data` matches `SaveConfig` default values (L1 + M1 owned, all materials 0, version = 1, `first_launch_complete = false`)
- Edge cases: `save.bak.json` exists but `save.json` does not (backup only) — should attempt backup before returning NewGame

**AC-5**: CRC32 regression — hash computed over correct subset

- Given: a `SaveData` object; compute hash A = `CRC32Calculator.Compute(Serialize(data, excludeIntegrityHash: true))`; set `data.integrity_hash = hashA`; then `Serialize(data)` (full, with hash)
- When: `VerifyIntegrity(data)` called
- Then: returns `true`
- When: `data.integrity_hash` set to a different value and `VerifyIntegrity` called
- Then: returns `false`

**AC-6**: `save.version > CURRENT_VERSION` → VersionTooNew, file untouched

- Given: `save.json` with `"version": 99` and an otherwise valid CRC32 for that data
- When: `LoadOrDefault()` is called
- Then: returns `SaveLoadResult.VersionTooNew(99)`; `save.json` content unchanged after the call

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/Meta/save_integrity_load_test.cs` — must exist and all tests pass
*(ADR-0005: EditMode test assembly. File I/O can be abstracted via `IFileSystem` interface injected into `SaveLoader`, enabling in-memory fake filesystem for all test cases.)*

**Status**: [x] ✅ 9/9 GREEN (`Assets/_Project/Tests/EditMode/Meta/save_integrity_load_test.cs`, Unity MCP, 2026-07-06). Covers AC-1..AC-6 (valid load, backup restore, both-corrupt→event+no-crash, first-launch defaults, VerifyIntegrity subset, VersionTooNew file-untouched) + backup-only recovery + reset/continue paths.

**Reconciliations vs story text** (surfaced for review):
1. `SaveLoadResult` = sealed class with a `SaveLoadStatus` enum + static factories (not C# positional records with inheritance) — avoids record-inheritance concerns on the Unity C# level; carries `Data`/`SaveVersion`/`IntegrityWarning`.
2. `SaveCorrupted` event added to **Core** (`Core/Events/SaveEvents.cs`) with a `BackupAlsoFailed` flag; Meta publishes it, never calls UI.
3. New-game defaults built by shared `NewGameFactory.Create(config)` (Meta) — Story 005 owns the surrounding flow; `MaterialKeys` centralises the `MaterialId`↔snake_case save-key map (weapon/difficulty keys use `enum.ToString()`).
4. Recovery methods (`ResetToNewGame`, `ContinueWithCorruption`) live on `SaveLoader`, not (yet) on `ISaveService` — the committed ISaveService surface is left intact; UI wiring is Story 006.
5. Test I/O uses a real temp dir (consistent with Story 002) rather than an `IFileSystem` fake — simpler and exercises the real File.Replace/Copy path.

---

## Dependencies

- Depends on: Story 001 must be DONE (CanonicalSerializer + CRC32Calculator), Story 002 must be DONE (backup file exists to test against)
- Unlocks: Story 004 (migration triggered after a successful load), Story 005 (default-init flow starts from `SaveLoadResult.NewGame`)
