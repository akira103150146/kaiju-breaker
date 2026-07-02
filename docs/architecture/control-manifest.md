# 控制清單 (Control Manifest) — 殲獸戰機 / KAIJU BREAKER

*文件路徑：docs/architecture/control-manifest.md*
*最後更新：2026-07-02*
*作者：Technical Director Agent*
*用途：`/dev-story` 實作時的每日速查表——程式設計師照此執行，無需重讀每份 ADR。*

> **這是規則表，不是說明文件。** 每條是 MUST（必須）或 MUST NOT（絕不）。
> 權威來源：architecture.md、ADR-0001~0005、technical-preferences.md、coding-standards.md。
> 遇到本表與程式碼衝突 → 以本表 + ADR 為準並回報 Technical Director。

---

## 0. 來源狀態警示 (Source Status)

- **ADR-0002 / 0003 / 0004 / 0005 = Accepted**：以下由其衍生的規則為**已鎖定 (binding)**。
- **ADR-0001 = Proposed**（待手機效能原型閘門：1,000 敵彈 @60fps、0 GC/frame）。**§3 BulletSim 全部規則為暫定 (provisional)**——後端可能退回純 Mono 池。撰寫層與事件契約不受影響（那才是可逆性的重點）。
- 任何標 **[需查證 6.3 API]** 之處：實作前查 `docs/engine-reference/unity/VERSION.md` + 官方文件。**絕不臆造 API 簽名。**

---

## 1. 全域規則 (Global Rules — 適用所有系統)

### 1.1 命名 (Naming)
- **MUST** 類別 / 方法 / 屬性用 `PascalCase`（`PartStateMachine`, `TryUpgrade`, `CurrentHeat`）。
- **MUST** 私有欄位用 `_camelCase`（`_activeBullets`）。
- **MUST** ScriptableObject 資產檔用 `PascalCase.asset`（`L2_FocusBeam.asset`）。
- **MUST** ECS component 用 `PascalCase` struct + `IComponentData`（`BulletVelocity`）。
- **MUST** 事件型別命名沿用 GDD 契約名的 PascalCase 對映：`on_part_break` → `PartBroke`、`on_laser_hit` → `LaserHit`、`on_missile_hit` → `MissileHit`（權威：architecture.md §3.3 + ADR-0002）。
  - ⚠️ **來源衝突（見 §6）**：technical-preferences 寫「C# events `On` 前綴（`OnPartDestroyed`）」。**本清單以 ADR-0002 的無前綴事件型別名為準**（`PartBroke`），因 ADR 為 binding 且事件是 struct 型別非 C# `event` 委派。C# `event`/`Action` 欄位若有才加 `On` 前綴。
- **MUST** 常數用 `PascalCase`（`MaxConcurrentBulletsMobile`）；`UPPER_SNAKE_CASE` 亦被 technical-preferences 允許但**本專案優先 PascalCase**求一致。

### 1.2 資料驅動 (Data-Driven — ADR-0003)
- **MUST** 所有 gameplay / balance 數值來自 ScriptableObject（放 `Assets/_Project/Content/`），執行期唯讀載入。
- **MUST NOT** 在 gameplay 程式碼寫任何魔數 / 硬編碼平衡值 / `const` 調校值。
- **MUST** 玩家可變資料（Tier 等級、素材、設定覆寫）走 JSON 存檔（ADR-0004），**絕不寫回 SO**。
- **MUST** 跨 GDD 共享的旋鈕只有一個擁有者 SO（如難度乘數只在 `DifficultyConfig`）；其他系統經介面讀，不複製值。
- **MUST** SO 用 `OnValidate` 對 GDD 安全範圍做編輯期斷言。

### 1.3 DI 優於單例 (DI over Singletons — ADR-0005)
- **MUST** 系統以建構子 / 方法注入依賴（`IEventBus`、查詢介面、config SO）。
- **MUST NOT** 用持有遊戲狀態的 static 單例。唯一允許的 static = 無狀態工具 + `Core` 匯流排存取點（可測試替換）。
- **MUST** 所有 public 方法可脫離其他系統、用假事件 / 假查詢 / 假 SO 單元測試。

