# Story 010: VOLTWYRM Encounter Integration (Vertical Pierce, Shield Gates, Phase Transitions)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Blocked
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**BLOCKED**: ADR-0001 is Proposed — enemy-firing runtime awaits ADR-0001 LOCK. Additionally, the L4 Vertical Pierce Corridor performance test (100 HU/s across 4 simultaneous parts) requires BulletSim's multi-hit resolution per pierce event to be implemented and profiled. Data authoring (Stories 008, 009) may proceed. This story cannot be implemented or tested until ADR-0001 advances to Accepted.

**GDD**: `design/gdd/kaiju/03-voltwyrm.md` §5–§10
**Requirement**: `TR-kaiju-008` (one-way phase transitions), `TR-kaiju-002` (dual-shield ARMORED gate), `TR-kaiju-009` (L4 pierce expressiveness at runtime)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: Bullet Engine Backend (primary, **Proposed**) + ADR-0002: Event Bus + ADR-0003: ScriptableObjects
**ADR Decision Summary**: VOLTWYRM is the showcase boss for both the L4 Vertical Pierce Corridor (ADR-0001 expressiveness gate 11.3) and Combo C Blast & Saturate (L3+M2). BulletSim must emit `LaserHit` events for each neck segment hit by a single L4 pierce traversal. The dual shield armor gate mirrors CARAPEX's dorsal_cannon gate: ARMOR_INTACT → missile BU=0; L3WaveHit → ARMOR_STRIPPED + stagger 2s; ARMOR_STRIPPED → BU at ×1.5 stagger multiplier. Phase transitions are driven by `KaijuBehaviorController` (Story 001) reading phase conditions from `Voltwyrm.asset` (Story 008).

**Engine**: Unity 6.3 | **Risk**: HIGH (ADR-0001 Proposed; L4 multi-hit pierce requires Burst Job per-projectile multi-segment collision — [需查證 Entities 1.3 raycast/pierce implementation pattern against vertical AABB chain])
**Engine Notes**: DOTS/ECS pierce resolution must emit N `HitEvent` structs per pierce (one per segment hit) via `NativeQueue<HitEvent>`. Bridge must batch-republish all N `LaserHit` events in the same frame synchronously. Verify `NativeQueue` append pattern is Burst-compatible with Unity 1.3 Entities. [需查證 6.3 API before implementing Bridge multi-hit loop]

**Control Manifest Rules (Feature layer)**:
- Required: Each pierce `LaserHit` event carries the specific `part_id` of the segment hit (not a generic "voltwyrm hit"). All 4 neck-seg HitEvents from one L4 pierce publish synchronously in one Bridge update tick. Dual shield gates: L3WaveHit on either shield independently → that shield's ARMOR_STRIPPED. Both shields BROKEN = Pattern B fully stops; Pattern C terminal ring fires once per shield on BROKEN.
- Forbidden: Single `LaserHit` event covering "all segments" (each segment must be a distinct event for KaijuParts to process heat independently). Shared stagger timer between shields (each shield has independent `stagger_timer`). ADR-0001 DOTS types outside BulletSim.
- Guardrail: L4 sync rate test must show ≥ 4× efficiency advantage vs L2 single-segment (the quantitative ADR-0001 §11.3 gate). Phase transitions are one-way irreversible per Story 001 contract.

---

## Acceptance Criteria

*From `design/gdd/kaiju/03-voltwyrm.md` §10.1 through §10.7:*

