# Asset Specs — Material / Core Icons

> **Source**: `design/gdd/material-economy.md` §C.1, `design/registry/entities.yaml`
> **Art Bible**: `design/art-bible.md` §2.1 (Material Orb Teal, locked), §5.4 (per-kaiju color direction), §9.1 (naming)
> **Generated**: 2026-07-05
> **Status**: 5 assets specced / 0 approved / 0 in production / 0 done
> **Target root**: `Assets/_Project/Art/UI/HUD/` (small HUD-scale icon variant) and shared with Meta screens (future pass)

These five icons are the canonical visual identity for the five materials in
`design/registry/entities.yaml` (`shard_common`, `core_carapace`, `core_limb`,
`core_energy`, `essence_kaiju`). Each icon must be legible at a small HUD counter
size (recommend a 12×12px base canvas, matching the material-counter's compact
footprint per `hud-ui-system.md` §D.3) as well as scaling cleanly to a larger Meta
screen size later (out of this pass's scope, but the same source art should scale by
integer factor per art-bible §3.1).

---

## ASSET-048 — icon_shard_common (通用碎片 / Common Shard)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Icon) |
| Dimensions | 12×12 px |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `icon_shard_common.png` |
| Palette | Material Orb Teal `#62F0D8` — **this exact value is locked by `game-feel.md`, "不可更改" (art-bible §2.1)** — with a small `#FFFFFF` highlight facet |
| Behavior | This icon is also the sprite used for the in-flight "Material Homing Orb" that arcs from a broken part to the HUD counter (game-feel.md/art-bible §08 "素材軌道球") — same asset, two use contexts (world-space collectible + HUD icon) |

**Visual Description**: A small angular crystal-shard shape (2–3 facets suggested by
internal edge lines) in the game's signature teal-green — the one deliberately
cold-adjacent color used for "harvest/reward" semantics, distinct from both the
player's blue and the enemy's warm palette (art-bible §01 exception note). Simple,
immediately legible silhouette — this is the single most frequently-seen collectible
in the game.

**Readability Constraints**: Must remain visually distinct from the Material Orb
Teal value's neighbors on the cold side of the palette (Laser Core Cyan `#40F8FF`,
Missile Cold Blue `#70C8F0`) at a glance — while teal is close to cyan, the icon's
distinct crystal-shard silhouette (vs. lasers' line shape or missiles' oval shape)
should carry the disambiguation per the colorblind-safe redundancy table (art-bible
§4.3, "拋物線軌跡飛向計數器" behavioral cue as the primary redundant signal).

**Generation Prompt**:
```
16-bit retro arcade pixel art icon, small angular crystal shard, teal-green
color (#62F0D8) with a small bright white facet highlight (#FFFFFF), 2-3
internal facet edge lines, 1px darker teal outline, flat color fill, no smooth
gradient, transparent background, 12x12 pixel canvas, crisp hard pixel edges,
simple collectible-gem icon design.
Negative prompt: no warm colors, no round/blob shape (must read as angular
crystal), no blur.
```

**Target Path**: `Assets/_Project/Art/UI/HUD/icon_shard_common.png`
**Atlas**: `atlas_ui`
**Status**: PENDING — awaiting API key

---

## ASSET-049 — icon_core_carapace (甲殼核心 / Carapace Core)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Icon) |
| Dimensions | 12×12 px |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `icon_core_carapace.png` |
| Palette | Drawn from CARAPEX's own theme (art-bible §5.4): Amber Carapace `#B87020` base, Core Dark Red Pulse `#800010` inner accent |
| Thematic Tie | Sourced exclusively by CARAPEX; upgrades L1/M2/M4 (`material-economy.md` §C.1) |

**Visual Description**: A small hexagonal or plated-shell-shaped core icon in
CARAPEX's amber/rust palette with a small dark-red pulse-core dot at its center —
visually a miniature callback to ASSET-005's chest reactor core design, so players
subconsciously associate this icon with "the CARAPEX boss" even without reading text.

**Generation Prompt**:
```
16-bit retro arcade pixel art icon, small hexagonal plated core/shell shape,
amber-orange coloring (#B87020) with a small dark red glowing dot at the center
(#800010), 1px dark brown outline, flat color fill, transparent background,
12x12 pixel canvas, crisp hard pixel edges, callback to a crustacean/carapace
boss theme.
Negative prompt: no cold colors, no smooth round blob shape.
```

**Target Path**: `Assets/_Project/Art/UI/HUD/icon_core_carapace.png`
**Atlas**: `atlas_ui`
**Status**: PENDING — awaiting API key

