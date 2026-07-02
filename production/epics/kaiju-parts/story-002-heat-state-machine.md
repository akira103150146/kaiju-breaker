# Story 002: Heat State Machine (INTACT ↔ SOFTENED)

> **Epic**: 可破壞部位系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju-part-system.md`
**Requirement**: `TR-part-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; TR-ID derived from GDD §H.1)*

**ADR Governing Implementation**: ADR-0002: 事件架構 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: `KaijuParts` subscribes to `LaserHit` (= `on_laser_hit`) events via `IEventBus.Subscribe<LaserHit>`; updates `H_current` per D.1 formula each frame. Heat-state transitions publish `PartSoftened` / `PartSoftenedExit` as `readonly struct` events via `IEventBus.Publish<T>` — synchronous, same-frame dispatch. All thresholds and decay rates read from `PartSystemConfig` SO injected at construction. Zero GC in hot path (struct events + `in` keyword).

**Engine**: Unity 6 | **Risk**: LOW
**Engine Notes**: Frame delta time via `Time.deltaTime`; ensure the tick method's `deltaTime` source is consistent with the game loop strategy documented in implementation (scaled time for game logic). No post-cutoff APIs involved.

**Control Manifest Rules (this layer — KaijuParts)**:
- Required: subscribe to `LaserHit` via `IEventBus.Subscribe<LaserHit>`; publish `PartSoftened` / `PartSoftenedExit` via `IEventBus.Publish<T>` (§4.1)
- Required: all threshold values (`theta_S`, `theta_S_exit`, `H_decay_rate`, `H_max_*`) from `PartSystemConfig` SO; no hardcoded values (§1.2)
- Required: BROKEN parts (`break_state == BROKEN`) skip all heat updates — immediate return on any hit event (§3 KaijuParts)
- Required: KaijuParts is the SOLE publisher of `PartSoftened` and `PartSoftenedExit`; no other system may publish these (§4.2)
- Forbidden: direct reference to `Weapons` assembly; all input arrives via EventBus (§1.4)
- Forbidden: hardcoded threshold, decay, or H_max values (§5)

---

## Acceptance Criteria

*From GDD `design/gdd/kaiju-part-system.md` §H.1, §D.1, §D.2, scoped to this story:*

- [ ] D.1 — Each frame, if `break_state != BROKEN`: when `on_laser_hit` received for this part in the current frame, `H_current += heat_delta` (decay suppressed for this frame); when no `on_laser_hit` received, `H_current -= H_decay_rate × Δt`; result clamped to `[0, H_max]` — decay and fill are mutually exclusive per frame
- [ ] D.2 — INTACT → SOFTENED: when updated `H_current >= theta_S`, `heat_state = SOFTENED`; `PartSoftened(part_id, kaiju_id, H_current, H_max)` published exactly once per transition
- [ ] D.2 — SOFTENED → INTACT: when `H_current < theta_S_exit`, `heat_state = INTACT`; `PartSoftenedExit(part_id, kaiju_id)` published exactly once per transition
- [ ] D.2 Hysteresis — parts with `H_current` in `[theta_S_exit, theta_S)` retain current `heat_state` with no event emitted (no oscillation across the boundary)
- [ ] `H_current` is always clamped to `[0, H_max]` after every update — no underflow below 0, no overflow above `H_max`
- [ ] BROKEN parts: any `on_laser_hit` received → immediate return; `H_current` unchanged, `heat_state` unchanged, no event published

---

## Implementation Notes

*Derived from ADR-0002 and ADR-0003:*

- Subscribe to `LaserHit` on `IEventBus` in `PartStateSystem` constructor / `OnEnable`; unsubscribe on `Dispose` / `OnDisable`. Store pending heat deltas in a `Dictionary<string, float> _pendingHeatDeltas` (keyed by `part_id`), cleared each frame after processing.
- `TickHeat(float deltaTime)` — called once per frame for all parts:
  ```csharp
  foreach (var part in _parts.Values)
  {
      if (part.BreakState == BreakState.Broken) continue;
      if (_pendingHeatDeltas.TryGetValue(part.PartId, out float delta))
          part.HCurrent = Mathf.Clamp(part.HCurrent + delta, 0f, part.HMax);
      else
          part.HCurrent = Mathf.Clamp(part.HCurrent - _config.HDecayRate * deltaTime, 0f, part.HMax);
      EvaluateHeatState(part);
  }
  _pendingHeatDeltas.Clear();
  ```
- `EvaluateHeatState(part)` — two separate branches to preserve hysteresis:
  ```csharp
  if (part.HeatState == HeatState.Intact && part.HCurrent >= _config.ThetaS)
  {
      part.HeatState = HeatState.Softened;
      _bus.Publish(new PartSoftened { PartId=part.PartId, KaijuId=part.KaijuId,
          CurrentHeat=part.HCurrent, HMax=part.HMax });
  }
  else if (part.HeatState == HeatState.Softened && part.HCurrent < _config.ThetaSExit)
  {
      part.HeatState = HeatState.Intact;
      _bus.Publish(new PartSoftenedExit { PartId=part.PartId, KaijuId=part.KaijuId });
  }
  ```
