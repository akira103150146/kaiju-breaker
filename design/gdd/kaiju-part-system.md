# 可破壞部位系統 (Breakable Part System) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/kaiju-part-system.md*
*最後更新: 2026-07-01*
*狀態: Draft*
*相依概念文件: design/gdd/game-concept.md*
*呼叫端文件: design/gdd/weapon-system.md（LOCKED）*
*平行撰寫中: design/gdd/material-economy.md*

---

## A. 概覽 (Overview)

可破壞部位系統（Breakable Part System）是殲獸戰機核心狩獵循環的**接收端（Receiver）**。武器系統（`weapon-system.md`，Director-Approved，LOCKED）作為輸出端，透過雙軌核心機制（蓄熱軌 / 破甲軌）持續向本系統提交命中事件；本系統負責維護每個可破壞部位（Breakable Part）的獨立狀態機，並在條件達成時觸發部位破壞（Part Break）、永久移除部位判定，並通知素材經濟系統（`material-economy.md`，撰寫中）執行素材掉落。

每隻巨獸（Kaiju）通常擁有 **2–5 個**可破壞部位，以**相鄰圖（Adjacency Graph）**描述空間拓撲關係，供武器 Tier-3 機制（L2 破點漣漪、M3 穿甲爆破鏈）消費。後期高難度 Boss（如三頭目陣容最終頭目）允許最多 **8 個**部位，以支撐更複雜的子彈地獄戰鬥設計；高部位數僅限後期頭目，早中期頭目仍建議控制在 5 個以內確保設計可讀性。部位分三類：**普通部位（Normal Part）**、**強化部位（Armored Part）**、**Boss 核心部位（Boss Core Part）**。

**系統邊界**：本系統是雙軌機制的狀態管理者，不計算武器輸出量（由武器系統計算後傳入），不決定素材種類（由素材系統負責），不管理巨獸整體生命（只追蹤 Boss Core 是否 BROKEN）。

**難度不縮放**：本系統所有部位數值（HU/BU 容量、衰減率、閾值）在四個難度等級下維持恆定，嚴格落實「難度是門，不是牆」支柱。

---

## B. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：挑戰（Challenge）＋感官愉悅（Sensation）＋表達（Expression）

**「我正在拆解一個活著的機構」** — 巨獸不是一個整體的血條，而是由複數個彼此相依部位組成的有機系統。玩家感受到的是外科手術式的精準拆解：先以雷射把部位加熱進入**軟化（SOFTENED）**狀態（目標閃爍橙紅、脈動發光），再用飛彈填滿**破甲槽（Break Bar）**讓部位爆裂（BROKEN）。每一步都有明確的感官獎勵，直接服務「破壞即獎勵」支柱。

**「核心就是靶心，但我想要全部」** — Boss 核心部位（Boss Core）始終是唯一的致勝目標；選擇性部位是額外的素材來源與技巧挑戰。每場戰鬥都有一個靜默的微決策：「我可以只打核心結束，或者多花時間把那兩個強化部位也破了。值得嗎？」這是「頭目是靈魂」支柱的核心張力。

**「護甲不是牆，是門」** — 強化部位的弱點被護甲遮蔽；玩家必須以波動砲（L3）震盪硬直（Stagger）強制剝甲，才能在 2 秒窗口命中弱點。讀懂這個「開門-射入」時機，是區分熟練玩家與一般玩家的技術門檻，呼應「以智取勝」的核心幻想。

---

## C. 詳細規則 (Detailed Rules)

### C.1 部位實體定義 (Part Entity Definition)

每個可破壞部位（Breakable Part）是一個獨立遊戲實體，持有以下資料：

| 欄位 | 型別 | 初始值 | 說明 |
|------|------|--------|------|
| `part_id` | String | — | 在巨獸內唯一識別子（例：`"left_wing"`）|
| `kaiju_id` | String | — | 所屬巨獸實例識別子 |
| `part_type` | Enum | — | `NORMAL` / `ARMORED` / `BOSS_CORE` |
| `H_current` | float (HU) | 0 | 當前熱量值，clamp 到 \[0, H_max\] |
| `H_max` | float (HU) | 依類型 | 熱量容量；從全域旋鈕讀取，可被巨獸定義覆寫 |
| `B_current` | float (BU) | 0 | 當前破甲累積量，clamp 到 \[0, B_max\] |
| `B_max` | float (BU) | 依類型 | 破甲容量；從全域旋鈕讀取，可被巨獸定義覆寫 |
| `heat_state` | Enum | `INTACT` | `INTACT`（未軟化）/ `SOFTENED`（已軟化）|
| `armor_state` | Enum | `ARMOR_INTACT` | ARMORED 專用：`ARMOR_INTACT` / `ARMOR_STRIPPED`；其他部位此欄位恆為 N/A |
| `stagger_timer` | float (s) | 0 | 震盪硬直剩餘秒數；≤ 0 表示無效 |
| `break_state` | Enum | `ALIVE` | `ALIVE` / `BROKEN`（終態，不可逆）|
| `adjacency_list` | Array\[String\] | \[\] | 空間相鄰的部位 part_id 列表（由巨獸定義檔宣告）|
| `drop_table_id` | String | — | 破壞時查詢的掉落表 ID（供 material-economy 使用）|

**與 weapon-system.md 的狀態名稱對應**：weapon-system.md D.3 M_state_mult 查表中的 `NORMAL（未軟化）` 對應本系統的 `heat_state == INTACT`；`SOFTENED`、`STAGGERED`、`BROKEN` 命名一致。

**單位換算錨點（繼承自 weapon-system.md D.1）**：

```
1 D₀  ≙  10 BU/s  （對已軟化部位的破甲填充速率）
破甲值(BU) = 武器輸出(×D₀) × 10
```

HU（熱量單位）屬蓄熱軌，與 BU（破甲單位）各自獨立校準；D₀→BU 換算僅適用於破甲軌。

---

### C.2 部位狀態機 (Part State Machine)

每個部位的完整狀態由三個維度**組合**描述：
1. **熱量狀態（Heat State）**：`INTACT` ↔ `SOFTENED`（依 H_current 動態切換）
2. **護甲狀態（Armor State）**：`ARMOR_INTACT` ↔ `ARMOR_STRIPPED`（ARMORED 部位專用）
3. **破壞狀態（Break State）**：`ALIVE` → `BROKEN`（單向終態）

STAGGERED 是疊加在上述狀態之上的**時限覆蓋層**（`stagger_timer > 0`），影響 M_state_mult 查表結果（見 D.3）。

#### 熱量狀態轉換

```
每幀執行（break_state == BROKEN 時跳過）：

INTACT  → SOFTENED:  if H_current >= theta_S
                        heat_state = SOFTENED
                        emit on_part_softened(...)

SOFTENED → INTACT:   elif H_current < theta_S_exit
                        heat_state = INTACT
                        emit on_part_softened_exit(...)
```

