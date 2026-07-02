# Story 007: LACERA Encounter Integration (Sweeping Limbs, Dynamic World Position, L4 Window)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Blocked
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**BLOCKED**: ADR-0001 is Proposed — enemy-firing runtime awaits ADR-0001 LOCK (1,000 bullets @60fps, 0 GC/frame verified). Dynamic `world_position` integration with `IPartStateQuery` also requires LACERA's runtime movement system to be wired into `KaijuParts`, which itself depends on the BulletSim Bridge architecture being settled. Data authoring (Stories 005, 006) may proceed. This story cannot be implemented or tested until ADR-0001 advances to Accepted.

**GDD**: `design/gdd/kaiju/02-lacera.md` §4 (Moving Parts), §6 (Phases), §10 (Acceptance Criteria)
**Requirement**: `TR-kaiju-006` (L4 vertical window), `TR-kaiju-007` (dynamic world_position), `TR-kaiju-005` (readability at max density)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: Bullet Engine Backend (primary, **Proposed**) + ADR-0002: Event Bus + ADR-0003: ScriptableObjects
**ADR Decision Summary**: BulletSim resolves each limb emitter's world_position at fire-time via `IPartStateQuery.GetWorldPosition(kaiju_id, part_id)` (ADR-0002 query interface). `KaijuParts` owns the position update loop, computing each moving part's screen-space position each frame from `PartMovementSpec` (arc, speed, phase) in `Lacera.asset`. `on_part_break` payload.world_position = position at break frame. M1 tracking missile reads live position per frame. BulletSim pattern triggers (Pattern A PassCenter) fire when sweep state passes center — monitored by BulletSim per the sweep spec from `Lacera.asset`.

