# Story 005: LACERA KaijuDef SO & Part Data (Moving Parts)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju/02-lacera.md` §4 (Parts & Data), §9 (Material Output)
**Requirement**: `TR-kaiju-004` (LACERA drop theme), `TR-kaiju-007` (moving part world_position data definition)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: Data-Driven Config via ScriptableObjects
**ADR Decision Summary**: LACERA's `KaijuDef` SO is the authoritative source for all 6 parts (4 NORMAL sweep-arc limbs, 1 ARMORED tail, 1 BOSS_CORE head), their adjacency graph, drop_table_ids, and — critically — their movement specification (`sweep_arc`, `oscillate`, or `stationary_relative` with full parameters). The movement spec is pure data; the runtime animation/position update lives in `KaijuParts`. This story authors the data only.

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: Pure SO authoring. No DOTS dependency. Movement parameters (arc_half_deg, speed_deg_per_s, phase_rad) are serialized floats on `PartMovementSpec` — a sub-struct of `PartDef`.

**Control Manifest Rules (Feature layer — Content tier)**:
- Required: `KaijuDef` asset `Lacera.asset` at `Assets/_Project/Content/Kaiju/Lacera.asset`; movement spec on all moving parts (all except `head_core`); phase_rad values for all sweep-arc parts must be mutually distinct (OnValidate: warn if any two fore/hind limbs share identical phase_rad); `OnValidate` passes with zero errors.
- Forbidden: Hardcoded movement speeds in C# — all `speed_deg_per_s` values authored in SO. Duplicating `stagger_duration` locally — read from `PartSystemConfig`.
- Guardrail: Adjacency follows GDD 4.2 graph exactly (head_core only connects to fore limbs and tail; hind limbs only connect to corresponding fore limb). No cross-side connections (hind_limb_left MUST NOT be adjacent to hind_limb_right).

---

## Acceptance Criteria

*From `design/gdd/kaiju/02-lacera.md` §4.1, §4.2, §4.3, §9.1:*

- [ ] `KaijuDef` asset `Lacera.asset` created at `Assets/_Project/Content/Kaiju/Lacera.asset` with `kaiju_id = "lacera"`, `kaiju_tier = 2`, `role = "movement_boss"`
- [ ] **6 parts** authored in `PartDef[]`:
  - `head_core` (BOSS_CORE); movement type = `stationary_relative` (follows body_movement, no independent arc)
  - `fore_limb_left` (NORMAL); movement type = `sweep_arc`; arc_half_deg=60, speed_deg_per_s=45.0, phase_rad=0.0, pivot_bone="shoulder_left"
  - `fore_limb_right` (NORMAL); movement type = `sweep_arc`; arc_half_deg=60, speed_deg_per_s=45.0, phase_rad=3.14159 (π), pivot_bone="shoulder_right"
  - `hind_limb_left` (NORMAL); movement type = `sweep_arc`; arc_half_deg=90, speed_deg_per_s=30.0, phase_rad=1.5708 (π/2), pivot_bone="hip_left"
  - `hind_limb_right` (NORMAL); movement type = `sweep_arc`; arc_half_deg=90, speed_deg_per_s=30.0, phase_rad=4.7124 (3π/2), pivot_bone="hip_right"
  - `tail_carapace` (ARMORED); movement type = `oscillate`; arc_half_deg=30, speed_deg_per_s=20.0, phase_rad=0.0, pivot_bone="tail_base"
- [ ] All 6 parts have `H_max_override = null` and `B_max_override = null` (global defaults: NORMAL=100/100, ARMORED=150/150, BOSS_CORE=200/200)
- [ ] **Body movement spec** on `KaijuDef`: `body_movement.pattern = "vertical_drift"`, `amplitude_screen_pct = 5`, `speed_cycles_per_min = 12`
- [ ] **Adjacency graph** correct per GDD §4.2:
  - `head_core` ↔ `fore_limb_left`, `fore_limb_right`, `tail_carapace`
  - `fore_limb_left` ↔ `hind_limb_left`
  - `fore_limb_right` ↔ `hind_limb_right`
  - `hind_limb_left` NOT adjacent to `hind_limb_right` (no cross-side connection)
- [ ] **Drop table IDs** — all 6 parts use drop tables resolving to `core_limb` (肢體系 kaiju_lacera theme):
  - `head_core` → `drop_lacera_core`
  - `fore_limb_left`, `fore_limb_right`, `hind_limb_left`, `hind_limb_right` → `drop_lacera_limb`
  - `tail_carapace` → `drop_lacera_tail`
  - All 3 distinct drop_table_id values confirmed in `EconomyConfig` → `core_limb`