### 1.4 組件邊界 (Assembly Boundaries — ADR-0005)
- **MUST** 一系統一 `.asmdef`；系統只依賴 `Core` + `Content`（+ 必要的 `Difficulty` 唯讀）。
- **MUST NOT** 任一系統組件引用另一系統組件（`Weapons` 不引用 `KaijuParts`）。跨系統只走 `Core` 的 `IEventBus` + 查詢介面。
- **MUST** 只有 `App`（組合根）引用全部系統並佈線 DI。

### 1.5 提交 (Commits — coding-standards)
- **MUST** 用 Conventional Commits：`feat:` / `fix:` / `chore:` / `docs:` / `test:` / `refactor:`。
- **MUST** commit body 引用 story / task ID（`Story: EPIC-001-S02`）與相關設計文件。
- **MUST NOT** 未經使用者指示 commit。

### 1.6 測試 (Testing — coding-standards)
- **MUST**（新增 gameplay 系統時）先寫測試（verification-driven）。
- **MUST** 依 story 類型附測試證據，方可標 Done：
  - **Logic**（公式 / AI / 狀態機）→ 自動化單元測試通過【BLOCKING】。
  - **Integration**（多系統）→ 整合測試或紀錄 playtest【BLOCKING】。
  - **Visual/Feel** → 截圖 + lead 簽核【ADVISORY】。
  - **UI** → 手動走查文件或互動測試【ADVISORY】。
  - **Config/Data** → smoke check【ADVISORY】。
- **MUST** 測試決定性（無亂數種子 / 無時間相依）、隔離（自建自拆狀態）、獨立（不依執行順序）、無 I/O（DI 注入假依賴）。
- **MUST** 測試檔命名 `[system]_[feature]_test`；函式 `test_[scenario]_[expected]`。
- **MUST NOT** 為過 CI 停用 / skip 失敗測試——修根因。
- ⚠️ **來源衝突（見 §6）**：coding-standards 說測試放 `tests/unit/[system]/`；ADR-0005 說放 `Assets/_Project/Tests/<Module>/`（EditMode）+ `Assets/_Project/Tests/PlayMode/`。**Unity 專案以 ADR-0005 路徑為準**（Unity Test Framework 需組件內）。

### 1.7 效能預算 (Performance Budget)
- **MUST** 目標 60 FPS（PC + 手機）；frame budget 16.6 ms。
- **MUST** draw call ≤ 200（全域，URP 2D，靠 SpriteAtlas 批次）；敵彈子系統目標個位數 draw call。
- **MUST** 手機彈幕+碰撞份額 ≤ 3.5 ms（PC ≤ 2.0 ms）[需引擎階段驗證]。
- **MUST** 穩態 0 B/frame GC alloc（彈幕熱路徑）。
- **MUST NOT** 為數量犧牲可讀性——同屏硬上限 (`readability_cap_priority`) 不可關。
- 記憶體天花板：[待設定——選定最低規手機後]。

### 1.8 文件與 ADR
- **MUST** 所有 public API 有 doc comment。
- **MUST** 每個系統有對應 ADR（新系統走 `/architecture-decision`）。

---

## 2. 分層規則 (Per-Layer Rules)

依賴方向由上而下，**只可向下依賴**，絕不反向。

| 層 (Layer) | 組件 | MUST 依賴 | MUST NOT 依賴 |
|---|---|---|---|
| **Foundation** | `Core` | 僅 `UnityEngine` | 任何系統組件、`Content`、DOTS 套件、任何實作 |
| **Foundation** | `Content`（SO 定義） | `Core` | 任何系統組件、DOTS、`App` |
| **Feature** | Weapons, KaijuParts, Economy, Meta, Stage, Difficulty, Input, GameFeel | `Core` + `Content`（Stage 額外 `Difficulty` 唯讀） | **任何其他 Feature 系統**、DOTS 套件、`App` |
| **Feature (DOTS)** | `BulletSim` | `Core` + `Content` + Unity.Entities/Burst/Collections/Mathematics | 其他 Feature 系統、`App`；DOTS 型別**不得外洩** |
| **Presentation** | `UI`, `GameFeel` | `Core` + `Content`（訂閱事件 + 查詢介面） | 其他 Feature 系統、直接改遊戲狀態 |
| **Composition** | `App` | **全部**（唯一組合根） | — |
| **Tests** | `<System>.Tests` | 對應 System + `Core` + `Content` + NUnit | 其他系統的 Tests |

