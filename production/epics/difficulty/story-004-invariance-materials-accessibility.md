# Story 004: 素材產量 + 內容可及性不變性自動化測試

> **Epic**: 難度系統 (Difficulty System)
> **Status**: Ready
> **Layer**: Feature (Tests)
> **Type**: Logic
> **Estimate**: 2–3 h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/difficulty-system.md`
**Requirements**: `TR-difficulty-004` (H.4 素材產量在 D1–D4 等量，阻斷), `TR-difficulty-005` (H.5 內容可及性，功能性阻斷)

> **Note**: `docs/architecture/tr-registry.yaml` 尚未正式化。TR-IDs 由 `design/gdd/difficulty-system.md §H` 推導，`production/epics/difficulty/EPIC.md` 確認。

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 — ScriptableObject 為唯一調校資料來源
**ADR Decision Summary**: `EconomyConfig` SO 為素材產量的唯一來源；`difficulty_yield_bonus` 恆為 0.0（不可在 SO 中設為非零值）；`Economy` 系統計算產量只讀 `break_quality` + `part_type`，完全不讀 `IDifficultyProvider`——不變性由架構分離保障，測試 PROVE 此保障成立。H.5 可及性驗證 Stage 內容選擇邏輯不含難度鎖定判斷。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 純 EditMode C# 測試，不涉及 Unity rendering 或 ECS；無已知 6.3 breaking change 影響。

**Control Manifest Rules (Feature/Tests layer — `docs/architecture/control-manifest.md` v2026-07-02)**:
- MUST (§1.6): Logic story → 自動化單元測試通過【BLOCKING】；測試放 `Assets/_Project/Tests/Difficulty/EditMode/`
- MUST (§1.6): 測試決定性、隔離、無外部 I/O（DI 注入假 SO + 假依賴）
- MUST (§3 `Economy`): 訂閱 `on_part_break`，依 `break_quality` + `kaiju_id` 獨立計算產量；MUST NOT 從 payload 讀「已算好的產量」；產量倍率全在 `EconomyConfig`（零硬編碼）；由 27 情境素材產量測試覆蓋（weapon-system / economy epic 的基礎責任）
- MUST (§3 `Difficulty`): MUST NOT 讓其他系統複製/快取難度值；`Economy` MUST NOT 接受 `IDifficultyProvider` 作為輸入
- MUST NOT (§5): `Economy` 路徑中不得有任何 `IDifficultyProvider` 引用；`difficulty_yield_bonus` 不得在執行期被設為非零值

---

## Acceptance Criteria

*From GDD `design/gdd/difficulty-system.md` §H.4、§H.5、§C.5（等值原則），§E.5（D4 素材隱性效應設計接受）:*

### H.4 素材產量不變性（BLOCKING）

- [ ] `tests/material_yield_invariance_test` 存在且在 Unity Test Runner EditMode 通過（BLOCKING gate）
- [ ] `EconomyConfig.difficulty_yield_bonus` 在 D1–D4 任何 tier 環境下讀取值恆為 `0.0f`（`Assert.AreEqual(0.0f, config.DifficultyYieldBonus)`）
- [ ] 36 個測試案例通過：3 品質等級（Standard / Precision / Perfect）× 3 部位類型（NORMAL / ARMORED / BOSS_CORE）× 4 難度（D1–D4）＝ 36；斷言 `Economy.CalculateYield(quality, partType, contextD1) == Economy.CalculateYield(quality, partType, contextD4)` 對所有組合成立
- [ ] `Economy` assembly 中無任何 `IDifficultyProvider` 型別引用（反射掃描；若存在 → Assert.Fail，架構違規）

### H.5 內容可及性（BLOCKING）

- [ ] `tests/content_accessibility_test` 存在且通過（BLOCKING gate）
- [ ] 靜態 / 整合測試確認：`Stage` 的關卡選擇（Stage 1–3）在 D1–D4 下回傳相同的可選關卡集合（無任何基於 tier 的過濾或 `if (tier >= DX)` 鎖定邏輯）
- [ ] 模擬 D1–D4 下各觸發莢艙保底邏輯路徑，確認保底機制在所有 tier 下均生效（不因難度旗標而跳過）
- [ ] `Economy` 的 `difficulty_yield_bonus = 0.0` 驗證：確認在 `EconomyConfig.asset`（`Assets/_Project/Content/Balance/`）中此欄位值為 `0.0f`，且 `OnValidate` 拒絕非零值（`LogError`）

---

## Implementation Notes

*Derived from ADR-0003 Decision §3–§4, GDD §C.5、§E.5, and control manifest §3 `Economy`:*

### 素材不變性的架構根因

`Economy.CalculateYield(break_quality, part_type)` 簽名**不接受任何難度參數**。產量公式只讀 `EconomyConfig`（`shard_yield[quality][part_type]`、`core_yield[part_type]`）與 `on_part_break` payload 的 `break_quality`。`difficulty_yield_bonus` 是一個 `EconomyConfig` 欄位，設計鐵則規定恆為 `0.0f`；`OnValidate` 拒絕非零值確保沒有人意外「加 D4 bonus」。

D4 下素材隱性降低（GDD §E.5）是玩家 uptime 下降導致更少 Precision/Perfect break，不是 API-level 的難度縮放——Economy 完全不知道難度存在，這是正確的。

### 36 個測試案例結構

```
for quality in [Standard, Precision, Perfect]:
  for partType in [NORMAL, ARMORED, BOSS_CORE]:
    yield_D1 = Economy.CalculateYield(quality, partType)  // difficulty-agnostic
    yield_D4 = Economy.CalculateYield(quality, partType)  // same call, same result
    Assert.AreEqual(yield_D1, yield_D4)
