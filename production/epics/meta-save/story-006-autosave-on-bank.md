# Story 006: Autosave-on-Bank: on_part_break Instant Credit & Suspend Sync

> **Epic**: 元進度與存檔系統
> **Status**: ✅ Complete (2026-07-06 — 10/10 EditMode GREEN, part of 350-case suite; PlayMode lifecycle = manual QA)
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: 4h
> **Manifest Version**: 2026-07-02
> **Last Updated**: 2026-07-06

## Context

**GDD**: `design/gdd/meta-progression-system.md`
**Requirement**: `TR-meta-001`, `TR-meta-002`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time. Note: tr-registry.yaml not yet formalized; TR-IDs inferred from GDD §H.1 + §H.2.)*

**ADR Governing Implementation**: ADR-0004: 存檔系統（JSON + 原子寫入 + CRC32）
**ADR Decision Summary**: Meta subscribes to `PartBroke` (= `on_part_break`) and credits `shard_yield` + `core_yield` from the event payload into `materials[]` in the same frame; `kaiju_records[id].parts_ever_broken` is updated in the same frame; `EnqueueSave(DeepCopy(state))` is called in the same frame — all three operations are synchronous before the frame ends. Material yield values in the payload are computed by the `Economy` system; Meta reads them, it does NOT recompute. `on_app_suspend`/`on_app_quit` call `SyncWrite` (blocking) as a safety net to capture the latest state.

Secondary ADR: ADR-0002 (typed event bus, synchronous dispatch, `PartBroke` event struct ownership by KaijuParts)

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**:
- `OnApplicationPause(bool pauseStatus)` is called on the `MonoBehaviour` that owns the Meta bridge. On iOS, the game has ~5 s after `applicationDidEnterBackground` to complete writes before the OS may suspend/kill. `SyncWrite` must complete within this window [需查證 6.3 API — verify actual time budget per platform in `docs/engine-reference/unity/VERSION.md`].
- `OnApplicationQuit` is called in Editor and standalone builds; mobile processes may be killed without it. `OnApplicationPause(true)` is the critical mobile safety net.
- The `PartBroke` event struct (defined in `Core` by ADR-0002) carries `shard_yield: int` and `core_yield: int` pre-computed by the Economy system. Meta reads these values; do NOT subscribe to `PartBroke` and recompute yields independently.

**Control Manifest Rules (Feature layer — Meta)**:
- Required: `PartBroke` handler must credit materials + update kaiju_records + call `EnqueueSave` all within the same synchronous event dispatch frame
- Required: `shard_yield` and `core_yield` read from event payload — Meta MUST NOT call `IEconomyQuery` or recalculate
- Required: `on_app_suspend`/`on_app_quit` → `SyncWrite` (blocking); NOT `EnqueueSave` (async)
- Required: All event subscriptions set up in `MetaSaveService.Initialize()`; all unsubscribed in `Dispose()`
- Forbidden: Meta must not publish back to IEventBus in response to `PartBroke` (loop risk); UI notification for "material banked" is a `GameFeel` / `UI` concern
- Forbidden: `Economy` assembly must not be referenced by `Meta` — yield values arrive via the `PartBroke` payload only (ADR-0002 §3 event ownership)

---

## Acceptance Criteria

*From GDD `design/gdd/meta-progression-system.md` §C.5 + §C.6 + §D.1 + §H.1 + §H.2, scoped to this story:*

