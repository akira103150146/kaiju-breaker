# ADR-0001：彈幕引擎後端 (Bullet Engine Backend)

- **Title**: 敵彈模擬採 DOTS/ECS + Burst + Jobs；遊戲/UI 採 MonoBehaviour + 物件池（混合），撰寫層解耦以保後端可換
- **Status**: **Proposed（待效能原型驗證後 LOCK）**
- **Date**: 2026-07-01
- **Deciders**: Technical Director（旗艦決策）
- **相關**: `bullet-system.md`（策略文件）、`game-concept.md`（第一技術風險）、`difficulty-system.md`（密度縮放）、ADR-0003（撰寫層 SO）、architecture.md §5.3（DOTS↔Mono 邊界）

---

## Context（技術脈絡與問題）

`game-concept.md` 明列**專案第一技術風險：手機彈幕效能——同畫面上千子彈 × 觸控 × 手機**。手機（ARM 中階機）是首要平台。`bullet-system.md` 設定三條不可讓步目標：**零 per-bullet GC**、**資料驅動模式撰寫**、**彈幕永遠讀得懂**，並要求手機 sustain **800–1,200 顆敵彈 @60fps**（承諾點 1,000）。

敵彈是一種非常特定的工作負載：**大量、同質、每幀對每顆做相同數學**（位置積分、lifetime、邊界剔除、廣相碰撞）。這正是資料導向設計 (DOP) 的最佳情境，也正是傳統 GameObject/MonoBehaviour 每彈一物件的最差情境（Transform 開銷、managed 記憶體局部性差、逐彈 `Update()` 呼叫、GC 尖峰）。

同時，遊戲其餘部分（玩家、可破壞部位、武器、UI、打擊感）是**少量、異質、狀態豐富、需與設計師工作流與除錯工具緊密結合**的物件——ECS 對它們是過度工程且傷開發效率。

問題：**彈幕模擬用什麼後端？並且如何在採用高風險新技術（DOTS）時保留退路？**

---

## Decision（決策）

採用**混合架構 (Hybrid)**：

1. **敵彈模擬 → DOTS/ECS + Burst + Jobs**（Unity 6.3 Entities 1.3+），隔離於單一組件 `KaijuBreaker.BulletSim`。敵彈為純資料 struct（position/velocity/lifetime/color_id/type…），模擬與碰撞在 Burst Job 中平行執行；空間網格廣相每幀由 Job 重建；命中結果經 `NativeQueue<HitEvent>` 交給主執行緒 Bridge。

2. **遊戲/部位/武器/UI/打擊感 → MonoBehaviour + 物件池**。玩家**雷射**為連續判定（raycast/overlap 對 ≤8 部位），**不進 ECS**。玩家**飛彈**走獨立池（ECS 池 vs Mono 池為開放項，見下）。

3. **模式撰寫層 (Pattern Authoring Layer) 與模擬後端刻意解耦**：設計師以 `EmitterPatternSO`（ScriptableObject）撰寫三頭目全部模式（`bullet-system.md` §4；ADR-0003），載入時烘焙為 Burst 友善的不可變 Blob。**撰寫資產完全不知道後端是 ECS 還是 MonoBehaviour。** 此可逆性是採用 DOTS 的前提條件。

4. **DOTS↔Mono 邊界為單一明確 Bridge**（architecture.md §5.3）：跨界只傳值型 struct（玩家點座標、部位 AABB、密度乘數、timeScale 進；HitEvent 出），零 managed 引用進 ECS，零 per-bullet GC。

5. **難度密度 hook**：彈幕系統**讀取** `DifficultyConfig` 的 `bullet_density_mult[tier]`，只縮放彈數/臂數/射頻；速度/形狀恆定；密度後過同屏硬上限（可讀性優先於密度）。

### 必要驗證閘門（REQUIRED — LOCK 前置條件）

**本 ADR 狀態為 Proposed，直到效能原型在手機基準機上達成以下，才轉 Accepted 並 LOCK：**

- [ ] 手機基準機 sustain **1,000 敵彈（含碰撞+繪製）穩定 60fps ≥ 60 秒，無掉幀尖峰**（`bullet-system.md` 11.1）。
- [ ] 一場完整最高密度戰鬥 **GC Alloc = 0 B/frame**（穩態；Unity Profiler，`bullet-system.md` 11.2）。
- [ ] 三頭目全部既有模式可由 `EmitterPatternSO` 撰寫，無需新增 shape 或程式（表達力，11.3）。
- [ ] 廣相在 1,000+ 敵彈下每幀碰撞成本落在 ≤3.5ms 份額內。

