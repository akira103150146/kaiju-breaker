# Epic: Content 調校資料框架（ScriptableObject）

> **Layer**: Foundation
> **GDD**: docs/architecture/architecture.md §6（全系統調校資料，無專屬 GDD）
> **Architecture Module**: `KaijuBreaker.Content`
> **Status**: Ready
> **Stories**: 9 stories — see table below

## Overview

本 Epic 建立所有靜態調校資料的唯一載體：`KaijuBreaker.Content` 提供全部 ScriptableObject 定義（`WeaponDef`×8、`WeaponBalanceConfig`、`PartSystemConfig`、`KaijuDef`、`DifficultyConfig`、`EconomyConfig`、`StageDef`/`SegmentDef`/`PodDropConfig`、`GameFeelConfig`、`EmitterPatternSO`、`InputSettings`、`SaveConfig`），放於 `Assets/_Project/Content/`，執行期唯讀。此模組落實 ADR-0003「資料驅動、零硬編碼」，取代 GDD 中所有 `assets/data/**/*.yaml` 引擎無關佔位路徑，並確立「靜態 SO vs 玩家可變 JSON 存檔」的可維護性切分。它是所有 Core/Feature/Presentation 系統的資料前置依賴。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 ADR-0003 決策條目推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: 資料驅動調校（ScriptableObject） | 所有平衡/調校旋鈕以唯讀 SO 表達於 `Assets/_Project/Content`；取代 GDD YAML 佔位；測試以假 SO fixture 注入 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-content-001 | 所有 GDD「調校旋鈕」章節數值以唯讀 SO 承載，放 `Assets/_Project/Content`，執行期唯讀載入 | ADR-0003 ✅ |
| TR-content-002 | 提供全套 SO 定義類別（WeaponDef/PartSystemConfig/KaijuDef/DifficultyConfig/EconomyConfig/StageDef/GameFeelConfig/InputSettings/EmitterPatternSO/SaveConfig） | ADR-0003 ✅ |
| TR-content-003 | 靜態 SO 與玩家可變 JSON 存檔嚴格分離，執行期不寫回 SO | ADR-0003 ✅ |
| TR-content-004 | 跨 GDD 共享旋鈕單一權威來源（難度乘數唯一存 DifficultyConfig；`stagger_duration` 單一擁有者） | ADR-0003 ✅ |
| TR-content-005 | SO 以 `OnValidate` 做安全範圍編輯期斷言；測試以工廠/`.asset` fixture 注入，無行內魔數 | ADR-0003 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `docs/architecture/architecture.md` §6 and ADR-0003 are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | [WeaponBalanceConfig + WeaponDef ScriptableObjects](story-001-weapon-balance-config-so.md) | Config/Data | Ready | ADR-0003 |
| 002 | [PartSystemConfig + KaijuDef ScriptableObjects](story-002-part-kaiju-so.md) | Config/Data | Ready | ADR-0003 |
| 003 | [DifficultyConfig ScriptableObject](story-003-difficulty-config-so.md) | Config/Data | Ready | ADR-0003 |
| 004 | [GameFeelConfig ScriptableObject](story-004-game-feel-config-so.md) | Config/Data | Ready | ADR-0003 |
| 005 | [EmitterPatternSO + MovementPatternSO + EnemyDef](story-005-emitter-movement-pattern-so.md) | Config/Data | Ready | ADR-0003 |
| 006 | [StageDef + SegmentDef + PodDropConfig ScriptableObjects](story-006-stage-segment-so.md) | Config/Data | Ready | ADR-0003 |
| 007 | [EconomyConfig + InputSettings + SaveConfig ScriptableObjects](story-007-peripheral-config-so.md) | Config/Data | Ready | ADR-0003 |
| 008 | [ContentRegistry Service](story-008-content-registry.md) | Integration | Ready | ADR-0003 |
| 009 | [SO Test Fixture Support](story-009-so-test-fixtures.md) | Logic | Ready | ADR-0003 |