- `theta_S`（全域旋鈕，預設 100 HU）：軟化入口閾值
- `theta_S_exit`（全域旋鈕，預設 80 HU）：軟化退出閾值
- 滯後帶 `[theta_S_exit, theta_S)` = \[80, 100\) HU：在此區間維持現有狀態，避免頻繁切換

#### 護甲狀態轉換（ARMORED 部位專用）

```
ARMOR_INTACT  → ARMOR_STRIPPED:  L3 蓄力震波命中時
                                    armor_state = ARMOR_STRIPPED
                                    stagger_timer = stagger_duration

ARMOR_STRIPPED → ARMOR_INTACT:   stagger_timer 倒計至 0
                                    armor_state = ARMOR_INTACT
```

- 除 L3 Wave Cannon 蓄力震波外，無任何路徑可觸發 ARMOR_STRIPPED
- BU 在 ARMOR_INTACT 期間**鎖定（飛彈偏轉，填充 = 0）**，ARMOR_STRIPPED 期間正常累積
- BU 在 stagger_timer 歸零後、護甲恢復時**保留不清零**（見 C.4）

#### BROKEN 狀態（終態）

```
ALIVE → BROKEN:  if B_current >= required_break_threshold_[type]
                     break_state = BROKEN
                     H_current = 0 ; B_current = 0
                     碰撞體從判定移除；切換已破壞動畫態
                     emit on_part_break(...)
                     if part_type == BOSS_CORE: emit on_boss_core_break(...)
```

BROKEN 後的任何命中事件：本系統立即 return，播放空白音效，無任何狀態更新。

#### 全部位狀態轉換圖摘要

```
所有部位：
  ALIVE ─────────────────────────────────── → BROKEN（終態）
  │
  ├─ 熱量狀態: INTACT ⇄ SOFTENED
  │              （依 H_current vs theta_S / theta_S_exit 動態切換）
  │
  └─ STAGGERED 疊加層（stagger_timer > 0 時生效）
        觸發：L3 蓄力震波命中
        到期：stagger_timer 歸零

ARMORED 部位額外層：
  ARMOR_INTACT ──[L3 震波]──→ ARMOR_STRIPPED
  ARMOR_STRIPPED ──[timer=0]──→ ARMOR_INTACT
```

---

### C.3 部位類型 (Part Types)

| 類型 | H_max（全域旋鈕） | B_max（全域旋鈕） | required_break_threshold | 弱點可見性 | 破壞結果 |
|------|--------------------|-------------------|--------------------------|------------|----------|
| **NORMAL（普通部位）** | `H_max_normal` = 100 HU | `B_max_normal` = 100 BU | `required_break_threshold_normal` = 100 BU | 永遠可見 | 素材掉落；碰撞體移除 |
| **ARMORED（強化部位）** | `H_max_armored` = 150 HU | `B_max_armored` = 150 BU | `required_break_threshold_armored` = 150 BU | 弱點隱藏（ARMOR_INTACT 期間判定框不可命中）；L3 震波剝甲後露出 | 高階素材掉落；碰撞體移除 |
| **BOSS_CORE（核心部位）** | `H_max_boss_core` = 200 HU | `B_max_boss_core` = 200 BU | `required_break_threshold_boss_core` = 200 BU | 永遠可見（明顯標記，視覺優先） | **觸發勝利條件**（`on_boss_core_break`）；素材掉落 |

**TTB（Time-To-Break）設計目標（繼承自 weapon-system.md D.4）**：

| 部位類型 | TTB 目標 | 備注 |
|----------|----------|------|
| NORMAL | 15–25 秒 | 主要戰鬥節拍 |
| ARMORED | 30–45 秒（跨多輪震盪窗口） | 需要 L3 配合；TTB 含等待 stagger 窗口的時間 |
| BOSS_CORE | 50–80 秒 | 多階段高潮；核心狩獵張力 |

**可選擇性原則**：NORMAL 與 ARMORED 部位不是勝利必要條件。玩家可選擇只打 BOSS_CORE 結束戰鬥，或多花時間破光全部位換取高階素材。兩條策略都合法——這是「頭目是靈魂」支柱的靈魂張力。

**部位覆寫（Per-Kaiju Override）**：巨獸定義檔可為任何部位設置 `H_max_override` / `B_max_override`，覆寫全域旋鈕預設值，以塑造個別巨獸的獨特節奏。覆寫值不受難度縮放（見 C.8）。

---

### C.4 強化部位護甲機制 (Armored Part Mechanics)

強化部位的護甲層是一個**進入閘門（Access Gate）**，而非數值縮放。它控制飛彈是否能到達破甲槽，而非使 BU 容量更大。

| 護甲狀態 | 雷射命中效果（HU） | 飛彈命中效果（BU） |
|----------|------------------|--------------------|
| `ARMOR_INTACT` | 正常積累熱量（H_current 上升）| 彈頭偏轉，BU 增加量 = 0（M_state_mult = 0）|
| `ARMOR_STRIPPED` | 正常積累熱量 | 正常套用 M_state_mult（× `stagger_break_mult` = 1.5）|

**核心設計點**：
- 雷射**不受護甲阻擋**：玩家可在等待 L3 冷卻時預先蓄熱，讓部位進入 SOFTENED
- 理想序列：①以雷射使部位達 SOFTENED（H_current ≥ theta_S）→ ②L3 震波剝甲（ARMOR_STRIPPED + STAGGERED）→ ③在 2 秒窗口內以飛彈填充 BU（at × 1.5）
- **BU 跨窗口保留**：每次 stagger 窗口結束後護甲恢復，但已累積的 BU 不清零。第二輪、第三輪 stagger 繼續在已有基礎上積累，直至 B_current ≥ required_break_threshold_armored

---

### C.5 事件契約 (Event Contract)

本系統作為**接收端（Receiver）**消費武器系統命中事件，同時作為**發射端（Emitter）**向下游系統推送狀態變更。

> **注意**：weapon-system.md F.1 在「武器系統輸出」清單中列出了 `on_part_break(part_id)`，此為描述側重點不同——`on_part_break` 應由**本系統**在破壞條件達成後發出，武器系統作為接收方執行碰撞體清除動作。

#### 接收事件（由武器系統發出，本系統消費）

```
on_laser_hit(
  part_id:    String,   // 被命中的部位 ID
  kaiju_id:   String,   // 所屬巨獸實例 ID
  heat_delta: float     // 本幀新增熱量（HU）；= H_rate_weapon × Δt；必須 > 0
)
```

```
on_missile_hit(
  part_id:          String,   // 被命中的部位 ID
  kaiju_id:         String,   // 所屬巨獸實例 ID
  break_delta_base: float,    // 基礎破甲量（BU）；= 武器輸出(×D₀) × 10；未套用 M_state_mult
  weapon_id:        String    // 武器識別子，供 Tier-3 鏈式效果判斷
)
```