- [ ] `PartBroke` event handler: `materials["shard_common"] += evt.ShardYield`; `materials[CoreTypeForKaiju(evt.KaijuId)] += evt.CoreYield`; `kaiju_records[evt.KaijuId].parts_ever_broken.Add(evt.PartId)` (set semantics — no duplicates); `EnqueueSave(DeepCopy(_state))` — all in one synchronous call within the event dispatch frame
- [ ] `HuntEnded` event handler (all_broken = true): `materials["shard_common"] += evt.ShardBonus`; `materials["essence_kaiju"] += evt.EssenceYield`; `kaiju_records[kaijuId].full_clear_count += 1`; `hunt_count_per_difficulty[evt.Difficulty] += 1`; update `best_time_per_difficulty[evt.Difficulty]` if `evt.CompletionTimeS < current_best || current_best == null`; `stats.total_runs_completed += 1`; `stats.total_full_clears += 1`; `EnqueueSave`
- [ ] `HuntEnded` event handler (all_broken = false): NO material bonus; `hunt_count_per_difficulty[evt.Difficulty] += 1`; `stats.total_runs_completed += 1`; `full_clear_count` unchanged; `EnqueueSave`
- [ ] Consecutive `PartBroke` events accumulate in `materials[]` without truncation or integer overflow for up to INT32_MAX total — materials storage type is `int` (no upper bound per GDD §C.3)
- [ ] `CoreTypeForKaiju(kaijuId)` mapping: CARAPEX → `core_carapace`; LACERA → `core_limb`; VOLTWYRM → `core_energy` (matches GDD §D.1; mapping loaded from `SaveConfig` or `EconomyConfig`, not hardcoded)
- [ ] `OnApplicationPause(true)` Unity lifecycle callback: calls `SyncWrite(DeepCopy(_state))` and blocks until the write and file rename are confirmed complete before returning
- [ ] `OnApplicationQuit()` lifecycle callback: calls `SyncWrite` with the same blocking semantics
- [ ] Kill process within 100 ms after `PartBroke` fires → restart → banked material amount equals the pre-kill inventory plus the yield from the broken part (**manual QA criterion per GDD §H.1**; automated: verify `EnqueueSave` is called synchronously within the same call stack as the `PartBroke` handler before the handler returns)
- [ ] `stats.total_parts_broken` incremented by 1 on every `PartBroke` event regardless of part type or break quality

---

## Implementation Notes

*Derived from ADR-0004 Decision §6, GDD §C.6.3, and control-manifest.md §3 Meta + §4.2:*

**Event subscriptions (all wired in `MetaSaveService.Initialize()`):**
```
_eventBus.Subscribe<PartBroke>(OnPartBroke);
_eventBus.Subscribe<HuntEnded>(OnHuntEnded);
_eventBus.Subscribe<WeaponPodPickup>(OnWeaponPodPickup);        // Story 007
_eventBus.Subscribe<WeaponUpgradeConfirmed>(OnWeaponUpgraded);  // future story
_eventBus.Subscribe<LoadoutConfirmed>(OnLoadoutConfirmed);       // Story 005
_eventBus.Subscribe<SettingsChanged>(OnSettingsChanged);
```

**`PartBroke` handler (same-frame credit — GDD §C.6.3)**:
```csharp
void OnPartBroke(in PartBroke evt) {
    _state.materials["shard_common"]        += evt.ShardYield;
    _state.materials[CoreTypeFor(evt.KaijuId)] += evt.CoreYield;
    _state.kaiju_records[evt.KaijuId].parts_ever_broken.Add(evt.PartId);
    _state.stats.total_parts_broken         += 1;
    EnqueueSave(DeepCopy(_state));   // async — does NOT block this handler
}
```

**Core type mapping** — read from `SaveConfig.KaijuCoreTypeMap` (a `Dictionary<string, string>` SO field):
```
{ "CARAPEX": "core_carapace", "LACERA": "core_limb", "VOLTWYRM": "core_energy" }
```
No hardcoded strings in `MetaSaveService`.

**`parts_ever_broken` set semantics**: Use `HashSet<string>` in memory; serialize as JSON array (deduplication on deserialize with `Distinct()`). The GDD states "集合語義，重複不重記".

**Suspend/Quit safety net**:
```csharp
// MonoBehaviour in App layer (bridge) or MetaSaveServiceMonoBridge:
void OnApplicationPause(bool pauseStatus) {
    if (pauseStatus) _saveService.FlushSyncNow();
}
void OnApplicationQuit() {
    _saveService.FlushSyncNow();
}

// MetaSaveService:
public void FlushSyncNow() {
    SaveData snapshot = DeepCopy(_state);
    _saveWorker.StopAndDrain();       // signal worker to stop; drain any pending snapshot
    _atomicWriter.SyncWrite(snapshot); // blocking write
}
```

