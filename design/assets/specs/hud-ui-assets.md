# Asset Specs — UI / HUD Visual Style + Font

> **Source**: `design/art-bible.md` §02 (master palette), §03.2 (UI element sizes), §07 (UI/HUD visual style)
> **Governing ADR**: ADR-0006 (UI framework — SpriteRenderer world-space bars + UGUI HUD/meta)
> **Generated**: 2026-07-05
> **Status**: font PENDING (director drops TTF) · palette + layout SPEC READY · icons PENDING (await AI-art key)
> **Target root**: `Assets/_Project/Art/UI/` (sprites) · `Assets/_Project/Art/Fonts/` (font assets)

This spec closes the **HUD gap** from the first art pass and answers the directive *"字體跟介面風格要對得上"*
(font + interface style must be cohesive). It turns art-bible §07 into an implementable style: one pixel
font, one cold-family palette, and per-component rules. **The crude prototype look = Unity's default Arial
+ ad-hoc muddy colors.** Fixing it is (1) a pixel font, (2) the palette below, (3) consistent component styling.

---

## 1. Font (the #1 style fix)

Art-bible §7.2: **pixel bitmap font**, Latin cap/digit height 5–7px, **Chinese 16px min**, integer scaling only,
primary white with cold-cyan `#40F8FF` emphasis. The game ships **Traditional Chinese** (殲獸戰機), so the font
MUST cover 繁體 CJK, not just Latin/Simplified.

### 1.1 Recommended fonts (all OFL — free for commercial use)

| Role | Font | Why | Source |
|------|------|-----|--------|
| **Primary HUD/body (pick this)** | **方舟像素 Ark Pixel — 16px** | Pan-CJK incl. 繁體 + Latin + digits in ONE file; a native **16px** cut that exactly meets §7.2's 16px CJK minimum; clean, neutral, made for pixel games | github.com/TakWolf/ark-pixel-font (OFL 1.1) |
| Alt body (denser) | **缝合像素 Fusion Pixel — 12px** | Same author, 10/12px cuts if 16px feels large on phone | github.com/TakWolf/fusion-pixel-font (OFL) |
| Alt body (popular) | **最像素 Zpix — 12px** | Widely used, 繁+简, 12px | github.com/SolidZORO/zpix-pixel-font (OFL) |
| Title / brand flavor (optional) | **俐方體 Cubic 11 — 11px** | Traditional-Chinese-designed, elegant; good for the 殲獸戰機 logotype only | "Cubic 11 / 俐方體", GitHub, OFL |
| Latin big numerals (optional) | **PixelOperator** or **m6x11** | If HUD score/ammo numbers want a chunkier Latin look; Ark Pixel already covers this, so optional | free / CC0 |

**Decision: use 方舟像素 Ark Pixel 16px as the single primary font** everywhere (menu, HUD, results). One font =
cohesive. Add Cubic 11 later only for the title logotype if we want extra brand character.

### 1.2 Unity import settings (crispness is all in the import)

A pixel font rendered wrong looks blurry — worse than Arial. Rules:

- **Display only at integer multiples of the native size** (16, 32, 48 for Ark Pixel 16px). Never 20, 24, 30 — those blur.
- **Legacy `Font` (used by the prototype IMGUI + assignable to `_pixelFont`)**: import the TTF, set *Rendering Mode = Hinted Raster* (or *Hinted Smooth* off), *Character = Dynamic*, *Incl. Font Data = on*. Keep label `fontSize` at 16/32/48.
- **Production TMP (UGUI, ADR-0006)**: Create → TextMeshPro → Font Asset; *Sampling Point Size = 16* (native), *Atlas Population = Static*, *Render Mode = **RASTER** (bitmap, no SDF)*, atlas texture *Filter Mode = **Point (no filter)***, *Compression = None*. Use the **TextMeshPro/Bitmap** shader (not the SDF shader — SDF softens pixel edges). Pre-populate the atlas with the CJK glyph set the game uses (extract from the string tables) so runtime has no dynamic misses.
- Text color = white `#FFFFFF`; emphasis = `#40F8FF`. No soft drop-shadows; if a shadow is needed use a 1px hard `#000000`/`#102040` offset (art-bible §3.3 outline colors).

### 1.3 Wiring (already done in the prototype)

`StagePrototypeDriver` and `MainMenuPrototype` now expose a `_pixelFont` (`Font`) field. **Drop the Ark Pixel
16px TTF into `Assets/_Project/Art/Fonts/`, assign it to both `_pixelFont` fields in the Inspector** → every
menu/HUD/results label switches to the pixel font instantly (single `Style()` chokepoint). Until then it falls
back to the built-in font but already uses the correct palette below.

---

## 2. Palette → UI mapping (art-bible §02 / §07)

**Rule: UI chrome is COLD (player/tech family). Warm is reserved for kaiju + threat/warning only** (art-bible §01 Law 1).

