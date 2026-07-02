# Story 002: Full-Clear Essence Award

> **Epic**: 素材經濟與永久升級
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S (1–2 h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/material-economy.md`
**Requirement**: `TR-economy-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002 (primary): 事件架構與系統間通訊; ADR-0003 (secondary): 資料驅動調校
**ADR Decision Summary**: Economy subscribes to `on_hunt_end(is_all_parts_broken)` via `IEventBus`. When `is_all_parts_broken = true`, Economy independently computes essence yield and completeness shard bonus from `EconomyConfig` SO values and delivers them to `ISaveService`. No essence is awarded when `is_all_parts_broken = false`. Essence never drops from `on_part_break` — it is a hunt-end settlement reward only (GDD §C.2, §D.1).

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: Same `IEventBus.Subscribe<HuntEnded>` pattern as Story 001. No additional engine APIs required. `HuntEnded` struct defined in `Core`.

**Control Manifest Rules (Core layer)**:
- Required: Economy MUST subscribe `on_hunt_end`; deliver `essence_per_full_clear` essence and `shard_completeness_bonus` shards to `ISaveService` only when `is_all_parts_broken = true` (control-manifest §3 `Economy`; GDD §D.1 settlement formula).
- Required: `essence_per_full_clear` and `shard_completeness_bonus` MUST come from `EconomyConfig` SO — zero hard-coded values (control-manifest §1.2, ADR-0003).
- Required: Economy READS `break_quality` from events and MUST NOT recompute it — this story's handler reads `is_all_parts_broken` from `HuntEnded`, not break quality (control-manifest §4.2).
- Forbidden: MUST NOT award essence or completeness shards when `is_all_parts_broken = false` (GDD §C.2, §E.3).
- Forbidden: MUST NOT award essence via `on_part_break` handler — essence is a settlement reward, not a per-break drop (GDD §C.2; per-break drop table explicitly excludes `essence_kaiju`).
- Guardrail: Economy assembly MUST only depend on `Core` + `Content` (control-manifest §2).

---

## Acceptance Criteria

*From GDD `design/gdd/material-economy.md` §H.4, §C.2 (End-of-Hunt Bonus), §D.1 (essence formula):*

- [ ] When `on_hunt_end(is_all_parts_broken: true)` fires: `essence_kaiju` credited = `essence_per_full_clear` (default 1); `shard_common` bonus credited = `shard_completeness_bonus` (default 5).
- [ ] When `on_hunt_end(is_all_parts_broken: false)` fires: zero `essence_kaiju` credited; zero completeness shard bonus credited. Per-break shards and cores credited via Story 001 are unaffected and retained.
- [ ] Essence is never yielded by the `on_part_break` handler — essence credits originate only from `on_hunt_end`.
- [ ] Both `essence_per_full_clear` and `shard_completeness_bonus` are read from `EconomyConfig` SO — changing the SO values changes the award without code changes.

---

## Implementation Notes

*Derived from ADR-0002 §1 (Typed Event Bus) and ADR-0003 §1 (SO as config carrier):*

In `EconomyService`, subscribe via `_eventBus.Subscribe<HuntEnded>(OnHuntEnded)`. The `HuntEnded` struct (defined in `Core`) carries `IsAllPartsBroken: bool`.

In `OnHuntEnded(in HuntEnded evt)`:

```csharp
if (evt.IsAllPartsBroken)
{
    _saveService.CreditMaterials(MaterialId.EssenceKaiju, _config.EssencePerFullClear);
    _saveService.CreditMaterials(MaterialId.ShardCommon, _config.ShardCompletenessBonus);
}
// else: no bonus — do not call CreditMaterials
```

`EconomyConfig` values are injected via constructor. Unit tests inject a fixture SO with known values and a stub `ISaveService` to assert `CreditMaterials` call count and arguments. The `OnPartBroke` handler (Story 001) must never call `CreditMaterials` for `MaterialId.EssenceKaiju` — verify in test AC-3.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: Per-break shard and core yield (`on_part_break` handler).
- Story 003: Persistent storage of credited essence across sessions.
- Story 004: Consuming `essence_kaiju` in Tier 2→3 upgrade cost check.
- Story 005: Anti-degenerate TTB matrix guard.

---

## QA Test Cases

*Written by qa-lead at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Full-clear awards `essence_per_full_clear` essence and `shard_completeness_bonus` shards
  - Given: `EconomyConfig` fixture with `essence_per_full_clear=1`, `shard_completeness_bonus=5`; stub `ISaveService` tracking `CreditMaterials` calls
  - When: `HuntEnded` event fires with `is_all_parts_broken=true`
  - Then: `CreditMaterials(EssenceKaiju, 1)` called exactly once; `CreditMaterials(ShardCommon, 5)` called exactly once from the hunt-end handler; no other material credits from this handler
  - Edge cases: `essence_per_full_clear=2` (safe range max per GDD §G.1) → awards 2 essence; changing config propagates without code change

- **AC-2**: Non-full-clear awards zero essence and zero completeness bonus
  - Given: Same config; stub `ISaveService`
  - When: `HuntEnded` event fires with `is_all_parts_broken=false`
  - Then: `CreditMaterials` NOT called with `EssenceKaiju`; NOT called with `ShardCommon` for the completeness bonus (per-break credits from Story 001 are unrelated and already committed); zero net awards from `OnHuntEnded`
  - Edge cases: `HuntEnded` fires twice in the same session (edge condition from restart logic) — each invocation independently checks `is_all_parts_broken`; no cross-call accumulation state in the handler

- **AC-3**: Essence never yielded by `on_part_break` handler
  - Given: `EconomyService` with both `OnPartBroke` and `OnHuntEnded` subscribed; `PartBroke` event fires for any `break_quality` and any kaiju theme
  - When: `OnPartBroke` executes
  - Then: `CreditMaterials(EssenceKaiju, *)` is never called from `OnPartBroke`; inspection of the handler confirms no `MaterialId.EssenceKaiju` credit path exists
  - Edge cases: Final part break that completes the hunt — `on_part_break` fires first (per GDD §C.2 event ordering), then `on_hunt_end` fires separately; essence credited once only in `OnHuntEnded`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Economy/economy_essence_full_clear_test.cs` — must exist and all cases must pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE (`EconomyService` class exists with `IEventBus` subscription infrastructure established); `HuntEnded` struct defined in `Core` with `IsAllPartsBroken: bool`; `EconomyConfig` has `EssencePerFullClear` and `ShardCompletenessBonus` fields with `OnValidate` range checks
- Unlocks: Story 003 (inventory persistence needs both yield sources — per-break and full-clear — defined before integration)