```

注意：由於 `CalculateYield` 本來就不接受 tier 參數（架構保障），測試實際上驗證的是「任何試圖將 tier 傳入 Economy 的重構都會讓測試編譯失敗」。36 案例以不同 quality/partType 組合確認產量矩陣值本身正確，與難度無關。

若 `Economy` 被未來開發者錯誤地加上 `tier` 參數，`Assert.AreEqual` 仍是第一層防線，但編譯型保障更強——保持方法簽名無 tier 參數。

### H.5 可及性測試方法

建立 `StageContentAccessibilityChecker`（測試 helper）：
- 注入 `StageDef[]` fixture（Stage 1–3 定義）
- 注入假 `IDifficultyProvider` 分別設定 D1/D2/D3/D4
- 呼叫 Stage 的「取可選關卡」邏輯（需要 Stage 系統暴露此邏輯為可測試的純函式）
- 斷言 D1/D2/D3/D4 回傳集合相同（`CollectionAssert.AreEquivalent`）

莢艙保底：注入假 `StageProgressTracker`（模擬各種連敗計數），呼叫保底觸發判斷，確認觸發條件在各 tier 下相同。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- [Story 001]: `DifficultyConfig` SO、`DifficultyTier` enum（必須先 DONE）
- [Story 002]: `DifficultySystem` 運行期實作（必須先 DONE）
- [Story 003]: 部位 TTB + 武器輸出不變性測試（H.2、H.3）
- Economy 系統的 27 情境素材產量正確性測試（weapon-system / economy epic 的責任；本 story 的 36 案例是其子集中「跨難度恆定」的斷言層，不取代其完整測試）
- 隱性素材品質降低（GDD §E.5）的量化 playtest：設計接受的隱性效應，非 bug，不需自動化測試
- D1 可及性 playtest（TR-difficulty-006）：Stage 1 MVP 里程碑 playtest，5 人測試，非本 story
- D4 彈幕可讀性 playtest（TR-difficulty-007）：Vertical Slice 里程碑 playtest
- 難度選擇 UI（TR-difficulty-008 UI 部分）：`hud-ui` epic

---

## QA Test Cases

*Logic story — EditMode automated test specs. 測試檔：`Assets/_Project/Tests/Difficulty/EditMode/MaterialYieldInvariance_Test.cs` + `ContentAccessibility_Test.cs`*

**Test: H.4 素材產量不變性（36 案例）**

4×9 yield matrix（展示 D1 vs D4 抽樣；完整測試跑全 36）：

| Quality     | Part Type  | D1 yield | D2 yield | D3 yield | D4 yield |
|-------------|------------|----------|----------|----------|----------|
| Standard    | NORMAL     | X        | X        | X        | X        |
| Standard    | ARMORED    | Y        | Y        | Y        | Y        |
| Standard    | BOSS_CORE  | Z        | Z        | Z        | Z        |
| Precision   | NORMAL     | X'       | X'       | X'       | X'       |
| ...         | ...        | ...      | ...      | ...      | ...      |

（每列四格相同 = 不變性成立；具體數值由 `EconomyConfig.asset` 決定，測試以 fixture 注入，非行內魔數）

- **AC H.4-1**: `difficulty_yield_bonus` 恆為 0.0
  - Given: `EconomyConfig` 以 `ScriptableObject.CreateInstance` 建立，`OnValidate` 呼叫
  - When: 讀 `config.DifficultyYieldBonus`
  - Then: `Assert.AreEqual(0.0f, config.DifficultyYieldBonus)`
  - Edge cases: 嘗試在 Inspector 設 `0.1f` → `OnValidate` 觸發 `LogError`（test: `LogAssert.Expect`）

- **AC H.4-2**: Standard/NORMAL 產量 D1 == D4
  - Given: `EconomyConfig` fixture（真實 shard_yield/core_yield 值）；假 `IDifficultyProvider` D1 / D4
  - When: `Economy.CalculateYield(BreakQuality.Standard, PartType.NORMAL)` 各執行一次
  - Then: `Assert.AreEqual(yield_D1, yield_D4)`
  - Edge cases: 由於 `CalculateYield` 不接受 tier 參數，兩次呼叫結果天然相同；若方法簽名含 tier 參數則測試需更新

- **AC H.4-3**: 全部 36 組合通過
  - Given: `EconomyConfig` fixture；遍歷 quality × partType × tier 全部組合
  - When: 對每組 `(quality, partType)` 在 D1 vs D4 各算一次
  - Then: 36 組 `Assert.AreEqual` 全部通過
  - Edge cases: `Economy` assembly 無 `IDifficultyProvider` 引用（反射確認，若存在 Assert.Fail）

**Test: H.5 內容可及性**

- **AC H.5-1**: D1–D4 可選關卡集合相同
  - Given: `StageDef` fixture × 3 stages（全部 unlock_flag = true）；假 `IDifficultyProvider` D1/D2/D3/D4
  - When: 各 tier 下呼叫 `Stage.GetSelectableStages(provider)`
  - Then: `CollectionAssert.AreEquivalent(stages_D1, stages_D4)`；D2/D3 同理
  - Edge cases: 部分 Stage unlock_flag = false → 所有 tier 均不可選（難度無關）

- **AC H.5-2**: 莢艙保底在 D1–D4 均生效
  - Given: 假 `StageProgressTracker`（設連敗 N 次達保底觸發條件）；假 `IDifficultyProvider` 各 tier
  - When: 各 tier 下執行莢艙掉落判斷邏輯
  - Then: 所有 tier 下保底均觸發（`Assert.IsTrue(guaranteeTriggered)`）
  - Edge cases: N-1 次（未達保底）在所有 tier 均不觸發

- **AC H.5-3**: Stage 選擇邏輯無 difficulty tier 條件分支
  - Given: 反射或 Roslyn 靜態分析
  - When: 掃描 `RunController`/`Stage` 中關卡選擇路徑的所有 `if`/`switch` 條件
  - Then: 無任何條件包含 `DifficultyTier` 或 `IDifficultyProvider` 型別比較（難度不影響 content availability）
  - Edge cases: `BulletSim`/`Stage` 讀取密度乘數的分支（合法）不被誤判為 content gating

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- Logic (BLOCKING): `Assets/_Project/Tests/Difficulty/EditMode/MaterialYieldInvariance_Test.cs` — 36 個 Assert.AreEqual 全部通過
- Logic (BLOCKING): `Assets/_Project/Tests/Difficulty/EditMode/ContentAccessibility_Test.cs` — H.5 可及性斷言全部通過

> **路徑裁決**（control manifest §1.6）：測試路徑以 ADR-0005 為準（`Assets/_Project/Tests/Difficulty/EditMode/`）。

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE (`DifficultyTier`、`DifficultyConfig` 必須存在)
- Depends on: Story 002 DONE (`DifficultySystem` 可實例化，供 H.5 可及性測試注入不同 tier)
- Depends on: `economy` epic 的 `EconomyConfig` SO 定義與 `Economy.CalculateYield` 實作（或 stub）
- Depends on: `stage` epic 的 `Stage.GetSelectableStages` 可測試純函式（或 stub）
- Unlocks: Epic Definition of Done（H.4 + H.5 BLOCKING 驗收條件滿足，配合 Story 003 的 H.2 + H.3，完成「難度是門」支柱的全部可自動化驗證）
