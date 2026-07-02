# Story 007: Reduce-Motion 無障礙開關 (Reduce-Motion Accessibility Toggle)

> **Epic**: 打擊感（VFX / SFX / Game Feel）
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: M (3–4h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/game-feel.md`
**Requirement**: `TR-gamefeel-006`
*(TR-IDs derived from `game-feel.md §I` — not yet formally registered in `docs/architecture/tr-registry.yaml`; see EPIC.md note)*

**ADR Governing Implementation**:
- Primary: ADR-0003: 資料驅動調校 — `ShakeAccessibilityMult`, `FlashAccessibilityMult`, `SlowmoAccessibilityMult`, `HitstopAccessibilityMult` are `GameFeelConfig` knobs; toggling reduce-motion sets each to the appropriate value; systems already read these multipliers (Stories 002–006)
- Secondary: ADR-0002: 事件架構 — toggle state change does not require event bus; it is a direct settings write-then-read pattern; existing subscriptions in each system already read config each invocation

**ADR Decision Summary**: All game-feel systems read their accessibility multiplier from `GameFeelConfig` at every trigger call (no cached values), so changing the multiplier fields is immediately effective next invocation without re-subscribing or restarting. Toggle state is persisted to player save (ADR-0004) to survive app restart. The `GameFeelConfig` SO is the single source of truth for multiplier values; the toggle just writes to the appropriate fields.

**Engine**: Unity 6.3 LTS | **Risk**: MEDIUM
**Engine Notes**:
- Immediate effect requires that no system caches the multiplier at subscribe time. Verify Stories 002–006 implementations read `config.XAccessibilityMult` at trigger time, not in `Awake`/constructor.
- Toggle persistence: player save (ADR-0004) via `ISaveService.SaveAccessibilityPreference(bool reduceMotion)`. Save integration is not the focus of this story — coordinate with Meta system (S10) for the save API. This story's AC tests the in-session immediate effect.
- No engine-specific API needed beyond existing `Time.timeScale` and `GameFeelConfig` field writes.
- `SlowmoAccessibilityMult = 0.0`: when this is 0, `SlowMoSystem.TriggerSlowMo` is a no-op (as implemented in Story 003 — checks the multiplier). Verify this path in unit test.

**Control Manifest Rules (Presentation layer — §3 GameFeel)**:
- Required: All accessibility multipliers live in `GameFeelConfig` (single source of truth, ADR-0003)
- Required: Immediate effect without game restart
- Required: Toggle state persisted via ADR-0004 save mechanism
- Forbidden: MUST NOT change game/part state
- Forbidden: MUST NOT introduce a separate "accessibility config" SO — `GameFeelConfig` owns all game-feel knobs including accessibility

---

## Acceptance Criteria

*From GDD `design/gdd/game-feel.md §I.7, §H.1`, scoped to this story:*

- [ ] Reduce-Motion toggle enabled → `ShakeAccessibilityMult = 0.25` (shake at 25% intensity; not 0 — preserves hit feedback)
- [ ] Reduce-Motion toggle enabled → `FlashAccessibilityMult = 0.0` (full white flash completely disabled; no overlay)
- [ ] Reduce-Motion toggle enabled → `SlowmoAccessibilityMult = 0.0` (slow-motion fully disabled; no timescale drop)
- [ ] Reduce-Motion toggle enabled → `HitstopAccessibilityMult = 0.5` (hitstop at 50% duration; 115ms → ~58ms, 220ms → 110ms)
- [ ] All four multiplier changes take effect immediately in the same session — no restart required
- [ ] Reduce-Motion disabled → all multipliers restore to 1.0 (full effects)
- [ ] Toggle state is persisted to player save — survives app quit and restart (verify with Meta system / ADR-0004 integration)
- [ ] Hitstop in Reduce-Motion mode: beat/tempo sensation still perceptible at 50% duration (QA subjective — advisory)
- [ ] Automated test passes: `Assets/_Project/Tests/GameFeel/GameFeel_ReduceMotion_Test.cs`

---

## Implementation Notes

*Derived from ADR-0003 Decision (SO as single config source), ADR-0002 (existing subscriptions read config at invocation), GDD §H.1:*

1. Add a runtime-mutable field `bool ReduceMotionEnabled` to `GameFeelConfig` (runtime state only — NOT serialized in the `.asset` file; this is a session value set from save at app load). Note: `GameFeelConfig` is defined as read-only by ADR-0003, but accessibility state is a player preference that must be settable. Resolve: keep the SO fields for multiplier values as SO defaults; add a `RuntimeGameFeelSettings` class (not a SO) that mirrors multipliers and is populated from save at load. Systems read from `RuntimeGameFeelSettings` (injected). Alternatively: have `GameFeelConfig` expose non-serialized `[NonSerialized]` fields overridden at load. **Coordinate with Technical Director on chosen approach before implementing.**
2. Create `GameFeelSettingsService` in `KaijuBreaker.GameFeel` assembly. Constructor: `GameFeelConfig config`, `ISaveService save`.
3. `SetReduceMotion(bool enabled)`:
   - If `enabled`: set `config.ShakeAccessibilityMult = 0.25f`; `config.FlashAccessibilityMult = 0.0f`; `config.SlowmoAccessibilityMult = 0.0f`; `config.HitstopAccessibilityMult = 0.5f`.
   - If `disabled`: restore all to `1.0f`.
   - Call `save.SaveAccessibilityPreference(enabled)` (non-blocking, queued save).
4. `LoadFromSave(bool savedReduceMotion)`: called at game start; applies the saved preference via `SetReduceMotion`.
5. All five systems (HitstopSystem, SlowMoSystem, ShakeSystem, FlashSystem/BreakPayoffHandler, SoftenedSignatureSystem) read their multiplier from `config` at every trigger call — confirmed by Stories 002–006 implementations. No cached value issue.
6. `SlowmoAccessibilityMult = 0.0` path: in `SlowMoSystem.TriggerSlowMo()`, guard: `if (config.SlowmoAccessibilityMult <= 0f) return;` — no timescale change.
7. `FlashAccessibilityMult = 0.0` path: in `FlashSystem.Trigger()`, `effectiveValue = value * config.FlashAccessibilityMult` → if 0, `_flashIntensity` stays 0, no overlay rendered.

> **Implementation Note on SO mutability**: If the architectural decision is to keep `GameFeelConfig` as a true runtime-readonly SO, introduce a thin `RuntimeGameFeelOverrides` (plain C# class, not SO) that multiplies against the SO baseline. Systems receive both via constructor: `effectiveMult = so.ShakeAccessibilityMult * overrides.ShakeAccessibilityMult`. `SetReduceMotion` writes to `RuntimeGameFeelOverrides`, not the SO. This preserves ADR-0003's SO-readonly contract. Flag this decision to Technical Director before implementation.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Stories 002–006: The multiplier read paths (already implemented when this story runs)
- UI system (S7): Settings menu screen where the player finds the toggle; this story only implements the underlying logic
- SOFTENED icon toggle (§C.3 Optional Layer 4 — `SoftenedIconEnabled`) — separate per-feature accessibility setting, not reduce-motion
- Color-blind mode — future epic
- ADR-0004 (Meta/save) full save implementation — coordinate with Meta system; this story verifies persistence but does not own the save format

---

## QA Test Cases

*Written by qa-lead at story creation. Automated — Logic story.*

- **AC-1**: Shake at 25% in Reduce-Motion mode
  - Given: `GameFeelSettingsService` initialized; `SetReduceMotion(true)` called; fake `ShakeSystem` with injected config
  - When: Shake triggered with raw magnitude 24
  - Then: `effectiveMagnitude == 24 * 0.25 = 6.0` (≤ 6px actual camera offset)
  - Edge cases: `mult = 0.0` → `effectiveMagnitude = 0`; `mult = 1.0` → `effectiveMagnitude = 24`

- **AC-2**: Flash fully disabled in Reduce-Motion mode
  - Given: `SetReduceMotion(true)` applied; `FlashAccessibilityMult = 0.0`
  - When: `flash.Trigger(0.92f)` called (from part break)
  - Then: `_flashIntensity == 0.0` (0.92 × 0.0 = 0); no white overlay rendered
  - Edge cases: Toggle off mid-flash (`_flashIntensity = 0.5`): multiply by new mult → `0.5 × 0.0 = 0` (immediate disable)

- **AC-3**: Slow-mo disabled in Reduce-Motion mode
  - Given: `SetReduceMotion(true)` applied; `SlowmoAccessibilityMult = 0.0`
  - When: `SlowMoSystem.TriggerPartBreak()` called
  - Then: `Time.timeScale` NOT modified by slow-mo (remains at 1.0 or whatever hitstop left it at)
  - Edge cases: Toggle reduce-motion ON while slow-mo ramp is in progress → ramp terminates immediately, `Time.timeScale` jumps to 1.0 next frame check

- **AC-4**: Hitstop at 50% duration in Reduce-Motion mode
  - Given: `SetReduceMotion(true)` applied; `HitstopAccessibilityMult = 0.5`; `HitstopPartBreakMs = 115`
  - When: `PartBroke` received
  - Then: Effective hitstop duration = `115 * 0.5 = 57.5ms` (within ±5ms tolerance → 52.5–62.5ms)
  - Edge cases: `HitstopAccessibilityMult = 0.0` → 0ms hitstop (no freeze); boss death 220ms × 0.5 = 110ms

- **AC-5**: Immediate effect without restart
  - Given: Reduce-Motion disabled (all mults = 1.0); in-session, between two part breaks
  - When: `SetReduceMotion(true)` called; next `PartBroke` received
  - Then: Next part break uses reduced values (shake 25%, flash 0%, etc.); previous part break was full values; no restart
  - Edge cases: Toggle rapidly on/off/on within same second — each part break uses the current mult at trigger time

- **AC-6**: Restore to full on disable
  - Given: `SetReduceMotion(true)` applied; then `SetReduceMotion(false)` called
  - When: All mult fields checked
  - Then: `ShakeAccessibilityMult = 1.0`, `FlashAccessibilityMult = 1.0`, `SlowmoAccessibilityMult = 1.0`, `HitstopAccessibilityMult = 1.0`
  - Edge cases: Toggle disable when no reduce-motion was active (idempotent — all already 1.0)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- Logic: `Assets/_Project/Tests/GameFeel/GameFeel_ReduceMotion_Test.cs` — must exist and pass (BLOCKING)
- Manual (advisory): `production/qa/evidence/reduce-motion-evidence.md` — subjective QA check that hitstop at 50% still "feels like a beat" and shake at 25% still provides haptic sense

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (GameFeelConfig SO — must be DONE), Story 002 (HitstopSystem DONE), Story 003 (SlowMoSystem DONE), Story 004 (ShakeSystem DONE), Story 005 (SOFTENED Signature DONE), Story 006 (Break Payoff DONE — needed to test full reduce-motion effect across integrated pipeline)
- Unlocks: None (final story in game-feel epic)
