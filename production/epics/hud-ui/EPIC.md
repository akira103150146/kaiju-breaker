# Epic: HUD / UI 系統

> **Layer**: Presentation
> **GDD**: design/gdd/hud-ui-system.md
> **Architecture Module**: `KaijuBreaker.UI`
> **Status**: Ready
> **Stories**: 11 stories — see Stories table below

## Overview

本 Epic 實作全部玩家可讀性介面：`KaijuBreaker.UI` 提供世界座標部位血條、武器 HUD（雙槽/L3 蓄力條/副武器彈匣 Pip）、素材計數，以及戰鬥外三畫面（Loadout / 永久升級 / 難度選擇）；訂閱事件與查詢介面呈現狀態，絕不遮蔽敵彈或玩家判定點。含手機安全區佈局、像素 UI 縮放與無障礙功能。**UI 框架由 ADR-0006 定案**（三層:世界座標血條 = SpriteRenderer；in-combat HUD = UGUI 多 Canvas;meta 畫面 = UGUI），TR-ui-001/007/008 已覆蓋。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `hud-ui-system.md` §M 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: 事件架構 | UI 訂閱事件（`on_part_softened` 等）+ 查詢介面（`IPartStateQuery`/`ISaveService`）呈現，零直接引用 | LOW |
| ADR-0006: UI 框架 | 三層拆分——世界座標血條用 `SpriteRenderer`(不進 Canvas，避免彈幕區開銷)、in-combat HUD 用 UGUI 多 Canvas(判定點 overlay sort 99)、meta 畫面用 UGUI；Accepted | MEDIUM（Unity 6.3 URP 2D Canvas / PixelPerfect 交互 [需查證]）|

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-ui-001 | M.1 彈幕可讀性不受 HUD 干擾（Alpha 阻斷）：D4 不遮敵彈外框 ≥70%；判定點閃光可辨；血條不錯位 | ADR-0006 ✅（判定點 overlay sort 99 > flash；血條走 SpriteRenderer 不進彈幕區 Canvas） |
| TR-ui-002 | M.2 SOFTENED ≤0.5s 可讀：HEAT 條同幀切橙脈動；`pulse_hz` 與 part-system 一致；退出同幀停 | ADR-0002 ✅（事件） |
| TR-ui-003 | M.3 副武器彈匣狀態正確：Pip 即時、換彈條、拾取補滿（狀態機自動化測試） | ADR-0002 ✅ |
| TR-ui-004 | M.4 L3 蓄力條正確：僅 L3 顯示、填充時間符 `l3_charge_time`、90% 閃爍、冷卻遮罩 | ADR-0002 ✅ |
| TR-ui-005 | M.5 升級畫面費用顯示正確：不足紅✗灰化、庫存即時同步、Tier-3 模糊預覽、建議狩獵指向 | ADR-0002 ✅（ISaveService 查詢） |
| TR-ui-006 | M.6 難度 UI 行為：首次預選 D1、記憶上輪、輪中灰化（自動化測試） | ADR-0002 ✅ |
| TR-ui-007 | M.7 跨平台 HUD 安全區：手機 Portrait 15%/20% 條、觸控目標 ≥44dp、4K 整數倍銳利縮放 | ADR-0006 ✅ |
| TR-ui-008 | M.8 無障礙功能：文字縮放 150%、色盲替代、Reduce-Motion、一級可達 | ADR-0006 ✅（UGUI + USS/設定層；Reduce-Motion 與 game-feel 協調） |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/hud-ui-system.md` are verified
- UI-framework decisions follow ADR-0006 (Accepted), which covers TR-ui-001/007/008
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | World-Space Part HEAT/BREAK Bars (PartBarController) | Integration | Ready | ADR-0006 |
| 002 | In-Combat HUD Canvas Setup + Weapon Slot Displays + Hitbox Overlay | UI | Ready | ADR-0006 |
| 003 | Secondary Weapon Ammo Pips & Reload Bar | Integration | Ready | ADR-0002 |
| 004 | L3 Charge Bar | Integration | Ready | ADR-0002 |
| 005 | Material Counter & Boss HP Bar | Integration | Ready | ADR-0002 |
| 006 | Screen Flow / UIScreenManager Stack | Integration | Ready | ADR-0006 |
| 007 | Loadout Screen | UI | Ready | ADR-0006 |
| 008 | Permanent Upgrade Screen | Integration | Ready | ADR-0002 |
| 009 | Difficulty Select Screen | Integration | Ready | ADR-0002 |
| 010 | Cross-Platform Safe Areas, Thumb Zones & Pixel-Perfect Scaling | UI | Ready | ADR-0006 |
| 011 | Accessibility (Text Scale, Colorblind Mode, Reduce-Motion) | Integration | Ready | ADR-0002 |
