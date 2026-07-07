# Story 002: 頓幀系統 (Hitstop System)

> **Epic**: 打擊感（VFX / SFX / Game Feel）
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: M (3–4h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/game-feel.md`
**Requirement**: `TR-gamefeel-002`
*(TR-IDs derived from `game-feel.md §I` — not yet formally registered in `docs/architecture/tr-registry.yaml`; see EPIC.md note)*

**ADR Governing Implementation**:
- Primary: ADR-0002: 事件架構 — subscribe `PartBroke` / `BossCoreBreak` via `IEventBus`, same-frame synchronous dispatch
- Secondary: ADR-0003: 資料驅動調校 — read `HitstopPartBreakMs`, `HitstopBossDeathMs`, `HitstopAccessibilityMult` from `GameFeelConfig` SO

**ADR Decision Summary**: `GameFeel` subscribes to typed event structs from `Core`; zero direct system references. All timing values are read from `GameFeelConfig` SO injected at construction. Input polls on `unscaledDeltaTime` — the hitstop timer itself counts down on unscaled time so the freeze does not freeze its own countdown.

**Engine**: Unity 6.3 LTS | **Risk**: MEDIUM
**Engine Notes**:
- `Time.timeScale = 0` freezes physics (`FixedUpdate`) and all `Update()` calls using `Time.deltaTime`. Player input and UI MUST use `Time.unscaledDeltaTime` — this is enforced by the `Input` system (S6), not by this story.
- The hitstop countdown timer MUST decrement on `Time.unscaledDeltaTime`; otherwise the freeze never ends.
- `AudioSource.Play()` is NOT affected by `Time.timeScale` — SFX fires correctly during hitstop. Verify this holds in Unity 6.3 per `docs/engine-reference/unity/VERSION.md`.
- ECS BulletSim ([需查證 6.3 API]): BulletSim is responsible for incorporating `Time.timeScale` into its simulation deltaTime; this story only sets `Time.timeScale`. Verify that `timeScale = 0` correctly freezes bullet movement in the chosen BulletSim implementation.
- `HitstopPartBreakMs` safe max = 150ms (`OnValidate` in GameFeelConfig enforces this); Boss death `HitstopBossDeathMs` safe range 160–280ms.

**Control Manifest Rules (Presentation layer — §3 GameFeel)**:
- Required: Subscribe via `IEventBus.Subscribe<PartBroke>` and `IEventBus.Subscribe<BossCoreBreak>`
- Required: `Time.timeScale = 0` for hitstop; timer on `Time.unscaledDeltaTime`
- Required: All timing values from `GameFeelConfig` (no magic numbers)
- Forbidden: MUST NOT change game/part state — pure presentation consumer
- Forbidden: MUST NOT reference `KaijuParts` assembly directly

---

## Acceptance Criteria

*From GDD `design/gdd/game-feel.md §I.2, §C.2, §C.5`, scoped to this story:*

- [ ] `PartBroke` event received → `Time.timeScale = 0` immediately; restored after exactly `hitstop_part_break_ms` (115ms) ±5ms of unscaled time
- [ ] `BossCoreBreak` event received → `Time.timeScale = 0` for `hitstop_boss_death_ms` (220ms) ±5ms; overrides any in-progress part-break hitstop (timer resets to 220ms)
- [ ] Hitstop timer counts down on `Time.unscaledDeltaTime` — the freeze does not freeze its own countdown
- [ ] Player movement/dodge input received during hitstop is buffered and executed on the first frame after `Time.timeScale` is restored (input not lost)
- [ ] Bullets (Bullet pool / BulletSim) are stationary during hitstop — `timeScale = 0` is correctly propagated to the bullet simulation [verify with BulletSim team]
- [ ] Consecutive `PartBroke` events during an active hitstop: timer **resets** to `hitstop_part_break_ms` (not additive); total hitstop is never > 150ms from a single chain
- [ ] Automated test passes: `Assets/_Project/Tests/GameFeel/GameFeel_Hitstop_Test.cs`

---

## Implementation Notes

*Derived from ADR-0002 Decision (synchronous event bus + DI) and ADR-0003 Decision (SO config):*

1. Create `HitstopSystem` in `KaijuBreaker.GameFeel` assembly (Presentation layer).
2. Constructor receives `IEventBus bus` and `GameFeelConfig config` via DI (injected by `App`).
3. In constructor: `bus.Subscribe<PartBroke>(OnPartBroke)` and `bus.Subscribe<BossCoreBreak>(OnBossCoreBreak)`.
4. `OnPartBroke`: if no active hitstop or boss hitstop not active → `Time.timeScale = 0`; `_hitstopTimer = config.HitstopPartBreakMs * 0.001f`. If already in part-break hitstop → reset `_hitstopTimer` (not accumulate).
5. `OnBossCoreBreak`: `Time.timeScale = 0`; `_hitstopTimer = config.HitstopBossDeathMs * 0.001f`; `_isBossHitstop = true` (prevents part-break reset from overriding).
6. `Update()` (runs on MonoBehaviour or GameFeel orchestrator's update loop):
   - Decrement `_hitstopTimer -= Time.unscaledDeltaTime`.
   - When `_hitstopTimer <= 0` and `Time.timeScale == 0`: restore `Time.timeScale = 1.0f` (or signal `SlowMoSystem` to begin — sequencing handled in Story 006).
7. Input buffering: the `Input` system (S6) is responsible for buffering inputs on `unscaledDeltaTime`; `HitstopSystem` does NOT own input logic. This story's AC verifies the observable behavior (input not dropped).
8. Accessibility multiplier applied at trigger: `effectiveDuration = rawMs * config.HitstopAccessibilityMult` — Story 007 owns the multiplier logic but the field must be read here.
9. Use `IRandom`-free deterministic logic (no RNG in this system). Timer is purely arithmetic — unit-testable without Play Mode.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `GameFeelConfig` SO definition (must be DONE before this story)
- Story 003: Slow-motion timescale drop that begins *after* hitstop ends
- Story 006: Full D.4 payoff sequence ordering (flash, shake, debris, slow-mo handoff)
- Story 007: Accessibility multiplier knob (`HitstopAccessibilityMult`) applied at runtime
- `Input` system (S6): actual input buffering implementation during hitstop

---

## QA Test Cases

*Written by qa-lead at story creation. Automated — Logic story.*

- **AC-1**: Part break hitstop duration
  - Given: `GameFeelConfig.HitstopPartBreakMs = 115`; `HitstopSystem` initialized with injected config and fake `IEventBus`
  - When: Publish `PartBroke` event
  - Then: `Time.timeScale == 0` immediately (same frame); after 115ms ±5ms of unscaled time `Time.timeScale` is restored (≠ 0)
  - Edge cases: `HitstopPartBreakMs = 80` (safe min); `= 150` (safe max — `OnValidate` boundary)

- **AC-2**: Boss death hitstop overrides part-break hitstop
  - Given: Part-break hitstop active with 50ms remaining (`_hitstopTimer = 0.05f`)
  - When: `BossCoreBreak` event published
  - Then: `_hitstopTimer` resets to `HitstopBossDeathMs * 0.001f = 0.22f`; hitstop continues 220ms from override point
  - Edge cases: `BossCoreBreak` published with 0ms remaining on part-break timer

- **AC-3**: Consecutive part-breaks reset (not add) hitstop timer
  - Given: Part-break hitstop active, `_hitstopTimer = 0.030f` (30ms remaining)
  - When: Second `PartBroke` event received
  - Then: `_hitstopTimer == 0.115f` (reset to 115ms); total hitstop not 145ms
  - Edge cases: Two `PartBroke` events in the exact same frame (both processed synchronously; second one resets timer again → still 115ms)

- **AC-4**: Hitstop timer uses unscaled time
  - Given: `Time.timeScale = 0` (hitstop active); simulate 50ms of unscaled time passing
  - When: Check `_hitstopTimer`
  - Then: `_hitstopTimer == 0.115f - 0.050f = 0.065f` (countdown progressed despite timeScale = 0)
  - Edge cases: `Time.unscaledDeltaTime` spike (large frame) — timer overshoots to negative, clamp to 0 and restore

- **AC-5**: Input buffered during hitstop (integration-observable)
  - Given: Hitstop active (fake timeScale frozen in test); fake `IInputBuffer` injected
  - When: Dodge input signal received during hitstop
  - Then: `IInputBuffer.HasBufferedDodge == true`; after hitstop end, `IInputBuffer.ConsumeDodge()` called first frame
  - Edge cases: Multiple inputs during hitstop — buffer stores last or queues (define per input system spec)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- Logic: `Assets/_Project/Tests/EditMode/GameFeel/GameFeel_Hitstop_Test.cs` — must exist and pass (BLOCKING)

**Status**: [x] ✅ 7/7 GREEN (Unity MCP, 2026-07-07). Covers AC-1 (freeze→restore after 115ms), AC-3 (consecutive reset not accumulate), AC-4 (unscaled countdown), AC-2 (boss overrides part-break + part-break can't override boss), overshoot restore, a11y-mult-0 disables. AC-5 (input buffering) = Input system's, out of scope.

**Reconciliations vs story text** (surfaced for review):
1. Freeze goes through an injected `ITimeScaleControl` (App maps to `Time.timeScale`) so timing is EditMode-testable via `Tick(unscaledDelta)` — no Play Mode. Committed event is `BossCoreBroke` (not `BossCoreBreak`).
2. Accessibility multiplier (`HitstopAccessibilityMult`) applied at trigger; a value of 0 disables hitstop (reduce-motion, Story 007 owns the knob). No game-state mutation (pure presentation).

---

## Dependencies

- Depends on: Story 001 (GameFeelConfig SO — must be DONE)
- Unlocks: Story 006 (Break Payoff Sequence — hitstop is a prerequisite sub-system)
