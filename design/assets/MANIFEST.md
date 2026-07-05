# Master Asset Manifest — KAIJU BREAKER (MVP)

> **Role**: single index + batch-generation worklist for every spec'd MVP art asset.
> **Specs**: `design/assets/specs/*.md` (per-entity detail — palettes, sizes, AI prompts).
> **Art bible**: `design/art-bible.md` (binding visual law). **Generated**: 2026-07-05.
> **Generation**: BLOCKED on the AI-image API key (sprites/VFX) + the pixel-font TTF (director drop).
> This manifest is what a batch run iterates once those land.

## Status legend

| Status | Meaning |
|--------|---------|
| ⛔ PENDING-KEY | spec ready; awaiting the AI-image API key to generate |
| 🔤 PENDING-FONT | awaiting the director to drop the chosen pixel-font TTF |
| 🟢 DONE | asset generated + imported |
| 🎨 APPROVED | generated + art-lead sign-off |

**MVP totals: 56 sprite/VFX assets + 1 font family + UI icon set. All currently ⛔/🔤 PENDING.**
Import root: `Assets/_Project/Art/`. All sprites: PNG RGBA, 1px solid outline, integer scale only (art-bible §3.3/§3.1).

---

## 1. Player Ship — 4 assets · `Art/Characters/Ship/` · spec `player-ship-assets.md`

| ID | Asset | Notes | Status |
|----|-------|-------|--------|
| char_ship_idle | idle float | 14×20, 4-frame @8fps | ⛔ PENDING-KEY |
| char_ship_bank_left / _right | tilt frames | 2+2 frames | ⛔ PENDING-KEY |
| char_ship_engine_flame | engine flame | 4-frame @12fps | ⛔ PENDING-KEY |
| char_ship_hitbox_dot | 1×1 white hitbox | **P0 — highest-priority pixel**, never scales (art-bible §4.4) | ⛔ PENDING-KEY |

## 2. Kaiju — CARAPEX (甲殼系) — 4 parts · `Art/Kaiju/Carapex/` · spec `kaiju-carapex-assets.md`

Each part needs INTACT + SOFTENED-glow + BROKEN (and ARMORED parts: STRIPPED) states (art-bible §5.3).

| ID | Part | Type | Status |
|----|------|------|--------|
| kaiju_carapex_chest_reactor_core | 胸口核心 | BossCore | ⛔ PENDING-KEY |
| kaiju_carapex_mandible (×L/R) | 大顎 | Normal | ⛔ PENDING-KEY |
| kaiju_carapex_dorsal_cannon | 背甲炮 | Armored (+stripped) | ⛔ PENDING-KEY |

## 3. Kaiju — LACERA (肢體系) — 7 parts · `Art/Kaiju/Lacera/` · spec `kaiju-lacera-assets.md`

| ID | Part | Type | Status |
|----|------|------|--------|
| kaiju_lacera_head_core | 頭部核心 | BossCore | ⛔ PENDING-KEY |
| kaiju_lacera_fore_limb / hind_limb | 前/後肢 (×4) | Normal (+limb_stub_broken, tip_chargeflash) | ⛔ PENDING-KEY |
| kaiju_lacera_tail_carapace | 尾甲 | Armored (+stripped) | ⛔ PENDING-KEY |

## 4. Kaiju — VOLTWYRM (能量系) — 4 parts · `Art/Kaiju/Voltwyrm/` · spec `kaiju-voltwyrm-assets.md`

| ID | Part | Type | Status |
|----|------|------|--------|
| kaiju_voltwyrm_core_node | 核心節 | BossCore | ⛔ PENDING-KEY |
| kaiju_voltwyrm_neck_seg | 頸段 (×4) | Normal | ⛔ PENDING-KEY |
| kaiju_voltwyrm_shield | 能量盾 (×2) | Armored (+stripped) | ⛔ PENDING-KEY |

## 5. Break / State VFX — 6 assets · `Art/VFX/` · spec `kaiju-break-vfx-assets.md`

