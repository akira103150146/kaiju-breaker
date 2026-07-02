# Story 003: 部位 TTB + 武器輸出不變性自動化測試（BLOCKING）

> **Epic**: 難度系統 (Difficulty System)
> **Status**: Ready
> **Layer**: Feature (Tests)
> **Type**: Logic
> **Estimate**: 3–4 h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/difficulty-system.md`
**Requirements**: `TR-difficulty-002` (H.2 部位 TTB 在 D1–D4 恆定，**阻斷**), `TR-difficulty-003` (H.3 武器輸出在 D1–D4 恆定，**阻斷**)

> **Note**: `docs/architecture/tr-registry.yaml` 尚未正式化。TR-IDs 由 `design/gdd/difficulty-system.md §H` 推導，`production/epics/difficulty/EPIC.md` 確認。

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 — ScriptableObject 為唯一調校資料來源
**ADR Decision Summary**: 測試以假 SO fixture 注入（工廠函式或測試專用 `.asset`），不用行內魔數；`PartSystemConfig` 與武器 `WeaponBalanceConfig`/`WeaponDef` 均以固定 fixture 注入，不讀 `IDifficultyProvider`——不變性由 「部位/武器系統從不讀取難度值」這一架構屬性決定，測試 PROVE 此屬性成立。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: EditMode 測試不需要 Play 模式，ScriptableObject.CreateInstance 在 EditMode 下可用。矩陣結果輸出到 `production/qa/evidence/*.txt` 使用 `System.IO.File.WriteAllText`（Unity Editor only，無 6.3 breaking change 風險）。

**Control Manifest Rules (Feature/Tests layer — `docs/architecture/control-manifest.md` v2026-07-02)**:
- MUST (§1.6): Logic story → 自動化單元測試通過【BLOCKING】；測試放 `Assets/_Project/Tests/Difficulty/EditMode/`（ADR-0005 裁決）
- MUST (§1.6): 測試決定性（無亂數種子、無時間相依）、隔離（自建自拆狀態）、無 I/O 外部依賴（DI 注入假 SO fixture）
- MUST (§3 `KaijuParts`): 部位系統 MUST NOT 從 `IDifficultyProvider` 讀任何值；H_max/B_max/theta_S 等全域旋鈕在 D1–D4 完全恆定
- MUST (§3 `Weapons`): 武器數值 MUST NOT 受難度縮放；所有旋鈕在 `WeaponBalanceConfig`/`WeaponDef`（ADR-0003）
- MUST NOT (§5): 測試 MUST NOT 在 `KaijuParts` 或 `Weapons` 路徑中找到任何 `IDifficultyProvider` 讀取點（invariance 的架構保障）

---

## Acceptance Criteria

*From GDD `design/gdd/difficulty-system.md` §H.2、§H.3（BLOCKING），§C.3、§C.4（不變性設計鐵則）:*

### H.2 部位 TTB 不變性（BLOCKING）

- [ ] `tests/part_ttb_invariance_test` 存在且在 Unity Test Runner EditMode 通過（BLOCKING gate）
- [ ] 測試在模擬 D1/D2/D3/D4 環境下各執行「L2 × M1 × NORMAL 部位完整 TTB 模擬（H=0, B=0 → BROKEN）」，斷言四個難度下 `TTB_base` 浮點值**完全相等**（`Assert.AreEqual`，不用近似比較）
- [ ] 測試擴展至 ARMORED 與 BOSS_CORE 部位類型，輸出 **4×3 矩陣**（4 難度 × 3 部位類型）；確認每列（同部位類型、跨 D1–D4）數值完全相同
- [ ] 矩陣輸出存入 `production/qa/evidence/ttb_invariance_matrix.txt`（格式：CSV 或 tab-separated，含 header；NORMAL/ARMORED/BOSS_CORE 欄，D1–D4 列）
- [ ] 測試報告如果任一格數值與 D1 基準不符，立即 `Assert.Fail` 並標示哪個 tier/part_type 組合發生偏差（Fail message 含有效除錯資訊）

### H.3 武器輸出不變性（BLOCKING）

- [ ] `tests/weapon_output_invariance_test` 存在且通過（BLOCKING gate）
- [ ] 對 8 把武器各自模擬 30 秒持續輸出（最優命中條件：100% uptime、無換彈間斷建模），在 D1–D4 四個難度環境下斷言 `Sustained_Output(weapon)` 浮點值完全相等
- [ ] 測試輸出 **4×8 矩陣**（4 難度 × 8 武器）的 `Sustained_Output` 值；確認每列（同武器、跨 D1–D4）數值完全相同
- [ ] 測試共享 `weapon-system.md H.1` 等功率等價測試的基礎設施（`WeaponSimFixture`），在此增加「跨難度恆定」斷言層，不另起爐灶
- [ ] 若任一武器任一 tier 輸出與 D1 基準不符，`Assert.Fail` 並標示武器 ID 與 tier

### 架構屬性驗證（阻斷條件之基礎）

- [ ] 靜態反射掃描（或 Roslyn Analyzer）確認 `KaijuParts` assembly 中**不存在任何 `IDifficultyProvider` 型別引用**；確認 `Weapons` assembly 中亦無 `IDifficultyProvider` 引用（MUST NOT — 若存在即為架構違規，test FAIL）

---

## Implementation Notes

*Derived from ADR-0003 Decision §4 and control manifest §3 `KaijuParts`/`Weapons`/§1.6:*

### 不變性的架構根因

`PartStateMachine` 只讀取 `PartSystemConfig` SO（`H_max`、`B_max`、`theta_S`、`H_decay_rate`、`stagger_duration` 等），完全不接受 `IDifficultyProvider` 參數。武器邏輯只讀取 `WeaponDef`/`WeaponBalanceConfig` SO（`H_rate`、`B_rate`、`D0_reference` 等），同樣不讀難度。TTB 與輸出的跨難度恆定是**架構天然保障**，不是執行期強制。這些測試的作用是 PROVE 該保障不被未來修改破壞（regression gate）。

### TTB 模擬方法（H.2）

建立測試 helper `PartTTBSimulator`：
- 注入 `PartSystemConfig` fixture（以 `ScriptableObject.CreateInstance<PartSystemConfig>()` 建立，填入 GDD §C.3 / kaiju-part-system.md §C.8 標準值）
- 注入 `WeaponDef` fixture for L2（Focus Beam），`WeaponTierConfig` for M1（Tier 1 旋鈕）
- 執行 Heat loop：每 timestep 加 `H_rate × dt`，超過 theta_S 進入 SOFTENED；Heat 達 `H_max` → BROKEN；記錄模擬時間 = TTB_base
- **不注入** `IDifficultyProvider`——此 simulator 應不接受任何難度參數

對 D1/D2/D3/D4 各呼叫一次 `PartTTBSimulator.Simulate(partType)` → 四個結果應 `Assert.AreEqual`。

### 武器輸出模擬方法（H.3）

建立 `WeaponOutputSimulator`（可複用 weapon-system 測試基礎設施）：
- 注入 `WeaponDef` fixture × 8 武器
- 模擬 30 秒、100% uptime（`H_rate × 30s`；換彈時間由 `WeaponDef.ReloadTime` 決定）
- 計算 `Sustained_Output = total_heat_damage_delivered / 30`（或等效定義，與 weapon-system.md H.1 對齊）
- **不接受**難度參數

對 D1/D2/D3/D4 各呼叫 → 結果 `Assert.AreEqual`。

### 矩陣輸出格式

```
// ttb_invariance_matrix.txt 範例格式
Tier,NORMAL,ARMORED,BOSS_CORE
D1,18.3,34.2,61.5
D2,18.3,34.2,61.5
D3,18.3,34.2,61.5
D4,18.3,34.2,61.5
Generated: 2026-07-02 (CI run)
```

輸出由 `[OneTimeSetUp]` 或 test teardown 寫入；路徑 `production/qa/evidence/ttb_invariance_matrix.txt`（相對於 Unity project root）。Editor-only `System.IO.File.WriteAllText` 可接受（測試環境），不進遊戲執行期。

### 依賴說明

TTB Simulator 依賴 `PartStateMachine`（或等效的純 C# 邏輯類別）來自 `KaijuParts` epic。若 `KaijuParts` epic 尚未完成，本 story 可先用「stub PartStateMachine」實作熱量累積公式，待真實實作就緒後替換 stub，斷言不變。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- [Story 001]: `DifficultyConfig` SO、`DifficultyTier` enum（必須先 DONE）
- [Story 002]: `DifficultySystem` 運行期實作（必須先 DONE，用於注入不同 tier 做測試）
- [Story 004]: 素材產量不變性測試（H.4）與內容可及性測試（H.5）
- 隱性 TTB 延長設計工具（GDD §C.4、§D.4）：非遊戲內公式，不需自動化測試；設計師用 spreadsheet 驗算
- D4 彈幕可讀性 playtest（TR-difficulty-007）：Visual/Feel 驗收，屬 Vertical Slice 里程碑
- 武器等功率等價（weapon-system.md H.1 的 8×8 矩陣）：武器系統 epic 的責任；本 story 共用其基礎設施但不重新擁有定義

---

## QA Test Cases

*Logic (BLOCKING) story — 全部為自動化 EditMode 測試。*

**Test: H.2 部位 TTB 不變性矩陣**

- **AC H.2-1**: D1 vs D2 TTB 相等（NORMAL 部位）
  - Given: `PartSystemConfig` fixture（H_max=100, B_max=100, theta_S=100, H_decay_rate=3）；`WeaponDef` L2 M1 fixture（H_rate=37.5）；`DifficultySystem` 分別設為 D1 / D2
  - When: 各執行 `PartTTBSimulator.Simulate(PartType.NORMAL)` → 得 `ttb_D1`, `ttb_D2`
  - Then: `Assert.AreEqual(ttb_D1, ttb_D2)` 通過
  - Edge cases: 確認 Simulator 內部不讀取 `IDifficultyProvider`（反射確認）

- **AC H.2-2**: D1 vs D3 / D4 TTB 相等（NORMAL 部位）
  - Given: 同 H.2-1
  - When: 各執行 D3 / D4 context
  - Then: `ttb_D3 == ttb_D1`；`ttb_D4 == ttb_D1`

- **AC H.2-3**: ARMORED 部位 4×1 列（D1–D4）全等
  - Given: `PartSystemConfig` fixture（H_max=150, B_max=150 for ARMORED）
  - When: D1–D4 各執行 `Simulate(PartType.ARMORED)`
  - Then: 所有四值相等

- **AC H.2-4**: BOSS_CORE 部位 4×1 列（D1–D4）全等
  - Given: `PartSystemConfig` fixture（H_max=200, B_max=200 for BOSS_CORE）
  - When: D1–D4 各執行 `Simulate(PartType.BOSS_CORE)`
  - Then: 所有四值相等

- **AC H.2-5**: 矩陣輸出檔存在且格式正確
  - Given: H.2-1 ~ H.2-4 測試套件執行完成
  - When: 檢查 `production/qa/evidence/ttb_invariance_matrix.txt`
  - Then: 檔案存在；包含 4 列（D1–D4）× 3 欄（NORMAL/ARMORED/BOSS_CORE）數值；每列三格數值在同一列中完全相同

**Test: H.3 武器輸出不變性矩陣**

- **AC H.3-1**: 全部 8 武器 D1 vs D2 輸出相等
  - Given: `WeaponDef` fixture × 8 武器（各 Tier 1 旋鈕，GDD §G.1-G.3 數值）；`DifficultySystem` 設 D1 / D2
  - When: 各執行 `WeaponOutputSimulator.Simulate(weaponId, duration=30s)`
  - Then: 對所有 8 個 `weaponId`：`output_D2[w] == output_D1[w]`
  - Edge cases: uptime=1.0（最優命中，確保無換彈/閃避擾動）

- **AC H.3-2**: 全部 8 武器 D3 / D4 輸出與 D1 相等
  - Given: 同 H.3-1
  - When: D3 / D4 各執行
  - Then: `output_D3[w] == output_D1[w]`；`output_D4[w] == output_D1[w]` 對所有 8 武器成立

- **AC H.3-3**: `Weapons` assembly 無 `IDifficultyProvider` 引用（架構屬性）
  - Given: 反射載入 `KaijuBreaker.Weapons.asmdef` 中所有型別
  - When: 掃描所有方法/屬性的參數型別與欄位型別
  - Then: 無任何 `IDifficultyProvider` 型別引用；若存在 → `Assert.Fail("Weapons assembly incorrectly references IDifficultyProvider")`

- **AC H.3-4**: `KaijuParts` assembly 無 `IDifficultyProvider` 引用（架構屬性）
  - Given: 反射載入 `KaijuBreaker.KaijuParts.asmdef` 中所有型別
  - When: 掃描所有型別的依賴
  - Then: 無任何 `IDifficultyProvider` 型別引用；若存在 → `Assert.Fail`

---

## Test Evidence

**Story Type**: Logic (BLOCKING)
**Required evidence**:
- Logic (BLOCKING): `Assets/_Project/Tests/Difficulty/EditMode/PartTTBInvariance_Test.cs` — 通過（4×3 矩陣全部 Assert.AreEqual）
- Logic (BLOCKING): `Assets/_Project/Tests/Difficulty/EditMode/WeaponOutputInvariance_Test.cs` — 通過（4×8 矩陣全部 Assert.AreEqual）
- Evidence asset: `production/qa/evidence/ttb_invariance_matrix.txt` — 由測試自動產生，供設計師審閱

> **路徑裁決**（control manifest §1.6）：測試路徑以 ADR-0005 為準（`Assets/_Project/Tests/Difficulty/EditMode/`）；evidence 輸出路徑為 `production/qa/evidence/`（CI 後 commit 進 repo）。

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE (`DifficultyTier`、`IDifficultyProvider`、`DifficultyConfig` 必須存在以供 fixture 建立)
- Depends on: Story 002 DONE (`DifficultySystem` 可實例化並設定 tier，用於驗證不同 tier 下模擬呼叫無差異)
- Depends on: `kaiju-parts` epic 的 `PartStateMachine`（或 Story 003 自建 stub 實作公式，待真實類別就緒後替換）
- Depends on: `weapons` epic 的 `WeaponOutputSimulator` 基礎設施（或 stub；共享 weapon-system.md H.1 的測試基礎設施）
- Unlocks: Epic Definition of Done（H.2 + H.3 為 BLOCKING 驗收條件）