| Token | Hex | Use |
|-------|-----|-----|
| Bg Deep | `#0A0E1A` | Full-screen menu/results background (§7.5 arcade-screen feel) |
| Panel Base | `#1A2030` | Cards, stat panels, bar troughs |
| Panel Selected | `#1E4A66` | Selected primary card / difficulty (cold blue) |
| Panel Selected (secondary) | `#1A5250` | Selected secondary card (cold teal — primary-vs-secondary cue) |
| Accent Cyan | `#40F8FF` | Selection text, emphasis numbers, title, material counter (§7.2) |
| Tech UI Blue | `#00C0E0` | HUD section headers, cold bar fills (HP, reload), system indicators (§02) |
| Button Blue | `#2080F0` | Primary buttons (START, retry) (§7.5) |
| Disabled | `#303040` | Greyed buttons / cooldown fill (§7.5) |
| Text | `#FFFFFF` | Primary text |
| Text Dim | `#8AA0B0` | Secondary labels, niche/desc, help lines (cold gray) |
| Warn (warm) | `#CC2200` | Low HP / low ammo / insufficient-material ✗ (§7.5) |
| Amber (warm) | `#FFE060` | Kaiju threat: boss HP bar, L3 charge-READY, boss card select (§7.4) |
| Kaiju Card Base / Sel | `#241512` / `#5A2A1E` | Boss-target cards — warm, because kaiju = warm (on-brand) |

---

## 3. Component specs

### 3.1 HUD screen zones (art-bible §7.4) — event-driven, never block the play field

| Zone | Content | Style |
|------|---------|-------|
| Top-left (A) | Phase label, score, player HP bar | Header text `#00C0E0`; HP fill `#00C0E0`, flips `#CC2200` under 30% |
| Top-center | Boss name + core BREAK bar (boss only) | Name `#FFE060`; bar fill `#FFE060` (warm threat), trough `#1A2030` |
| Top-right (B) | Material counter | `素材 N` in `#40F8FF`; on gain, 1-shot `#62F0D8` bounce (§7.4) |
| Bottom-left (C) | Primary weapon + L3 charge bar | Label `#40F8FF`; charge fill cold `#00C0E0` → warm `#FFE060` when ready |
| Bottom-right (D) | Secondary weapon + ammo / reload | Label `#40F8FF`; reload fill `#00C0E0`; ammo count `#FFFFFF`, `#CC2200` when ≤1 |
| Center | **nothing** — never place fixed HUD over the play field | — |

### 3.2 World-space part bars — spec already complete in art-bible §7.3

HEAT bar (above part, gray→orange, full `#FF6600` pulse at 2 Hz when SOFTENED) + BREAK bar (below, blue→white).
Height 3px, width = part sprite width, ≥6 screen-px after mobile scaling. **These are SpriteRenderer world-space
(ADR-0006), not HUD.** Build in the hud-ui epic; the prototype fakes them per-part.

### 3.3 Meta screens (loadout / results / upgrade) — art-bible §7.5

Bg `#0A0E1A`; cards `#1A2030` base, selected `#40F8FF` 2px outline (UGUI can do a real outline; IMGUI prototype
approximates with a tint + `✓`); primary button `#2080F0` white text; disabled `#303040`; insufficient-material
`#CC2200 ✗`; difficulty cards equal size, selected ×1.05 + cyan outline, **no recommended bias**; Tier-3 preview =
pixel-blur mask `#1A2030` keeping the silhouette.

### 3.4 UI sprite icons (PENDING — await AI-art key) — sizes from art-bible §3.2

| Asset | Size | Notes |
|-------|------|-------|
| Weapon icons L1–L4 / M1–M4 | 16×16 px | uniform |
| Tier badge T0–T3 | 8×6 px | top-right of icon |
| Ammo pip | 6×6 px | filled / empty |
| Material / core icons | 16×16 px | shard + 3 cores + essence (see `material-icons-assets.md`) |

---

## 4. Prototype vs production

- **Now (prototype, IMGUI):** restyled to the palette above + `_pixelFont` hook. Interim — makes the visible build
  cohesive without waiting on the real UI epic. Throwaway with the rest of `Assets/_Project/Prototype/`.
- **Production (hud-ui epic, ADR-0006):** UGUI Canvas + TextMeshPro (bitmap font asset) for HUD/meta, SpriteRenderer
  world-space bars. This spec is the style contract that epic implements. Keep the same palette tokens + font.

---

## 5. Generation worklist (batch once the font TTF + AI-art key land)

| Item | Status | Action |
|------|--------|--------|
| Ark Pixel 16px font asset | **PENDING (director)** | download OFL TTF → `Art/Fonts/` → assign `_pixelFont` + build TMP bitmap asset |
| Weapon / tier / ammo / material icons | **PENDING (AI-art key)** | generate per §3.4 + `material-icons-assets.md` |
| UGUI HUD + meta screens | backlog | hud-ui epic implements this spec |