```
on_l3_wave_hit(
  part_id:  String,   // 被 L3 蓄力震波命中的部位 ID
  kaiju_id: String
)
// 本事件觸發 STAGGERED 疊加層，並對 ARMORED 部位設置 ARMOR_STRIPPED
// 獨立於 on_missile_hit，因震波效果和破甲是不同的結算步驟
```

#### 發出事件（本系統發出，下游系統消費）

```
on_part_softened(
  part_id:      String,
  kaiju_id:     String,
  current_heat: float,  // 觸發瞬間的 H_current（供 VFX 比例縮放）
  H_max:        float   // 部位 H_max（供 VFX 比例縮放）
)
// 消費者：VFX/SFX 系統（啟動橙紅脈動光暈）；武器系統（更新 M_state_mult 快取）
```

```
on_part_softened_exit(
  part_id:  String,
  kaiju_id: String
)
// 消費者：VFX/SFX 系統（移除軟化視覺效果）；武器系統（更新 M_state_mult 快取）
```

```
on_part_staggered(
  part_id:        String,
  kaiju_id:       String,
  duration:       float,  // = stagger_duration（秒）
  armor_stripped: bool    // ARMORED 部位護甲是否被剝除（供 VFX 顯示弱點框）
)
// 消費者：VFX/SFX 系統；武器系統（更新 M_state_mult）；M1 追蹤飛彈鎖定更新
```

```
on_part_stagger_end(
  part_id:        String,
  kaiju_id:       String,
  armor_restored: bool    // ARMORED 部位護甲是否恢復
)
// 消費者：VFX/SFX 系統；武器系統（更新 M_state_mult）
```

```
on_part_break(
  part_id:        String,
  kaiju_id:       String,
  part_type:      PartType,       // NORMAL | ARMORED | BOSS_CORE
  world_position: Vector2,        // 部位世界座標（素材掉落生成點）
  drop_table_id:  String,         // 素材掉落表 ID（供 material-economy 直接使用）
  break_quality:  BreakQuality,   // 破壞當下品質（供 material-economy 決定素材產出倍率）
  adjacency_list: Array[String],  // 相鄰存活部位 ID（供 Tier-3 鏈式效果使用）
  is_chain_break: bool            // 是否由 M3 Tier-3 連鎖觸發（false = 玩家直接摧毀）
)
// break_quality 於破壞成立那一幀依部位狀態計算（enum BreakQuality）：
//   SOFTENED_STAGGERED  if heat_state == SOFTENED and stagger_timer > 0
//   SOFTENED            if heat_state == SOFTENED
//   NORMAL              otherwise
// 對應 material-economy.md 產出倍率：NORMAL=base / SOFTENED=1.5× / SOFTENED_STAGGERED=2×＋雙倍核心
// 消費者：
//   - material-economy（觸發素材掉落，使用 drop_table_id、break_quality 和 world_position）
//   - VFX/SFX 系統（破壞爆炸效果）
//   - 武器系統（移除碰撞體；L2 Tier-3 漣漪邏輯由武器系統在此事件後觸發）
//   - 本系統自身（M3 Tier-3 連鎖傷害在 is_chain_break==false 時在同幀執行）
```

```
on_boss_core_break(
  kaiju_id:       String,
  world_position: Vector2
)
// 在 on_part_break（part_type == BOSS_CORE）確認發出後，同幀緊接發出此事件
// 消費者：遊戲狀態系統（Game State System）→ 觸發勝利結算序列
```

---

### C.6 相鄰圖與巨獸定義 (Adjacency Graph & Kaiju Definition)

#### 資料模型

相鄰關係以**有向宣告、雙向推導**的方式存入巨獸定義檔（Kaiju Definition File）。系統啟動載入時，本系統從定義檔建立無向圖（Undirected Graph）並快取為 `Dict[String, Array[String]]`（HashMap of adjacency lists），供執行期 O(1) 鄰居查詢。

**巨獸定義檔格式（YAML，`assets/data/kaiju/[kaiju_id].yaml`）**：

```yaml
kaiju_id: "kaiju_dragon_alpha"
parts:
  - id: "head"
    type: BOSS_CORE             # NORMAL | ARMORED | BOSS_CORE
    H_max_override: null        # null = 使用全域旋鈕預設值；設整數值則覆寫
    B_max_override: null
    adjacency: ["neck", "left_horn", "right_horn"]
    drop_table_id: "drop_boss_core_tier1"

  - id: "neck"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["head", "left_shoulder", "right_shoulder"]
    drop_table_id: "drop_normal_tier1"

  - id: "left_horn"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["head"]
    drop_table_id: "drop_armored_tier1"

  - id: "right_horn"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["head"]
    drop_table_id: "drop_armored_tier1"
```

**圖建立規則**：
1. 遍歷所有 parts 的 adjacency 陣列
2. 每條宣告邊 (A → B) 同時在圖中加入 (A, B) 和 (B, A)（確保雙向）
3. 重複邊（同一對部位宣告兩次）自動去重
4. 單一部位最多宣告 `adjacency_max_neighbors`（預設 4）個相鄰部位（防止鏈式效果過度擴散）

#### 相鄰圖與 Tier-3 效果的消費關係

| 武器 Tier-3 效果 | 消費相鄰資料的系統 | 觸發時機 | 效果 |
|-----------------|-------------------|----------|------|
| L2「破點漣漪（Heat Ripple）」| 武器系統（收到 `on_part_break` 後） | 部位破壞瞬間 | 所有存活鄰居各得 +`l2_t3_adjacent_heat_pct` × 鄰居 H_max HU |
| M3「穿甲爆破鏈（AP Chain）」| 本系統（在 `on_part_break` 同幀，`is_chain_break==false`）| 部位破壞瞬間 | 最多 2 個存活鄰居各受 `m3_t3_chain_dmg_mult × D₀` 破甲（套用 M_state_mult）|
| L1「殘熱焰（Residual Flame）」| 武器系統（作用於被命中部位本身）| 命中後 1.5s | 持續填充被命中部位的 HU；不消費相鄰資料 |

> L1 殘熱焰不使用相鄰圖，但需要 part_id 查詢 H_current 和 H_max，仍需本系統提供部位狀態讀取介面。

---

### C.7 破壞即獎勵 (Break = Reward)

部位破壞**立即觸發** `on_part_break` 事件（見 C.5），由 material-economy 系統依 `drop_table_id` 決定素材種類與數量，並在 `world_position` 生成可拾取素材實體。

**核心設計約束**：
- 素材掉落 **100% 綁定被破壞的部位**（非隨機寶箱），直接服務「破壞即獎勵」支柱
- 部位破壞後**永不再生**（`part_regen_enabled = false`，不可運行期修改）
- 破壞狀態是**對局內永久**的：在本輪戰鬥中被破壞的部位維持 BROKEN，不重生；新一輪戰鬥重新載入巨獸定義時初始化為 ALIVE
- 具體素材稀有度曲線由 `material-economy.md` 定義（本系統只保證傳入正確的 `drop_table_id`）

