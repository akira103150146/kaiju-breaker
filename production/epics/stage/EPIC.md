# Epic: 關卡系統與 Run 流程

> **Layer**: Core
> **GDD**: design/gdd/stage-system.md
> **Architecture Module**: `KaijuBreaker.Stage`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories stage`

## Overview

本 Epic 實作關卡結構與整輪生命週期：`KaijuBreaker.Stage` 驅動手作波段池的隨機重組（依 `difficulty_weight` 升序、no-repeat window）、6 種雜兵生成、武器莢艙保底掉落（雙池：主雷射 / 副飛彈），並擁有 **Run 狀態機（LOADOUT→STAGE→BOSS→RESULTS）**——狀態列舉於 `Core`，狀態轉換與存檔觸發點對齊 meta 跨輪流程。關卡高潮即巨獸；波段密度由 `Difficulty` 決定。狀態機為純 C# 類別，可用假事件驅動做 PlayMode 測試。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `stage-system.md` §L 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0005: 專案結構與組件邊界 | Run 狀態機純 C# 於 Stage（列舉在 Core）；場景以附加子場景串接；可假事件測試 | LOW |
| ADR-0003: 資料驅動調校 | `StageDef`+`SegmentDef`+`PodDropConfig`+`EnemyConfig` 承載波段池/莢艙/雜兵旋鈕 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-stage-001 | L.1 關卡結構完整性：引入→隨機波段×N→前頭目喘息→頭目；排序/no-repeat/難度過濾正確（100 次生成測試） | ADR-0005 / ADR-0003 ✅ |
| TR-stage-002 | L.2 武器莢艙保底：抵達喘息前 ≥1 主 + ≥1 副莢；喘息補 1 莢（200 輪測試） | ADR-0003 ✅ |
| TR-stage-003 | L.3 難度縮放一致性：敵人數/子彈密度符 config；速度/波段池/莢艙規則 D1–D4 恆定 | ADR-0003 ✅ |
| TR-stage-004 | L.4 引導設計達成率：Stage 1 新手描述核心循環 ≥70%；前 10 分必拾莢；首次破壞 <5 分 | ❌ 無 ADR（design GDD，playtest 驗收） |
| TR-stage-005 | L.5 縱列預告效果：S3-02 縱列 x 偏差 ≤5px；L4 縱列 TTK ≤ 40%；穿透感知 ≥60% | ❌ 無 ADR（design GDD）|
| TR-stage-006 | L.6 視覺可讀性：6 種雜兵子彈 D4 辨識 ≥80%；莢艙誤認 ≤10%；攜帶者圖示 ≥80% | ❌ 無 ADR（design GDD）；對齊 ADR-0001 可讀性護欄 |
| TR-stage-007 | Run 狀態機 LOADOUT→STAGE→BOSS→RESULTS 驅動，存檔觸發點對齊 meta 跨輪流程 | ADR-0005 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/stage-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories stage` to break this epic into implementable stories.
