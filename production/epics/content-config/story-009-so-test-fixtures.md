# Story 009: SO Test Fixture Support

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M (3h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `docs/architecture/architecture.md` §6（可測性設計；無獨立 GDD）
**Requirement**: `TR-content-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: 測試不使用行內魔數；以 `ScriptableObject.CreateInstance<T>()` 工廠方法建立最小有效 SO fixture；系統以建構子/方法接收 `IContentRegistry` 介面，使測試可注入假 SO。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `ScriptableObject.CreateInstance<T>()` 在 EditMode 測試中穩定可用（Unity Test Framework 支援）；`[TearDown]` 使用 `Object.DestroyImmediate(so)` 釋放記憶體。無 post-cutoff 風險。

**Control Manifest Rules (this layer)**:
- Required: 工廠 helpers 位於 `Assets/_Project/Tests/EditMode/Content/Fixtures/`；不含任何 magic number（值使用具名常數或工廠參數）；每個工廠 helper 建立**最小有效**的 SO（OnValidate 不報錯的最低值集合）
- Forbidden: 在工廠 helper 中硬編碼具體 GDD 平衡值—平衡值屬 `.asset` 檔；工廠 helper 只設定「結構有效」的最小值
- Guardrail: 測試 fixture 只在 `Tests/` Assembly 引用；不從 `KaijuBreaker.*` 系統組件引用 fixture 工廠

---

## Acceptance Criteria

*From ADR-0003 §C.4 (測試可注入) and coding-standards.md (可測性要求):*

**Fixture 工廠 helpers**
- [ ] `WeaponDefFixtures` 靜態類提供：`MinimalLaserWeaponDef()` 返回有效 `WeaponDef`（`WeaponType = Laser`、所有雷射欄位設為 OnValidate 通過的最小值）；`MinimalMissileWeaponDef()` 同理
- [ ] `WeaponBalanceConfigFixtures` 靜態類提供：`Default()` 返回有效 `WeaponBalanceConfig`（所有欄位設 GDD 中點值，OnValidate 無錯誤）
- [ ] `PartSystemConfigFixtures` 靜態類提供：`Default()` 返回有效 `PartSystemConfig`
- [ ] `KaijuDefFixtures` 靜態類提供：`MinimalOnePartKaiju(string kaijuId)` 返回 `KaijuDef`（含 1 個 BossCore `PartDef`，OnValidate 通過）
- [ ] `DifficultyConfigFixtures` 靜態類提供：`Default()` 返回有效 `DifficultyConfig`（`EnemyCountMult = {1,1,1,1}`、`BulletDensityMult = {1,1,1,1}`—刻意用最小倍率以隔離非難度相關的測試）
- [ ] `GameFeelConfigFixtures` 靜態類提供：`Default()` 返回有效 `GameFeelConfig`（ShakeMagnitudeCap ≥ 最大 shake 值；所有 a11y 乘數 = 1.0）
- [ ] `EmitterPatternFixtures` 靜態類提供：`LinearBurst(int bulletCount)` 返回有效 `EmitterPatternSO`
- [ ] `EnemyDefFixtures` 靜態類提供：`SimpleEnemy(string enemyId, bool isElite = false)` 返回有效 `EnemyDef`（T1 hp tier、contact damage > 0）
- [ ] `ContentRegistryStub` 類別實作 `IContentRegistry`；以 constructor 參數 `Dictionary<string, ScriptableObject>` 作為 lookup table；提供便利工廠 `ContentRegistryStub.WithDefaults()` 以 fixture 工廠預建所有 global config SO

**OnValidate 單元測試**
- [ ] `WeaponBalanceConfigValidationTests` EditMode test class 覆蓋：`StaggerDuration_BelowMin_LogsError`、`HMaxNormal_AboveMax_LogsError`、`AllDefaultValues_NoError`
- [ ] `DifficultyConfigValidationTests` 覆蓋：`ArrayLength_NotFour_LogsError`、`D1Multiplier_BelowOne_LogsError`、`DefaultValues_NoError`
- [ ] `GameFeelConfigValidationTests` 覆蓋：`ShakeMagnitudeCap_BelowMaxShake_LogsError`、`SlowmoTimescale_AboveOne_LogsError`、`DefaultValues_NoError`
- [ ] 上述測試均位於 `Assets/_Project/Tests/EditMode/Content/`；命名規格：`[TypeName]_[Scenario]_[Expected]`

---

## Implementation Notes

*Derived from ADR-0003 §C.4 and coding-standards.md (test isolation rules):*

**CreateInstance Pattern**：
```csharp
// 每個工廠方法內部結構（以 WeaponBalanceConfig 為例）：
public static WeaponBalanceConfig Default()
{
    var so = ScriptableObject.CreateInstance<WeaponBalanceConfig>();
    so.D0Reference = 100f;          // 最小有效值（非 GDD 精確值）
    so.HMaxNormal = 100f;
    // ... 僅設 OnValidate 需要的最小欄位集
    return so;
}
```

**LogError 攔截 Pattern**（OnValidate 測試）：
```csharp
[Test]
public void StaggerDuration_BelowMin_LogsError()
{
    var logReceived = false;
    Application.logMessageReceived += (msg, _, type) =>
    {
        if (type == LogType.Error && msg.Contains("StaggerDuration"))
            logReceived = true;
    };
    var so = ScriptableObject.CreateInstance<WeaponBalanceConfig>();
    so.StaggerDuration = 0.5f; // 低於下限
    so.OnValidate();            // 如需公開：internal for testing / [EditorOnly]
    Assert.IsTrue(logReceived, "Expected LogError for StaggerDuration out of range");
    Object.DestroyImmediate(so);
}
```

`OnValidate` 為 Unity Magic Method（在 Inspector 中自動呼叫）但在 C# 中是 `protected`；需要在 SO 類別上添加 `internal void OnValidateForTesting() => OnValidate();` 或使用 `[TestFixture]` with `ReflectionHelper`。**建議：在 SO 類中新增 `internal void ForceValidate() => OnValidate();`**（`KaijuBreaker.Content.Tests` assembly 設為 `internalsVisibleTo`）。

**Tear Down**：所有測試方法必須在 `[TearDown]` 中 `Object.DestroyImmediate(so)` 釋放 CreateInstance 建立的 SO，防止測試間記憶體汙染。

**無魔數原則**：工廠 helper 設的值使用具名常數（`private const float MinimumHMax = 80f`），不用行內 `80f`。

---

## Out of Scope

- Stories 001–007：SO 類別定義（本故事依賴這些類別已存在）
- [Story 008]：`ContentRegistryStub` 在本故事建立；ContentRegistry 正式實作在 Story 008
- 其他系統（Weapons、Stage 等）的測試本身—本故事只建 Content 層的 fixture 基礎建設
- Visual/feel 測試（shader 輸出、VFX 外觀）—不屬於自動化測試範圍

---

## QA Test Cases

*Logic story — automated unit test specs (EditMode):*

- **AC-1**: WeaponBalanceConfig.Default() fixture 通過 OnValidate
  - Given: 呼叫 `WeaponBalanceConfigFixtures.Default()` 建立 SO
  - When: 呼叫 `so.ForceValidate()`
  - Then: 無 `LogError` 產生；所有欄位值非 C# 預設 0（float 欄位均有設值）
  - Teardown: `Object.DestroyImmediate(so)`

- **AC-2**: WeaponBalanceConfig 越界值觸發 LogError
  - Given: 呼叫 `WeaponBalanceConfigFixtures.Default()`；修改 `so.HMaxNormal = 50f`
  - When: 呼叫 `so.ForceValidate()`
  - Then: `Application.logMessageReceived` 收到 `LogType.Error`；message 含 `"HMaxNormal"`
  - Teardown: `Object.DestroyImmediate(so)`

- **AC-3**: ContentRegistryStub.WithDefaults() 返回完整查詢結果
  - Given: `var registry = ContentRegistryStub.WithDefaults()`
  - When: 呼叫 `registry.GetDifficultyConfig()`
  - Then: 返回非 null 的 `DifficultyConfig`；`EnemyCountMult.Length == 4`；無 `ContentNotFoundException`

- **AC-4**: 測試隔離性—DestroyImmediate 執行
  - Given: 連續執行 `WeaponDefFixtures.MinimalLaserWeaponDef()` 測試 10 次
  - When: 每次 `[TearDown]` 呼叫 `Object.DestroyImmediate(so)`
  - Then: Unity Memory Profiler 顯示 SO 已釋放；無 orphaned ScriptableObject 警告

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: EditMode 自動化測試通過 — `Assets/_Project/Tests/EditMode/Content/`
- `WeaponBalanceConfigValidationTests.cs`
- `DifficultyConfigValidationTests.cs`
- `GameFeelConfigValidationTests.cs`
- `ContentRegistryStubTests.cs`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Stories 001–007（所有 SO 類型需先定義；fixture 工廠針對各 SO 類型）
- Unlocks: 所有其他系統 Epic 的 EditMode 單元測試（使用 Content fixture 基礎建設注入假 config）
