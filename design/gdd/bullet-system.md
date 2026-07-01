# 彈幕系統 (Bullet / Danmaku System) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑：design/gdd/bullet-system.md*
*最後更新：2026-07-01*
*狀態：Draft — 技術策略文件（Technical Strategy）*
*引擎：Unity 6.3 LTS（C#）*
*相依文件：game-concept.md（視覺鐵則、最高技術風險）| difficulty-system.md（密度縮放權威）| weapon-system.md（玩家武器 / 部位事件）| kaiju-part-system.md（部位碰撞消費端）| kaiju/01-carapex.md · 02-lacera.md · 03-voltwyrm.md（彈幕模式來源）*

> **本文件是策略文件，非實作規格。** 它為後續 `/architecture-decision` 架構階段設定方向與預算；具體 ECS 資料佈局、Job 排程、Blob 資產結構等由架構階段的 ADR 定案。所有標記 **[需引擎階段驗證]** 的數值為目標值，須以原型效能量測確認後才可視為承諾。

---

## 1. 概覽 (Overview)

彈幕系統是殲獸戰機的即時模擬核心：它負責敵彈（暖色 / 威脅）與部分玩家彈（飛彈）的生成、運動、碰撞與回收。它同時是 `game-concept.md` 明列的**最高技術風險**——「同畫面上千子彈 × 觸控 × 手機」。系統設計圍繞三個不可讓步的目標：

1. **零 per-bullet GC**：所有子彈來自預配置物件池，執行期不做逐彈堆配置。
2. **資料驅動的模式撰寫**：設計師以資料資產（非程式碼）表達三頭目所有既有模式（螺旋、瞄準牆、聚肢扇形、爆裂環、水平寬扇、十字 / 八方），並接受 `difficulty-system.md` 的密度乘數 hook。
3. **彈幕永遠讀得懂**：任何密度下，玩家彈 / 敵彈 / 判定點三者色溫可辨；系統以硬性同屏彈數上限與可讀性護欄保證此鐵則不被效能或密度衝破。

系統不定義部位狀態機、武器數值或素材掉落——它在碰撞發生時**發出既有事件**（`on_laser_hit` / `on_missile_hit` / `on_l3_wave_hit`，權威定義於 weapon-system.md F.1 與 kaiju-part-system.md），本身不消費也不重定義這些契約。

---

## 2. 玩家幻想 (Player Fantasy) — 可讀的混亂 (Readable Chaos)

**「螢幕上有一千顆子彈，而我看得清每一顆。」**

彈幕射擊的爽感建立在一個矛盾上：畫面必須**看起來**危險到令人屏息，但**讀起來**永遠公平。玩家死亡時的正確情緒是「我看到了那顆子彈、我判斷失誤」，而非「不知道哪來的子彈打死我」。這條情感線由三個系統承諾支撐：

- **判定點至上**：玩家的受擊點是單一小點（single point），恆亮冷色一格；戰機外殼再大，生死只由那一點決定——這讓「貼彈擦身」成為可控的技術表現，而非運氣。
- **色溫即生死**：暖色一律是威脅、冷色一律是你。玩家用色溫在 0.1 秒內分辨安全與危險（game-concept.md 視覺鐵則）。
- **密度是壓力，不是混亂**：D4 惡夢的子彈是 D1 的兩倍（difficulty-system.md D.3），但**速度不變、形狀不變、間隙規則不變**——更多子彈製造更緊的走位，而非更差的可讀性。

系統的最終驗收不是「能跑多少子彈」，而是「在能跑的子彈數下，玩家仍讀得懂」。效能預算與可讀性上限**互為約束**：見第 5、9 節。

---

## 3. 架構 (Architecture)

系統由三個解耦子系統構成：**彈池（Pool）** → **模式模擬（Emitter / Pattern Simulation）** → **碰撞（Collision）**。資料流單向：模式向池請求子彈實例 → 模擬每幀推進子彈狀態 → 碰撞廣相偵測 → 命中發出事件並將子彈歸還池。