- **MUST**：`Core` 只放真正共享的抽象（事件型別、查詢介面、共用型別、狀態機列舉、數學工具）——**不放實作**，防 `Core` 膨脹。
- **MUST**：新跨系統通訊需求 → 在 `Core` 加事件型別或查詢介面（有意的摩擦），不可直接互引用。

---

## 3. 分系統規則 (Per-System Rules)

### `Core`（橫切基礎）
- **MUST** 擁有 `IEventBus`、所有事件 `readonly struct`、查詢介面、共用型別（`WeaponId`, `PartType`, `BreakQuality`, `DifficultyTier`）、狀態機列舉。
- **MUST NOT** 依賴任何系統或 `Content`；不放任何實作邏輯。

### `Content`（SO 定義）
- **MUST** 集中所有 SO 定義類別（`WeaponDef`, `PartDef`, `KaijuDef`, `DifficultyConfig`, `GameFeelConfig`, `EmitterPatternSO`, `StageDef`, `EconomyConfig`, `SaveConfig`, `InputSettings`）。
- **MUST** 每個 config 類別有 `OnValidate` 範圍檢查。
- **MUST NOT** 含執行期行為邏輯（純資料 + 驗證）。

### `BulletSim`（S9 彈幕）— ⚠️ 規則暫定，ADR-0001 待閘門
- **MUST** DOTS/ECS + Burst + Jobs **僅在本組件**；敵彈為純資料 struct。
- **MUST** 跨 DOTS↔Mono 邊界只傳值型 struct（POD）——玩家點座標、部位 AABB、密度乘數、`Time.timeScale` 進；`HitEvent` 出。
- **MUST** 池與 `NativeArray` 預配置（手機 1536 / PC 2560 敵彈；×1.3 餘裕）；執行期零配置。
- **MUST** 讀 `IDifficultyProvider.bullet_density_mult` 只縮放彈數 / 臂數 / 射頻；速度 / 形狀恆定；密度後過同屏硬上限。
- **MUST** 敵彈共用單一暖色 sprite atlas + 單材質。
- **MUST** 命中結果經 `NativeQueue<HitEvent>` 交主執行緒 Bridge，由 Bridge 翻譯為匯流排事件。
- **MUST** 把 `Time.timeScale` 併入模擬 deltaTime 來源，使頓幀 (`timeScale=0`) 凍結敵彈 [需查證 6.3 API]。
- **MUST NOT** 讓 `Entity` / DOTS 型別 / managed 引用外洩出本組件或進 ECS World。
- **MUST NOT** 逐彈 `MonoBehaviour.Update()`；無 `Instantiate` / `Destroy` / boxing 於熱路徑。
- **MUST** 玩家雷射**不進 ECS**（連續判定屬 `Weapons`）；玩家飛彈池歸屬為開放項（量測後定）。

### `Weapons`（S1 武器）
- **MUST** 雷射用 raycast/overlap 對 ≤8 部位在 Mono 側判定並發 `on_laser_hit(part_id, kaiju_id, heat_delta)`。
- **MUST** 飛彈命中發 `on_missile_hit(part_id, kaiju_id, break_delta_base, weapon_id)`；L3 發 `on_l3_wave_hit(part_id, kaiju_id)`。
- **MUST** 經 `IWeaponTierQuery` 讀當前 Tier，再從 `WeaponDef` 取該 Tier 靜態旋鈕。
- **MUST** 保持 D₀ 等功率約束**資料驅動**（在 `WeaponBalanceConfig`/`WeaponDef`），由 8×8 TTB 矩陣測試驗證。
- **MUST** 追蹤飛彈 / Tier-3 觸發經 `IPartStateQuery` 讀部位 `world_position` / `heat_state`。
- **MUST NOT** 引用 `KaijuParts`；**MUST NOT** 自己發 `on_part_break`（那是接收方，命中後由部位系統判定破壞）。
- **MUST**（接收 `on_part_break` 時）僅清自身碰撞體 / 觸發 L2·M3 Tier-3 效果，不改部位狀態。

