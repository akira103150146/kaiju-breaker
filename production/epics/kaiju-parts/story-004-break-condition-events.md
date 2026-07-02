# Story 004: Break Condition & Event Emission

> **Epic**: 可破壞部位系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju-part-system.md`
**Requirement**: `TR-part-001`, `TR-part-004`, `TR-part-005`, `TR-part-008`
*(TR-IDs derived from GDD §H.1, §H.4, §H.5, §H.8 — registry not yet formalised)*

**ADR Governing Implementation**: ADR-0002: 事件架構 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: KaijuParts subscribes to `MissileHit` events; computes `M_state_mult` by D.3 lookup table using live `armor_state`, `heat_state`, `stagger_timer`, and `part_type`; updates `B_current`; when `B_current >= required_break_threshold_[type]`, immediately sets `break_state = BROKEN`, computes `break_quality` from the live part state at that exact frame, and publishes `PartBroke` with the full 8-field payload. If `part_type == BOSS_CORE`, publishes `BossCoreBreak` immediately after in the same synchronous call stack. KaijuParts is the SOLE emitter of `PartBroke` and `BossCoreBreak`. Payload carries all downstream-needed data one-time; no receiver may call back to query state (avoids time-ordering coupling).

**Engine**: Unity 6 | **Risk**: LOW
**Engine Notes**: `readonly struct` events with `in` parameter; synchronous `IEventBus.Publish` — zero GC in hot path. Re-entrancy: M3 chain (Story 005) calls `TriggerPartBreak` within `OnPartBroke` handler — guarded by `is_chain_break` flag (non-recursive per GDD §E.4). No post-cutoff APIs.

**Control Manifest Rules (this layer — KaijuParts)**:
- Required: KaijuParts is the SOLE publisher of `PartBroke` and `BossCoreBreak`; Weapons MUST NOT publish these (§3 KaijuParts, §4.2)
- Required: `break_quality` computed at break frame from live `heat_state` and `stagger_timer` — SOFTENED_STAGGERED if (SOFTENED AND stagger_timer > 0); SOFTENED if (SOFTENED AND stagger_timer ≤ 0); NORMAL otherwise (§3 KaijuParts)
- Required: `PartBroke` payload carries all 8 fields in one call: `PartId`, `KaijuId`, `PartType`, `WorldPosition`, `DropTableId` (non-null/non-empty), `BreakQuality`, `AdjacencyList`, `IsChainBreak` (§3 KaijuParts, §4.2)
- Required: event order for BOSS_CORE — `PartBroke` published first, then `BossCoreBreak`, same synchronous frame (§H.4 GDD, §4.2 manifest)
- Required: BROKEN is terminal — all subsequent `LaserHit`, `MissileHit`, `L3WaveHit` return immediately; `H_current` and `B_current` reset to 0 on break (§3 KaijuParts)
- Forbidden: `shard_yield` / `core_yield` in `PartBroke` payload — Economy calculates yield independently (§3 Economy, §4.2)
- Forbidden: NORMAL or ARMORED part break triggering `BossCoreBreak` (§H.4 GDD)

---

## Acceptance Criteria

*From GDD §H.1, §H.4, §H.5, §H.8, §D.3, §C.2, §E.6, §E.7, scoped to this story:*

- [ ] H.1 / D.3 — `M_state_mult` lookup table implements all 6 rows from GDD D.3: ARMORED+ARMOR_INTACT→`0`; ARMORED+ARMOR_STRIPPED(stagger>0)→`1.5`; NORMAL/BOSS_CORE+INTACT+stagger=0→`B_unsoftened_mult(0.35)`; NORMAL/BOSS_CORE+SOFTENED+stagger=0→`1.0`; NORMAL/BOSS_CORE+stagger>0 (any heat_state)→`stagger_break_mult(1.5)`; SOFTENED+STAGGERED→`1.5` (direct lookup, not 1.0×1.5)
- [ ] D.3 — `B_fill = break_delta_base × M_state_mult`; `B_current = clamp(B_current + B_fill, 0, B_max)`
- [ ] D.3 — When `B_current >= required_break_threshold_[type]`: `break_state = BROKEN`, `H_current = 0`, `B_current = 0`; `TriggerPartBreak` invoked
- [ ] H.1 / E.7 — BROKEN is irreversible; all subsequent hit events on a BROKEN part return immediately — no field changes, no events
- [ ] `break_quality` computed at break frame: `SOFTENED_STAGGERED` if heat_state==SOFTENED AND stagger_timer>0; `SOFTENED` if heat_state==SOFTENED AND stagger_timer<=0; `NORMAL` otherwise
- [ ] H.8 — Every `PartBroke` payload has non-null, non-empty `DropTableId`; assert at call site; throw `InvalidOperationException` if violated (data-integrity guard catches bad KaijuDef assets early)
- [ ] `AdjacencyList` in `PartBroke` payload: the raw `part.AdjacencyList` from KaijuDef (Story 001); Story 005 will update these lists to bidirectional graph values at graph-build time, but the field is populated here
- [ ] H.4 — BOSS_CORE part: `PartBroke` published first, then `BossCoreBreak(kaiju_id, world_position)` in the same call stack
- [ ] H.4 — NORMAL or ARMORED part breaking: `PartBroke` published; `BossCoreBreak` NOT published
- [ ] H.5 — BROKEN part after a full `InitializeParts` reset (new round): `break_state=ALIVE`, `H_current=0`, `B_current=0` — state fully reset; prior BROKEN is not carried over

---

## Implementation Notes

*Derived from ADR-0002 and ADR-0003:*

- Subscribe to `MissileHit` on `IEventBus` in `PartStateSystem`.
- `HandleMissileHit(in MissileHit evt)`:
  ```csharp
  var part = _parts[evt.PartId];
  if (part.BreakState == BreakState.Broken) return;
  float mult = LookupStateMult(part);
  float bFill = evt.BreakDeltaBase * mult;
  part.BCurrent = Mathf.Clamp(part.BCurrent + bFill, 0f, part.BMax);
  if (part.BCurrent >= GetBreakThreshold(part.PartType))
      TriggerPartBreak(part, isChainBreak: false);
  ```
- `LookupStateMult(BreakablePart part)` — pure function; priority order: (1) ARMORED+ARMOR_INTACT → 0; (2) stagger_timer > 0 → `_config.StaggerBreakMult`; (3) SOFTENED → 1.0f; (4) else → `_config.BUnsoftenedMult`. Note: rows 2 and 3 share stagger_timer > 0 check — stagger takes priority, producing 1.5 regardless of heat_state (this covers the SOFTENED+STAGGERED=1.5 row without double-multiplication).
- `TriggerPartBreak(BreakablePart part, bool isChainBreak)`:
  ```csharp
  var bq = ComputeBreakQuality(part);
  Debug.Assert(!string.IsNullOrEmpty(part.DropTableId), "DropTableId null/empty — bad KaijuDef");
  part.BreakState = BreakState.Broken;
  part.HCurrent = 0f;
  part.BCurrent = 0f;
  _bus.Publish(new PartBroke {
      PartId = part.PartId, KaijuId = part.KaijuId, PartType = part.PartType,
      WorldPosition = GetWorldPosition(part.PartId),
      DropTableId = part.DropTableId, BreakQuality = bq,
      AdjacencyList = part.AdjacencyList, IsChainBreak = isChainBreak });
  if (part.PartType == PartType.BossCore)
      _bus.Publish(new BossCoreBreak { KaijuId = part.KaijuId,
          WorldPosition = GetWorldPosition(part.PartId) });
  ```
- `ComputeBreakQuality`: `SOFTENED_STAGGERED` if `heat_state==Softened && stagger_timer > 0`; `SOFTENED` if `heat_state==Softened`; `NORMAL` otherwise.
- `GetWorldPosition`: delegates to scene-graph wiring (part's Transform.position); stubbed with `Vector2.zero` in unit tests.
- `PartBroke` and `BossCoreBreak` structs declared in `Core` assembly, implement `IGameEvent`.
- Threshold lookup: `GetBreakThreshold(PartType t)` → switch on t returning the corresponding `_config` field.
- Tests use `FakeEventBus` recording publish order; inject fixture `PartSystemConfig`; call `HandleMissileHit` directly with controlled `BreakDeltaBase`; assert event sequence, payload fields, and part state.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `BreakablePart` data structure, `DropTableId` assignment from `KaijuDef`, `part_regen_enabled`
- Story 002: `heat_state` transitions; `heat_state` field read here but managed by Story 002
- Story 003: `armor_state` and `stagger_timer`; fields consumed read-only in `LookupStateMult`
- Story 005: M3 Tier-3 chain (`TriggerPartBreak` with `isChainBreak=true` is CALLED from Story 005's `OnPartBroke` handler); adjacency graph; bidirectional `AdjacencyList` values in payload
- Story 006: VFX for break explosion, SOFTENED/BROKEN visual readability

---

## QA Test Cases

*Written by qa-lead at story creation. Implement against these — do not invent new test cases during implementation.*

- **AC-1**: `LookupStateMult` — all 6 table rows
  - Given: parts configured for each row in GDD D.3
  - When: `LookupStateMult` called for each configuration
  - Then: ARMORED+ARMOR_INTACT→`0`; ARMORED+ARMOR_STRIPPED+stagger>0→`1.5`; NORMAL+INTACT+stagger=0→`0.35`; NORMAL+SOFTENED+stagger=0→`1.0`; NORMAL+stagger>0→`1.5`; NORMAL+SOFTENED+stagger>0→`1.5` (direct lookup, same value, not a double-mult)
  - Edge cases: BOSS_CORE follows same rules as NORMAL; `stagger_timer=0.001` (barely positive) → stagger branch triggers (1.5)

- **AC-2**: B_fill computation and clamp
  - Given: NORMAL SOFTENED part (mult=1.0), `B_current=80`, `B_max=100`, `break_delta_base=30`
  - When: `HandleMissileHit(break_delta_base=30)`
  - Then: `B_fill=30`; `B_current` would be 110 but clamped to 100 → threshold reached → `TriggerPartBreak` called; `B_current` reset to 0; `PartBroke` published
  - Edge cases: mult=0 → `B_fill=0`, `B_current` unchanged; `break_delta_base=0` → guard with early return or no-op

- **AC-3**: SOFTENED+STAGGERED → mult=1.5 (not double-multiplied)
  - Given: NORMAL part, `heat_state=SOFTENED`, `stagger_timer=1.0s`; `break_delta_base=100`
  - When: `HandleMissileHit`
  - Then: `B_fill = 100 × 1.5 = 150` (not 100 × 1.0 × 1.5 = 150 — same numeric result but test confirms the code path hits the stagger branch, not SOFTENED branch then multiplied again)
  - Edge cases: trace through `LookupStateMult` to verify stagger-priority branch is taken, not SOFTENED=1.0 branch

- **AC-4**: `break_quality` computed correctly at break frame
  - Given three scenarios at the moment `B_current >= threshold`:
    - (a) `heat_state=SOFTENED`, `stagger_timer=1.5s`
    - (b) `heat_state=SOFTENED`, `stagger_timer=0`
    - (c) `heat_state=INTACT`, `stagger_timer=0`
  - When: `TriggerPartBreak` called in each
  - Then: `PartBroke.BreakQuality` = (a) `SOFTENED_STAGGERED`, (b) `SOFTENED`, (c) `NORMAL`
  - Edge cases: (d) `heat_state=INTACT`, `stagger_timer=1.0s` → `NORMAL` (stagger alone does not upgrade quality); quality uses live state at break frame, not cached earlier

- **AC-5**: `DropTableId` non-null/non-empty guard
  - Given: part with `drop_table_id=""` (empty string — invalid KaijuDef)
  - When: `TriggerPartBreak` called
  - Then: `InvalidOperationException` thrown (or assertion fails loudly); no `PartBroke` published for invalid data
  - Edge cases: `drop_table_id="drop_normal_tier1"` → passes guard; `PartBroke.DropTableId == "drop_normal_tier1"`

- **AC-6**: BOSS_CORE break fires `PartBroke` then `BossCoreBreak`; ordering enforced
  - Given: BOSS_CORE part reaching threshold; `FakeEventBus` recording publish order
  - When: `TriggerPartBreak(part, isChainBreak=false)`
  - Then: recorded events in order = `[PartBroke, BossCoreBreak]`; `BossCoreBreak.KaijuId` matches part's `kaiju_id`
  - Edge cases: NORMAL part break → only `[PartBroke]` recorded, no `BossCoreBreak`; ARMORED part break → same

- **AC-7**: BROKEN part ignores all hit events
  - Given: part `break_state=BROKEN`, `B_current=0`, `H_current=0`
  - When: `MissileHit`, `LaserHit`, `L3WaveHit` all dispatched
  - Then: all fields unchanged; no events published; handlers return at first guard
  - Edge cases: E.8 — multiple missiles same frame all blocked once BROKEN set (first missile triggers break; remaining return immediately via guard)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/KaijuParts/EditMode/kaijuparts_break_condition_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (`heat_state` field live on `BreakablePart`) must be DONE; Story 003 (`armor_state`, `stagger_timer` fields live) must be DONE
- Unlocks: Story 005 (M3 chain calls `TriggerPartBreak(isChainBreak=true)` defined here; `PartBroke` event consumed by M3 handler); Story 006 (`PartBroke` event live for readability verification)