**Engine**: Unity 6.3 | **Risk**: HIGH (ADR-0001 Proposed; IPartStateQuery's per-frame position query from BulletSim side — must not cross DOTS boundary with managed refs [需查證 Burst-compatible query pattern])
**Engine Notes**: Per-frame world_position computation in `KaijuParts` must use unscaled time (`Time.unscaledDeltaTime`) or scaled time depending on hitch behavior — verify with engine-programmer. Position must be passed to BulletSim Bridge as value-type (Vector2 struct, not managed reference).

**Control Manifest Rules (Feature layer)**:
- Required: `KaijuParts` implements `IPartStateQuery.GetWorldPosition(kaijuId, partId)` returning current-frame position for each alive moving part. M1 homing missile queries this interface every frame (ADR-0002 §4.3). `on_part_break` payload carries break-frame world_position (not spawn position). Moving part positions computed from `PartMovementSpec` data in SO.
- Forbidden: Caching world_position as a stale value on KaijuParts and not updating per-frame. Passing managed `Transform` references across DOTS↔Mono boundary. Any limb position hardcoded outside SO.
- Guardrail: Pattern B deactivates in Phase 2 (via `phase_restriction = Phase1Only` flag in SO + `KaijuBehaviorController`). Limb speed ×1.5 in Phase 2 (Berserker Whirl): the multiplier comes from `Lacera_PatternC_BerserkerWhirl.asset`, not hardcoded.

---

## Acceptance Criteria

*From `design/gdd/kaiju/02-lacera.md` §10.1 through §10.6:*

**Moving Part System Correctness — AC 10.1**
- [ ] All 4 NORMAL limb `world_position` values update each frame per `sweep_arc` spec (pivot_bone, arc_half_deg, speed_deg_per_s, phase_rad from `Lacera.asset`)
- [ ] Left fore-limb (phase=0) and right fore-limb (phase=π) are anti-phase: when left is at maximum positive angle, right is at maximum negative angle (verified by position sampling at T=period/2)
- [ ] `IPartStateQuery.GetWorldPosition("lacera", "fore_limb_left")` returns different values at T=0 and T=0.75s (confirms movement; sweep period for fore limbs ≈ 2.67s)
- [ ] `tail_carapace` world_position oscillates with ±30° from pivot_bone, period ≈ 3.0s (20°/s × 2 × 30° = 2 × 1.5s = 3s)
- [ ] `head_core` world_position follows body_movement vertical_drift (±5% screen height, 0.2 Hz); `head_core` itself has zero independent movement
- [ ] `on_part_break` payload for any NORMAL limb: `world_position` = dynamically computed position at break frame (material drops appear at the correct dynamic location, not at spawn origin)

**Weapon Differentiation — AC 10.2 (M1 vs M3 hit rate)**
- [ ] M1 tracking missile (±60° tracking cone): hit rate against fore_limb_left ≥ 80% across 10 test shots at T1 difficulty
- [ ] M3 homing missile (no tracking, straight): hit rate against fore_limb_left < 50% across 10 test shots at T1 difficulty (confirms moving parts create meaningful hit difficulty for non-tracking weapons)
- [ ] Combo B (L2 × M1): full combat TTB (4 limbs + head_core) ∈ [60 s, 90 s] (T1 difficulty; averaged over 5 runs)

**L4 Vertical Alignment Window — AC 10.6**
- [ ] During one complete Phase 1 combat (all limbs alive), the Y-axis difference between `head_core.world_position.y` and any `hind_limb.world_position.y` falls within ≤ 20 px (alignment window) at least **8 times**
- [ ] The alignment window duration is ≥ 0.3 s per occurrence (sufficient for L4 beam to fire; L4 fire interval = 0.4s so partial window still allows a shot)
- [ ] Level designer sign-off recorded in Boss layout review doc (`production/qa/evidence/lacera-l4-window-review.md`)

**Phase Transitions — GDD §6**
- [ ] Phase 1 → Phase 2: ≥2 NORMAL limbs BROKEN → Pattern C activates (Berserker Whirl); Pattern B deactivates; residual limb speed ×1.5
- [ ] Phase 2 → Phase 3: ≥3 NORMAL limbs BROKEN → most emitters quiet; Phase 3 condition leaves 0–1 active nodes; `head_core` more exposed
- [ ] `tail_carapace` (ARMORED): L3 → ARMOR_STRIPPED + stagger 2.0 s; M3 Tier-3 chain from tail BROKEN → head_core BU + 15 (if `is_chain_break=false`)

**Readability — AC 10.4**
- [ ] All LACERA bullets use warm colors (#FF8C00 / #FF4500) across all patterns and difficulties
- [ ] T4 max density: 5-person screenshot test shows bullet vs player-hitpoint discrimination ≥ 80%
- [ ] Pattern B charge-up 0.5 s flash recognized as "attack warning" by ≥ 80% of screenshot test participants

---

## Implementation Notes

*Derived from ADR-0001/0002/0003 and control-manifest §3 Weapons / KaijuParts:*

**KaijuParts per-frame position update loop** (owned by KaijuParts, not BulletSim):
```csharp
// Inside KaijuParts Update() — runs on main thread, Mono side
foreach (var part in _activeParts) {
    if (part.MovementSpec.movementType == SweepArc) {
        float angle = part.MovementSpec.arcHalfDeg
                      * Mathf.Sin(2 * Mathf.PI * _time * part.FrequencyHz + part.MovementSpec.phaseRad);
        part.WorldPosition = PivotWorldPosition(part.pivotBoneName) + ArcToOffset(angle);
    }
    // stationary_relative: position = body_drift_offset + anchor
    // oscillate: same formula as sweep_arc but different semantic label
}
_time += Time.deltaTime;  // or unscaledDeltaTime — coordinate with engine-programmer
```

`IPartStateQuery.GetWorldPosition` returns this cached per-frame value (safe to call from BulletSim Bridge synchronously on main thread during Bridge's update tick — verify Bridge architecture with engine-programmer).

**M1 tracking integration**: M1 missile (`Weapons` system) calls `IPartStateQuery.GetWorldPosition` each frame to steer toward target. This is already required by control-manifest §3 Weapons: "追蹤飛彈 / Tier-3 觸發經 IPartStateQuery 讀部位 world_position / heat_state". This story validates that LACERA's moving parts satisfy that requirement.

**Pattern A PassCenter trigger**: BulletSim (or Bridge) checks each frame whether a limb's computed angle has crossed zero (center position) since last frame. This crossing detection is: `sign(angle_prev) != sign(angle_curr)`. On crossing, BulletSim fires Pattern A burst at current world_position. The emitter source is the limb; all limbs run independently.

**L4 window measurement**: To verify AC 10.6, implement a debug logging fixture (Editor-only) that records `head_core.y` and each `hind_limb.y` at 60fps for 60s and counts windows where |Δy| ≤ 20px lasting ≥ 0.3s. This is a test-mode only feature; not shipped in release build.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 005: `Lacera.asset` KaijuDef data
- Story 006: Pattern SO asset definitions
- Story 001: KaijuBehaviorController phase state machine
- LACERA material yield calculation → Economy epic
- LACERA VFX (limb explosion stub, asymmetric tilt) → GameFeel epic

---

## QA Test Cases

*Integration story — automated test specs (BLOCKING when unblocked):*

- **AC-1**: Per-frame world_position update correctness
  - Given: `KaijuParts` fixture with `Lacera.asset`; `fore_limb_left` (arc=60°, speed=45°/s, phase=0)
  - When: Simulate 1.333 s (one quarter-period) of Update() calls at fixed dt
  - Then: `IPartStateQuery.GetWorldPosition("lacera", "fore_limb_left")` at T=0.667s ≈ peak position (angle≈+60°); at T=1.333s ≈ center (angle≈0°) — within ±2px tolerance
  - Edge cases: Negative arc values (limb moving backward through center) must also trigger PassCenter; `speed_deg_per_s=0` must yield stationary position

- **AC-2**: Anti-phase between fore-limb-left and fore-limb-right
  - Given: Both fore limbs initialized; fore_left phase=0, fore_right phase=π
  - When: Query both positions at T=period/2 (≈1.333s)
  - Then: `fore_limb_left.angle ≈ +60°`; `fore_limb_right.angle ≈ −60°` (or equivalent mirror position)
  - Edge cases: At T=0, both limbs should be at center (sin(0)=0 and sin(π)=0) — verify this is the intended initial state

- **AC-3**: on_part_break carries dynamic world_position
  - Given: `fore_limb_left` at computed angle ≈ +45° (off-center); event spy on bus
  - When: Break event triggered (B_current threshold reached)
  - Then: `PartBroke.world_position` == dynamically computed position at +45°, NOT the limb's spawn/anchor position
  - Edge cases: If break occurs at center (angle=0), world_position should still be the computed center position (not zero vector)

- **AC-4**: M1 hit rate ≥ 80% vs M3 < 50% (statistically validated)
  - Given: Weapon simulation fixture with fore_limb_left moving at 45°/s; M1 (tracking, ±60°) vs M3 (straight, Mach-speed ≈120 px/s) — 10 shots each from standard player position
  - When: 10 M1 shots fired; 10 M3 shots fired
  - Then: M1 hit count ≥ 8 of 10; M3 hit count < 5 of 10
  - Edge cases: If limb is in center position (momentarily stationary), both weapons should hit; this inflates M3 rate — run test with limb at offset angles

- **AC-5**: L4 vertical alignment window count (Phase 1)
  - Given: Debug fixture simulating head_core vertical_drift + hind_limb sweep at all phase offsets for 60 s at 60 fps
  - When: Count frames where |head_core.y − hind_limb_left.y| ≤ 20px OR |head_core.y − hind_limb_right.y| ≤ 20px and window lasts ≥ 18 frames (0.3s at 60fps)
  - Then: Window count ≥ 8
  - Edge cases: Window must be contiguous (not two separate 0.15s windows counted as one); test must run for at least one full hind_limb period (≈6s) — 60s coverage ensures statistical confidence

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Kaiju/lacera_encounter_integration_test.cs` — must exist and pass
- `production/qa/evidence/lacera-l4-window-review.md` — level designer review sign-off
- `production/qa/evidence/lacera-readability-evidence.md` — T4 screenshot test results

**Status**: [ ] Not yet created — blocked until ADR-0001 LOCK

---

## Dependencies

- Depends on: Story 001 (KaijuBehaviorController), Story 005 (Lacera.asset), Story 006 (pattern SOs), ADR-0001 LOCK
- Unlocks: LACERA boss fully playable; TR-kaiju-006 and TR-kaiju-007 close here
