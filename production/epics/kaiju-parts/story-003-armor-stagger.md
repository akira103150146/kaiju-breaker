# Story 003: Armor Gate & Stagger Timer (ARMOR_INTACT ↔ ARMOR_STRIPPED)

> **Epic**: 可破壞部位系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju-part-system.md`
**Requirement**: `TR-part-003`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; TR-ID derived from GDD §H.3)*

**ADR Governing Implementation**: ADR-0002: 事件架構 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: `KaijuParts` subscribes to `L3WaveHit` (= `on_l3_wave_hit`) events via `IEventBus`. The stagger timer drives armor-state transitions on ARMORED parts; `stagger_timer > 0` is also the sentinel that Story 004's `LookupStateMult` uses to return `1.5`. Events `PartStaggered` / `PartStaggerEnd` are published as `readonly struct` via `IEventBus.Publish<T>`, synchronous same-frame. All timing values from `PartSystemConfig` SO; BU (`B_current`) is never cleared on armor restoration.

**Engine**: Unity 6 | **Risk**: LOW
**Engine Notes**: Stagger countdown uses `Time.deltaTime`; Δt source must be consistent with the heat-tick Δt established in Story 002. Floating-point timer: clamp to `0f` via `Mathf.Max` rather than exact equality check to avoid never-reaching-zero bugs. No post-cutoff APIs.

**Control Manifest Rules (this layer — KaijuParts)**:
- Required: `on_l3_wave_hit` is the SOLE path to set `armor_state = ARMOR_STRIPPED`; no other event handler or method may change armor state (§3 KaijuParts)
- Required: KaijuParts OWNS `PartStaggered` and `PartStaggerEnd`; publish via `IEventBus.Publish<T>` (§4.2)
- Required: `stagger_duration` from `PartSystemConfig` (the same value as `l3_stagger_window` in `WeaponDef`; coordinate single-SO-owner rule — §1.2)
- Required: `B_current` NOT cleared when stagger expires and armor restores (§3 KaijuParts, §E.2 GDD)
- Required: overlapping L3 hits reset `stagger_timer` to `stagger_duration`; no additive stacking (§D.4 GDD)
- Required: BROKEN parts return immediately on `L3WaveHit` — no timer or armor changes (§3 KaijuParts)
- Forbidden: any path other than `L3WaveHit` that triggers armor strip; Forbidden: `B_current` reset on armor restore

---

## Acceptance Criteria

*From GDD `design/gdd/kaiju-part-system.md` §H.3, §C.4, §D.4, §E.2, scoped to this story:*

- [ ] H.3 / D.4 — `on_l3_wave_hit` is the sole trigger for `armor_state = ARMOR_STRIPPED`; simultaneously sets `stagger_timer = stagger_duration`; emits `PartStaggered(part_id, kaiju_id, duration, armor_stripped=true)` for ARMORED parts
- [ ] D.4 — NORMAL / BOSS_CORE parts: `on_l3_wave_hit` sets `stagger_timer = stagger_duration` and emits `PartStaggered(armor_stripped=false)`; no `armor_state` field change
- [ ] D.4 — Each frame: if `stagger_timer > 0` and `break_state != BROKEN`, `stagger_timer = Mathf.Max(stagger_timer - Δt, 0f)`
- [ ] D.4 — When `stagger_timer` reaches 0: for ARMORED parts `armor_state = ARMOR_INTACT`; emits `PartStaggerEnd(part_id, kaiju_id, armor_restored = (part_type == ARMORED))`
- [ ] E.2 — `B_current` is preserved (not cleared) when `armor_state` transitions back to `ARMOR_INTACT` after stagger expiry
- [ ] D.4 — Overlapping L3 hit while `stagger_timer > 0`: resets timer to `stagger_duration` (not additive); `armor_state` remains `ARMOR_STRIPPED`
- [ ] H.3 (gate correctness used by Story 004) — while `armor_state == ARMOR_INTACT`, missile `M_state_mult = 0` (B_fill = 0); this is enforced in Story 004's `LookupStateMult`, but this story correctly sets and maintains the `armor_state` flag that lookup reads
- [ ] BROKEN part receiving `L3WaveHit`: immediate return; no timer, no armor change, no event

---

## Implementation Notes

*Derived from ADR-0002 and ADR-0003:*

- Subscribe to `L3WaveHit` on `IEventBus` in the same `PartStateSystem` as `LaserHit` (Story 002).
- `HandleL3WaveHit(in L3WaveHit evt)`:
  ```csharp
  var part = _parts[evt.PartId];
  if (part.BreakState == BreakState.Broken) return;
  part.StaggerTimer = _config.StaggerDuration;
  if (part.PartType == PartType.Armored)
      part.ArmorState = ArmorState.Stripped;
  _bus.Publish(new PartStaggered {
      PartId = part.PartId, KaijuId = part.KaijuId,
      Duration = _config.StaggerDuration,
      ArmorStripped = part.PartType == PartType.Armored });
  ```
- `TickStagger(float deltaTime)` — called in same frame-tick loop as `TickHeat`:
  ```csharp
  foreach (var part in _parts.Values)
  {
      if (part.StaggerTimer <= 0f || part.BreakState == BreakState.Broken) continue;
      part.StaggerTimer = Mathf.Max(part.StaggerTimer - deltaTime, 0f);
      if (part.StaggerTimer == 0f)
      {
          if (part.PartType == PartType.Armored) part.ArmorState = ArmorState.Intact;
          _bus.Publish(new PartStaggerEnd {
              PartId = part.PartId, KaijuId = part.KaijuId,
              ArmorRestored = part.PartType == PartType.Armored });
      }
  }
  ```
- `PartStaggered` struct fields: `PartId`, `KaijuId`, `Duration` (float), `ArmorStripped` (bool). Declared in `Core`.
- `PartStaggerEnd` struct fields: `PartId`, `KaijuId`, `ArmorRestored` (bool). Declared in `Core`.
- `L3WaveHit` struct fields (consumed): `PartId`, `KaijuId`. Published by `Weapons` assembly.
- `stagger_duration` ownership: `PartSystemConfig.StaggerDuration` is the single source of truth. `WeaponDef.L3StaggerWindow` must equal it; verify at `OnValidate` time or treat `PartSystemConfig` as the master and have `WeaponDef` reference it.
- Tests inject `FakeEventBus` and `PartSystemConfig` fixture; call `HandleL3WaveHit` then `TickStagger` with controlled `deltaTime`; assert `StaggerTimer`, `ArmorState`, and recorded events.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `PartSystemConfig` knob definitions, `BreakablePart` data structure, `armor_state` field initialisation
- Story 002: Heat fill/decay, INTACT↔SOFTENED transitions, `PartSoftened` / `PartSoftenedExit` events
- Story 004: `on_missile_hit` handler; `M_state_mult` lookup that READS `armor_state` and `stagger_timer` produced here
- Story 006: VFX/GameFeel system consuming `PartStaggered` to show weakness indicator frame within 0.3s

---

## QA Test Cases

*Written by qa-lead at story creation. Implement against these — do not invent new test cases during implementation.*

- **AC-1**: ARMORED part — L3 wave strips armor and starts stagger timer
  - Given: ARMORED part, `armor_state=ARMOR_INTACT`, `stagger_timer=0`, `stagger_duration=2.0s`
  - When: `HandleL3WaveHit` called
  - Then: `armor_state=ARMOR_STRIPPED`, `stagger_timer=2.0s`; one `PartStaggered` published with `ArmorStripped=true`, `Duration=2.0`
  - Edge cases: BROKEN ARMORED part → immediate return, no field changes, no event

- **AC-2**: NORMAL part — L3 staggers but no armor change
  - Given: NORMAL part, `stagger_timer=0`, `stagger_duration=2.0s`
  - When: `HandleL3WaveHit` called
  - Then: `stagger_timer=2.0s`; one `PartStaggered` published with `ArmorStripped=false`; NORMAL has no `armor_state` field affected
  - Edge cases: BOSS_CORE part — same behaviour as NORMAL (no armor state)

- **AC-3**: Stagger timer counts down per frame; clamps at 0
  - Given: ARMORED part, `stagger_timer=2.0s`; `Δt=0.016s`
  - When: `TickStagger(0.016f)` called
  - Then: `stagger_timer=1.984s`; no `PartStaggerEnd` yet
  - Edge cases: final frame — `stagger_timer=0.01s`, `Δt=0.016s` → `Mathf.Max(0.01-0.016, 0) = 0`; `PartStaggerEnd` fires; value never goes negative

- **AC-4**: Armor restores at expiry; `B_current` preserved
  - Given: ARMORED part, `armor_state=ARMOR_STRIPPED`, `stagger_timer=0.01s`, `B_current=75`; `Δt=0.016s`
  - When: `TickStagger(0.016f)`
  - Then: `stagger_timer=0`, `armor_state=ARMOR_INTACT`; `PartStaggerEnd` published with `ArmorRestored=true`; `B_current` still `75` (not reset)
  - Edge cases: NORMAL part at expiry → `PartStaggerEnd` with `ArmorRestored=false`; no armor_state change

- **AC-5**: Overlapping L3 resets timer — no additive stacking
  - Given: ARMORED part, `stagger_timer=1.0s` (mid-stagger); `stagger_duration=2.0s`
  - When: second `HandleL3WaveHit` called
  - Then: `stagger_timer=2.0s` (reset, not 3.0s); `armor_state` still `ARMOR_STRIPPED`
  - Edge cases: two L3 hits in the same frame → timer reset twice = still `2.0s` (idempotent)

- **AC-6**: Cross-window BU accumulation — BU not zero'd between windows
  - Given: ARMORED part; first stagger window ends with `B_current=40`; `armor_state` restores to `ARMOR_INTACT`
  - When: second `HandleL3WaveHit` arrives → `armor_state=ARMOR_STRIPPED` again
  - Then: `B_current` is still `40` before any new missile hits in window 2
  - Edge cases: `B_current` does not decay during `ARMOR_INTACT` periods (no BU decay mechanic in GDD)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/KaijuParts/EditMode/kaijuparts_armor_stagger_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (`BreakablePart`, `PartSystemConfig`, `L3WaveHit` / `PartStaggered` / `PartStaggerEnd` Core types) must be DONE
- Unlocks: Story 004 (`armor_state` and `stagger_timer` are consumed by `LookupStateMult` in break-bar update)
