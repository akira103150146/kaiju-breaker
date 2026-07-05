# Asset Specs — Weapon Projectiles & Hit VFX (L1–L4, M1–M4)

> **Source**: `design/gdd/weapon-system.md` (LOCKED), `design/decisions/2026-07-03-director-decisions.md`
> **Art Bible**: `design/art-bible.md` §2.1 (Cold Family), §3.2 (Player Projectile sizes), §08 (VFX)
> **Generated**: 2026-07-05
> **Status**: 10 assets specced (2 full detail + 8 stubbed) / 0 approved / 0 in production / 0 done
> **Target root**: `Assets/_Project/Art/Bullets/Player/` (projectiles), `Assets/_Project/Art/VFX/HitSparks/` (hit VFX)

**All player weapon assets are cold-family only** (art-bible §2.1) — this is
non-negotiable per Art Law #1. No warm color may appear on any player projectile,
including "hot" weapon concepts like L3's charge or M3's torpedo — heat/impact is
communicated via brightness and saturation shifts *within* the cold family (e.g.
brighter cyan for a charged shot), never a hue shift toward orange/red.

**Priority note (flagged for director)**: this task's brief calls out **L1 散波雷射 +
M2 蜂群飛彈** as the MVP full-detail pair. Cross-referencing `stage-system.md` §F.4/G.1,
Stage 1's actual designed weapon pod pool is **L1+L2 (primary) / M1+M3 (secondary)**,
and CARAPEX's own GDD "展示 Loadout" is **L2×M3**, not L1×M2. All three docs are
internally consistent with each other (stage-system + CARAPEX agree on L2×M3 as the
tutorial showcase); only this asset-spec task's stated priority pair (L1+M2) diverges
from that showcase pairing. This isn't a contradiction in the game design itself —
L1 and M2 are still real, needed MVP weapons — but if art-generation budget is tight
and only one pair can be fully polished first, the director should confirm whether
**L1+M2** (this task's instruction) or **L2+M3** (the design docs' own tutorial
showcase pair) is the actual priority. I've specced L1+M2 in full per this task's
explicit instruction and stubbed the rest; flip the priority easily by promoting the
L2+M3 stub entries below to full detail if the director prefers to match the GDDs.

---

## FULL DETAIL — MVP Pair (per task instruction)

### ASSET-025 — player_laser_l1_spread (L1 散波雷射 / Spread Laser)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Player Projectile) |
| Dimensions | 2×8 px per beam segment (art-bible §3.2), 3 beams fired simultaneously in a fan (Tier-3 expands to 4 beams — director decision: "先用簡單 sprite scale 佔位" for the beam-count visual growth, i.e. reuse this same beam asset, just draw more of them, no new art per tier) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `player_laser_l1_beam_segment.png` (single tileable segment, engine-stretched to beam length) |
| Palette | Laser Core Cyan `#40F8FF` (beam core) → Laser Glow Blue `#80E8FF` (outer glow fade) · Outline `#103060` (1px cold-blue, art-bible §3.3) |
| Animation/State | Static sprite (no frame animation) — "firing" is communicated by spawn/despawn rate and the muzzle-flash VFX (out of scope for this file, HUD/muzzle FX are a `technical-artist` shader task), not a beam animation |

**Visual Description**: A short, thin vertical energy-line segment — bright cyan
core with a soft blue glow bleeding outward — designed to be stretched/tiled by the
engine into a full-length beam. Three of these fire simultaneously in a fan pattern
(±angle spread per weapon-system.md §C.4), each rendered independently so their
individual outlines stay crisp even where beams cross near the muzzle.

