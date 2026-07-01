# ADR-0005：專案結構與組件邊界 (Project Structure & Assembly Boundaries)

- **Title**: 一系統一 `.asmdef`；系統間零直接引用（僅經 `Core` 事件匯流排 + 查詢介面）；`App` 為唯一組合根；DOTS 隔離於 `BulletSim`；DI over singletons 以強制模組邊界與可測試性
- **Status**: **Accepted**
- **Date**: 2026-07-01
- **Deciders**: Technical Director
- **相關**: `coding-standards.md`（DI over singletons、public 方法可測）、`coordination-rules.md`（無單方跨域改動）、architecture.md §2/§3、ADR-0001（BulletSim 隔離）、ADR-0002（事件/查詢）

---

## Context（技術脈絡與問題）

`coding-standards.md` 硬約束：**DI over singletons、所有 public 方法可單元測試、每系統可獨立驗證**。`coordination-rules.md`：**系統不得修改其領域外的檔案，除非明確委派**。專案有 11 個設計系統 + DOTS 彈幕 + 混合架構，且由多個專員 (specialist) 平行開發。

問題：**用什麼實體程式結構，讓模組邊界被編譯器強制、系統可獨立測試、DOTS 風險隔離、且平行開發不互相踩線？**

若全部程式放單一 Assembly-CSharp：任何類別可引用任何類別，邊界只靠自律 → 必然腐化為大泥球；且任一改動觸發全專案重編譯，DOTS 依賴污染全域。

---

## Decision（決策）

### 1. 一系統一組件定義 (`.asmdef`)

依 architecture.md §2.1 切分 14 個組件（Core, Content, BulletSim, Weapons, KaijuParts, Economy, Meta, Stage, Difficulty, Input, GameFeel, UI, App, + 各 Tests）。**組件邊界即編譯期強制的依賴邊界與測試邊界。**

### 2. 依賴規則（編譯器強制）

```
Core        → 無依賴（僅 UnityEngine）；定義事件型別、查詢介面、共用型別、狀態機列舉
Content     → 僅依賴 Core
<每個系統>  → 僅依賴 Core + Content（+ 必要的 Difficulty 唯讀，如 Stage/BulletSim）
BulletSim   → Core + Content + Unity.Entities/Burst/Collections/Mathematics（DOTS 隔離於此）
App         → 依賴全部（唯一組合根 / composition root）
<System>.Tests → 對應 System + Core + Content + NUnit
```

**鐵則：任一系統組件不得引用另一系統組件。** `Weapons` 不引用 `KaijuParts`；跨系統通訊只經 `Core` 的 `IEventBus` 與查詢介面（ADR-0002）。這由 `.asmdef` 的 references 清單編譯期強制——不是靠自律。

### 3. `App` 為唯一組合根，DI over singletons

- `KaijuBreaker.App` 是**唯一**引用全部系統的組件。啟動時建構具體實作、佈線 `IEventBus`、把實作查詢介面（`IPartStateQuery`…）注入需要者。
- **禁止持有遊戲狀態的 static 單例**（`coding-standards.md`）。系統以建構子/方法注入依賴。唯一允許的 static 是無狀態工具與 `Core` 匯流排存取點（設計為可測試替換）。
- 結果：每個系統可脫離其他系統，用假事件/假查詢在 EditMode 測試（滿足「獨立可測」）。

### 4. DOTS 隔離

Unity.Entities/Burst/Collections 依賴**只在 `BulletSim`**。其餘系統不引用 DOTS 套件 → DOTS 學習曲線、編譯依賴、World 生命週期複雜度全部圈在一個組件內（ADR-0001 可逆性的結構基礎）。

### 5. 測試結構

- EditMode 測試（純邏輯：狀態機、公式、素材產量、存檔遷移）放 `Tests/<Module>/`，注入假 SO fixture（ADR-0003）。
- PlayMode 測試（需 ECS World / 場景 / 時間：彈幕模擬、頓幀輸入、Run 狀態機）放 `Tests/PlayMode/`。
- 測試檔命名 `[system]_[feature]_test`、函式 `test_[scenario]_[expected]`（`coding-standards.md`）；決定性、隔離、無 I/O、DI 注入。

### 6. 資料夾即組件（一對一）

`Assets/_Project/Scripts/<Module>/` 一個資料夾一個 `.asmdef`（architecture.md §3.1）。平行開發時，專員各自擁有自己的組件資料夾，`coordination-rules.md` 的「無單方跨域改動」由資料夾+組件邊界具體化。

---

## Alternatives Considered（替代方案）

### A. 單一 Assembly-CSharp（無 asmdef）
- **優點**：零設定、改動即編譯、無跨組件引用摩擦。
- **缺點**：無編譯期邊界 → 系統互相引用腐化為大泥球；任一改動全專案重編譯（Unity 大專案編譯慢）；DOTS 依賴污染全域；無法獨立測試單一系統。
- **否決**：直接抵觸可測試性與模組邊界要求。

### B. 粗粒度分層組件（如 `Gameplay` / `UI` / `Core` 三個）
- **優點**：比單一組件好，設定少。
- **缺點**：`Gameplay` 內部（武器/部位/素材/彈幕/難度…）仍無邊界，仍會腐化；DOTS 無法隔離於彈幕；平行開發仍踩線。
- **否決**：粒度不足以強制系統邊界或隔離 DOTS。

### C. 細粒度到每個類別一組件
- **否決**：過度切分 → 組件爆量、引用管理地獄、編譯圖複雜；一系統一組件是甜蜜點。

### D. 系統間直接引用 + 介面（無事件匯流排）
- **否決**：即使用介面，直接引用具體系統仍造成組件互依環；事件匯流排（ADR-0002）打破依賴環，系統只依賴 `Core`。

---

## Consequences（後果）

### 正面
- 模組邊界由編譯器強制，非自律 → 抗腐化。
- 系統可獨立編譯與測試（注入假依賴）→ 落實 `coding-standards.md`。
- DOTS 隔離於 `BulletSim` → 風險圈定、後端可換（ADR-0001）。
- 增量編譯：改一個系統只重編該組件 + 依賴者，非全專案。
- 平行開發：專員各擁組件，邊界清楚，減少 merge 衝突與跨域踩線（`coordination-rules.md`）。
- 組合根集中於 `App` → 佈線邏輯一處可見。

### 負面 / 成本
- `.asmdef` 需維護 references 清單；新增跨系統通訊需在 `Core` 加事件/介面（有意的摩擦——迫使思考邊界）。
- 組件多 → 專案結構前期設定成本；需團隊理解「系統不直接互引用」紀律。
- 過度嚴格時，臨時想「就引用一下」會被編譯器擋 → 短期不便，長期是防護。
- `Core` 可能膨脹（所有共享型別/介面集中）→ 需留意 `Core` 只放真正共享的抽象，不放實作。

### 效能意涵
- 組件邊界對執行期效能中性（Unity 組件是編譯單位，非執行期開銷）。
- 增量編譯縮短迭代時間（間接開發效率）。
- DI 注入在啟動組合根一次完成，非逐幀開銷。

---

## Reversibility（可逆性）
**中**：組件可合併（降低邊界）比拆分容易。若某邊界證明過細，合併兩組件只需併 `.asmdef` 與調整 references。但「系統不直接互引用」的核心原則應保留——它是可測試性的結構基礎。

---

*ADR 版本：1.0.0*
