# Story 002: DifficultySystem 實作 + 運行期乘數套用

> **Epic**: 難度系統 (Difficulty System)
> **Status**: Ready
> **Layer**: Feature (Difficulty)
> **Type**: Integration
> **Estimate**: 3–4 h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/difficulty-system.md`
**Requirements**: `TR-difficulty-001` (application — D.1/D.2 formulas applied at runtime), `TR-difficulty-005` (content accessibility — no difficulty gating), `TR-difficulty-008` (config-driven tier selection defaults)

> **Note**: `docs/architecture/tr-registry.yaml` 尚未正式化。TR-IDs 由 `design/gdd/difficulty-system.md §H` 推導，`production/epics/difficulty/EPIC.md` 確認。

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 — ScriptableObject 為唯一調校資料來源
**ADR Decision Summary**: `DifficultyConfig` SO 是乘數的唯一來源；`DifficultySystem` 實作 `IDifficultyProvider`，Stage 與 BulletSim 一律經介面查詢——不自存難度值；測試以假 SO fixture + 假 IDifficultyProvider 注入做隔離驗證。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 無已知 Unity 6.3 breaking change 影響純 C# 系統邏輯或 ScriptableObject 載入。`Stage` 整合側若用 Coroutine 做波段生成需查 6.3 async/coroutine API（控制清單 `[需查證 6.3 API]`）。

**Control Manifest Rules (Feature layer — `docs/architecture/control-manifest.md` v2026-07-02)**:
- MUST (§3 `Difficulty`): `DifficultySystem` 為 `enemy_count_mult`/`bullet_density_mult` 唯一權威來源，實作 `IDifficultyProvider`；乘數全在 `DifficultyConfig`；MUST NOT 讓其他系統複製或快取難度值
- MUST (§3 `Stage`): 讀 `IDifficultyProvider` 取密度/敵量乘數，不自存；MUST NOT 引用 `Difficulty` assembly 具體類別（經介面）
- MUST (§3 `BulletSim`): 讀 `IDifficultyProvider.GetBulletDensityMult` 只縮放彈數/臂數/射頻；速度/形狀恆定；密度後過同屏硬上限
- MUST (§1.3): 系統以建構子/方法注入依賴（IDifficultyProvider、DifficultyConfig）；MUST NOT 用持有遊戲狀態的 static singleton
- MUST (§1.4): 只有 `App`（組合根）引用全部系統並佈線 DI

---

## Acceptance Criteria

*From GDD `design/gdd/difficulty-system.md` §H.1、§H.5、§H.8（config defaults）, §C.1、§C.3、§D.1、§D.2:*

- [ ] `DifficultySystem` 類別實作 `IDifficultyProvider`，建構子接受 `DifficultyConfig`（注入，非 singleton 存取）
- [ ] `DifficultySystem.SetTier(DifficultyTier)` 設定本輪難度，呼叫後 `CurrentTier` 立即反映；`LockForRun()` 後 `SetTier` 不再改變值（run 進行中鎖定，見 GDD §C.1 / E.1）
- [ ] `DifficultySystem.GetEnemyCountMult(tier)` 回傳 `DifficultyConfig.difficulty_enemy_mult[tier]`；`GetBulletDensityMult(tier)` 回傳 `DifficultyConfig.difficulty_bullet_mult[tier]`（單一來源，不快取、不複製）
- [ ] `Stage` 在每波次生成前讀 `IDifficultyProvider.GetEnemyCountMult(CurrentTier)`，計算 `actual_count = Mathf.CeilToInt(base_count * mult)`，並截斷至 `DifficultyConfig.enemy_cap_per_scene`（GDD D.1）；≥24 個測試案例通過（H.1）
- [ ] `BulletSim`（或其 Mono Bridge）在每次敵人射擊事件前讀 `IDifficultyProvider.GetBulletDensityMult(CurrentTier)`，計算 `actual_bullets = Mathf.CeilToInt(base_bullets * mult)`（GDD D.2）；對應 ≥12 個彈數測試案例通過（H.1 彈數部分）
- [ ] `DifficultySystem` 在 run-start 若 `save.last_selected_difficulty` 存在則預填上輪難度（`remember_last_difficulty = true`）；首次遊玩（無存檔）預設 D1（`default_difficulty_on_first_launch = D1`）——config-flag 驅動，非硬編碼
- [ ] 所有已解鎖 Stage 在 D1–D4 下均可從 Stage 選擇入口進入，`Stage` 的內容載入邏輯**不含任何基於 `DifficultyTier` 的內容鎖定判斷**（TR-difficulty-005 / GDD H.5）
- [ ] 武器莢艙掉落保底機制在 D1–D4 下均正常生效（保底邏輯不受難度旗標影響，繼承 `stage-system.md` L.2）
- [ ] 所有 public 方法有 XML doc comment（control manifest §1.8）

---

## Implementation Notes

*Derived from ADR-0003 Decision §3 and control manifest §3 `Difficulty`/`Stage`/`BulletSim`:*

`DifficultySystem` 放 `KaijuBreaker.Difficulty.asmdef`（Feature layer）。建構子簽名建議：`DifficultySystem(DifficultyConfig config, ISaveService saveService)`，讓 `App` 在組合根佈線時注入。

Run-start 流程（`RunController` 驅動）：
1. `App` / `RunController` 呼叫 `DifficultySystem.SetTier(selectedTier)`
2. 呼叫 `DifficultySystem.LockForRun()` → 鎖定本輪難度
3. `Stage` 與 `BulletSim` 在各自的生成點透過 `IDifficultyProvider` 介面讀取乘數（pull model，不 push）

`Stage` 計算 `actual_count`：
```csharp
int actual_count = Mathf.Min(
    Mathf.CeilToInt(baseCount * _difficultyProvider.GetEnemyCountMult(_difficultyProvider.CurrentTier)),
    _difficultyConfig.EnemyCapPerScene
);
```
（`_difficultyConfig` 亦可經 `IDifficultyProvider` 暴露，或另行注入——選擇以 single source of truth 為原則，不在 Stage 複製 `enemy_cap_per_scene` 值。）

`BulletSim` 的密度乘數跨 DOTS↔Mono 邊界只傳值型 float（控制清單 §3 `BulletSim`），不傳 `IDifficultyProvider` 參考進 ECS world。

**內容可及性驗證**：H.5 要求「無任何基於難度的鎖定判斷」——實作上靠 code review + 一個自動化 Integration 測試：模擬 D1–D4 下各呼叫 Stage 內容選擇路徑，斷言回傳的可選 Stage 列表完全相同（無難度過濾）。

**remember_last_difficulty**：DifficultySystem 在 run 結束時呼叫 `ISaveService` 寫 `last_selected_difficulty`；run-start 時讀取並 `SetTier`。此流程由 `RunController` 協調，DifficultySystem 不直接持有 SaveService 的寫出點。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- [Story 001]: `DifficultyConfig` SO 定義、`DifficultyTier` enum、`IDifficultyProvider` 介面（Story 001 MUST be DONE before this story starts）
- [Story 003]: 部位 TTB + 武器輸出不變性自動化測試（H.2、H.3）——Story 003 tests PROVE the invariance; this story implements the system being tested
- [Story 004]: 素材產量不變性與內容可及性自動化測試（H.4）
- 難度選擇 UI 畫面、輪中難度灰化（TR-difficulty-008 UI 部分）：`hud-ui` epic 負責；本 story 僅確保 `DifficultySystem` 暴露 `CurrentTier` 與 `IsLockedForRun` 供 UI 訂閱
- D1 可及性 playtest（TR-difficulty-006）：Stage 1 MVP 里程碑 playtest
- D4 彈幕可讀性 playtest（TR-difficulty-007）：Vertical Slice 里程碑 playtest
- 子彈速度/形狀縮放：GDD §C.2 設計約束明確禁止，MUST NOT 實作

---

## QA Test Cases

*Integration story — automated test specs. 測試檔：`Assets/_Project/Tests/Difficulty/EditMode/DifficultySystem_Integration_Test.cs`*

**AC-1 / H.1 敵人數量乘數應用（≥12 測試案例）**

4×3 enemy count matrix（`base_count` ∈ {1, 5, 11}，`difficulty_enemy_mult` = {D1: 1.00, D2: 1.25, D3: 1.50, D4: 1.75}）：

| base_count | D1 (×1.00) | D2 (×1.25) | D3 (×1.50) | D4 (×1.75) |
|------------|------------|------------|------------|------------|
| 1          | 1          | 2          | 2          | 2          |
| 5          | 5          | 7          | 8          | 9          |
| 11         | 11         | 14         | 17         | 20 (cap)   |

- Given: `DifficultyConfig` fixture（GDD §D.3 預設乘數）；`DifficultySystem` 設定對應 tier；`Stage` 波段邏輯可注入假 `IDifficultyProvider`
- When: 以 `base_count` ∈ {1, 5, 11} 分別呼叫 Stage 的敵人數量計算，tier ∈ {D1, D2, D3, D4}
- Then: 回傳值 == 上表對應格；D4/base=11 == min(ceil(11×1.75), 20) == min(20, 20) == 20
- Edge cases: `base_count = 0` → `actual_count >= 1`（ceil 保護）；`actual_count` 不超過 `enemy_cap_per_scene = 20`

**AC-2 / H.1 子彈密度乘數應用（≥12 測試案例）**

4×3 bullet density matrix（`base_bullets` ∈ {1, 5, 8}，`difficulty_bullet_mult` = {D1: 1.00, D2: 1.25, D3: 1.50, D4: 2.00}）：

| base_bullets | D1 (×1.00) | D2 (×1.25) | D3 (×1.50) | D4 (×2.00) |
|--------------|------------|------------|------------|------------|
| 1            | 1          | 2          | 2          | 2          |
| 5            | 5          | 7          | 8          | 10         |
| 8            | 8          | 10         | 12         | 16         |

- Given: `DifficultyConfig` fixture（GDD §D.3 預設乘數）；假 `IDifficultyProvider` 回傳指定 tier
- When: 以 `base_bullets` ∈ {1, 5, 8} × tier ∈ {D1, D2, D3, D4} 呼叫子彈數計算（`BulletSim` 的生成路徑或其可測試的計算函式）
- Then: 回傳值 == 上表對應格
- Edge cases: D4/base=8 == 16（base_bullets_max × 2.0；GDD §D.2 理論上限）

**AC-3: `LockForRun` 鎖定後 SetTier 無效**
- Given: `DifficultySystem` 以 D2 初始化，呼叫 `LockForRun()`
- When: 呼叫 `SetTier(D4)`
- Then: `CurrentTier` 仍為 D2；`GetEnemyCountMult` 仍回傳 1.25
- Edge cases: `UnlockForRunEnd()` 後 `SetTier(D4)` 生效（run 結束解鎖）

**AC-4: remember_last_difficulty 行為**
- Given: 假 `ISaveService` 回傳 `last_selected_difficulty = D3`；`DifficultyConfig.remember_last_difficulty = true`
- When: `DifficultySystem` 在 run-start 初始化
- Then: `CurrentTier == D3`
- Edge cases: `ISaveService` 無存檔（首次）且 `default_difficulty_on_first_launch = D1` → `CurrentTier == D1`

**AC-5 / H.5: Stage 內容可及性（無難度鎖定）**
- Given: 假 Stage 選擇邏輯，D1–D4 各 `IDifficultyProvider`；全部 Stage 已解鎖
- When: 各 tier 下呼叫 Stage 內容選擇，取得可選 Stage 列表
- Then: D1/D2/D3/D4 回傳的可選 Stage 列表**完全相同**（無任何 tier 過濾）
- Edge cases: Stage 處於「未解鎖」狀態 → 所有 tier 均不可選（難度無關，是 unlock 狀態決定）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration: `Assets/_Project/Tests/Difficulty/EditMode/DifficultySystem_Integration_Test.cs` — 所有測試通過（BLOCKING）；≥24 enemy + bullet 乘數測試案例 + lock 行為 + 可及性測試

> **路徑裁決**（control manifest §1.6）：測試路徑以 ADR-0005 為準（`Assets/_Project/Tests/Difficulty/EditMode/`）。

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE (`DifficultyTier` enum、`IDifficultyProvider` 介面、`DifficultyConfig` SO 必須存在)
- Unlocks: Story 003 (TTB/Weapon 不變性測試需要可運行的 DifficultySystem 做 tier 注入), Story 004 (素材/可及性測試亦需要 DifficultySystem)