```
[Emitter 資產]──spawn request──▶[Bullet Pool]──active set──▶[Simulation Job]
      ▲                                                          │
      │ density_mult (difficulty)                                ▼
[Difficulty System]                                    [Collision Broad-phase]
                                                          │            │
                                             player point │            │ kaiju part
                                                          ▼            ▼
                                                   [despawn]   [on_missile_hit / …]
                                                                (weapon/part events)
```

### 3.1 更新方式決策：DOTS/ECS + Burst + Jobs（推薦）

**推薦：以 DOTS（Entities）+ Burst + Jobs 實作敵彈模擬，玩家彈與少量特例走輕量 MonoBehaviour 池。** 見第 5.3 節完整理由。核心論點：敵彈是「大量、同質、每幀對每顆做相同數學（位置積分、邊界剔除、廣相碰撞）」的工作負載——這正是 ECS + Burst 的最佳情境。手機（ARM）上 Burst 的 SIMD 向量化與無 GC 的 struct 資料佈局，是達成上千彈穩定 fps 的最可靠路徑，且直接回應 game-concept.md 對 DOTS+Burst 的預判。

### 3.2 彈生命週期與物件池 (Bullet Lifecycle & Object Pooling)

**鐵則：執行期零 per-bullet 配置。** 所有子彈容量在關卡載入時預配置（pre-allocated），生成 = 從閒置集合取用並寫入初始狀態；消滅 = 標記閒置並歸還。無 `Instantiate` / `Destroy`、無 `new`、無 boxing。

| 階段 | 行為 | 效能保證 |
|------|------|---------|
| **預配置（Load）** | 依平台上限一次性配置整池（見第 5 節容量目標）；ECS 下為預留 Entity 容量 + 元件陣列 | 關卡載入時完成，戰鬥中不再配置 |
| **生成（Spawn）** | Emitter 取閒置索引，寫入 `position / velocity / bullet_type / color_id / lifetime`；無配置 | O(1) 取用；批次生成一次寫入連續記憶體 |
| **推進（Simulate）** | 每幀 Burst Job 積分位置、遞減 lifetime、標記出界 | SIMD 向量化；無分支預測懲罰的資料導向迴圈 |
| **消滅（Despawn）** | 出界 / 命中 / lifetime 歸零 → 歸還閒置集合；swap-back 保持 active set 緊密 | O(1) 歸還；無記憶體釋放 |

**池分區**：敵彈池與玩家飛彈池**分離配置**，避免玩家彈耗盡影響敵彈預算，也讓碰撞廣相能各自最佳化（敵彈 vs 玩家點；玩家彈 vs 部位）。

### 3.3 模式模擬 (Pattern Simulation)

每顆敵彈為純資料（struct）：位置、速度、可選的曲率 / 角速度（螺旋）、可選的追蹤目標句柄（極少數，見第 6 節玩家飛彈；敵彈原則上**非追蹤**——三頭目所有敵彈均為固定方向或發射時瞄準，符合可讀性鐵則）。模擬 Job 每幀對 active set 做位置積分與邊界剔除，與碰撞 Job 之間以緊密的 active 陣列傳遞，避免隨機記憶體存取。

---

## 4. 模式撰寫模型 (Pattern Authoring Model)

**目標：設計師用資料資產撰寫彈幕，不寫程式碼，且能表達三頭目全部既有模式。** 以 Unity `ScriptableObject`（撰寫期）→ 烘焙為 Burst 友善的 Blob / struct（執行期）的雙層模型實作。

### 4.1 兩層結構

| 層級 | 形式 | 由誰使用 |
|------|------|---------|
| **撰寫層** | `EmitterPatternSO`（ScriptableObject 資產）＋ Inspector | 設計師 / 關卡設計師；可視化調參、無程式碼 |
| **執行層** | 烘焙後的不可變 Blob 參數 + Burst Job | 模擬系統；載入時從 SO 烘焙，執行期唯讀 |