### `KaijuParts`（S2 可破壞部位）
- **MUST** 擁有部位狀態機、相鄰圖、STAGGERED / 護甲閘門邏輯。
- **MUST** 依 `heat_delta` 累蓄熱 H → SOFTENED；依 `break_delta_base` 累破甲 B → 護甲閘門 / STAGGERED。
- **MUST** 由本系統**獨占發出** `on_part_softened/_exit`、`on_part_staggered/_stagger_end`、`on_part_break(...)`、`on_boss_core_break`。
- **MUST** 在破壞成立那一幀計算 `break_quality`（`NORMAL` / `SOFTENED` / `SOFTENED_STAGGERED`，依 `heat_state`/`stagger_timer`）並放入 payload。
- **MUST** `on_part_break` payload 一次攜齊：`part_id, kaiju_id, part_type, world_position, drop_table_id, break_quality, adjacency_list, is_chain_break`。
- **MUST** 實作 `IPartStateQuery`（`heat_state`, `armor_state`, `world_position`, `H_current`/`H_max`）。
- **MUST** 鏈式破壞以 `is_chain_break=true` 旗標防遞迴（非遞迴呼叫）。
- **MUST** Boss Core 破壞時保證事件順序 `on_part_break` → `on_boss_core_break`。
- **MUST NOT** 把 `shard_yield`/`core_yield` 算進 payload（那是 `Economy` 的職責）。

### `Economy`（S3 素材經濟）
- **MUST** 訂閱 `on_part_break` / `on_hunt_end`，依 `break_quality` + `kaiju_id` **獨立計算** shard/core 產量。
- **MUST NOT** 從 payload 讀「已算好的產量」——payload **不含**產量，讀 `break_quality` 自算（對齊 material-economy.md F.1）。
- **MUST** 產量倍率 / 升級成本表全在 `EconomyConfig`（零硬編碼）。
- **MUST** 由 27 情境素材產量測試覆蓋。

### `Meta`（S10 元進度 / 存檔）— ADR-0004
- **MUST** 實作 `ISaveService`；單槽 JSON 於 `Application.persistentDataPath`（`save.json` + `.bak` + `.tmp`）。
- **MUST** 原子寫入：canonical 序列化 → CRC32 → 寫 tmp → flush/fsync → rename → copy 備份。
- **MUST** canonical 形式：key 字母排序、無空白、浮點固定格式（CRC32 決定性依賴此）。
- **MUST** 訂閱 `on_part_break` 同幀寫記憶體永久庫存（素材數值取自 `Economy` 結果）+ `enqueue_save`。
- **MUST** 背景 Save Worker 執行緒，佇列深度 1 覆蓋式；`on_app_suspend`/`quit` 走**同步寫**安全網 [需查證 6.3 API]。
- **MUST** 載入 CRC32 校驗失敗 → 讀備份 → 皆失敗顯示錯誤+重置，**不崩潰**。
- **MUST** 版本遷移純函數鏈，最多向前 3 世代；未來版本拒載；缺欄位填新遊戲預設。
- **MUST NOT** 用 `PlayerPrefs` / `BinaryFormatter` / 直接覆寫 `save.json`（無 tmp-rename）。
- **MUST NOT** 做 DRM / 防篡改（明確非目標）；**MUST NOT** 臆造 `JsonUtility` 能力——序列化器選型於實作決定並記錄。

### `Stage`（S4 關卡 / 波段 + Run 流程）
- **MUST** 驅動 Run 狀態機 `LOADOUT → STAGE → BOSS → RESULTS`（`RunController`，純 C# 可測）。
- **MUST** 在對的轉換點觸發 autosave（`on_loadout_confirmed`、莢艙拾取、部位破壞、`on_hunt_end`）。
- **MUST** 波段隨機重組 + 莢艙保底掉落；Pre-Boss Lull 非同步預載 `kaiju_<id>`。
- **MUST** 讀 `IDifficultyProvider` 取密度 / 敵量乘數，不自存。
- **MUST NOT** 引用其他 Feature 系統（`Difficulty` 唯讀例外，經介面）。