**L4 Vertical Pierce Corridor — AC 10.1**
- [ ] L4 single pierce: all 4 neck_seg parts receive `LaserHit(heat_delta = l4_h_rate = 25 HU/s)` per pierce traversal — 4 events per pierce, one per segment, in same frame
- [ ] **Quantitative test**: combined heat rate from L4 pierce = `4 × 25 = 100 HU/s` across the vertical corridor; single-target L4 on a non-vertical-layout boss = 25 HU/s; ratio = **4.0× confirmed** (auto-test: `Assets/_Project/Tests/Kaiju/l4_vertical_advantage_voltwyrm_test.cs`)
- [ ] TTB playtest: L4×M2 on VOLTWYRM — 4 neck_segs all BROKEN — TTB ≤ TTB of same loadout on non-vertical Boss × 0.55 (L4 ≥ 45% faster; 5-run average)
- [ ] Player perception test: 5 players, post-session; ≥ 60% spontaneously describe "piercing through multiple sections" or equivalent without prompting

**Dual Shield Armor Gates — AC 10.2**
- [ ] `shield_left` and `shield_right` each behave as independent armor gates
- [ ] Either shield in ARMOR_INTACT: `MissileHit` → BU_fill = 0 (deflect) for that shield specifically
- [ ] L3WaveHit on `shield_left` → `shield_left` ARMOR_STRIPPED + stagger_timer 2.0s + Pattern C ring burst (1-3 rings depending on difficulty)
- [ ] `shield_left` and `shield_right` have independent `stagger_timer` (stripping one does not reset or affect the other)
- [ ] ARMOR_STRIPPED for either shield: M2 swarm missile BU_fill = `break_delta_base × stagger_break_mult` (×1.5)
- [ ] Shield BROKEN → Pattern B stops for that specific shield (other shield Pattern B continues if still alive); Pattern C terminal ring (20 bullets) fires once

**Adjacency Chain Effects — AC 10.3**
- [ ] `neck_seg_4` BROKEN → L2 Tier-3 (if player has it): adjacent alive segments (`neck_seg_3` and `core_node`) each receive `l2_t3_adjacent_heat_pct × H_max = 30 HU`
- [ ] `neck_seg_4` BROKEN → M3 Tier-3: `is_chain_break = true`; 15 BU transmitted to `core_node`
- [ ] If `shield_left` is still ARMOR_INTACT when M3 chain tries to reach `core_node` via shield path: chain BU = 0 (deflect — `is_chain_break = true` but shield armor gate still blocks BU)
- [ ] `core_node` receiving M3 chain BU accumulates normally; `core_node` own `on_part_break` does not trigger second chain layer (recursion guard)

**Phase Transitions (One-Way) — AC 10.5**
- [ ] Any 1 neck_seg BROKEN → Phase 2 immediately active: Pattern B switches to both shields firing simultaneously (not alternating); Pattern C proactive trigger begins (every 4s regardless of hit)
- [ ] All 4 neck_segs BROKEN **OR** both shields BROKEN → Phase 3 immediately active: Pattern D (core direct-fire) enabled for D2+; Pattern A at max arm count; `core_node` stationary
- [ ] Phase transitions are confirmed one-way: advancing to Phase 3 cannot revert even if game state re-evaluated
- [ ] `phase3_speed_mult` from `Voltwyrm_PatternA_SerpentSpiral.asset` applied correctly at D3 (+15%) and D4 (+30%) once Phase 3 activates

**Full-Clear Reward — AC 10.6**
- [ ] 7/7 parts all BROKEN: after `BossCoreBroke` fires, `on_hunt_end(is_all_parts_broken=true)` triggers → `essence_kaiju` × 1 + `shard_completeness_bonus` (5) awarded
- [ ] 6/7 parts BROKEN (any one missed): no `essence_kaiju`, no completion bonus
- [ ] UI displays "N parts remaining" before victory animation if not all broken (pre-screen text; ADVISORY UX check)

**Readability — AC 10.4**
- [ ] D4 Phase 3 static screenshot test: 5 participants correctly distinguish player hitpoint from VOLTWYRM bullets ≥ 80% accuracy
- [ ] Pattern A 4-arm spiral at max density: arm gap allows player movement at max speed within 0.5s (analytical: gap_angle = 360°/4 = 90°; at screen traversal speed, 90° gap = ~X px — verify against player max speed spec)
- [ ] Shield visual (cold purple-blue) not mistaken for enemy bullet: ≥ 90% of 5 participants correctly identify shield as "attackable object", not projectile

