# ADR-0003：資料驅動調校 — ScriptableObject 為唯一調校資料來源 (Data-Driven Config via ScriptableObjects)

- **Title**: 所有平衡/調校旋鈕以 ScriptableObject（唯讀）表達，放 `Assets/_Project/Content`；取代 GDD 中的 YAML/JSON 佔位路徑；測試以假 SO fixture 注入
- **Status**: **Accepted**
- **Date**: 2026-07-01
- **Deciders**: Technical Director
- **相關**: `coding-standards.md`（零硬編碼、DI）、全 GDD 的「調校旋鈕」章節、ADR-0001（EmitterPattern 撰寫層）、ADR-0002（IWeaponTierQuery）、ADR-0004（存檔——玩家可變資料，與本 ADR 互補）

---

## Context（技術脈絡與問題）

`coding-standards.md` 硬約束：**「Gameplay values 必須資料驅動（外部 config），永不硬編碼」**。每份 GDD 都有龐大的「調校旋鈕 (Tuning Knobs)」章節（武器 60+ 旋鈕、部位、難度乘數、素材產量/成本、打擊感震動/慢動作/頓幀、彈幕模式、輸入手感…），且都明文寫「所有數值存放於外部資料檔，禁止硬編碼」。

GDD 為**引擎無關**撰寫，用了佔位路徑：`assets/data/weapons/*.yaml`、`assets/data/balance/game-feel.yaml`、`assets/data/stages/difficulty_config.yaml`、`input_settings.json` 等。這些是 GDD 佔位，**非 Unity 實作路徑**。

問題：**在 Unity 6.3 中，這些調校資料用什麼機制承載，才能同時滿足零硬編碼、設計師無需碰程式即可調、可被單元測試注入、且與玩家存檔資料清楚分離？**

---

## Decision（決策）

### 1. ScriptableObject 為所有靜態調校資料的唯一載體

所有 GDD 調校旋鈕以 **ScriptableObject 資產**表達，放 `Assets/_Project/Content/`，執行期**唯讀**載入。此決策**取代 GDD 中所有 YAML/JSON 調校佔位路徑**——GDD 的 `assets/data/**/*.yaml` 映射為對應 SO 資產（見對照表）。

| GDD 佔位路徑 | Unity SO 資產 | 內容 |
|-------------|--------------|------|
| `assets/data/weapons/*.yaml` | `WeaponBalanceConfig` + `WeaponDef`×8 | D₀、H_rate、B_rate、彈匣、Tier 效果 |
| `assets/data/balance/part-system.yaml` | `PartSystemConfig` | H_max/B_max/theta_S/衰減/破壞閾值 |
| `assets/data/kaiju/[id].yaml` | `KaijuDef`（含 `PartDef[]`, adjacency, drop_table_id）| 巨獸部位組成/相鄰圖/覆寫值 |
| `EmitterPatternSO` 資產 | `EmitterPatternSO`（→烘焙 Blob）| 三頭目彈幕模式（撰寫層，ADR-0001）|
| `assets/data/stages/difficulty_config.yaml` | `DifficultyConfig` | 難度乘數（**唯一來源**，見 §3）|
| `assets/data/economy/*` | `EconomyConfig` | 產量倍率、升級成本表 |
| `assets/data/stages/*` | `StageDef` + `SegmentDef` + `PodDropConfig` + `EnemyConfig` | 波段池、莢艙、雜兵旋鈕 |
| `assets/data/balance/game-feel.yaml` | `GameFeelConfig` | 震動/慢動作/頓幀/SOFTENED 旋鈕 |
| `assets/data/input/input_settings.json`（預設值部分）| `InputSettings` | 觸控偏移、lerp、死區、映射預設 |

### 2. 唯讀原則與存檔分離

- **SO = 設計師撰寫、玩家不可改、隨遊戲版本走、執行期唯讀**。
- **玩家可變資料（Tier 等級、素材、設定覆寫）走 JSON 存檔**（ADR-0004），**不寫回 SO**。
- 邊界：`WeaponDef` 定義 L2 各 Tier 的旋鈕值（靜態）；玩家「當前 L2 是 Tier 幾」存於 save（可變）。執行期 `Weapons` 經 `IWeaponTierQuery`（ADR-0002）讀當前 Tier，再從 `WeaponDef` 取該 Tier 的靜態旋鈕。**靜態與可變絕不混寫**——這是可維護性關鍵切分。

