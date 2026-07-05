# Asset Specs — Entity: Player Ship (戰機)

> **Source**: `design/gdd/game-concept.md` §Visual Identity Anchor, `design/art-bible.md` §3.2/§6
> **Art Bible**: `design/art-bible.md`
> **Generated**: 2026-07-05
> **Status**: 4 assets specced / 0 approved / 0 in production / 0 done
> **Target root**: `Assets/_Project/Art/Characters/Ship/` (see manifest note on path reconciliation)

All player-ship assets are governed by Art Law #1 (Tech vs. Titan, art-bible §01) and the
Tech Asset ID Test (art-bible §6.3): geometric silhouette, cold palette only, zero warm
detail, fewer colors/detail than any kaiju asset. The player hitbox dot (§4.4) is the
single highest-priority pixel in the entire game — never compromise on it.

---

## ASSET-001 — char_ship_idle (Idle Float Animation)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Character) |
| Dimensions | 14×20 px per frame, 4-frame sheet (56×20 px sheet or 4 separate files) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `char_ship_idle_01.png` … `char_ship_idle_04.png` |
| Playback | 8 fps, loop; 1–2 px vertical bob (art-bible §3.5) |
| Palette | Ship Primary Blue `#2080F0` (body) · Ship Rim Highlight `#60B0FF` (1–2px edge) · Cockpit Ice `#A0D4FF` (canopy, small area) · Ship Shadow Indigo `#103880` (underside) · Outline `#102040` (1px, cold dark-blue-black per §3.3) |
| Shape Language | Vertical wedge (楔形): sharp nose, wider tail; hard geometric edges, no organic curves (art-bible §6.2) |

**Visual Description**: A small, clean high-tech interceptor silhouette pointing up-screen (nose toward the kaiju). Wedge-shaped hull, hard chamfered edges, a single small cockpit canopy near the front third rendered in pale ice-blue. A 1–2px rim-light highlight traces the leading edges. No warm colors anywhere on the hull — this must pass the 4-question Tech Asset ID Test (art-bible §6.3) with all "yes" answers. The ship must read as visually simpler and smaller than any kaiju part on screen at the same time.