**Performance Budget — AC 10.7**
- [ ] 7-part VOLTWYRM at D4 max density (Pattern A 4-arm + Pattern B both shields + Pattern C active): total on-screen bullet count measured in Editor; stays within BulletSim `readability_cap_priority` hardcap
- [ ] 7-object `KaijuParts` state machine update: frame cost within performance budget (confirm with lead-programmer; record measurement in `production/qa/evidence/voltwyrm-perf-evidence.md`)

---

## Implementation Notes

*Derived from ADR-0001/0002/0003 and control-manifest §3:*

**L4 multi-hit pierce**: BulletSim pierce Job must collect all segment AABBs in the vertical corridor before the pierce passes through. On pierce traversal, for each AABB intersected: append `HitEvent(kaijuId="voltwyrm", partId=seg_id, heatDelta=l4HRate)` to `NativeQueue<HitEvent>`. Bridge processes queue in one batch per frame: for each HitEvent, call `IEventBus.Publish(new LaserHit(...))`. All 4 LaserHit events publish synchronously before frame ends. `KaijuParts` processes each independently (heat accumulation per segment). Order within the 4 events does not matter for correctness — each segment is independent.

**Dual-shield independent stagger**: `KaijuParts` maintains a separate `stagger_timer` per ARMORED part instance (not shared). `shield_left.stagger_timer` and `shield_right.stagger_timer` are fields on their respective `PartState` structs. L3WaveHit on one shield does not affect the other's timer.

