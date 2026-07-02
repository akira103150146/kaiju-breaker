# Epic: HUD / UI 系統

> **Layer**: Presentation
> **GDD**: design/gdd/hud-ui-system.md
> **Architecture Module**: `KaijuBreaker.UI`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories hud-ui`

## Overview

本 Epic 實作全部玩家可讀性介面：`KaijuBreaker.UI` 提供世界座標部位血條、武器 HUD（雙槽/L3 蓄力條/副武器彈匣 Pip）、素材計數，以及戰鬥外三畫面（Loadout / 永久升級 / 難度選擇）；訂閱事件與查詢介面呈現狀態，絕不遮蔽敵彈或玩家判定點。含手機安全區佈局、像素 UI 縮放與無障礙功能。**架構暫定 UGUI，但 UI 框架 ADR（UGUI vs UI Toolkit）尚待 `technical-artist`/`lead-programmer` 補寫——與框架/佈局/縮放相關的需求目前無專屬 ADR 覆蓋。**

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `hud-ui-system.md` §M 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: 事件架構 | UI 訂閱事件（`on_part_softened` 等）+ 查詢介面（`IPartStateQuery`/`ISaveService`）呈現，零直接引用 | LOW |
| UI ADR（待補）| HUD 框架 UGUI vs UI Toolkit 決議；世界座標血條 + 像素縮放策略 | MEDIUM（ADR 尚未撰寫；架構暫定 UGUI，非 MVP 阻斷但屬未追溯決策）|

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-ui-001 | M.1 彈幕可讀性不受 HUD 干擾（Alpha 阻斷）：D4 不遮敵彈外框 ≥70%；判定點閃光可辨；血條不錯位 | ❌ 無 ADR（UI ADR 待補；ADR-0002 僅覆蓋事件訂閱） |
| TR-ui-002 | M.2 SOFTENED ≤0.5s 可讀：HEAT 條同幀切橙脈動；`pulse_hz` 與 part-system 一致；退出同幀停 | ADR-0002 ✅（事件） |
| TR-ui-003 | M.3 副武器彈匣狀態正確：Pip 即時、換彈條、拾取補滿（狀態機自動化測試） | ADR-0002 ✅ |
| TR-ui-004 | M.4 L3 蓄力條正確：僅 L3 顯示、填充時間符 `l3_charge_time`、90% 閃爍、冷卻遮罩 | ADR-0002 ✅ |
| TR-ui-005 | M.5 升級畫面費用顯示正確：不足紅✗灰化、庫存即時同步、Tier-3 模糊預覽、建議狩獵指向 | ADR-0002 ✅（ISaveService 查詢） |
| TR-ui-006 | M.6 難度 UI 行為：首次預選 D1、記憶上輪、輪中灰化（自動化測試） | ADR-0002 ✅ |
| TR-ui-007 | M.7 跨平台 HUD 安全區：手機 Portrait 15%/20% 條、觸控目標 ≥44dp、4K 整數倍銳利縮放 | ❌ 無 ADR（UI ADR 待補） |
| TR-ui-008 | M.8 無障礙功能：文字縮放 150%、色盲替代、Reduce-Motion、一級可達 | ❌ 無 ADR（UI ADR 待補） |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/hud-ui-system.md` are verified
- The pending UI-framework ADR (UGUI vs UI Toolkit) is authored and Accepted, covering TR-ui-001/007/008
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories hud-ui` to break this epic into implementable stories.
