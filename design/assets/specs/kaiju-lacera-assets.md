# Asset Specs — Kaiju: LACERA (刃肢獸 / #02, 肢體系)

> **Source**: `design/gdd/kaiju/02-lacera.md`, `design/gdd/kaiju-part-system.md`
> **Art Bible**: `design/art-bible.md` §3.2, §5.1–5.4
> **Generated**: 2026-07-05
> **Status**: 6 assets specced / 0 approved / 0 in production / 0 done
> **Target root**: `Assets/_Project/Art/Kaiju/Lacera/`

**Silhouette contract (art-bible §5.2)**: central trunk with dynamic outward-radiating
blade limbs; asymmetric, dynamic sweeping motion (unlike CARAPEX's static symmetry).
Screen occupancy: torso ~40% width, limb sweep reaches ~70% width. Color direction:
sick yellow-green body (`#789010`) + orange-brown segment rings (`#885010`), bright
orange blade tips (`#D07000`) as the threat-emphasis accent (art-bible §5.4).

**State-machine note**: same INTACT/SOFTENED/BROKEN convention as CARAPEX (see that
file's header) — SOFTENED is a shared shader overlay, not a separate art asset.
**LACERA is the one exception in the roster requesting a BROKEN "stub" remnant
sprite** (see ASSET-014) — flagged as a minor scope note for director confirmation,
since it goes slightly beyond the generic art-bible §5.3 "sprite disappears" default.

---

## ASSET-009 — kaiju_lacera_head_core (BOSS_CORE)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, animated) |
| Part ID | `head_core` — BOSS_CORE, 48×48 px, hitbox marker ×1.2, stationary-relative to body drift |
| Dimensions | 48×48 px canvas, 4–8 frame idle-pulse sheet |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_lacera_head_core_intact_01.png` … `_0{4-8}.png` |
| Palette | Sick Yellow-Green Body `#789010` (head carapace) · Orange-Brown Segment `#885010` (jaw/mandible ring) · Blade Tip Bright Orange `#D07000` (small accent, e.g. mouth-part highlights) · Joint Deep Shadow `#385000` |

**Visual Description**: A centipede/shrimp-hybrid head, always visible per BOSS_CORE
visibility rules, nested at the center of the trunk where four limbs and the tail
converge (kaiju/02-lacera.md §4.2 adjacency diagram). Must read as clearly "the
target" even while limbs sweep across it — bright orange accent details around
mandibles/eyes help it stand out against its own sick-green body color, since it
lacks the CARAPEX core's dramatic red pulsing organ (LACERA's threat language leans
into segmented/bladed silhouette rather than a glowing core).

**Animation/State Requirements**: 4–8 frame idle pulse (8fps), synced to the shared
`body_movement.vertical_drift` (kaiju/02-lacera.md §4.3 — the head moves with the
torso's slow up/down drift, it does not animate independently beyond the idle pulse).

**Readability Constraints**: Because LACERA's limbs periodically occlude the head
(kaiju/02-lacera.md Phase 1 "四肢遮擋頻繁"), the head's silhouette must stay
recognizable from partial views — avoid a design that depends on seeing the whole
head at once to identify it as the core.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, centipede-shrimp hybrid kaiju boss head,
sick yellow-green chitin plating (#789010), orange-brown segmented jaw ring
(#885010), small bright orange mandible/eye accent highlights (#D07000), deep
olive shadow recesses (#385000), 1px solid dark warm-black outline (#1A0800),
flat color fill, no smooth gradients, transparent background, 48x48 pixel canvas,
crisp hard pixel edges, insectoid multi-limbed kaiju boss head design.
Negative prompt: no cold colors, no smooth unsegmented shapes, no red glowing core.
```

**Target Path**: `Assets/_Project/Art/Kaiju/Lacera/kaiju_lacera_head_core_intact_0{1-8}.png`
**Atlas**: `atlas_lacera`
**Status**: PENDING — awaiting API key

---

## ASSET-010 — kaiju_lacera_fore_limb (NORMAL, Sweep-Arc Animated, Mirrorable)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, animated, mirrorable) |
| Part ID | `fore_limb_left` / `fore_limb_right` — NORMAL, 16×72 px each, ±60° sweep arc @ 45°/s |
| Dimensions | 16×72 px canvas, rotation handled in-engine (single static art pose is fine — see note) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_lacera_fore_limb_intact.png` (single source; both left/right + all sweep angles derive from in-engine rotation of this one sprite around its pivot bone) |
| Palette | Sick Yellow-Green Body `#789010` (limb shaft) · Orange-Brown Segment `#885010` (joint rings) · Blade Tip Bright Orange `#D07000` (blade edge, threat accent) · Joint Deep Shadow `#385000` |

**Visual Description**: A long, segmented blade-limb — think scythe-arm — extending
from the shoulder, tapering to a sharp bright-orange blade tip. Because the part
rotates continuously in-engine around its `pivot_bone` (kaiju/02-lacera.md §4.3), this
should be authored as a **single static pose art asset** (limb extended straight out)
rather than a walk-cycle-style animation sheet — the sweeping motion itself is a
runtime rotation, not a frame-by-frame animation. Confirm with `technical-artist`
whether a 2-frame subtle "blade glint" idle overlay is wanted in addition to the
rotation.

**Animation/State Requirements**: Static single-pose sprite (rotation is
engine-driven). No telegraph-flash variant needed for the base sweep attack (Blade
Wave Barrage fires *during* the sweep itself, not from a stationary pre-fire pose) —
however, Pattern B "Convergence Burst" (kaiju/02-lacera.md §5) does have a 0.5s
charge-up moment where limb tips flash bright orange-red; spec that as a small
**tip-only** overlay/tint asset (ASSET-010b) rather than a full redraw.

**Readability Constraints**: The blade tip's bright-orange accent must stay legible
against LACERA's own body color at all rotation angles — since Unity sprite rotation
doesn't redraw pixel art cleanly at arbitrary angles, flag to `technical-artist`
whether pre-rendering discrete rotation steps (e.g. every 15°) is needed instead of
true continuous rotation, to avoid pixel-art blur/aliasing at non-cardinal angles
(this violates the "no non-integer-scaling blur" rule in spirit, art-bible §3.1).

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, long segmented scythe-like blade limb
extending from a shoulder joint, sick yellow-green shaft (#789010), orange-brown
segmented joint rings (#885010), sharp bright orange blade tip (#D07000), deep
olive shadow (#385000), 1px solid dark warm-black outline (#1A0800), flat color
fill, transparent background, 16x72 pixel canvas (tall narrow), crisp hard pixel
edges, insectoid kaiju limb design, straight extended pose for engine rotation.
Negative prompt: no cold colors, no curved/organic tentacle look, no bullet shapes.
```

### ASSET-010b — kaiju_lacera_limb_tip_chargeflash (Convergence Burst Telegraph Overlay)

| Field | Value |
|-------|-------|
| Dimensions | Small overlay covering only the blade-tip ~1/4 of the 16×72 limb (approx 16×18px) |
| Palette | Brighter orange-red variant of `#D07000` (toward `#FF4500` per art-bible's LACERA convergence-burst bullet color), 0.5s charge-up flash |
| Naming | `kaiju_lacera_limb_tip_chargeflash_01.png` … `_04.png` |
**Status**: PENDING — awaiting API key

**Target Path**: `Assets/_Project/Art/Kaiju/Lacera/kaiju_lacera_fore_limb_intact.png` (+ tip flash overlay set)
**Atlas**: `atlas_lacera`
**Status**: PENDING — awaiting API key

---

## ASSET-011 — kaiju_lacera_hind_limb (NORMAL, Sweep-Arc Animated, Mirrorable)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, animated, mirrorable) |
| Part ID | `hind_limb_left` / `hind_limb_right` — NORMAL, 14×80 px each, ±90° sweep arc @ 30°/s (wider, slower arc than fore limb) |
| Dimensions | 14×80 px canvas, single static pose (rotation engine-driven, same convention as ASSET-010) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_lacera_hind_limb_intact.png` |
| Palette | Same family as ASSET-010, but recommend a slightly longer/thinner taper to visually distinguish "hind" from "fore" at a glance even mid-sweep (silhouette variety within the same creature) |

**Visual Description**: Same design language as the fore limb but longer and
thinner, reading as the "rear pair" of blade-limbs with a wider, lazier sweep. Should
be immediately distinguishable from the fore limb in a side-by-side comparison (both
limb types are simultaneously visible on screen) — vary proportions (longer shaft,
narrower blade) rather than introducing a new color family, to keep LACERA's overall
palette cohesive.

**Animation/State Requirements**: Same as ASSET-010 (static pose, engine rotation).
No unique telegraph asset — hind limbs participate in Pattern A/C but not the
Convergence Burst charge-tip in the same way (confirm with design if hind limbs also
need the tip-flash overlay; if so, reuse ASSET-010b scaled to this limb's proportions).

**Readability Constraints**: Same rotation-blur concern as ASSET-010 — flag to
`technical-artist`.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, long thin segmented scythe-like blade limb,
longer and narrower than a companion fore-limb design, sick yellow-green shaft
(#789010), orange-brown segmented joint rings (#885010), sharp bright orange
blade tip (#D07000), deep olive shadow (#385000), 1px solid dark warm-black
outline (#1A0800), flat color fill, transparent background, 14x80 pixel canvas
(very tall, narrow), crisp hard pixel edges, insectoid kaiju limb design, straight
extended pose for engine rotation.
Negative prompt: no cold colors, no organic tentacle curves, no bullet shapes.
```

**Target Path**: `Assets/_Project/Art/Kaiju/Lacera/kaiju_lacera_hind_limb_intact.png`
**Atlas**: `atlas_lacera`
**Status**: PENDING — awaiting API key

---

## ASSET-012/013 — kaiju_lacera_tail_carapace (ARMORED — Two Physical States)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, two distinct states) |
| Part ID | `tail_carapace` — ARMORED, 32×48 px, ±30° slow oscillate @ 20°/s |
| Dimensions | 32×48 px canvas × 2 (INTACT + STRIPPED) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_lacera_tail_carapace_intact.png` (ASSET-012), `kaiju_lacera_tail_carapace_stripped.png` (ASSET-013) |
| Palette (INTACT) | Orange-Brown Segment `#885010` (thick tail plate), Sick Yellow-Green Body `#789010` (base) |
| Palette (STRIPPED) | Cracked plate revealing dark-red/olive weak point + 2px pure-white pulsing outline (shared cross-boss language, see CARAPEX file ASSET-007/008) |

**Visual Description (INTACT)**: A thick armored tail-plate/stinger-guard at the
rear of the trunk, visually "closed" with no exposed weak point, gently oscillating.

**Visual Description (STRIPPED)**: Cracked-open tail plate exposing the same
cross-boss white-outlined weak point convention.

**Animation/State Requirements**: Same INTACT↔STRIPPED transition convention as
CARAPEX's dorsal cannon (6-frame, 0.15s crack-open animation) — spec as ASSET-013b.

**Readability Constraints**: Same as all ARMORED parts — the white 2px pulsing
outline is the universal "now hittable" signal and must render identically across
all three kaiju.

**Generation Prompt (INTACT)**:
```
16-bit retro arcade pixel art sprite, thick armored tail-plate guard at the rear
of an insectoid kaiju, orange-brown segmented plating (#885010), sick
yellow-green base accents (#789010), no visible weak points, deep olive shadow
(#385000), 1px solid dark warm-black outline (#1A0800), flat color fill,
transparent background, 32x48 pixel canvas, crisp hard pixel edges.
Negative prompt: no cold colors, no cracks or openings, no red glow.
```

**Generation Prompt (STRIPPED)**:
```
16-bit retro arcade pixel art sprite, the same tail-plate guard cracked open,
jagged broken orange-brown plate edges (#885010), exposing a dark reddish-olive
organic weak-point core in the newly opened cavity, rimmed with a bright pure
white pulsing 2px outline (#FFFFFF), deep olive shadow (#385000), 1px solid dark
warm-black outline on the outer plating (#1A0800), flat color fill, transparent
background, 32x48 pixel canvas, crisp hard pixel edges.
Negative prompt: no cold colors except the white weak-point outline, no smooth
unbroken plating.
```

### ASSET-013b — kaiju_lacera_tail_carapace_crack_open (Transition Animation)
Same convention as ASSET-007b (CARAPEX): 6-frame, 0.15s, 32×48px, commissioned after
012/013 approval.

**Target Path**: `Assets/_Project/Art/Kaiju/Lacera/kaiju_lacera_tail_carapace_{intact|stripped}.png`
**Atlas**: `atlas_lacera`
**Status**: PENDING — awaiting API key

---

## ASSET-014 — kaiju_lacera_limb_stub_broken (BROKEN Remnant — LACERA-Specific Enhancement)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, post-break remnant) |
| Applies to | `fore_limb_left/right`, `hind_limb_left/right` (NORMAL limbs only, not the tail or head) |
| Dimensions | ~14–16 × 20 px (a short stub, roughly the base 1/4 of the full limb) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_lacera_limb_stub_broken.png` |
| Palette | Same limb palette, but darker/duller (no bright blade-tip orange — the blade itself is gone), with visible jagged break-edge highlight |

**Visual Description**: A short broken stump remaining at the shoulder/hip socket
after a limb is destroyed, per the LACERA GDD's explicit visual note (§3: "肢體爆裂後
殘留刃基殘骸(stub)，尖端像素碎片散落，軀幹不對稱傾斜"). This is a deliberate
LACERA-specific enhancement — the "creature is visibly losing pieces" physical-damage
storytelling that the design calls out as reinforcing Pillar 4 (Breaking is the
Reward).

> **Flag for director**: This is a minor deviation from the generic art-bible §5.3
> "BROKEN state = sprite disappears entirely" rule. It's not a contradiction (the
> *original* limb sprite does disappear — the stub is a small *new* asset that
> replaces it, not a retained fragment of the original), but the art bible's blanket
> §5.3 language doesn't currently carve out this exception. Recommend either (a)
> approving this as a documented per-kaiju art variance, or (b) formally adding a
> "some parts may spawn a post-break remnant sprite" clause to art-bible §5.3 so
> future kaiju content doesn't have to re-litigate this. Low stakes, but worth a
> one-line director sign-off before production.

**Readability Constraints**: The stub must be visually inert (no pulsing, no bright
accents) so it reads unambiguously as "dead weight," never mistaken for a still-live
threat.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, short broken limb stump/stub remaining at a
shoulder socket after a blade-limb has been severed, dull sick yellow-green
(#789010) and orange-brown (#885010) tones darkened toward the shadow value
(#385000), jagged torn break-edge with a slightly lighter fracture highlight, no
bright blade-tip color remaining, 1px solid dark warm-black outline (#1A0800),
flat color fill, transparent background, ~16x20 pixel canvas, crisp hard pixel
edges, visibly inert/dead appearance.
Negative prompt: no bright orange blade tip, no glow, no pulsing highlight
implied, no signs of remaining threat.
```

**Target Path**: `Assets/_Project/Art/Kaiju/Lacera/kaiju_lacera_limb_stub_broken.png`
**Atlas**: `atlas_lacera`
**Status**: PENDING — awaiting API key (pending director sign-off on the flag above)

---

*Spec file version: 1.0.0 — Art Director Agent — 2026-07-05*