一隻巨獸的一個攻擊模式（如 CARAPEX 模式 A）= 一或多個 `Emitter` 的組合，掛在部位上，由部位狀態機（ALIVE / BROKEN）啟停。部位破壞即停用其 Emitter——直接落實「破壞減少彈幕節點」（LACERA 斷肢、VOLTWYRM 頸段破壞降低螺旋臂數）。

### 4.2 Emitter 參數（能表達所有既有模式的最小集合）

| 參數 | 型別 | 用途 | 對應既有模式 |
|------|------|------|------------|
| `shape` | enum | `AIMED_FAN / RING / SPIRAL / WALL / CROSS`（可組合） | 見下對照表 |
| `bullet_count` | int | 每次發射彈數（**密度 hook 作用點**，見 4.4） | 所有模式的「每次彈數」 |
| `spread_deg` | float | 扇形總角 / 環間隔基準 | CARAPEX A ±25°、LACERA A/B 扇角 |
| `aim_mode` | enum | `FIXED_DIR / AIM_AT_PLAYER / RADIAL` | 瞄準玩家 vs 固定方向 |
| `spiral_arm_count` | int | 螺旋臂數（**密度 hook 作用點**） | VOLTWYRM 模式 A 臂數 |
| `spiral_angular_speed` | float | 每臂旋轉角速度（**難度不縮放**） | VOLTWYRM 螺旋 0.8s/周期 |
| `bullet_speed` | float (px/s) | 子彈速度（**難度恆定，永不縮放**） | 全部（120/100/90 px/s 等） |
| `fire_interval` | float (s) | 發射週期（**難度可縮放射頻**，見 4.4） | 各模式射頻 |
| `charge_telegraph_s` | float | 發射前電報 / 蓄力閃光時長 | 全模式電報（可讀性必需） |
| `color_id` | enum | 暖色調色盤索引（橙 / 黃 / 深紅） | 視覺鐵則強制暖色 |
| `spawn_origin` | ref | 綁定發射部位（動態 world_position） | LACERA 移動肢體發射源 |

### 4.3 三頭目模式對照（表達力驗證）

| 巨獸 / 模式 | 既有描述 | Emitter 表達 |
|------------|---------|------------|
| CARAPEX A 螯牙交叉 | 3 發扇形，中心瞄準玩家 ±25°，交替源 | `AIMED_FAN`, `aim=AIM_AT_PLAYER`, `spread=50`, 兩 Emitter 交替 |
| CARAPEX B 背甲礫散 | 5 發水平寬扇向下噴散 | `WALL`, `aim=FIXED_DIR`(下), `spread`覆蓋50%寬 |
| CARAPEX C 核心光刃 | Phase1 單發瞄準 / Phase3 4-way 十字 / D3-4 8-way | `AIMED_FAN`(1) → `CROSS`(4) → `CROSS+RADIAL`(8) |
| LACERA A 刃浪掃射 | 每肢掃過中心射扇形，相位各異 | 每肢一 `AIMED_FAN` Emitter，`spawn_origin`=移動肢，相位由掃弧驅動 |
| LACERA B 聚肢爆彈 | 四肢聚中心齊射向心扇形 | 四 Emitter 同幀觸發，`RING`/`AIMED_FAN` 向心，破壞肢不參與 |
| LACERA C 殘肢亂舞 | 殘肢加速、每弧射 2 次 | 同 A，`fire_interval` 減半（存活肢） |
| VOLTWYRM A 蛇陣螺旋 | 多臂旋轉螺旋，臂間均等間隙 | `SPIRAL`, `spiral_arm_count`=臂數, `spiral_angular_speed`恆定 |
| VOLTWYRM B 能量瞄準牆 | 向玩家收束 + 固定散角 | `WALL`, `aim=AIM_AT_PLAYER`, 固定 `spread` |
| VOLTWYRM C 護盾爆裂環 | 護盾為圓心均勻擴散環，12/20 顆 | `RING`, `bullet_count`=12/20, `aim=RADIAL` |
| VOLTWYRM Phase3 核心直射 | 6 顆窄束瞄準彈 | `AIMED_FAN`, `bullet_count`=6, 窄 `spread` |