---

### C.8 難度不縮放規則 (Difficulty Invariance)

本系統所有部位數值在難度 1–4 下維持恆定：

| 恆定數值類別 | 涉及旋鈕 |
|-------------|---------|
| 熱量容量 | `H_max_normal`, `H_max_armored`, `H_max_boss_core`（含覆寫值）|
| 熱量衰減率 | `H_decay_rate` |
| 軟化閾值 | `theta_S`, `theta_S_exit` |
| 破甲容量 | `B_max_normal`, `B_max_armored`, `B_max_boss_core`（含覆寫值）|
| 飛彈效率乘數 | `B_unsoftened_mult`, `stagger_break_mult` |
| 破壞閾值 | `required_break_threshold_normal/armored/boss_core` |
| 震盪硬直時間 | `stagger_duration` |

**難度縮放只作用於敵彈密度**（由難度系統控制）。其隱性效果：高難度下玩家閃避頻率上升 → 雷射命中率（有效蓄熱 uptime）下降 → 實際 TTB 自然延長。這是合法的隱性難度調節，不需要本系統做任何參數改變，也不違背「難度是門，不是牆」支柱。

---

## D. 公式 (Formulas)

### D.1 熱量槽更新公式 (Heat Bar Update)

**命名表達式**：

```
H(t) = clamp( H(t-1) + H_fill(t) − H_decay(t),  0,  H_max )

H_fill(t)  = heat_delta        （本幀收到 on_laser_hit 事件時）
           = 0                 （本幀無 on_laser_hit 事件時）

H_decay(t) = H_decay_rate × Δt （本幀無 on_laser_hit 事件時）
           = 0                 （本幀有 on_laser_hit 事件時）
```

雷射命中時衰減暫停（兩者互斥），與 weapon-system.md D.2 一致。部位 `break_state == BROKEN` 時不執行此更新。

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `H(t)` | float | \[0, H_max\] HU | 本幀更新後的熱量值 |
| `H(t-1)` | float | \[0, H_max\] HU | 上一幀的熱量值 |
| `heat_delta` | float | (0, ∞) HU | `on_laser_hit` 攜帶的熱量注入量；= H_rate_weapon × Δt |
| `H_decay_rate` | float | \[1, 8\] HU/s | 無雷射命中時的冷卻速率（全域旋鈕，預設 3 HU/s）|
| `Δt` | float | (0, 1/30\] s | 幀時長 |
| `H_max` | float | \[80, 280\] HU | 部位熱量容量上限（依類型與覆寫決定）|

**輸出範圍**：強制 clamp 至 \[0, H_max\]，不允許溢出或負值。

**運算範例**（L2 集束雷射，H_max = 100 HU，H_decay_rate = 3 HU/s，Δt = 0.016s）：
```
H_fill(t)  = 37.5 × 0.016 = 0.6 HU
H_decay(t) = 0（雷射命中，衰減暫停）
H(t) = clamp(50.0 + 0.6 − 0, 0, 100) = 50.6 HU
```

---

### D.2 軟化狀態判定公式 (Softened State Evaluation)

每幀在 D.1 更新後執行：

**命名表達式**（偽代碼）：

```
if break_state == BROKEN: return

if heat_state == INTACT and H(t) >= theta_S:
    heat_state ← SOFTENED
    emit on_part_softened(part_id, kaiju_id, H(t), H_max)

elif heat_state == SOFTENED and H(t) < theta_S_exit:
    heat_state ← INTACT
    emit on_part_softened_exit(part_id, kaiju_id)
```

**理論軟化時間**（繼承自 weapon-system.md D.2）：

```
T_soften = theta_S / max( H_rate_weapon − H_decay_rate,  ε )
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `theta_S` | float | \[80, 120\] HU | 軟化入口閾值（全域旋鈕，預設 100 HU）|
| `theta_S_exit` | float | \[60, 90\] HU | 軟化退出閾值（全域旋鈕，預設 80 HU；必須 < theta_S）|
| `H_rate_weapon` | float | \[8, 50\] HU/s | 武器在最優命中條件下的熱量速率（由武器系統計算後透過 on_laser_hit 傳入）|
| `ε` | float | 0.01 | 防除零保護 |
| `T_soften` | float | \[0, ∞) s | 從 0 HU 到達 theta_S 的理論最短時間 |

**輸出範圍**：T_soften ≥ 0；若 H_rate_weapon ≤ H_decay_rate + ε，部位理論上永不軟化（設計時應避免此情況）。

**運算範例**（L2 集束雷射，theta_S = 100 HU，H_decay_rate = 3 HU/s）：
```
T_soften = 100 / max(37.5 − 3, 0.01) = 100 / 34.5 ≈ 2.9 秒（理論最短；實戰約 5–8 秒）
```

---

### D.3 破甲槽更新公式 (Break Bar Update)

每次 `on_missile_hit` 事件觸發時執行：

**命名表達式**：

```
M_state_mult = lookup_state_mult(part)    （見下方查表）
B_fill       = break_delta_base × M_state_mult
B(new)       = clamp( B_current + B_fill,  0,  B_max )

if B(new) >= required_break_threshold_[type]:
    trigger_part_break(part)              （設 break_state = BROKEN，發出事件）
else:
    B_current ← B(new)
```

**M_state_mult 完整查表**（繼承並擴展自 weapon-system.md D.3，加入 ARMORED 護甲狀態）：

| 部位類型 | 熱量狀態 | 護甲狀態 | M_state_mult | 說明 |
|----------|----------|----------|-------------|------|
| NORMAL / BOSS_CORE | INTACT | N/A | `B_unsoftened_mult`（0.35） | 未軟化低效路徑 |
| NORMAL / BOSS_CORE | SOFTENED | N/A | 1.0 | 軟化後全效率 |
| NORMAL / BOSS_CORE | 任意 | N/A（stagger_timer > 0） | `stagger_break_mult`（1.5） | 震盪窗口最高效率 |
| NORMAL / BOSS_CORE | SOFTENED | N/A（stagger_timer > 0） | `stagger_break_mult`（1.5） | SOFTENED + STAGGERED 不疊乘 |
| ARMORED | 任意 | ARMOR_INTACT | **0**（彈頭偏轉） | 護甲完整時飛彈無效 |
| ARMORED | 任意 | ARMOR_STRIPPED（stagger_timer > 0） | `stagger_break_mult`（1.5） | 剝甲窗口，含 SOFTENED 時仍為 1.5 |

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `break_delta_base` | float | (0, ∞) BU | `on_missile_hit` 攜帶的基礎破甲量；= 武器輸出(×D₀) × 10 |
| `M_state_mult` | float | \{0, 0.35, 1.0, 1.5\} | 狀態乘數；查表決定 |
| `B_fill` | float | \[0, ∞) BU | 本次命中實際填充量 |
| `B_max` | float | \[80, 280\] BU | 部位破甲容量（依類型與覆寫）|
| `B_unsoftened_mult` | float | \[0.20, 0.50\] | 未軟化飛彈效率乘數（全域旋鈕，預設 0.35）|
| `stagger_break_mult` | float | \[1.2, 2.0\] | 震盪硬直破甲效率乘數（全域旋鈕，預設 1.5）|
| `required_break_threshold_*` | float | \[80, 280\] BU | 破壞判定閾值；預設等於對應 B_max |

**輸出範圍**：B_current clamp 至 \[0, B_max\]，達到 required_break_threshold 後觸發 BROKEN 並重置為 0。

**運算範例**（M3 穿甲魚雷，SOFTENED 普通部位，熱衝擊引爆）：
```
break_delta_base = m3_heat_shock_fill_mult × m3_dmg_unsoftened_mult × D₀ × 10
                 = 2.0 × 3.0 × 1 × 10 = 60 BU

