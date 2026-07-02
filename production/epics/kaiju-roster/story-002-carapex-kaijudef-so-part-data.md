# Story 002: CARAPEX KaijuDef SO & Part Data

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju/01-carapex.md` §4 (Part Composition), §9 (Material Drops)
**Requirement**: `TR-kaiju-002` (ARMORED data), `TR-kaiju-004` (CARAPEX drop theme)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: Data-Driven Config via ScriptableObjects
**ADR Decision Summary**: All kaiju configuration — parts, adjacency graph, drop_table_id, H_max/B_max overrides, world-layout anchor positions — lives in a `KaijuDef` ScriptableObject asset under `Assets/_Project/Content/Kaiju/`. GDD YAML paths (`assets/data/kaiju/carapex.yaml`) are engine-agnostic placeholders; the Unity source of truth is the SO asset. OnValidate enforces safe ranges.

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: Pure SO authoring; no post-cutoff API risk. `OnValidate` uses standard Unity serialization. No Entities/Burst dependency.

**Control Manifest Rules (Feature layer — Content tier)**:
- Required: Asset at `Assets/_Project/Content/Kaiju/Carapex.asset` (KaijuDef type); `OnValidate` must emit Inspector warnings for invalid adjacency or unknown drop_table_id. All balance values (H_max defaults, stagger_duration) read from `PartSystemConfig` — never duplicated in this asset.
- Forbidden: Hardcoded H_max/B_max numeric literals in the SO class definition; player-variable data (current heat, current BU) in SO.
- Guardrail: `H_max_override = null` must cause runtime fallback to global `PartSystemConfig.h_max_normal` (CARAPEX uses all-global defaults per GDD §4). Asset must pass `OnValidate` with zero Inspector errors before story closes.

---

## Acceptance Criteria

*From `design/gdd/kaiju/01-carapex.md` §4, §9, §10 AC-02 and AC-04:*

- [ ] `KaijuDef` asset `Carapex.asset` created at `Assets/_Project/Content/Kaiju/Carapex.asset` with `kaiju_id = "carapex"`, `difficulty_tier = 1`
- [ ] **4 parts** authored in `PartDef[]`: `chest_reactor_core` (BOSS_CORE), `left_mandible` (NORMAL), `right_mandible` (NORMAL), `dorsal_cannon` (ARMORED)
- [ ] All 4 parts have `H_max_override = null` and `B_max_override = null` (global defaults apply: NORMAL=100/100, ARMORED=150/150, BOSS_CORE=200/200)
- [ ] **Adjacency graph** correct and bidirectional after SO loads:
  - `chest_reactor_core` adjacent to: `left_mandible`, `right_mandible`, `dorsal_cannon`
  - `left_mandible` adjacent to: `chest_reactor_core`, `right_mandible`
  - `right_mandible` adjacent to: `chest_reactor_core`, `left_mandible`
  - `dorsal_cannon` adjacent to: `chest_reactor_core` only
- [ ] **Drop table IDs** assigned: `chest_reactor_core` → `drop_carapex_core`; `left_mandible` / `right_mandible` → `drop_carapex_normal`; `dorsal_cannon` → `drop_carapex_armored`
- [ ] All 3 drop_table_id values exist as entries in `EconomyConfig` and resolve to `core_carapace` as the core material (甲殼系 theme rule)
- [ ] **World layout**: `dorsal_cannon` anchor x-position and `chest_reactor_core` anchor x-position differ by ≤ 5% of screen width (vertical alignment for L4 niche — confirmed in asset inspector and noted in SO's design_note field)
- [ ] `dorsal_cannon` part has `armor_type = ARMORED` and `stagger_duration` reference points to global `PartSystemConfig.stagger_duration` (2.0 s) — not a local override
- [ ] `Carapex.asset` passes `OnValidate` with zero Inspector error logs

---

## Implementation Notes

*Derived from ADR-0003 §Decision and GDD §4 YAML inline:*

Create `Assets/_Project/Content/Kaiju/Carapex.asset` as a `KaijuDef` ScriptableObject. Populate fields matching the inline YAML from `design/gdd/kaiju/01-carapex.md` §4. The YAML's `null` overrides map to `KaijuDef.PartDef.hMaxOverride = -1` (sentinel for "use global") — or use `bool useGlobalH_max = true` per the `KaijuDef` schema (check existing `KaijuDef.cs` definition before deciding sentinel value).

Adjacency is declared one-directionally in the SO; the runtime `KaijuParts` system derives bidirectionality (per `kaiju-part-system.md C.6`). Do not manually mirror every edge in the asset — let the system derive it. OnValidate should warn if any adjacency target part_id does not exist in the same KaijuDef's parts list.

`dorsal_cannon` part should have a boolean `isArmored = true` (or be declared with `PartType.Armored`) — do not model the armor gate logic in this SO; that belongs to `KaijuParts`. This story only authors the data; AC-02 armor gate behavior is verified in Story 004.

For the vertical-alignment acceptance criterion: use a `[Header("Design Notes")]` `[TextArea] string designNote` field in `PartDef` (or `KaijuDef`) to record the L4 alignment note. The x-position values are authored as normalized screen-fraction anchor floats in `PartDef.anchorPositionNormalized`.

Phase speed multiplier knob: add `phase2SpeedMult = 1.15f` to `KaijuDef` (with OnValidate range check 1.0–2.0). This is the `carapex_phase2_dorsal_speed_mult` knob from GDD §8.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 003: EmitterPatternSO definitions for Pattern A/B/C (attack patterns are separate assets)
- Story 004: ARMORED armor gate runtime behavior (`dorsal_cannon` BU=0 when ARMOR_INTACT); phase transitions firing; tutorial loop validation
- Story 001: KaijuBehaviorController phase state machine

---

## QA Test Cases

*Config/Data story — smoke check (advisory):*

- **AC-1**: Asset structure smoke check
  - Setup: Open `Carapex.asset` in Unity Inspector
  - Verify: 4 parts listed; all part_ids match GDD names; PartType values correct; adjacency lists match GDD §4 table; drop_table_ids match GDD §9 table
  - Pass condition: Zero red Inspector errors from OnValidate; all fields populated (no empty strings)

- **AC-2**: Drop theme alignment
  - Setup: Open `EconomyConfig.asset`; search for `drop_carapex_core`, `drop_carapex_normal`, `drop_carapex_armored`
  - Verify: All 3 entries exist and each resolves core material to `core_carapace`
  - Pass condition: No missing entries; core material = `core_carapace` for all 3 table IDs

- **AC-3**: Vertical alignment check
  - Setup: Read `chest_reactor_core.anchorPositionNormalized.x` and `dorsal_cannon.anchorPositionNormalized.x` in Inspector
  - Verify: |x_dorsal − x_core| ≤ 0.05 (5% screen width)
  - Pass condition: Values within tolerance; design_note field populated with L4 alignment rationale

- **AC-4**: OnValidate passes
  - Setup: Enter Play mode or trigger OnValidate (right-click asset → Re-Validate if available)
  - Verify: No red Console errors logged by `Carapex.asset`
  - Pass condition: Console clear of Kaiju SO validation errors

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- `production/qa/smoke-carapex-kaijudef.md` — smoke check pass doc

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `KaijuDef` SO class definition must exist in `Content` assembly (may require coordination with engine-programmer if not yet scaffolded)
- Unlocks: Story 004 (CARAPEX encounter integration needs this asset)