三頭目所有模式均可由此參數集表達，且不需新增 shape 種類——**表達力充分**。

### 4.4 難度密度 hook（與 difficulty-system.md 對齊）

彈幕系統**讀取**難度密度乘數，不擁有它。權威為 `difficulty-system.md` D.2（`bullet_density_mult[tier]` ∈ {1.00, 1.25, 1.50, 2.00}）。作用規則：

```
actual_bullet_count   = ceil( base_count      × bullet_density_mult[tier] )   // 4.2 bullet_count
actual_spiral_arms    = ceil( base_arm_count  × bullet_density_mult[tier] )   // 或依巨獸表逐階指定
```

- **只縮放彈數 / 臂數 / 射頻**；`bullet_speed`、`spread_deg`、`spiral_angular_speed`、模式形狀、電報時長**恆定不縮放**（difficulty-system.md C.3；kaiju 各文件第 8 節）。
- 巨獸文件已逐階列出精確彈數（如 VOLTWYRM 臂數 1/2/3/4）者，**以巨獸文件表為準**（設計師顯式覆寫）；未逐階指定者，套用上式通用公式。
- 密度縮放後的 `actual_bullet_count` 送入生成路徑前，先過同屏彈數上限（第 9 節）——**可讀性 / 效能上限優先於密度乘數**。

---

## 5. 效能策略與預算 (Performance Strategy & Budgets) — 第一風險

### 5.1 目標與框架預算

| 平台 | 目標 fps | 框架預算 | 彈幕模擬 + 碰撞份額 [需引擎階段驗證] |
|------|---------|---------|--------------------------------------|
| **PC** | 60 fps 穩定（120 為加分） | 16.6 ms | ≤ 2.0 ms |
| **手機（中階基準機）** | 60 fps 穩定；最低不掉出 30 | 16.6 ms | ≤ 3.5 ms |

手機以「中階 Android（如發售前 3–4 年的中段機）」為基準機，非旗艦；達標基準機即涵蓋多數裝置。基準機型號於引擎階段確定並記錄。

### 5.2 同屏彈數預算 (Max Concurrent Bullets)

| 平台 | 敵彈同屏預算 [需引擎階段驗證] | 玩家飛彈池 | 池預配置容量（含餘裕） |
|------|------------------------------|-----------|----------------------|
| **PC** | 目標維持 1,500–2,000 顆穩定 60 fps | 256 | 敵彈 2,560 / 玩家 256 |
| **手機** | 目標維持 **800–1,200** 顆穩定 60 fps | 128 | 敵彈 1,536 / 玩家 128 |

- 池容量預配置為「同屏預算 × 約 1.3 餘裕」，吸收生成尖峰（如 VOLTWYRM Phase3 多環同觸發）。
- **手機 800–1,200 是本專案第一風險的核心承諾數字，明確標記 [需引擎階段驗證]**：須由效能原型在基準機上實測 sustain。若原型顯示達不到，處置順序為：(1) 降低 D4 密度乘數（difficulty-system.md G.1 安全範圍內），(2) 收緊同屏上限，(3) 最後才考慮視覺犧牲——**絕不犧牲可讀性換數量**。

### 5.3 為何選 DOTS/Burst 而非純 MonoBehaviour 池（Unity 6.3 論證）