M_state_mult     = 1.0（SOFTENED，NORMAL 部位）
B_fill           = 60 × 1.0 = 60 BU
B(new)           = clamp(40 + 60, 0, 100) = 100 BU  → 觸發 PART_BREAK
```

---

### D.4 震盪硬直計時器公式 (Stagger Timer)

**命名表達式（每幀更新）**：

```
if stagger_timer > 0 and break_state != BROKEN:
    stagger_timer ← max( stagger_timer − Δt,  0 )

    if stagger_timer == 0:
        if part_type == ARMORED:
            armor_state ← ARMOR_INTACT
        emit on_part_stagger_end(part_id, kaiju_id, armor_restored=(part_type==ARMORED))
```

**震盪硬直觸發（on_l3_wave_hit 事件處理）**：

```
on_l3_wave_hit(part_id, kaiju_id):
    if break_state == BROKEN: return

    stagger_timer ← stagger_duration        （重疊觸發時重置計時器，不疊加）
    if part_type == ARMORED:
        armor_state ← ARMOR_STRIPPED

    emit on_part_staggered(part_id, kaiju_id, stagger_duration,
                           armor_stripped=(part_type == ARMORED))
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `stagger_timer` | float | \[0, stagger_duration\] s | 當前硬直剩餘秒數 |
| `stagger_duration` | float | \[1.5, 3.0\] s | 硬直持續時間（全域旋鈕，預設 2.0s；= `l3_stagger_window` in weapon-system.md G.2）|

**輸出範圍**：clamp 至 0，不允許負值。重疊觸發重置到 stagger_duration（不疊加）。

**運算範例**（stagger_duration = 2.0s，Δt = 0.016s）：
```
幀 1: stagger_timer = 2.000 → 2.000 − 0.016 = 1.984s
...
幀 125: stagger_timer ≈ 0.000 → 發出 on_part_stagger_end
```

---

### D.5 相鄰熱量脈衝公式 (Adjacency Heat Pulse)

觸發條件：部位 P 破壞（`on_part_break` 發出，`is_chain_break == false`），武器系統確認攻擊者持有 L2 Tier-3 升級後執行此計算。

**命名表達式**：

```
for each adj_id in P.adjacency_list:
    adj ← get_part(adj_id)
    if adj.break_state == BROKEN: continue

    heat_pulse   = adj.H_max × l2_t3_adjacent_heat_pct
    adj.H_current ← clamp( adj.H_current + heat_pulse,  0,  adj.H_max )
    re_evaluate_softened_state(adj)   （可能立即觸發 on_part_softened）
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `l2_t3_adjacent_heat_pct` | float | \[0.20, 0.50\] | 熱量脈衝比例（% of 鄰居 H_max）；全域旋鈕，預設 30% |
| `adj.H_max` | float | \[80, 280\] HU | 各鄰居部位自身的熱量容量（可能不同）|
| `heat_pulse` | float | \[16, 140\] HU | 實際注入熱量；= adj.H_max × l2_t3_adjacent_heat_pct |

**輸出範圍**：adj.H_current clamp 至 \[0, adj.H_max\]；可能同幀觸發 SOFTENED 轉換。

**運算範例**（P 破壞，鄰居 Q = ARMORED，H_max = 150 HU，H_current = 60 HU）：
```
heat_pulse    = 150 × 0.30 = 45 HU
Q.H_current   = clamp(60 + 45, 0, 150) = 105 HU
105 >= theta_S (100) → 觸發 on_part_softened(Q)  ← 同幀即時觸發
```

---

### D.6 相鄰鏈式破甲傷害公式 (Adjacency Chain Break Damage)

觸發條件：部位 P 破壞（`on_part_break` 發出，`is_chain_break == false`），武器系統確認攻擊者持有 M3 Tier-3 升級，本系統同幀執行鏈式計算。

**命名表達式**：

```
chain_damage_base = m3_t3_chain_dmg_mult × D₀ × 10    （= 1.5 × 10 = 15 BU at default）

eligible = [adj for adj in P.adjacency_list
            if adj.break_state != BROKEN]
targets  = eligible[:m3_t3_chain_max_targets]           （最多取前 2 個）

for each target in targets:
    mult       = lookup_state_mult(target)              （套用正常 M_state_mult 規則）
    B_chain    = chain_damage_base × mult
    target.B_current ← clamp( target.B_current + B_chain,  0,  target.B_max )

    if target.B_current >= required_break_threshold_for(target):
        trigger_part_break(target, is_chain_break=true)  （不再傳遞鏈式效果）
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `m3_t3_chain_dmg_mult` | float | \[1.0, 2.0\] ×D₀ | 每個鏈式目標的基礎破甲值（全域旋鈕，預設 1.5 ×D₀）|
| `chain_damage_base` | float | \[10, 20\] BU | 換算後破甲量；= m3_t3_chain_dmg_mult × 10 |
| `m3_t3_chain_max_targets` | int | \{1, 2\} | 連鎖最多目標數（全域旋鈕，預設 2）|
| `mult` | float | \{0, 0.35, 1.0, 1.5\} | 目標當前 M_state_mult（同 D.3 查表）|
| `B_chain` | float | \[0, 30\] BU | 鏈式傷害實際填充量 |

**輸出範圍**：target.B_current clamp 至 \[0, B_max\]。鏈式破壞使用 `is_chain_break=true` 旗標，不再觸發下一層鏈式效果（非遞迴）。

