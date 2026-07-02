# Story 006: Softened/Broken Readability Hooks

> **Epic**: 可破壞部位系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju-part-system.md`
**Requirement**: `TR-part-002`
*(TR-ID derived from GDD §H.2 — registry not yet formalised)*

**ADR Governing Implementation**: ADR-0002: 事件架構 (primary)
**ADR Decision Summary**: `GameFeel` (S8) subscribes to `PartSoftened`, `PartSoftenedExit`, `PartStaggered`, `PartStaggerEnd`, and `PartBroke` events via `IEventBus.Subscribe<T>` — no direct reference to `KaijuParts` assembly. All readability effects (orange-red pulsing glow, ARMOR_STRIPPED weakness indicator, break explosion) are driven purely by these events. KaijuParts fires events synchronously (same-frame); visual onset latency is bounded by the VFX / animator startup time after event receipt. The ≤ 0.5s visual onset gate (H.2) is the acceptance blocker for this story.

**Engine**: Unity 6 | **Risk**: LOW
**Engine Notes**: `GameFeel` acts in the same frame as event publication (synchronous dispatch, ADR-0002). Visual onset includes one render frame (~16.6 ms at 60 fps) plus VFX/animator warm-up. The ≤ 0.5s gate is a human-perceptible upper bound, not a frame-budget constraint. SOFTENED glow uses `softened_pulse_frequency_hz` via a shader or animation curve — confirm the VFX approach with technical artist before implementation. No post-cutoff APIs expected; verify Unity 6 `ParticleSystem` API if used.

**Control Manifest Rules (this layer — Presentation / GameFeel)**:
- Required: `GameFeel` subscribes via `IEventBus.Subscribe<T>`; MUST NOT reference `KaijuParts` assembly directly (§1.4, §2)
- Required: `GameFeel` MUST NOT modify any part state (`H_current`, `B_current`, `heat_state`, `armor_state`, `break_state`) — pure presentation consumer (§3 GameFeel manifest)
- Required: all visual knobs (`softened_visual_onset_max_s`, `softened_pulse_frequency_hz`, `softened_color_hue`, `stagger_visual_onset_max_s`) from `GameFeelConfig` SO; no hardcoded values (§1.2)
- Required: `PartSoftened.CurrentHeat` and `PartSoftened.HMax` (from Story 002 payload) available for proportional VFX intensity if needed
- Required: KaijuParts OWNS all events that feed readability hooks; event payloads are fully defined in Stories 002–005 — this story only implements the consumer side
- Forbidden: `GameFeel` polling `IPartStateQuery` per-frame for heat values (§4.3 manifest — use events, not polling); Forbidden: hardcoded #FF6600 or 2.0 Hz in code

---

## Acceptance Criteria

*From GDD `design/gdd/kaiju-part-system.md` §H.2, §F.3, §G.3, §C.2 BROKEN visual, scoped to this story:*

- [ ] H.2 — SOFTENED visual onset: colour-shift + pulsing orange-red glow appears on-screen ≤ `softened_visual_onset_max_s` (0.5s) after `PartSoftened` published; verified by timed playtest observation
- [ ] H.2 — Recognition rate: ≥ 80% of 5 untrained testers correctly identify all SOFTENED parts across 10 static gameplay screenshots with varying bullet densities (D1–D4)
- [ ] H.2 — At D4 bullet density, SOFTENED visual indicator is not occluded > 50% of elapsed screen time by enemy bullets
- [ ] H.2 — `PartSoftenedExit` causes the SOFTENED glow to disappear within one rendered frame of event receipt (synchronous same-frame dispatch ensures no delay beyond render)
- [ ] F.3 / G.3 — `PartStaggered(ArmorStripped=true)` causes the ARMORED-part weakness-indicator frame to appear within `stagger_visual_onset_max_s` (0.3s) from event receipt
- [ ] C.2 BROKEN visual — `PartBroke` receipt causes: (a) the part's visual switches to broken/destroyed state (destroyed mesh, scorch VFX, or animation-state transition); (b) subsequent missile hits the dead zone play null / pass-through SFX (audio concern — placeholder empty clip) and show no B_current change in debug overlay
- [ ] G.3 / ADR-0003 — All `GameFeelConfig` knobs used by this story (`softened_visual_onset_max_s`, `softened_pulse_frequency_hz`, `softened_color_hue`, `stagger_visual_onset_max_s`, `hitbox_size_multiplier_normal/armored/core`) are sourced from `GameFeelConfig` SO; no hardcoded values in `GameFeel` code

---

## Implementation Notes

*Derived from ADR-0002 (event subscription pattern) and ADR-0003 (SO config):*

- `GameFeelSystem` subscribes in constructor / `OnEnable`:
  ```csharp
  _bus.Subscribe<PartSoftened>(OnPartSoftened);
  _bus.Subscribe<PartSoftenedExit>(OnPartSoftenedExit);
  _bus.Subscribe<PartStaggered>(OnPartStaggered);
  _bus.Subscribe<PartStaggerEnd>(OnPartStaggerEnd);
  _bus.Subscribe<PartBroke>(OnPartBroke);
  ```
- `OnPartSoftened(in PartSoftened evt)`: look up part's visual GameObject by `PartId`; enable SOFTENED shader parameter (orange-red hue, pulse animation at `_config.SoftenedPulseFrequencyHz`); optionally scale intensity by `evt.CurrentHeat / evt.HMax`.
- `OnPartSoftenedExit(in PartSoftenedExit evt)`: disable SOFTENED shader parameter on part visual.
- `OnPartStaggered(in PartStaggered evt)`: if `evt.ArmorStripped`, activate the weakness-indicator overlay on the ARMORED part visual.
- `OnPartStaggerEnd(in PartStaggerEnd evt)`: if `evt.ArmorRestored`, deactivate the weakness-indicator overlay.
- `OnPartBroke(in PartBroke evt)`: trigger break VFX at `evt.WorldPosition`; switch part's animator state to "Broken"; deactivate hitbox (if not already deactivated by Weapons system).
- `GameFeelConfig` SO fields for this story: `SoftenedVisualOnsetMaxSeconds` (default 0.5), `SoftenedPulseFrequencyHz` (2.0), `SoftenedColorHue` (#FF6600), `StaggerVisualOnsetMaxSeconds` (0.3). All with `OnValidate` safe-range checks.
- SOFTENED timing measurement for CI: record `Time.time` when `PartSoftened` received; a PlayMode integration test can assert that the first visible VFX frame is within `SoftenedVisualOnsetMaxSeconds`. For manual verification, a debug overlay can display "time since PartSoftened" on the part.
- BROKEN hitbox deactivation: Weapons system clears its collision body on `PartBroke`; `GameFeel` handles the visual side. Coordinate with Weapons epic to avoid double-deactivation.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: `PartSoftened` / `PartSoftenedExit` emission (already live)
- Story 003: `PartStaggered` / `PartStaggerEnd` emission (already live)
- Story 004: `PartBroke` emission, H_current/B_current reset (already live); Weapons-side hitbox deactivation on `PartBroke`
- Story 005: Adjacency / chain effects; `PartSoftened` from L2 heat ripple is produced by Story 005 path but consumed identically here
- Audio system: SFX cues for SOFTENED, STAGGERED, BROKEN — separate audio epic
- User-study recruitment and facilitation — QA / design team responsibility; this story prepares evidence artefact

---

## QA Test Cases

*Visual/Feel story — manual verification steps:*

- **AC-1**: SOFTENED visual onset ≤ 0.5s
  - Setup: Start a kaiju encounter; fire laser continuously at a NORMAL part until `PartSoftened` fires (H_current reaches theta_S = 100 HU); a debug overlay shows "SOFTENED event received" timestamp
  - Verify: Orange-red colour shift + pulsing glow appears on the part's visual within 0.5 seconds of the debug timestamp
  - Pass condition: Lead designer or QA tester observes glow onset ≤ 0.5s (use slow-motion playback at 0.2× timescale or stopwatch); glow pulses at approximately 2 Hz

- **AC-2**: SOFTENED recognition — 5-person user test
  - Setup: Prepare 10 static gameplay screenshots captured across D1–D4 difficulties; each contains ≥ 1 SOFTENED part and ≥ 1 INTACT part in the same frame; vary enemy bullet density per screenshot
  - Verify: Each of 5 untrained testers views all 10 screenshots and marks which parts they believe are SOFTENED; record results in evidence document
  - Pass condition: Aggregate recognition rate ≥ 80% (i.e., ≥ 80% of total {tester × screenshot} identifications are correct); if < 80%, escalate visual intensity — this criterion is a hard blocker for Alpha milestone (GDD §H.2)

- **AC-3**: SOFTENED indicator visible at D4 bullet density
  - Setup: Force difficulty to D4; enter a boss fight; ensure one part is in SOFTENED state; record 10 seconds of gameplay video at the highest enemy bullet density
  - Verify: Review video frame-by-frame; count frames where the SOFTENED indicator is ≥ 50% occluded by enemy bullets
  - Pass condition: Occluded frame count is < 50% of total recorded frames (indicator visible ≥ 50% of screen time); if failing, adjust rendering layer priority or indicator size

- **AC-4**: `PartSoftenedExit` removes glow within one rendered frame
  - Setup: Part in SOFTENED state; stop laser fire; let H_current decay below theta_S_exit (80 HU); `PartSoftenedExit` fires
  - Verify: Review slow-motion playback or frame-step immediately after event; confirm glow is absent in the next rendered frame
  - Pass condition: No SOFTENED glow visible in the frame following `PartSoftenedExit` receipt; no residual fade that lasts multiple frames (unless a deliberate fast-fade ≤ 2 frames is acceptable — confirm with designer)

- **AC-5**: ARMORED weakness indicator appears within 0.3s of `PartStaggered(ArmorStripped=true)`
  - Setup: ARMORED part; fire L3 wave; `PartStaggered(ArmorStripped=true)` fires; debug overlay shows event timestamp
  - Verify: Weakness-indicator frame (e.g., yellow border or glowing outline) appears on the ARMORED part visual within 0.3s of event timestamp
  - Pass condition: Lead sign-off that indicator appeared within 0.3s; confirm indicator disappears when `PartStaggerEnd(ArmorRestored=true)` fires

- **AC-6**: BROKEN visual — correct state switch and dead-zone behaviour
  - Setup: Break any part (bring B_current to threshold); observe the frame after `PartBroke`
  - Verify: (a) Part visual switches to broken/destroyed state (missing-limb mesh, scorch particle effect, or equivalent animation state); (b) aim missiles at the destroyed part's location; confirm B_current debug overlay shows no change and a pass-through / null sound plays
  - Pass condition: Lead sign-off on both (a) and (b); no B_current change detectable in debug overlay for 3 consecutive missile hits to the dead zone

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/qa/evidence/kaijuparts-softened-readability-evidence.md` — must contain: 10-screenshot set paths, 5-tester result table, recognition-rate calculation, and lead designer sign-off

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (`PartSoftened` / `PartSoftenedExit` events live), Story 003 (`PartStaggered` / `PartStaggerEnd` events live), Story 004 (`PartBroke` event live), Story 005 (adjacency/chain fully operational) — all must be DONE for end-to-end visual verification
- Unlocks: None — final story in this epic
