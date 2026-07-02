# Epic: 難度系統

> **Layer**: Core
> **GDD**: design/gdd/difficulty-system.md
> **Architecture Module**: `KaijuBreaker.Difficulty`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories difficulty`

## Overview

本 Epic 實作四階難度（D1 普通 / D2 困難 / D3 極限 / D4 惡夢）的密度縮放：`KaijuBreaker.Difficulty` 為 `enemy_count_mult`/`bullet_density_mult` 的唯一權威來源 (single source of truth)，經 `IDifficultyProvider` 供 `Stage`/`BulletSim` 唯讀取用。核心設計鐵則為「難度是門」——只縮放彈幕密度/雜兵數，**絕不縮放**部位 TTB、武器輸出、素材產量，亦不鎖內容。此不變性為阻斷級驗收（部位/武器/素材跨難度數值恆定的自動化矩陣測試）。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `difficulty-system.md` §H 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: 資料驅動調校 | 難度乘數唯一存 `DifficultyConfig`；其他系統唯讀取用不另存；唯一密度來源 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-difficulty-001 | H.1 乘數正確應用：D1–D4 實際敵人數/子彈數 = config 值（ceil ±1）；≥24 測試案例 | ADR-0003 ✅ |
| TR-difficulty-002 | H.2 部位 TTB 在 D1–D4 恆定（阻斷）：全域部位旋鈕與 TTB_base 跨難度完全相等（4×3 矩陣） | ADR-0003 ✅ |
| TR-difficulty-003 | H.3 武器輸出在 D1–D4 恆定（阻斷）：8 武器 30s 持續輸出跨難度相同（4×8 矩陣） | ADR-0003 ✅ |
| TR-difficulty-004 | H.4 素材產量在 D1–D4 等量：`difficulty_yield_bonus` 恆 0；36 測試案例 | ADR-0003 ✅ |
| TR-difficulty-005 | H.5 內容可及性：D1–D4 所有關卡可選、莢艙保底生效、無難度鎖定判斷 | ADR-0003 ✅ |
| TR-difficulty-006 | H.6 D1 可及性承諾：5 人新手 Stage 1 完整循環達成 ≥80%；D1 彈幕辨識 ≥80% | ❌ 無 ADR（design GDD，playtest 驗收） |
| TR-difficulty-007 | H.7 D4 彈幕可讀性下界：辨識率 ≥70%；未達標優先調降 `difficulty_bullet_mult[D4]`（安全範圍） | ❌ 無 ADR（design GDD）；處置對齊 ADR-0001 可讀性優先 |
| TR-difficulty-008 | H.8 難度選擇 UI 行為：首次預選 D1、記憶上輪、輪中灰化 | ADR-0003 ✅（config 預設）；UI 呈現跨 hud-ui 覆蓋 |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/difficulty-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories difficulty` to break this epic into implementable stories.