**運算範例**（P 破壞，相鄰 A = SOFTENED NORMAL，B_current = 90 BU；相鄰 B = ARMORED，ARMOR_INTACT）：
```
targets = [A, B]（取前 2 個存活鄰居）

target A: mult = 1.0（SOFTENED, NORMAL）
  B_chain = 15 × 1.0 = 15 BU
  A.B_current = clamp(90 + 15, 0, 100) = 100 BU → trigger_part_break(A, is_chain_break=true)

target B: mult = 0（ARMORED, ARMOR_INTACT）
  B_chain = 15 × 0 = 0 BU （護甲偏轉連鎖傷害）
  B.B_current 不變
```

---

## E. 邊界情況 (Edge Cases)

### E.1 SOFTENED + STAGGERED 同時存在（普通 / Boss Core 部位）

**情況**：部位已進入 SOFTENED（H_current ≥ theta_S），隨後被 L3 震波命中進入 STAGGERED（stagger_timer > 0）。

**處理**：M_state_mult = `stagger_break_mult`（1.5），**不與 SOFTENED 的 1.0 疊乘**（weapon-system.md D.3 M_state_mult 查表明確規定：SOFTENED + STAGGERED = 1.5）。SOFTENED 熱量狀態繼續保持（H_current 仍高於 theta_S_exit）。

**設計意圖**：防止組合效果意外創造出 ×1.5 疊加更高值。STAGGERED 的 ×1.5 是稀少的爆發窗口，不是乘算獎勵。

---

### E.2 ARMORED 部位跨震盪窗口累積 BU

**情況**：第一次 stagger 窗口（2 秒）結束，B_current = 40 BU，未達破壞閾值（150 BU）；護甲恢復（ARMOR_INTACT）。

**處理**：B_current **保留不清零**。護甲恢復後新命中飛彈的 M_state_mult = 0（偏轉），BU 停止增長。第二次 stagger 時在 40 BU 基礎上繼續累積。這是刻意設計——多輪震盪窗口的累積效果是攻克強化部位的核心策略。

---

### E.3 L2 Tier-3 熱量脈衝導致鄰居同幀 SOFTENED

**情況**：D.5 的熱量脈衝在同幀使鄰居 Q 的 H_current 達到 theta_S。

**處理**：在 `re_evaluate_softened_state(Q)` 中立即切換 Q 的 heat_state 並發出 `on_part_softened(Q)`，**不延遲到下幀**。VFX/SFX 系統和武器系統在同幀更新 Q 的狀態。這使得「破一個部位連帶讓鄰居軟化」的視覺反饋即時可見。

---

### E.4 M3 Tier-3 連鎖破壞的遞迴問題

**情況**：M3 鏈式效果使鄰居 A 觸發 BROKEN；A 的 `on_part_break` 是否再次觸發 M3 鏈式到 A 的鄰居？

**處理**：**不遞迴**。`trigger_part_break(A, is_chain_break=true)` 標記的事件，M3 Tier-3 邏輯檢查 `is_chain_break == false` 後跳過連鎖計算。`on_part_break` 事件的 `is_chain_break` 欄位向下傳遞，確保任何層級的鏈式破壞都不再傳播連鎖效果。

---

### E.5 熱量在 STAGGERED 期間持續衰減

**情況**：L3 震波後玩家停止射擊，2 秒內熱量衰減 = H_decay_rate × stagger_duration = 3 × 2 = 6 HU。

**處理**：熱量衰減正常進行。若觸發 STAGGERED 時 H_current = 100 HU（SOFTENED），2 秒後 H_current ≈ 94 HU — 仍高於 theta_S_exit（80 HU），SOFTENED 狀態維持。這是設計允許的：玩家在震盪窗口內仍可享有 SOFTENED 狀態（但需持續補充蓄熱以防冷卻至 theta_S_exit 以下）。若玩家完全停火超過約 6.7 秒（(100-80)/3）後才觸發 L3，部位回到 INTACT；STAGGERED 的 ×1.5 仍有效，但對 NORMAL/BOSS_CORE 部位無影響（STAGGERED 已覆蓋 SOFTENED 的 ×1.0）。

---

### E.6 Boss Core 破壞的事件順序

**情況**：Boss Core B_current 達到 required_break_threshold_boss_core，同幀需要觸發素材掉落和勝利結算。

**處理**：固定事件發出順序：
1. `on_part_break`（part_type = BOSS_CORE） → material-economy 登記素材掉落
2. `on_boss_core_break` → 遊戲狀態系統啟動勝利結算序列

遊戲狀態系統的結算動畫在 `on_boss_core_break` 收到後才啟動，確保 material-economy 有機會在結算前完成掉落登記。具體掉落實體生成時序由 material-economy.md 定義，不在本系統職責範圍。

---

### E.7 已破壞部位被命中

**情況**：飛彈或雷射命中 break_state == BROKEN 的部位（碰撞體清除存在 1 幀延遲的情況）。

**處理**：本系統在 `on_laser_hit` / `on_missile_hit` 的第一步檢查 `break_state == BROKEN`，若成立立即 return；播放空白命中音效（穿透效果），無 HU/BU 變更，無任何事件發出。碰撞體的完整移除由武器系統在收到 `on_part_break` 後的下一幀完成。

---

### E.8 同幀多次命中同一部位

**情況**：M2 蜂群飛彈的 8 枚微型飛彈在同幀全數命中同一部位。

**處理**：每枚飛彈產生一次獨立的 `on_missile_hit` 事件，按接收順序依序執行 D.3 公式（非批次處理）。若前幾枚已觸發 PART_BREAK，後續事件被 E.7 保護（break_state == BROKEN 立即 return）。B_current 不會因同幀多枚超出 B_max——clamp 保護每次更新。

---

### E.9 L3 Tier-3「共鳴擴散」注入熱量的狀態切換

**情況**：L3 Tier-3 升級在蓄力震波命中時向部位注入 `l3_t3_heat_inject_pct × H_max` HU（weapon-system.md G.2 = 50%）。此熱量注入是否由本系統處理？

**處理**：L3 Tier-3 熱量注入由武器系統計算後包入 `on_laser_hit` 事件（heat_delta = l3_t3_heat_inject_pct × H_max）發送給本系統，本系統以 D.1 公式正常處理。若注入後 H_current ≥ theta_S，D.2 立即切換 SOFTENED 並發出事件。此設計維持本系統的接口一致性：所有熱量來源都經由 `on_laser_hit` 事件傳入。

---

## F. 系統相依 (Dependencies)

### F.1 武器系統（Weapon System）——輸入來源（必要）

*相依文件*：`design/gdd/weapon-system.md`（LOCKED）

- **本系統消費**：`on_laser_hit`、`on_missile_hit`、`on_l3_wave_hit`（見 C.5）
- **武器系統消費**：`on_part_softened`、`on_part_softened_exit`、`on_part_staggered`、`on_part_stagger_end`、`on_part_break`（用於 M_state_mult 快取更新、L2 Tier-3 漣漪觸發、碰撞體清除）
- **武器系統查詢本系統**：部位的 `heat_state`、`armor_state`、`stagger_timer`、`H_current`、`H_max`（供 UI、M1 追蹤鎖定、L2 Tier-3 觸發條件）
- **護甲剝甲唯一路徑**：L3 Wave Cannon 蓄力震波——ARMORED 部位的 ARMOR_STRIPPED 狀態只能由此觸發，無其他路徑（見 H.3）

