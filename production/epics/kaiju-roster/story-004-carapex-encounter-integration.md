# Story 004: CARAPEX Encounter Integration (Armor Gate, Phase Firing, Tutorial Loop)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Blocked
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**BLOCKED**: ADR-0001 is Proposed — enemy-firing runtime awaits ADR-0001 LOCK (1,000 bullets @60fps on mobile, 0 GC/frame verified). Data authoring (Stories 002, 003) may proceed. This story cannot be implemented or tested until ADR-0001 advances to Accepted.

**GDD**: `design/gdd/kaiju/01-carapex.md` §5–§10
**Requirement**: `TR-kaiju-001` (tutorial loop), `TR-kaiju-002` (ARMORED gate), `TR-kaiju-003` (BOSS_CORE victory)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: Bullet Engine Backend (primary, **Proposed**) + ADR-0003: ScriptableObjects + ADR-0002: Event Bus
**ADR Decision Summary**: BulletSim (DOTS/ECS + Burst + Jobs) executes `EmitterPatternSO` Blobs and returns `HitEvent` structs via `NativeQueue` to the Mono Bridge, which republishes as `LaserHit` / `MissileHit` / `L3WaveHit` bus events. `KaijuParts` consumes these, applies armor gate logic (ARMORED BU=0 when ARMOR_INTACT), and publishes `PartBroke` / `BossCoreBroke`. `KaijuBehaviorController` (Story 001) drives phase transitions. This integration story wires all systems for CARAPEX specifically.

**Engine**: Unity 6.3 | **Risk**: HIGH (ADR-0001 Proposed; Entities 1.3+ post-cutoff API — verify against `docs/engine-reference/unity/VERSION.md` before implementing)
**Engine Notes**: DOTS↔Mono Bridge is the sole cross-boundary point. Verify `Time.timeScale` integration for hitch freeze (§BulletSim rules). All DOTS Entity types must stay inside `BulletSim` assembly. [需查證 6.3 Entities API before any Blob authoring call]

**Control Manifest Rules (Feature layer)**:
- Required: `KaijuParts` publishes `PartBroke` with full payload (part_id, kaiju_id, part_type, world_position, drop_table_id, break_quality, adjacency_list, is_chain_break). `KaijuBehaviorController` reads phase changes from `KaijuDef` knobs. Armor gate: `L3WaveHit` on dorsal_cannon → ARMOR_STRIPPED; missile hit on ARMOR_INTACT → BU+0 + deflect VFX.
- Forbidden: Hardcoding phase condition logic in CARAPEX-specific code — phase conditions must come from `Carapex.asset` (Story 002). DOTS Entity refs leaking past BulletSim Bridge. Direct `KaijuParts` → `BulletSim` assembly reference.
- Guardrail: Full event chain (LaserHit → PartBroke → Economy + GameFeel + UI) must complete synchronously in one frame. Armor-gate BU=0 cannot be worked around by any weapon except L3 Wave.

---

## Acceptance Criteria

*From `design/gdd/kaiju/01-carapex.md` §10 AC-01 through AC-09:*

**ARMORED Gate (dorsal_cannon) — AC-02**
- [ ] `dorsal_cannon` in ARMOR_INTACT: any `MissileHit` event → `KaijuParts` applies BU_fill = 0; deflect animation VFX triggered
- [ ] `L3WaveHit` on `dorsal_cannon` → `armor_state = ARMOR_STRIPPED`; `stagger_timer = 2.0 s` (from `PartSystemConfig`)
- [ ] ARMOR_STRIPPED visual (cracked shell + pulsing weak-point highlight + HUD 2s countdown bar) appears within 0.3 s of `L3WaveHit` event (`stagger_visual_onset_max_s = 0.3`)
- [ ] During ARMOR_STRIPPED: `MissileHit` → BU_fill = `break_delta_base × stagger_break_mult` (×1.5 from `PartSystemConfig`)
- [ ] `stagger_timer` reaches 0 → `armor_state = ARMOR_INTACT`; `B_current` for dorsal_cannon preserved (not reset) — BU rollover across windows confirmed

