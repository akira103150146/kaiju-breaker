# Epic: 輸入系統

> **Layer**: Feature
> **GDD**: design/gdd/input-system.md
> **Architecture Module**: `KaijuBreaker.Input`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories input`

## Overview

本 Epic 實作三方案輸入抽象：`KaijuBreaker.Input` 以 Unity Input System 統一觸控（Sky Force 式相對偏移拖曳）、鍵鼠、手柄的動作映射，處理 L3 蓄力事件與全面重映射。主要平台為手機，**觸控彈幕手感為本專案未解高風險——L1 觸控專屬原型驗證為阻斷 pre-MVP 條件**。輸入手感數值資料驅動於 `InputSettings` SO（玩家覆寫存 save），且不隨難度縮放。含無障礙基線（L3 Toggle 模式、可關震動、READY 非顏色提示）。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `input-system.md` §L 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: 資料驅動調校 | `InputSettings` SO 承載觸控偏移/lerp/死區/映射預設；玩家覆寫存 save | LOW |
| ADR-0005: 專案結構與組件邊界 | Input 為獨立組件，僅依賴 Core + Content；經事件供其他系統 | MEDIUM（Unity 6 Input System package API 對 2022.3 有重大變更 [需查證 6.3 API]） |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-input-001 | L.1 觸控手感驗證（阻斷 pre-MVP）：專屬原型、遮蔽解決、彈幕閃避可行、L3 觸控可行 | ❌ 無 ADR（原型驗證閘門；ADR-0003 僅覆蓋設定資料） |
| TR-input-002 | L.2 跨方案等價（阻斷 pre-VS）：觸控部位破壞中位數 ≥ 滑鼠 0.8×；L3 成功率各 ≥80% | ❌ 無 ADR（design GDD，playtest） |
| TR-input-003 | L.3 鍵盤單一方案完整遊玩（阻斷）：WASD+空白+Z 可完成完整流程 | ❌ 無 ADR（design GDD） |
| TR-input-004 | L.4 手柄完整遊玩（阻斷）：左搖桿+RT+LT+Start 可完成完整流程 | ❌ 無 ADR（design GDD） |
| TR-input-005 | L.5 無障礙基線（阻斷）：L3 Toggle 等效、震動可完全關閉、READY 非顏色提示保留 | ADR-0003 ✅（設定資料） |
| TR-input-006 | L.6/L.7 蓄力中斷 0.3s 寬限、副武器按鈕誤觸率 ≤10%（建議） | ❌ 無 ADR（design GDD） |
| TR-input-007 | 輸入手感數值資料驅動於 `InputSettings`，不隨難度縮放 | ADR-0003 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/input-system.md` are verified
- The L.1 touch-feel prototype gate passes on real mobile hardware before touch is admitted to MVP
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories input` to break this epic into implementable stories.