- [ ] **Phase_rad uniqueness**: all 4 sweep-arc limbs have distinct phase_rad values (0, π, π/2, 3π/2 — no two identical)
- [ ] `Lacera.asset` passes `OnValidate` with zero Inspector error logs

---

## Implementation Notes

*Derived from ADR-0003 §Decision and GDD §4.3 inline YAML:*

`PartDef` requires a `PartMovementSpec` sub-struct with fields:
```csharp
[Serializable]
public struct PartMovementSpec
{
    public PartMovementType movementType;  // Stationary, SweepArc, Oscillate
    public string pivotBoneName;           // bone ID string; empty if stationary
    public float arcHalfDeg;              // total arc = 2 × this
    public float speedDegPerS;
    public float phaseRad;                // sinusoidal phase offset
}
```

`body_movement` is a `BodyMovementSpec` at the `KaijuDef` root level (not per-part):
```csharp
[Serializable]
public struct BodyMovementSpec
{
    public BodyMovementPattern pattern;    // VerticalDrift, Hover, etc.
    public float amplitudeScreenPct;       // ±5 for LACERA
    public float speedCyclesPerMin;        // 12 for LACERA
}
```

The runtime system (`KaijuParts`) reads these values each frame to compute `world_position` for each moving part. This story does not implement the runtime update — that is Story 007.

OnValidate additions for LACERA:
- Warn if any two limb `phaseRad` values are within 0.1 rad of each other (synchronization risk).
- Warn if `fore_limb_left` and `fore_limb_right` do NOT have phase_rad offset of π ±0.01 (design intent: anti-phase).
- Error if `hind_limb_left` is listed as adjacent to `hind_limb_right` (forbidden per GDD §4.2).

The `tail_carapace` movement type `oscillate` uses the same `PartMovementSpec` struct as `sweep_arc` — just with a different `PartMovementType` enum value. The runtime interpolation formula is the same sinusoidal sweep; `oscillate` is semantically identical at the data level (distinguish only for editor labeling clarity).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: LACERA EmitterPatternSO definitions (Pattern A/B/C)
- Story 007: Runtime world_position update per frame; M1 tracking against dynamic position; L4 vertical window; phase encounter firing integration
- Story 001: KaijuBehaviorController phase framework

---

## QA Test Cases

*Config/Data story — smoke check (advisory):*

- **AC-1**: Part count and type check
  - Setup: Open `Lacera.asset` in Inspector
  - Verify: 6 parts listed; types = [BOSS_CORE, NORMAL×4, ARMORED×1]; all part_ids match GDD §4.1 table
  - Pass condition: Zero Inspector errors; all 6 parts present with correct PartType

- **AC-2**: Movement spec populated for all moving parts
  - Setup: Expand each `PartDef` in Inspector; check `PartMovementSpec` sub-struct
  - Verify: All 4 limbs have `movementType = SweepArc`; `tail_carapace` = Oscillate; `head_core` = Stationary; all arc/speed/phase values match GDD §4.3 YAML
  - Pass condition: No zero-value fields on any moving part; phase_rad values are 0, π, π/2, 3π/2

- **AC-3**: Adjacency graph correctness
  - Setup: Inspect `adjacency` lists for each part
  - Verify: head_core lists [fore_limb_left, fore_limb_right, tail_carapace]; hind_limb_left lists [fore_limb_left] only; hind_limb_right lists [fore_limb_right] only
  - Pass condition: No cross-side connections; no self-loops; matches GDD §4.2 graph exactly

- **AC-4**: Drop theme alignment (core_limb for all parts)
  - Setup: Open `EconomyConfig.asset`; find drop_lacera_core, drop_lacera_limb, drop_lacera_tail
  - Verify: All 3 entries exist; core material = `core_limb` for all 3
  - Pass condition: LACERA 肢體系 theme satisfied; no `core_carapace` or `core_energy` in LACERA drop tables

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- `production/qa/smoke-lacera-kaijudef.md` — smoke check pass doc

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `KaijuDef` SO class extended with `PartMovementSpec` sub-struct and `BodyMovementSpec` (may need engine-programmer scaffold if not present from Story 002 work)
- Unlocks: Story 007 (LACERA encounter integration)
