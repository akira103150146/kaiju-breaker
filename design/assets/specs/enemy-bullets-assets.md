# Asset Specs — Enemy Bullet Sprites (6-Color Sub-Palette)

> **Source**: `design/art-bible.md` §2.2.1, §4.1–4.3, `design/gdd/difficulty-system.md`, `design/gdd/enemy-tier-system.md` §D.4
> **Art Bible**: `design/art-bible.md` §2.2.1 (Enemy Bullet Sub-palette — hard constraint), §4 (Bullet & Readability Spec)
> **Generated**: 2026-07-05
> **Status**: 12 assets specced / 0 approved / 0 in production / 0 done
> **Target root**: `Assets/_Project/Art/Bullets/Enemy/`

**Hard constraint (art-bible §4.2, non-negotiable)**: every enemy bullet must use one
of the **six colors** below, a 1px pure-black (`#000000`) outline, and one of two
shapes only — round dot (4–6px) or short oval (4×6px). No enemy bullet may use a
thin-line or long-oval silhouette (that shape language is reserved for player
lasers/missiles, art-bible §4.3 colorblind-safe redundancy table).

---

## The Six Approved Colors (art-bible §2.2.1)

| Color | Hex | Boss/Pattern Association |
|-------|-----|---------------------------|
| Bullet Primary Orange | `#FF8000` | CARAPEX mandible cross-fire; standard baseline enemy bullet |
| Bullet Secondary Yellow | `#FFCC00` | CARAPEX dorsal gravel; VOLTWYRM energy bullets; high-threat/large patterns |
| Bullet Dark Red | `#CC2200` | CARAPEX core pulse; highest-threat/core-exclusive |
| Bullet Bright Orange | `#FF8C00` | LACERA blade wave barrage main bullet |
| Bullet Orange-Red | `#FF4500` | LACERA convergence burst; high-density cluster |
| Bullet Energy White-Gold | `#FFF0A0` | VOLTWYRM outermost spiral-arm energy bullet (highest compression) |

---

## ASSET-036 — bullet_orange_round_small (Full Template — Round Shape)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Enemy Bullet) |
| Dimensions | 4–6 px round dot (art-bible §3.2/§4.2) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `bullet_orange_round_small.png` |
| Palette | Bullet Primary Orange `#FF8000` base · brighter inner highlight (1–2px, e.g. toward `#FFB040`) per art-bible §4.2 "內核高亮" · **1px pure black outline `#000000`, mandatory, never reduced or omitted** |
| Telegraph Variant | A brighter version of this same sprite (toward `#FFB040` base, per art-bible §4.2: "電報顏色為對應彈色的更亮版本") used during the `charge_telegraph_s` pre-fire window — implement as a runtime brightness/tint shift of this same sprite rather than a separate baked asset (recommend to `technical-artist`; keeps asset count from doubling) |
| Animation | None — "形狀即信號，無動畫需求" (art-bible §3.5) — enemy bullets are single static frames |

**Visual Description**: A small solid orange circle with a slightly brighter
off-center highlight near the top-left (simulating a light source, adding a touch of
dimensionality without any gradient blur — use 2-3 discrete color bands) and a crisp
1px pure-black outline. This is the most common enemy bullet in the game (CARAPEX's
tutorial-level mandible fire) so its silhouette essentially defines "this is an enemy
bullet" for a first-time player.