### `Difficulty`（S5 難度）
- **MUST** 為 `enemy_count_mult` / `bullet_density_mult` 的**唯一權威來源** (single source of truth)，實作 `IDifficultyProvider`。
- **MUST** 乘數全在 `DifficultyConfig`。
- **MUST NOT** 讓其他系統複製 / 快取難度值（一律經介面查）。

### `Input`（S6 輸入）
- **MUST** 用 Unity Input System 抽象三方案動作（觸控 / 鍵鼠 / 手柄），觸控為一指相對偏移拖曳。
- **MUST** 輸入輪詢走 `Time.unscaledDeltaTime`（頓幀不得吃掉閃避輸入）。
- **MUST** 輸入手感數值**不隨難度縮放**；預設在 `InputSettings`（SO），玩家覆寫存 save。
- **MUST** 全 UI 支援滑鼠點擊 + 觸控點按——**MUST NOT** 用 hover-only 互動（維持雙平台輸入 parity）。

### `GameFeel`（S8 打擊感）
- **MUST** 訂閱部位事件消費（`on_part_softened`、`on_part_break` 等）；頓幀 / 慢動作 / 震動 / 閃光 / SOFTENED 簽章 / 素材軌道球。
- **MUST** 頓幀 `Time.timeScale=0`、慢動作 `=0.12` 作用於 scaled 世界；螢幕震動 / 閃光淡出走 `unscaledDeltaTime`。
- **MUST** 全部旋鈕在 `GameFeelConfig`。
- **MUST NOT** 改遊戲 / 部位狀態（純表現層消費者）。

### `UI`（S7 HUD/UI）
- **MUST** 訂閱事件 + 經 `IPartStateQuery` 查詢畫世界座標部位血條（`H_current`/`H_max`）、武器 HUD、素材計數。
- **MUST** Loadout / 升級 / 難度三畫面。
- **MUST NOT** 直接改遊戲狀態或引用其他 Feature 系統。
- 框架暫定 UGUI（UI ADR 待補，非 MVP 阻斷）。

---

## 4. 事件契約規則 (Event-Contract Rules — ADR-0002)

### 4.1 匯流排機制
- **MUST** 事件為 `Core` 的 `readonly struct`（實作 `IGameEvent`），欄位即 GDD payload 契約。
- **MUST** 用 `IEventBus.Publish<T>(in T evt)` / `Subscribe<T>(Action<T>)`；`in` 傳遞避免複製 → 穩態零 GC。
- **MUST** 同步分發 (synchronous dispatch)：事件於發布**當幀同步派送**，維持 GDD 同幀語義。
- **MUST NOT** 用 C# `event`/`Action` 直接互訂（會迫使引用發布者型別）；**MUST NOT** 用輪詢取代離散事件；**MUST NOT** 用 static `EventManager`。
- SO event channel 只可作 UI/設定等低頻、需 Inspector 接線處的輔助，**MUST NOT** 承載核心戰鬥鏈。

### 4.2 命中→破壞→獎勵鏈（方向與所有權）
```
Weapons ──(on_laser_hit / on_missile_hit / on_l3_wave_hit)──► KaijuParts
KaijuParts ──(on_part_break, 含 break_quality)──► Economy + Meta + GameFeel + UI + Weapons
KaijuParts ──(on_boss_core_break, 在 on_part_break 之後)──► RunController(Stage)
```
- **MUST** 發出者擁有事件定義：`Weapons` 發命中事件；`KaijuParts` 發 `on_part_break` / core break（**非** Weapons）。
- **MUST** 全鏈**同步、同幀**完成：破壞成立 → 素材入帳 → autosave enqueue → 頓幀 → UI 更新，皆同一幀。
- **MUST** payload 一次攜齊下游所需（`break_quality`, `world_position`, `drop_table_id`…），下游**不得回查**造成時序耦合。
- **MUST** `break_quality` 由 `KaijuParts` 在破壞幀計算；`Economy` **讀取而非重算**破壞品質，但**自算**產量。
- **MUST** DOTS 側命中經 Bridge（唯一翻譯層）republish 為匯流排事件；跨界只傳值型 struct。
- **MUST** 同步分發下處理重入：鏈式破壞用 `is_chain_break` 旗標防遞迴；順序敏感事件由發出者 / Bridge 保證固定順序。

