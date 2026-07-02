# Story 001: DifficultyConfig ScriptableObject + Core 基礎型別與介面

> **Epic**: 難度系統 (Difficulty System)
> **Status**: Ready
> **Layer**: Feature (Content + Core)
> **Type**: Config/Data
> **Estimate**: 2–3 h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/difficulty-system.md`
**Requirement**: `TR-difficulty-001` (partial — data asset definition and Core contract only; runtime application in Story 002)

> **Note**: `docs/architecture/tr-registry.yaml` 尚未正式化。TR-ID 由 `design/gdd/difficulty-system.md §H` 驗收標準推導，並在 `production/epics/difficulty/EPIC.md` 確認。

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 — ScriptableObject 為唯一調校資料來源
**ADR Decision Summary**: 所有 gameplay/balance 旋鈕以 ScriptableObject（唯讀）表達，放 `Assets/_Project/Content/`，取代 GDD 的 YAML 佔位路徑；難度乘數唯一存於 `DifficultyConfig`，其他系統不複製值，一律經 `IDifficultyProvider` 介面查詢。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `ScriptableObject`, `[CreateAssetMenu]`, `OnValidate` 均為 Unity 穩定 API，ADR-0003 標記 LOW risk。實作前仍需以 `docs/engine-reference/unity/VERSION.md` 確認 `CreateAssetMenu` 屬性語法是否有 6.3 變更，標示 `[需查證 6.3 API]`。

**Control Manifest Rules (Feature layer — from `docs/architecture/control-manifest.md` v2026-07-02)**:
- MUST (§1.2): 所有 gameplay/balance 數值來自 SO（`Assets/_Project/Content/`），執行期唯讀載入；SO 用 `OnValidate` 對 GDD 安全範圍做編輯期斷言
- MUST (§3 `Content`): `DifficultyConfig` 放 `Content` assembly；含 `OnValidate` 範圍檢查；MUST NOT 含執行期行為邏輯（純資料＋驗證）
- MUST (§3 `Core`): `DifficultyTier` enum 與 `IDifficultyProvider` 介面放 `Core` assembly；`Core` MUST NOT 依賴任何系統或 `Content`；不放任何實作邏輯
- MUST (§4.3): `IDifficultyProvider` 為跨系統唯讀查詢介面，放 `Core`，測試時注入假實作
- MUST NOT (§5): 硬編碼乘數值；在 `Content` SO 寫執行期邏輯

---

## Acceptance Criteria

*From GDD `design/gdd/difficulty-system.md` §G.1、§G.2、§D.3，scoped to this story:*

- [ ] `DifficultyTier` enum 存在於 `Core` assembly（`KaijuBreaker.Core`），成員為 `D1, D2, D3, D4`，無多餘值
- [ ] `IDifficultyProvider` 介面存在於 `Core` assembly，公開三個成員：`DifficultyTier CurrentTier { get; }`、`float GetEnemyCountMult(DifficultyTier tier)`、`float GetBulletDensityMult(DifficultyTier tier)`；無設定方法（唯讀查詢）
- [ ] `DifficultyConfig` ScriptableObject 存在於 `Content` assembly（`KaijuBreaker.Content`），欄位涵蓋 GDD §G.1 全部 8 個乘數旋鈕（`difficulty_enemy_mult[D1–D4]`、`difficulty_bullet_mult[D1–D4]`）及 §G.2 全部 4 個 UI 行為旋鈕（`default_difficulty_on_first_launch`、`remember_last_difficulty`、`mid_run_difficulty_change_allowed`、`enemy_cap_per_scene`）；無執行期邏輯
- [ ] `DifficultyConfig.OnValidate` 針對 §G.1 安全範圍做編輯期斷言：`enemy_mult[D1] == 1.0f`（閘門，違反 → `LogError`）；`bullet_mult[D1] == 1.0f`（閘門）；`enemy_mult[D2]` ∈ [1.10, 1.50]；`enemy_mult[D3]` ∈ [1.25, 1.75]；`enemy_mult[D4]` ∈ [1.50, 2.00]；`bullet_mult[D2]` ∈ [1.10, 1.50]；`bullet_mult[D3]` ∈ [1.25, 1.75]；`bullet_mult[D4]` ∈ [1.75, 2.50]
- [ ] `Assets/_Project/Content/Balance/DifficultyConfig.asset` 資產以 GDD §D.3 預設值預填：`enemy_mult` = {D1: 1.00, D2: 1.25, D3: 1.50, D4: 1.75}；`bullet_mult` = {D1: 1.00, D2: 1.25, D3: 1.50, D4: 2.00}；`enemy_cap_per_scene = 20`；`default_difficulty_on_first_launch = D1`；`remember_last_difficulty = true`；`mid_run_difficulty_change_allowed = false`
- [ ] 所有 public 類別與方法有 XML doc comment（control manifest §1.8）
- [ ] `DifficultyConfig` 不含任何 `MonoBehaviour` 繼承；不含 `Update()`、`Start()` 等執行期鉤子

---

## Implementation Notes

*Derived from ADR-0003 Decision §1–§4 and control manifest §3:*

`DifficultyTier` 與 `IDifficultyProvider` 必須進 `Core` assembly（`KaijuBreaker.Core.asmdef`）。這讓 `Stage`、`BulletSim`、`UI` 等所有 Feature 系統可透過 `Core` 引用此介面，且不需要互相引用（控制清單 §1.4 assembly boundary 要求）。

`DifficultyConfig` 進 `Content` assembly（`KaijuBreaker.Content.asmdef`）。欄位建議使用 `[SerializeField] private float[] _enemyCountMult` + `public property` 提供唯讀存取；或 `[field: SerializeField] public float[] EnemyCountMult { get; private set; }` — 以 Unity 6.3 慣用形式為準（查 engine-reference 確認）。

`OnValidate` 中 D1 乘數閘門用 `Debug.LogError`（非 `LogWarning`），確保設計師在 Inspector 意外移動 D1 值時有明確的阻斷提示。D2–D4 安全範圍用 `Debug.LogWarning` 提示越界即可（不阻斷，但需設計師確認）。

`IDifficultyProvider` 故意不暴露 `SetTier()` 或任何寫入方法 — 設定由 `App` 組合根（Composition Root）呼叫 `DifficultySystem` 具體類別的設定方法；消費端（Stage、BulletSim）只持有 `IDifficultyProvider` 介面，不知道設定路徑（DI 原則）。

測試路徑裁決（控制清單 §1.6）：Unity 專案測試放 `Assets/_Project/Tests/Difficulty/EditMode/`，非 `tests/unit/difficulty/`。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- [Story 002]: `DifficultySystem` 具體實作（implements `IDifficultyProvider`）、run-start 難度設定邏輯、乘數運行期套用、run 鎖定機制
- [Story 003]: 部位 TTB 與武器輸出不變性自動化測試（TR-difficulty-002、TR-difficulty-003）
- [Story 004]: 素材產量不變性與內容可及性測試（TR-difficulty-004、TR-difficulty-005）
- UI 難度選擇畫面、輪中灰化呈現（TR-difficulty-008 UI 部分）：屬 `hud-ui` epic；本 story 的 `mid_run_difficulty_change_allowed = false` 僅是 config flag，UI 讀取並呈現由 UI epic 負責
- D1 可及性 playtest（TR-difficulty-006）：Stage 1 MVP 里程碑 playtest 驗收，非本 epic 程式實作
- D4 彈幕可讀性 playtest（TR-difficulty-007）：Vertical Slice 里程碑 playtest 驗收

---

## QA Test Cases

*Logic / Config-Data story — EditMode automated test specs. 測試檔：`Assets/_Project/Tests/Difficulty/EditMode/DifficultyConfig_Validation_Test.cs`*

- **AC-1**: `DifficultyTier` enum 存在且完整
  - Given: `KaijuBreaker.Core.asmdef` 已編譯，測試使用 `typeof(DifficultyTier)`
  - When: 以 `System.Enum.GetValues(typeof(DifficultyTier))` 取得所有值
  - Then: 回傳陣列長度 == 4；包含 D1, D2, D3, D4（按順序 0–3）
  - Edge cases: 不含 D0 或 D5 等多餘值

- **AC-2**: `IDifficultyProvider` 介面成員正確且唯讀
  - Given: `KaijuBreaker.Core.asmdef` 已編譯
  - When: 以 `typeof(IDifficultyProvider).GetMembers()` 反射查詢
  - Then: 介面有且僅有 `CurrentTier`（get only property）、`GetEnemyCountMult(DifficultyTier)`、`GetBulletDensityMult(DifficultyTier)` 三個成員；無 set 方法；無 `SetTier` 或任何寫入方法
  - Edge cases: 介面回傳型別正確（`DifficultyTier`；`float`；`float`）

- **AC-3**: `OnValidate` 拒絕 D1 `enemy_mult` 不為 1.0
  - Given: `ScriptableObject.CreateInstance<DifficultyConfig>()` 建立測試用實例；`LogAssert.Expect(LogType.Error, ...)` 設定期望
  - When: 將 `enemy_mult[D1]` 設為 `1.1f` 並呼叫 `OnValidate()`
  - Then: `LogError` 被觸發（LogAssert 通過）
  - Edge cases: `bullet_mult[D1] = 0.9f` 亦觸發 LogError

- **AC-4**: `OnValidate` 接受 GDD §D.3 預設值
  - Given: 建立 `DifficultyConfig`，填入 enemy_mult={1.00, 1.25, 1.50, 1.75}、bullet_mult={1.00, 1.25, 1.50, 2.00}、`enemy_cap_per_scene=20`
  - When: 呼叫 `OnValidate()`
  - Then: 無 `LogError` 輸出（`LogAssert.NoUnexpectedReceived()`）
  - Edge cases: D4 `bullet_mult = 2.50`（安全範圍上界）亦接受；`enemy_cap_per_scene = 15`（安全範圍下界）亦接受

- **AC-5**: `OnValidate` 拒絕 D4 `bullet_mult` 越界
  - Given: 建立 `DifficultyConfig`，其他值合法
  - When: 設 `bullet_mult[D4] = 2.51f`（超出安全範圍 1.75–2.50）並呼叫 `OnValidate()`
  - Then: `LogError` 或 `LogWarning` 觸發（依設計師越界嚴重度區分；D4 越界為 Warning）
  - Edge cases: `= 2.50` 接受；`= 1.74` 亦拒絕（低於下界）

- **AC-6**: `DifficultyConfig` 不含執行期行為
  - Given: 反射查詢 `DifficultyConfig` 型別
  - When: 查詢 `Update`、`Start`、`Awake`、`FixedUpdate` 等方法存在性
  - Then: 均不存在；`DifficultyConfig` 繼承 `ScriptableObject`（非 `MonoBehaviour`）

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- Config/Data: `Assets/_Project/Tests/Difficulty/EditMode/DifficultyConfig_Validation_Test.cs` 所有 EditMode 測試在 Unity Test Runner 通過；`Assets/_Project/Content/Balance/DifficultyConfig.asset` 資產存在且 Inspector 值符合 GDD §D.3

> **路徑裁決**（control manifest §1.6）：Unity 專案測試路徑以 ADR-0005 為準（`Assets/_Project/Tests/Difficulty/EditMode/`）；`coding-standards.md` 的 `tests/unit/difficulty/` 為引擎無關通則，Unity 專案不適用。

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (foundational — establishes Core types and Content SO; all other Difficulty stories depend on this)
- Unlocks: Story 002 (DifficultySystem 需要 IDifficultyProvider + DifficultyConfig), Story 003 (TTB/Weapon 不變性測試需要 DifficultyTier 注入), Story 004 (素材/可及性測試需要 DifficultyTier 注入)
