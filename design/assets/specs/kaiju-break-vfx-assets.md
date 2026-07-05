# Asset Specs — Shared Kaiju Break-State VFX (Cross-Boss)

> **Source**: `design/gdd/kaiju-part-system.md` §C.2/E, `design/gdd/game-feel.md` §C, `design/gdd/hit-feel-tiering.md`
> **Art Bible**: `design/art-bible.md` §5.3, §08 (VFX / Particle Color Language)
> **Generated**: 2026-07-05
> **Status**: 6 assets specced / 0 approved / 0 in production / 0 done
> **Target root**: `Assets/_Project/Art/VFX/`

These assets are **shared across all three kaiju** (and future kaiju) rather than
re-authored per boss, per the state-machine notes in each kaiju's own spec file.
This keeps the SOFTENED/BROKEN visual contract (art-bible §5.3) consistent and
cuts production cost — one generic asset set, tinted at runtime by
`technical-artist`'s shader work using each part's own base color.

---

## ASSET-019 — vfx_part_softened_glow (SOFTENED State Overlay)

| Field | Value |
|-------|-------|
| Category | VFX / Particles (shader-driven overlay, not a sprite-sheet animation) |
| Dimensions | Radial gradient ring texture, recommend 64×64 px source (scaled to match each part's bounding box at runtime) |
| Format | PNG, RGBA 32-bit, greyscale-alpha mask (tinted via shader, not baked color) |
| Naming | `vfx_softened_glow_mask.png` |
| Palette | Tinted at runtime between SOFTENED Orange `#FF6600` and Molten Peak Yellow `#FFCC00` (art-bible §2.3), 2Hz sine pulse (`softened_pulse_frequency_hz = 2.0`, locked value — do not vary per kaiju) |
| Z-Order | Must render **below** the enemy bullet layer (art-bible §5.3: "光暈必須渲染於敵彈層之下") |

**Visual Description**: A soft radial glow ring/aura mask that expands slightly
outward from a part's silhouette. This is authored as a **greyscale alpha mask**, not
a pre-colored sprite — the actual `#FF6600`→`#FFCC00` color pulse and part-specific
scaling are shader responsibilities (`technical-artist`). The art deliverable here is
the shape/falloff of the glow only.

**Animation/State Requirements**: Static mask texture; the pulsing behavior is
entirely shader-driven (2Hz sine, locked value shared with the HUD's HEAT-bar pulse
per art-bible §7.3 sync requirement — `softened_heat_bar_pulse_hz` must equal this).

**Readability Constraints**: Must have a soft enough falloff that it never fully
occludes an enemy bullet's 1px black outline (art-bible §5.3/§08 VFX readability
constraint) — recommend testing at low opacity (peak ~40-50% alpha) before handoff.

**Generation Prompt**:
```
Soft radial glow gradient mask, greyscale to alpha, bright center fading smoothly
to transparent edge, circular/oval falloff, 64x64 pixel texture, for use as a
tintable shader overlay (no baked-in color — pure white-to-transparent gradient
so it can be recolored at runtime), soft edges intentional for this specific
asset only (this is the one exception to the pixel-art hard-edge rule, since it
is a shader glow mask, not a sprite).
```

**Target Path**: `Assets/_Project/Art/VFX/Particles/vfx_softened_glow_mask.png`
**Atlas**: `atlas_vfx`
**Status**: PENDING — awaiting API key

---

## ASSET-020 — vfx_part_weakpoint_frame (ARMOR_STRIPPED Weak-Point Outline)

| Field | Value |
|-------|-------|
| Category | VFX / UI-adjacent (shared cross-boss outline element) |
| Dimensions | 9-slice or per-part-scaled rectangular outline frame, 2px stroke width |
| Format | PNG, RGBA 32-bit, transparent center |
| Naming | `vfx_part_weakpoint_frame.png` |
| Palette | Weakness Frame White `#FFFFFF` (art-bible §2.3 — shares the hitbox-white value deliberately, "傳達緊迫感") |
| Behavior | Pulses/fades on a countdown synced to `stagger_duration = 2.0s` (art-bible §5.3) |