**Pattern B phase-gate**: In Phase 1, shields fire alternately (one fires, then after interval the other fires — offset by `fireInterval/2`). In Phase 2 (≥1 neck_seg BROKEN), both shields fire simultaneously (same trigger time). This timing change is authored in `KaijuBehaviorController` phase data (read from `Voltwyrm.asset`'s `PhaseCondition`) — not hardcoded in the Pattern B SO. The SO just defines "how many rows at what interval"; the phase controller decides "alternating vs simultaneous".

**Phase 3 `core_node` stationary**: When Phase 3 triggers, `KaijuBehaviorController` sends a `SetBodyMovement(phase=3)` command; the body animation system reads `core_stationary_in_phase3 = true` from `Voltwyrm.asset` and freezes `core_node` position. `core_node` AABB becomes fixed — simplifies player aiming for the finale.

**App wiring for VOLTWYRM**: Follows same pattern as CARAPEX (Story 004) but with 7 parts and dual independent shield state machines. `App` instantiates `KaijuBehaviorController(voltwyrm_def, bus, emitterActivator)` and `KaijuParts` manages 7 part states. No VOLTWYRM-specific controller subclass needed — all configuration is in `Voltwyrm.asset`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 008: `Voltwyrm.asset` KaijuDef data
- Story 009: Pattern SO definitions (all 4 patterns)
- Story 001: KaijuBehaviorController phase framework (shared, implemented once)
- VOLTWYRM material yield → Economy epic
- VOLTWYRM VFX (shield burst glow, neck-seg explosion) → GameFeel epic

---

## QA Test Cases

*Integration story — automated test specs (BLOCKING when unblocked):*

- **AC-1**: L4 pierce heat rate = 100 HU/s across vertical corridor
  - Given: KaijuParts fixture with 4 neck_segs; l4_h_rate = 25 HU/s; L4 pierce fires once per 0.4s
  - When: Simulate 1.0 s of L4 firing (2.5 pierces)
  - Then: Each neck_seg.H_current increases by ≈ 62.5 HU (25 × 2.5); total heat across 4 segs = ≈ 250 HU = 100 HU/s × 4 confirmed
  - Edge cases: Neck_seg_1 BROKEN mid-test — remaining 3 segs continue accumulating; confirm L4 still hits surviving segs through the gap

- **AC-2**: L4 vs non-vertical boss — ≥4× efficiency ratio
  - Given: Fixture for non-vertical Boss (e.g., CARAPEX with parts side-by-side); same L4 firing pattern
  - When: Measure heat accumulated across ALL parts of non-vertical boss vs VOLTWYRM over same 1.0s
  - Then: CARAPEX L4 heat ≤ 25 HU/s total (1 part hit per pierce); VOLTWYRM = 100 HU/s; ratio ≥ 4.0×
  - Edge cases: If CARAPEX L4 hits 2 parts by chance (vertical alignment), note but still confirm VOLTWYRM advantage is ≥4×

- **AC-3**: Dual-shield independent armor gates
  - Given: Both shields in ARMOR_INTACT; each with B_current = 0
  - When: L3WaveHit published for `shield_left` only
  - Then: `shield_left.armor_state = ARMOR_STRIPPED`; `shield_left.stagger_timer = 2.0s`; `shield_right.armor_state` remains ARMOR_INTACT; `shield_right.stagger_timer` = 0 (unchanged)
  - Edge cases: Simultaneous L3WaveHit on both shields in same frame — both should independently transition; no shared timer interference

- **AC-4**: Phase 3 triggers on all-segs-broken OR both-shields-broken (OR gate)
  - Given: Phase 1 active; spy on `KaijuBehaviorController.CurrentPhase`
  - When: Simulate `shield_left` BROKEN + `shield_right` BROKEN (both shields, no neck segs broken)
  - Then: `CurrentPhase == 3` immediately; Pattern D (core direct-fire) enabled; Pattern B fully stopped (both shields broken)
  - When (separate test run): Simulate all 4 neck_segs BROKEN (shields still alive)
  - Then: `CurrentPhase == 3`; Pattern B continues from alive shields; Pattern A at max arm count
  - Edge cases: Phase 3 cannot revert even if hypothetically a part un-broke (guard test)

- **AC-5**: Full-clear 7/7 essence_kaiju trigger
  - Given: Economy + Stage fixture; all 7 parts broken; `BossCoreBroke` fires
  - When: `on_hunt_end(is_all_parts_broken=true)` received by Economy
  - Then: Player inventory receives `essence_kaiju += 1`; `shard_common += 5` (completeness bonus)
  - When (6/7 variant): 6 parts broken; `BossCoreBroke` fires
  - Then: No `essence_kaiju` awarded; `shard_completeness_bonus` = 0

- **AC-6**: Pattern C terminal ring on shield BROKEN
  - Given: `shield_left` in ARMOR_STRIPPED, B_current = 145; spy on bullet emission events
  - When: `MissileHit` pushes B_current to 150 → `shield_left` BROKEN
  - Then: `PartBroke(shield_left)` fires; terminal ring burst (20 bullets) emitted from shield_left position; Pattern B for shield_left permanently ceases
  - Edge cases: If `shield_right` also alive, `shield_right.PatternB` must continue firing after `shield_left` BROKEN

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Kaiju/voltwyrm_encounter_integration_test.cs` — must exist and pass (includes `l4_vertical_advantage_voltwyrm_test` as a nested test class or separate file)
- `production/qa/evidence/voltwyrm-readability-evidence.md` — D4 Phase 3 screenshot test
- `production/qa/evidence/voltwyrm-l4-perception-evidence.md` — 5-player post-session survey
- `production/qa/evidence/voltwyrm-perf-evidence.md` — 7-part performance budget measurement + lead-programmer sign-off

**Status**: [ ] Not yet created — blocked until ADR-0001 LOCK

---

## Dependencies

- Depends on: Story 001 (KaijuBehaviorController), Story 008 (Voltwyrm.asset), Story 009 (pattern SOs), ADR-0001 LOCK
- Unlocks: VOLTWYRM fully playable; TR-kaiju-008 and TR-kaiju-009 close here; weapon-system.md open question #2 (L4 vertical pierce) resolved
