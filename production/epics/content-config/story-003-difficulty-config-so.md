# Story 003: DifficultyConfig ScriptableObject

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: S (2h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/difficulty-system.md`
**Requirement**: `TR-content-002`, `TR-content-001`, `TR-content-004`, `TR-content-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: 所有難度調校旋鈕（含乘數陣列）以唯讀 SO 承載；`DifficultyConfig` 是難度乘數的**唯一擁有者**（TR-content-004）；`stage-system.md` K.1 列出的同名旋鈕均指向此 SO，不複製值。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 固定長度陣列（`float[]` 長度 4）在 Unity Inspector 正常序列化；無 post-cutoff 風險。

**Control Manifest Rules (this layer)**:
- Required: `KaijuBreaker.Content.asmdef`；`OnValidate` 驗證陣列長度恰為 4（D1–D4）、D1 乘數 = 1.0 不變式
- Forbidden: 在任何其他 SO 複製難度乘數值；引用系統組件
- Guardrail: `DifficultyConfig` 是 D1–D4 乘數的單一來源；StageDef（Story 006）讀取此 SO，不自持乘數

---

## Acceptance Criteria

*From `difficulty-system.md` G.1 and G.2, governed by ADR-0003:*

- [ ] `DifficultyConfig` C# class 位於 `Assets/_Project/Scripts/Content/`；`[CreateAssetMenu(menuName = "KaijuBreaker/DifficultyConfig")]`
- [ ] `DifficultyConfig` 持有 G.1 全部乘數：`EnemyCountMult`（float[4]，值 {1.00, 1.25, 1.50, 1.75}）、`BulletDensityMult`（float[4]，值 {1.00, 1.25, 1.50, 2.00}）；索引 0–3 對應 D1–D4
- [ ] `DifficultyConfig` 持有 G.2 UI 行為旋鈕：`DefaultDifficultyOnFirstLaunch`（`DifficultyTier` enum，D1）、`RememberLastDifficulty`（bool，true）、`MidRunDifficultyChangeAllowed`（bool，false）、`EnemyCapPerScene`（int，20）
- [ ] `DifficultyConfig.asset` 位於 `Assets/_Project/Content/Difficulty/`；全部欄位以 G.1/G.2 預設值填充
- [ ] `OnValidate()` 斷言：兩陣列長度 = 4；`EnemyCountMult[0]` = 1.0（D1 不得為倍率縮減）；`BulletDensityMult[0]` = 1.0；`EnemyCapPerScene` ∈ [1, 50]
- [ ] `DifficultyTier` enum（`D1`/`D2`/`D3`/`D4`）定義於 `KaijuBreaker.Content` 組件並作為公開類型共享

---

## Implementation Notes

*Derived from ADR-0003 and difficulty-system.md §C (單一來源原則):*

`EnemyCountMult` 與 `BulletDensityMult` 使用 `float[]` 而非 4 個獨立 `float` 欄位，以便在 Inspector 中保持可讀性並支援程式化索引（`DifficultyConfig.EnemyCountMult[(int)tier - 1]`）。

**TR-content-004 執行**：`stage-system.md` K.1 的難度旋鈕（同名同值）不新增至 `StageDef`；Story 006 的 `StageDef` 透過 ContentRegistry 讀取 `DifficultyConfig`，或由 Difficulty 系統在執行期注入。若有設計師需要在 StageDef Inspector 直接查看當前乘數，提供 `[ContextMenu]` 方法印出 log 即可，不複製欄位。

`DifficultyTier` enum 需要放在 `KaijuBreaker.Content`（不能放在 Difficulty system 組件），使所有讀取難度 config 的系統可以直接使用。

---

## Out of Scope

- [Story 008]: ContentRegistry 提供 `GetDifficultyConfig()` 查詢
- [Story 009]: `DifficultyConfig` fixture 工廠
- Difficulty UI 互動邏輯（`KaijuBreaker.Difficulty` 系統—本故事只建資料容器）
- Mid-run 難度切換 UI/UX 實作（`MidRunDifficultyChangeAllowed` 旋鈕已存，功能留給 Difficulty 系統故事）

---

## QA Test Cases

*Config/Data — manual smoke check steps:*

- **AC-1**: DifficultyConfig 乘數陣列預設值正確
  - Setup: 選取 `DifficultyConfig.asset`
  - Verify: Inspector 顯示 `EnemyCountMult` 長度 4、值 {1.00, 1.25, 1.50, 1.75}；`BulletDensityMult` 長度 4、值 {1.00, 1.25, 1.50, 2.00}；`DefaultDifficultyOnFirstLaunch = D1`、`RememberLastDifficulty = true`、`MidRunDifficultyChangeAllowed = false`、`EnemyCapPerScene = 20`
  - Pass condition: 全部 8 個 G.1/G.2 旋鈕值與 GDD 完全一致

- **AC-2**: OnValidate 偵測 D1 乘數被錯誤修改
  - Setup: 將 `EnemyCountMult[0]`（D1）改為 `0.8`（小於 1.0）
  - Verify: Console `LogError` 含 `EnemyCountMult[0]`（D1 基準值不得低於 1.0）
  - Pass condition: 還原 `1.0` 後無錯誤

- **AC-3**: OnValidate 偵測陣列長度錯誤
  - Setup: 在 Inspector 將 `EnemyCountMult` 陣列大小改為 3
  - Verify: Console `LogError` 含 `EnemyCountMult` 與 `length must be 4`
  - Pass condition: 還原長度 4 後無錯誤

- **AC-4**: DifficultyTier enum 可在其他程式碼使用
  - Setup: 在任意 `KaijuBreaker.*` 組件（非 Content）中引用 `DifficultyTier.D1`
  - Verify: 編譯成功（enum 在 Content 組件為 public）
  - Pass condition: 零編譯錯誤

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass — `production/qa/smoke-content-config.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None（可與 001、002 並行開發）
- Unlocks: Story 006 (StageDef 依賴 DifficultyConfig 為乘數來源), Story 008, Story 009