**Readability Constraints**: Must remain legible at 14×20px at ×1 integer scale on the smallest supported phone screen (art-bible §3.1, integer scaling only ×1/×2/×3/×4). Silhouette must stay recognizably "wedge + canopy" even fully desaturated (no color-only cues on the hull itself — the hitbox dot, spec'd separately in ASSET-003, carries the critical player-position signal).

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, top-down vertical shmup player fighter ship,
small sleek geometric wedge silhouette, nose pointing up, hard chamfered edges,
no organic curves, cold blue color scheme only (#2080F0 hull, #60B0FF thin rim
highlight on leading edges, #A0D4FF small cockpit canopy glass, #103880 belly
shadow), 1px solid dark navy outline (#102040), flat color fill, no gradients,
no anti-aliasing, no dithering, transparent background, 14x20 pixel canvas,
crisp hard pixel edges, Cave/Raiden-style bullet-hell player ship aesthetic.
Negative prompt: no warm colors, no red/orange/yellow anywhere, no soft shading,
no blur, no glow, no organic/biological shapes, no weapons visible on hull.
```

**Target Path**: `Assets/_Project/Art/Characters/Ship/char_ship_idle_0{1-4}.png`
**Atlas**: `atlas_player_weapons`-adjacent — actually its own ship atlas or shared `atlas_ui`; recommend a small dedicated `atlas_ship` (art-bible §9.4 does not explicitly list one — flagged in manifest inconsistency log).
**Status**: PENDING — awaiting API key

---

## ASSET-002 — char_ship_bank (Left/Right Banking Frames)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Character) |
| Dimensions | 14×20 px per frame, 4 frames per direction (left/right) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `char_ship_bank_left_01.png`…`_04.png`, `char_ship_bank_right_01.png`…`_04.png` |
| Playback | Instant on input (2-frame entry / 2-frame return to idle), art-bible §3.5 |
| Palette | Identical to ASSET-001 |
| Visual Deviation | 2–4px visual tilt/skew of the hull toward the movement direction (art-bible §3.2 "側移時觸發，視覺偏轉 2–4px") |

**Visual Description**: Same ship as ASSET-001, redrawn with a slight roll/lean (2–4px horizontal skew of the silhouette, wingtip dipping toward the turn direction) to sell lateral movement. Right-bank frames may be produced by mirroring left-bank frames if the hull is symmetric — confirm with technical-artist whether horizontal-flip is acceptable or whether asymmetric canopy glare requires a separate right-hand draw.

**Readability Constraints**: Must not change the ship's cold-only palette or add any new silhouette elements (no new weapon pods, no warm accents) — banking is a pure motion cue, not a new ship state.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, same small geometric wedge player fighter
ship as the idle sprite, banking/leaning 15 degrees to the left, wingtip dipping
down-left, 2-4 pixel horizontal skew of silhouette, cold blue palette (#2080F0
hull, #60B0FF rim highlight, #A0D4FF canopy, #103880 shadow), 1px dark navy
outline (#102040), flat color fill, no gradients, no anti-aliasing, transparent
background, 14x20 pixel canvas, hard pixel edges.
Negative prompt: no warm colors, no new geometry additions, no blur, no glow.
```

**Target Path**: `Assets/_Project/Art/Characters/Ship/char_ship_bank_{left|right}_0{1-4}.png`
**Status**: PENDING — awaiting API key

---

## ASSET-003 — char_ship_hitbox_dot (Player Hitbox Indicator)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Critical UI/Gameplay Element) |
| Dimensions | **1×1 px, fixed — never scaled, never varied** (art-bible §3.2/§4.4) |
| Format | PNG, RGBA 32-bit, single solid pixel, no border |
| Naming | `char_ship_hitbox_dot.png` |
| Z-Order | Absolute top of render stack — above the full-white flash overlay (`flash_max_alpha = 0.85`) |
| Palette | Hitbox White `#FFFFFF` — **the only correct value, never negotiable** |

**Visual Description**: A single solid white pixel. No outline, no anti-aliasing, no drop shadow. This is the single most important pixel in the game per art-bible §01 Law #2 and §4.4 — it is the ground truth for "where is the player" in any bullet-hell density. It must never be color-shifted, tinted, dimmed, or hidden by any other system.

**Readability Constraints**: Must remain visible under 0.85-alpha full-screen white flash (art-bible §4.4 — this is *why* `flash_max_alpha` is capped below 1.0, not 1.0). Must not be merged into the ship sprite's sprite-atlas batch (art-bible §3.2: "不可合批於其他 Sprite") — render as a separate always-on-top draw call.

**Generation Prompt**:
```
Single solid pure white 1x1 pixel, no anti-aliasing, no outline, transparent
background, RGBA PNG, hex #FFFFFF exactly.
```
(This asset is trivial enough to be hand-authored directly in-engine rather than
run through an image-generation pipeline — flagged as a candidate for
`technical-artist` to generate procedurally instead of via the AI batch job.)

**Target Path**: `Assets/_Project/Art/Characters/Ship/char_ship_hitbox_dot.png`
**Status**: PENDING — awaiting API key (or direct hand-authoring, see note above)

---

## ASSET-004 — char_ship_engine_flame (Engine Exhaust Animation)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (VFX-adjacent, attached layer) |
| Dimensions | 6×10 px per frame, 4-frame sheet |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `char_ship_engine_flame_01.png` … `_04.png` |
| Playback | 12 fps, loop; intensity/length scales with ship velocity (art-bible §3.5) |
| Layer | Independent layer, attached to ship bottom, renders *behind* the hull |
| Palette | Laser Core Cyan `#40F8FF` (flame core) → Ship Primary Blue `#2080F0` (fade-out edge) — art-bible §6.2 |

**Visual Description**: A small tapering flame/thruster glow trailing from the ship's engine ports. Bright cyan-white core fading to the ship's own blue at the tail. Reads as pure cold-tech propulsion — no warm color at any point, even in the "hottest" part of the flame (this is a deliberate deviation from real-world engine exhaust color logic, in service of the cold=player rule).

**Readability Constraints**: Must stay visually subordinate to the ship silhouette and never overlap or obscure the hitbox dot (ASSET-003). Low particle/pixel density — this is atmosphere, not a readability-critical element.

**Generation Prompt**:
```
16-bit pixel art sprite, small thruster exhaust flame trailing from a spaceship
engine, tapering teardrop shape, bright cyan-white core (#40F8FF) fading to
medium blue (#2080F0) at the tail tip, flat color bands (no smooth gradient,
2-3 discrete color steps), no outline needed, transparent background, 6x10
pixel canvas, crisp hard pixel edges, 4-frame flicker animation set.
Negative prompt: no orange/yellow/red flame colors, no smoke, no soft blur,
no anti-aliasing.
```

**Target Path**: `Assets/_Project/Art/Characters/Ship/char_ship_engine_flame_0{1-4}.png`
**Status**: PENDING — awaiting API key

---

*Spec file version: 1.0.0 — Art Director Agent — 2026-07-05*