**Visual Description**: A crisp 2px pure-white rectangular (or part-silhouette-hugging)
outline that appears the instant any ARMORED part enters `ARMOR_STRIPPED`. This is the
**single visual element shared identically across CARAPEX's dorsal cannon, LACERA's
tail carapace, and VOLTWYRM's shields** — do not let it vary in color, weight, or
style between bosses; consistency here is load-bearing for the "this is now hittable"
teaching moment established by the tutorial boss (CARAPEX).

**Animation/State Requirements**: Recommend a simple 9-slice frame so it can scale to
each ARMORED part's differing dimensions (80×48 for CARAPEX, 32×48 for LACERA, 40×56
for VOLTWYRM) without redrawing. Countdown-fade behavior (shrinking/fading over the
2s stagger window) is shader/UI-driven, not baked into frames.

**Generation Prompt**:
```
Crisp 2 pixel wide pure white (#FFFFFF) rectangular outline frame, hard edges, no
anti-aliasing, no glow falloff, transparent center and background, designed as a
9-slice UI element (corners + edges) so it can stretch to fit rectangular areas of
varying size, sharp pixel-perfect corners.
```

**Target Path**: `Assets/_Project/Art/VFX/Particles/vfx_part_weakpoint_frame.png`
**Atlas**: `atlas_vfx`
**Status**: PENDING — awaiting API key

---

## ASSET-021 — vfx_part_break_explosion (Generic Tintable Part-Break Explosion)

| Field | Value |
|-------|-------|
| Category | VFX / Explosions (sprite-sheet animation) |
| Dimensions | Recommend 96×96 px canvas per frame, 8–12 frame sheet (art-bible §3.5: "8–12 幀 15 fps") |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `vfx_part_break_frame_01.png` … `_1{0-2}.png` |
| Playback | 15 fps, paired with hitstop (`hitstop_part_break_ms` per game-feel.md) then slow-motion |
| Palette | Debris Flash White-Yellow `#FFF1C0` (25% mix) + Debris Secondary Orange `#FF8A4A` (25% mix) + **50% part's own base color** (art-bible §08 "部位破壞主碎片") |

**Visual Description**: A generic pixel-art explosion burst — radiating debris chunks
and a bright flash core — authored in a **neutral white-hot base** so that it can be
multiply-tinted at runtime with each part's own color (per the 50/25/25 mix rule).
This avoids commissioning three separate explosion sets per kaiju theme. Confirm with
`technical-artist` whether the engine's particle/shader pipeline can do runtime
tinting of a baked sprite sheet, or whether three pre-tinted variants (amber/carapex,
yellow-green/lacera, energy-yellow/voltwyrm) are simpler to implement — flagged as an
open technical question, not a design blocker.

**Animation/State Requirements**: 8–12 frames, 15fps, explosion expands outward then
dissipates. Should read as "part is gone" (matches art-bible §5.3 BROKEN state:
"部位 Sprite 消失；爆炸序列粒子依§09規格散射").

**Readability Constraints**: Must not exceed the screen-shake cap (`shake_magnitude_cap
= 24px`) or extend the full-white-flash beyond 0.4s decay-to-<20%-alpha (art-bible §08
VFX readability constraints) — these are `technical-artist`/`game-feel.md` owned
values, but the sprite sheet's own visual "punch" should feel proportionate to a
15fps/8-12-frame budget, not longer.

**Generation Prompt**:
```
16-bit retro arcade pixel art explosion sprite sheet, 8 to 12 frames, radiating
debris chunks and a bright white-hot flash core expanding outward then
dissipating into scattered particles, neutral white-yellow base coloring
(#FFF1C0 core, #FF8A4A secondary debris) designed to be multiply-tinted at
runtime, flat color bands, no smooth gradient blur, transparent background,
96x96 pixel canvas per frame, crisp hard pixel edges, classic arcade shmup
boss-part destruction explosion.
Negative prompt: no smoke by itself (smoke is a separate small particle asset,
see vfx_debris_particle_set), no cold colors, no photorealistic fire.
```

**Target Path**: `Assets/_Project/Art/VFX/Explosions/vfx_part_break_frame_{01-12}.png`
**Atlas**: `atlas_vfx`
**Status**: PENDING — awaiting API key (technical-artist to confirm runtime-tint feasibility)

