# ADR-0002：事件架構與系統間通訊 (Event Architecture & Inter-System Communication)

- **Title**: 系統間以 `Core` 定義的型別化事件匯流排 (typed event bus) + 唯讀查詢介面通訊；DOTS↔Mono 以單一 Bridge 橋接
- **Status**: **Accepted**
- **Date**: 2026-07-01
- **Deciders**: Technical Director
- **相關**: `weapon-system.md` F.1、`kaiju-part-system.md` C.5、`material-economy.md` F.1、`meta-progression-system.md` F、`game-feel.md` C.1、architecture.md §5.2/§5.3、ADR-0005

---

## Context（技術脈絡與問題）

殲獸戰機的核心互動是一條跨 5+ 系統的事件鏈：**武器 → 部位 → 素材 → 存檔 / 打擊感 / UI**。GDD 已精確定義所有事件契約：

- `Weapons` 發：`on_laser_hit(part_id, kaiju_id, heat_delta)`、`on_missile_hit(part_id, kaiju_id, break_delta_base, weapon_id)`、`on_l3_wave_hit(part_id, kaiju_id)`
- `KaijuParts` 發：`on_part_softened/_exit`、`on_part_staggered/_stagger_end`、**`on_part_break(part_id, kaiju_id, part_type, world_position, drop_table_id, break_quality, adjacency_list, is_chain_break)`**、`on_boss_core_break(kaiju_id, world_position)`
- 下游消費者：`Economy`（依 break_quality 算素材）、`Meta`（即時入帳 autosave）、`GameFeel`（頓幀/慢動作/簽章）、`UI`（血條/計數）、`RunController`（勝利結算）

同時存在**查詢型**（非事件）需求：`Weapons` 需讀部位 `heat_state`、`world_position`（M1 追蹤、L2/M3 Tier-3 觸發）；`UI` 讀部位 `H_current`/`H_max` 畫血條。

`coding-standards.md` 硬約束：**DI over singletons、public 方法可單元測試、系統可獨立驗證**。`coordination-rules.md`：**系統不得直接改動彼此領域**。

問題：**用什麼機制讓這些系統解耦通訊，又能承載精確的 payload 契約、可測試、且不引入 GC/耦合？**

---

## Decision（決策）

### 1. 型別化事件匯流排 (Typed Event Bus) 於 `Core`

在 `KaijuBreaker.Core` 定義 `IEventBus` 與**強型別事件 struct**（一事件一 `readonly struct`，欄位即 GDD payload 契約）。發布/訂閱以泛型 API：

```
IEventBus.Publish<T>(in T evt);
IEventBus.Subscribe<T>(Action<T> handler);   // T : struct, IGameEvent
```

- **事件為 `readonly struct`（值型）**：承載 payload 無 managed 配置、`in` 傳遞避免複製、穩態零 GC——與零 GC 目標一致。
- **契約集中於 `Core`**：`PartBroke`（= `on_part_break`）、`LaserHit`、`MissileHit`… 型別是系統間唯一共享面。欄位對齊 GDD 契約，穩定 ID 追蹤於 `tr-registry.yaml`。
- **同步分發 (synchronous dispatch)**：事件在發布當幀同步派送，維持 GDD 要求的**同幀語義**（如 `on_part_break` 同幀入帳、L2 Tier-3 漣漪同幀、Boss Core 破壞事件順序 `on_part_break`→`on_boss_core_break`）。

### 2. 唯讀查詢介面 (Read-Only Query Interfaces) 於 `Core`，DI 注入

非事件的跨系統讀取走介面注入，**不用事件、不用單例**：

```
IPartStateQuery   → KaijuParts 實作；Weapons/UI 注入（heat_state, armor_state, world_position, H_current…）
IDifficultyProvider → Difficulty 實作；Stage/BulletSim 注入（bullet_density_mult, enemy_count_mult）
ISaveService      → Meta 實作；Economy/Stage/UI 注入（讀庫存、enqueue autosave）
IWeaponTierQuery  → Meta/Economy 提供；Weapons 注入（讀 Tier 套用旋鈕）
```

`KaijuBreaker.App`（組合根）建構實作並注入。系統測試時注入假實作 (fake/stub)，滿足「獨立可測」。

### 3. 事件所有權原則 (Event Ownership)