**Readability Constraints**: Must be instantly distinguishable from enemy bullets by
shape alone (thin line vs. round/oval, art-bible §4.1 three-element system) even if
color perception fails entirely (colorblind-safe redundancy, art-bible §4.3). Must
never bleed toward any warm hue even when overlapping other beams or VFX.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, thin vertical energy laser beam segment,
bright cyan-white core (#40F8FF) with a soft light-blue glow fade at the edges
(#80E8FF), 1px solid dark navy-blue outline (#103060), flat color bands (2-3
discrete brightness steps, no smooth gradient blur), transparent background,
2x8 pixel canvas (very small, tall thin line), crisp hard pixel edges, designed
to be vertically tiled/stretched by a game engine into a longer continuous beam,
Cave/Raiden-style player laser weapon aesthetic.
Negative prompt: no warm colors, no round/oval shapes, no organic curves, no
blur, no anti-aliasing.
```

**Target Path**: `Assets/_Project/Art/Bullets/Player/player_laser_l1_beam_segment.png`
**Atlas**: `atlas_player_weapons`
**Status**: PENDING — awaiting API key

---

### ASSET-026 — vfx_laser_hit_spark_cyan (Generic Laser Hit Spark — Shared L1/L2/L4)

| Field | Value |
|-------|-------|
| Category | VFX / Hit Sparks |
| Dimensions | 2×2 px per spark, 2 sparks spawned per hit event (art-bible §08: "雷射命中火花") |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `vfx_laser_hit_spark_cyan.png` |
| Palette | Laser Core Cyan `#40F8FF` — matches the weapon's own core color, no separate palette needed |
| Reused By | L1, L2, L4 (all continuous-beam lasers) — L3's shockwave uses its own dedicated ring VFX, see stub table below |

**Visual Description**: A tiny bright cyan spark particle, spawned in pairs at the
point of laser-part contact. Deliberately minimal — this is a **high-frequency**
event (fires continuously while a beam holds on target) so per art-bible §08's juice
budget philosophy (echoed in `hit-feel-tiering.md`'s "juice is a scarce resource"
principle), it must stay small and cheap, not competing visually with the rarer,
bigger part-break explosion.

**Generation Prompt**:
```
Tiny bright cyan spark particle sprite (#40F8FF), simple 4-pointed star or small
angular burst shape, 2x2 pixel canvas, flat color, no outline needed at this
size, transparent background, designed for high-frequency particle reuse.
```

**Target Path**: `Assets/_Project/Art/VFX/HitSparks/vfx_laser_hit_spark_cyan.png`
**Atlas**: `atlas_vfx`
**Status**: PENDING — awaiting API key

---

### ASSET-027 — player_missile_m2_micro (M2 蜂群飛彈 / Swarm Launcher)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (Player Projectile) |
| Dimensions | 4×8 px, small oval, 8 fired simultaneously in a fan covering ~70% screen width (art-bible §3.2, weapon-system.md §C.5) |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `player_missile_m2_micro.png` |
| Palette | Missile Cold Blue `#70C8F0` (body) · Laser Glow Blue `#80E8FF` (small engine trail highlight) · Outline `#103060` |
| Tier-3 note | Tier-3 "飽和齊射" increases magazine 8→12, split into two 6-round bursts — same sprite reused, no new art asset needed (magazine count is a data change, not a visual one) |

**Visual Description**: A small elongated oval micro-missile body with a subtle
engine-glow tail. Reads as visually distinct from M1's homing missile (ASSET stub
below, larger/rounder) by being noticeably smaller and thinner, reflecting its
"8 cheap micro-missiles" design identity vs. M1's "2 chunky homing missiles."