---

## ASSET-050 — icon_core_limb (四肢核心 / Limb Core)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Icon) |
| Dimensions | 12×12 px |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `icon_core_limb.png` |
| Palette | Drawn from LACERA's theme: Sick Yellow-Green Body `#789010`, Blade Tip Bright Orange `#D07000` accent |
| Thematic Tie | Sourced exclusively by LACERA; upgrades L2/L4/M1 |

**Visual Description**: A small claw/blade-fragment-shaped icon (echoing LACERA's
limb silhouette) in sick yellow-green with a bright orange blade-tip accent.

**Generation Prompt**:
```
16-bit retro arcade pixel art icon, small curved claw or blade-fragment shape,
sick yellow-green coloring (#789010) with a bright orange tip accent (#D07000),
1px dark olive outline, flat color fill, transparent background, 12x12 pixel
canvas, crisp hard pixel edges, callback to an insectoid limb-boss theme.
Negative prompt: no cold colors, no round blob shape.
```

**Target Path**: `Assets/_Project/Art/UI/HUD/icon_core_limb.png`
**Atlas**: `atlas_ui`
**Status**: PENDING — awaiting API key

---

## ASSET-051 — icon_core_energy (能量核心 / Energy Core)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Icon) |
| Dimensions | 12×12 px |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `icon_core_energy.png` |
| Palette | Drawn from VOLTWYRM's theme: Energy Arc Yellow `#FFE860`, Core Extreme White-Yellow `#FFFFF0` inner highlight |
| Thematic Tie | Sourced exclusively by VOLTWYRM; upgrades L3/M3 |

**Visual Description**: A small glowing orb/spark icon (echoing VOLTWYRM's core node
design, ASSET-015) in bright energy-yellow with a near-white hot center — the
brightest of the three core icons, matching VOLTWYRM's status as "visually brightest
boss" in the roster.

**Generation Prompt**:
```
16-bit retro arcade pixel art icon, small glowing energy orb/spark shape, bright
yellow coloring (#FFE860) with a near-white hot inner highlight (#FFFFF0), 1px
dark outline, flat color fill (2-3 discrete brightness bands, no smooth blur),
transparent background, 12x12 pixel canvas, crisp hard pixel edges, callback to
a plasma-energy boss theme.
Negative prompt: no cold colors, no organic/plated texture.
```

**Target Path**: `Assets/_Project/Art/UI/HUD/icon_core_energy.png`
**Atlas**: `atlas_ui`
**Status**: PENDING — awaiting API key

---

## ASSET-052 — icon_essence_kaiju (巨獸精魄 / Kaiju Essence)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Icon) |
| Dimensions | 12×12 px (larger celebratory variant may be wanted for the Results screen — flagged as future work, out of this pass) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `icon_essence_kaiju.png` |
| Palette | Deliberately **not** tied to any single boss theme (it's the rarest, "full-clear only" material) — recommend a blend echoing all three cores: a soft prismatic mix leaning toward the Boss-Death VFX gold-white (`#FFFFF0`/`#FFE860`, art-bible §08) to signal "ultimate/completion" tier without favoring one boss |
| Thematic Tie | Universal — Tier 2→3 upgrade material for all 8 weapons |

**Visual Description**: A small glowing wisp/flame-drop shape, rendered in the same
gold-white palette as the Boss Death Burst VFX (ASSET-023) to visually tie "getting
this material" to "the most triumphant moment in the game" (a full-clear kill). This
is the rarest, most prestigious icon in the set and should read as visibly "more
special" than the three core icons — brighter, with perhaps a subtle sparkle accent.

**Generation Prompt**:
```
16-bit retro arcade pixel art icon, small glowing wisp or flame-drop shape,
radiant gold-white coloring (#FFE860 body, #FFFFF0 bright core highlight), a
tiny sparkle/star accent detail, 1px dark outline, flat color fill (3-4
brightness bands for extra shine), transparent background, 12x12 pixel canvas,
crisp hard pixel edges, designed to read as the rarest/most prestigious material
icon in a set of five.
Negative prompt: no cold colors, no single-boss-specific color scheme (must
read as boss-agnostic/universal).
```

**Target Path**: `Assets/_Project/Art/UI/HUD/icon_essence_kaiju.png`
**Atlas**: `atlas_ui`
**Status**: PENDING — awaiting API key

---

*Spec file version: 1.0.0 — Art Director Agent — 2026-07-05*