**BOSS_CORE Victory (chest_reactor_core) — AC-03**
- [ ] `chest_reactor_core` reaches B_current ≥ 200 BU → `PartBroke` fired → `BossCoreBroke` fired immediately after in same frame
- [ ] Victory works with all optional parts (mandibles + dorsal_cannon) still ALIVE
- [ ] `PartBroke` precedes `BossCoreBroke` in event dispatch order (verified by integration test spy)

**Phase Transitions — AC-07**
- [ ] Any mandible BROKEN → Pattern B fire interval scaled by `carapex_phase2_dorsal_speed_mult` (1.15 default) read from `Carapex.asset`
- [ ] Both mandibles BROKEN → Pattern C switches to 4-way cross (D1/D2) or 8-way (D3/D4) — variant index from `Carapex_PatternC_CorePulse.asset` phase variant array
- [ ] `dorsal_cannon` BROKEN → Pattern B permanently ceases (emitter deactivated; no re-activation)
- [ ] Phase transitions verified one-way: after Phase 3 activates, no return to earlier phases

**Difficulty Density Scaling — AC-08**
- [ ] Bullet speeds for A/B/C (120/100/90 px/s) are invariant across D1–D4 (confirmed by reading SO speed fields, not by runtime measurement)
- [ ] Bullet counts and fire intervals for each pattern match GDD §8 tables at D1 through D4
- [ ] `dorsal_cannon` H_max / B_max / `stagger_duration` values read at runtime = values in `PartSystemConfig` (difficulty invariant — `difficulty_invariance_test` coverage)

**L2×M3 TTB (Loadout Verification) — AC-09**
- [ ] NORMAL mandible (100 BU, D1, L2+M3 Tier 0): actual TTB ∈ [15 s, 25 s] (weapon-system.md D.4 target window)
- [ ] This test case covered by `Assets/_Project/Tests/Kaiju/weapon_loadout_matrix_test.cs` (or shared weapon test — coordinate with gameplay-programmer)

**Tutorial Loop (Playtest — AC-01)**
- [ ] 5 new-player user test (no prior exposure): first mandible break within 3 min recorded (timestamp log)
- [ ] Player self-discovers L3 strip mechanic within 5 min of first encounter with dorsal_cannon
- [ ] Post-session survey: ≥70% describe "heat then detonate" without prompting

**Bullet Readability (Playtest — AC-05)**
- [ ] Screenshot test: 5 participants correctly identify enemy bullets vs safe gaps ≥80% accuracy across Patterns A/B/C screenshots
- [ ] SOFTENED mandible orange-red pulse visible under D4 max density (bullet occlusion ≤50% of pulse time)
- [ ] ARMOR_STRIPPED countdown bar not fully occluded by bullets at D4

---

## Implementation Notes

*Derived from ADR-0001 §Hybrid Architecture + ADR-0002 §Event Chain + control-manifest §3:*

Wire order (App composition root, `App` assembly only):
1. `BulletSim` bridge translates `HitEvent` structs to `LaserHit` / `MissileHit` / `L3WaveHit` bus events.
2. `KaijuParts` (the `IPartStateQuery` implementor) subscribes to hit events for `kaiju_id = "carapex"`. On `L3WaveHit` for `dorsal_cannon`: set `armor_state = ARMOR_STRIPPED`, start stagger timer, publish `PartStaggered`. On `MissileHit` for ARMOR_INTACT part: apply BU_fill = 0, trigger deflect VFX command.
3. `KaijuBehaviorController` (Story 001, constructed with `Carapex.asset`) subscribes to `PartBroke` and drives phase transitions. It calls `IEmitterActivator.SetActivePhase(phase)` → BulletSim activates the corresponding SO blob set.
4. `GameFeel` (S8) subscribes to `PartBroke` for stagger visual / VFX (armor strip crack, countdown HUD bar). MUST NOT duplicate GameFeel logic here.
5. `Economy` (S3) subscribes to `PartBroke` and independently computes material yield from `break_quality`.

CARAPEX-specific: Pattern B emitter must be wired to check `armor_state` of `dorsal_cannon` each fire cycle. Use `IPartStateQuery.GetArmorState(kaiju_id, part_id)` to gate Pattern B emission (returns ARMOR_INTACT → skip fire; ARMOR_STRIPPED → pause for stagger window duration).