### F.2 素材經濟系統（Material Economy System）——掉落接收端（必要）

*相依文件*：`design/gdd/material-economy.md`（**撰寫中——此為跨系統相依，需平行對齊**）

- **本系統發出**：`on_part_break`（攜帶 `drop_table_id`、`part_type`、`world_position`）
- **素材系統的責任**：依 `drop_table_id` 查詢掉落表、決定素材種類與數量、在 `world_position` 生成可拾取實體
- **契約邊界**：本系統不知道具體素材種類與稀有度曲線；只保證在正確時機傳入有效的 `drop_table_id`
- **待對齊事項**：drop_table_id 的命名規範與 material-economy.md 的掉落表結構需在該文件完稿後對齊

### F.3 視覺/音效回饋系統（VFX / SFX System）——感知層（阻斷條件）

- **本系統發出**：`on_part_softened`、`on_part_softened_exit`、`on_part_staggered`、`on_part_stagger_end`、`on_part_break`
- **VFX 系統的責任**：SOFTENED 狀態的橙紅色偏移 + 脈動光暈 + 可選圖示；ARMOR_STRIPPED 的弱點框顯示；破壞爆炸效果
- **阻斷要求（繼承 weapon-system.md H.5）**：SOFTENED 視覺提示必須在 `on_part_softened` 發出後 **0.5 秒內**可被玩家感知（見 H.2）

### F.4 遊戲狀態系統（Game State System）——勝利結算

- **本系統發出**：`on_boss_core_break`
- 遊戲狀態系統收到後啟動勝利結算序列（動畫、素材結算、返回強化介面）
- 本系統在確認 `on_boss_core_break` 被接收後可選擇性凍結所有部位的幀更新

### F.5 巨獸設計（Kaiju Design）——靜態資料來源

- 所有部位定義（`H_max_override`、`B_max_override`、`part_type`、`adjacency`、`drop_table_id`）由設計師撰寫在 `assets/data/kaiju/[kaiju_id].yaml`
- 本系統在載入時解析此定義、建立相鄰圖、初始化所有部位實體
- **關卡設計約束（繼承自 weapon-system.md F.5）**：
  - 每個 Boss 至少需含 ≥ 2 個垂直對齊部位（供 L4 穿透雷射最優情境，見 weapon-system.md H.6）
  - 每個 Boss 至少需含 1 個 ARMORED 部位（使 L3 Wave Cannon 護甲剝甲能力有有意義的情境）
  - Boss 頂部建議配置密集部位叢集（供 M4 叢集炸彈最優情境）

### F.6 難度系統（Difficulty System）

本系統不接受任何來自難度系統的縮放指令。難度系統只控制敵彈密度；本系統所有旋鈕在運行期靜態讀取（見 C.8）。

---

## G. 調校旋鈕 (Tuning Knobs)

**所有數值存放於外部資料檔，禁止硬編碼。**  
全域旋鈕路徑：`assets/data/balance/weapon-system.yaml`（與 weapon-system.md 共用）  
部位系統專屬旋鈕路徑：`assets/data/balance/part-system.yaml`

### G.1 全域部位旋鈕（引用自 weapon-system.md G.1，本系統為消費端）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 本系統消費方式 |
|----------|--------|----------|------|---------------|
| `H_max_normal` | 100 HU | 80–150 | 曲線 | NORMAL 部位熱量容量上限（D.1 的 H_max）|
| `H_max_armored` | 150 HU | 120–200 | 閘門 | ARMORED 部位熱量容量上限 |
| `H_max_boss_core` | 200 HU | 160–280 | 閘門 | BOSS_CORE 部位熱量容量上限 |
| `H_decay_rate` | 3 HU/s | 1–8 | 手感 | 無雷射命中時的熱量冷卻速率（D.1）|
| `theta_S` | 100 HU | 80–120 | 閘門 | 軟化入口閾值（D.2）；建議 = H_max_normal |
| `theta_S_exit` | 80 HU | 60–90 | 手感 | 軟化退出閾值（D.2）；必須 < theta_S |
| `B_max_normal` | 100 BU | 80–150 | 曲線 | NORMAL 部位破甲容量上限（D.3）|
| `B_max_armored` | 150 BU | 120–200 | 閘門 | ARMORED 部位破甲容量上限 |
| `B_max_boss_core` | 200 BU | 160–280 | 閘門 | BOSS_CORE 部位破甲容量上限 |
| `B_unsoftened_mult` | 0.35 | 0.20–0.50 | 閘門 | 未軟化飛彈效率乘數（D.3 查表）|
| `required_break_threshold_normal` | 100 BU | 80–150 | 閘門 | NORMAL 部位破壞判定閾值（D.3）；預設 = B_max_normal |
| `required_break_threshold_armored` | 150 BU | 120–200 | 閘門 | ARMORED 部位破壞判定閾值 |
| `required_break_threshold_boss_core` | 200 BU | 160–280 | 閘門 | BOSS_CORE 部位破壞判定閾值 |
| `stagger_duration` | 2.0s | 1.5–3.0 | 手感 | STAGGERED / ARMOR_STRIPPED 持續時間（D.4）；= `l3_stagger_window` in weapon-system.md G.2，兩值必須保持同步 |
| `stagger_break_mult` | 1.5 | 1.2–2.0 | 曲線 | 震盪硬直破甲效率乘數（D.3 查表）|

### G.2 Tier-3 鏈式效果旋鈕（引用自 weapon-system.md G.2 / G.3）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 本系統消費方式 |
|----------|--------|----------|------|---------------|
| `l2_t3_adjacent_heat_pct` | 30% | 20–50% | 曲線 | 破點漣漪注入比例（D.5）|
| `m3_t3_chain_dmg_mult` | 1.5 ×D₀ | 1.0–2.0 | 曲線 | 穿甲爆破鏈每目標破甲值（D.6）；= 15 BU at default |
| `m3_t3_chain_max_targets` | 2 | 閘門 | 閘門 | 連鎖最多目標數（D.6）；≥3 時鏈式效果過強 |

