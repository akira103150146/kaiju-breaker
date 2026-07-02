# Story 008: ContentRegistry Service

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (3-4h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `docs/architecture/architecture.md` §6（ContentRegistry 設計節點）
**Requirement**: `TR-content-001`, `TR-content-003`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: `ContentRegistry` 服務以 Addressables 載入全套 SO 資產，提供型別安全的唯讀查詢介面；執行期不寫回 SO（TR-content-003）；`core_boot` Addressables 群組在遊戲啟動時非同步預載全域 config SO。

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**: Addressables API 在 Unity 6.x 有調整（`Addressables.LoadAssetAsync<T>` 回傳類型在 6.x 中為 `AsyncOperationHandle<T>`，2.x package 版本需查 `com.unity.addressables` changelog）。實作前查 `docs/engine-reference/godot/VERSION.md` 備注，並確認 Addressables 套件版本（Project Settings > Package Manager）。`IContentRegistry` 介面設計需與 Addressables 解耦，允許測試注入 stub。

**Control Manifest Rules (this layer)**:
- Required: `KaijuBreaker.Content.asmdef`；公開 `IContentRegistry` 介面；`ContentRegistry` 實作為 DI 注入，非 static singleton；Runtime 快取唯讀
- Forbidden: 回寫 SO 資產；靜態存取 `ContentRegistry.Instance`（改用介面注入）；將玩家存檔資料與 SO 混存
- Guardrail: `core_boot` 群組只含全域 config SO；EnemyDef、SegmentDef 等動態關卡資產放個別 Addressables 群組（stage_content）

---

## Acceptance Criteria

*From architecture.md §6 and ADR-0003 Implementation Guidelines:*

- [ ] `IContentRegistry` 介面定義於 `Assets/_Project/Scripts/Content/`；提供類型安全查詢方法：`GetWeaponBalanceConfig()`、`GetWeaponDef(string weaponId)`、`GetPartSystemConfig()`、`GetKaijuDef(string kaijuId)`、`GetDifficultyConfig()`、`GetGameFeelConfig()`、`GetEnemyDef(string enemyId)`、`GetStageDef(string stageId)`、`GetPodDropConfig()`、`GetEconomyConfig()`、`GetInputSettings()`、`GetSaveConfig()`
- [ ] `ContentRegistry` 類別實作 `IContentRegistry`；以 Addressables `LoadAssetAsync<T>` 異步載入，結果快取於 `Dictionary<string, ScriptableObject>`（runtime readonly）
- [ ] `ContentRegistry.InitializeAsync()` 預載 `core_boot` 群組全域 config SO（WeaponBalanceConfig、PartSystemConfig、DifficultyConfig、GameFeelConfig、PodDropConfig、EconomyConfig、InputSettings、SaveConfig）；完成前返回 `Task` / `UniTask`（視 Project 選用的 async 函式庫）
- [ ] 按需載入方法（`GetWeaponDef`、`GetKaijuDef`、`GetEnemyDef`、`GetStageDef`）：若快取命中直接返回；未命中則同步 throw `ContentNotFoundException`（呼叫端應先確保已預載對應 Addressables 群組）
- [ ] 執行期無 SO 回寫（`ContentRegistry` 只讀 SO 欄位，不呼叫 `EditorUtility.SetDirty`、不修改任何 SO 欄位值）
- [ ] `ContentNotFoundException`（自定義 Exception）包含遺失資產的 key 與型別資訊
- [ ] 至少一個 EditMode integration test 通過（見 QA Test Cases AC-2）

---

## Implementation Notes

*Derived from ADR-0003 §C.2 and architecture.md §6:*

**Addressables 版本確認**：實作前先執行 `Window > Package Manager`，確認 `com.unity.addressables` 版本。Unity 6.3 預設版本可能為 1.22.x 或 2.x；若為 2.x，`AsyncOperationHandle` API 有所不同（查 engine-reference 或官方 changelog）。

**DI Pattern**：`ContentRegistry` 應透過建構子注入（或 `[Inject]` 若 Project 採 DI Container）傳入 `IAddressablesLoader` 抽象，使測試可注入 stub loader（不觸發實際 Addressables 系統）。

**快取策略**：全域 config SO（WeaponBalanceConfig 等）在 `InitializeAsync()` 時全部預載；`WeaponDef`、`EnemyDef`、`KaijuDef` 等實例較多的 SO 可考慮 lazy load（首次查詢時載入並快取）。本故事 MVP 實作全部預載（最簡單），lazy 優化留 tech debt 紀錄。

**TR-content-003 守衛**：`IContentRegistry` 所有返回值為 `T`（不暴露 `ref`）；呼叫端不得取得 SO 的可寫引用。文件層面的保護：在 README/Interface doc comment 說明禁止修改返回值。

---

## Out of Scope

- Stories 001–007：SO 類型定義（本故事依賴這些類型，但不重新定義）
- [Story 009]：ContentRegistry 的 stub/mock 在測試故事中實作
- Addressables 群組設定（Build Settings、標籤、`core_boot` group asset 配置）—屬 DevOps 故事或 tools-programmer 範疇
- 玩家存檔 JSON 讀寫（ADR-0004 範疇；ContentRegistry 只處理靜態 SO）
- Hot-reload / editor Live Update 支援（後期優化，記入 tech debt）

---

## QA Test Cases

*Integration story — EditMode integration test specs:*

- **AC-1**: IContentRegistry 介面完整性
  - Given: `ContentRegistryStub` 實作 `IContentRegistry`（注入預建的假 WeaponDef SO fixture）
  - When: 呼叫 `GetWeaponDef("L2_FocusBeam")`
  - Then: 返回非 null 的 `WeaponDef`；`weaponDef.WeaponType == WeaponType.Laser`；無例外

- **AC-2**: ContentRegistry 快取命中路徑
  - Given: `ContentRegistry` 以 `StubAddressablesLoader`（立即返回預設 fixture，不觸發實際 Addressables）初始化；`InitializeAsync()` 已 await
  - When: 連續兩次呼叫 `GetDifficultyConfig()`
  - Then: 兩次返回相同物件引用（快取命中，無重複載入）；`StubAddressablesLoader.LoadCount` = 1

- **AC-3**: ContentNotFoundException 於遺失 key 時拋出
  - Given: `ContentRegistry` 已初始化；`GetWeaponDef("NONEXISTENT_ID")` 尚未預載
  - When: 呼叫 `GetWeaponDef("NONEXISTENT_ID")`
  - Then: 拋出 `ContentNotFoundException`；exception.Message 含 `"NONEXISTENT_ID"` 與 `"WeaponDef"`

- **AC-4**: 執行期無 SO 回寫驗證
  - Given: `ContentRegistry` 已初始化；從 `GetWeaponBalanceConfig()` 取得 `WeaponBalanceConfig` 引用
  - When: （手動 review）搜尋 `ContentRegistry.cs` 中所有 SO 使用點
  - Then: 無 `EditorUtility.SetDirty`、無 `so.SomeField =`（賦值）、無 `JsonUtility.FromJsonOverwrite`

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: EditMode integration test 通過 — `Assets/_Project/Tests/EditMode/Content/ContentRegistryIntegrationTest.cs`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Stories 001–007（全套 SO 類型需先定義；ContentRegistry 的查詢方法以這些類型為簽章）
- Unlocks: 所有需要讀取 config 的系統（KaijuBreaker.Weapons、KaijuBreaker.Stage 等）