---

## ASSET-022 — vfx_debris_particle_set (Small Static Debris Particles)

| Field | Value |
|-------|-------|
| Category | VFX / Particles (static sprites, not animated) |
| Dimensions | 3 tiny sprites: white-yellow flash chunk (~4×4px), orange debris chunk (~4-6×4-6px), black smoke puff (~4×4px) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `vfx_debris_flash_white_small.png`, `vfx_debris_orange_small.png`, `vfx_debris_smoke_black.png` |
| Palette | Debris Flash White-Yellow `#FFF1C0` · Debris Secondary Orange `#FF8A4A` · Black Smoke Particle `#2A1A22` |
| Quantities (engine-driven, not per-asset) | Main debris 22+ particles per break; smoke 5 particles, high-gravity fast fall (art-bible §08) |

**Visual Description**: Three tiny single-piece debris sprites used by the particle
system to compose each part-break explosion's aftermath (separate from the main
explosion sheet in ASSET-021 — these are the smaller trailing/falling bits). Simple
angular pixel chunks, no internal detail needed at this size.

**Generation Prompt**:
```
Set of three tiny 16-bit pixel art debris particle sprites for a particle system:
(1) a small bright white-yellow angular chunk (#FFF1C0), 4x4 pixels; (2) a small
orange angular chunk (#FF8A4A), 4x4 to 6x6 pixels; (3) a small dark
near-black smoke puff (#2A1A22), soft round blob, 4x4 pixels. Flat color fill,
no outline needed at this size, transparent background, hard pixel edges except
the smoke puff which may have 1-2 step soft falloff.
```

**Target Path**: `Assets/_Project/Art/VFX/Particles/vfx_debris_{flash_white|orange|smoke_black}_small.png`
**Atlas**: `atlas_vfx`
**Status**: PENDING — awaiting API key

---

## ASSET-023 — vfx_boss_death_burst (Boss Core Death — Highest-Tier VFX)

| Field | Value |
|-------|-------|
| Category | VFX / Particles (single large particle sprite, reused at high count) |
| Dimensions | Single particle sprite ~6×6 to 8×8px, spawned ×110 by the particle system (art-bible §08) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `vfx_boss_death_particle.png` |
| Palette | `#FFFFF0` white-yellow inner core + `#FFE860` outer ring, radiating burst from the core's position |
| Trigger | `on_boss_core_break` — the single biggest visual moment in the game, shared identically across all bosses per `hit-feel-tiering.md` C.3.2 ("完全沿用 game-feel.md，不修改") |

**Visual Description**: A single bright particle sprite (small glowing spark/shard)
spawned at high volume (110 instances) radiating outward from the core's death
position. Because this is the universal "you won" moment across all three (and
future) bosses, this asset is deliberately generic/kaiju-agnostic — it does not use
any per-boss theme color.

**Generation Prompt**:
```
Small bright glowing particle sprite for a burst VFX, white-yellow-gold coloring
(#FFFFF0 core, #FFE860 outer glow), simple spark/shard shape, 6x6 to 8x8 pixel
canvas, flat color bands with slight soft glow falloff (particle-appropriate,
not full pixel-art hard edge), transparent background, designed for high-volume
particle system reuse (must look good spawned 100+ times on screen at once).
```

**Target Path**: `Assets/_Project/Art/VFX/Particles/vfx_boss_death_particle.png`
**Atlas**: `atlas_vfx`
**Status**: PENDING — awaiting API key

---

## ASSET-024 — vfx_full_white_flash_overlay (Technical Note, Not an Art Asset)

**This is not an image asset.** The full-screen white flash (art-bible §08:
`flash_max_alpha = 0.85`, capped below 1.0 specifically to keep the player hitbox dot
visible per §4.4) is implemented as a full-screen solid-color overlay quad controlled
by `game-feel.md`'s alpha-decay curve, not a generated sprite. Included here only so
the manifest has a complete record of every visual element referenced in this pass —
routed to `technical-artist`, not the AI art-generation batch.

**Status**: N/A — implementation task, not an art-generation asset. No manifest entry needed beyond this note.

---

*Spec file version: 1.0.0 — Art Director Agent — 2026-07-05*
