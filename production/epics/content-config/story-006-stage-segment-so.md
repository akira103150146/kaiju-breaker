# Story 006: StageDef + SegmentDef + PodDropConfig ScriptableObjects

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: M (3-4h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/stage-system.md`（K.2 關卡池旋鈕、K.3 武器莢艙旋鈕、F.2 莢艙生命週期）
**Requirement**: `TR-content-002`, `TR-content-001`, `TR-content-004`, `TR-content-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: 關卡結構、分段池配置、武器莢艙投放規則均以唯讀 SO 承載；**難度乘數不在此 SO 中**（TR-content-004 單一來源為 Story 003 的 `DifficultyConfig`）。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `ScriptableObject[]` 引用（SegmentDef 池、EnemyDef 引用）在 Unity Inspector 穩定序列化。無 post-cutoff 風險。

**Control Manifest Rules (this layer)**:
- Required: `KaijuBreaker.Content.asmdef`；`StageDef` 引用 `KaijuDef`（同組件），不引用系統類型；`OnValidate` 驗證 K.2/K.3 安全範圍
- Forbidden: 複製難度乘數（`EnemyCountMult`、`BulletDensityMult`）—單一來源為 `DifficultyConfig`（Story 003）；引用系統組件
- Guardrail: `StageDef` 只描述關卡結構；排程/序列邏輯在 `KaijuBreaker.Stage` 系統

---

## Acceptance Criteria

*From `stage-system.md` K.2 (段落池)、K.3 (莢艙投放)、F.2 (莢艙生命週期), governed by ADR-0003:*

**StageDef**
- [ ] `StageDef` C# class 位於 `Assets/_Project/Scripts/Content/`；`[CreateAssetMenu(menuName = "KaijuBreaker/StageDef")]`
- [ ] 持有：`StageId`（string）、`BossKaijuId`（string）、`SegmentDrawCount`（int，K.2 每局抽取段落數）、`NoRepeatWindow`（int，K.2 無重複窗口）、`PreBossLullDurationSeconds`（float，K.2 Boss 前緩衝）、`SegmentPool`（`SegmentDef[]`，此 Stage 可選的段落集合）、`PrimaryWeaponPool`（`WeaponDef[]`）、`SecondaryWeaponPool`（`WeaponDef[]`）
- [ ] `StageDef.OnValidate()` 斷言：`StageId` 非空；`SegmentDrawCount` ∈ [1, 10]；`PreBossLullDurationSeconds` > 0；`SegmentPool` 長度 ≥ `SegmentDrawCount`（池大小需足以抽取）
- [ ] `Stage1.asset` 位於 `Assets/_Project/Content/Stages/`；`SegmentPool` 引用 Stage 1 全部 SegmentDef assets；`BossKaijuId = "carapex"`

**SegmentDef**
- [ ] `SegmentDef` C# class 位於同組件；`[CreateAssetMenu(menuName = "KaijuBreaker/SegmentDef")]`
- [ ] 持有：`SegmentId`（string）、`SegmentDisplayName`（string，開發用）、`EnemyPool`（`EnemyDef[]`，此段落可出現的敵人集合）、`WaveCount`（int）、`EliteWaveIndex`（int，-1 = 無 elite wave）、`MinDifficultyTier`（`DifficultyTier`，此段落最低難度需求）
- [ ] `SegmentDef.OnValidate()` 斷言：`SegmentId` 非空；`WaveCount` ∈ [1, 8]；`EnemyPool` 非空
- [ ] MVP Stage 1 段落 assets（`Assets/_Project/Content/Stages/Segments/`）：`S1_OpeningRush.asset`、`S1_CrissCross.asset`、`S1_ArtilleryRow.asset`（至少 3 個 placeholder SegmentDef；值由關卡設計故事填充）

**PodDropConfig**
- [ ] `PodDropConfig` C# class 位於同組件；`[CreateAssetMenu(menuName = "KaijuBreaker/PodDropConfig")]`
- [ ] 持有 K.3 全部旋鈕：`GuaranteedPrimaryPerStage`（int，1）、`GuaranteedSecondaryPerStage`（int，1）、`PreBossLullPodCount`（int，1）、`PodCarrierFlashIntervalSeconds`（float，0.5）；持有 F.2 生命週期旋鈕：`PodCycleIntervalSeconds`（float，3.0）、`PodDwellTimeSeconds`（float，12.0）、`PodDescendSpeedPxPerSec`（float）、`PodReachableBandYPct`（float，可互動 Y 範圍百分比）、`PodBobAmplitudePx`（float）、`PodDespawnAfterSeconds`（float）
- [ ] `PodDropConfig.OnValidate()` 斷言：`GuaranteedPrimaryPerStage` ≥ 1；`PodDwellTimeSeconds` > `PodCycleIntervalSeconds`（莢艙停留要比循環間隔長）；`PodCarrierFlashIntervalSeconds` ∈ (0.0, 2.0]
- [ ] `PodDropConfig.asset` 位於 `Assets/_Project/Content/Stages/`；全部欄位以 K.3/F.2 預設值填充
- [ ] 確認：`StageDef` 無 `EnemyCountMult` / `BulletDensityMult` 欄位（單一來源為 DifficultyConfig）

---

## Implementation Notes

*Derived from ADR-0003 and stage-system.md §F/K:*

**TR-content-004 執行**：`stage-system.md` K.1 列出難度乘數旋鈕，但這些數值的**擁有者是 `DifficultyConfig`**（Story 003），`StageDef` 不自持乘數。Stage 系統在執行期透過 ContentRegistry 取得 DifficultyConfig，再取對應 Tier 的乘數。

`StageDef.SegmentPool` 是 `SegmentDef[]`（Unity SO 陣列引用）；執行期抽段邏輯（shuffle、`NoRepeatWindow` 過濾）在 `KaijuBreaker.Stage` 系統，不在此 SO。

`SegmentDef.EnemyPool` 是可出現的 `EnemyDef[]`（不是固定排列），具體 wave 排列由 `KaijuBreaker.Stage` 的 WaveBuilder 依 `WaveCount` 與 `EliteWaveIndex` 動態生成。

Stage 1 MVP 只需 3 個 SegmentDef placeholder；完整 5 段（stage-system.md K.2）的內容由關卡設計故事補充。

---

## Out of Scope

- [Story 003]: `DifficultyConfig` 持有難度乘數（本故事 StageDef 不複製）
- [Story 005]: `EnemyDef` / `EmitterPatternSO` / `MovementPatternSO` 類型定義（本故事引用但不定義）
- [Story 008]: ContentRegistry 依 StageId 查找 StageDef
- [Story 009]: 三類 SO 的 fixture 工廠
- Wave 排列 / 段落序列排程邏輯（`KaijuBreaker.Stage` 系統）
- Stage 2/3 的 StageDef 與 SegmentDef assets（後續關卡內容故事）

---

## QA Test Cases

*Config/Data — manual smoke check steps:*

- **AC-1**: PodDropConfig 全部旋鈕填值正確
  - Setup: 選取 `PodDropConfig.asset`
  - Verify: `GuaranteedPrimaryPerStage = 1`、`PreBossLullPodCount = 1`、`PodCarrierFlashIntervalSeconds = 0.5`、`PodCycleIntervalSeconds = 3.0`、`PodDwellTimeSeconds = 12.0`
  - Pass condition: 全部 K.3/F.2 旋鈕符合 GDD；無欄位殘留 C# 預設 0

- **AC-2**: PodDropConfig OnValidate 莢艙停留 vs 循環時間檢查
  - Setup: 將 `PodDwellTimeSeconds` 改為 `2.0`（小於 `PodCycleIntervalSeconds = 3.0`）
  - Verify: Console `LogError` 提及 `PodDwellTimeSeconds` 不得短於 `PodCycleIntervalSeconds`
  - Pass condition: 還原 `12.0` 後無錯誤

- **AC-3**: StageDef 段落池引用完整
  - Setup: 選取 `Stage1.asset`
  - Verify: `SegmentPool` 長度 ≥ `SegmentDrawCount`；每個 SegmentDef 引用均非 None（無缺失引用）；`BossKaijuId = "carapex"`
  - Pass condition: Inspector 無黃色警告（Missing Reference）

- **AC-4**: StageDef 不含難度乘數欄位
  - Setup: 在 IDE 搜尋 `StageDef.cs` 是否含 `EnemyCountMult` 或 `BulletDensityMult`
  - Verify: 搜尋結果為空
  - Pass condition: 零重複欄位（單一來源為 DifficultyConfig）

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass — `production/qa/smoke-content-config.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003（DifficultyConfig 類型需先存在以確認 StageDef 不重複），Story 005（EnemyDef 類型需先存在供 SegmentDef.EnemyPool 引用）
- Unlocks: Story 008, Story 009
