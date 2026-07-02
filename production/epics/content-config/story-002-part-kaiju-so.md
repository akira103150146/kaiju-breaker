# Story 002: PartSystemConfig + KaijuDef ScriptableObjects

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: M (3h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/kaiju-part-system.md`
**Requirement**: `TR-content-002`, `TR-content-001`, `TR-content-004`, `TR-content-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: 所有 GDD 調校旋鈕以唯讀 SO 表達；跨 GDD 共享旋鈕由單一 SO 擁有（TR-content-004）；KaijuDef 的 `PartDef[]` 用 `[Serializable]` 嵌入類，不建立獨立 SO。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `[Serializable]` nested class in SO Inspector 在 Unity 5+ 穩定；無 post-cutoff 風險。`NullableFloat` 以 `float` + `bool UseOverride` 替代（Unity Inspector 不直接支援 `float?`）。

**Control Manifest Rules (this layer)**:
- Required: `KaijuBreaker.Content.asmdef`；`OnValidate` 斷言 G.3 安全範圍；`KaijuDef` 只含結構定義，不含行為
- Forbidden: 複製 `WeaponBalanceConfig` 已擁有的旋鈕（H_max_*、theta_S、stagger_duration 等）；引用系統組件
- Guardrail: `PartSystemConfig` 只持有 kaiju-part-system.md G.3 **部位系統專屬**旋鈕；其餘共用旋鈕由 Story 001 的 `WeaponBalanceConfig` 擁有

---

## Acceptance Criteria

*From `kaiju-part-system.md` G.3 (PartSystemConfig) and C.6 (KaijuDef schema):*

- [ ] `PartSystemConfig` C# class 位於 `Assets/_Project/Scripts/Content/`；`[CreateAssetMenu(menuName = "KaijuBreaker/PartSystemConfig")]`
- [ ] `PartSystemConfig` 持有 G.3 全部**部位系統專屬**旋鈕（不重複 WeaponBalanceConfig 已有欄位）：`PartRegenEnabled`（bool，false）、`ChainBreakIsRecursive`（bool，false）、`AdjacencyMaxNeighbors`（int，4）、`HitboxSizeMultiplierNormal`（float，1.0）、`HitboxSizeMultiplierArmored`（float，0.8）、`HitboxSizeMultiplierCore`（float，1.2）= 共 6 欄位
- [ ] 確認：`stagger_duration`、`theta_S`、`theta_S_exit`、`H_max_*`、`B_max_*` 均**不**出現在 `PartSystemConfig`（單一來源為 Story 001 的 `WeaponBalanceConfig`）
- [ ] `PartSystemConfig.asset` 位於 `Assets/_Project/Content/Parts/`；欄位以 G.3 預設值填充
- [ ] `PartSystemConfig.OnValidate()` 斷言安全範圍：`AdjacencyMaxNeighbors` ∈ [1, 8]、`HitboxSizeMultiplierNormal/Armored/Core` ∈ [0.5, 2.0]
- [ ] `KaijuDef` C# class 存在；內嵌 `[Serializable] PartDef` 類持有：`PartId`（string）、`PartType`（`PartType` enum：`Normal`/`Armored`/`BossCore`）、`HMaxOverride`（float，0 = 使用全域）、`HMaxUseOverride`（bool）、`BMaxOverride`（float，0 = 使用全域）、`BMaxUseOverride`（bool）、`Adjacency`（string[]）、`DropTableId`（string）
- [ ] `KaijuDef` 持有：`KaijuId`（string）、`Parts`（`PartDef[]`）；`[CreateAssetMenu(menuName = "KaijuBreaker/KaijuDef")]`
- [ ] 3 個骨架 `KaijuDef.asset` 位於 `Assets/_Project/Content/Kaiju/`：`Carapex.asset`、`Lacera.asset`、`Voltwyrm.asset`；`KaijuId` 欄位填入 ID，`Parts[]` 陣列長度 > 0（含 schema 正確的 placeholder PartDef）
- [ ] `KaijuDef.OnValidate()` 斷言：`KaijuId` 非空；`Parts` 陣列至少含 1 個 `BossCore` 類型部位

---

## Implementation Notes

*Derived from ADR-0003 Implementation Guidelines and kaiju-part-system.md §C.6:*

`PartDef` 以 `[Serializable]` 嵌入類存於 `KaijuDef`（非獨立 SO），因為部位資料的生命週期與 kaiju 一致。Unity Inspector 對 `float?` 支援差，用 `float HMaxOverride` + `bool HMaxUseOverride` 兩欄位等效表達可選覆蓋。

**單一來源原則（TR-content-004）**：`softened_visual_onset_max_s`（G.3 列出但屬視覺時序）屬於 Story 004 的 `GameFeelConfig`；`stagger_visual_onset_max_s` 同屬 `GameFeelConfig`。`PartSystemConfig` 只持有 G.3 中無其他 SO 擁有的旋鈕（共 6 個）。

KaijuDef.asset 的 PartDef[] 詳細部位資料（各 kaiju 具體部位結構、相鄰圖、掉落表）由對應 kaiju GDD 文件（`design/gdd/kaiju/`）提供；本故事建立 schema 與骨架 asset，值由後續 kaiju 內容故事填充。

---

## Out of Scope

- [Story 004]: `GameFeelConfig` 擁有 `softened_visual_onset_max_s`、`stagger_visual_onset_max_s`（視覺時序旋鈕）
- [Story 001]: `WeaponBalanceConfig` 擁有 H_max_*、B_max_*、stagger_duration 等共享旋鈕
- [Story 008]: ContentRegistry 依 KaijuId 查找 KaijuDef
- [Story 009]: `KaijuDef` / `PartSystemConfig` 的 EditMode 測試 fixture 工廠
- 各 KaijuDef.asset 的完整部位資料填值（留給 kaiju 內容 story）

---

## QA Test Cases

*Config/Data — manual smoke check steps:*

- **AC-1**: PartSystemConfig 預設值與 G.3 一致
  - Setup: 在 Project 視窗選取 `PartSystemConfig.asset`
  - Verify: `PartRegenEnabled = false`、`ChainBreakIsRecursive = false`、`AdjacencyMaxNeighbors = 4`、三個 hitbox 乘數符合 G.3 預設；Inspector 不顯示任何 `H_max_*`、`StaggerDuration` 欄位
  - Pass condition: 6 欄位值正確；無與 WeaponBalanceConfig 重複的欄位出現

- **AC-2**: PartSystemConfig OnValidate 越界偵測
  - Setup: 將 `HitboxSizeMultiplierArmored` 改為 `0.1`（低於安全下界 0.5）
  - Verify: Console `LogError` 含 `HitboxSizeMultiplierArmored`
  - Pass condition: 還原 `0.8` 後無錯誤

- **AC-3**: KaijuDef 骨架 assets 存在且 schema 正確
  - Setup: 在 Project 視窗展開 `Assets/_Project/Content/Kaiju/`
  - Verify: 三個 `.asset` 存在；選取各 asset 確認 `KaijuId` 非空、`Parts` 陣列長度 > 0；至少一個 PartDef 的 `PartType = BossCore`
  - Pass condition: OnValidate 無錯誤；三個 KaijuId 各不同

- **AC-4**: 無 WeaponBalanceConfig 旋鈕被複製
  - Setup: 在 IDE 搜尋 `PartSystemConfig.cs` 中是否含 `StaggerDuration`、`HMaxNormal`、`ThetaS` 等欄位名稱
  - Verify: 搜尋結果為空
  - Pass condition: 零重複欄位

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass — `production/qa/smoke-content-config.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（`WeaponBalanceConfig` 欄位清單需先確認，以確保 PartSystemConfig 不重複）
- Unlocks: Story 008 (ContentRegistry 需要 KaijuDef/PartSystemConfig 類型), Story 009 (test fixture)