### 4.3 查詢介面（非事件讀取）
- **MUST** 跨系統唯讀查詢走 `Core` 介面 DI 注入，**不用事件、不用單例**：
  - `IPartStateQuery`（KaijuParts 實作 → Weapons/UI 注入）
  - `IDifficultyProvider`（Difficulty 實作 → Stage/BulletSim 注入）
  - `ISaveService`（Meta 實作 → Economy/Stage/UI 注入）
  - `IWeaponTierQuery`（Meta/Economy 提供 → Weapons 注入）
- **MUST** 測試時注入假實作 (fake/stub)。

---

## 5. 禁止模式 (Forbidden Patterns — 整合)

- **MUST NOT** 硬編碼 gameplay / balance 數值——一律 SO（ADR-0003）。
- **MUST NOT** 跨系統組件互相引用——只依賴 `Core`；`App` 為唯一組合根（ADR-0005）。
- **MUST NOT** 用持有遊戲狀態的 singleton——DI over singletons（coding-standards / ADR-0005）。
- **MUST NOT** 用 rigidbody 模擬敵彈——kinematic 移動 + trigger overlap / DOTS sim（ADR-0001）。
- **MUST NOT** 讓 DOTS / `Entity` 型別外洩出 `BulletSim`——只經 Bridge 以值型 struct 跨界（ADR-0001/0002）。
- **MUST NOT** 熱路徑 `Instantiate`/`Destroy`/boxing / 逐彈 `Update()`（效能）。
- **MUST NOT** 為數量犧牲彈幕可讀性（`readability_cap_priority` 不可關）。
- **MUST NOT** 用 `PlayerPrefs` / `BinaryFormatter` / 無 tmp-rename 的直接覆寫存檔（ADR-0004）。
- **MUST NOT** 停用 / skip 失敗測試以過 CI（coding-standards）。
- **MUST NOT** 臆造 Unity 6.3 API 簽名——查 `engine-reference/unity/VERSION.md`。
- **MUST NOT** 把靜態調校值寫回 SO 或把玩家可變值塞進 SO——SO 唯讀、save 可寫，兩者不混（ADR-0003/0004）。

---

## 6. 已發現的來源衝突 (Flagged Contradictions)

1. **事件命名前綴** — technical-preferences.md §Naming 說 C# events 用 `On` 前綴（`OnPartDestroyed`）；architecture.md §3.3 + ADR-0002 用無前綴事件 struct 型別名（`on_part_break` → `PartBroke`, `LaserHit`, `MissileHit`）。**裁決：事件 struct 型別以 ADR-0002 為準（無前綴，binding）；`On` 前綴僅用於 C# `event`/`Action` 委派欄位。** 建議更新 technical-preferences 消歧。

2. **測試檔位置** — coding-standards.md 說 `tests/unit/[system]/`、`tests/integration/[system]/`；ADR-0005 + architecture.md §3.2 說 `Assets/_Project/Tests/<Module>/`（EditMode）+ `Tests/PlayMode/`。**裁決：Unity 專案以 ADR-0005 路徑為準**（Unity Test Framework 需組件內）。coding-standards 的路徑是引擎無關通則。

3. **常數命名** — technical-preferences 允許 `PascalCase` 或 `UPPER_SNAKE_CASE`；architecture.md §3.3 僅示 `PascalCase`。**裁決：優先 `PascalCase`** 求一致（非硬衝突，僅收斂）。

4. **ADR-0001 狀態** — 被引用為「binding 決策」但實為 **Proposed**（待效能原型閘門）。**§3 BulletSim 規則為暫定**；若閘門未過退回純 Mono 池，本表 BulletSim 段需修訂（撰寫層 SO + 事件契約不變）。

5. **draw call 數字**（非衝突，記錄）— 全域預算 ≤200（technical-preferences）；敵彈子系統目標個位數（architecture §8.4）。兩者相容：個位數是子系統目標，200 是全畫面上限。
