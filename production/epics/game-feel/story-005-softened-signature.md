# Story 005: SOFTENED 簽章與 STAGGERED 回饋 (SOFTENED Signature & STAGGERED Feedback)

> **Epic**: 打擊感（VFX / SFX / Game Feel）
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: L (4–6h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/game-feel.md`
**Requirement**: `TR-gamefeel-001` (primary — SOFTENED perception ≤0.5s, P0 blocker), `TR-gamefeel-005` (partial — SOFTENED glow z-order below bullet layer)
*(TR-IDs derived from `game-feel.md §I` — not yet formally registered in `docs/architecture/tr-registry.yaml`; see EPIC.md note)*

**ADR Governing Implementation**:
- Primary: ADR-0002: 事件架構 — subscribe `PartSoftened`, `PartSoftenedExit`, `PartStaggered`, `PartStaggerEnd` via `IEventBus`
- Secondary: ADR-0003: 資料驅動調校 — all G.4 SOFTENED knobs from `GameFeelConfig` SO; per-event flash and shake values referenced but those systems are delegated (not owned here)

**ADR Decision Summary**: `SoftenedSignatureSystem` subscribes to typed event structs from `Core.IEventBus`; same-frame synchronous dispatch guarantees the visual onset happens the same frame as the event. All knobs (`SoftenedColorHue`, `SoftenedPulseFrequencyHz`, `SoftenedGlowRadiusPct`, `SoftenedSfxMaxPerFrame`) from injected `GameFeelConfig`. No direct references to `KaijuParts` or `Weapons`.

**Engine**: Unity 6.3 LTS | **Risk**: MEDIUM
**Engine Notes**:
- SOFTENED color shift: apply a `MaterialPropertyBlock` tint to the part's renderer. Property blocks avoid material duplication (critical for draw-call budget ≤200). Do NOT create a new material instance per part.
- Glow ring: pulsing at `SoftenedPulseFrequencyHz = 2Hz` using `Mathf.Sin(Time.unscaledTime * 2π * config.SoftenedPulseFrequencyHz)`. If using a shader `_Time` uniform, verify post-4.4 shader texture type changes do not break the glow pass `[需查證 6.3 API]`.
- Glow z-order: MUST render below the bullet layer. In URP 2D, control via Sorting Layer + Order in Layer on the glow renderer. Do not assume sorting layer names — check `docs/engine-reference/unity/VERSION.md` for URP 2D sorting constraints.
- `AudioSource.PlayOneShot(sfxSoften)` plays on unscaled time (unaffected by `Time.timeScale`). Verify in Unity 6.3.
- Per-frame SFX cap (`SoftenedSfxMaxPerFrame = 2`): use a frame counter reset in `LateUpdate` (or via a per-frame scheduler) — multiple `PartSoftened` events in the same frame share the budget.
- `on_part_softened_exit` (0.2s fade): animate via `Mathf.Lerp` on `Time.unscaledDeltaTime` — safe during slow-mo.

**Control Manifest Rules (Presentation layer — §3 GameFeel)**:
- Required: Subscribe via `IEventBus.Subscribe<T>`; same-frame visual onset
- Required: SOFTENED glow MUST render on a Sorting Layer below enemy bullet layer (readability guardrail)
- Required: All knobs from `GameFeelConfig`
- Forbidden: MUST NOT change game/part state (pure visual consumer)
- Forbidden: Color shift MUST NOT obscure bullet outlines — glow radius stays within `SoftenedGlowRadiusPct` of part width

---

## Acceptance Criteria

*From GDD `design/gdd/game-feel.md §I.1, §I.5 (partial), §C.3, §C.4`, scoped to this story:*

- [ ] `PartSoftened` received: color shift to `SoftenedColorHue` (#FF6600) + 2Hz sine glow ring appears within ≤ `SoftenedVisualOnsetMaxS` (0.5s) — **P0 Alpha blocker per I.1**
- [ ] `sfxSoften` plays on the exact same frame as `PartSoftened` event (no cross-frame delay)
- [ ] Per-frame SFX cap: if > `SoftenedSfxMaxPerFrame` (2) parts soften in the same frame, only 2 SFX calls fire; all glow rings activate for every part regardless
- [ ] Glow ring pulse: sine wave at `SoftenedPulseFrequencyHz` (2Hz) with color transition from #FF6600 (dark) to #FFCC00 (peak); outer radius = `SoftenedGlowRadiusPct` (25%) of part pixel width; pulse continues until `PartSoftenedExit` or `PartBroke`
- [ ] Glow ring rendered BELOW enemy bullet sorting layer — bullet outlines not obscured by glow at any density (TR-gamefeel-005 partial)
- [ ] `PartSoftenedExit` received: color shift fades out over 0.2s (unscaled time); glow ring disappears; brief descending SFX plays (sfxSoften reverse or fade-out variant)
- [ ] `PartStaggered` with `armor_stripped = true`: 14 blue-gray debris particles spawn at part world position; `sfxArmorStrip` plays; 2px white weakness frame appears on part edge; weakness frame maintained until `PartStaggerEnd`
- [ ] `PartStaggered` with `armor_stripped = false`: white flash (intensity 0.45, handled by flash system), 3 cold-blue sparks spawn; no debris
- [ ] `PartStaggerEnd` with `armor_restored = true`: weakness frame disappears; armor restore animation (shards slow-drift back, 0.3s on unscaled clock)
- [ ] Recognition test (advisory): ≥ 80% of 5 testers identify all SOFTENED parts in screenshots at D1–D4 bullet density (I.1 user test — gate for Alpha milestone)
- [ ] SOFTENED glow remains perceivable at maximum bullet density (Nightmare D4) — glow visible in bullet-gap intervals

---

## Implementation Notes

*Derived from ADR-0002 Decision (event bus + DI), ADR-0003 Decision (SO config), GDD §C.3, §C.4, §E.3, §E.4:*

1. Create `SoftenedSignatureSystem` in `KaijuBreaker.GameFeel` assembly. Constructor: `IEventBus bus`, `GameFeelConfig config`, `IParticlePool particlePool`, `IAudioSystem audio`.
2. Subscribe: `PartSoftened`, `PartSoftenedExit`, `PartStaggered`, `PartStaggerEnd`.
3. **SOFTENED onset** (on `PartSoftened`):
   - Apply `MaterialPropertyBlock` color tint to the part renderer: `_Color = config.SoftenedColorHue` (orange overlay blend).
   - Enable glow ring renderer (pre-instantiated overlay sprite on the part's sorting layer - 1). Set shader glow radius to `config.SoftenedGlowRadiusPct × part.PixelWidth`.
   - Start `_pulseTimer` driving `sin(unscaledTime × 2π × config.SoftenedPulseFrequencyHz)` to animate glow alpha between #FF6600 and #FFCC00.
   - SFX: increment `_softenSfxThisFrame`; if `_softenSfxThisFrame <= config.SoftenedSfxMaxPerFrame`, call `audio.PlayOneShot(sfxSoften)`.
4. **SFX frame counter reset**: decrement `_softenSfxThisFrame = 0` in `LateUpdate` (or a scheduled post-event-dispatch reset).
5. **SOFTENED exit** (on `PartSoftenedExit`): tween `MaterialPropertyBlock` tint from #FF6600 to neutral over 0.2s (unscaled); disable glow renderer; play descending SFX.
6. **STAGGERED armor strip** (on `PartStaggered` where `armor_stripped = true`): spawn 14 blue-gray particles from `IParticlePool`; play `sfxArmorStrip`; enable 2px white weakness frame overlay on the part (separate sorted sprite).
7. **STAGGERED simple** (on `PartStaggered` where `armor_stripped = false`): call `FlashSystem.Trigger(0.45f)` (owned by Story 006); spawn 3 cold-blue sparks from pool.
8. **STAGGER end** (on `PartStaggerEnd` where `armor_restored`): disable weakness frame; play armor-restore particle animation (shards return, 0.3s unscaled lerp).
9. Edge case E.3: when SOFTENED starts during existing flash (0.92), the SFX still fires — do not suppress SFX based on other state.
10. Edge case E.4: per-frame cap `_softenSfxThisFrame` resets each frame; never suppress glow ring activation.
11. `IParticlePool` and `IAudioSystem` are injected abstractions — no `FindObjectOfType`, no static calls.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `GameFeelConfig` SO with G.4 knob definitions
- Story 004: Shake magnitude for SOFTENED event (ShakeSystem.TriggerShake called separately by BreakPayoffHandler or event subscription in ShakeSystem itself)
- Story 006: Full white flash system (D.3 model), break debris, material orbs — BROKEN event handled there
- Story 007: Reduce-motion modifiers (flash and glow behavior under reduce-motion)
- Flash for `armor_stripped = false` stagger (intensity 0.45): `FlashSystem.Trigger` is called here but the flash system itself is implemented in Story 006

---

## QA Test Cases

*Written by qa-lead at story creation. Visual/Feel story — manual verification steps.*

- **AC-1**: SOFTENED visual onset ≤ 0.5s (P0 blocker)
  - Setup: Load a test stage with a single kaiju part; use debug tool or test harness to publish `PartSoftened` event for a specific part; enable frame-timestamp overlay or use Unity's frame debugger
  - Verify: On the same frame the event is published, the part's color shifts toward #FF6600 and the glow ring activates (or at most 1 frame later, rendering pipeline latency ≤ 16ms << 500ms threshold)
  - Pass condition: Color shift + glow ring visible within 1 frame of event; total onset ≤ 0.5s with no perceptible delay in normal gameplay

- **AC-2**: SFX same-frame playback
  - Setup: Enable Unity Audio debug log / frame number overlay; trigger `PartSoftened` via debug
  - Verify: Console shows `sfxSoften` play call on the same frame number as `PartSoftened` event
  - Pass condition: Same frame number logged for event and SFX; zero cross-frame delay

- **AC-3**: Per-frame SFX cap (3 parts soften simultaneously)
  - Setup: Debug-publish 3 `PartSoftened` events in a single frame
  - Verify: Exactly 2 SFX calls are issued; 3 glow rings activate (all 3 parts get visuals)
  - Pass condition: Audio log shows 2 `PlayOneShot` calls; all 3 parts have orange tint + glow

- **AC-4**: Glow z-order below bullets
  - Setup: Enter Nightmare difficulty (maximum bullet density); trigger SOFTENED on a part; take screenshot at peak bullet density
  - Verify: In screenshot, at least one bullet's outline is clearly visible overlapping a glow ring region — bullets not hidden behind glow
  - Pass condition: No bullet is fully occluded by the glow ring in any screenshot reviewed

- **AC-5**: STAGGERED armor strip debris + weakness frame
  - Setup: Trigger `PartStaggered` with `armor_stripped = true` via debug
  - Verify: 14 blue-gray debris particles appear; `sfxArmorStrip` plays; 2px white outline appears on part edges
  - Pass condition: All three effects present simultaneously; weakness frame persists; no debris from `armor_stripped = false` STAGGERED

- **AC-6**: 5-person recognition test for SOFTENED (I.1 advisory gate)
  - Setup: Prepare 5 static screenshots: one per difficulty D1–D4, plus one mid-break; SOFTENED parts present at varied positions; create questionnaire asking testers to circle SOFTENED parts
  - Verify: Run test with 5 participants unfamiliar with the game
  - Pass condition: ≥ 4 out of 5 testers identify all SOFTENED parts correctly in all screenshots (80% threshold per GDD I.1)

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- Visual/Feel: `production/qa/evidence/softened-signature-evidence.md` + lead sign-off (ADVISORY, but gate for Alpha milestone per I.1)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (GameFeelConfig SO — must be DONE)
- Unlocks: Story 006 (Break Payoff — SOFTENED visual state context useful for full payoff testing)