| 準則 | MonoBehaviour + GameObject 池 | DOTS/ECS + Burst + Jobs（推薦） |
|------|------------------------------|--------------------------------|
| **手機同屏上限** | 數百顆即受 GameObject/Transform 開銷與 managed 記憶體局部性拖累 | 上千顆可行；資料連續、SIMD 向量化 |
| **GC** | 易因 managed 元件、事件配置產生 GC 尖峰 → 掉幀 | struct-only、無 managed 配置，天然零 GC |
| **多核** | 主執行緒為主，難平行 | Job 排程跨核心平行，手機多核受益顯著 |
| **維護成本** | 團隊熟悉、除錯直觀 | 學習曲線高、除錯工具較生疏 |
| **可逆性** | — | 中：模式撰寫層（SO）與模擬層解耦，若 DOTS 受阻可退回 MonoBehaviour 模擬而不動撰寫資產 |

**決策理由**：敵彈是同質大量、每幀相同數學的理想 DOP 負載；手機是首要平台且是風險所在；Burst 在 ARM 上的向量化是達標最可靠路徑。**風險緩解（關鍵）**：模式撰寫層（第 4 節 ScriptableObject）刻意與模擬後端解耦——撰寫資產不知道後端是 ECS 還是 MonoBehaviour。若引擎階段原型顯示 DOTS 整合成本過高，可退回 MonoBehaviour 池 + `NativeArray` + `IJobParallelFor` 的混合方案而不重寫任何模式資產。此可逆性是採用 DOTS 的前提。**此後端選擇須以 `/architecture-decision` ADR 正式定案，並以 5.2 效能原型驗證後才 LOCK。**

### 5.4 繪製批次與剔除

- **像素 sprite 批次**：所有敵彈共用單一 sprite atlas + 單一材質，以批次 / GPU instancing 一次繪製，將 draw call 壓到個位數。暖色調色盤限制少色，天然利於單 atlas。
- **離屏剔除**：出界子彈立即 despawn（非僅停繪）——省模擬與碰撞成本。剔除邊界略大於視口（約 +5–10% 螢幕邊緣），避免入畫邊緣彈突現。
- **無逐彈 Update()**：模擬集中於 Job，杜絕上千個 MonoBehaviour.Update 的呼叫開銷。

---

## 6. 碰撞 (Collision)

### 6.1 玩家判定 = 單點 (Single Point Hitbox)

玩家受擊判定為**單一小點**（game-concept.md 鐵則），恆亮冷色一格。敵彈 vs 玩家因此退化為「上千個移動圓 vs 一個點」——本質便宜，但需廣相避免每幀 O(N) 全掃退化為熱點。

### 6.2 廣相策略 (Broad-phase)

| 方向 | 策略 | 理由 |
|------|------|------|
| **敵彈 → 玩家點** | 以玩家點為中心的**小範圍查詢**；配合均勻空間網格（spatial hash grid），只檢查玩家所在格 + 鄰格內的敵彈 | 玩家只有一個查詢點，網格把每幀候選彈數從「全部」降到「附近數十顆」 |
| **玩家飛彈 → 巨獸部位** | 部位數少（≤ 8）且體積大；玩家飛彈少（≤ 256）→ 對少量部位 AABB / 網格粗篩即可 | 目標少，無需重型結構 |

空間網格每幀由模擬 Job 重建（子彈已在連續陣列，重建便宜）。網格格寬約等於最大子彈直徑的數倍，平衡格數與每格候選數。**[需引擎階段驗證]**：格寬與 32/64 顆的每格上限於原型調參。

### 6.3 命中即發事件（不重定義契約）

碰撞只負責偵測與**發出既有事件**，不定義後果：

- **敵彈命中玩家點** → 觸發玩家受擊（由玩家 / game-feel 系統消費；本系統僅發訊號 + despawn 該彈）。
- **玩家飛彈命中部位** → 發 `on_missile_hit(part_id, B_fill, state_mult)`；雷射命中發 `on_laser_hit` / `on_l3_wave_hit`。這些契約與後果（蓄熱、破甲、STAGGERED、素材）**權威在 weapon-system.md F.1 與 kaiju-part-system.md，本系統不重定義，只呼叫**。
- **護甲偏轉**（ARMORED 部位 ARMOR_INTACT 時 `B_fill=0`）等狀態判斷屬部位系統；彈幕系統照常發事件，由部位系統決定填充量。

