# Epic: 打擊感（VFX / SFX / Game Feel）

> **Layer**: Presentation
> **GDD**: design/gdd/game-feel.md
> **Architecture Module**: `KaijuBreaker.GameFeel`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories game-feel`

## Overview

本 Epic 實作全部 juice 與軟化簽章：`KaijuBreaker.GameFeel` 消費部位事件，驅動頓幀（`Time.timeScale=0`）、慢動作、螢幕震動（≤24px 護欄）、閃光、SOFTENED 色偏脈動簽章（#FF6600，≤0.5s 可辨）與素材軌道球。關鍵時間紀律：頓幀/慢動作作用於 scaled 世界，但玩家輸入/UI/震動計算走 unscaled 時鐘（頓幀不得吃掉閃避輸入）；ECS 彈幕併入 `timeScale` 使 `time_scale=0` 敵彈靜止。全值資料驅動於 `GameFeelConfig`，含 reduce-motion 開關。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `game-feel.md` §I 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: 事件架構 | GameFeel 訂閱部位事件（`on_part_softened`/`on_part_break`…）同幀反應，零直接引用 | LOW |
| ADR-0003: 資料驅動調校 | `GameFeelConfig` 承載震動/慢動作/頓幀/SOFTENED/reduce-motion 全旋鈕 | MEDIUM（`time_scale=0` 凍結敵彈的 ECS 時間注入方式 [需查證 6.3 API]）|

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-gamefeel-001 | I.1 SOFTENED 感知 ≤0.5s、sfx 同幀播放、辨識率 ≥80%、D4 遮蔽 ≤50%（UX 阻斷） | ADR-0002 ✅ |
| TR-gamefeel-002 | I.2 頓幀正確性：時長精確 ±5ms、輸入記錄後首幀執行不丟失、`time_scale=0` 敵彈靜止 | ADR-0002 ✅ + [需查證 ECS 時間注入] |
| TR-gamefeel-003 | I.3 慢動作正確性：timescale 降/回升；SFX 與玩家輸入不縮速 | ADR-0002 ✅ |
| TR-gamefeel-004 | I.4 螢幕震動上限：單事件 ≤24px；連續事件取最大不累加；死亡時敵彈仍可視 | ADR-0003 ✅ |
| TR-gamefeel-005 | I.5 可讀性護欄：閃光 ≤0.4s 淡出、SOFTENED 光暈渲染於敵彈層下、判定點閃光可辨 | ADR-0003 ✅ + design |
| TR-gamefeel-006 | I.7 Reduce-Motion：震動 25%、閃光消失、慢動作停用、頓幀縮 50%、即時生效 | ADR-0003 ✅ |
| TR-gamefeel-007 | I.8 全值資料驅動：`GameFeelConfig` 含 G 節全旋鈕、改值即時反映、無硬編碼繞過 | ADR-0003 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/game-feel.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories game-feel` to break this epic into implementable stories.
