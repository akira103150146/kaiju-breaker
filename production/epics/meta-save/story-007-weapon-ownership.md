# Story 007: Weapon Ownership & Unlock Persistence

> **Epic**: 元進度與存檔系統
> **Status**: ✅ Complete (2026-07-06 — 7/7 EditMode GREEN, part of 357-case suite)
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 2h
> **Manifest Version**: 2026-07-02
> **Last Updated**: 2026-07-06

## Context

**GDD**: `design/gdd/meta-progression-system.md`
**Requirement**: `TR-meta-003`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time. Note: tr-registry.yaml not yet formalized; TR-ID inferred from GDD §H.3.)*

**ADR Governing Implementation**: ADR-0004: 存檔系統（JSON + 原子寫入 + CRC32）
**ADR Decision Summary**: `weapons[id].owned` is a monotonically increasing boolean — once true, never reverts. First pickup of a weapon pod sets `owned = true` and immediately enqueues an autosave. Second pickup of the same weapon does nothing to ownership (no duplicate event, no redundant write). New game starts with `owned = true` for starting weapons (from `SaveConfig.StartingOwnedWeapons`) and `owned = false` for all others. Weapon tier upgrade does NOT grant ownership.

Secondary ADR: ADR-0002 (`WeaponPodPickup` event struct in `Core`; subscription in `Meta`)

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**:
- No post-cutoff engine risk for this story — pure C# state mutation + event subscription. The `WeaponPodPickup` event is published by the Stage system; Meta subscribes via `IEventBus`.
- The `notify_ui(WEAPON_UNLOCKED, weapon_id)` call from GDD §C.2.2 is implemented as publishing a `WeaponUnlocked` Core event (not a direct UI call) — consistent with ADR-0002 / ADR-0005 assembly boundaries.

**Control Manifest Rules (Feature layer — Meta)**:
- Required: `owned` flag transitions only from `false → true`; never `true → false`; enforced by guard in handler
- Required: `WeaponUnlocked` event published on `IEventBus` (not a direct UI call); UI subscribes independently
- Required: `ISaveService.IsWeaponOwned(WeaponId)` returns the current in-memory owned state (not a disk read)
- Forbidden: Tier upgrade must not set `owned = true` (explicit guard)
- Forbidden: Duplicate unlock notification on second pickup (guard on `is_first_time` or owned check)
- Forbidden: Hardcoded weapon IDs in handler — all weapon IDs from `SaveConfig.ActiveWeaponIds`

---

## Acceptance Criteria

*From GDD `design/gdd/meta-progression-system.md` §C.2 + §H.3, scoped to this story:*

- [ ] New game: `ISaveService.IsWeaponOwned("L1") == true`; `IsWeaponOwned("M1") == true`; `IsWeaponOwned("L2") == false`; all 8 weapons in `SaveConfig.ActiveWeaponIds` have correct starting owned state per `SaveConfig.StartingOwnedWeapons`
- [ ] `WeaponPodPickup` handler: when `evt.IsFirstTime == true` (or equivalently `weapons[id].owned == false`), sets `weapons[id].owned = true`, calls `EnqueueSave(DeepCopy(_state))`, publishes `WeaponUnlocked { WeaponId = id }` event on `IEventBus`
- [ ] `owned = true` persists across application restart: save → reload → `IsWeaponOwned(id)` still returns `true` for the newly unlocked weapon
- [ ] Second pickup of the same weapon (`weapons[id].owned == true`): `owned` remains `true`; `EnqueueSave` is NOT called; `WeaponUnlocked` event is NOT published
- [ ] `WeaponUpgradeConfirmed` event (any tier 0→1, 1→2, 2→3): `weapons[id].owned` unchanged — tier upgrade does NOT set `owned` (explicit test: verify owned=false before upgrade, owned=false after)
- [ ] All 8 weapon IDs can transition from `owned=false` to `owned=true` independently; each transition is reflected in `ISaveService.IsWeaponOwned()` immediately (same-frame)
- [ ] `parts_ever_broken` set is not mutated by ownership unlock (ownership and part-break tracking are independent)