---

## 7. 可讀性護欄 (Readability Guardrails) — 彈幕永遠讀得懂

系統以硬性機制保證視覺鐵則，不倚賴設計自律：

| 護欄 | 機制 | 來源 / 協調 |
|------|------|------------|
| **暖色強制** | 敵彈 `color_id` 限暖色調色盤（橙 #FF8000 / 黃 #FFCC00 / 深紅 #CC2200 等）；玩家彈限冷色 | game-concept.md 視覺鐵則；kaiju 各文件彈色 |
| **高對比像素外框** | 每顆敵彈黑色 / 亮色像素外框，深背景下高對比 | VOLTWYRM 第 3 節；LACERA 10.4 |
| **判定點恆亮** | 玩家單點判定恆顯冷色一格，永不被特效遮蔽（繪製於最高層） | game-concept.md「判定點至上」 |
| **電報保證** | 每模式發射前 `charge_telegraph_s` 蓄力閃光；密度縮放不縮短電報 | CARAPEX/LACERA/VOLTWYRM 各模式電報 |
| **同屏彈數上限** | 硬上限（第 5.2）截斷生成，即使密度乘數要求更多——螢幕永不擠到不可讀 | difficulty-system.md H.7 D4 可讀性下界 ≥ 70% |
| **螢幕震動上限協調** | 彈幕密度峰值時，與 game-feel 協調 screen-shake 上限，避免震動 + 高密度雙重壓垮可讀性 | 待 game-feel GDD；本系統提供「高密度中」訊號供其鉗制震動 |

**可讀性 > 密度 > 數量**：當三者衝突，永遠先保可讀性（截斷密度），再保數量。這是本系統對 game-concept.md 支柱的硬承諾。

---

## 8. 玩家彈 (Player Projectiles)

玩家武器（weapon-system.md）分兩類，在本系統中**走不同路徑**：

| 類型 | 武器 | 系統處理 | 理由 |
|------|------|---------|------|
| **雷射系（連續光束）** | L1 散波 / L2 集束 / L3 波動砲 / L4 穿透 | **非池化子彈**；每幀以 raycast / overlap 判定命中，光束為視覺表現 | 光束是連續判定，不是離散彈；L4 穿透 = 一條 raycast 命中路徑上所有部位（各發 `on_laser_hit`），L2 = 極窄 overlap，L1 = 3–4 條扇形 raycast |
| **飛彈系（離散彈）** | M1 追蹤 / M2 蜂群 / M3 穿甲 / M4 叢集 | **池化投射物**（獨立玩家彈池）；M1 / M2 部分含追蹤 | 離散、有飛行時間、部分追蹤，適合池化投射物路徑 |

### 8.1 雷射（連續光束）
- 每幀對其判定形狀（點 / 窄束 / 扇形束 / 穿透線）做 overlap / raycast，對命中部位發 `on_laser_hit`（或 L3 蓄力震波發 `on_l3_wave_hit`）。
- 不佔敵彈池、不進空間網格的敵彈通道；命中查詢直接對少量部位（≤ 8），成本可忽略。
- L4 穿透：單條 raycast 收集路徑上所有部位命中點，逐一發事件（實現 VOLTWYRM 縱列 100 HU/s 同步蓄熱）。

### 8.2 飛彈（池化投射物）
- 走與敵彈相同的池 + Job 模擬基建，但為**獨立玩家彈池**（第 3.2 分區）。
- M1 追蹤 / M2 部分追蹤：飛彈持有目標部位句柄，模擬時以 ±追蹤角（m1_tracking_angle_deg 等，權威在 weapon-system.md）向目標 world_position 轉向。追蹤飛彈是本系統少數的「追蹤實體」，數量小（≤ 128–256），不影響敵彈預算。
- 命中部位發 `on_missile_hit`；M3 穿甲 / M4 叢集的 AoE / 鏈式後果由武器 / 部位系統處理，本系統只發命中事件。

