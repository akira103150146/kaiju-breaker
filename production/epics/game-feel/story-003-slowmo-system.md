# Story 003: 慢動作系統 (Slow-Motion System)

> **Epic**: 打擊感（VFX / SFX / Game Feel）
> **Status**: ✅ Complete (2026-07-07, EditMode GREEN)
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: M (3–4h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/game-feel.md`
**Requirement**: `TR-gamefeel-003`
*(TR-IDs derived from `game-feel.md §I` — not yet formally registered in `docs/architecture/tr-registry.yaml`; see EPIC.md note)*

**ADR Governing Implementation**:
- Primary: ADR-0002: 事件架構 — subscribe `PartBroke` / `BossCoreBreak` via `IEventBus`
- Secondary: ADR-0003: 資料驅動調校 — read all G.2 slow-mo knobs from `GameFeelConfig` SO

**ADR Decision Summary**: `SlowMoSystem` subscribes to typed struct events from `Core.IEventBus`; no direct system references. All timescale values and durations are read from `GameFeelConfig` (injected). Slow-mo formula D.2 runs on `Time.unscaledDeltaTime` (the FX clock) so the ramp-up is independent of the scaled timescale it is manipulating.

**Engine**: Unity 6.3 LTS | **Risk**: MEDIUM
**Engine Notes**:
- `Time.timeScale = 0.12` (or `0.05`) affects `FixedUpdate` and all `Update()` calls using `Time.deltaTime`. Physics slows; projectile movement slows (correct per GDD §C.6).
- Ramp-up MUST use `Time.unscaledDeltaTime` — otherwise the ramp itself would run at 0.12x speed and take 8× longer than intended.
- `AudioSource` playback: default behavior in Unity is that `AudioSource.pitch` is NOT affected by `Time.timeScale`. SFX plays at normal speed during slow-mo. [需查證 6.3 API]: verify `AudioSource.pitch` and `AudioClip` speed are unaffected by `Time.timeScale` in Unity 6.3 LTS before relying on this.
- Player input: `Input` system (S6) uses `Time.unscaledDeltaTime` — input sampling frequency is NOT reduced during slow-mo. This story does not own input, but the AC verifies the observable result.
- Slow-mo is triggered AFTER hitstop ends (Story 006 owns sequencing); this system only implements the drop + ramp math.

**Control Manifest Rules (Presentation layer — §3 GameFeel)**:
- Required: Subscribe `PartBroke` and `BossCoreBreak` via `IEventBus.Subscribe<T>`
- Required: Ramp calculation on `Time.unscaledDeltaTime` (FX clock)
- Required: All knobs from `GameFeelConfig` — zero magic numbers
- Forbidden: MUST NOT change game/part state
- Forbidden: MUST NOT reference `KaijuParts` assembly

---

## Acceptance Criteria

*From GDD `design/gdd/game-feel.md §I.3, §D.2`, scoped to this story:*

- [ ] On `PartBroke`: `Time.timeScale` drops instantly to `slowmo_part_break_timescale` (0.12); held for `slowmo_part_break_hold_s` (0.65s) on unscaled clock; then ramps linearly at `slowmo_ramp_rate` (3.8/s) back to 1.0
- [ ] On `BossCoreBreak`: `Time.timeScale` drops instantly to `slowmo_boss_death_timescale` (0.05); held for `slowmo_boss_death_hold_s` (1.20s); then same ramp rate back to 1.0
- [ ] Boss death slow-mo overrides any in-progress part-break slow-mo (uses the lower timescale_min and longer hold)
- [ ] Ramp math: `time_scale(t) = min(1.0, timescale_min + slowmo_ramp_rate × elapsed_fxDt)` per GDD D.2
- [ ] SFX playback speed is NOT reduced during slow-mo (AudioSource unaffected by `Time.timeScale`)
- [ ] Player input sampling frequency is NOT reduced during slow-mo (input polls at normal unscaled rate)
- [ ] If a new `PartBroke` arrives during an active slow-mo hold, the hold timer resets to `slowmo_part_break_hold_s` (max-not-add, consistent with E.1)
- [ ] Automated test passes: `Assets/_Project/Tests/GameFeel/GameFeel_SlowMo_Test.cs`

---

## Implementation Notes

*Derived from ADR-0002 Decision (synchronous event bus + DI) and ADR-0003 Decision (SO config), GDD D.2 formula:*

1. Create `SlowMoSystem` in `KaijuBreaker.GameFeel` assembly. Constructor receives `IEventBus bus` and `GameFeelConfig config`.
2. Subscribe: `bus.Subscribe<PartBroke>(OnPartBroke)` and `bus.Subscribe<BossCoreBreak>(OnBossCoreBreak)`.
3. `TriggerSlowMo(float timescaleMin, float holdDuration)` internal method:
   - `Time.timeScale = timescaleMin`
   - `_holdTimer = holdDuration`
   - `_isActive = true`
4. `OnPartBroke`: call `TriggerSlowMo(config.SlowmoPartBreakTimescale, config.SlowmoPartBreakHoldS)`. If boss slow-mo active, ignore (boss takes precedence).
5. `OnBossCoreBreak`: call `TriggerSlowMo(config.SlowmoBossDeathTimescale, config.SlowmoBossDeathHoldS)`; `_isBossSlowMo = true`.
6. Per-frame update (unscaled time):
   - If `_holdTimer > 0`: `_holdTimer -= Time.unscaledDeltaTime`.
   - Else if `Time.timeScale < 1.0f`: `Time.timeScale = Mathf.Min(1.0f, Time.timeScale + config.SlowmoRampRate * Time.unscaledDeltaTime)`.
   - When `Time.timeScale >= 1.0f`: `_isActive = false`; `_isBossSlowMo = false`.
7. Accessibility multiplier: if `config.SlowmoAccessibilityMult == 0.0f`, skip `TriggerSlowMo` entirely (Story 007 sets the multiplier; this system checks the value).
8. `SlowMoSystem` is triggered by Story 006 (Break Payoff Sequence), not by directly subscribing to `PartBroke` for sequencing — the Story 006 orchestrator calls `SlowMoSystem.TriggerPartBreak()` after hitstop ends. However, the system must also work standalone (for unit testing). Coordinate with Story 006 on whether direct subscription or method call is used.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `GameFeelConfig` SO definition
- Story 002: Hitstop (`Time.timeScale = 0`) that precedes slow-mo
- Story 006: D.4 sequence ordering — this story only provides the `TriggerPartBreak()` / `TriggerBossDeath()` API
- Story 007: Setting `SlowmoAccessibilityMult = 0.0` at runtime

---

## QA Test Cases

*Written by qa-lead at story creation. Automated — Logic story.*

- **AC-1**: Part break slow-mo instant drop
  - Given: `config.SlowmoPartBreakTimescale = 0.12`, `SlowMoSystem` initialized
  - When: `SlowMoSystem.TriggerPartBreak()` called
  - Then: `Time.timeScale == 0.12f` on the same call (no delay, no lerp to drop)
  - Edge cases: `Time.timeScale` already 0.5 from a previous event → overrides to 0.12

- **AC-2**: Hold duration correct (unscaled)
  - Given: Part break slow-mo triggered (`timescaleMin = 0.12`, `holdS = 0.65`)
  - When: Advance `Time.unscaledDeltaTime` by 0.65s in test (simulated)
  - Then: `_holdTimer <= 0` and ramp begins; `Time.timeScale` starts increasing
  - Edge cases: `holdS = 0` — ramp begins immediately on next frame

- **AC-3**: Ramp math formula correctness
  - Given: Hold has ended; `timescaleMin = 0.12`, `slowmoRampRate = 3.8`; start ramp
  - When: 0.1s of unscaled time passes
  - Then: `Time.timeScale == Mathf.Min(1.0f, 0.12f + 3.8f * 0.1f) = 0.5f`
  - Edge cases: Ramp overshoots 1.0 → clamped to exactly 1.0; `_isActive` set false

- **AC-4**: Boss death slow-mo values
  - Given: `config.SlowmoBossDeathTimescale = 0.05`, `config.SlowmoBossDeathHoldS = 1.20`
  - When: `SlowMoSystem.TriggerBossDeath()` called
  - Then: `Time.timeScale == 0.05f`; hold for 1.20s; ramp to 1.0 in ≈ 0.25s (theoretical: (1.0-0.05)/3.8 ≈ 0.25s)
  - Edge cases: `TriggerPartBreak` received during boss slow-mo hold → ignored (boss takes precedence)

- **AC-5**: Consecutive part breaks reset hold (not add)
  - Given: Part break hold at 0.3s remaining
  - When: Second `PartBroke` triggers another `TriggerPartBreak()`
  - Then: `_holdTimer` resets to 0.65s (full hold duration, not 0.95s)
  - Edge cases: Second `TriggerPartBreak` while ramp is in progress → resets to `timescaleMin + full hold`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- Logic: `Assets/_Project/Tests/GameFeel/GameFeel_SlowMo_Test.cs` — must exist and pass (BLOCKING)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (GameFeelConfig SO — must be DONE)
- Unlocks: Story 006 (Break Payoff Sequence — slow-mo is a prerequisite sub-system)