**Readability Constraints**: Must stay legible as a discrete oval shape even at high
on-screen density (8 fired at once, potentially overlapping with L1's 3-beam fan) —
recommend keeping the outline crisp and avoiding excessive internal detail at this
small size.

**Generation Prompt**:
```
16-bit retro arcade pixel art sprite, small elongated oval micro-missile
projectile, cold light-blue body (#70C8F0), small bright blue engine trail glow
at the tail (#80E8FF), 1px solid dark navy-blue outline (#103060), flat color
fill, transparent background, 4x8 pixel canvas (small oval), crisp hard pixel
edges, Cave/Raiden-style player swarm-missile aesthetic, nose pointing up.
Negative prompt: no warm colors, no round bullet-like circular shape (must read
as oval/elongated, not round like an enemy bullet), no blur.
```

**Target Path**: `Assets/_Project/Art/Bullets/Player/player_missile_m2_micro.png`
**Atlas**: `atlas_player_weapons`
**Status**: PENDING — awaiting API key

---

### ASSET-028 — vfx_missile_hit_spark (Generic Missile Impact Spark)

| Field | Value |
|-------|-------|
| Category | VFX / Hit Sparks |
| Dimensions | Small burst, ~4×4 px |
| Format | PNG, RGBA 32-bit, transparent background |
| Naming | `vfx_missile_hit_spark_blue.png` |
| Palette | Missile Cold Blue `#70C8F0` / Laser Glow Blue `#80E8FF` |
| Reused By | Baseline impact spark for all missile-family weapons before their dedicated explosion VFX plays (M1, M2 direct hits) — M3/M4 have their own larger dedicated explosion assets, see stub table |

**Visual Description**: A small blue impact burst, slightly larger/chunkier than the
laser hit spark (ASSET-026) to communicate "physical impact" vs. "energy contact,"
while staying in the same cold family.

**Generation Prompt**:
```
Small cold-blue impact spark burst sprite (#70C8F0 core, #80E8FF outer points),
simple angular burst/star shape, 4x4 pixel canvas, flat color, no outline needed
at this size, transparent background, slightly chunkier than a laser spark to
read as a physical impact rather than an energy contact.
```

**Target Path**: `Assets/_Project/Art/VFX/HitSparks/vfx_missile_hit_spark_blue.png`
**Atlas**: `atlas_vfx`
**Status**: PENDING — awaiting API key

---

## STUBBED — Remaining 6 Weapons (L2, L3, L4, M1, M3, M4)

All share the cold-family palette rules above (Laser Core Cyan `#40F8FF` / Laser Glow
Blue `#80E8FF` / Ship Primary Blue `#2080F0` / Missile Cold Blue `#70C8F0` /
Tech UI Blue `#00C0E0`, outlines `#103060`). Sizes are locked by art-bible §3.2 and
must not be invented ad hoc when these move to full detail.

| ID | Name | Dimensions | Shape/Notes | Generation Prompt (compact) |
|----|------|-----------|-------------|------------------------------|
| ASSET-029 | `player_laser_l2_beam` | 3×12 px | Single thicker beam, "細束持續開火"; slightly wider/brighter than L1 to read as a focused-fire weapon | 16-bit pixel art, thin bright cyan focused laser beam segment (#40F8FF core, #80E8FF glow), 1px navy outline (#103060), 3x12px, flat color, hard edges, transparent bg, engine-tileable. |
| ASSET-030 | `player_laser_l3_pulse` (Tap mode) | ~4×10 px | Short blunt pulse bolt, cold blue-white | 16-bit pixel art, short blunt energy pulse bolt, cold blue-white (#40F8FF/#A0D4FF), 1px navy outline, 4x10px, flat color, hard edges, transparent bg. |
| ASSET-031 | `vfx_l3_wave_shockwave_ring` (Hold/charge mode) | Full-width animated ring, 6-frame expand sheet | **Director-approved change**: shape is a "往上的光柱" (upward pillar), not a fan (2026-07-03 decision, point 2) — update from art-bible §3.2's original "藍色震波圓環" wording; flag art-bible §3.2 L3 row for a follow-up text correction | 16-bit pixel art, vertical energy pillar/column shockwave, bright cyan-white core (#40F8FF) with light-blue outer glow (#80E8FF), 6-frame expansion animation, flat color bands, hard edges, transparent bg, full-screen-height beam column. |
| ASSET-032 | `player_laser_l4_beam` | 2px wide × full vertical screen height | Pierces everything; white core + cyan edge per art-bible §3.2 ("冷白芯+冷青邊") | 16-bit pixel art, ultra-thin full-height piercing laser beam, bright white core (#FFFFFF-adjacent cold white) with cyan edge glow (#40F8FF), 1px navy outline, 2px wide canvas, flat color, hard edges, transparent bg. |
| ASSET-033 | `player_missile_m1_homing` | 5×10 px | Larger/rounder oval than M2, 2 fired per shot, homing — consider a subtle fin/vane detail to differentiate silhouette from M2 | 16-bit pixel art, oval homing missile, cold blue body (#70C8F0), small tail fins, engine glow (#80E8FF), 1px navy outline, 5x10px, flat color, hard edges, transparent bg. |
| ASSET-034 | `player_missile_m3_torpedo` + `vfx_torpedo_explosion` | Torpedo: 6×14 px diamond/cone, cold-white front tip; Explosion VFX: 22-particle burst, `#FF6600`+`#CC4000` (art-bible §08 — **note this is the one player-weapon VFX that legitimately goes warm on impact**, since it's an explosion effect, not the projectile itself) | Largest/heaviest player projectile silhouette, no追蹤 (straight flight) | Torpedo: 16-bit pixel art, diamond/cone-shaped torpedo, cold blue-grey body (#70C8F0), bright white-cyan nose tip, 1px navy outline, 6x14px, flat color, hard edges, transparent bg. |
| ASSET-035 | `player_missile_m4_bomb` + `vfx_cluster_detonation` | Bomb: 8×10 px circular, cold blue-grey; Detonation VFX: 26-particle burst `#FF8000`+`#FFCC00` (art-bible §08 — same warm-on-impact note as M3) | Parabolic arc drop, splits into 6 child bombs at Tier-3 (reuse same sprite, no new art per tier) | Bomb: 16-bit pixel art, small round bomb/cluster canister, cold blue-grey body (#70C8F0-adjacent grey-blue), 1px navy outline, 8x10px, flat color, hard edges, transparent bg. |

**Explosion VFX note (ASSET-034/035 warm-color exception)**: M3 and M4's *explosion*
VFX are specified in art-bible §08 using warm orange/red particle colors. This is
**not** a violation of the cold-family rule for player weapons — the rule governs the
*projectile itself* (art-bible §6.1: player hull/projectiles must stay cold); an
explosion is a physical-impact effect, and warm colors read correctly there (fire/heat
at the point of detonation) without confusing the player-vs-enemy color language,
since it's a brief, non-recurring burst tied to a impact position, not a persistent
moving object. No director flag needed — this is already how art-bible §08 specifies
it; noted here only to preempt confusion when a future artist asks "wait, isn't warm
=enemy-only?"

**Target Paths**: `Assets/_Project/Art/Bullets/Player/player_*.png`, `Assets/_Project/Art/VFX/Explosions/vfx_*_explosion.png`
**Atlas**: `atlas_player_weapons` (projectiles), `atlas_vfx` (explosions/rings)
**Status**: PENDING — awaiting API key (all 6 stubbed; promote to full detail on director request)

---

*Spec file version: 1.0.0 — Art Director Agent — 2026-07-05*
