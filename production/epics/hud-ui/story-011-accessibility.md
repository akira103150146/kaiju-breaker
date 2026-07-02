# Story 011: Accessibility (Text Scale, Colorblind Mode, Reduce-Motion)

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: ~3–4h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-008`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Event Architecture (Primary); ADR-0006: UI Framework Selection (Secondary)
**ADR Decision Summary**: Reduce-Motion toggle broadcasts `ReduceMotionChanged` struct event via `IEventBus` (ADR-0002); `GameFeel` system subscribes and adjusts its knobs. Colorblind mode and text scale use `UIConfig` ScriptableObject with three color/sprite set variants (ADR-0006 §3 Consequences: "色盲模式需程式碼切換 UIConfig SO 三套設定"). All accessibility settings persist in `ISaveService` (ADR-0002 `ISaveService`). One-level main-menu access is a screen-flow requirement: Accessibility settings screen is pushed directly from the main menu root, not nested.

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: Text scale applies `TMP_Text.fontSize` multiplier (1.0/1.25/1.50) via `TextScaleManager` traversal of all `TMP_Text` components in active meta screens. [需查證: `TMP_Text.fontSize` scaling vs `TMP_Text.fontSizeMin/Max` interaction in Unity 6.3 — ensure scale does not interact with auto-size bounds; disable `TMP_Text.enableAutoSizing` on all scaled text elements before applying manual scale.] Colorblind sprite swaps use `UIConfig` SO fields (three `Sprite` arrays for default/blue-yellow/shape-first modes); no shader-level color transform is required for MVP.

**Control Manifest Rules (Presentation layer)**:
- Required: `ReduceMotionChanged` event broadcast via `IEventBus` (same frame as toggle); accessibility settings persisted via `ISaveService`; colorblind mode applied to entire `KaijuBreaker.UI` scope via `UIConfig` SO swap; text scale via `TextScaleManager.ApplyScale()` called on `OnShow()` of each meta screen; accessibility setting reachable from main menu in ≤1 action
- Forbidden: Hardcoding font sizes; shader color-transform as the sole colorblind cue (must have shape/rhythm backup cue per GDD §I.2); per-frame `TMP_Text.fontSize` writes; direct reference to `GameFeel` assembly (event-only coordination)
- Guardrail: Text at 150% must not truncate or overlap with adjacent elements; Reduce-Motion does not disable functional elements (HUD reload bar and charge bar are preserved per GDD §I.3); colorblind mode in `UIConfig` SO covers D-area ammo pips' low-blink cue (shape, not color — already compliant from Story 003)

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §I.1–I.5, M.8:*

- [ ] Text scale setting: three presets (100% / 125% / 150%); affects Meta Screen text (weapon card flavor, upgrade costs, difficulty descriptions, upgrade descriptions); does NOT affect HUD blood bars, weapon icons, pip sprites (those are scaled by global integer UI scale)
- [ ] Text at 150%: all Meta Screen text strings display fully without truncation or element overlap (visual verification in Loadout / Upgrade / Difficulty screens)
- [ ] Colorblind mode: three options (預設 / 藍黃對比 / 形狀優先); switching swaps `UIConfig.CurrentColorSet` and re-applies sprites/colors to all active screens
- [ ] When "形狀優先" mode active: SOFTENED part state perceivable via 2Hz pulse rhythm without color cue (manual 5-person test ≥70% per M.8 — ADVISORY)
- [ ] Reduce-Motion ON: orbit ball fly-in animation disabled (counter shows direct number jump); meta screen enter animations disabled (cards appear instantly); HUD reload bar and L3 charge bar animations **preserved** (functional, per GDD §I.3)
- [ ] Reduce-Motion toggle fires `ReduceMotionChanged(enabled: bool)` event via `IEventBus` on the **same frame** as the setting change (automated test)
- [ ] Game-feel system receives `ReduceMotionChanged` and adjusts — this coordination is verified by checking the event is published (game-feel subscription is out of scope for this story's tests)
- [ ] Accessibility settings (color-blind mode, Reduce-Motion, text scale) are reachable from the main menu root in ≤1 navigation action (push of AccessibilitySettingsScreen from main menu)
- [ ] All accessibility settings persist to `ISaveService` and are restored on next launch

---

## Implementation Notes

*Derived from ADR-0006 §3 Consequences and ADR-0002 event pattern:*

**`TextScaleManager : MonoBehaviour`** (singleton-like, but DI-injectable via interface):
- `ApplyScale(float multiplier)`: iterate all `TMP_Text` components in active Canvas hierarchy (use `GetComponentsInChildren<TMP_Text>()` on each meta screen root); set `fontSize = baseFontSize * multiplier`. Cache `baseFontSize` per `TMP_Text` on first call (before any multiplier applied).
- Call `ApplyScale()` in `OnShow()` of every meta screen (Loadout, Upgrade, Difficulty) after layout rebuild.
- `baseFontSize` cache: store as `[SerializeField] float _baseFontSizeCache` on `TMP_Text` wrapper or in a parallel dictionary. Do not call `ApplyScale` more than once per `OnShow` cycle.

**`ColorblindManager : MonoBehaviour`**:
- Three `ColorSet` structs in `UIConfig` SO: `Default`, `BlueYellow`, `ShapeFirst`
- `ApplyColorSet(ColorblindMode mode)`: `UIConfig.ActiveMode = mode`; broadcast `ColorblindModeChanged(mode)` event for any screen to refresh its sprites; refresh all currently active screens immediately.
- Each screen implements `IColorblindRefreshable.OnColorblindModeChanged()` — or screens subscribe to `ColorblindModeChanged` event in `OnEnable`.

**Reduce-Motion**:
- `AccessibilitySettings.ReduceMotion` property setter: `IEventBus.Publish(new ReduceMotionChanged { Enabled = value })` on the same frame.
- Subscribers in `MaterialCounterDisplay` (Story 005): on `ReduceMotionChanged(true)` → skip orbit-ball animation, call `UpdateCountDirect()`. On `ReduceMotionChanged(false)` → restore animation.
- Screen enter animations (card slide-ins etc., if added by art): subscribe to `ReduceMotionChanged` and set `Animator.enabled = !enabled` or skip the coroutine.
- HUD reload bar and L3 charge bar: do NOT subscribe — always animate (GDD §I.3 explicit carve-out).

**Accessibility Settings screen** (`AccessibilitySettingsScreen : MonoBehaviour, IScreen`):
- Pushed from main menu root via `UIScreenManager.Push(accessibilityScreen)` — single action.
- Contains: text-scale toggle (3 buttons), colorblind mode toggle (3 buttons), Reduce-Motion toggle (on/off).
- `OnShow()`: read current settings from `ISaveService`; set button states.
- On any toggle change: update setting, persist via `ISaveService`, apply immediately.

**Persistence**: `ISaveService.SetAccessibilitySettings(TextScale, ColorblindMode, ReduceMotion)` on each change.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 003**: Ammo pip low-blink is already shape-based (toggle `enabled`) — colorblind-safe by design from Story 003 implementation; this story only verifies the end-to-end colorblind mode switch does not break that behaviour
- **Story 010**: Safe-area layout — text at 150% must fit within safe area; tested here, but safe area fitting is implemented in Story 010
- game-feel system subscription to `ReduceMotionChanged` — game-feel's responsibility; this story only guarantees the event is published on the same frame as the toggle
- Voice subtitles — GDD §I.4 explicitly defers this to post-MVP

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **AC-1**: Text scale 150% no truncation (manual check)
  - Setup: Set text scale to 150%; open Loadout, Upgrade, and Difficulty screens in sequence
  - Verify: All weapon card flavor text, upgrade cost labels, difficulty description text fully visible; no strings cut off at element boundary; no element overlap
  - Pass condition: All visible text strings complete at 150% scale; no layout clipping in 1080×1920 portrait and 1920×1080 landscape

- **AC-2**: Colorblind mode swaps color set
  - Setup: Enable "藍黃對比" colorblind mode; open Loadout screen
  - Verify: All UI colour-dependent elements show blue-yellow variant (per `UIConfig.BlueYellow` set); ✗/✓ icons still use shape cue (red/green colour change is supplementary, not sole cue)
  - Pass condition: No element relies on colour alone; shape/icon cue present for all functional states; mode persists after app restart

- **AC-3**: Reduce-Motion disables orbit ball animation
  - Setup: Enable Reduce-Motion; trigger material pickup during combat
  - Verify: Orbit ball fly-in animation absent; material counter number jumps directly to new value without animated trajectory
  - Pass condition: Zero animated GameObject path visible from part to counter; counter updates instantaneously

- **AC-4**: Reduce-Motion preserves reload bar and L3 charge bar
  - Setup: Enable Reduce-Motion; equip M3 (3-pip); fire all ammo; watch reload bar; equip L3; hold L3 trigger
  - Verify: Reload bar animates normally (fills over reload time); L3 charge bar fills over 1.5s
  - Pass condition: Both functional animations present despite Reduce-Motion being ON

- **AC-5**: ReduceMotionChanged event fires same frame as toggle (automated test)
  - Given: `AccessibilitySettings.ReduceMotion = false`; `IEventBus` spy capturing published events
  - When: `AccessibilitySettings.ReduceMotion = true` (setter invoked)
  - Then: `IEventBus` spy received `ReduceMotionChanged { Enabled = true }` within the same synchronous call stack; event count == 1
  - Edge cases: Toggle false → true → false → two events total (true then false); no event if value unchanged

- **AC-6**: Accessibility setting one-level reachable from main menu
  - Setup: Start at main menu root screen
  - Verify: A single tap/D-Pad-confirm on accessibility option pushes AccessibilitySettingsScreen without intermediate screens
  - Pass condition: Accessibility screen visible within 1 user action from main menu; Back button returns to main menu root

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration (BLOCKING): `Assets/_Project/Tests/UI/reduce_motion_event_test.cs` — must exist and pass; covers ReduceMotionChanged same-frame event publication (AC-5)
- UI (ADVISORY): `production/qa/evidence/accessibility-evidence.md` — screenshots of 150% text scale in all 3 meta screens; colorblind mode visual verification; manual walkthrough doc for one-level access (AC-6); 5-person SOFTENED rhythm perception test result (if run, per M.8 ≥70% threshold)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 005 DONE (`MaterialCounterDisplay` that subscribes to `ReduceMotionChanged`); Story 007 DONE (Loadout screen text elements to scale); Story 008 DONE (Upgrade screen text elements to scale); Story 009 DONE (Difficulty screen text elements); `ReduceMotionChanged` event struct in Core; `ISaveService` with `SetAccessibilitySettings()`/`GetAccessibilitySettings()` in Core; `UIConfig` SO with three `ColorSet` variants (art director provides color values)
- Unlocks: Epic DoD — this is the final story; all M.1–M.8 acceptance criteria are now addressable