- **發出者擁有事件定義**；`on_part_break` 由 **`KaijuParts` 發出**（非武器系統——`kaiju-part-system.md` C.5 明訂；武器系統作為接收方清碰撞體）。
- **payload 一次性攜齊下游所需資料**（如 `break_quality`, `world_position`, `drop_table_id`），避免下游回查造成時序耦合。
- **`shard_yield`/`core_yield` 不放進 `on_part_break` payload**：由 `Economy` 依 `break_quality`+`kaiju_id` 獨立計算（對齊 `material-economy.md` F.1），`Meta` 讀 `Economy` 結果入帳。此為三方（part/economy/save）確認的資料流（architecture.md 開放問題 5）。

### 4. DOTS↔Mono 事件橋 (Bridge)

敵彈碰撞在 ECS Burst Job 產出 `NativeQueue<HitEvent>`（值型）；主執行緒 **Bridge** 排空佇列，翻譯為匯流排事件（`on_missile_hit` republish、`PlayerHit`）。**Bridge 是 ECS 世界與 managed 事件匯流排的唯一翻譯層**（architecture.md §5.3），單點可測、可替換（ADR-0001 退路只換此處）。

---

## Alternatives Considered（替代方案）

### A. C# `event` / `Action` 直接互訂（系統各自持有委派）
- **優點**：最簡單、零基礎設施。
- **缺點**：訂閱者需引用發布者型別 → 系統互相引用 → 破壞組件邊界與 DI；難集中管理契約；生命週期/取消訂閱易漏。
- **否決**：抵觸 ADR-0005 的組件解耦。

### B. ScriptableObject 事件通道 (SO Event Channels)（每事件一 SO 資產）
- **優點**：Inspector 可視、設計師可接線、Unity 社群常見模式。
- **缺點**：payload 豐富的契約（`on_part_break` 8 欄位）用 SO channel 承載笨重；每事件一資產爆量；同幀語義與型別安全較弱；跨界仍需翻譯。
- **否決為主幹**：對高頻、富 payload、需同幀語義的核心戰鬥鏈不適合。**可選作 UI/設定等低頻、需 Inspector 接線處的輔助**（非 MVP 阻斷）。

### C. 純查詢輪詢 (Polling)（每幀主動查狀態，無事件）
- **缺點**：破壞同幀事件語義（頓幀、素材入帳、鏈式效果都需事件驅動的即時性）；浪費；難表達「破壞瞬間」這類離散事件。
- **否決**：與 GDD 的事件驅動設計本質不符。

### D. 全域靜態事件單例 (static EventManager)
- **否決**：`coding-standards.md` 明禁 singletons over DI；不可測試替換。（例外：`Core` 的匯流排存取點本身設計為可注入/可替換，非有狀態單例。）

---

## Consequences（後果）

### 正面
- 系統零直接互引用 → 組件邊界乾淨、可獨立編譯與測試（注入假事件/假查詢）。
- 型別化 struct 事件 → payload 契約即編譯期型別、穩態零 GC、同幀語義。
- 契約集中 `Core` + `tr-registry.yaml` → 漂移可控、可追溯。
- Bridge 單點橋接 DOTS↔Mono → 混合架構整合清晰、退路明確。

### 負面 / 成本
- 需維護一套事件匯流排基礎設施（發布/訂閱/生命週期/測試替身）。
- 每個 GDD 事件需對映一個 `Core` struct 型別，並隨契約演進維護（有紀律成本）。
- 同步分發需注意重入 (re-entrancy)：如 M3 Tier-3 鏈式在 `on_part_break` 處理中觸發下一個 `trigger_part_break(is_chain_break=true)`——以 `is_chain_break` 旗標防遞迴（`kaiju-part-system.md` E.4），非遞迴，設計已定。
- 事件順序敏感處（Boss Core：`on_part_break`→`on_boss_core_break`）需 Bridge/發出者保證固定順序。

### 效能意涵
- 事件為值型 struct + `in` 傳遞 → 穩態零 GC，支持彈幕零 GC 目標。
- 同步分發無執行緒切換開銷；跨界僅值型 struct，無 managed 引用進 ECS。

---

## Reversibility（可逆性）
**高**：匯流排 API 與查詢介面是內部抽象；若需改分發策略（如加入延遲佇列或 SO channel 輔助）只動 `Core` 實作，事件型別與訂閱點不變。

---

*ADR 版本：1.0.0*
