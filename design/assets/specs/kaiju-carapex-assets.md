# Asset Specs — Kaiju: CARAPEX (鎧殼獸 / #01, 甲殼系)

> **Source**: `design/gdd/kaiju/01-carapex.md`, `design/gdd/kaiju-part-system.md`
> **Art Bible**: `design/art-bible.md` §3.2, §5.1–5.4
> **Generated**: 2026-07-05
> **Status**: 4 assets specced / 0 approved / 0 in production / 0 done
> **Target root**: `Assets/_Project/Art/Kaiju/Carapex/`

**Silhouette contract (art-bible §5.2)**: thick horizontal trapezoid, mirrored dorsal
carapace + forward-mounted claws, left-right symmetric. Must pass the 3-kaiju
silhouette test (5 testers, <1s correct ID, ≥80%) alongside LACERA and VOLTWYRM.
Screen occupancy target: 60–75% width, 50–65% height at 320×480 baseline
(~192–240px wide × ~240–312px tall for the full assembled boss).

**State-machine note (applies to every part in this file, see art-bible §5.3)**: Each
part has three states — `INTACT → SOFTENED → BROKEN`. **Only the INTACT sprite is a
hand-authored art asset.** SOFTENED is a runtime shader color-shift (`#FF6600` hue
overlay + pulsing glow, driven by `technical-artist`) layered on top of the INTACT
sprite — no separate SOFTENED sprite is commissioned. BROKEN removes the sprite and
plays the shared explosion VFX (see `kaiju-break-vfx-assets.md`) — no separate BROKEN
sprite either, **except** where a part is `ARMORED`, which requires a second physically
different sprite for `ARMOR_STRIPPED` (the armor plate cracks open — this is a real
geometry change, not a tint).

---

## ASSET-005 — kaiju_carapex_chest_reactor_core (BOSS_CORE)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, animated) |
| Part ID | `chest_reactor_core` — BOSS_CORE, 64×64 px, hitbox marker ×1.2 |
| Dimensions | 64×64 px canvas, 4–8 frame idle-pulse sheet (art-bible §3.5: BOSS_CORE idle 4-8 frames, 8fps) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_carapex_chest_core_intact_01.png` … `_0{4-8}.png` |
| Palette | Amber Carapace `#B87020` (surrounding plate) · Core Dark Red Pulse `#800010` (the pulsing organic core itself) · Carapace Deep Shadow `#402010` (recesses) · Outline `#1A0800` (1px, kaiju warm-black per §3.3) |
| Visibility | **Always visible, permanently un-hidden** — must read as the unambiguous "this is the win-condition target" element even at first glance (kaiju-part-system.md F.5) |

**Visual Description**: A biomechanical reactor-organ nested in the center of the
carapace, protected by amber-plated housing but with the deep-red pulsating core
itself exposed and unmissable. The core visibly throbs (idle animation — subtle
scale/brightness pulse, not a full state change) to draw the eye as the permanent
kill-target. Surrounding plate texture uses the 3-tone amber/rust/sick-yellow
hierarchy shared across all CARAPEX parts (art-bible §5.4) so it reads as "part of
the same creature" while the core itself stays visually distinct via its darker,
wetter red.

**Animation/State Requirements**: 4–8 frame idle pulse loop at 8fps (art-bible §3.5).
SOFTENED = shader overlay (not art asset, see file header). BROKEN = sprite removed
+ shared explosion VFX, tinted with this part's own palette per the 50%/25%/25% debris
mix rule (art-bible §08).

