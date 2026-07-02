# Story 008: VOLTWYRM KaijuDef SO & Part Data (Vertical Pierce Corridor)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju/03-voltwyrm.md` §4 (Part Composition), §9 (Material Output)
**Requirement**: `TR-kaiju-004` (VOLTWYRM drop theme, core_energy), `TR-kaiju-002` (dual-shield ARMORED data)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: Data-Driven Config via ScriptableObjects
**ADR Decision Summary**: VOLTWYRM's `KaijuDef` SO defines 7 parts: 4 NORMAL neck segments in a vertical chain, 2 ARMORED energy shields, 1 BOSS_CORE. The vertical chain adjacency (neck_seg_1↔2↔3↔4↔core_node) is the structural data that enables the L4 Vertical Pierce Corridor — a key game design requirement (weapon-system.md open question #2). All 7 parts use global H_max/B_max defaults; no overrides.

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: Pure SO authoring. 7 parts is the largest part count in the roster (≤8 max per kaiju-part-system.md A section update). No DOTS dependency.

**Control Manifest Rules (Feature layer — Content tier)**:
- Required: `Voltwyrm.asset` at `Assets/_Project/Content/Kaiju/Voltwyrm.asset`; all 4 neck segments and core_node must share the same normalized x-anchor position (±1% tolerance; verified by OnValidate); `OnValidate` checks `adjacency_max_neighbors ≤ 4` for all parts; `core_node` must have exactly 3 neighbors (neck_seg_4, shield_left, shield_right) — within the 4-neighbor limit.
- Forbidden: Hardcoded H_max/B_max numeric literals; player-variable data in SO; adjacency count > 4 for any part.
- Guardrail: Part count = 7; OnValidate must error if count ≠ 7 (this boss's design integrity depends on exact part count). Drop table IDs must all resolve to `core_energy` (energy-系 theme — no other core type permitted for VOLTWYRM).

---

## Acceptance Criteria

*From `design/gdd/kaiju/03-voltwyrm.md` §4.1, §4.2, §4.3, §9.1:*

- [ ] `KaijuDef` asset `Voltwyrm.asset` created at `Assets/_Project/Content/Kaiju/Voltwyrm.asset` with `kaiju_id = "voltwyrm"`
- [ ] **7 parts** authored in `PartDef[]`:
  - `neck_seg_1` (NORMAL); lowest segment, closest to player
  - `neck_seg_2` (NORMAL)
  - `neck_seg_3` (NORMAL)
  - `neck_seg_4` (NORMAL); directly below `core_node`
  - `shield_left` (ARMORED)
  - `shield_right` (ARMORED)
  - `core_node` (BOSS_CORE); top of vertical stack; receives dual shield adjacency
- [ ] All 7 parts have `H_max_override = null` and `B_max_override = null` (globals: NORMAL=100/100, ARMORED=150/150, BOSS_CORE=200/200)
- [ ] **Vertical chain adjacency** correct (bidirectional after system derivation):
  - `neck_seg_1` ↔ `neck_seg_2`
  - `neck_seg_2` ↔ `neck_seg_3`
  - `neck_seg_3` ↔ `neck_seg_4`
  - `neck_seg_4` ↔ `core_node`
  - `shield_left` ↔ `core_node`
  - `shield_right` ↔ `core_node`
  - `neck_seg_1` NOT adjacent to `shield_left` or `shield_right` (shields only link to core)
- [ ] **Vertical column layout**: all 5 vertical parts (`neck_seg_1` through `neck_seg_4` and `core_node`) share anchor x-position within ±1% of screen width (L4 Pierce Corridor requirement — measured via OnValidate)
- [ ] `core_node` adjacency count = 3 (`neck_seg_4`, `shield_left`, `shield_right`) — within `adjacency_max_neighbors = 4` limit; OnValidate confirms
- [ ] **Drop table IDs** assigned and all resolving to `core_energy` in EconomyConfig:
  - `neck_seg_1`, `neck_seg_2`, `neck_seg_3`, `neck_seg_4` → `drop_voltwyrm_seg`
  - `shield_left`, `shield_right` → `drop_voltwyrm_shield`
  - `core_node` → `drop_voltwyrm_core`
  - All 3 distinct drop_table_ids confirmed in `EconomyConfig` → `core_energy`
- [ ] **7-part performance note** field on `KaijuDef`: SO has a `designNote` or `performanceBudgetNote` text field referencing "7/7 parts — max-adjacent Boss; confirm 7-object pool headroom with lead-programmer before Vertical Slice review" (VOLTWYRM §10.7 performance budget check)
- [ ] `Voltwyrm.asset` passes `OnValidate` with zero Inspector error logs

---

## Implementation Notes

*Derived from ADR-0003 §Decision and GDD §4.3 inline YAML:*

VOLTWYRM has no moving part data (unlike LACERA) — all parts are stationary relative to the snake body. The snake body has an S-curve drift animation (Phase 1/2) and Phase 3 high-frequency jitter — these are body-level animations authored as `BodyMovementSpec` on the KaijuDef root:

```csharp
body_movement = {
    pattern = "S_drift",
    phase1_amplitude_screen_pct = 25,
    phase2_amplitude_screen_pct = 35,
    phase3_core_stationary = true,   // core_node stops moving in Phase 3
    sprint_interval_s = 10.0f        // Phase 2 lateral sprint interval (D2 default)
}
```

The neck segment jitter in Phase 3 is a body animation override, not a `PartMovementSpec`. The `core_node` in Phase 3 is explicitly stationary (a Boolean flag `core_stationary_in_phase3 = true`). These are visual parameters for the movement animator, not part combat data.

OnValidate additions for VOLTWYRM:
- **Error** if part count ≠ 7.
- **Error** if any neck_seg or core_node anchor x-position differs by > 1% screen width from the first neck_seg's x-position (vertical column integrity).
- **Error** if `core_node.adjacency.Count != 3`.
- **Warning** if any ARMORED part's adjacency list contains anything other than `core_node` (shields should only link to core, per GDD §4.2 design intent).

Note from GDD §10.7: the `kaiju-part-system.md` A section was updated to allow up to 8 parts for late-game bosses. OnValidate should allow up to 8 (not 7). The ≤8 limit is the SO's authoritative guard.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 009: VOLTWYRM EmitterPatternSO definitions (Patterns A/B/C and core direct-fire)
- Story 010: Runtime encounter integration — dual-shield armor gate, vertical pierce 100 HU/s validation, phase transitions, performance budget confirmation
- Story 001: KaijuBehaviorController (phase framework used by all 3 bosses)

---

## QA Test Cases

*Config/Data story — smoke check (advisory):*

- **AC-1**: Part count = 7
  - Setup: Open `Voltwyrm.asset` in Inspector
  - Verify: `PartDef[]` array length = 7; all 7 part_ids present and match GDD §4.1 table
  - Pass condition: Zero Inspector errors; count = 7 confirmed

- **AC-2**: Vertical column alignment
  - Setup: Inspect anchor x-positions of neck_seg_1 through neck_seg_4 and core_node
  - Verify: All 5 x-positions within ±1% of screen width of each other (e.g., if screen width = 1280, all x-values within ±12.8 px of each other)
  - Pass condition: OnValidate x-alignment check passes; no errors logged

- **AC-3**: Adjacency graph — vertical chain + shield branches
  - Setup: Inspect adjacency list for each part
  - Verify: neck_seg_1=[neck_seg_2]; neck_seg_2=[neck_seg_1,neck_seg_3]; ...; neck_seg_4=[neck_seg_3,core_node]; shield_left=[core_node]; shield_right=[core_node]; core_node=[neck_seg_4,shield_left,shield_right]
  - Pass condition: core_node has exactly 3 neighbors; no shield-to-seg adjacency; matches GDD §4.2 diagram

- **AC-4**: Drop table → core_energy theme
  - Setup: Open `EconomyConfig.asset`; search for drop_voltwyrm_seg, drop_voltwyrm_shield, drop_voltwyrm_core
  - Verify: All 3 entries exist; each resolves to `core_energy` as core material type
  - Pass condition: VOLTWYRM 能量系 theme satisfied; no `core_carapace` or `core_limb` in VOLTWYRM tables

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- `production/qa/smoke-voltwyrm-kaijudef.md` — smoke check pass doc

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `KaijuDef` SO class with `BodyMovementSpec` sub-struct and 8-part OnValidate limit (from Story 002/005 baseline; coordinate if not yet present)
- Unlocks: Story 010 (VOLTWYRM encounter integration)