### G.3 部位系統專屬旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `part_regen_enabled` | false | — | 閘門 | 部位對局內永不再生；**恆為 false，任何改為 true 的路徑均屬設計違規**|
| `chain_break_is_recursive` | false | — | 閘門 | M3 Tier-3 連鎖是否遞迴；**恆為 false**（見 E.4）|
| `adjacency_max_neighbors` | 4 | 2–6 | 閘門 | 單一部位最多可宣告相鄰數；防止鏈式效果過度擴散 |
| `softened_visual_onset_max_s` | 0.5s | 0.25–0.5 | 閘門 | SOFTENED 視覺提示的最大感知延遲上限；**阻斷 Alpha 里程碑的驗收上限**（見 H.2）|
| `softened_pulse_frequency_hz` | 2.0 Hz | 1.5–3.0 | 手感 | 軟化脈動光暈的閃爍頻率 |
| `softened_color_hue` | 橙紅 #FF6600 | — | 手感 | 軟化狀態色調目標；美術執行，需符合「彈幕永遠讀得懂」視覺鐵則 |
| `stagger_visual_onset_max_s` | 0.3s | 0.1–0.5 | 手感 | 護甲剝離（ARMOR_STRIPPED）視覺提示的最大出現延遲 |
| `hitbox_size_multiplier_normal` | 1.0 | 0.7–1.3 | 曲線 | NORMAL 部位判定框大小相對於美術的比例（1.0 = 完全貼合）|
| `hitbox_size_multiplier_armored` | 0.8 | 0.6–1.0 | 閘門 | ARMORED 弱點框大小比例（偏小使命中感更有技術門檻）|
| `hitbox_size_multiplier_core` | 1.2 | 0.9–1.5 | 手感 | BOSS_CORE 判定框大小比例（偏大使核心易於瞄準，強化終局目標感）|

---

## H. 驗收標準 (Acceptance Criteria)

### H.1 狀態機正確性（功能性 — 阻斷）

- [ ] NORMAL / BOSS_CORE 部位：飛彈命中 INTACT 狀態的 B_fill = `break_delta_base × B_unsoftened_mult`
- [ ] NORMAL / BOSS_CORE 部位：飛彈命中 SOFTENED 狀態的 B_fill = `break_delta_base × 1.0`
- [ ] 所有部位：STAGGERED 期間（stagger_timer > 0）的 B_fill = `break_delta_base × stagger_break_mult`
- [ ] SOFTENED + STAGGERED：B_fill = `break_delta_base × 1.5`（確認不是 1.0 × 1.5 = 1.5 的意外「等值雙算」，而是查表直接取 1.5）
- [ ] B_current 達到 `required_break_threshold_*` 時觸發 BROKEN，且 BROKEN 為不可逆終態
- [ ] BROKEN 部位的後續命中：B_current / H_current 不變，無事件發出
- [ ] 自動化測試：`tests/unit/part-system/part_state_machine_test.[ext]`

### H.2 SOFTENED 視覺提示可讀性（體驗性 — UX 阻斷，繼承 weapon-system.md H.5）

- [ ] 部位進入 SOFTENED 後，色調偏移 + 脈動光暈必須在 **0.5 秒內**出現於螢幕（≤ `softened_visual_onset_max_s`）
- [ ] 不熟悉遊戲的受測者在 10 張含不同彈幕密度的靜態截圖中，能正確識別所有 SOFTENED 部位，**成功率 ≥ 80%**
- [ ] SOFTENED 視覺效果在最高敵彈密度（難度 4 / Nightmare）下仍可辨識（彈幕遮蓋時間 ≤ 50%）
- [ ] 驗收方法：設計師主持 5 人用戶測試；若未達標，視覺強度調整升為最高優先級，**阻斷 Alpha 里程碑**

### H.3 ARMORED 部位護甲閘門（功能性 — 阻斷）

- [ ] ARMOR_INTACT 期間，任何飛彈命中的 B_fill = 0（護甲完全偏轉）
- [ ] ARMOR_STRIPPED 只能由 L3 Wave Cannon 蓄力震波觸發（`on_l3_wave_hit` 事件），無其他路徑
- [ ] ARMOR_STRIPPED 期間的 B_fill = `break_delta_base × stagger_break_mult`（× 1.5）
- [ ] BU 在 stagger_timer 歸零後（ARMOR_INTACT 恢復）保留不清零
- [ ] 自動化測試：`tests/unit/part-system/armored_part_gate_test.[ext]`

### H.4 Boss Core 破壞觸發勝利（功能性 — 阻斷）

- [ ] BOSS_CORE 部位 B_current ≥ required_break_threshold_boss_core 時，`on_boss_core_break` 確實發出
- [ ] 事件順序正確：`on_part_break` → `on_boss_core_break`（素材登記先於結算）
- [ ] NORMAL / ARMORED 部位的 BROKEN 不觸發 `on_boss_core_break`
- [ ] 自動化測試：`tests/unit/part-system/boss_core_win_condition_test.[ext]`

### H.5 部位永不再生（功能性 — 阻斷）

- [ ] `part_regen_enabled` 在任何難度、任何關卡設定下均為 false
- [ ] 對局內 BROKEN 部位在戰鬥結束前不恢復 ALIVE
- [ ] 新一輪戰鬥（重新載入巨獸定義）後，所有部位正確初始化為 ALIVE（狀態重置，非殘留）
- [ ] 自動化測試：`tests/unit/part-system/no_regen_test.[ext]`

### H.6 相鄰鏈式效果正確性（功能性）

- [ ] L2 Tier-3 熱量脈衝：P 破壞後，所有存活鄰居各得 `adj.H_max × l2_t3_adjacent_heat_pct` HU（clamp 至 H_max）
- [ ] L2 Tier-3 若脈衝後鄰居達 theta_S，同幀觸發 `on_part_softened`
- [ ] M3 Tier-3 連鎖：最多 2 個存活鄰居受 `m3_t3_chain_dmg_mult × 10 × M_state_mult` BU
- [ ] M3 Tier-3 連鎖不遞迴（`is_chain_break=true` 時跳過連鎖計算）
- [ ] ARMOR_INTACT 的 ARMORED 部位：M3 連鎖 B_fill = 0（護甲偏轉鏈式傷害）
- [ ] 自動化測試：`tests/unit/part-system/adjacency_chain_test.[ext]`

### H.7 部位數值難度不縮放（功能性）

- [ ] 在難度 1–4 設定下分別讀取所有全域部位旋鈕，確認回傳值在各難度完全相同
- [ ] 任何難度系統相關模組不存在向本系統寫入縮放係數的路徑
- [ ] 自動化測試或設計師靜態審核：`tests/unit/part-system/difficulty_invariance_test.[ext]`

### H.8 破壞即素材掉落（功能性 — 阻斷）

- [ ] 每次 `on_part_break` 事件均攜帶非空、非 null 的 `drop_table_id`
- [ ] material-economy 系統在收到 `on_part_break` 後確認執行掉落邏輯（整合測試）
- [ ] 掉落素材的 `world_position` 與部位破壞位置一致（目視驗證）
- [ ] 整合測試：`tests/integration/part-material-drop/drop_on_break_test.[ext]`

---

*文件版本：1.0.0*  
*作者：Systems Designer Agent*  
*關聯 GDD：game-concept.md | weapon-system.md（LOCKED, caller）| material-economy.md（待撰寫，被本系統呼叫）*