**Readability Constraints**: Must remain the single largest, brightest warm element on
the CARAPEX silhouette so it reads instantly as "the core" even in dense D4 bullet
fields (kaiju-part-system.md AC target: 80% recognition). Must never be confused with
enemy bullets — no round/oval bullet-shaped silhouette elements within the sprite.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, biomechanical kaiju boss-core organ, thick
armored amber-orange carapace housing (#B87020) surrounding an exposed pulsating
dark-red organic reactor core (#800010), sick-yellow accent plating (#C09820),
deep brown-black shadow recesses (#402010), 1px solid dark warm-black outline
(#1A0800), flat color fill with 2x2 checkerboard dither allowed only in large
shadow areas, no anti-aliasing, no smooth gradients, transparent background,
64x64 pixel canvas, crisp hard pixel edges, R-Type/Blazing Star boss-part
aesthetic, insectoid/crustacean biomechanical design language.
Negative prompt: no cold colors (no blue/cyan/white-blue), no clean geometric
shapes, no smooth curves without plate segmentation, no bullet-like round shapes.
```

**Target Path**: `Assets/_Project/Art/Kaiju/Carapex/kaiju_carapex_chest_core_intact_0{1-8}.png`
**Atlas**: `atlas_carapex`
**Status**: PENDING — awaiting API key

---

## ASSET-006 — kaiju_carapex_mandible (NORMAL, Left/Right Mirror Pair)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, animated, mirrorable) |
| Part ID | `left_mandible` / `right_mandible` — NORMAL, 48×64 px each (art-bible §3.2: "鏡像對稱可共用源檔") |
| Dimensions | 48×64 px canvas, 4-frame idle sheet + attack-telegraph flash frames |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_carapex_mandible_intact_01.png` … `_04.png` (source; right side = horizontal mirror in engine, no separate art needed) |
| Palette | Amber Carapace `#B87020` (base) · Rust Orange Claw `#A83810` (mandible tip/pincer highlight) · Sick Yellow Plate `#C09820` (segment texture) · Carapace Deep Shadow `#402010` · Outline `#1A0800` |
| Attack Telegraph | Additional 4-6 frame bright-amber pulse variant for the 0.5s pre-fire telegraph (kaiju/01-carapex.md §5 Pattern A) |

**Visual Description**: A large lobster/crustacean-style claw-mandible jutting from the
carapace's flank, hinged at the shoulder, with a serrated rust-orange pincer tip that
visibly opens/closes in its idle animation (this doubling as the attack telegraph per
the GDD's "對應大顎脈動為明亮琥珀色，持續0.5s後發射"). Segment lines in sick-yellow
break up the silhouette so it doesn't read as a single flat blob. Source art can be
drawn once (left-facing) and mirrored for the right side in-engine — confirm this with
`technical-artist` before commissioning a second hand-drawn variant.

**Animation/State Requirements**: 4-frame idle open/close pulse (8fps). Separate
telegraph-flash frame set (brighter amber `#FF8000`-adjacent tint) fires 0.5s before
each Mandible Cross-fire volley. BROKEN = sprite removed + explosion VFX (no stub
remnant needed for CARAPEX per its GDD — contrast with LACERA, which explicitly
requests a stub, see `kaiju-lacera-assets.md`).

**Readability Constraints**: The telegraph flash must be unambiguous against the
part's own resting-state amber — recommend at least one full palette step brighter
(toward `#FFB040`, matching art-bible §4.2's "電報顏色為對應彈色的更亮版本" rule) so
players can distinguish "about to fire" from "idle" at a glance, independent of the
bullet color itself.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, large crustacean/lobster-style claw-mandible
limb attached to a kaiju's carapace flank, hinged pincer tip, amber-orange base
plating (#B87020), rust-orange serrated pincer highlight (#A83810), sick-yellow
segment lines (#C09820), deep brown shadow (#402010), 1px solid dark warm-black
outline (#1A0800), flat color fill, no smooth gradients, transparent background,
48x64 pixel canvas, crisp hard pixel edges, insectoid/crustacean biomechanical
kaiju boss-part design, side-facing orientation for horizontal mirroring.
Negative prompt: no cold colors, no smooth unsegmented curves, no bullet shapes.
```

**Target Path**: `Assets/_Project/Art/Kaiju/Carapex/kaiju_carapex_mandible_intact_0{1-4}.png` (+ telegraph variant set)
**Atlas**: `atlas_carapex`
**Status**: PENDING — awaiting API key

---

## ASSET-007/008 — kaiju_carapex_dorsal_cannon (ARMORED — Two Physical States)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, two distinct states) |
| Part ID | `dorsal_cannon` — ARMORED, 80×48 px, top-center mounted |
| Dimensions | 80×48 px canvas × 2 (INTACT + STRIPPED are each a full separate sprite, art-bible §3.2) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_carapex_dorsal_cannon_intact.png` (ASSET-007), `kaiju_carapex_dorsal_cannon_stripped.png` (ASSET-008) |
| Palette (INTACT) | Amber Carapace `#B87020` · Sick Yellow Plate `#C09820` (thick armor plating, no visible weak point) |
| Palette (STRIPPED) | Cracked-open plating in the same base tones, revealing an interior deep-red weak point core with a **2px pulsing pure-white outline** (`#FFFFFF`, art-bible §5.3 "弱點外框白") |

**Visual Description (INTACT)**: A thick, closed armored turret mounted dorsally at
the top-center of CARAPEX, no visible seams suggesting weakness — reads as solid and
impenetrable. Faint downward-facing barrel/vent suggesting where the Dorsal Gravel
Spray pattern originates.

**Visual Description (STRIPPED)**: The same turret with its armor plates visibly
blown open (matches the 6-frame, 0.15s armor-crack animation in art-bible §3.5),
exposing a dark-red organic weak point in the newly-opened cavity. The weak point
carries a 2px pure-white pulsing outline that is **shared visual language** with every
other ARMORED part's stripped state across all three kaiju (this outline color/weight
must not vary by kaiju — it's a cross-boss "this is now hittable" signal).

**Animation/State Requirements**: INTACT has a subtle idle pulse (4 frames, matches
general part idle cadence). The INTACT→STRIPPED transition itself is a **separate
6-frame transition animation** (art-bible §3.5, 0.15s), commissioned as its own
sprite sheet (`kaiju_carapex_dorsal_cannon_crack_open_01..06.png`) bridging the two
static states — flagged as ASSET-007b below.

**Readability Constraints**: This is the canonical "armor is a gate, not a wall"
teaching moment for the whole game (CARAPEX is the tutorial boss) — the INTACT vs
STRIPPED silhouette difference must be large and unambiguous even glanced at for a
fraction of a second, since the GDD's tutorial acceptance criteria (AC-02) requires
players to notice the armor-open state within 0.3s of the visual change appearing.

**Generation Prompt (INTACT)**:
```
16-bit retro arcade pixel art sprite, thick closed armored dorsal turret/cannon
mounted on top of a kaiju's carapace, solid amber-orange armor plating (#B87020),
sick-yellow accent seams (#C09820), no visible weak points, small downward-facing
barrel vent, deep brown shadow (#402010), 1px solid dark warm-black outline
(#1A0800), flat color fill, transparent background, 80x48 pixel canvas, crisp
hard pixel edges, imposing sealed-turret silhouette.
Negative prompt: no cold colors, no cracks or openings, no red glow.
```

**Generation Prompt (STRIPPED)**:
```
16-bit retro arcade pixel art sprite, the same dorsal turret with its armor
plating violently cracked open, jagged broken amber-orange plate edges (#B87020,
#A83810 fracture highlights), exposing a dark wet red organic weak-point core
(#800010) in the newly opened cavity, the weak point rimmed with a bright pure
white pulsing 2px outline (#FFFFFF), deep brown shadow (#402010), 1px solid dark
warm-black outline on the outer plating (#1A0800), flat color fill, transparent
background, 80x48 pixel canvas, crisp hard pixel edges.
Negative prompt: no cold colors except the white weak-point outline, no smooth
unbroken plating, no ambiguous silhouette that could be mistaken for INTACT.
```

**Target Path**: `Assets/_Project/Art/Kaiju/Carapex/kaiju_carapex_dorsal_cannon_{intact|stripped}.png`
**Atlas**: `atlas_carapex`
**Status**: PENDING — awaiting API key

### ASSET-007b — kaiju_carapex_dorsal_cannon_crack_open (Transition Animation)

| Field | Value |
|-------|-------|
| Dimensions | 80×48 px, 6-frame sheet |
| Duration | 0.15s total (art-bible §3.5) |
| Naming | `kaiju_carapex_dorsal_cannon_crack_open_01.png` … `_06.png` |
| Generation note | Interpolation sheet bridging ASSET-007 (INTACT) and ASSET-008 (STRIPPED) — commission after both endpoint sprites are approved, so the AI/artist has fixed start/end references. |
**Status**: PENDING — awaiting API key (blocked on 007/008 approval first)

---

## Manifest Reconciliation Note (CARAPEX)

CARAPEX's GDD (`kaiju/01-carapex.md` §3) does **not** request a broken-limb "stub"
remnant sprite the way LACERA's GDD does — its mandibles simply disappear on BROKEN
per the generic art-bible §5.3 rule. This is intentional per CARAPEX's tutorial-boss
role (simpler visual language) and is **not** an inconsistency; noted here only for
clarity when comparing against `kaiju-lacera-assets.md`.

---

*Spec file version: 1.0.0 — Art Director Agent — 2026-07-05*
