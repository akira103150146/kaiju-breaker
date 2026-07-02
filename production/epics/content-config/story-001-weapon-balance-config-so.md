# Story 001: WeaponBalanceConfig + WeaponDef ScriptableObjects

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: M (3-4h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-content-002`, `TR-content-001`, `TR-content-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: 所有 GDD 調校旋鈕以唯讀 ScriptableObject 資產表達於 `Assets/_Project/Content/`，取代 GDD YAML 佔位路徑；測試以假 SO fixture 注入，系統以介面/建構子接收 config。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `ScriptableObject`、`CreateAssetMenu`、`OnValidate`、`SerializeField` 均為 Unity 5 起穩定 API，6.3 無破壞性變更。無需查 engine-reference。

**Control Manifest Rules (this layer)**:
- Required: `KaijuBreaker.Content.asmdef`；所有欄位 `[SerializeField]` private，公開 read-only 屬性；`OnValidate` 覆蓋 GDD G.1/G.2/G.3 全部安全範圍
- Forbidden: 引用任何系統組件（Weapons、KaijuParts 等）；運行期行為邏輯；硬編碼平衡值
- Guardrail: Content → Core 單向依賴；SO 為純資料容器

---

## Acceptance Criteria

*From `weapon-system.md` G.1 (WeaponBalanceConfig) and G.2–G.3 (WeaponDef), governed by ADR-0003:*

- [ ] `WeaponBalanceConfig` C# class 位於 `Assets/_Project/Scripts/Content/`；`[CreateAssetMenu(menuName = "KaijuBreaker/WeaponBalanceConfig")]`
- [ ] `WeaponBalanceConfig` 持有 G.1 全部 16 個全域旋鈕（C# 名稱用 PascalCase）：`D0Reference`、`HMaxNormal`、`HMaxArmored`、`HMaxBossCore`、`HDecayRate`、`ThetaS`、`ThetaSExit`、`BMaxNormal`、`BMaxArmored`、`BMaxBossCore`、`BUnsoftenedMult`、`RequiredBreakThresholdNormal`、`RequiredBreakThresholdArmored`、`RequiredBreakThresholdBossCore`、`StaggerDuration`、`StaggerBreakMult`
- [ ] `StaggerDuration` 是 `stagger_duration`（kaiju-part-system.md G.1）與 `l3_stagger_window`（weapon-system.md G.2）的**唯一來源**；兩份 GDD 的旋鈕均映射至此欄位
- [ ] `WeaponDef` C# class 位於同組件；`WeaponType` enum（`Laser`/`Missile`）區分武器池；持有 G.2 雷射系旋鈕（`L1HRateFull`、`L1HRateCenter`、`L1T3ResidualRateMult`、`L1T3ResidualDuration`、`L2HRate`、`L2T3AutotrackHeatPct`、`L2T3AutotrackRangePx`、`L2T3AdjacentHeatPct`、`L3TapOutputMult`、`L3ChargeTime`、`L3ChargeOutputMult`、`L3ChargeCooldown`、`L3T3HeatInjectPct`、`L4FireInterval`、`L4HRate`、`L4T3AfterimageRateMult`、`L4T3AfterimageRateMultDuration`）及 G.3 飛彈系旋鈕（`M1MissilesPerShot`、`M1DmgPerMissileMult`、`M1MagSize`、`M1ReloadTime`、`M1TrackingAngleDeg`、`M1T3MissilesPerShot`、`M2MicroCount`、`M2ConeWidthPct`、`M2ReloadTime`、`M2T3MagCount`、`M2T3BurstMicroCd`、`M3DmgUnsoftenedMult`、`M3HeatShockFillMult`、`M3MagSize`、`M3ReloadTime`、`M3T3ChainDmgMult`、`M3T3ChainMaxTargets`、`M4AoeRadiusPct`、`M4DropYMinPct`、`M4DropYMaxPct`、`M4TotalOutputCapMult`、`M4SingleTargetMult`、`M4MagSize`、`M4ReloadTime`、`M4T3ChildCount`、`M4T3ChildDmgPct`）
- [ ] `WeaponBalanceConfig.asset` 位於 `Assets/_Project/Content/Weapons/`；全部 16 欄位以 GDD G.1 預設值填充
- [ ] 8 個 `WeaponDef.asset`：`L1_SpreadLaser`、`L2_FocusBeam`、`L3_WaveCannon`、`L4_PierceBeam`、`M1_HomingMissile`、`M2_SwarmLauncher`、`M3_APTorpedo`、`M4_ClusterBomb`；位於 `Assets/_Project/Content/Weapons/`；各自以 GDD G.2/G.3 預設值填充；非對應武器類型的欄位留 0/false
- [ ] `WeaponBalanceConfig.OnValidate()` 對全部 16 欄位斷言安全範圍（範圍見 G.1）；越界時 `Debug.LogError` 含欄位名稱與安全範圍
- [ ] `WeaponDef.OnValidate()` 對各武器類型的對應欄位斷言 G.2/G.3 安全範圍

---

## Implementation Notes

*Derived from ADR-0003 Implementation Guidelines:*

`WeaponBalanceConfig` 與 `WeaponDef` 均放 `KaijuBreaker.Content.asmdef`；`OnValidate` 使用 `Debug.LogError` 而非 `throw`（Editor-only 斷言模式）。`WeaponDef` 用平鋪欄位（不用巢狀 SO 或陣列 per tier），Tier 解析邏輯留給 `KaijuBreaker.Weapons`。

`M3T3ChainDmgMult` 與 `M3T3ChainMaxTargets` 定義於 M3 的 `WeaponDef`，供 `KaijuParts` 系統讀取 Tier-3 鏈式效果—欄位的所有權在武器 SO，消費端為部位系統（ADR-0002 介面注入）。

資產命名：`PascalCase.asset`（architecture.md §3.3）。

---

## Out of Scope

- [Story 008]: ContentRegistry 依 `WeaponId` 查找 `WeaponDef` 的執行期查詢服務
- [Story 009]: `WeaponDef` / `WeaponBalanceConfig` 的 EditMode 測試 fixture 工廠
- `KaijuBreaker.Weapons` 武器系統邏輯—本故事只建立資料容器
- Stage 2/3 武器（全 8 武器在本故事完成；後期關卡不新增武器 SO 種類）

---

## QA Test Cases

*Config/Data — manual smoke check steps:*

- **AC-1**: WeaponBalanceConfig 預設值正確載入
  - Setup: 開啟 Unity Editor；在 Project 視窗選取 `WeaponBalanceConfig.asset`
  - Verify: Inspector 顯示全部 16 欄位值與 `weapon-system.md` G.1 預設值一致（例：`D0Reference = 100`、`HMaxNormal = 100`、`BUnsoftenedMult = 0.35`、`StaggerDuration = 2.0`）
  - Pass condition: 16 個欄位全部匹配；無欄位殘留 C# 預設值 0（GDD 明定非零者）

- **AC-2**: WeaponBalanceConfig OnValidate 越界偵測
  - Setup: 在 Inspector 將 `HMaxNormal` 改為 `50`（低於安全下界 80）
  - Verify: Unity Console 出現含 `HMaxNormal` 與安全範圍 `[80, 150]` 的 `LogError`
  - Pass condition: 改回 `100` 後 Console 無新錯誤；`Debug.LogError` 非 `throw`（不中斷 Editor 流程）

- **AC-3**: 全部 8 個 WeaponDef assets 可載入且已填值
  - Setup: 在 Project 視窗展開 `Assets/_Project/Content/Weapons/`
  - Verify: 8 個 `.asset` 檔存在；逐一選取確認各自的武器專屬欄位已填 GDD 預設（例：`L2_FocusBeam.asset` 的 `L2HRate = 37.5`、`M3_APTorpedo.asset` 的 `M3DmgUnsoftenedMult = 3.0`）
  - Pass condition: 所有武器型別對應欄位均為 GDD 規定值；非此武器類型的欄位留 0 但 OnValidate 無誤

- **AC-4**: WeaponDef OnValidate 越界偵測
  - Setup: 選取 `L2_FocusBeam.asset`；將 `L2HRate` 改為 `10`（低於安全下界 28）
  - Verify: Console `LogError` 含 `L2HRate`
  - Pass condition: 還原至 `37.5` 後無錯誤

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass — `production/qa/smoke-content-config.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None（Foundation 最優先）
- Unlocks: Story 008 (ContentRegistry 需要 WeaponDef 類型), Story 009 (test fixture 需要 WeaponDef 類型)
