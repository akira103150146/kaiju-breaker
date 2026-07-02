# Story 010: Cross-Platform Safe Areas, Thumb Zones & Pixel-Perfect Scaling

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: ~3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-007`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0006: UI Framework Selection (Primary)
**ADR Decision Summary**: `SafeAreaFitter : MonoBehaviour` applies `Screen.safeArea` to `HUD_Static` and `HUD_Dynamic` root `RectTransform`s (ADR-0006 §2 Safe Area paragraph). `Canvas.pixelPerfect = true` on all in-combat and meta Canvases, referencing `PixelPerfectCamera`, ensures integer-multiple scaling (×1/×2/×3/×4) — no non-integer scale permitted (ADR-0006 §1, §2). `mobile_bottom_ui_height_pct` (0.20) and top (0.15) are read from `HudConfig` SO to position C/D and A/B areas within mobile portrait thumb zones.

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: `Screen.safeArea` returns a `Rect` in screen pixels — convert to anchor values by dividing by `Screen.width`/`Screen.height` and applying to `RectTransform.anchorMin`/`anchorMax`. [需查證 #1 in ADR-0006: confirm `Canvas.pixelPerfect` with `PixelPerfectCamera` reference in URP `Screen Space – Camera` mode works correctly in Unity 6.3 — file spike result in `docs/architecture/tech-spikes/`.] 44dp touch target: convert dp to pixels at runtime via `Screen.dpi` (`pixels = dp * Screen.dpi / 160f`); [需查證: `Screen.dpi` accuracy on target iOS/Android devices].

**Control Manifest Rules (Presentation layer)**:
- Required: `SafeAreaFitter` applied to `HUD_Static` root, `HUD_Dynamic` root, and each meta screen Canvas root `RectTransform`; `mobile_bottom_ui_height_pct` and top percentage from `HudConfig` SO; touch target ≥ 44dp enforced via `LayoutElement.minWidth/minHeight` or explicit `RectTransform.sizeDelta` — not hardcoded px
- Forbidden: Non-integer UI scale on any Canvas (enforced by `Canvas.pixelPerfect`); fixed pixel anchors that bypass `SafeAreaFitter`; per-frame `Screen.safeArea` polling (read once on `Start()` and on orientation change)
- Guardrail: Middle 65% of mobile portrait play area must have zero fixed HUD elements; world-space part bars are exempt from this rule (they are world-space, not screen-space); integer-scale assertion in Editor validation script (CI-enforced)

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §H.1, H.2, H.3, M.7:*

- [ ] Mobile portrait: all fixed HUD elements (A/B top, C/D bottom) are fully within the device's `Screen.safeArea`; notch and rounded corners do not clip any UI element
- [ ] Mobile portrait: top UI strip occupies ≤15% of screen height (`mobile_top_ui_height_pct` derived from layout); bottom UI strip occupies `mobile_bottom_ui_height_pct` (0.20); middle 65% free of fixed elements
- [ ] Mobile portrait: secondary weapon button (D area) touch target rectangle ≥ 44×44 dp (`Screen.dpi`-based conversion), verified via Inspector or Accessibility Inspector on device
- [ ] PC 4K (3840×2160): all UI Sprites render at integer-multiple scale (×4 from 960×540 base); pixel edges are sharp — zero sub-pixel blur in screenshot
- [ ] `SafeAreaFitter` applied to `HUD_Static` root, `HUD_Dynamic` root, Loadout screen Canvas root, Upgrade screen Canvas root, Difficulty screen Canvas root
- [ ] Orientation change (portrait ↔ landscape if supported): `SafeAreaFitter` re-applies on next frame after `Screen.orientation` changes
- [ ] Meta screen weapon card 2×2 grid layout active when `Screen.height > Screen.width` (portrait); 4×1 layout when landscape

---

## Implementation Notes

*Derived from ADR-0006 §2 (Safe Area and Pixel Perfect):*

`SafeAreaFitter : MonoBehaviour` — attach to each Canvas root `RectTransform` that must respect device safe area:

```csharp
void Start() => Apply();