**Allowed boundary loss (by design, GDD §C.6.2)**: If the process is killed in the sub-millisecond window between `EnqueueSave` and the Save Worker completing the disk write, the most recent part-break's materials may be lost. This is the explicit design trade-off — NOT a bug. The manual QA test (H.1) targets 100% pass rate in practice because `OnApplicationPause` sync write covers the common "switch app" scenario.

**Integration test note**: The event bus subscription logic is the cross-system boundary tested here. Use a fake `IEventBus` that allows controlled event publishing; inject fake `ISaveWorker` to intercept `EnqueueSave` calls; inject real `SaveData` state model. This is an **EditMode integration test** (no scene required).

The `OnApplicationPause` / `OnApplicationQuit` path requires a **PlayMode test** or documented manual QA because it involves the Unity `MonoBehaviour` lifecycle.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: `AtomicSaveWriter`, `SyncWrite`, `EnqueueSave` mechanics
- Story 007: `WeaponPodPickup` handler (first-pickup ownership write) — subscription set up here but handler logic lives in Story 007
- Material yield calculation: owned by `Economy` system (yields arrive via `PartBroke` payload per ADR-0002)
- `GameFeel` system: orbital ball animation and visual feedback for material banking
- `UI` system: "material banked" HUD counter increment animation
- `on_weapon_upgrade_confirmed` handler: belongs to the Tier upgrade feature (future story)

---

## QA Test Cases

*Authored by lead-programmer (lean mode). Developer implements against these.*

**AC-1**: PartBroke same-frame credit + EnqueueSave called

- Given: `MetaSaveService` initialized with `materials["shard_common"] = 10`, `materials["core_carapace"] = 2`; fake `IEventBus`; mock `ISaveWorker` that records `EnqueueSave` calls
- When: `PartBroke { KaijuId="CARAPEX", PartId="armored_dorsal_cannon", ShardYield=3, CoreYield=1, ... }` published
- Then: `GetMaterialCount("shard_common") == 13`; `GetMaterialCount("core_carapace") == 3`; `kaiju_records["CARAPEX"].parts_ever_broken` contains `"armored_dorsal_cannon"`; `EnqueueSave` called exactly once within the same synchronous call stack
- Edge cases: `CoreYield = 0` (NORMAL part) — `core_carapace` unchanged; `ShardYield = 0` — shard_common unchanged

**AC-2**: 27 break-scenario material accumulation (3 break_quality × 3 part_type × 3 kaiju)

- Given: `MetaSaveService` initialized with all materials = 0; 27 `PartBroke` events fired in sequence with pre-computed yields from GDD §D.1 formula
- When: all 27 events processed
- Then: each material counter equals the exact sum of yields across all events targeting it; no truncation; `int` arithmetic (verify no silent int32 wrap for plausible large counts)
- Edge cases: 4 consecutive CARAPEX armored-part breaks (all Precision quality): `core_carapace` increments by 1 per event; final count = 4 (no cap)

**AC-3**: HuntEnded all_broken=true — bonus credited, records updated

- Given: existing state `kaiju_records["CARAPEX"].full_clear_count = 1`, `hunt_count["D2"] = 0`, `best_time["D2"] = null`
- When: `HuntEnded { KaijuId="CARAPEX", IsAllPartsBroken=true, CompletionTimeS=180.0, Difficulty="D2", ShardBonus=5, EssenceYield=1 }` published
- Then: `materials["shard_common"] += 5`; `materials["essence_kaiju"] += 1`; `full_clear_count == 2`; `hunt_count["D2"] == 1`; `best_time["D2"] == 180.0`; `stats.total_full_clears` incremented; `EnqueueSave` called
- Edge cases: second hunt at D2 with time 200.0 (slower) → `best_time["D2"]` remains 180.0; third hunt at D2 with time 120.0 (faster) → `best_time["D2"] == 120.0`

**AC-4**: HuntEnded all_broken=false — no material bonus

- Given: initial `materials["shard_common"] = 10`, `materials["essence_kaiju"] = 0`
- When: `HuntEnded { IsAllPartsBroken=false, Difficulty="D1", ShardBonus=0, EssenceYield=0 }` published
- Then: `materials["shard_common"]` unchanged (still 10); `materials["essence_kaiju"]` unchanged (still 0); `hunt_count["D1"]` incremented; `full_clear_count` unchanged; `EnqueueSave` called
- Edge cases: failed hunt (player died) — same as all_broken=false for material purposes

