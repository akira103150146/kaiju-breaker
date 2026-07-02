# Story 006: LACERA EmitterPatternSO Definitions (Patterns A / B / C)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju/02-lacera.md` §5 (Attack Patterns), §8 (Difficulty Scaling)
**Requirement**: `TR-kaiju-009` (EmitterPatternSO expressiveness), `TR-kaiju-005` (bullet readability)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: ScriptableObjects (primary) + ADR-0001: Bullet Engine Backend (expressiveness reference — Proposed)
**ADR Decision Summary**: LACERA's three attack patterns are authored as `EmitterPatternSO` assets (ADR-0003, Accepted). Key difference from CARAPEX: Pattern A is limb-local — each limb emitter fires independently when passing center angle, using the limb's dynamic `world_position` as origin. The `emitter_source` field references a part_id; BulletSim uses `IPartStateQuery.GetWorldPosition(part_id)` to resolve the emission origin at fire time. Data authoring only; runtime integration is Story 007.

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: No DOTS in this story. `EmitterPatternSO` is a standard SO. Post-cutoff API risk is zero for data authoring.

**Control Manifest Rules (Feature layer — Content tier)**:
- Required: Pattern SOs at `Assets/_Project/Content/Kaiju/LACERA/Patterns/`; `emitter_source` fields reference valid LACERA part IDs; warm bullet colors (orange-yellow #FF8C00 / orange-red #FF4500); per-difficulty bullet-count arrays length=4; Pattern B charge-up telegraph spec present.
- Forbidden: Cool-color bullets; emitter logic in SO (purely declarative); new EmitterShape types.
- Guardrail: Pattern B (`emitter_trigger = Convergence`) uses an SO-level flag marking it as Phase 1 only; Phase 2 deactivation is handled by `KaijuBehaviorController` reading the flag, not by hardcoded index in controller C#.

---

## Acceptance Criteria

*From `design/gdd/kaiju/02-lacera.md` §5 Attack Patterns and §8 Difficulty Scaling:*

**Pattern A — Blade Wave Barrage (`Lacera_PatternA_BladeWaveBarrage.asset`)**
- [ ] `emitter_source`: references each limb part_id independently (one SO, parameterized via `emitter_source = "{limb_part_id}"` or one SO per limb — choose whichever matches `EmitterPatternSO` schema; document choice in SO `designNote` field)
- [ ] Bullet counts per limb per shot: D1=3, D2=4, D3=5, D4=6
- [ ] Bullet fan angle: 60° (fixed across all difficulties)
- [ ] Trigger condition: `trigger_type = PassCenter` (fires when limb arc passes center angle; runtime detects this from sweep state)
- [ ] Bullet speed: defined in SO; value to match design intent (~120 px/s or as set by gameplay-programmer in weapon-system baseline — coordinate with existing `WeaponBalanceConfig` bullet speed context)
- [ ] Bullet color: `#FF8C00` (orange-yellow); pixel border present
- [ ] Per-difficulty fire-count array length = 4; no zero entries
- [ ] OnValidate: color warm-hue check passes; fan angle ∈ [30°, 90°]

**Pattern B — Convergence Burst (`Lacera_PatternB_ConvergenceBurst.asset`)**
- [ ] `trigger_type = ConvergenceBurst` (simultaneous fire from all surviving limbs; limbs move to center position 0.5 s before firing)
- [ ] `phase_restriction = Phase1Only` flag set to true (Story 001 reads this flag; Pattern B deactivated on Phase 2 transition)
- [ ] Trigger interval: random ∈ [12, 18] s (authored as `min_interval = 12, max_interval = 18` — deterministic seed for testing)
- [ ] Bullet counts per limb per burst: D1=4, D2=5, D3=6, D4=8
- [ ] Fan spread type: `convergent_fan_90` (toward player, 90° arc) for D1/D2; D3/D4 also include 1–2 supplemental side bullets (authored as `side_bullet_count`)
- [ ] Telegraph spec: `telegraph_duration = 0.5 s`; `telegraph_type = charge_flash`; `telegraph_color = #FF4500` (orange-red at limb tip)
- [ ] Bullet color: `#FF4500` (orange-red, convergence burst variant)
- [ ] OnValidate: phase_restriction field present; interval min < max; telegraph duration > 0

**Pattern C — Berserker Whirl (`Lacera_PatternC_BerserkerWhirl.asset`)**
- [ ] `trigger_type = PassCenter` (same as Pattern A, fires as limb sweeps through center)
- [ ] `phase_restriction = Phase2Plus` flag (active from Phase 2 onward; replaces Pattern A's calm rhythm)
- [ ] Speed multiplier flag: `limb_speed_mult = 1.5` (the SO carries this multiplier; runtime applies it to limb sweep speed when this pattern is active — BulletSim Bridge reads SO field)
- [ ] Shots per arc: D1=1, D2=1, D3=2, D4=2 (shots per center crossing per limb)
- [ ] Bullet counts per limb per shot: D1=3, D2=4, D3=4, D4=5
- [ ] Bullet color: `#FF8C00` (same orange-yellow as Pattern A — visual consistency as Berserker is escalation, not new pattern type)
- [ ] OnValidate: limb_speed_mult ∈ [1.0, 3.0]; shots per arc ≤ 3

**Cross-pattern**
- [ ] No new `EmitterShape` types required — Pattern A/B/C all expressible with Fan and Aimed shapes plus per-limb parameterization
- [ ] All 3 SOs pass OnValidate with zero Inspector errors
- [ ] Design notes field on each SO references the LACERA GDD section and design intent (for future maintainers)

---

## Implementation Notes

*Derived from ADR-0003 SO schema and GDD §5/§8:*

**Per-limb emitter design choice**: Pattern A fires independently from each limb's current position. Two authoring options:
- Option A: One `EmitterPatternSO` with `emitter_source = ""` (empty → fires from all limbs), BulletSim iterates over alive limbs. Simpler, but less explicit.
- Option B: One SO per limb part_id (4 SOs). More explicit but 4× asset count.

Recommend Option A for maintainability (one SO to tune). BulletSim interprets empty `emitter_source` on a LACERA pattern as "all alive NORMAL limbs." Document this convention in the SO's `designNote` field and in BulletSim's pattern interpreter comment.

**Pattern B center-convergence**: The 0.5s limb convergence animation (limbs move to center position before firing) is a position override, not an `EmitterPatternSO` concern. The SO carries `converge_to_center_duration = 0.5f` as a float field that BulletSim reads to temporarily override limb sweep position during the animation. This is pure data.

**Pattern C speed multiplier**: The `limb_speed_mult = 1.5f` field is read by `KaijuBehaviorController` when activating Phase 2; it passes the multiplier to `IEmitterActivator.SetActivePhase(2, speedMult: 1.5f)`. BulletSim then applies the multiplier to the sweep angular speed for Pattern C's limbs. The field lives in the SO (not hardcoded in the controller).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 005: LACERA KaijuDef SO (part data, adjacency, movement spec)
- Story 007: Runtime per-frame limb position update; M1 hit-rate validation; L4 vertical window; encounter phase firing
- Story 001: KaijuBehaviorController activation logic

---

## QA Test Cases

*Config/Data story — smoke check (advisory):*

- **AC-1**: Bullet colors warm for all 3 patterns
  - Setup: Open all 3 LACERA pattern SOs in Inspector
  - Verify: Pattern A = #FF8C00 (hue ~27°); Pattern B = #FF4500 (hue ~14°); Pattern C = #FF8C00 — all warm-orange spectrum
  - Pass condition: OnValidate color check passes; no blue/green/purple entries

- **AC-2**: Per-difficulty bullet counts match GDD §8 table
  - Setup: Inspect `bulletCountPerTier[4]` array on each SO
  - Verify: Pattern A = [3,4,5,6]; Pattern B = [4,5,6,8]; Pattern C shots per arc = [1,1,2,2]
  - Pass condition: All arrays length=4; values match GDD table exactly

- **AC-3**: Phase restriction flags set
  - Setup: Inspect Pattern B's `phase_restriction` field
  - Verify: `phase_restriction = Phase1Only` present and true
  - Setup: Inspect Pattern C's `phase_restriction` field
  - Verify: `phase_restriction = Phase2Plus` present and true
  - Pass condition: Both flags set; no phase restriction on Pattern A (active all phases)

- **AC-4**: Expressiveness — no new EmitterShape types
  - Setup: Review `EmitterShape` enum values used across all 3 LACERA pattern SOs
  - Verify: Only existing shape values used (Fan, Aimed, etc.); no enum value that doesn't exist in the base `EmitterPatternSO` schema
  - Pass condition: Zero new EmitterShape enum additions required for LACERA patterns

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- `production/qa/smoke-lacera-patterns.md` — smoke check pass doc

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `EmitterPatternSO` schema includes `phase_restriction`, `limb_speed_mult`, `converge_to_center_duration` fields (schema extension may require engine-programmer coordination)
- Unlocks: Story 007 (LACERA encounter integration uses these SOs)