void Apply()
{
    Rect safeArea = Screen.safeArea;
    Vector2 anchorMin = safeArea.position;
    Vector2 anchorMax = safeArea.position + safeArea.size;
    anchorMin.x /= Screen.width;  anchorMin.y /= Screen.height;
    anchorMax.x /= Screen.width;  anchorMax.y /= Screen.height;
    _rect.anchorMin = anchorMin;
    _rect.anchorMax = anchorMax;
}
```

Subscribe to orientation-change callback or poll `Screen.orientation` in `Update()` — call `Apply()` only when orientation changes (cache last value). Alternatively use `Screen.onOrientationChange` if available in Unity 6.3 [需查證].

**Touch target enforcement**: `SecondaryWeaponButton` has a `LayoutElement.minWidth = GetDpToPx(44)` and `minHeight = GetDpToPx(44)`:
```csharp
float GetDpToPx(float dp) => dp * Screen.dpi / 160f;
```
Cap at reasonable min: if `Screen.dpi == 0` (editor fallback), use 96.

**Integer scaling**: `Canvas.pixelPerfect = true` on every Canvas. `PixelPerfectCamera.RefPixelsPerUnit` must match `Sprite.pixelsPerUnit` (100 by default). Confirm in Editor: create Editor validation script in `KaijuBreaker.Tools` that asserts `canvas.pixelPerfect == true` on all UGUI Canvases in the combat and meta scenes (CI-executed).

**Mobile meta screen layout**: `LoadoutScreen` and `DifficultySelectScreen` already implement portrait/landscape switching logic (Stories 007, 009). This story adds the `SafeAreaFitter` component to those screen Canvas root RectTransforms — it does NOT reimplement their layout logic.

Config: `HudConfig.MobileBottomUiHeightPct` (0.20), `HudConfig.MobileTopUiHeightPct` (derived as 0.15); used by layout anchoring in C/D and A/B nodes.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002**: `Canvas.pixelPerfect = true` is set when creating Canvases — this story does not re-create Canvases, it only ensures `SafeAreaFitter` is attached and validated
- **Story 001**: World-space part bars are exempt from safe-area fitting (world-space, not screen UI)
- Mobile touch input handling (single-finger drag for ship movement) — belongs to `Input` system, not UI
- L3 mobile charge input scheme — deferred to Prototype milestone (GDD §H.2, weapon-system.md §I)

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **Manual check AC-1**: Safe area respected on mobile portrait (notch device)
  - Setup: Run on iPhone with notch (or Unity Device Simulator with notch active); set to Portrait mode; open combat scene
  - Verify: A/B area elements not clipped by notch; C/D area elements not clipped by home indicator
  - Pass condition: All HUD text and icons fully visible without device chrome overlap; `Screen.safeArea` Rect visually matches UI boundary

- **Manual check AC-2**: 15%/20% zone layout
  - Setup: Open combat scene on 1080×1920 portrait (mobile); enable layout debug gizmos
  - Verify: A/B area top strip occupies top 15% (top 288px of 1920); C/D bottom strip occupies bottom 20% (bottom 384px); middle 65% (1248px) has zero fixed HUD children
  - Pass condition: No fixed RectTransform children found in middle zone; confirmed by Editor Inspector

- **Manual check AC-3**: 44dp touch target on secondary weapon button
  - Setup: On Android device with known DPI (e.g., 420 DPI Pixel 6); open combat scene; inspect secondary weapon button
  - Verify: Button touch area ≥ 44dp × 44dp = `44 × 420/160` ≈ 115.5 × 115.5 px; confirmed via device Accessibility Inspector or Unity Debug.Log of `GetDpToPx(44)`
  - Pass condition: Touch activates reliably from edge of declared target; no require tap on center

- **Manual check AC-4**: 4K pixel-perfect integer scaling
  - Setup: Set PC resolution to 3840×2160 (or 2560×1440 as ×3 test); open combat scene; capture screenshot
  - Verify: Weapon slot icons, pip sprites, blood bar sprites have crisp integer-aligned pixel edges (zoom screenshot 4× and inspect)
  - Pass condition: Zero half-pixel or anti-aliased edges on any pixel-art sprite; `Canvas.pixelPerfect = true` confirmed in Inspector for all 3 in-combat Canvases

- **Manual check AC-5**: SafeAreaFitter on all 5 required Canvas roots
  - Setup: Open combat scene and all meta screen Prefabs in Unity Editor
  - Verify: HUD_Static root, HUD_Dynamic root, LoadoutScreen root, UpgradeScreen root, DifficultySelectScreen root each have `SafeAreaFitter` component attached and enabled
  - Pass condition: All 5 roots confirmed; no missing component warning

---

## Test Evidence

**Story Type**: UI
**Required evidence**:
- UI (ADVISORY): `production/qa/evidence/cross-platform-safe-areas-evidence.md` — screenshots of safe-area layout on notch device, 4K scaling test, 44dp touch target confirmation

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 DONE (HUD Canvases with `Canvas.pixelPerfect = true` already set); Story 007 DONE (Loadout screen Canvas Prefab exists); Story 008 DONE (Upgrade screen Canvas Prefab exists); Story 009 DONE (Difficulty screen Canvas Prefab exists); `PixelPerfectCamera` configured in combat scene; `HudConfig.MobileBottomUiHeightPct`, `MobileTopUiHeightPct` fields in HudConfig SO
- Unlocks: Story 011 (accessibility text scale tested in a layout where safe areas are correctly applied)
