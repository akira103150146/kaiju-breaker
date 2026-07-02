# Story 001: Boss Phase Controller Framework (Shared)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju/01-carapex.md` §6, `design/gdd/kaiju/03-voltwyrm.md` §10.5
**Requirement**: `TR-kaiju-003`, `TR-kaiju-008`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Event Bus (primary) + ADR-0003: ScriptableObjects (secondary)
**ADR Decision Summary**: All cross-system communication travels via `IEventBus` typed readonly-struct events (ADR-0002, Accepted). KaijuDef SO defines phase-transition conditions per boss (ADR-0003, Accepted). The phase controller is a pure C# state machine — it subscribes to `on_part_break`, reads break state against `KaijuDef` phase conditions, and publishes `on_boss_core_break` after `on_part_break` for BOSS_CORE. No BulletSim coupling here.

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: Pure C# state machine, no Entities/Burst. Unity Test Framework (NUnit, EditMode). No post-cutoff API risk.

**Control Manifest Rules (Feature layer)**:
- Required: Subscribe via `IEventBus.Subscribe<PartBroke>` — MUST NOT hold a MonoBehaviour reference to `KaijuParts`. Phase conditions read from `KaijuDef` SO injected at construction. All public methods unit-testable with fake event bus + fake SO.
- Forbidden: Direct reference to `KaijuParts` assembly; DOTS types; static singletons holding phase state.
- Guardrail: `on_part_break` → `on_boss_core_break` event order must be guaranteed in the same frame. Phase transitions are one-way (no rollback). `is_chain_break` flag must not trigger additional phase re-evaluation recursively.

---

## Acceptance Criteria

*From `design/gdd/kaiju/01-carapex.md` §10 AC-03 and §10 AC-07; `design/gdd/kaiju/03-voltwyrm.md` §10.5:*

- [ ] `KaijuBehaviorController` subscribes to `IEventBus<PartBroke>` and re-evaluates phase conditions on each part break
- [ ] Phase transition is **one-way irreversible**: once Phase 2 activates, breaking a BOSS_CORE does not revert to Phase 1
- [ ] For any boss where `BOSS_CORE` breaks: `PartBroke` (for that part) is published **before** `BossCoreBroke` in the same frame — event ordering guaranteed
- [ ] BOSS_CORE break triggers `BossCoreBroke` even when all optional parts are still ALIVE (core-only speedrun path)
- [ ] Phase conditions are read from `KaijuDef.PhaseConditions[]` (SO field) — not hardcoded in controller
- [ ] Controller correctly handles all CARAPEX phases: Phase 1 (all ALIVE) → Phase 2 (any mandible BROKEN) → Phase 3 (both mandibles BROKEN)
- [ ] Controller correctly handles all VOLTWYRM phases: Phase 1 (all ALIVE) → Phase 2 (any neck_seg BROKEN) → Phase 3 (all neck_segs BROKEN OR both shields BROKEN)
- [ ] `carapex_phase2_dorsal_speed_mult` (and equivalent per-boss speed scalars) are read from `KaijuDef` SO knobs, not hardcoded

---

## Implementation Notes

*Derived from ADR-0002 Event Bus and ADR-0003 ScriptableObjects:*

`KaijuBehaviorController` is a pure C# class (not MonoBehaviour) instantiated and wired by `App`. Constructor signature:
```csharp
KaijuBehaviorController(KaijuDef def, IEventBus bus, IEmitterActivator emitterActivator)
```

On construction, subscribe to `PartBroke`. On each `PartBroke` event for this kaiju's `kaiju_id`:
1. Update internal `_brokenParts` HashSet.
2. Walk `def.PhaseConditions[]` from highest phase index downward; find the first condition where all required parts are BROKEN.
3. If resolved phase > current phase: advance phase (one-way guard: `if newPhase > _currentPhase`), call `IEmitterActivator.SetActivePhase(newPhase)`.
4. If the broken part's `part_type == PartType.BossCore`: publish `BossCoreBroke` **after** confirming `PartBroke` has been dispatched (same synchronous call stack, no deferred).

`KaijuDef.PhaseConditions` is a serialized array of `PhaseCondition`:
```csharp
[Serializable]
public struct PhaseCondition
{
    public int PhaseIndex;
    public string[] RequiredBrokenPartIds;   // all must be in _brokenParts to activate
    public string[] AnyBrokenPartIds;        // at least one must be in _brokenParts (OR gate)
}
```

Phase speed multipliers (e.g., `carapex_phase2_dorsal_speed_mult`) are authored as float fields on `KaijuDef` and read by `KaijuBehaviorController` when broadcasting phase-change data to the emitter activator. MUST NOT store the value in the controller itself — always re-read from SO.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002–003: CARAPEX KaijuDef SO authoring and pattern SO definitions
- Story 004: Wiring KaijuBehaviorController to BulletSim firing (awaits ADR-0001 LOCK)
- Story 005–010: Per-boss data and encounter integration

---

## QA Test Cases

*Logic story — automated test specs:*

- **AC-1**: Phase transitions one-way
  - Given: `KaijuBehaviorController` initialised at Phase 1 with CARAPEX KaijuDef fixture
  - When: `left_mandible` BROKEN event published, then `right_mandible` BROKEN event published
  - Then: Phase advances to 2 then 3; no rollback when querying current phase
  - Edge cases: publishing the same `PartBroke` event twice must not double-advance phase

- **AC-2**: BOSS_CORE break emits `BossCoreBroke` after `PartBroke` in same frame
  - Given: spy subscribed to both `PartBroke` and `BossCoreBroke` on event bus
  - When: `chest_reactor_core` BROKEN event simulated with `part_type = BossCore`
  - Then: spy log shows `PartBroke` at sequence index N, `BossCoreBroke` at sequence index N+1 (same dispatch cycle)
  - Edge cases: if event bus is synchronous (required by ADR-0002), sequence guaranteed by call order

- **AC-3**: Victory fires even when optional parts ALIVE
  - Given: only `chest_reactor_core` broken; mandibles and dorsal_cannon still ALIVE
  - When: BOSS_CORE break event fires
  - Then: `BossCoreBroke` still published; no guard condition blocking it

- **AC-4**: Phase condition read from SO, not hardcoded
  - Given: a custom KaijuDef fixture with Phase 2 requiring part "test_part_A" BROKEN
  - When: `PartBroke` for "test_part_A" published
  - Then: controller advances to Phase 2 — proving condition comes from SO fixture, not compiled constant

- **AC-5**: Speed multiplier read from SO on phase change
  - Given: KaijuDef fixture with `phase2_speed_mult = 1.15f`
  - When: phase advances to 2
  - Then: `IEmitterActivator.SetActivePhase(2)` is called with speed multiplier 1.15f from the SO

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Kaiju/boss_phase_controller_test.cs` — EditMode NUnit — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None — this is the foundational shared framework
- Unlocks: Story 004 (CARAPEX encounter), Story 007 (LACERA encounter), Story 010 (VOLTWYRM encounter)
