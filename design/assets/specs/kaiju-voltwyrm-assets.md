# Asset Specs — Kaiju: VOLTWYRM (熾蛇 / #03, 能量系)

> **Source**: `design/gdd/kaiju/03-voltwyrm.md`, `design/gdd/kaiju-part-system.md`
> **Art Bible**: `design/art-bible.md` §3.2, §5.1–5.4, §2.4 (shield cold-color exception)
> **Generated**: 2026-07-05
> **Status**: 4 assets specced / 0 approved / 0 in production / 0 done
> **Target root**: `Assets/_Project/Art/Kaiju/Voltwyrm/`

**Silhouette contract (art-bible §5.2)**: vertical snake/pillar, stacked segments
top-to-bottom, shields flanking the core at the top. Screen occupancy: ~25% width,
70–80% height (~80px wide × ~336–384px tall) — the tallest, narrowest of the three
silhouettes, reinforcing its "vertical pierce corridor" design purpose
(`weapon-system.md` §E.3, L4's showcase boss).

**State-machine note**: same INTACT/SOFTENED/BROKEN convention as CARAPEX/LACERA. No
stub-remnant request in VOLTWYRM's GDD — sprite disappears on BROKEN per the generic
art-bible §5.3 rule (segments are absorbed into the vertical corridor gap, no stub
needed).

**Special case — shields use the cold-color exception (art-bible §2.4)**: `shield_left`
/ `shield_right` are the **only** ARMORED parts in the entire roster whose INTACT
state uses a cold color family (`#503090` deep purple-blue) instead of warm — this is
a deliberate, tightly-scoped exception to stop large static shields from being
misread as enemy bullets in a dense orange/gold bullet field. Once STRIPPED, they
convert to warm (`#FF7020`) like every other ARMORED part. Do not generalize this
exception to any other part.

---

## ASSET-015 — kaiju_voltwyrm_core_node (BOSS_CORE)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, animated) |
| Part ID | `core_node` — BOSS_CORE, 48×48 px, top of the vertical column, continuously rotating halo |
| Dimensions | 48×48 px canvas, continuous rotation animation (art-bible §3.5: "持續旋轉光環動畫") — recommend an 8-frame rotation loop |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_voltwyrm_core_node_01.png` … `_08.png` |
| Palette | Core Extreme White-Yellow `#FFFFF0` (innermost, brightest point in the whole game per art-bible §5.4: "BOSS_CORE 視覺最亮") · Energy Arc Yellow `#FFE860` (halo ring) · Body Edge Orange-Red `#FF9020` (outer glow edge) |

**Visual Description**: The single brightest element in the entire game — an
almost-white-hot energy core at the very top of VOLTWYRM's column, wrapped in a
continuously spinning halo ring of yellow energy arcs. Unlike CARAPEX's organic
red-pulse core or LACERA's segmented insect head, this core should read as pure
energy/plasma — no plating, no organic texture, just layered concentric light. The
rotating halo is a signature visual (referenced directly in the GDD's player-fantasy
narrative as "the most spectacular visual payoff moment in the game" when hit by L4).

**Animation/State Requirements**: Continuous 8-frame halo rotation loop (not a pulse —
a true rotation, distinct from the pulse-idle convention used by CARAPEX/LACERA
cores). SOFTENED overlay and BROKEN explosion follow the shared convention (see
`kaiju-break-vfx-assets.md`), but this core's explosion on death is explicitly the
biggest VFX moment in the game (art-bible §08: "Boss 死亡金白爆射" — 110 particles,
`#FFFFF0` + `#FFE860`) — that VFX asset already covers the death moment; this sprite
spec is for the living core only.

**Readability Constraints**: Must remain visually distinct from any player element —
despite being "the brightest thing on screen," its rotating-halo motion signature and
position (fixed at the top of a vertical snake, never near the player's y-position)
prevent confusion with the player's cold-blue elements.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, pure plasma/energy kaiju boss core, almost
white-hot innermost glow (#FFFFF0), surrounded by a rotating ring of yellow
energy arcs (#FFE860), outer glow edge fading to orange-red (#FF9020), no organic
texture or plating, layered concentric light rings, 1px solid dark warm-black
outline on the outermost silhouette (#1A0800), flat color bands (2-3 discrete
brightness steps, no smooth gradient blur), transparent background, 48x48 pixel
canvas, crisp hard pixel edges, sci-fi energy-serpent boss core design.
Negative prompt: no cold colors (no blue/cyan), no organic/biological texture,
no plating or armor, no bullet-like round shapes.
```

**Target Path**: `Assets/_Project/Art/Kaiju/Voltwyrm/kaiju_voltwyrm_core_node_0{1-8}.png`
**Atlas**: `atlas_voltwyrm`
**Status**: PENDING — awaiting API key

---

## ASSET-016/017 — kaiju_voltwyrm_shield (ARMORED — Cold-Color Exception, Mirrorable)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, two distinct states, mirrorable) |
| Part ID | `shield_left` / `shield_right` — ARMORED, 40×56 px each, hexagonal energy-lattice |
| Dimensions | 40×56 px canvas × 2 states (INTACT + STRIPPED); left/right mirror from one source |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_voltwyrm_shield_intact.png` (ASSET-016), `kaiju_voltwyrm_shield_stripped.png` (ASSET-017) |
| Palette (INTACT — cold exception) | Shield Deep Purple-Blue `#503090` (main lattice, semi-transparent) · Shield Inner Purple `#302080` (inner glow) — **cold family, art-bible §2.4 exception** |
| Palette (STRIPPED — reverts to warm) | Shield Exposed Orange `#FF7020` (crack overlay), same white 2px pulsing weak-point outline convention as all other ARMORED parts |

**Visual Description (INTACT)**: A hexagonal, semi-transparent energy-lattice shield
plate flanking the core, deliberately cold-toned (deep purple-blue) so it reads as "a
large static structure to interact with" rather than "a bullet to dodge" — this is the
single most important color-exception in the entire palette and must be treated with
care; do not let it drift warm even slightly, or it defeats its own stated purpose
(art-bible §2.4).

**Visual Description (STRIPPED)**: The lattice cracks and the exposed cracks glow
warm orange (`#FF7020`) — this is the moment the shield "joins" the warm=threat
language, signaling its weak point is now open. The 2px white pulsing outline
(shared cross-boss convention) appears around the crack.

**Animation/State Requirements**: INTACT can have a subtle semi-transparent
shimmer/pulse (slow, cold-toned, distinct from the SOFTENED 2Hz warm pulse elsewhere
in the game — must not be visually confusable with SOFTENED). STRIPPED transition:
6-frame crack animation (ASSET-016b), same convention as other ARMORED parts.

**Readability Constraints**: art-bible AC-ART-04 explicitly requires a 5-person test
showing ≥90% correct "shield vs. bullet" identification — this is the highest bar of
any single readability acceptance criterion in the whole art bible, reflecting how
much rides on this one color exception working correctly.

**Generation Prompt (INTACT)**:
```
16-bit retro arcade pixel art sprite, hexagonal semi-transparent energy-lattice
shield plate, deep purple-blue coloring (#503090 outer lattice, #302080 inner
glow), geometric hex-grid pattern, cold sci-fi energy-barrier aesthetic, 1px
solid dark outline (#1A0800), flat color fill with slight transparency
implied by lighter hex-cell centers, no smooth gradient blur, transparent
background, 40x56 pixel canvas, crisp hard pixel edges, clearly a large static
structure (not a projectile).
Negative prompt: no warm colors, no round/oval bullet-like shapes, no organic
texture.
```

**Generation Prompt (STRIPPED)**:
```
16-bit retro arcade pixel art sprite, the same hexagonal energy-lattice shield
now cracked open, jagged fracture lines glowing warm orange (#FF7020), the
crack rimmed with a bright pure white pulsing 2px outline (#FFFFFF), remaining
intact lattice sections still deep purple-blue (#503090), 1px solid dark outline
(#1A0800), flat color fill, transparent background, 40x56 pixel canvas, crisp
hard pixel edges.
Negative prompt: no fully-warm shield (retain some purple-blue remnants), no
smooth unbroken lattice.
```

### ASSET-016b — kaiju_voltwyrm_shield_crack_open (Transition Animation)
Same convention as prior crack-open transitions: 6-frame, 0.15s, 40×56px.

**Target Path**: `Assets/_Project/Art/Kaiju/Voltwyrm/kaiju_voltwyrm_shield_{intact|stripped}.png`
**Atlas**: `atlas_voltwyrm`
**Status**: PENDING — awaiting API key

---

## ASSET-018 — kaiju_voltwyrm_neck_seg (NORMAL, Reused ×4 Instances)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Kaiju Part, animated, single design × 4 instances) |
| Part ID | `neck_seg_1` through `neck_seg_4` — NORMAL, 48×32 px each, stacked vertically to form the Vertical Pierce Corridor (`weapon-system.md` §E.3) |
| Dimensions | 48×32 px canvas, animated energy-pulse flow (art-bible §3.5: "能量脈衝流動動畫") |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `kaiju_voltwyrm_neck_seg_01.png` … `_0{4-8}.png` (single shared design, instanced 4× in the scene, per kaiju/03-voltwyrm.md §4.1 "四個縱列頸段") |
| Palette | Energy Arc Yellow `#FFE860` (flowing pulse core) · Body Edge Orange-Red `#FF9020` (segment boundary/edge) |

**Visual Description**: A horizontal band segment of the serpent's body, with a
visible internal energy current flowing (upward or downward — confirm direction with
`technical-artist`, likely flowing toward the core to visually reinforce "feeding the
reactor") through its center, edged in orange-red. All four instances share this one
design (no per-segment variation needed — the GDD treats them as functionally and
visually identical, differentiated only by position). When SOFTENED (shader overlay,
shared convention), all four segments pulsing orange-red in sync is the single
biggest "L4 landed on all four at once" visual payoff in the game
(kaiju/03-voltwyrm.md §2: "所有頸段同步脈動，是遊戲中視覺回饋最壯觀的『蓄熱全中』時刻").

**Animation/State Requirements**: 4–8 frame flowing-energy idle loop (8fps, per
art-bible general idle cadence — confirm whether this part deserves a faster/more
fluid loop given its "flowing" description; default to the standard cadence unless
`technical-artist` flags a performance reason to reduce frame count).

**Readability Constraints**: Segments must read as clearly stacked/aligned along a
single vertical axis even amid VOLTWYRM's S-curve body drift (kaiju/03-voltwyrm.md
§6 Phase 1 "緩慢 S 形漂移") — the segment boundaries (orange-red edge) should stay
crisp so players can visually confirm vertical alignment for the L4 corridor shot.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, horizontal segment of a serpent kaiju's
body, glowing yellow energy current flowing through the segment's center
(#FFE860), orange-red segment boundary edges (#FF9020), sci-fi energy-serpent
body-ring design, 1px solid dark warm-black outline (#1A0800), flat color bands
(no smooth gradient blur), transparent background, 48x32 pixel canvas (wide,
short), crisp hard pixel edges, clearly stackable/tileable top-to-bottom design.
Negative prompt: no cold colors, no organic scale texture, no bullet shapes.
```

**Target Path**: `Assets/_Project/Art/Kaiju/Voltwyrm/kaiju_voltwyrm_neck_seg_0{1-8}.png`
**Atlas**: `atlas_voltwyrm`
**Status**: PENDING — awaiting API key

---

*Spec file version: 1.0.0 — Art Director Agent — 2026-07-05*
