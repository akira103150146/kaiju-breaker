# Story 001: Part Entity & Two-Bar Data Model

> **Epic**: 可破壞部位系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju-part-system.md`
**Requirement**: `TR-part-005`, `TR-part-007`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; note: TR-IDs derived from GDD §H — registry not yet formalised)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 (primary); ADR-0002: 事件架構 (secondary)
**ADR Decision Summary**: All gameplay tuning knobs live in ScriptableObjects under `Assets/_Project/Content/`, executed read-only at runtime. `PartSystemConfig` SO owns all global part knobs (H_max/B_max/theta_S/decay/thresholds/chain params). `KaijuDef` SO owns per-kaiju part definitions, per-part overrides, adjacency declarations, and `drop_table_id` strings. `IPartStateQuery` is declared in `Core` and DI-injected into consuming systems (`Weapons`, `UI`). Static config is strictly separated from player-mutable save data (ADR-0004).

**Engine**: Unity 6 | **Risk**: LOW
**Engine Notes**: SO `OnValidate` safe-range assertions use standard Unity Inspector; no post-cutoff APIs. `KaijuDef` contains `PartDef[]` — serialise as `[System.Serializable]` inner class, not SO sub-asset, to keep drag-and-drop simple and avoid asset explosion. Nullable float for overrides: use `bool hasOverride + float overrideValue` pattern or `Optional<float>` wrapper (Unity does not natively serialise `float?`).

**Control Manifest Rules (this layer — KaijuParts / Core)**:
- Required: all balance values from SO (`Assets/_Project/Content/`); `OnValidate` range checks on every tuning-knob field (§1.2)
- Required: `IPartStateQuery` in `Core` assembly; `KaijuParts` assembly depends only on `Core` + `Content` (§2, §3 Core)
- Required: `PartType` and `BreakQuality` enums live in `Core` as shared types (§3 Core)
- Required: `part_regen_enabled` field on `PartSystemConfig` is always `false`; no runtime write path exists (§3 KaijuParts)
- Forbidden: any difficulty-system write path modifying part knobs at runtime; DOTS types must not appear in this assembly (§5)
- KaijuParts OWNS `on_part_break` and `break_quality`; this story creates the data substrate those events carry in Stories 004–005

---

## Acceptance Criteria

*From GDD `design/gdd/kaiju-part-system.md` §C.1, §H.5, §H.7, scoped to this story:*

- [ ] `PartSystemConfig` SO exposes all §G knobs with `OnValidate` safe-range assertions: `H_max_normal` [80–150], `H_max_armored` [120–200], `H_max_boss_core` [160–280], `H_decay_rate` [1–8], `theta_S` [80–120], `theta_S_exit` [60–90] (must be `< theta_S`), `B_max_normal/armored/boss_core`, `B_unsoftened_mult` [0.20–0.50], `stagger_break_mult` [1.2–2.0], `required_break_threshold_normal/armored/boss_core`, `stagger_duration` [1.5–3.0], `l2_t3_adjacent_heat_pct` [0.20–0.50], `m3_t3_chain_dmg_mult` [1.0–2.0], `m3_t3_chain_max_targets` {1,2}, `adjacency_max_neighbors` [2–6], `softened_visual_onset_max_s` [0.25–0.5], `part_regen_enabled` (always `false`)
- [ ] `KaijuDef` SO holds `PartDef[]`; each `PartDef` carries: `part_id` (String, unique within kaiju), `part_type` (PartType), `H_max_override` (nullable float), `B_max_override` (nullable float), `adjacency` (String[]), `drop_table_id` (String, non-empty)
- [ ] `BreakablePart` runtime class holds all §C.1 fields: `part_id`, `kaiju_id`, `part_type`, `H_current` (float, clamped [0, H_max]), `H_max`, `B_current` (float, clamped [0, B_max]), `B_max`, `heat_state` (init: INTACT), `armor_state` (ARMORED parts init: ARMOR_INTACT; others: N/A), `stagger_timer` (init: 0), `break_state` (init: ALIVE), `adjacency_list` (String[], raw from KaijuDef), `drop_table_id`
- [ ] `H_max_override` / `B_max_override`: non-null value overrides global SO default for that part; null uses the global knob for the part's type
- [ ] `IPartStateQuery` interface declared in `Core` assembly: `GetHeatState(part_id)`, `GetArmorState(part_id)`, `GetWorldPosition(part_id)`, `GetHeatCurrent(part_id)`, `GetHeatMax(part_id)`; `PartStateSystem` implements it
- [ ] H.5 — `part_regen_enabled` returns `false` at all difficulty settings D1–D4; no method signature accepts a difficulty parameter that could return `true`
- [ ] H.5 — `InitializeParts(KaijuDef)` resets all parts to `break_state=ALIVE`, `H_current=0`, `B_current=0`, `heat_state=INTACT`, `armor_state=ARMOR_INTACT` (ARMORED only), `stagger_timer=0`; calling it a second time (new round) produces clean state — no carry-over from prior BROKEN parts
- [ ] H.7 — Reading any `PartSystemConfig` knob is identical across D1–D4; no difficulty-system module has a write path into `PartSystemConfig`

---

## Implementation Notes

*Derived from ADR-0003 and ADR-0002 Implementation Guidelines:*

- Define `PartSystemConfig : ScriptableObject` in `Content` assembly. Use `[Range]` attributes for Inspector clamping; `OnValidate` enforces `theta_S_exit < theta_S` and `part_regen_enabled == false` (log error + revert if violated).
- Define `KaijuDef : ScriptableObject` in `Content` assembly with `PartDef` as a `[System.Serializable]` inner class. Use `bool H_max_has_override` + `float H_max_override_value` pair for nullable pattern (Unity cannot serialise `float?` directly).
- Define `BreakablePart` as a plain C# class (not MonoBehaviour) in `KaijuParts` assembly. `PartStateSystem` owns a `Dictionary<string, BreakablePart>` keyed by `part_id`.
- `PartStateSystem` receives `PartSystemConfig` and `KaijuDef` via constructor injection; `App` (composition root) wires DI. `PartStateSystem` implements `IPartStateQuery`.
- `PartType` enum (`NORMAL`, `ARMORED`, `BOSS_CORE`) and `BreakQuality` enum (`NORMAL`, `SOFTENED`, `SOFTENED_STAGGERED`) declared in `Core` assembly (shared types per manifest §3).
- H_max resolution: `part.HMax = partDef.H_max_has_override ? partDef.H_max_override_value : SelectGlobalMax(partDef.PartType, config)`.
- Difficulty invariance: `PartSystemConfig` is a flat SO with no difficulty parameter. `DifficultyConfig` (separate SO) owns only bullet-density knobs — there is no cross-write path. A static analysis check (grep for `PartSystemConfig` writes outside `Content`) can verify this in CI.
- Tests create fixture SOs via `ScriptableObject.CreateInstance<PartSystemConfig>()` with explicit field assignments; no inline magic numbers per coding standards (§1.6).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: `on_laser_hit` handler, heat fill/decay formula (D.1), INTACT↔SOFTENED transition (D.2)
- Story 003: `on_l3_wave_hit` handler, armor-gate and stagger-timer countdown (D.4)
- Story 004: `on_missile_hit` handler, D.3 break-bar formula, `on_part_break` emission
- Story 005: Bidirectional adjacency graph construction from `PartDef.adjacency` declarations
- Story 006: VFX event payload verification and SOFTENED visual onset timing

---

## QA Test Cases

*Written by qa-lead at story creation. Implement against these — do not invent new test cases during implementation.*

- **AC-1**: `PartSystemConfig` default values match GDD §G
  - Given: `PartSystemConfig` created via `ScriptableObject.CreateInstance` with Unity field defaults
  - When: each knob read
  - Then: `H_max_normal=100`, `theta_S=100`, `theta_S_exit=80`, `H_decay_rate=3`, `B_max_normal=100`, `B_unsoftened_mult=0.35`, `stagger_break_mult=1.5`, `stagger_duration=2.0`, `part_regen_enabled=false`
  - Edge cases: `OnValidate` with `theta_S_exit=100, theta_S=100` logs error; `part_regen_enabled=true` assignment triggers error and reverts

- **AC-2**: `H_max_override` applied when set; global knob used when null
  - Given: `PartDef` with `part_type=NORMAL`, `H_max_has_override=true`, `H_max_override_value=200`; `config.H_max_normal=100`
  - When: `PartStateSystem.InitializeParts(kaijuDef)` runs
  - Then: `part.HMax == 200`
  - Edge cases: `H_max_has_override=false` → `part.HMax == 100`; ARMORED with no override → `part.HMax == config.H_max_armored (150)`

- **AC-3**: `InitializeParts` produces correct initial state for all three part types
  - Given: `KaijuDef` with one NORMAL, one ARMORED, one BOSS_CORE part
  - When: `InitializeParts` called
  - Then: all have `H_current=0`, `B_current=0`, `break_state=ALIVE`, `stagger_timer=0`, `heat_state=INTACT`; ARMORED has `armor_state=ARMOR_INTACT`; NORMAL and BOSS_CORE have armor_state field = N/A (or sentinel)
  - Edge cases: calling `InitializeParts` twice resets all fields including any previously modified `B_current`

- **AC-4**: `part_regen_enabled` always false across all difficulty contexts
  - Given: `PartSystemConfig` (any instance)
  - When: `part_regen_enabled` read
  - Then: returns `false`; no public setter or method exists that could return `true`
  - Edge cases: reflection test — assigning `true` via `SerializedObject` triggers `OnValidate` error

- **AC-5**: `IPartStateQuery` returns correct initial values
  - Given: `PartStateSystem` initialised with one NORMAL part (`part_id="left_wing"`, `H_max=100`)
  - When: `GetHeatCurrent("left_wing")` and `GetHeatMax("left_wing")` called
  - Then: returns `0f` and `100f` respectively
  - Edge cases: unknown `part_id` → `KeyNotFoundException` or documented sentinel; empty `KaijuDef` (no parts) → dictionary empty, query throws or returns sentinel

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/KaijuParts/EditMode/kaijuparts_entity_model_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None — foundational story; implements `IEventBus` and `Core` types must already exist (core-foundation prerequisite outside this epic)
- Unlocks: Story 002, Story 003, Story 004, Story 005, Story 006