Pattern C emitter: `KaijuBehaviorController` passes current phase index to `IEmitterActivator`; BulletSim selects `PhaseVariant[phaseIndex]` from the baked blob at runtime.

`carapex_phase2_dorsal_speed_mult` is read from `Carapex.asset` by `KaijuBehaviorController` when broadcasting Phase 2 activation. The emitter activator passes the multiplier to BulletSim as a `float speedMult` in the phase-change command struct (value type only across boundary).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: `Carapex.asset` KaijuDef authoring
- Story 003: `EmitterPatternSO` asset authoring (Pattern A/B/C)
- Story 001: KaijuBehaviorController phase state machine class
- Material yield calculation → Economy system (S3, separate epic)
- VFX particle system implementation → GameFeel system (S8, separate story)

---

## QA Test Cases

*Integration story — automated test specs (BLOCKING when unblocked):*

- **AC-1**: ARMORED BU gate
  - Given: CARAPEX KaijuParts fixture; `dorsal_cannon` in ARMOR_INTACT; `PartSystemConfig` fixture with stagger_break_mult=1.5
  - When: `MissileHit(part_id="dorsal_cannon", break_delta_base=10)` published
  - Then: `dorsal_cannon.B_current` unchanged (BU_fill=0); deflect event published
  - Edge cases: Multiple missiles in same frame all yield BU=0; L3WaveHit correctly transitions to ARMOR_STRIPPED

- **AC-2**: ARMOR_STRIPPED BU fills at ×1.5
  - Given: `dorsal_cannon` in ARMOR_STRIPPED (stagger_timer active)
  - When: `MissileHit(break_delta_base=10)` published
  - Then: `dorsal_cannon.B_current` increases by 15 (10 × 1.5)
  - Edge cases: stagger_timer expires mid-combat → armor returns to INTACT → next missile BU=0

- **AC-3**: BU persists across stagger windows
  - Given: `dorsal_cannon` ARMOR_STRIPPED, B_current=80; stagger_timer expires
  - When: armor_state returns to ARMOR_INTACT
  - Then: B_current = 80 (unchanged); next ARMOR_STRIPPED window starts from 80
  - Edge cases: B_current must not reset to 0 on stagger-end event

- **AC-4**: BOSS_CORE event order
  - Given: event bus spy recording all events
  - When: `chest_reactor_core` B_current reaches 200 BU (threshold hit)
  - Then: spy log contains `PartBroke(chest_reactor_core)` at index N; `BossCoreBroke` at index N+1; no gap event between them
  - Edge cases: victory must fire even if mandibles/dorsal all ALIVE

- **AC-5**: Phase 2 speed multiplier applied
  - Given: `KaijuBehaviorController` using `Carapex.asset` (phase2SpeedMult=1.15)
  - When: `left_mandible` BROKEN → Phase 2 activates
  - Then: `IEmitterActivator.SetActivePhase(2)` called with speedMult=1.15f
  - Edge cases: multiplier must be read from SO asset, not a hardcoded literal

- **AC-6**: TTB integration (L2×M3 NORMAL mandible, D1, Tier 0)
  - Given: weapon simulation fixture; `left_mandible` (100 BU); L2 H_rate=37.5 HU/s; M3 break_delta_base=10, state_mult SOFTENED=6×
  - When: L2 fires for ~5-6s to SOFTENED, then 2 M3 missiles fired
  - Then: `left_mandible` BROKEN at T ∈ [15s, 25s]
  - Edge cases: include realistic dodge downtime (≥30% uptime loss) in fixture parameters

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Kaiju/carapex_encounter_integration_test.cs` — must exist and pass
- `production/qa/evidence/carapex-tutorial-loop-evidence.md` — 5-person playtest session doc + sign-off
- `production/qa/evidence/carapex-readability-evidence.md` — screenshot test results

**Status**: [ ] Not yet created — blocked until ADR-0001 LOCK

---

## Dependencies

- Depends on: Story 001 (KaijuBehaviorController), Story 002 (Carapex.asset), Story 003 (pattern SOs), ADR-0001 LOCK (BulletSim runtime)
- Unlocks: Sprint acceptance of CARAPEX boss as MVP-complete (TR-kaiju-001 closes here)