---

## 9. 系統相依 (Dependencies)

| 相依系統 | 方向 | 說明 |
|---------|------|------|
| `difficulty-system.md` | 難度 → 彈幕（輸入） | 讀取 `bullet_density_mult[tier]`；只縮放彈數 / 臂數 / 射頻（第 4.4）。速度 / 形狀恆定 |
| `weapon-system.md` | 彈幕 → 武器 / 部位（發事件） | 玩家彈命中發 `on_laser_hit` / `on_missile_hit` / `on_l3_wave_hit`（契約權威在 weapon-system F.1，本系統不重定義） |
| `kaiju-part-system.md` | 彈幕 → 部位（發事件 / 讀狀態） | 命中部位發事件；讀部位 world_position（LACERA 移動肢）與 ALIVE/BROKEN（啟停 Emitter）。護甲偏轉判斷屬部位系統 |
| `kaiju/01-03` | 巨獸 → 彈幕（撰寫資料） | 各巨獸模式 A/B/C 的 Emitter 資產與逐階密度表；本系統提供撰寫模型 |
| **game-feel GDD（待撰寫）** | 雙向協調 | 本系統提供「高密度中」訊號供 screen-shake 鉗制；受擊回饋表現由 game-feel 定義 |
| **VFX / SFX 系統** | 彈幕 → VFX | 電報閃光、命中特效、暖色調色盤 / 像素外框素材 |

---

## 10. 調校旋鈕 (Tuning Knobs)

**所有數值存於外部資料檔（`assets/data/bullets/bullet_config.yaml` 與各 `EmitterPatternSO` 資產），禁止硬編碼。**

| 旋鈕 | 預設值 | 安全範圍 | 類型 | 說明 |
|------|--------|---------|------|------|
| `pool_capacity_enemy_pc` | 2560 | 2048–4096 | 閘門 | PC 敵彈池預配置容量 |
| `pool_capacity_enemy_mobile` | 1536 | 1024–2048 | 閘門 | 手機敵彈池預配置容量 [需引擎階段驗證] |
| `pool_capacity_player_missile` | 256 (PC) / 128 (行動) | 64–512 | 閘門 | 玩家飛彈池容量 |
| `max_concurrent_bullets_pc` | 2000 | 1500–2500 | 效能 | PC 同屏敵彈硬上限 |
| `max_concurrent_bullets_mobile` | 1000 | 800–1200 | 效能 | 手機同屏敵彈硬上限 [需引擎階段驗證] |
| `density_hook_source` | difficulty-system | — | 閘門 | 密度乘數來源，禁止本系統另設 |
| `offscreen_cull_margin_pct` | 8% | 5–12% | 手感 | 離屏剔除邊界外擴比例 |
| `spatial_grid_cell_px` | — | — | 效能 | 廣相網格格寬 [需引擎階段驗證，原型調參] |
| `spatial_grid_max_per_cell` | 48 | 32–64 | 效能 | 每格候選彈上限 [需引擎階段驗證] |
| `telegraph_min_s` | 0.3 | 0.3–0.8 | 手感 | 發射電報最短時長；密度縮放不得低於此 |
| `readability_cap_priority` | true | — | 閘門 | 同屏上限優先於密度乘數；**不可關閉** |

> 巨獸各模式的 `bullet_count / spiral_arm_count / fire_interval / bullet_speed / spread_deg / color_id` 存於各 `EmitterPatternSO`，逐階密度以巨獸文件第 8 節表為準。

---

## 11. 驗收標準 (Acceptance Criteria)