| ID | Effect | Status |
|----|--------|--------|
| vfx_part_softened_glow (+mask) | SOFTENED pulse glow, 2Hz | ⛔ PENDING-KEY |
| vfx_part_weakpoint_frame | ARMOR_STRIPPED weak-point flash | ⛔ PENDING-KEY |
| vfx_part_break_explosion | BROKEN, 8–12 frame @15fps | ⛔ PENDING-KEY |
| vfx_debris_particle_set | debris (part-color / flash-white / orange) + black smoke | ⛔ PENDING-KEY |
| vfx_boss_death_burst | Boss death gold-white burst | ⛔ PENDING-KEY |
| vfx_full_white_flash_overlay | full-screen white flash (max α 0.85) | ⛔ PENDING-KEY |

## 6. Weapons — projectiles + hit VFX — 11 assets · `Art/Bullets/Player/` + `Art/VFX/` · spec `weapons-assets.md`

| ID | Weapon | Status |
|----|--------|--------|
| laser_l1_spread / l1_beam_segment | L1 散波 | ⛔ PENDING-KEY |
| laser_l2_beam | L2 集束 | ⛔ PENDING-KEY |
| laser_l3_pulse | L3 波動砲 | ⛔ PENDING-KEY |
| laser_l4_beam | L4 穿透 | ⛔ PENDING-KEY |
| missile_m1_homing / m2_micro / m3_torpedo / m4_bomb | M1–M4 | ⛔ PENDING-KEY |
| laser_hit_spark_cyan / missile_hit_spark_blue | cold hit sparks | ⛔ PENDING-KEY |

## 7. Enemy Bullets — 12 assets · `Art/Bullets/Enemy/` · spec `enemy-bullets-assets.md`

Warm sub-palette × {round, oval} small — must stay readable on the smallest phone (art-bible §4.2). 1px pure-black outline.

| Family | Variants | Status |
|--------|----------|--------|
| orange / brightorange / orangered / darkred / yellow / energywhitegold | round_small + oval_small each (12) | ⛔ PENDING-KEY |

## 8. Material / Core Icons — 8 assets · `Art/UI/HUD/` · spec `material-icons-assets.md`

| ID | Icon | Status |
|----|------|--------|
| icon_shard_common | 通用碎片 | ⛔ PENDING-KEY |
| icon_core_carapace / core_limb / core_energy | 3 主題核心 | ⛔ PENDING-KEY |
| icon_essence_kaiju | 巨獸精魄 | ⛔ PENDING-KEY |
| (+ material homing-orb / burst variants) | HUD fly-in | ⛔ PENDING-KEY |

## 9. UI / HUD — font + interface · `Art/Fonts/` + `Art/UI/HUD/` · spec `hud-ui-assets.md`

| Item | Notes | Status |
|------|-------|--------|
| Primary pixel font (tech-feel — see spec §1) | 繁體 CJK + Latin, native 16px; drop TTF → build TMP bitmap asset | 🔤 PENDING-FONT |
| Weapon icons L1–L4 / M1–M4 (16×16) | 8 | ⛔ PENDING-KEY |
| Tier badges T0–T3 (8×6) | 4 | ⛔ PENDING-KEY |
| Ammo pip (6×6, filled/empty) | 1 | ⛔ PENDING-KEY |
| HUD/menu chrome | palette + layout DONE in prototype restyle (`896b9b1`); production = UGUI+TMP (ADR-0006) | 🟢 palette / ⛔ icons |

---

## Batch-generation order (once key + font land)

1. **Font** (🔤): drop TTF → assign `_pixelFont` → build TMP bitmap asset. Unblocks the whole UI look.
2. **Readability-critical first** (⛔): hitbox dot, enemy bullets, player projectiles — the "always readable" trio (art-bible §04).
3. **Kaiju parts** (⛔): CARAPEX → LACERA → VOLTWYRM, all states.
4. **VFX + icons** (⛔): break/state VFX, material + weapon/tier icons.
5. **Ship** (⛔): idle / bank / engine.

Per-asset AI-generation prompts live in each spec file. Regenerate this manifest's counts if a spec adds/removes assets.