**若未達 1,000**：依處置順序 (1) 降 D4 密度乘數 → (2) 收緊同屏上限 → (3) 最後才視覺；**絕不犧牲可讀性換數量**。若 DOTS 整合成本被證明過高，執行「純 Mono 退路」（見 Alternatives），撰寫資產與事件契約不動。

---

## Alternatives Considered（替代方案）

### A. 純 MonoBehaviour + GameObject 池（每彈一物件）
- **優點**：團隊熟悉、除錯直觀、無 DOTS 學習曲線、與 Unity 工作流無縫。
- **缺點**：手機數百顆即受 Transform/GameObject 開銷與 managed 局部性拖累；易因 managed 元件/事件配置產生 GC 尖峰掉幀；主執行緒為主，難平行。**達不到 1,000 敵彈承諾的信心低。**
- **否決理由**：直接抵觸第一技術風險的核心承諾數字。**但保留為退路**——因撰寫層解耦，可退回「Mono 池 + `NativeArray` + `IJobParallelFor`」混合而不重寫任何 Emitter 資產。

### B. 純 DOTS（全遊戲皆 ECS，含部位/武器/UI）
- **優點**：單一範式、效能天花板最高。
- **缺點**：部位狀態機、武器 Tier、UI、打擊感都是少量異質狀態豐富物件，用 ECS 是過度工程；大幅拖慢開發、除錯工具生疏、與設計師工作流脫節；Unity 6.3 混用 ECS 與 GameObject 的橋接（Baking/Companion）本身有成本。
- **否決理由**：把 DOP 用在錯的負載上，傷可維護性與時程，收益不成比例。

### C. 混合：DOTS 敵彈 + Mono 其餘（採用）
- **優點**：把 DOP 用在對的負載（同質大量敵彈），把 OOP 用在對的負載（異質狀態物件）；DOTS 風險隔離於單一組件；撰寫層解耦保退路；直接回應 `game-concept.md` 對 DOTS+Burst 的預判。
- **缺點**：需維護 DOTS↔Mono 邊界（Bridge）；兩種範式並存的心智負擔；DOTS 學習曲線（但侷限於一個組件）。
- **採用理由**：唯一同時滿足「手機達標」與「可逆/可維護」的方案。

---

## Consequences（後果）

### 正面
- 敵彈負載走最可靠的手機達標路徑（Burst SIMD、零 GC、跨核平行）。
- DOTS 風險與學習成本**隔離於 `BulletSim` 單一組件**；其餘系統以熟悉的 MonoBehaviour 開發。
- 撰寫層 SO 解耦 → 後端可換、設計師無需碰程式即撰寫彈幕、Emitter 資產穩定。
- Bridge 為單一可測/可替換整合點；退回純 Mono 只換後端與 Bridge。
- 可讀性護欄（同屏上限、單 atlas、暖色）以硬機制保證，不倚賴自律。

### 負面 / 成本
- 團隊需學 Entities 1.3 / Burst / Jobs（侷限於一組件，但仍是真成本）。
- DOTS↔Mono 邊界須嚴守「僅傳值型 struct」紀律，違反即引入 GC/耦合。
- Unity 6.3 的 Entities API 與 LLM 知識落差大 **[需查證 6.3 API]**：ECS 時間注入（頓幀凍結敵彈）、Blob 烘焙、World 生命週期與場景整合，實作前務必查 `docs/engine-reference/unity/VERSION.md` 與官方文件，**不得臆造 API**。
- 本決策在效能原型前**不可視為承諾**；1,000@60fps 是目標非事實。

### 效能意涵 (Performance Implications)
- 目標：手機 ≤3.5ms 彈幕+碰撞份額、同屏 800–1,200（承諾 1,000）；PC ≤2.0ms、1,500–2,000。均標記 [需引擎階段驗證]。
- 池預配置容量：敵彈 手機1536/PC2560、玩家飛彈 手機128/PC256（×1.3 餘裕）。
- 目標零 per-bullet GC（穩態 0 B/frame）。
- 單一敵彈 sprite atlas + 單材質 → draw call 個位數。

---

## Reversibility（可逆性）

**中等偏高**：撰寫層（`EmitterPatternSO`）與事件契約（`on_missile_hit` 等）與模擬後端解耦。若 DOTS 受阻，退回 MonoBehaviour 模擬只影響 `BulletSim` 內部與 Bridge，**不動任何 Emitter 資產、不動任何其他系統**。此可逆性是採用高風險 DOTS 的先決條件，也是本決策 Status 為 Proposed 的原因。

---

*ADR 版本：1.0.0 — Proposed，pending 效能原型*
