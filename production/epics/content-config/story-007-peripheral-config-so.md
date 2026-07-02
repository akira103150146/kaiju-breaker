# Story 007: EconomyConfig + InputSettings + SaveConfig ScriptableObjects

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: S (2h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/` (多份 GDD 的 meta/peripheral 旋鈕)
**Requirement**: `TR-content-002`, `TR-content-001`, `TR-content-003`, `TR-content-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: `EconomyConfig`、`InputSettings`、`SaveConfig` 均為唯讀 SO；`SaveConfig` 持有存檔系統設定旋鈕（路徑、備援策略）但**不存玩家存檔資料**，嚴格遵守 TR-content-003（靜態 SO vs 玩家可變 JSON 分離）。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `Application.persistentDataPath` 在 Runtime 取值，不硬編碼路徑字串；`SaveConfig` 只持有相對路徑片段（如檔名、子目錄）。無 post-cutoff 風險。

**Control Manifest Rules (this layer)**:
- Required: `KaijuBreaker.Content.asmdef`；`OnValidate` 驗證各 SO 安全範圍
- Forbidden: `SaveConfig` 持有任何玩家可變資料（存檔路徑以外的存檔內容）；在任何其他 SO 複製已定義的旋鈕
- Guardrail: TR-content-003—`SaveConfig` 只描述**如何**存檔（路徑片段、策略旋鈕），**不儲存**存檔本體；玩家資料存 JSON（ADR-0004 範疇）

---

## Acceptance Criteria

*From ADR-0003 SO 映射表，各 GDD 調校旋鈕 (meta/peripheral 群組):*

**EconomyConfig**
- [ ] `EconomyConfig` C# class 位於 `Assets/_Project/Scripts/Content/`；`[CreateAssetMenu(menuName = "KaijuBreaker/EconomyConfig")]`
- [ ] 持有碎片產量旋鈕：`ShardYieldBase`（int，1）、`ShardYieldSoftenedMult`（float，1.5）、`ShardYieldSoftenedStaggeredMult`（float，2.0）、`EliteShardBonus`（int，3）
- [ ] 持有核心產量旋鈕（關聯 break quality）：`CoreYieldBossCore`（int，確定掉落數）；其餘部位類型 core yield 由後續 economy GDD 補充
- [ ] 持有升級成本骨架欄位：`WeaponUpgradeCostT1ToT2`（int）、`WeaponUpgradeCostT2ToT3`（int）；具體值由 economy GDD（`design/gdd/material-economy.md`）確認後填充
- [ ] `EconomyConfig.asset` 位於 `Assets/_Project/Content/Economy/`；已知旋鈕以 GDD / ADR-0003 預設值填充
- [ ] `OnValidate()` 斷言：`ShardYieldBase` ≥ 1；`ShardYieldSoftenedMult` ≥ 1.0；`ShardYieldSoftenedStaggeredMult` ≥ `ShardYieldSoftenedMult`（堆疊乘數不得低於基礎乘數）

**InputSettings**
- [ ] `InputSettings` C# class 位於同組件；`[CreateAssetMenu(menuName = "KaijuBreaker/InputSettings")]`
- [ ] 持有 ADR-0003 映射的輸入旋鈕：`TouchSensitivity`（float，1.0）、`TouchDeadzoneRadius`（float，px）、`RelativeDragLerp`（float，0–1）、`GamepadDeadzoneNormalized`（float，0–1）、`L3ChargeHoldThresholdSeconds`（float，觸控長按觸發 L3 蓄力的時間閾值）
- [ ] `InputSettings.asset` 位於 `Assets/_Project/Content/Input/`；欄位以合理預設值填充（具體值由 input GDD 確認）
- [ ] `OnValidate()` 斷言：`TouchSensitivity` ∈ (0.0, 3.0]；`RelativeDragLerp` ∈ (0.0, 1.0]；`GamepadDeadzoneNormalized` ∈ [0.0, 0.5]；`L3ChargeHoldThresholdSeconds` > 0

**SaveConfig**
- [ ] `SaveConfig` C# class 位於同組件；`[CreateAssetMenu(menuName = "KaijuBreaker/SaveConfig")]`
- [ ] 持有存檔系統配置旋鈕：`SaveFileName`（string，"player_save.json"）、`BackupFileName`（string，"player_save.bak.json"）、`TempFileName`（string，"player_save.tmp.json"）、`BackupWriteEveryN`（int，存 N 次後寫備援）、`CorruptionHandlingPolicy`（`CorruptionPolicy` enum：`WipeAndRestart`/`UseBackup`/`AlertUser`）
- [ ] **明確不持有**：玩家進度、武器解鎖狀態、任何 mutable 資料（OnValidate 無法驗證「是否被錯誤使用為存檔容器」，文件中明確標注）
- [ ] `SaveConfig.asset` 位於 `Assets/_Project/Content/Meta/`；欄位以合理預設值填充
- [ ] `OnValidate()` 斷言：`SaveFileName` 非空且不含路徑分隔符（只允許純檔名）；`BackupWriteEveryN` ≥ 1

---

## Implementation Notes

*Derived from ADR-0003 §C.3 (靜態 SO vs 可變 JSON):*

**TR-content-003 執行（SaveConfig）**：`SaveConfig` 只含「怎麼存」的**設定值**（檔名、策略），玩家存檔本體是 ADR-0004 定義的 JSON 格式，由 `KaijuBreaker.Persistence` 系統在 `Application.persistentDataPath + "/" + SaveConfig.SaveFileName` 路徑讀寫。兩者生命週期不同：SO 跟 build 走，JSON 存在裝置上。

**EconomyConfig 旋鈕不完整**：`material-economy.md` 尚未完整讀取，升級成本表的完整值待該 GDD 正式化後填充；本故事建立欄位骨架並以 ADR-0003 映射表已知值填入，其餘留 0 並在 `OnValidate` 中 `Debug.LogWarning`（非 Error）提示「待填值」。

`InputSettings.L3ChargeHoldThresholdSeconds` 為觸控平台專屬旋鈕（`weapon-system.md` G.2 中的 `l3_charge_time` 是**武器**的蓄力時間；此旋鈕是**輸入系統**的長按識別閾值）—兩者不同，不混用。

---

## Out of Scope

- [ADR-0004 Persistence 故事]: 玩家存檔 JSON schema、讀寫邏輯（SaveConfig 只設定，不實作）
- [Story 008]: ContentRegistry 提供 `GetEconomyConfig()`、`GetInputSettings()`、`GetSaveConfig()` 查詢
- [Story 009]: 三類 SO 的 fixture 工廠
- Economy / Upgrade 系統邏輯（`KaijuBreaker.Economy` 系統）
- 完整升級成本表（待 material-economy.md GDD 正式化後填充）

---

## QA Test Cases

*Config/Data — manual smoke check steps:*

- **AC-1**: EconomyConfig 碎片旋鈕預設值正確
  - Setup: 選取 `EconomyConfig.asset`
  - Verify: `ShardYieldBase = 1`、`ShardYieldSoftenedMult = 1.5`、`ShardYieldSoftenedStaggeredMult = 2.0`、`EliteShardBonus = 3`；升級成本欄位有值（非 0）或有 Warning log 提示待填
  - Pass condition: 已知旋鈕符合 GDD；未知旋鈕只有 Warning 而非 Error

- **AC-2**: EconomyConfig OnValidate 堆疊乘數驗證
  - Setup: 將 `ShardYieldSoftenedStaggeredMult` 設為 `1.2`（低於 `ShardYieldSoftenedMult = 1.5`）
  - Verify: Console `LogError` 含 `SoftenedStaggeredMult` 不得低於 `SoftenedMult`
  - Pass condition: 還原 ≥ 1.5 後無錯誤

- **AC-3**: SaveConfig TR-content-003 靜態/動態分離確認
  - Setup: 在 IDE 搜尋 `SaveConfig.cs` 中是否含 `List<>`、`Dictionary`、`WeaponUnlocked`、`HighScore` 等可變資料欄位名稱
  - Verify: 搜尋結果為空（SaveConfig 只含設定旋鈕，無玩家資料）
  - Pass condition: 零可變資料欄位

- **AC-4**: InputSettings OnValidate 範圍驗證
  - Setup: 將 `RelativeDragLerp` 改為 `1.5`（超過上限 1.0）
  - Verify: Console `LogError` 含 `RelativeDragLerp`
  - Pass condition: 還原 ≤ 1.0 後無錯誤

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass — `production/qa/smoke-content-config.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None（可與 001–005 並行開發）
- Unlocks: Story 008 (ContentRegistry 需要全套 SO 類型), Story 009 (test fixture)
