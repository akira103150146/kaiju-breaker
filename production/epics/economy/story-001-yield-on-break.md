# Story 001: Part-Break Material Yield Computation

> **Epic**: 素材經濟與永久升級
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S (2–3 h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/material-economy.md`
**Requirement**: `TR-economy-002`, `TR-economy-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002 (primary): 事件架構與系統間通訊; ADR-0003 (secondary): 資料驅動調校
**ADR Decision Summary**: Economy subscribes to `on_part_break` via `IEventBus`. It reads `break_quality` and `kaiju_id` from the `PartBroke` struct payload, then **independently computes** `shard_yield` and `core_yield` using `EconomyConfig` SO values — zero hard-coded. Yield multipliers and kaiju-theme→core mapping live exclusively in `EconomyConfig`. Economy MUST NOT read pre-computed yields from the payload; the payload does not carry them.

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: No post-cutoff APIs required. `IEventBus.Subscribe<PartBroke>` uses the stable generic pattern defined in `Core`. `EconomyConfig` is a standard `ScriptableObject`.

**Control Manifest Rules (Core layer)**:
- Required: `Economy` MUST subscribe `on_part_break` and compute shard/core yield from `break_quality` + `kaiju_id` independently (control-manifest §3 `Economy`).
- Required: Economy READS `break_quality` from the `PartBroke` event; MUST NOT recompute it. `KaijuParts` owns `break_quality` calculation and places it in the payload (control-manifest §4.2).
- Required: All yield multipliers and kaiju theme→core mappings MUST come from `EconomyConfig` SO — zero hard-coded values (control-manifest §1.2, ADR-0003).
- Forbidden: MUST NOT read shard/core yield from the `PartBroke` payload — payload does not carry them; `KaijuParts` MUST NOT put them there (control-manifest §3 `Economy`, §3 `KaijuParts`).
- Forbidden: MUST NOT reference `KaijuParts` assembly directly — only communicate via `Core` event structs (control-manifest §1.4).
- Guardrail: Economy assembly MUST only depend on `Core` + `Content` (control-manifest §2 Layer table).

---

## Acceptance Criteria

*From GDD `design/gdd/material-economy.md` §H.2 and §H.4, scoped to per-break yield:*

- [ ] Standard quality break: `shard_yield = floor(shard_base × 1.0)`. Core yield = 1 for any kaiju theme.
- [ ] Precision quality break: `shard_yield = floor(shard_base × shard_precision_mult)`. Core yield = 1.
- [ ] Perfect quality break: `shard_yield = floor(shard_base × shard_perfect_mult)`. Core yield = 2 when `core_perfect_double_drop = TRUE`; core yield = 1 when `core_perfect_double_drop = FALSE`.
- [ ] Kaiju theme→core mapping is correct: CARAPEX (甲殼系) → `core_carapace`; LACERA (肢體系) → `core_limb`; VOLTWYRM (能量系) → `core_energy`.
- [ ] Mapping is determined by `kaiju_id` / kaiju theme only — `part_type` (NORMAL / ARMORED / BOSS_CORE) does not change which core type is yielded or the core count.
- [ ] All 3 kaiju themes yield their respective theme core on every part break, regardless of part_type — no zero-core breaks at any quality level (minimum 1 core always; 2 only at Perfect with `core_perfect_double_drop = TRUE`).
- [ ] No `shard_yield` or `core_yield` field exists in the `PartBroke` event struct — Economy computes both independently after receiving the event.
- [ ] Automated test `Assets/_Project/Tests/Economy/economy_yield_on_break_test.cs` passes, covering all 3 × 3 × 3 = 27 primary scenarios (3 `break_quality` × 3 `part_type` × 3 kaiju themes) per GDD §H.2.

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines (§1 Typed Event Bus, §3 Event Ownership) and ADR-0003 §1 (SO as config carrier):*

Subscribe in `EconomyService` constructor via `_eventBus.Subscribe<PartBroke>(OnPartBroke)`. The `PartBroke` struct is defined in `Core`; its fields match the GDD F.1 signature: `part_id`, `kaiju_id`, `part_type`, `world_position`, `drop_table_id`, `break_quality`, `adjacency_list`, `is_chain_break`.

In `OnPartBroke(in PartBroke evt)`:
1. Compute `shard_yield = Mathf.FloorToInt(_config.ShardBase * _config.QualityShardMult[evt.BreakQuality])`.
2. Resolve `kaijuTheme` from `evt.KaijuId` via `_config.KaijuThemeCoreMap` (or via `IKaijuDataQuery` if theme must be looked up from `KaijuDef` — confirm interface availability before implementing; do not reference `KaijuParts` assembly).
3. Look up `coreType = _config.KaijuThemeCoreMap[kaijuTheme]`.
4. Compute `coreYield = (evt.BreakQuality == BreakQuality.SoftenedStaggered && _config.CorePerfectDoubleDrop) ? 2 : 1`.
5. Call `_saveService.CreditMaterials(MaterialId.ShardCommon, shardYield)` and `_saveService.CreditMaterials(coreType, coreYield)` in the same call frame.

All multipliers and mapping tables come from the injected `EconomyConfig` SO — no magic numbers in code. `EconomyConfig` is injected via constructor; unit tests inject a fixed-value fixture SO and a stub `ISaveService`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Full-clear essence award (`on_hunt_end` handler).
- Story 003: Persistent inventory storage and cross-session save/load round-trip.
- Story 004: Upgrade transaction cost check, material deduction, and tier application.
- Story 005: TTB matrix anti-dominant-loadout guard.

---

## QA Test Cases

*Written by qa-lead at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Standard quality yields `floor(shard_base × 1.0)` shards and 1 core for all kaiju themes
  - Given: `EconomyConfig` fixture with `shard_base=2`, `shard_precision_mult=1.5`, `shard_perfect_mult=2.0`, `core_perfect_double_drop=TRUE`; kaiju theme CARAPEX; `part_type=NORMAL`
  - When: `PartBroke` event fires with `break_quality=NORMAL`
  - Then: `CreditMaterials(ShardCommon, 2)` called; `CreditMaterials(core_carapace, 1)` called; no other material credits
  - Edge cases: Repeat for LACERA (→`core_limb`) and VOLTWYRM (→`core_energy`) — all yield `shard_yield=2`, `core_yield=1`; `part_type` changes to ARMORED and BOSS_CORE produce identical yields

- **AC-2**: Precision quality yields `floor(shard_base × shard_precision_mult)` shards and 1 core
  - Given: Same config; kaiju theme LACERA; `part_type=ARMORED`
  - When: `PartBroke` event fires with `break_quality=SOFTENED`
  - Then: `CreditMaterials(ShardCommon, 3)` called (floor(2×1.5)=3); `CreditMaterials(core_limb, 1)` called
  - Edge cases: Non-integer floor: `shard_base=3`, `shard_precision_mult=1.5` → `floor(4.5)=4` (floor, not round); verify floor not ceiling

- **AC-3**: Perfect quality yields `floor(shard_base × shard_perfect_mult)` shards and 2 cores when `core_perfect_double_drop=TRUE`
  - Given: Config with `core_perfect_double_drop=TRUE`; kaiju theme VOLTWYRM; `part_type=BOSS_CORE`
  - When: `PartBroke` event fires with `break_quality=SOFTENED_STAGGERED`
  - Then: `CreditMaterials(ShardCommon, 4)` called (floor(2×2.0)=4); `CreditMaterials(core_energy, 2)` called
  - Edge cases: `core_perfect_double_drop=FALSE` → `CreditMaterials(core_energy, 1)` — yields 1, not 2; Standard and Precision always yield `core_yield=1` regardless of the flag

- **AC-4**: Core type determined by kaiju theme only — all 9 theme × part_type combinations at Precision quality
  - Given: Each of 3 kaiju themes × 3 part_types; `break_quality=SOFTENED`
  - When: 9 `PartBroke` events fired
  - Then: Core type maps exclusively by theme: CARAPEX→`core_carapace`, LACERA→`core_limb`, VOLTWYRM→`core_energy`; `shard_yield` identical across part_types for same quality and theme
  - Edge cases: Unknown `kaiju_id` with no theme registration → implementation throws descriptive `ArgumentException` (fail loud, fail fast; do not silently award wrong core)

- **AC-5**: `PartBroke` event struct carries no pre-computed yield fields
  - Given: `PartBroke` struct definition in `Core`
  - When: Struct fields are inspected (compile-time check or reflection test)
  - Then: Struct contains no `ShardYield`, `CoreYield`, or any pre-computed material field; Economy computes both independently

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Economy/economy_yield_on_break_test.cs` — must exist and all 27 scenario cases must pass (BLOCKING per GDD §H.2)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `Core` `IEventBus` + `PartBroke` struct defined (core-foundation); `EconomyConfig` ScriptableObject class with `QualityShardMult`, `KaijuThemeCoreMap`, `CorePerfectDoubleDrop` fields in `Content` (content-config); `ISaveService.CreditMaterials(MaterialId, int)` declared in `Core` (interface only — Meta implementation not required for this story)
- Unlocks: Story 002 (essence full-clear uses same `IEventBus` subscription pattern); Story 003 (inventory persistence depends on yield delivery being defined)
