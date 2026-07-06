# Manual QA Evidence — Save Suspend/Quit Sync (meta-save Story 006)

> **Story**: `production/epics/meta-save/story-006-autosave-on-bank.md`
> **Requirement**: TR-meta-001 (§H.1 never-lost guarantee)
> **Type**: PlayMode / device — Unity `MonoBehaviour` lifecycle, not EditMode-testable.
> **Status**: ⏳ PENDING director/device run (code path implemented + EditMode-covered for the non-lifecycle logic).

## What is automated (EditMode — DONE, GREEN)

`Assets/_Project/Tests/EditMode/Meta/save_autosave_integration_test.cs` covers the non-lifecycle logic:
- `CreditMaterials` / `SpendMaterials` accumulate + persist, enqueue an autosave **same-frame** (synchronous call stack).
- `PartBroke` → `stats.total_parts_broken++` + autosave; `HuntEnded` → run/full-clear stats + autosave.
- `FlushSync` / `FlushSyncNow` write the latest state to disk synchronously (verified by reading `save.json` back).

## What needs manual/PlayMode verification

The Unity application-lifecycle callbacks in `MetaSaveLifecycleBridge` (`OnApplicationPause(true)`,
`OnApplicationQuit`) require the runtime and cannot run headless in EditMode.

### Test 1 — Suspend sync (mobile-critical)
1. Play the game, bank some materials (break a part).
2. Background the app (home button / task switch) → triggers `OnApplicationPause(true)`.
3. Force-kill the app from the task switcher.
4. Relaunch → **expected**: banked materials from step 1 are present (0 loss).

### Test 2 — Quit sync (desktop)
1. Bank materials.
2. Quit via window close / `OnApplicationQuit`.
3. Relaunch → **expected**: banked materials present.

### Test 3 — Allowed boundary loss (design, §C.6.2)
1. Bank a part-break, then kill the process within the sub-ms window between `EnqueueAutosave` and the
   worker's disk write **without** backgrounding first (hard-kill).
2. Relaunch → the very last break's materials MAY be lost. This is the documented trade-off, NOT a bug —
   `OnApplicationPause` sync covers the common switch-app case so H.1 passes in practice.

## Wiring note for the App composition root
`MetaSaveLifecycleBridge` is a `MonoBehaviour`; the App bootstrap must `AddComponent` it on a persistent
GameObject and call `Bind(metaSaveService)` once. (App-layer wiring is not part of this story.)