**AC-5**: parts_ever_broken is a set — no duplicate entries

- Given: `parts_ever_broken` for CARAPEX contains `["armored_dorsal_cannon"]`
- When: second `PartBroke` for the same `"armored_dorsal_cannon"` published (e.g. chain break in a future run)
- Then: `parts_ever_broken` still contains exactly one entry `"armored_dorsal_cannon"` (set deduplication)
- Edge cases: two different parts in the same event chain — both appear in the set

**AC-6**: FlushSyncNow (suspend) writes latest state synchronously

- Given: `MetaSaveService` with pending `EnqueueSave` not yet written by worker; fake `ISaveWorker` that tracks sync writes
- When: `FlushSyncNow()` called (simulating `OnApplicationPause(true)`)
- Then: `ISaveWorker.SyncWrite()` called with the current state snapshot; worker's async queue drained; `SyncWrite` returns only after write completes (not before)
- Edge cases: `FlushSyncNow` called when no pending write — still performs a sync write of current state

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Meta/save_autosave_integration_test.cs` (EditMode — event subscription + material credit logic)
- `Assets/_Project/Tests/PlayMode/save_suspend_sync_test.cs` (PlayMode — `OnApplicationPause`/`OnApplicationQuit` lifecycle hooks) OR documented manual QA in `production/qa/evidence/save-autosave-suspend-evidence.md`
*(ADR-0005: Integration tests use fake IEventBus + injected ISaveWorker; PlayMode needed only for Unity lifecycle callbacks.)*

**Status**: [x] ✅ EditMode 10/10 GREEN (`Assets/_Project/Tests/EditMode/Meta/save_autosave_integration_test.cs`, Unity MCP, 2026-07-06). PlayMode lifecycle → manual QA doc `production/qa/evidence/save-autosave-suspend-evidence.md` (⏳ pending device run).

**Reconciliations vs story text** (IMPORTANT — significant divergence from committed architecture):
1. **PartBroke/HuntEnded carry NO material yields.** The committed events (KaijuParts) don't have `ShardYield`/`CoreYield`/`Difficulty`/`CompletionTime`. Per the economy epic's committed division of labour (ADR-0002 §3), **Economy computes yields and calls `ISaveService.CreditMaterials`**; Meta persists — Meta must NOT recompute. So story ACs "Meta reads yields from PartBroke" are replaced by testing the real path: `MetaSaveService` implements the full `ISaveService` + `IWeaponTierQuery`, and `CreditMaterials`/`SpendMaterials`/`SetWeaponTier` accumulate into `_state` + autosave same-frame.
2. **Meta subscribes PartBroke/HuntEnded only for the RECORDS/STATS it can derive from the current events**: `stats.total_parts_broken++` (PartBroke), `stats.total_runs_completed++` + `total_full_clears++` (HuntEnded all-broken). **Deferred** (need an int→string kaiju-id map + richer events with difficulty/time): per-kaiju `parts_ever_broken`, per-kaiju `full_clear_count`, `hunt_count_per_difficulty`, `best_time_per_difficulty`. Documented as a follow-up.
3. **CoreType mapping** = `MaterialKeys` (Meta) — the material key is chosen by Economy (which material id it credits), not by Meta mapping a kaiju id. No `SaveConfig.KaijuCoreTypeMap` needed.
4. Materials stored as `long` → `GetMaterialCount` clamps to `int.MaxValue` (no int32 wrap). Every persistent mutation calls `EnqueueAutosave` (depth-1 queue coalesces) so same-frame capture is guaranteed regardless of bus dispatch order.
5. `FlushSyncNow` + `MetaSaveLifecycleBridge` (MonoBehaviour, `OnApplicationPause/Quit`) implemented; App-layer AddComponent+Bind wiring is out of scope (composition root).

---

## Dependencies

- Depends on: Story 001 (SaveData, CoreType mapping), Story 002 (EnqueueSave + SyncWrite), Story 005 (MetaSaveService initialized with valid state before any event arrives)
- Unlocks: Story 007 (WeaponPodPickup handler subscription — wired in this story's event setup)
