# Epic: 素材經濟與永久升級

> **Layer**: Core
> **GDD**: design/gdd/material-economy.md
> **Architecture Module**: `KaijuBreaker.Economy`
> **Status**: Ready
> **Stories**: 5 stories — see table below

## Overview

本 Epic 實作素材產量公式、升級成本與 Tier 授予：`KaijuBreaker.Economy` 消費 `on_part_break`，依 `break_quality`+`kaiju_id` 獨立計算 `shard_yield`/`core_yield`（巨獸主題綁定核心：甲殼→core_carapace / 肢體→core_limb / 能量→core_energy），結果交 `Meta` 即時入帳；亦處理永久升級消費 sink 與 Tier 授予（回饋武器系統經 `IWeaponTierQuery`）。設計目標為淺養成曲線、跨輪永久、且「最終養成不產生主導 loadout」——Tier 升級效果須作為 8×8 TTB 矩陣測試輸入以確認橫向選擇不被破壞。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `material-economy.md` §H 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: 事件架構 | 消費 `on_part_break`/`on_hunt_end`；`shard/core yield` 由 Economy 獨立算（不放進 payload），Meta 讀結果入帳 | LOW |
| ADR-0003: 資料驅動調校 | `EconomyConfig` 承載產量倍率/升級成本表；平衡測試以 SO 為輸入 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-economy-001 | H.1 端到端循環：破壞→HUD→結算記帳→升級→Tier 更新→戰場生效，跨局存檔無丟失、無回滾 | ADR-0002 / ADR-0004 ✅ |
| TR-economy-002 | H.2 產量公式正確：Standard/Precision/Perfect × 部位類型 × 巨獸主題 = 27 情境；主題核心映射 | ADR-0003 ✅ |
| TR-economy-003 | H.3 升級效果無主導 loadout：Tier-3 普通部位 TTB 改善 ≤15%；64 組矩陣 ≤2.0× | ADR-0003 ✅ |
| TR-economy-004 | H.4 素材類型分流：主題核心正確歸屬；全破壞結算精魄 + 完成度碎片；非全破壞無加成 | ADR-0002 / ADR-0003 ✅ |
| TR-economy-005 | H.5 進程曲線可玩性：第一把 Tier0→1 兩場內、Tier2→3 於 7–13 場、升滿 15–21h ±30% | ❌ 無 ADR（design GDD，playtest 驗收） |
| TR-economy-006 | H.6 最終養成不產生主導 loadout：全 Tier-3 試玩後每武器仍有最佳/近最佳情境 | ADR-0003 ✅ |
| TR-economy-007 | H.7 MVP 子集閉環：2 武器 × ≤Tier2 × 1 巨獸端到端跑通、升級效果可感知 | ADR-0002 / ADR-0003 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/material-economy.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Part-Break Material Yield Computation | Logic | Ready | ADR-0002 |
| 002 | Full-Clear Essence Award | Logic | Ready | ADR-0002 |
| 003 | Material Inventory — Persistence Handoff to Meta-Save | Integration | Ready | ADR-0002 |
| 004 | Tier 0→3 Upgrade Transaction | Logic | Ready | ADR-0003 |
| 005 | Anti-Degenerate Loadout Guard (TTB Matrix Assertion) | Logic | Ready | ADR-0003 |

## Next Step

Run `/story-readiness production/epics/economy/story-001-yield-on-break.md` to begin implementation.
