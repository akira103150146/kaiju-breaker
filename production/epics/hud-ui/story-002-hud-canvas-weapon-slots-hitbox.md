# Story 002: In-Combat HUD Canvas Setup + Weapon Slot Displays + Hitbox Overlay

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: ~3–4h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0006: UI Framework Selection (Primary); ADR-0002: Event Architecture (Secondary)
**ADR Decision Summary**: In-combat HUD uses UGUI in Screen Space – Camera mode, split into three Canvases sharing `HUD_Atlas`: `HUD_Static` (sortingOrder=10), `HUD_Dynamic` (sortingOrder=11), `Hitbox_Overlay` (sortingOrder=99). This split ensures only `HUD_Dynamic` rebuilds each frame; `Hitbox_Overlay` at sort 99 guarantees the P0 hitbox dot survives the `flash_max_alpha=0.85` full-screen flash (ADR-0006 §2). Weapon slot displays update via `IEventBus` weapon equip events (ADR-0002).

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: `Canvas.pixelPerfect = true` combined with `PixelPerfectCamera` Camera reference in Screen Space – Camera mode — [需查證 #1 in ADR-0006: confirm this works correctly in URP 2D Renderer under Unity 6.3]. Sort-order interaction between UGUI Screen Space – Camera Canvases and SpriteRenderer-based game-feel flash layer — [需查證 #5 in ADR-0006: confirm sortingOrder=99 renders above SpriteRenderer flash at same/lower sort value; file spike result in `docs/architecture/tech-spikes/`].

**Control Manifest Rules (Presentation layer)**:
- Required: Subscribe weapon equip events in `OnEnable`/`OnDisable`; update display on event (not every frame); weapon icon and tier badge Sprites sourced from `HUD_Atlas`; all layout via `RectTransform`; `Canvas.pixelPerfect = true`
- Forbidden: Canvas in bullet play area (canvases are Screen Space – Camera, not World Space); direct reference to `Weapons` assembly; any game-state mutation; hover-only interactions (must support both mouse-click and touch-tap)
- Guardrail: HUD total ≤5 additional draw calls (two Canvas batches + Hitbox + floating-text pool); `Hitbox_Overlay` single `Image` component only; bullet-readability-not-obstructed — C/D area HUD panels must not extend into the center play area

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §D.1, D.2, D.5, D.6, M.1:*

- [ ] Three Canvases created in combat scene: `HUD_Static` (sortingOrder=10), `HUD_Dynamic` (sortingOrder=11), `Hitbox_Overlay` (sortingOrder=99); all Screen Space – Camera mode referencing `PixelPerfectCamera`
- [ ] `Canvas.pixelPerfect = true` on all three Canvases
- [ ] `Hitbox_Overlay` contains a single 1px white `Image` (P0 hitbox dot) that is permanently enabled and **never** obstructed by any mask or flash
- [ ] P0 hitbox dot visible in screenshot taken at peak `flash_max_alpha = 0.85` full-screen flash (z-order verification, M.1)
- [ ] Primary weapon slot (C area, bottom-left): displays weapon icon (16×16 Image), weapon name (TMP, ≤4 Chinese chars), Tier badge (T0–T3 Image overlay on icon top-right) — updates on weapon equip event, no animation delay
- [ ] Secondary weapon slot (D area, bottom-right): displays weapon icon (16×16 Image), weapon name (TMP), Tier badge — updates on weapon equip event, no animation delay
- [ ] C/D area panels do not extend into the center play area (bullet zone); layout confined to bottom quadrant per GDD §C.2
- [ ] HUD elements do not occlude any enemy bullet outline at ≥70% screen coverage — design constraint from M.1 / difficulty-system.md H.7 (target verified via 5-person screenshot test at D4 difficulty, ADVISORY)

---

## Implementation Notes

*Derived from ADR-0006 §2 — In-Combat HUD Implementation Detail:*

**Canvas hierarchy** (all Screen Space – Camera, PixelPerfectCamera reference):
```
HUD_Root (scene root)
  ├── HUD_Static (Canvas, sortingOrder=10, pixelPerfect=true)
  │     └── [Stage label frame, area decoration — static, no per-frame update]
  ├── HUD_Dynamic (Canvas, sortingOrder=11, pixelPerfect=true)
  │     ├── PrimaryWeaponSlot (C area)
  │     ├── SecondaryWeaponSlot (D area)
  │     └── [L3 charge bar — Story 004]
  │         [Ammo pips + reload bar — Story 003]
  │         [Material counter — Story 005]
  │         [Boss HP bar — Story 005]
  └── Hitbox_Overlay (Canvas, sortingOrder=99, pixelPerfect=true)
        └── HitboxDot (Image, 1×1px white, always enabled)
```

`HUD_Static` contains only elements that do not change after scene load. Moving any element to `HUD_Dynamic` by mistake will cause unnecessary Canvas rebuilds — add an Editor validation script in `KaijuBreaker.Tools` to check this (CI-enforced).

**Weapon slot update pattern** (ADR-0002): subscribe to `WeaponEquipped` event struct in `OnEnable`; filter by `slot == Primary/Secondary`; update `Image.sprite` (icon from HUD_Atlas), `TMP_Text.SetText` (name), tier badge `Image.sprite`. Use `TMP_Text.SetText(string)` overload — no per-update string allocation.

**Hitbox dot**: position locked to player ship's world position via `Canvas.worldCamera` → screen-space conversion each frame in `LateUpdate`, OR parent the dot to the ship transform (simpler, no camera math). `Image.raycastTarget = false` to avoid input interference.

**P0 z-order invariant**: `Hitbox_Overlay.sortingOrder = 99` must always be the highest sortingOrder in the scene. `flash_max_alpha = 0.85` (not 1.0) is a hard constraint from `game-feel.md` precisely to keep the dot visible through flash — do not modify this value.

Sprite atlas: all HUD icon Sprites packed in `Assets/_Project/Art/UI/HUD_Atlas.spriteatlas`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 003**: Secondary ammo pips and reload bar (attach to SecondaryWeaponSlot node created here)
- **Story 004**: L3 charge bar (attach to PrimaryWeaponSlot node created here)
- **Story 005**: Material counter (B area) and Boss HP bar (A area)
- **Story 010**: `SafeAreaFitter` and mobile portrait layout; integer-scaling verification
- **Story 001**: World-space SpriteRenderer part bars (separate rendering layer, not Canvas)

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **Manual check AC-1**: Three Canvases with correct sort orders
  - Setup: Open combat scene in Unity Editor; select each Canvas GameObject in Hierarchy
  - Verify: Inspector shows HUD_Static sortingOrder=10, HUD_Dynamic sortingOrder=11, Hitbox_Overlay sortingOrder=99; all have `Canvas.pixelPerfect = true` and the `PixelPerfectCamera` Camera reference set
  - Pass condition: All three values match spec; no Canvas missing its Camera reference

- **Manual check AC-2**: P0 hitbox dot visible through full-screen flash
  - Setup: In a test scene, trigger game-feel on-hit flash at `flash_max_alpha = 0.85`; capture screenshot at peak alpha frame
  - Verify: 1px white hitbox dot identifiable in the screenshot despite the flash overlay
  - Pass condition: Dot visible in zoomed screenshot (at least 1 pixel clearly white against the flash); repeat for L/R movement positions

- **Manual check AC-3**: Weapon slot displays update on equip
  - Setup: Equip L2 (T2) as primary, M3 (T1) as secondary; open combat scene
  - Verify: C area shows L2 icon + T2 tier badge; D area shows M3 icon + T1 badge; weapon names correct
  - Pass condition: All display elements match weapon definition data; no stale data from previous weapon

- **Manual check AC-4**: HUD panels do not enter center play area
  - Setup: Open combat scene at 1920×1080; enable gizmo overlay showing safe areas
  - Verify: C/D area panel bounds (RectTransform) are entirely within the bottom 20% of screen height
  - Pass condition: No HUD panel RectTransform vertex above the bottom-20% boundary line

- **Manual check AC-5**: Pixel-perfect rendering (no sub-pixel blur)
  - Setup: Set PC resolution to 1920×1080 (×2 scale from 960×540 base); capture screenshot of weapon slot icon
  - Verify: Icon pixel edges are sharp with no anti-aliasing or sub-pixel blur
  - Pass condition: Pixels align to integer boundaries in a pixel-zoomed screenshot crop

---

## Test Evidence

**Story Type**: UI
**Required evidence**:
- UI (ADVISORY): `production/qa/evidence/hud-canvas-weapon-slots-hitbox-evidence.md` — screenshots of sort-order proof (flash test), weapon slot display, layout bounds check

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `IEventBus` with `WeaponEquipped` event struct in Core (core-foundation DONE); `HUD_Atlas` SpriteAtlas created by art pipeline (art director); `PixelPerfectCamera` configured in combat scene (engine-programmer)
- Unlocks: Story 003 (SecondaryWeaponSlot node available); Story 004 (PrimaryWeaponSlot node available); Story 005 (HUD_Dynamic Canvas available); Story 010 (applies SafeAreaFitter to HUD_Static/Dynamic root RectTransforms created here)