- `PartSoftened` struct fields: `PartId` (string), `KaijuId` (string), `CurrentHeat` (float), `HMax` (float) — payload needed by VFX for proportional glow intensity scaling.
- `PartSoftenedExit` struct fields: `PartId` (string), `KaijuId` (string).
- Both structs declared in `Core` assembly, implement `IGameEvent`.
- Tests inject `PartSystemConfig` fixture and a `FakeEventBus` that records published events; call `HandleLaserHit` and `TickHeat` directly; assert recorded event list and part field values.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `PartSystemConfig` / `KaijuDef` SO definitions, `BreakablePart` initialization, `IPartStateQuery` interface
- Story 003: `on_l3_wave_hit` handler, armor state transitions, stagger-timer countdown, `PartStaggered` / `PartStaggerEnd` events
- Story 004: `on_missile_hit` handler, D.3 break-bar formula, `PartBroke` emission, `break_quality` computation
- Story 006: VFX/GameFeel system consuming `PartSoftened`; 0.5s visual onset verification

---

## QA Test Cases

*Written by qa-lead at story creation. Implement against these — do not invent new test cases during implementation.*

- **AC-1**: Heat accumulates from laser hit; decay suppressed same frame
  - Given: part `H_current=50`, `H_max=100`, `H_decay_rate=3`; `LaserHit(heat_delta=10)` registered; `Δt=1.0s`
  - When: `TickHeat(1.0f)`
  - Then: `H_current=60` (fill applied, decay NOT applied); no `PartSoftened` (60 < theta_S=100)
  - Edge cases: `heat_delta <= 0` should be rejected at the event source (GDD: "must be > 0"); guard with early return if delta ≤ 0

- **AC-2**: Heat decays when no laser hit this frame
  - Given: part `H_current=50`, `H_decay_rate=3`; no `LaserHit`; `Δt=1.0s`
  - When: `TickHeat(1.0f)`
  - Then: `H_current=47`
  - Edge cases: `H_current=2`, `Δt=1.0s` → clamped to `0` (not `-1`); `H_current=0`, no laser → stays `0`

- **AC-3**: INTACT → SOFTENED transition fires `PartSoftened` once
  - Given: part `heat_state=INTACT`, `H_current=99`, `theta_S=100`; `LaserHit(heat_delta=2)`
  - When: `TickHeat`
  - Then: `H_current=100` (clamped to H_max=100), `heat_state=SOFTENED`; exactly one `PartSoftened` published with `CurrentHeat=100`, `HMax=100`
  - Edge cases: already `SOFTENED` and `H_current` stays `>= theta_S` → no duplicate `PartSoftened` on subsequent frames

- **AC-4**: Hysteresis band — no event when H_current in [theta_S_exit, theta_S)
  - Given: part `heat_state=SOFTENED`, `H_current=90`, `theta_S=100`, `theta_S_exit=80`; no laser hit; `H_decay_rate=3`, `Δt=1.0s`
  - When: `TickHeat(1.0f)` → `H_current` decays to `87` (still in [80, 100))
  - Then: `heat_state` remains `SOFTENED`; no `PartSoftenedExit` published
  - Edge cases: `H_current=80.0` exactly — condition is `< theta_S_exit`, so 80.0 is NOT `< 80.0` → remains SOFTENED

- **AC-5**: SOFTENED → INTACT transition fires `PartSoftenedExit` once
  - Given: part `heat_state=SOFTENED`, `H_current=79`, `theta_S_exit=80`; no laser hit; `Δt=0.016s`
  - When: `TickHeat` (H_current already below threshold; decay brings it lower)
  - Then: `heat_state=INTACT`; exactly one `PartSoftenedExit` published; subsequent frame at `H_current=76` produces no second `PartSoftenedExit`
  - Edge cases: `H_current=80` (not `< 80`) → no transition; `H_current=79.99` → transitions

- **AC-6**: BROKEN part ignores laser hit entirely
  - Given: part `break_state=BROKEN`, `H_current=50`
  - When: `LaserHit` received; `TickHeat` called
  - Then: `H_current` still `50`; no `PartSoftened` published; `heat_state` unchanged

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/KaijuParts/EditMode/kaijuparts_heat_sm_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (`BreakablePart` struct, `PartSystemConfig` SO, `IEventBus` + `LaserHit` / `PartSoftened` / `PartSoftenedExit` types in `Core`) must be DONE
- Unlocks: Story 003 (stagger timer reads `heat_state` established here), Story 004 (break-bar `LookupStateMult` reads `heat_state`), Story 006 (readability hooks verify `PartSoftened` payload)