### 3. 單一權威來源 (Single Source of Truth)

某些旋鈕跨多份 GDD 引用（如 `stagger_duration` = `l3_stagger_window`；難度乘數同時在 stage/difficulty GDD 陳述）。SO 層強制**單一擁有者**：
- 難度乘數唯一存於 `DifficultyConfig`；`Stage` 讀取不另存（`difficulty-system.md` 為權威）。
- 共享旋鈕（如 `stagger_duration`）由一個 config SO 擁有，其他系統經介面讀取，不複製值。

### 4. 資料驗證與測試

- SO 以 `OnValidate` 對「安全範圍 (safe range)」做編輯期斷言（GDD 每旋鈕標了安全範圍），越界即 Inspector 警告。
- **測試以假 SO fixture 注入**（工廠函式或測試專用 `.asset`），不用行內魔數（`coding-standards.md` 測試規則）。系統以介面/建構子接收 config，測試時注入固定 fixture → 決定性、隔離。
- 平衡公式測試（如 8×8 TTB 矩陣、素材產量 27 情境）把 SO 值作為輸入參數，改 SO 即可重跑，無需改碼。

---

## Alternatives Considered（替代方案）

### A. 保留 YAML/JSON 外部檔（如 GDD 字面）
- **優點**：純文字、版控 diff 友善、可外部工具改。
- **缺點**：需自寫解析/驗證層；Unity 無原生 Inspector 支援 → 設計師需編輯純文字（易錯、無型別安全、無資產引用如 sprite/prefab）；載入需 I/O 與反序列化。
- **否決**：ScriptableObject 是 Unity 原生資料驅動慣例，Inspector 可視、型別安全、可引用其他資產、無需自寫解析。GDD 的 YAML 只是引擎無關佔位。

### B. 硬編碼常數 + `const`/`static readonly`
- **否決**：直接違反 `coding-standards.md` 零硬編碼。不可調、不可測試注入。

### C. 混合：SO + 少量 JSON（外部可熱調）
- **考量**：JSON 便於 build 後外部調參（QA/playtest）。
- **裁決**：MVP 以 SO 為主（型別安全、設計師工作流）；**若** playtest 需 build 後熱調，可加一層「JSON 覆寫 SO」的除錯載入器作為未來增強，非 MVP 阻斷。存檔用 JSON 是另一回事（玩家可變資料，ADR-0004）。

---

## Consequences（後果）

### 正面
- 落實 `coding-standards.md` 零硬編碼；設計師在 Inspector 調參，無需碰程式或工程介入。
- 型別安全、可引用資產（sprite/prefab/其他 SO）、`OnValidate` 範圍驗證。
- 靜態調校 vs 玩家存檔清楚分離 → 版本遷移只需處理 save，SO 隨版本走。
- 測試注入假 SO → 決定性、隔離、無 I/O（`coding-standards.md` 測試獨立性）。
- 平衡調整 = 改資產，不改碼、不重編譯遊戲邏輯。

### 負面 / 成本
- SO 為二進位 `.asset`（YAML 文字序列化但含 GUID）→ merge 衝突比純資料 YAML 稍難；需 Git 紀律與小顆粒資產。
- 大量 SO 資產需良好命名/資料夾組織（§Content 結構）。
- 跨 GDD 共享旋鈕需明確單一擁有者，否則值漂移——以「單一權威來源」原則約束。
- Build 後不易外部熱調（除非加 JSON 覆寫層）——playtest 期可能想要，列為未來增強。

### 效能意涵
- SO 執行期唯讀、載入一次快取，無逐幀 I/O。
- `EmitterPatternSO` 載入時烘焙為 Burst Blob（不可變、值型），執行期唯讀 → 支持彈幕零 GC 與後端可換（ADR-0001）。

---

## Reversibility（可逆性）
**中高**：config 經介面注入系統。若某類資料改用 JSON 熱調，只需換該 config 的載入實作（SO→JSON deserialize 到同一 struct），消費端不變。

---

*ADR 版本：1.0.0*
