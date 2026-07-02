# Story 003: Material Inventory — Persistence Handoff to Meta-Save

> **Epic**: 素材經濟與永久升級
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M (3–4 h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/material-economy.md`
**Requirement**: `TR-economy-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002 (primary): `ISaveService` query interface + same-frame synchronous delivery; ADR-0003 (secondary): static/mutable data separation
**ADR Decision Summary**: Economy delivers computed yield (from Stories 001–002) to `ISaveService.CreditMaterials` in the **same frame** as the originating event fires — synchronous same-frame semantics are required by ADR-0002. `Meta` implements `ISaveService` and owns all persistent `PlayerProgress.material_inventory` state. Economy never owns or reads back inventory state — it is a push-only producer. `Meta` enqueues a background autosave after each credit. Player inventory is stored in the JSON save (ADR-0004), never written to SO (ADR-0003 read-only principle).

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `ISaveService` is a `Core` interface; `Meta` implementation uses `Application.persistentDataPath` with atomic JSON write (ADR-0004). No post-cutoff APIs required on the Economy side of this integration. Atomic write, CRC32, and background worker are `Meta` concerns — out of scope here.

**Control Manifest Rules (Core layer)**:
- Required: Economy MUST deliver credited materials to `ISaveService` in the same frame as `on_part_break` or `on_hunt_end` fires — synchronous same-frame semantics (control-manifest §4.2, §3 `Meta`).
- Required: Economy READS `break_quality` from events and MUST NOT recompute it — this story integrates the delivery of already-computed yields (Stories 001/002) to `ISaveService` (control-manifest §4.2).
- Required: `Meta` MUST credit inventory in memory and enqueue autosave in the same call (control-manifest §3 `Meta`); Economy depends only on the `ISaveService` interface contract.
- Required: Player inventory (`material_inventory`) and weapon tiers MUST persist to JSON save — never to SO. SO is read-only design data (control-manifest §1.2, §3 `Meta`).
- Forbidden: Economy MUST NOT own or cache inventory counts — `ISaveService` / `Meta` owns all player progress state (control-manifest §2 Layer table).
- Forbidden: Economy MUST NOT reference `Meta` assembly directly — only via the `ISaveService` interface declared in `Core` (control-manifest §1.4).
- Guardrail: Materials credited mid-fight (per `on_part_break`) are retained after failure / quit — never-lost rule (GDD §E.1). `enqueue_save` fires same frame ensures this.

---

## Acceptance Criteria

*From GDD `design/gdd/material-economy.md` §H.1 (end-to-end loop), §E.1 (never-lost), §F.5 (save data structure):*

- [ ] Materials computed by Economy (Stories 001, 002) are delivered to `ISaveService.CreditMaterials` in the same frame as the originating event — no deferred delivery.
- [ ] After game restart: `material_inventory` in save matches inventory at last quit exactly — cross-session persistence round-trip with zero loss or duplication.
- [ ] After battle failure or mid-fight quit: materials credited from already-broken parts (via `on_part_break`) are fully retained in save; no rollback of credited materials.
- [ ] `essence_kaiju` and `shard_completeness_bonus` credited at `on_hunt_end` are persisted with the same guarantee as per-break materials.
- [ ] Economy never reads back from `ISaveService` to get inventory totals — it is a push-only producer; inventory query is the responsibility of UI / upgrade system via `ISaveService` read methods.
- [ ] `ISaveService` interface (not `Meta` concrete class) is the only dependency injected into `EconomyService` — confirmed by assembly reference inspection.

---

## Implementation Notes

*Derived from ADR-0002 §2 (query interfaces, DI injection), ADR-0003 §2 (static/mutable separation), and GDD §F.5:*

`EconomyService` receives `ISaveService` via constructor injection (already present from Stories 001/002). No structural changes to `EconomyService` are required — this story's work is the integration test verifying that the push already implemented in Stories 001/002 actually persists correctly through a save round-trip.

The `ISaveService` interface in `Core` must declare:
- `CreditMaterials(MaterialId id, int amount)` — in-memory increment + enqueue autosave.
- Read methods (`GetMaterialCount(MaterialId id)`, etc.) — used by UI and Story 004, not by Economy.

The data structure expected by `Meta` from GDD §F.5:
```
PlayerProgress {
    weapon_tiers:       Map<WeaponID, int>        // 0–3
    material_inventory: Map<MaterialID, int>       // uncapped non-negative int
}
```

Integration test strategy: construct `EconomyService` with a real `ISaveService` stub or lightweight `Meta` implementation backed by a temp file. Fire a sequence of `PartBroke` and `HuntEnded` events, then serialize and deserialize the save, then assert inventory totals match. Verify that a second independent `ISaveService` instance loaded from the same temp file returns the same values.

For the never-lost guarantee: fire `PartBroke` for 2 of 4 parts, do NOT fire `HuntEnded`, serialize save, reload, confirm inventory has credits for 2 parts only and zero essence.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: Per-break yield computation logic.
- Story 002: Essence and completeness bonus computation logic.
- Story 004: Deducting materials on upgrade (spend side of inventory).
- Story 005: TTB matrix guard.
- Meta epic: Atomic JSON write, `.bak` backup, CRC32 verification, background Save Worker thread, version migration chain — owned by `Meta` system, not Economy.

---

## QA Test Cases

*Written by qa-lead at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Materials delivered to `ISaveService` in same frame as `PartBroke` event
  - Given: `EconomyService` with injected stub `ISaveService` that records the call frame index (via a frame counter incremented in the test harness, not `Time.frameCount` — use deterministic counter for EditMode); `PartBroke` event published on frame counter N
  - When: `IEventBus.Publish<PartBroke>(evt)` called
  - Then: `CreditMaterials` recorded on frame N — same frame, not deferred; stub confirms synchronous dispatch
  - Edge cases: Multiple `PartBroke` events in the same frame (chain break: `is_chain_break=true`) — each credited independently in the same frame; no batching deferred to next frame

- **AC-2**: Cross-session persistence round-trip — inventory survives serialize/deserialize
  - Given: `ISaveService` implementation backed by a temp file path; `EconomyService` processes events crediting 3 `shard_common` and 2 `core_carapace`
  - When: Save is written (sync flush), then a new `ISaveService` instance is loaded from the same temp file
  - Then: Reloaded `material_inventory[shard_common] == 3`; `material_inventory[core_carapace] == 2`; no material lost or duplicated
  - Edge cases: Zero-amount credit (implementation should be a no-op, not write a zero entry or corrupt existing count); reload from a fresh empty save returns all-zero inventory without error or exception

- **AC-3**: Mid-fight quit — credited materials retained, non-credited materials not awarded
  - Given: 4-part fight; `PartBroke` events fire for parts A and B (credits processed); parts C and D not broken; `HuntEnded` NOT fired (quit mid-fight)
  - When: Save serialized, then reloaded
  - Then: Inventory contains shard/core credits from parts A and B only; zero `essence_kaiju` (hunt not completed); zero `shard_completeness_bonus`; no rollback of A and B credits; no phantom credit from incomplete hunt
  - Edge cases: Quit immediately after `PartBroke` fires (same frame as credit call) — autosave enqueued same frame per control-manifest §3 `Meta`; credit is in save if `Meta` flushes on quit via `on_app_suspend` sync write

- **AC-4**: Economy does not reference `Meta` assembly — interface-only dependency verified
  - Given: `Economy.asmdef` file
  - When: Assembly references are inspected at compile time or via Unity project validation
  - Then: `Economy.asmdef` lists `Core` and `Content` as references; `Meta` is NOT listed; project compiles correctly without `Meta` in Economy's reference set

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Economy/economy_inventory_persistence_test.cs` — must exist and all cases must pass
- Note: AC-2 and AC-3 round-trip tests may require an EditMode test with a lightweight file-backed stub; if `Meta` PlayMode integration is needed for AC-3 flush verification, create a separate PlayMode test entry and mark it in the evidence doc

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE; Story 002 DONE; `ISaveService` interface declared in `Core` with `CreditMaterials(MaterialId, int)` and `GetMaterialCount(MaterialId)` methods; `Meta` system's `ISaveService` implementation must exist (or a conforming file-backed stub for integration test purposes)
- Unlocks: Story 004 (upgrade transaction needs to deduct from the same inventory that this story integrates into persistence)