---

## Implementation Notes

*Derived from ADR-0004 Decision §6 + GDD §C.2.2:*

**`WeaponPodPickup` handler (GDD §C.2.2 pseudocode)**:
```csharp
void OnWeaponPodPickup(in WeaponPodPickup evt) {
    if (!_state.weapons[evt.WeaponId].owned) {
        _state.weapons[evt.WeaponId] = _state.weapons[evt.WeaponId] with { owned = true };
        EnqueueSave(DeepCopy(_state));
        _eventBus.Publish(new WeaponUnlocked { WeaponId = evt.WeaponId });
    }
    // Regardless of ownership: Stage/Weapons system handles equip logic
    // Meta only manages the persistent owned flag
}
```

**Separation of concerns**:
- **Stage system** determines whether a weapon pod pickup is `is_first_time` (by querying `ISaveService.IsWeaponOwned` before the pickup event is published, or via the `IsFirstTime` flag in the `WeaponPodPickup` payload).
- **Meta** only acts on the `IsFirstTime` flag or performs its own `owned` check — either approach is valid as long as the check happens before the state mutation.
- **Weapons / Stage** system handles the in-run equip (switching active weapon) — completely independent from the ownership write.
- **UI system** subscribes to `WeaponUnlocked` event to show the unlock notification.

**Monotonic guarantee**: The `owned` field has no setter that can set it to `false` from outside `MetaSaveService`. Expose only `IsWeaponOwned(WeaponId)` as a read-only query; the write path is exclusively through the `WeaponPodPickup` handler.

**Round-trip test**: Call `SyncWrite` after the unlock, then call `LoadOrDefault` in the same test to verify the owned state survives deserialization. This round-trip test is the canonical proof that ownership is truly persistent.

