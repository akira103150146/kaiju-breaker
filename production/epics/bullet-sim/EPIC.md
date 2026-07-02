# Epic: 子彈/彈幕引擎（DOTS）

> **Layer**: Foundation
> **GDD**: design/gdd/bullet-system.md
> **Architecture Module**: `KaijuBreaker.BulletSim`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories bullet-sim`

## Overview

本 Epic 實作專案第一技術風險的核心：`KaijuBreaker.BulletSim` 以 DOTS/ECS + Burst + Jobs（Entities 1.3+）模擬大量同質敵彈——生成/積分/lifetime/離屏剔除/空間網格廣相碰撞，命中結果經 `NativeQueue<HitEvent>` 交主執行緒 Bridge 翻譯為匯流排事件。撰寫層 `EmitterPatternSO`（載入時烘焙為 Burst Blob）與模擬後端刻意解耦，使設計師可撰寫三頭目全部彈幕模式且後端可換。DOTS 依賴隔離於本單一組件。**目標：手機基準機 sustain 1,000 敵彈 @60fps、零 per-bullet GC——此為 ADR-0001 LOCK 前置的效能原型驗證閘門。**

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `bullet-system.md` §11 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: 彈幕引擎後端（旗艦）| 敵彈 DOTS/ECS+Burst+Jobs（隔離於 BulletSim），遊戲/UI Mono+池；撰寫層解耦保後端可換；單一 Bridge 橋接 | **HIGH（Status: Proposed；待效能原型於基準機達 1,000@60fps + 零 GC 後才 LOCK；Entities 1.3 時間注入/Blob 烘焙/World 生命週期均 [需查證 6.3 API]）** |
| ADR-0003: 資料驅動調校 | `EmitterPatternSO` 撰寫層 → 烘焙 Burst Blob，執行期唯讀 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-bullet-001 | 11.1 手機基準機 sustain 1,000 敵彈（含碰撞+繪製）穩定 60fps ≥60s 無掉幀；PC 2,000 | ADR-0001 ⚠️（Proposed，待效能原型驗證）|
| TR-bullet-002 | 11.2 一場最高密度戰鬥 GC Alloc = 0 B/frame（穩態） | ADR-0001 ⚠️（Proposed）|
| TR-bullet-003 | 11.3 三頭目全部模式可由 `EmitterPatternSO` 撰寫，無需新增 shape 或程式；設計師可 Inspector 調整 | ADR-0001 / ADR-0003 ✅ |
| TR-bullet-004 | 11.4 密度縮放 `ceil(base × bullet_density_mult)`；速度/形狀恆定；`readability_cap_priority` 截斷 | ADR-0001 / ADR-0003 ✅ |
| TR-bullet-005 | 11.5 單點判定；玩家飛彈→`on_missile_hit`、雷射→`on_laser_hit`/`on_l3_wave_hit`；L4 穿透對縱列各發一次 | ADR-0001 / ADR-0002 ✅ |
| TR-bullet-006 | 11.6 D4 最高密度「敵彈 vs 判定點」辨識率 ≥70%；敵彈暖色高對比、判定點恆亮永不遮蔽 | ADR-0001 ⚠️ + design GDD |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/bullet-system.md` are verified
- The ADR-0001 performance-prototype gate is measured on the mobile baseline device (1,000 bullets @60fps + zero GC), and ADR-0001 is transitioned Proposed → Accepted
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories bullet-sim` to break this epic into implementable stories.
