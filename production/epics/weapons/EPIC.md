# Epic: 武器系統

> **Layer**: Core
> **GDD**: design/gdd/weapon-system.md
> **Architecture Module**: `KaijuBreaker.Weapons`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories weapons`

## Overview

本 Epic 實作 8 把等功率 sidegrade 武器（雷射 raycast/overlap、飛彈池、雙軌蓄熱→破甲輸出、Tier 0–3 效果）。`KaijuBreaker.Weapons` 對部位發 `on_laser_hit`/`on_missile_hit`/`on_l3_wave_hit` 事件，經 `IPartStateQuery` 讀部位 `heat_state`/`world_position`（M1 追蹤、L2/M3 Tier-3 觸發），經 `IWeaponTierQuery` 讀當前 Tier 再從 `WeaponDef` 取靜態旋鈕。核心設計約束為「等功率橫向選擇、無主導 loadout、Tier-3 深化身份而非放大數值」，全部平衡值資料驅動於 SO，以 8×8 TTB 矩陣自動化測試驗證。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `weapon-system.md` §H 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: 事件架構 | 武器發 `on_*_hit` 事件；經查詢介面讀部位狀態；零直接引用 KaijuParts | LOW |
| ADR-0003: 資料驅動調校 | `WeaponBalanceConfig` + `WeaponDef`×8 承載 D₀/H_rate/B_rate/彈匣/Tier 效果；測試以 SO 值為輸入 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-weapon-001 | H.1 等功率等價：單武器最優命中 30s 總輸出 ∈ `D₀×[0.9,1.1]`（自動化測試） | ADR-0003 ✅ |
| TR-weapon-002 | H.2 無主導 loadout：8×8 TTB 任一 ≤ 最快 2.0×；無 loadout 於全部位類型均前三 | ADR-0003 ✅ |
| TR-weapon-003 | H.3 M3 熱衝擊破甲門檻：暴力破甲 TTB ≥ 正常蓄熱路徑 1.5× | ADR-0003 ✅ |
| TR-weapon-004 | H.4 L3 波動砲弱點窗口可操作：2.0s 窗口內完成 M2 齊射 ≥80% 成功 | ADR-0003 ✅ |
| TR-weapon-005 | H.5 雙軌可讀性：軟化視覺提示 ≤0.5s 可辨識 ≥80%（事件經 ADR-0002；視覺呈現跨 GameFeel/UI 交付） | ADR-0002 ✅（事件）；視覺 ❌ 無專屬 ADR（跨 game-feel/hud-ui 覆蓋） |
| TR-weapon-006 | H.6 L4 關卡可用性：每 Boss ≥1 垂直對齊穿透情境（關卡設計評審確認） | ❌ 無 ADR（design GDD + 關卡評審） |
| TR-weapon-007 | H.7 Tier-3 深化身份：普通部位 TTB 相對 Tier-1 縮短 ≤15%（自動化測試） | ADR-0003 ✅ |
| TR-weapon-008 | H.8 標誌性搭配節奏差異：三組搭配可清晰區分打法節奏 | ❌ 無 ADR（design GDD 主觀驗收） |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/weapon-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | WeaponDef & WeaponBalanceConfig ScriptableObjects | Config/Data | Ready | ADR-0003 |
| 002 | D₀ Balance Suite — Automated Formula Tests | Logic | Ready | ADR-0003 |
| 003 | WeaponController Base & Dual-Track Event Bus Wiring | Integration | Ready | ADR-0002 |
| 004 | Laser Family Firing — L1 Spread, L2 Focus, L4 Pierce | Integration | Ready | ADR-0002 |
| 005 | L3 Wave Cannon — Dual-Mode Tap/Charge + L3WaveHit | Integration | Ready | ADR-0002 |
| 006 | Missile Family Firing — M1 Homing, M2 Swarm, M4 Cluster | Integration | Ready | ADR-0002 |
| 007 | M3 AP Torpedo — Heat-Shock Gate & Softened Query | Integration | Ready | ADR-0002 |
| 008 | Laser Tier-3 Unique Mechanics (L1/L2/L3/L4) | Integration | Ready | ADR-0002 |
| 009 | Missile Tier-3 Unique Mechanics (M1/M2/M3/M4) | Integration | Ready | ADR-0002 |
| 010 | Loadout System — 1+1 Equip & Weapon Pod Pickup | Integration | Ready | ADR-0002 |