**Readability Constraints**: This is the anchor test case for art-bible AC-ART-01 —
must be distinguishable from the player's cold-blue projectiles by color AND shape
even under D4 maximum bullet density. Must remain crisp at ×1 integer scale on the
smallest supported phone screen.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, small solid round bullet, orange fill
(#FF8000) with a brighter orange-yellow highlight spot near the top-left
(#FFB040), 1px solid pure black outline (#000000), flat color fill only (2-3
discrete color bands, no smooth gradient, no anti-aliasing), transparent
background, 5x5 pixel canvas (round dot shape), crisp hard pixel edges,
Cave/Touhou-style bullet-hell enemy projectile aesthetic.
Negative prompt: no cold colors, no oval/elongated shape, no blur, no soft
edges, no missing outline.
```

**Target Path**: `Assets/_Project/Art/Bullets/Enemy/bullet_orange_round_small.png`
**Atlas**: `atlas_enemy_bullets`
**Status**: PENDING — awaiting API key

---

## ASSET-037 — bullet_orange_oval_small (Full Template — Oval Shape)

Same color/outline/highlight rules as ASSET-036, applied to the alternate
short-oval silhouette (4×6px, art-bible §4.2). Used where a design calls for a
"short-flight aimed shot" reading distinct from a "spray of dots" reading (e.g.
`aimed_gun`'s single aimed shot per stage-system.md §E.1).

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, small solid short-oval bullet (elongated
vertically), orange fill (#FF8000) with a brighter highlight spot (#FFB040),
1px solid pure black outline (#000000), flat color fill, transparent background,
4x6 pixel canvas, crisp hard pixel edges.
Negative prompt: no cold colors, no long thin line shape (must stay a short
oval, not a laser-like line), no blur.
```

**Target Path**: `Assets/_Project/Art/Bullets/Enemy/bullet_orange_oval_small.png`
**Atlas**: `atlas_enemy_bullets`
**Status**: PENDING — awaiting API key

---

## Remaining 5 Colors × 2 Shapes (Compact Stub Table)

Same construction rules as ASSET-036/037 (1–2px brighter inner highlight, 1px pure
black outline, no animation, static telegraph variant via runtime brightness shift).

| ID | Name | Hex | Shape | Generation Prompt (compact) |
|----|------|-----|-------|------------------------------|
| ASSET-038 | `bullet_yellow_round_small` | `#FFCC00` | Round 5px | 16-bit pixel art round bullet, yellow fill (#FFCC00), brighter highlight (#FFE060), 1px black outline, 5x5px, flat color, hard edges, transparent bg. |
| ASSET-039 | `bullet_yellow_oval_small` | `#FFCC00` | Oval 4×6px | Same as above, short-oval silhouette, 4x6px. |
| ASSET-040 | `bullet_darkred_round_small` | `#CC2200` | Round 5px | 16-bit pixel art round bullet, dark red fill (#CC2200), brighter highlight (#FF5030), 1px black outline, 5x5px, flat color, hard edges, transparent bg. |
| ASSET-041 | `bullet_darkred_oval_small` | `#CC2200` | Oval 4×6px | Same as above, short-oval silhouette, 4x6px. |
| ASSET-042 | `bullet_brightorange_round_small` | `#FF8C00` | Round 5px | 16-bit pixel art round bullet, bright orange fill (#FF8C00), brighter highlight (#FFB050), 1px black outline, 5x5px, flat color, hard edges, transparent bg. |
| ASSET-043 | `bullet_brightorange_oval_small` | `#FF8C00` | Oval 4×6px | Same as above, short-oval silhouette, 4x6px. |
| ASSET-044 | `bullet_orangered_round_small` | `#FF4500` | Round 5px | 16-bit pixel art round bullet, orange-red fill (#FF4500), brighter highlight (#FF7040), 1px black outline, 5x5px, flat color, hard edges, transparent bg. |
| ASSET-045 | `bullet_orangered_oval_small` | `#FF4500` | Oval 4×6px | Same as above, short-oval silhouette, 4x6px. |
| ASSET-046 | `bullet_energywhitegold_round_small` | `#FFF0A0` | Round 5px | 16-bit pixel art round bullet, pale white-gold fill (#FFF0A0), brighter near-white highlight (#FFFFF0), 1px black outline, 5x5px, flat color, hard edges, transparent bg. |
| ASSET-047 | `bullet_energywhitegold_oval_small` | `#FFF0A0` | Oval 4×6px | Same as above, short-oval silhouette, 4x6px. |

**Target Path**: `Assets/_Project/Art/Bullets/Enemy/bullet_*.png`
**Atlas**: `atlas_enemy_bullets` (single atlas for all 12 — art-bible §9.4 explicitly
calls this out as the batching-optimization atlas for GPU instancing, `bullet-system.md`
§5.4)
**Status**: PENDING — awaiting API key

---

## Difficulty-Tier Density Readability Note (Not a New Asset)

Per `difficulty-system.md` and `enemy-tier-system.md` §D.4, the four difficulty tiers
(D1–D4) and the Trash/Elite tier axis both scale **bullet count and fire rate only**
— never color, size, or outline weight (art-bible §4.2 hard spec is difficulty-
invariant by design). This means **all 12 sprites above are used unmodified across
every difficulty tier and every enemy tier**; there is no separate "D4 bullet" or
"Elite bullet" art asset to commission. The only readability risk at high density
(confirmed by `enemy-tier-system.md` E.3, theoretical max ~40 bullets/burst at Elite×D4)
is compositional/quantity-driven, not a sprite-design problem — it is mitigated at the
design/tuning layer (density knobs), not by drawing new sprites. No action needed from
the art side beyond ensuring the sprite atlas batches cleanly at high instance counts
(a `technical-artist`/`unity-specialist` performance concern, not an art content gap).

---

## Open Inconsistency Flag — Trash-Mob Body Color vs. Bullet Palette

`design/gdd/stage-system.md` §E.1 (Trash Enemy Roster) assigns each of the 10 trash
mobs a "主色" (primary color) hex value for their **body sprites** — e.g. `ram_grub`
`#FF8800`, `tri_shot` `#FFAA00`, `aimed_gun` `#FF5500`, `shield_flier` `#CC4400`, etc.
**None of these hex values appear in art-bible's approved palette anywhere** — not in
the six-color enemy-bullet sub-palette (§2.2.1) and not in any kaiju body palette
(§2.2.2–2.2.4). The art bible's stated design law (§02: "所有正式素材的顏色必須從本節
列出的色盤中選取；未列入者不得出現") technically forbids these values as written.

This spec deliberately does **not** invent trash-mob body sprite specs in this MVP
pass (out of this task's stated scope — the task only asked for enemy *bullet*
sprites, not mob bodies), but flags this for the director/next asset-spec pass:

- **Option A**: Formally extend art-bible §02 with a new "Trash/Elite Mob Body
  Sub-Palette" section, either adopting stage-system.md's existing hex values as
  official canon, or replacing them with values drawn from/adjacent to the existing
  six-color bullet palette (tighter, more consistent with the "limited arcade
  palette" philosophy stated in art-bible §02's own opening line).
- **Option B**: Treat stage-system.md's colors as non-binding placeholder prose
  (they read as narrative flavor text, not a locked spec) and let the eventual mob
  body art pass pick real values fresh from the existing approved palette.

**Recommendation**: Option A, reusing the existing bullet-palette hexes for mob
bodies where plausible (e.g. `ram_grub` → Bullet Primary Orange `#FF8000` instead of
its currently-written `#FF8800`) keeps the total color budget small, which is the
entire stated purpose of the "Limited Arcade Palette" system. This does not block the
current MVP pass (no mob body sprites are being commissioned yet) but should be
resolved before a future asset-spec pass covers the 10-mob roster.

---

*Spec file version: 1.0.0 — Art Director Agent — 2026-07-05*
