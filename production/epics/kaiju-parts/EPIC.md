# Epic: 可破壞部位系統

> **Layer**: Core
> **GDD**: design/gdd/kaiju-part-system.md
> **Architecture Module**: `KaijuBreaker.KaijuParts`
> **Status**: Ready
> **Stories**: 6 stories

## Overview

本 Epic 實作雙軌 soften→break 的權威狀態機：`KaijuBreaker.KaijuParts` 維護每部位的熱量槽 H（→SOFTENED）與破甲槽 B（護甲閘門/STAGGERED），消費武器命中事件並發出 `on_part_softened/_exit`、`on_part_staggered/_end`、`on_part_break`（攜 `break_quality`/`world_position`/`drop_table_id`/`adjacency`/`is_chain_break`）、`on_boss_core_break`；實作 `IPartStateQuery` 供武器/UI 查詢。此模組是全架構最關鍵資料流的中樞（武器↔部位↔素材↔存檔/打擊感/UI），`break_quality` 於破壞成立幀計算，為技術表現→獎勵的載體。部位數值難度不縮放、破壞不可逆、永不再生。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `kaiju-part-system.md` §H 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: 事件架構 | KaijuParts 擁有並發出 `on_part_break` 等事件；payload 一次攜齊；實作 `IPartStateQuery`；同幀語義 | LOW |
| ADR-0003: 資料驅動調校 | `PartSystemConfig`（H_max/B_max/theta_S/閾值）+ `KaijuDef` 部位/相鄰/掉落表；難度不縮放 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-part-001 | H.1 狀態機正確性：B_fill 查表（unsoftened/softened/staggered/1.5）、BROKEN 不可逆終態、破壞後命中無事件 | ADR-0002 / ADR-0003 ✅ |
| TR-part-002 | H.2 SOFTENED 視覺提示 ≤0.5s 出現、辨識率 ≥80%、D4 遮蓋 ≤50%（事件經 ADR-0002；視覺跨 GameFeel/UI 交付） | ADR-0002 ✅（事件）；視覺 ❌ 跨系統覆蓋 |
| TR-part-003 | H.3 ARMORED 護甲閘門：ARMOR_INTACT B_fill=0；ARMOR_STRIPPED 僅由 `on_l3_wave_hit` 觸發；BU 不清零 | ADR-0002 / ADR-0003 ✅ |
| TR-part-004 | H.4 Boss Core 破壞發 `on_boss_core_break`；事件順序 `on_part_break`→`on_boss_core_break`；僅核心觸發勝利 | ADR-0002 ✅ |
| TR-part-005 | H.5 部位永不再生：`part_regen_enabled` 恆 false；新輪正確重置為 ALIVE | ADR-0003 ✅ |
| TR-part-006 | H.6 相鄰鏈式：L2 T3 熱脈衝、M3 T3 連鎖（≤2 鄰居、不遞迴、護甲偏轉鏈式） | ADR-0002 / ADR-0003 ✅ |
| TR-part-007 | H.7 部位數值難度不縮放：D1–D4 全域旋鈕回傳完全相同；無寫入縮放路徑 | ADR-0003 ✅ |
| TR-part-008 | H.8 破壞即掉落：`on_part_break` 攜非空 `drop_table_id`；掉落 `world_position` 一致（整合測試） | ADR-0002 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/kaiju-part-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Part Entity & Two-Bar Data Model | Logic | Ready | ADR-0003 (primary), ADR-0002 |
| 002 | Heat State Machine (INTACT ↔ SOFTENED) | Logic | Ready | ADR-0002 (primary), ADR-0003 |
| 003 | Armor Gate & Stagger Timer | Logic | Ready | ADR-0002 (primary), ADR-0003 |
| 004 | Break Condition & Event Emission | Logic | Ready | ADR-0002 (primary), ADR-0003 |
| 005 | Adjacency Graph Load & Tier-3 Chain Consumers | Logic | Ready | ADR-0003 (primary), ADR-0002 |
| 006 | Softened/Broken Readability Hooks | Visual/Feel | Ready | ADR-0002 |