**Subscription**: The `WeaponPodPickup` subscription is set up in `MetaSaveService.Initialize()` alongside the other subscriptions wired in Story 006. This story implements the handler body.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 005: New-game starting owned state initialization (`InitializeNewGame` builds `weapons[*].owned` from `SaveConfig.StartingOwnedWeapons`)
- Story 006: Event subscription setup (`_eventBus.Subscribe<WeaponPodPickup>(OnWeaponPodPickup)` wired in Story 006's initialize block)
- Stage system: weapon pod drop rules, which weapons appear in which stages, `IsFirstTime` flag computation
- UI system: `WeaponUnlocked` notification dialog / animation
- Weapon Tier upgrade: `on_weapon_upgrade_confirmed` handler (future story — not this epic's scope)
- Loadout fallback for unowned weapons: covered in Story 005 (`ValidateLastLoadout`)

---

## QA Test Cases

*Authored by lead-programmer (lean mode). Developer implements against these.*

**AC-1**: New-game starting ownership matches SaveConfig

- Given: `SaveConfig.StartingOwnedWeapons = ["L1","M1"]`; `ActiveWeaponIds = ["L1","L2","L3","L4","M1","M2","M3","M4"]`
- When: `MetaSaveService.InitializeNewGame(config)` called
- Then: `IsWeaponOwned("L1") == true`; `IsWeaponOwned("M1") == true`; `IsWeaponOwned("L2") == false`; `IsWeaponOwned("L3") == false`; `IsWeaponOwned("L4") == false`; `IsWeaponOwned("M2") == false`; `IsWeaponOwned("M3") == false`; `IsWeaponOwned("M4") == false` (all 8 checked)
- Edge cases: repeat `InitializeNewGame` → same result (no accumulation)

**AC-2**: First pickup unlocks weapon, triggers save + event

- Given: `IsWeaponOwned("L2") == false`; mock `IEventBus` tracking published events; mock `ISaveWorker` tracking `EnqueueSave` calls
- When: `WeaponPodPickup { WeaponId = "L2", IsFirstTime = true }` published
- Then: `IsWeaponOwned("L2") == true`; `EnqueueSave` called once; `WeaponUnlocked { WeaponId = "L2" }` event published on `IEventBus`
- Edge cases: pickup of M4 (unowned) → M4 owned=true, no effect on M3 or any other weapon

**AC-3**: Second pickup of owned weapon — no state change, no redundant write/event

- Given: `IsWeaponOwned("L2") == true` (already unlocked)
- When: `WeaponPodPickup { WeaponId = "L2", IsFirstTime = false }` published
- Then: `IsWeaponOwned("L2")` still `true`; `EnqueueSave` NOT called; `WeaponUnlocked` NOT published
- Edge cases: `WeaponPodPickup` with `IsFirstTime = true` but `owned` already `true` in memory (defensive check) — same result, no duplicate write

**AC-4**: Ownership round-trip — persists across save + load

- Given: service initialized from new game; `WeaponPodPickup { "L3", IsFirstTime=true }` processed; `SyncWrite` called
- When: new `MetaSaveService` instance created and `LoadOrDefault` + `Initialize` run on the written file
- Then: `IsWeaponOwned("L3") == true` on the reloaded instance; `IsWeaponOwned("L2") == false` (unaffected by L3 unlock)
- Edge cases: save file has been through the CRC32 write-then-read cycle (integrity check must pass after unlock write)

**AC-5**: Weapon tier upgrade does NOT grant ownership

- Given: `IsWeaponOwned("L3") == false`
- When: `WeaponUpgradeConfirmed { WeaponId = "L3", NewTier = 1, ... }` event published (simulated)
- Then: `IsWeaponOwned("L3") == false` (unchanged); only `weapons["L3"].tier` changes
- Edge cases: tier upgrade for an already-owned weapon (L1 tier 0→1) — owned remains true; tier update does not reset owned

**AC-6**: Concurrent unlocks of multiple weapons in same session

- Given: new game state (L1, M1 owned; rest locked)
- When: `WeaponPodPickup` events fired for L2, L3, M2 in sequence (three pickups)
- Then: `IsWeaponOwned("L2") == true`; `IsWeaponOwned("L3") == true`; `IsWeaponOwned("M2") == true`; `EnqueueSave` called 3 times (once per new unlock); L4/M3/M4 still false
- Edge cases: after 3 unlocks, second pickup of L2 → no 4th EnqueueSave call

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/Meta/save_weapon_ownership_test.cs` — must exist and all tests pass
*(ADR-0005: EditMode test assembly. All 8 weapon IDs and all state transitions covered. AC-4 round-trip test uses `AtomicSaveWriter` + `SaveLoader` with a temporary in-memory or temp-path file.)*

**Status**: [x] ✅ 7/7 GREEN (`Assets/_Project/Tests/EditMode/Meta/save_weapon_ownership_test.cs`, Unity MCP, 2026-07-06). Covers AC-1 (new-game ownership all 8), AC-2 (first pickup unlock+save+event), AC-3 (second pickup no-op), AC-4 (round-trip through CRC32 save/load), AC-5 (tier upgrade doesn't grant ownership, +owned stays through tier change), AC-6 (independent multi-unlock, one save each, dup no-op).

**Reconciliations vs story text** (surfaced for review):
1. Uses the committed `WeaponPodGrabbed` event (stage-001) as the pickup signal — Meta does its own owned-check (monotonic false→true), so no `WeaponPodPickup{IsFirstTime}` event is needed (the story explicitly permits Meta's own check). Handler subscribed in `MetaSaveService.Initialize`.
2. `WeaponUnlocked` event added to Core (`WeaponEvents.cs`); Meta publishes it on first unlock, never calls UI.
3. AC-5's tier path exercised via the real `ISaveService.SetWeaponTier` (no `WeaponUpgradeConfirmed` event is committed) — verified owned unchanged before/after.

---

## Dependencies

- Depends on: Story 001 (WeaponSaveData DTO, ISaveService), Story 002 (EnqueueSave), Story 005 (InitializeNewGame sets starting ownership), Story 006 (subscription setup wired in Initialize)
- Unlocks: None — this is the final story in the epic
