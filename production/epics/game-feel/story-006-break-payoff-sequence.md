# Story 006: 破壞爆破酬勞序列 (Break Payoff Sequence)

> **Epic**: 打擊感（VFX / SFX / Game Feel）
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: L (5–7h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/game-feel.md`
**Requirement**: `TR-gamefeel-005` (primary — flash readability: ≤0.4s fade, player pixel visible during flash), `TR-gamefeel-002` (partial — sequencing verification: hitstop correctly gates slow-mo start)
*(TR-IDs derived from `game-feel.md §I` — not yet formally registered in `docs/architecture/tr-registry.yaml`; see EPIC.md note)*

**ADR Governing Implementation**:
- Primary: ADR-0002: 事件架構 — subscribe `PartBroke` and `BossCoreBreak` via `IEventBus`; same-frame synchronous dispatch preserves D.4 ordering
- Secondary: ADR-0003: 資料驅動調校 — `FlashDecayRate`, `FlashMaxAlpha`, `FlashAccessibilityMult`, debris particle counts from `GameFeelConfig`

**ADR Decision Summary**: `BreakPayoffHandler` subscribes to `PartBroke` and `BossCoreBreak` via `IEventBus`. Same-frame synchronous dispatch means all D.4 steps that happen "同幀" are triggered within the same dispatch call. Steps that happen after a delay (hitstop → slow-mo) use a coroutine or unscaled timer running on `unscaledDeltaTime`. All values from `GameFeelConfig`. No direct state changes to game/part systems.

**Engine**: Unity 6.3 LTS | **Risk**: MEDIUM
**Engine Notes**:
- `AudioSource.PlayOneShot(sfxBreak)`: unaffected by `Time.timeScale` — fires correctly when called during a frame before hitstop is set. Verify in Unity 6.3 `[需查證 6.3 API]`.
- Flash overlay: fullscreen Canvas `Image` with `GraphicRaycaster` disabled; rendered above game world but below UI cursor. Verify Canvas `sortingOrder` / `renderMode` in URP 2D does not interfere with post-process `[需查證 6.3 API]`.
- Material orb homing: update position on `Time.unscaledDeltaTime` so orbs travel at expected speed during slow-mo (they should appear to fly at normal speed while the world is slowed — giving the player time to see them). GDD design intent: orbs spawn during slow-mo and travel visibly.
- Hitstop-to-slowmo handoff: use a coroutine with `yield return new WaitForSecondsRealtime(hitstopDuration)` (real time, unscaled) or an unscaled timer in `Update` that calls `SlowMoSystem.TriggerPartBreak()` after hitstop ends.
- Debris particle pool: pre-warmed; no `Instantiate` in hot path. Verify particle pool API compatibility with Unity 6.3 `[需查證 6.3 API]`.
- `PartBroke.world_position` from payload (ADR-0002 payload carries `world_position`): use this for debris and orb spawn origin.

**Control Manifest Rules (Presentation layer — §3 GameFeel)**:
- Required: Subscribe via `IEventBus.Subscribe<T>`; same-frame dispatch for 同幀 steps; unscaled timer for cross-frame steps
- Required: Flash max-model (not additive): `flash_intensity = max(existing, event_flash_value)`
- Required: Flash fades on `unscaledDeltaTime` so it decays at normal speed during hitstop and slow-mo
- Required: Material orb spawn origin from `PartBroke.world_position` payload (no back-query)
- Forbidden: MUST NOT change game/part state (pure presentation)
- Forbidden: MUST NOT trigger additional independent hitstop from surviving-part detonations on boss death (they are visual-only)

---

## Acceptance Criteria

*From GDD `design/gdd/game-feel.md §D.4, §C.4 (BROKEN), §C.6, §I.5, §E.1, §E.2`, scoped to this story:*

- [ ] `PartBroke` triggers D.4 sequence **in order** (all same-frame unless noted):
  1. `sfxBreak(scale = part.w / 42)` — same frame
  2. `flash.Trigger(0.92f)` — same frame
  3. `shake.Trigger(ShakeMagPartBreakBase + ShakeMagPartBreakEscalation × brokenCount)` — same frame
  4. `debris.Spawn(22 + floor(part.w/42) main shards + 5 black smoke)` — same frame
  5. `HitstopSystem.Start(hitstop_part_break_ms)` → `Time.timeScale = 0` — same frame
  6. [115ms unscaled later] → `SlowMoSystem.TriggerPartBreak()` → `Time.timeScale = 0.12`
  7. [during slow-mo] → `MaterialOrbSystem.SpawnHoming(world_position, 4–7 orbs)` — after hitstop
- [ ] `BossCoreBreak` triggers: 220ms hitstop, timeScale 0.05, shake 24px, 110 gold-white particles from boss core, visual detonation of all surviving parts (no independent per-part hitstop from these), 4-note ascending arpeggio `sfxWin`
- [ ] Flash model (D.3): `flash_intensity = max(existing, event_value)` (not additive); decays `flash_intensity -= flash_decay_rate (2.6) × unscaledDeltaTime`; rendered as fullscreen canvas alpha = `flash_intensity × flash_max_alpha (0.85)`
- [ ] Flash fades below 20% alpha within 0.4s of Boss death event (I.5 readability guardrail): `1.0 × 0.85 / 2.6 ≈ 0.33s` — passes with margin
- [ ] Player 1px hitpoint marker remains visually identifiable in a screenshot taken at peak flash (flash_max_alpha = 0.85 means 85% white overlay maximum)
- [ ] Debris particles: main shards — 50% part-original color, 25% #fff1c0, 25% #ff8a4a; velocity 50–220 px/s with upward initial component (-40 vy), gravity 160; black smoke — 4×4px, `#2a1a22`, high gravity, fast fall
- [ ] Material homing orbs: 4–7 orbs spawn at `PartBroke.world_position`; initial random burst (45–100 px/s); after ~0.3s switch to parabolic home toward screen counter (right-top); color `#62F0D8`; on arrival: counter bounce animation + "+N material" float text
- [ ] E.1 (consecutive same-frame breaks): second `PartBroke` resets hitstop timer (not adds); slow-mo max-not-add; two SFX play simultaneously; flash = max; shake = max
- [ ] E.2 (boss death + part break same frame): boss parameters override — 220ms hitstop, 0.05 timescale, 24px shake; surviving parts detonate visually; no additional independent hitstop from those detonations
- [ ] Integration test or documented playtest: `Assets/_Project/Tests/GameFeel/GameFeel_BreakPayoff_Test.cs`

---

## Implementation Notes

*Derived from ADR-0002 Decision (synchronous dispatch + DI), ADR-0003 Decision (SO config), GDD D.4, D.3, §E.1, §E.2:*

1. Create `BreakPayoffHandler` in `KaijuBreaker.GameFeel` assembly. Constructor: `IEventBus bus`, `GameFeelConfig config`, `HitstopSystem hitstop`, `SlowMoSystem slowMo`, `ShakeSystem shake`, `FlashSystem flash`, `IParticlePool particlePool`, `IMaterialOrbSystem orbSystem`, `IAudioSystem audio`.
2. Subscribe: `PartBroke` → `OnPartBroke(PartBroke evt)`, `BossCoreBreak` → `OnBossCoreBreak(BossCoreBreak evt)`.
3. **`FlashSystem`** (owns the D.3 model — also instantiated here or as a standalone):
   - `Trigger(float value)`: `_flashIntensity = Mathf.Max(_flashIntensity, value)`.
   - Per-frame: `_flashIntensity = Mathf.Max(0f, _flashIntensity - config.FlashDecayRate * Time.unscaledDeltaTime)`.
   - Render: fullscreen canvas `Image.color = new Color(1,1,1, _flashIntensity * config.FlashMaxAlpha)`.
   - Respects `config.FlashAccessibilityMult`: `effectiveIntensity = value * config.FlashAccessibilityMult` (Story 007 sets the field).
4. **`OnPartBroke`**:
   - `audio.PlayScaled(sfxBreak, Mathf.Max(1f, evt.PartWidth / 42f))` (unscaled, fires before hitstop)
   - `flash.Trigger(0.92f)`
   - `shake.TriggerShake(config.ShakeMagPartBreakBase + config.ShakeMagPartBreakEscalation * _brokenCount)`
   - `particlePool.SpawnDebris(evt.world_position, evt.PartWidth, evt.PartOriginalColor)`
   - `hitstop.Start(config.HitstopPartBreakMs)` — sets `Time.timeScale = 0`
   - Start unscaled coroutine: after `HitstopPartBreakMs` ms → `slowMo.TriggerPartBreak()` → `orbSystem.SpawnHoming(evt.world_position, 4, 7)`
   - Increment `_brokenCount`.
5. **`OnBossCoreBreak`**:
   - `audio.Play(sfxWin)` (4-note arpeggio)
   - `flash.Trigger(1.0f)` (full white; capped by flash_max_alpha = 0.85)
   - `shake.TriggerShake(config.ShakeMagBossDeath)` (24px)
   - `particlePool.SpawnBossDeathVFX(evt.world_position, 110)` (110 gold-white particles)
   - `vfxOrchestrator.DetonateAllSurvivingParts()` (visual only — no events fired from this)
   - `hitstop.Start(config.HitstopBossDeathMs)` (220ms)
   - After 220ms: `slowMo.TriggerBossDeath()`
   - No material orbs for boss death (boss death triggers win sequence, not material collection).
6. **E.1 / E.2 priority**: `BreakPayoffHandler` defers to `HitstopSystem`'s internal priority logic (boss overrides part-break timer, already handled in Story 002). This handler just calls the trigger methods.
7. `_brokenCount` tracks number of `PartBroke` events received this session (for shake escalation).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: `HitstopSystem` timer logic and `Time.timeScale = 0` behavior
- Story 003: `SlowMoSystem` ramp math and hold duration
- Story 004: `ShakeSystem` max-model, decay, cap
- Story 005: SOFTENED and STAGGERED visual signatures
- Story 007: `FlashAccessibilityMult = 0.0` under reduce-motion (field read here; Story 007 sets it)
- Economy system (S3): material count calculation on orb arrival — orb arrival calls the Economy system; this story only spawns and animates the orbs
- Win sequence (Stage/RunController): triggered AFTER boss death VFX completes, not by this handler

---

## QA Test Cases

*Written by qa-lead at story creation. Integration story — automated + manual verification.*

- **AC-1**: D.4 sequence ordering verification
  - Given: `BreakPayoffHandler` initialized with mock sub-systems that log call order; `PartBroke` published
  - When: Event processed synchronously
  - Then: Call log order: [1] sfxBreak → [2] flash.Trigger → [3] shake.Trigger → [4] debris.Spawn → [5] hitstop.Start; after 115ms unscaled: [6] slowMo.TriggerPartBreak → [7] orbSystem.SpawnHoming
  - Edge cases: verify steps 1–5 happen in same frame; steps 6–7 happen after 115ms unscaled delay

- **AC-2**: Flash max-model (not additive)
  - Given: `_flashIntensity = 0.92f` (from prior part break)
  - When: New event triggers `flash.Trigger(0.30f)`
  - Then: `_flashIntensity == 0.92f` (unchanged; max(0.92, 0.30) = 0.92)
  - Edge cases: new event flash > 0.92 (e.g., boss death flash 1.0) → `_flashIntensity = 1.0`

- **AC-3**: Flash fades below 20% alpha within 0.4s
  - Given: `_flashIntensity = 1.0f`, `flash_decay_rate = 2.6`, `flash_max_alpha = 0.85`; simulate 0.4s of unscaled time
  - When: 0.4s of `unscaledDeltaTime` ticks applied (25 frames at 60fps)
  - Then: `_flashIntensity = max(0, 1.0 - 2.6 × 0.4) = max(0, -0.04) = 0`; rendered alpha = 0 (fully faded)
  - Edge cases: `flash_decay_rate = 1.5` (safe min) → fades in `1.0/1.5 = 0.67s` (advisory, not hard failure unless > 0.4s for boss event)

- **AC-4**: E.1 consecutive same-frame part breaks
  - Given: `PartBroke` received; hitstop at 80ms remaining; second `PartBroke` arrives same frame
  - When: Handler processes both
  - Then: `hitstop._timer == 115ms` (reset); `shake._currentShake == max(firstShake, secondShake)`; two `audio.Play` calls logged; `_flashIntensity == max(0.92, 0.92) = 0.92`
  - Edge cases: three `PartBroke` in same frame (all reset same timer; last reset wins, still 115ms)

- **AC-5**: E.2 boss death overrides part break same frame
  - Given: `PartBroke` and `BossCoreBreak` published in same frame (as per kaiju-part-system.md E.6 ordering)
  - When: `OnPartBroke` then `OnBossCoreBreak` called synchronously (same frame)
  - Then: `hitstop._timer == 220ms` (boss override); `Time.timeScale` target = 0.05 (boss slowmo); `shake._currentShake == 24` (boss mag); `BossVFX.DetonateAllSurvivingParts` called; no extra per-part hitstop from detonations
  - Edge cases: `BossCoreBreak` arrives before `PartBroke` in same frame (event ordering per ADR-0002 — `KaijuParts` guarantees `on_part_break` before `on_boss_core_break`)

- **AC-6 (Visual/Feel manual)**: Player hitpoint pixel visible during flash
  - Setup: Enter stage; trigger part break; screenshot at peak flash (within first 2 frames)
  - Verify: 1px white player position marker is distinguishable from the white flash overlay in screenshot
  - Pass condition: QA reviewer can locate the player position in the screenshot despite white overlay (flash_max_alpha = 0.85 < 1.0 preserves faint game underneath)

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration: `Assets/_Project/Tests/GameFeel/GameFeel_BreakPayoff_Test.cs` (PlayMode integration) OR documented playtest report (BLOCKING)
- Visual/Feel (advisory): `production/qa/evidence/break-payoff-sequence-evidence.md` + lead sign-off

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (HitstopSystem DONE), Story 003 (SlowMoSystem DONE), Story 004 (ShakeSystem DONE)
- Unlocks: Story 007 (Reduce-Motion — must test against the full payoff pipeline)