### 11.1 效能：手機 sustain（效能 — 阻斷 Vertical Slice）[需引擎階段驗證]

- [ ] **手機基準機上，同屏 sustain 1,000 顆敵彈（含碰撞與繪製）維持穩定 60 fps ≥ 60 秒，無掉幀尖峰**（此為本專案第一技術風險的核心驗收數字；800–1,200 為目標帶，1,000 為承諾點）。**此數值於效能原型量測前僅為目標，須以基準機實測確認後才 LOCK。**
- [ ] PC 上同屏 sustain 2,000 顆敵彈維持穩定 60 fps ≥ 60 秒。
- [ ] 若手機未達 1,000：依 5.2 處置順序（降 D4 密度 → 收緊上限 → 最後才視覺），記錄於效能報告，**不得以犧牲可讀性換取**。

### 11.2 零 GC（功能性 — 阻斷）

- [ ] 一場完整戰鬥（含頭目最高密度階段），彈幕系統在 Unity Profiler 中 GC Alloc = 0 B/frame（穩態，排除載入期）。
- [ ] 自動化 / 剖析證據存於 `production/qa/evidence/bullet_gc_profile_[date].md`。

### 11.3 模式表達力（功能性 — 阻斷）

- [ ] 三頭目全部既有模式（CARAPEX A/B/C、LACERA A/B/C、VOLTWYRM A/B/C + Phase3 核心直射）均能以 `EmitterPatternSO` 撰寫，無需新增 shape 類型或程式碼。
- [ ] 設計師（非程式）能在 Inspector 中新增 / 調整一個模式並在遊戲中看到結果，無需工程介入。

### 11.4 密度縮放正確性（功能性 — 阻斷）

- [ ] D1–D4 下實際生成彈數 = `ceil(base × bullet_density_mult[tier])`（或巨獸文件逐階指定值），允許 ceil ±1。
- [ ] `bullet_speed` / `spread_deg` / `spiral_angular_speed` / 電報時長在 D1–D4 下量測完全恆定（difficulty-system.md H 系列一致）。
- [ ] 密度乘數要求超過同屏上限時，`readability_cap_priority` 生效截斷，可讀性不破。

### 11.5 碰撞正確性（功能性 — 阻斷）

- [ ] 玩家單點判定：敵彈僅在接觸該點時觸發受擊；擦身（外殼重疊但點未觸）不觸發。
- [ ] 玩家飛彈命中部位正確發 `on_missile_hit`（含 state_mult）；雷射發 `on_laser_hit` / `on_l3_wave_hit`；ARMORED 偏轉由部位系統回 `B_fill=0`（本系統不判斷後果）。
- [ ] L4 穿透單發對縱列多部位各發一次 `on_laser_hit`（VOLTWYRM 四頸段驗證）。
- [ ] 廣相在 1,000+ 敵彈下每幀碰撞成本落在 5.1 份額內 [需引擎階段驗證]。

### 11.6 可讀性（體驗性 — UX 阻斷）

- [ ] D4 最高密度靜態截圖，5 人測試辨識「敵彈 vs 玩家判定點」成功率 ≥ 70%（繼承 difficulty-system.md H.7 / kaiju 各文件可讀性驗收）。
- [ ] 敵彈全為暖色 + 高對比外框；玩家判定點恆亮冷色且永不被彈幕 / 特效遮蔽（最高繪製層）。
- [ ] 每模式發射電報在最高密度下仍可辨識（不被同屏彈幕完全遮蔽）。

---

*文件版本：1.0.0*
*作者：Technical Director Agent*
*狀態：Draft — 技術策略；後端（DOTS vs MonoBehaviour）與手機 sustain 數字待 `/architecture-decision` ADR 與效能原型 LOCK*
*關聯 GDD：game-concept.md | difficulty-system.md | weapon-system.md | kaiju-part-system.md | kaiju/01-carapex.md · 02-lacera.md · 03-voltwyrm.md*
