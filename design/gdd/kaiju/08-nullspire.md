# 虛尖 / NULLSPIRE — 頭目設計文件
## 殲獸戰機 / KAIJU BREAKER · Boss #08（真‧最終頭目 / TRUE-FINAL Capstone）

`kaiju_id: nullspire` | 主題：虛空系（Void）| 建議難度階：D5 惡夢（畢業考）| 陣容序位：#08（最終）

*文件路徑：`design/gdd/kaiju/08-nullspire.md`*
*最後更新：2026-07-08*
*狀態：Draft*
*相依文件：game-concept.md | weapon-system.md（LOCKED）| kaiju-part-system.md | material-economy.md | `00-roster-overview.md`（§3.5 設計膠囊、§4 資料模型影響）*
*YAML 資料定義：`assets/data/kaiju/nullspire.yaml`（由本文件第 4 節 inline 提供）*

---

## 1. 概覽 (Overview)

虛尖（NULLSPIRE，`kaiju_id: nullspire`）是「殲獸戰機」8 頭目陣容的**真‧最終頭目**，定位為「畢業考（Capstone Synthesis Boss）」：牠不教任何單一武器，而是把前 7 隻頭目教過的**每一種技巧同時**攤開在玩家面前——沒有一把武器能支配全場。牠是一座漂浮於虛空中的黑曜方尖碑，由三段對齊脊柱（`spine_seg_1/2/3`，NORMAL，重現 VOLTWYRM 的縱向穿透走廊）垂直懸掛於核心之下；核心（`singularity_core`，BOSS_CORE）被兩片**持續公轉的虛空盾**（`void_shield_l/r`，ARMORED）夾護，盾牌旋轉到正面時會**physically 擋住**玩家射向核心的任何攻擊（重現 CARAPEX 的護甲閘門 + LACERA/PRISMSHELL 的運動部位）；再外圍是兩顆**繞碑公轉的虛矛衛星**（`satellite_1/2`，NORMAL），持續發射精準瞄準的窄幅虛空矛（重現 LACERA 的追蹤剋制 + PRISMSHELL 的公轉精準）。

**首要設計職責**：作為 8 頭目陣容的收尾試煉，NULLSPIRE 必須讓玩家在同一場戰鬥中**流暢切換**：用 L4 對齊脊柱＋核心的穿透窗口、用護甲管理（軟化貫穿或 L3 剝甲）應對旋轉盾、用追蹤/精準武器清衛星、用壓制節奏撐過奇點暴露窗。任何單一 Loadout 都合法且可通關（等功率鐵則），但**沒有一種 Loadout 能在全部四個子系統上都拿到最高效率**——這正是「畢業考」的核心命題。

本文件為**內容設計文件（Boss Content Design）**，非系統 GDD。部位數值、事件契約、材料產出等系統規則以相依 GDD 為準，本文件僅引用不重定義。

---

## 2. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：挑戰（Challenge）＋表達（Expression）＋能力感（Competence，Bartle: Achiever 收束感）

> 「這不是一場新的考驗——這是七場考驗的總和。我認得這個旋轉的護甲，認得這條穿透走廊，認得這種需要追蹤的公轉節奏。我不是在學新東西，我是在證明我已經學會的一切能同時派上用場。」

NULLSPIRE 不引入任何全新機制——牠的每一個子系統都是玩家已經內化過的語彙的**重組**：旋轉護甲讓玩家想起 CARAPEX「護甲是門」的教訓，但這次盾牌會動；穿透脊柱讓玩家想起 VOLTWYRM 的縱列蓄熱，但頂端多了一顆隨時可能同時入帳的核心；公轉衛星讓玩家想起 LACERA 的追蹤解法與 PRISMSHELL 的精準狙擊。玩家在戰鬥中段會意識到：**沒有任何一把武器能吃下全場**——這一刻的頓悟服務「能力感」與「以智取勝」的核心幻想，也是整個 8 頭目陣容的情感終點：玩家不是在被考驗新知識，而是在**慶祝**自己已經掌握的知識庫。

**「奇點只在盾牌讓路的瞬間才是靶心」**：核心的可命中狀態由旋轉盾的公轉節奏決定，不是靠玩家破壞盾牌才開啟——這是「破壞是加速器，不是鑰匙」的設計轉折（呼應 VOLTWYRM「護盾是壓力來源，不是勝利必要條件」的精神，並進一步強化：即使一片盾都不破，玩家仍可靠讀窗口贏得比賽，只是壓力更大）。

**終幕**：當雙盾皆破，奇點永久暴露，殘存部位火力全開衝刺——這是全遊戲視覺與節奏最盛大的高潮，服務〔頭目是靈魂〕支柱作為整個陣容的謝幕。

---

## 3. 外形與主題 (Silhouette & Theme)

| 維度 | 設計決策 |
|------|---------|
| **生物原型** | 非生物幾何體：漂浮黑曜方尖碑，垂直脊柱懸掛於下，核心與雙盾懸浮於上，衛星遠環公轉 |
| **像素規格** | 整體結構縱向佔畫面高度 75–85%（含衛星外環半徑）；核心＋雙盾群組佔畫面寬度 35–45%；脊柱佔畫面寬度 15–20% |
| **色系（機體本身）** | 冷紫黑主色（黑曜岩 `#1A0B2E`）＋虛空扭曲邊緣光（紫紺 `#6B2FBF`）——**機體是全遊戲唯一的冷色系巨獸**，刻意打破「巨獸＝暖色」的視覺慣性，呼應「虛空吞噬一切秩序」的主題 |
| **彈幕色彩（刻意例外）** | 儘管機體是冷色，**彈幕本身仍遵守「暖色＝威脅」鐵則**：脊柱螺旋彈、護甲脈衝彈、核心脈衝彈皆為暖色（橙紅／琥珀）。**唯一例外**：衛星虛矛（Void-Lance）為冷紫色 `#9B5FFF`，這是全遊戲唯一的冷色敵彈——必須用「高亮核心 + 粗黑外框 + 0.4s 蓄力閃光電報」把它與玩家判定點（冷藍白）清楚區隔（見第 5 節 Pattern C 與第 10 節 AC-09）|
| **動態** | 整座方尖碑緩慢 S 形水平漂移（振幅 = 畫面寬 18%，週期 10s，繼承 VOLTWYRM `body_movement` 風格）；脊柱三節與核心保持**剛性連結**，隨機體一起漂移但彼此永遠垂直對齊；雙盾繞核心公轉（內環）；雙衛星繞全機體公轉（外環，半徑大於雙盾） |
| **軟化 / 硬直視覺** | 全部位 SOFTENED 統一使用專案簽章色 `#FF6600` 脈動光暈（≤0.5s 可辨）；ARMOR_STRIPPED 沿用專案標準：甲殼裂縫 + 弱點框亮白脈動 + 倒計時像素條 |
| **核心暴露視覺** | `singularity_core` 在暴露窗口開啟瞬間，外環旋轉光環轉為亮白並加速旋轉，明確提示「現在可以打」；窗口關閉前 0.5s 光環閃爍作為預警 |

---

## 4. 部位組成 (Part Composition)

### 4.1 部位總表

| `part_id` | 中文名 | 類型 | `H_max` | `B_max` | 相鄰部位 | `drop_table_id` | 運動形式 |
|-----------|-------|------|---------|---------|---------|----------------|---------|
| `spine_seg_1` | 脊柱節一 | NORMAL | 100 HU | 100 BU | `spine_seg_2` | `drop_nullspire_normal` | 隨機體剛性漂移（無獨立運動） |
| `spine_seg_2` | 脊柱節二 | NORMAL | 100 HU | 100 BU | `spine_seg_1`, `spine_seg_3` | `drop_nullspire_normal` | 隨機體剛性漂移 |
| `spine_seg_3` | 脊柱節三 | NORMAL | 100 HU | 100 BU | `spine_seg_2`, `singularity_core` | `drop_nullspire_normal` | 隨機體剛性漂移；核心正下方 |
| `void_shield_l` | 左虛空盾 | ARMORED | 150 HU | 150 BU | `singularity_core` | `drop_nullspire_armored` | **Orbit 公轉**（繞核心，內環，會物理擋彈）|
| `void_shield_r` | 右虛空盾 | ARMORED | 150 HU | 150 BU | `singularity_core` | `drop_nullspire_armored` | **Orbit 公轉**（繞核心，內環，相位差 π，會物理擋彈）|
| `satellite_1` | 虛矛衛星一 | NORMAL | 100 HU | 100 BU | `satellite_2` | `drop_nullspire_normal` | **Orbit 公轉**（繞全機體，外環）|
| `satellite_2` | 虛矛衛星二 | NORMAL | 100 HU | 100 BU | `satellite_1` | `drop_nullspire_normal` | **Orbit 公轉**（繞全機體，外環，相位差 π）|
| `singularity_core` | 奇點核 | BOSS_CORE | 200 HU | 200 BU | `spine_seg_3`, `void_shield_l`, `void_shield_r` | `drop_nullspire_core` | 隨機體剛性漂移；**僅在暴露窗口內可被命中** |

**部位數量**：本 Boss 共 8 個部位，達到 `kaiju-part-system.md` A 節「後期高難度 Boss 最多 8 個部位」的上限，符合其作為陣容最終頭目的定位。全部位 HU/BU 總量 = 3×100 + 2×150 + 2×100 + 200 = **1000**，是 8 頭目陣容中最高（VOLTWYRM 為 900），對齊「真‧最終頭目」的規模預期。

**H_max / B_max 全部使用全域旋鈕預設值，無覆寫**——TTB 的延長來自機制複雜度（旋轉遮蔽、公轉迴避稅、雙軌切換），而非數值膨脹，嚴格遵守〔難度是門，不是牆〕與「TTB/輸出/產出跨難度不變」鐵則。

### 4.2 核心暴露機制 (Core Exposure Mechanic) — 幾何遮蔽，非護甲覆寫

`singularity_core` 本身沒有獨立的「無敵」旗標。牠的可命中性完全由**雙盾當前公轉角度**決定的**物理遮蔽（Positional Occlusion）**構成，此機制獨立於護甲狀態機（ARMOR_INTACT/ARMOR_STRIPPED）之外，且有 VOLTWYRM `shield_left/right` 「護盾雙方 BROKEN 後延伸命中 core_node」的先例可循：

- 每片虛空盾繞核心公轉，公轉半徑固定，角速度 `shield_orbit_speed` 依難度縮放（見第 8 節）
- 定義「**正面朝向玩家的封鎖弧**」：`block_arc_half_deg`（全域旋鈕，預設 45°，即封鎖弧總寬 90°）。當某片盾的當前公轉角度落在正面 ±45° 內，該盾即進入 **BLOCKING 姿態**：牠的碰撞體物理佔據玩家射向核心的路徑——任何雷射或飛彈打向核心方向時，實際命中的是這片盾（依標準 ARMORED 規則：雷射永遠可蓄熱；飛彈需護甲已開啟才填 BU，見 4.3 節 M_state_mult 沿用 kaiju-part-system.md D.3）
- 雙盾初始相位相差 180°（`phase_rad` 相差 π），角速度相同 → 相對相位恆定為 180°。搭配 90° 封鎖弧寬度，全公轉週期 `T` 內形成穩定的四段式循環：**封鎖（T/4）→暴露（T/4）→封鎖（T/4）→暴露（T/4）**，每個完整周期出現 **2 次**暴露窗口
- **暴露窗口（Exposure Window）**：當下沒有任何一片盾落在封鎖弧內時，`singularity_core` 判定框啟用，可被任何武器正常命中（依 BOSS_CORE 標準 M_state_mult 規則）
- 此機制與護甲狀態機**完全獨立**：無論盾牌是 ARMOR_INTACT 還是 ARMOR_STRIPPED、SOFTENED 與否，只要牠當前公轉角度落在封鎖弧內，牠的碰撞體照樣物理擋在核心前方；破壞盾牌（BROKEN）則是唯一能**永久**移除該封鎖來源的方法

**暴露窗口時長公式**：

```
T_orbit         = 360° / shield_orbit_speed_deg_per_s   （單片盾完整公轉週期，秒）
block_duration  = (2 × block_arc_half_deg / 360°) × T_orbit
expose_duration = block_duration                          （封鎖弧固定 90°，雙盾 180° 相位差 → 對稱四分）
window_interval = T_orbit / 2                             （每次暴露窗口間隔）
```

**運算範例（D1，`shield_orbit_speed` = 30°/s）**：
```
T_orbit         = 360 / 30 = 12s
block_duration  = (90/360) × 12 = 3s
expose_duration = 3s
window_interval = 6s
→ 核心每 6 秒暴露一次，每次暴露持續 3 秒（雙盾皆存活時）
```

**任一盾 BROKEN 後的變化**：若僅剩一片盾存活，遮蔽來源只剩一個，暴露窗口大幅擴大——剩餘盾仍以自身角速度公轉，每個公轉週期只封鎖一次（`block_duration` 不變），其餘時間全數暴露：

```
單盾殘存時：expose_duration_per_cycle = T_orbit − block_duration
```

以 Phase 2 角速度（36°/s，見第 8 節）計算：`T_orbit = 10s`，`block_duration = 2.5s` → 暴露時長 = 7.5s / 10s（75% duty），較雙盾情境（50% duty）大幅提升——這是「破壞即獎勵」在 NULLSPIRE 的具體體現：破一片盾不只減少子彈來源，還讓核心幾乎隨時可打。

**雙盾皆 BROKEN 後**：`singularity_core` 永久暴露，無任何遮蔽（見第 6 節 Phase 3）。

### 4.3 脊柱穿透走廊（沿用 VOLTWYRM，並延伸至核心）

`spine_seg_1 → spine_seg_2 → spine_seg_3 → singularity_core` 四者剛性連結、永遠垂直對齊（隨機體 S 形漂移整體移動，但彼此相對位置恆定）。L4 穿透雷射沿此縱軸單發同時命中所有存活部位：

- 若核心當前處於**暴露窗口**：L4 單發同時命中 `spine_seg_1/2/3` + `singularity_core`，四部位各自獲得 `l4_h_rate`（25 HU/s）熱量，**合計 100 HU/s 跨鏈同步蓄熱**——這是全遊戲最高的 L4 综合展示，直接回應 `weapon-system.md` 開放問題 #2 的終極案例
- 若核心當前處於**封鎖窗口**：L4 依然同時命中三節脊柱（75 HU/s 跨鏈），但路徑在核心前方被 BLOCKING 姿態的盾牌物理截斷，不繼續延伸至核心本身（沿用 VOLTWYRM「護盾雙方 BROKEN 後延伸命中 core_node」的同一設計慣例：遮蔽物在，路徑止於遮蔽物）

### 4.4 相鄰圖 (ASCII)

```
                     (虛矛衛星外環，半徑較大，緩速公轉)
        satellite_2 ⟲ · · · · · · · · · · · · · · ⟳ satellite_1
                    ╲                              ╱
                     ╲        (虛空盾內環)         ╱
                      ╲   void_shield_l ◐   ◑ void_shield_r
                       ╲         ╲         ╱        ╱
                        ╲         ╲       ╱        ╱
                         ╲     singularity_core   ╱
                          ╲          │           ╱
                           ╲    （剛性垂直連結） ╱
                                     │
                               spine_seg_3
                                     │
                               spine_seg_2
                                     │
                               spine_seg_1        ← 最靠近玩家
                                     │
                       ─────────────┴───────────── （玩家區域）
```

**相鄰宣告（雙向推導）**：
- `spine_seg_1` ↔ `spine_seg_2`
- `spine_seg_2` ↔ `spine_seg_3`
- `spine_seg_3` ↔ `singularity_core`
- `void_shield_l` ↔ `singularity_core`
- `void_shield_r` ↔ `singularity_core`
- `satellite_1` ↔ `satellite_2`（同環雙生體，供 L1/L2 Tier-3 鏈式效果使用）

`singularity_core` 擁有 3 個相鄰部位（`spine_seg_3`, `void_shield_l`, `void_shield_r`），在 `adjacency_max_neighbors`（預設 4）限制內，與 VOLTWYRM `core_node` 的拓撲設計一致。

### 4.5 `assets/data/kaiju/nullspire.yaml`

```yaml
kaiju_id: "nullspire"
display_name_zh: "虛尖"
display_name_en: "NULLSPIRE"
kaiju_tier: 5                    # 陣容序位 #08，建議難度階 D5（畢業考，見第 8 節難度縮放說明）
role: "capstone_synthesis_boss"
theme: "Void"                    # KaijuTheme.Void（新增列舉值）

body_movement:
  pattern: "horizontal_s_drift"
  amplitude_screen_pct: 18        # ±18% 螢幕寬度
  speed_cycles_per_min: 6         # 每分鐘完整漂移週期 6 次（10s/週期）

parts:
  - id: "spine_seg_1"
    type: NORMAL
    H_max_override: null          # 全域預設 100 HU
    B_max_override: null          # 全域預設 100 BU
    adjacency: ["spine_seg_2"]
    drop_table_id: "drop_nullspire_normal"
    movement:
      type: "rigid_link"          # 隨 body_movement 剛性移動，無獨立公轉/擺盪
    fire_gate: "ALIVE_ONLY"

  - id: "spine_seg_2"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["spine_seg_1", "spine_seg_3"]
    drop_table_id: "drop_nullspire_normal"
    movement:
      type: "rigid_link"
    fire_gate: "ALIVE_ONLY"

  - id: "spine_seg_3"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["spine_seg_2", "singularity_core"]
    drop_table_id: "drop_nullspire_normal"
    movement:
      type: "rigid_link"
    fire_gate: "ALIVE_ONLY"

  - id: "void_shield_l"
    type: ARMORED
    H_max_override: null          # 全域預設 150 HU
    B_max_override: null          # 全域預設 150 BU
    adjacency: ["singularity_core"]
    drop_table_id: "drop_nullspire_armored"
    movement:
      type: "orbit"
      pivot_part: "singularity_core"
      radius_px: 60
      speed_deg_per_s: 30.0        # Phase 1 基準；Phase 2 起加速，見第 8 節
      phase_rad: 0.0
      block_arc_half_deg: 45       # 正面封鎖弧半寬（新增欄位，見 4.2 節）
    fire_gate: "REQUIRE_BLOCKING_ARC"   # 新增 FireGate 值：僅在 BLOCKING 姿態時開火（見第 5 節 Pattern B）

  - id: "void_shield_r"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["singularity_core"]
    drop_table_id: "drop_nullspire_armored"
    movement:
      type: "orbit"
      pivot_part: "singularity_core"
      radius_px: 60
      speed_deg_per_s: 30.0
      phase_rad: 3.14159            # π 相位差，恆與左盾相對 180°
      block_arc_half_deg: 45
    fire_gate: "REQUIRE_BLOCKING_ARC"

  - id: "satellite_1"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["satellite_2"]
    drop_table_id: "drop_nullspire_normal"
    movement:
      type: "orbit"
      pivot_part: "body_center"     # 繞整體機身中心，非繞核心
      radius_px: 140
      speed_deg_per_s: 20.0         # Phase 1 基準；見第 8 節
      phase_rad: 0.0
    fire_gate: "ALIVE_ONLY"

  - id: "satellite_2"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["satellite_1"]
    drop_table_id: "drop_nullspire_normal"
    movement:
      type: "orbit"
      pivot_part: "body_center"
      radius_px: 140
      speed_deg_per_s: 20.0
      phase_rad: 3.14159            # π 相位差
    fire_gate: "ALIVE_ONLY"

  - id: "singularity_core"
    type: BOSS_CORE
    H_max_override: null           # 全域預設 200 HU
    B_max_override: null           # 全域預設 200 BU
    adjacency: ["spine_seg_3", "void_shield_l", "void_shield_r"]
    drop_table_id: "drop_nullspire_core"
    movement:
      type: "rigid_link"
    fire_gate: "ALIVE_ONLY"
    hittable_gate: "EXPOSURE_WINDOW_ONLY"   # 新增欄位：命中資格由 4.2 節暴露窗口機制決定，獨立於 armor_state
    design_note: >
      drop_nullspire_core 產出 core_void（虛空系巨獸主題規則，material-economy.md C.1）。
      singularity_core 的可命中性不透過 armor_state 或 heat_state 判定，而是由
      void_shield_l / void_shield_r 的即時公轉角度決定（見 4.2 節公式）；
      此 hittable_gate 欄位為 NULLSPIRE 專屬新增，需與 05-prismshell.md 的
      公轉晶面機制對齊實作（兩者共用 PartMovement.Orbit 擴充，見 00-roster-overview.md §4）。
```

> **Schema 擴充需求（供實作勘查）**：`block_arc_half_deg`、`fire_gate: REQUIRE_BLOCKING_ARC`、`hittable_gate: EXPOSURE_WINDOW_ONLY` 為本文件新增的資料欄位，尚未存在於 `PartDef` / `PartMovement` schema 中，需在 `00-roster-overview.md` §4 描述的擴充範圍內一併新增（皆為可選欄位，預設 null/None，向後相容）。

---

## 5. 攻擊模式 (Attack Patterns)

**全域規則**：機體本身冷紫黑，但**彈幕依然遵守「暖色＝威脅」鐵則**——唯一例外是 Pattern C 的虛矛（冷紫），且該例外附帶強制電報與加粗外框要求（見第 3 節）。子彈速度跨四難度階恆定；僅彈數、射速與（本 Boss 特有）部位公轉節奏依難度縮放（見第 8 節，比照 VOLTWYRM「蛇身衝刺間隔」先例，運動節奏縮放不違反鐵則）。

---

### Pattern A：脊柱雙螺旋 (Spine Twin Spiral)

| 屬性 | 值 |
|------|-----|
| **發射源** | 存活的 `spine_seg_1/2/3`，各自獨立發射 |
| **彈型** | 雙臂螺旋（D1 基準），暖橙紅 `#FF6A00`，黑色像素外框 |
| **臂旋轉速度** | 恆定 50°/s（不隨難度縮放，僅臂數縮放）|
| **子彈速度** | 130 px/s（恆定跨難度）|
| **射頻** | 每臂每 0.15s 生成 1 發（恆定跨難度，臂數變化即密度變化）|
| **觸發條件** | 對應脊柱節 `break_state == ALIVE`；破壞後對應發射點消失 |
| **設計目的** | 重現 VOLTWYRM Pattern A 的螺旋語彙，教玩家「這條走廊我認得」；密度縮放見第 8 節 |

---

### Pattern B：虛盾封鎖脈衝 (Void Shield Lockstep Pulse)

| 屬性 | 值 |
|------|-----|
| **發射源** | `void_shield_l` / `void_shield_r`，**僅在 BLOCKING 姿態時開火**（`fire_gate = REQUIRE_BLOCKING_ARC`）|
| **彈型** | 短程扇形虛能脈衝（1–3 發，依難度），暖琥珀色 `#FFB000`，向玩家方向噴射 |
| **子彈速度** | 110 px/s（恆定跨難度）|
| **射頻** | BLOCKING 姿態期間每 2.0s（D1）～1.0s（D4）一次（見第 8 節）|
| **觸發條件** | 對應盾 `break_state == ALIVE` 且當前公轉角度落在封鎖弧內（見 4.2 節）；離開封鎖弧（暴露窗口期間）**完全靜默**，不開火 |
| **視覺提示** | 盾牌進入封鎖弧前 0.4s，盾面亮起琥珀色蓄力光紋（電報），與暴露窗口開啟前 0.5s 的「光環轉白」提示形成清楚的「危險 vs 安全」二元讀取 |
| **設計目的** | 「盾牌擋著＝盾牌在打你；盾牌讓路＝核心是靶心」的二選一節奏，重現 CARAPEX 護甲閘門教訓，但疊加動態時機判讀（PRISMSHELL 的公轉語彙）|

---

### Pattern C：衛星虛矛連珠 (Satellite Void-Lance Volley) — **冷色例外**

| 屬性 | 值 |
|------|-----|
| **發射源** | `satellite_1` / `satellite_2`，各自獨立公轉發射，相位交錯（不同幀開火，避免同步）|
| **彈型** | 單發窄幅精準矛（Aimed，鎖定玩家發射瞬間座標，非持續追蹤）|
| **子彈色** | **冷紫 `#9B5FFF`**（全遊戲唯一冷色敵彈）＋亮白核心高光＋加粗黑色外框（2px，比其他彈幕外框粗 1px）|
| **子彈速度** | 300 px/s（恆定跨難度；刻意較快，強化「精準狙擊」的緊迫感，以強電報補償）|
| **電報** | 發射前 0.4s，衛星本體發出冷紫蓄力閃光（比其他部位電報更長 0.1s，補償冷色彈幕的辨識負擔）|
| **射頻（單顆衛星）** | 1 發 / 2.2s（D1）～1 發 / 1.2s（D4）（見第 8 節）|
| **觸發條件** | 對應衛星 `break_state == ALIVE`；破壞後對應發射點消失 |
| **設計目的** | 重現 LACERA 的「移動目標，追蹤剋制」與 PRISMSHELL 的「精準狙擊移動弱點」；冷色例外測試「彈幕永遠讀得懂」鐵則在最極端情境下的韌性（見第 10 節 AC-09）|

---

### Pattern D：奇點蝕光脈衝 (Singularity Erosion Pulse)

| 屬性 | 值 |
|------|-----|
| **發射源** | `singularity_core`（恆常啟用，不受暴露窗口影響——核心無論能否被打中都持續反擊）|
| **彈型（Phase 1）** | 1 發瞄準彈（Aimed），深紅 `#CC3333` |
| **彈型（Phase 2）** | 4-way 十字放射（固定方向，非追蹤）|
| **彈型（Phase 3／終幕，見 Pattern E）** | 8-way 放射，射頻大幅提升 |
| **子彈速度** | 100 px/s（恆定跨難度，全 Boss 最慢子彈之一，確保核心區域仍可讀）|
| **射頻** | Phase 1：1 次/5.0s；Phase 2：1 次/3.5s（見第 8 節難度微調）|
| **觸發條件** | 始終啟用；彈形依 Phase 切換 |
| **設計目的** | 核心不是被動靶心——牠全程都在反擊，即使暴露窗口未開，玩家也不能無視核心方向 |

---

### Pattern E：終焉齊奏 (Finale Chorus) — **僅 Phase 3**

| 屬性 | 值 |
|------|-----|
| **觸發** | `void_shield_l` 與 `void_shield_r` 皆 `BROKEN`（見第 6 節 Phase 3）|
| **效果** | 所有存活部位（殘存脊柱節、殘存衛星、`singularity_core`）的射頻**同步提升 50%**（`nullspire_p3_rate_mult`，全域旋鈕，預設 1.5，範圍 1.3–1.8）；`singularity_core` 永久暴露、彈型固定為 Pattern D 的 8-way 放射 |
| **視覺** | 全機體光效提升亮度、機身震動頻率加快，明確傳達「這是最後一段」|
| **設計目的** | 全部位殘存火力齊發的最終衝刺——服務〔頭目是靈魂〕支柱作為 8 頭目陣容的謝幕高潮 |

### 模式觸發條件彙總

| 模式 | 啟動 | 停止 |
|------|------|------|
| A 脊柱雙螺旋 | 對應脊柱節 `ALIVE` | 對應脊柱節 `BROKEN` |
| B 虛盾封鎖脈衝 | 對應盾 `ALIVE` 且處於 BLOCKING 姿態 | 對應盾 `BROKEN`（永久）；暴露窗口期間暫停 |
| C 衛星虛矛連珠 | 對應衛星 `ALIVE` | 對應衛星 `BROKEN` |
| D 奇點蝕光脈衝 | 始終 | `singularity_core` `BROKEN`（戰鬥結束）|
| E 終焉齊奏 | 雙盾皆 `BROKEN` | 戰鬥結束 |

---

## 6. 階段 (Phases)

階段由**部位破壞狀態**驅動，非血量閾值，延續全陣容的一貫哲學。NULLSPIRE 的階段轉換刻意設計為**雙路徑（OR 觸發）**，尊重〔橫向選擇〕支柱——玩家可以先攻盾、先攻衛星，或交錯進行，兩條路都能推進戰鬥。

### Phase 1：「方尖顯形 (Spire Manifest)」 *(全部位 ALIVE)*

- **觸發**：戰鬥開始
- **攻擊組合**：Pattern A（雙螺旋，基礎密度）＋ Pattern B（雙盾各自封鎖弧內開火）＋ Pattern C（雙衛星交錯連珠）＋ Pattern D（Phase 1 弱化瞄準彈）
- **設計意圖**：玩家在最高複雜度下重新校準前 7 隻頭目教過的所有反射：讀盾牌旋轉節奏找暴露窗口、讀脊柱對齊找 L4 走廊、讀衛星公轉找追蹤/精準時機。核心暴露窗口（雙盾情境）每 6 秒（D1）出現一次，持續 3 秒——足夠有經驗的玩家完成一次精準爆發輸出
- **核心爽點時刻**：首次在暴露窗口內對齊脊柱發射 L4，單發同時點亮 `spine_seg_1/2/3` + `singularity_core` 四個目標的橙紅蓄熱光——這是全遊戲最盛大的「蓄熱全中」視覺回饋，是 VOLTWYRM 展示的直接升級版

### Phase 2：「裂界形態 (Fractured Threshold)」 *(≥1 個 void_shield BROKEN，或雙衛星皆 BROKEN，任一成立即觸發)*

- **觸發**：`void_shield_l` 或 `void_shield_r` 任一 `BROKEN` **或** `satellite_1` 與 `satellite_2` 皆 `BROKEN`（OR 邏輯，尊重玩家的路線選擇）
- **攻擊組合**：
  - 若觸發路徑為「破盾」：剩餘盾公轉加速（30°/s → 36°/s）；暴露 duty cycle 從 50% 躍升至 75%（見 4.2 節公式）；Pattern A 螺旋臂數 +1；殘存衛星（若仍存活）連珠射頻提升
  - 若觸發路徑為「破雙衛星」：雙盾維持原公轉節奏，但 Pattern A 螺旋臂數 +1、Pattern B 封鎖脈衝彈數 +1（補償場上少了衛星帶來的密度真空）
- **設計意圖**：無論玩家先攻哪個子系統，戰鬥的複雜度都會用不同方式重新分配——這是「沒有支配性武器」命題在階段設計上的直接體現：優先清衛星的玩家換來更密的螺旋/盾脈衝；優先攻盾的玩家換來近乎常駐暴露的核心，但仍要應付雙衛星的追獵

### Phase 3：「奇點裸露 (Singularity Bare)」 *(void_shield_l 與 void_shield_r 皆 BROKEN)*

- **觸發**：兩片虛空盾均 `BROKEN`（無論衛星或脊柱當時狀態如何）
- **攻擊組合**：Pattern E 終焉齊奏啟動（所有存活部位射頻 ×1.5）；`singularity_core` 永久暴露，Pattern D 切換為 8-way 放射
- **設計意圖**：核心不再需要等待窗口——玩家終於能持續集火，但代價是所有殘存部位（脊柱、衛星，若仍存活）以及核心自身同時進入最高壓輸出。這是全戰鬥張力最高、也最爽快的收尾段：「門已經完全打開，剩下的只有你的火力對決牠的最後掙扎」
- **可選部位張力**：若脊柱三節與雙衛星在 Phase 3 開始時仍有部位存活，玩家面對熟悉的〔頭目是靈魂〕微決策——「集火核心速通，還是先清剩餘部位換 `core_void` 農量？」

---

## 7. 剋制與偏好 Loadout (Weapon Affinity)

**核心設計命題**：NULLSPIRE **沒有主剋制武器**。下表刻意呈現「每把武器都在某個子系統上拿到 ★★★，但沒有任何武器在全場總分上贏過其他武器」——這是「畢業考」精神的量化體現。

### 7.1 分子系統武器親和表

| 武器 | 對脊柱（穿透走廊）| 對虛空盾（旋轉封鎖）| 對衛星（公轉精準）| 對奇點核（窗口狙擊）|
|------|-----------------|-------------------|-------------------|-------------------|
| **L1 散波雷射** | ★★ 三束同時蓄熱三節，適合無 L4 玩家的替代穿透 | ★★ 廣域蓄熱可讓盾「軟化貫穿」而不必精準對位 | ★ 廣域命中移動衛星效率低 | ★ 暴露窗口短，廣域分散輸出不利 |
| **L2 集束雷射** | ★★ 單點蓄熱快，但需依序清三節 | ★★ 盾在封鎖弧內時是靜止可預測的近距目標，蓄熱穩定 | ★★★ 精準鎖定移動衛星，效率全場最佳 | ★★ 需精準對位窗口，但蓄熱速率最快 |
| **L3 波動砲** | ★ 蓄力期間無法對齊移動脊柱，效益低 | ★★★ 蓄力震波瞬間剝甲，跳過等待軟化貫穿的時間差 | ★ 衛星移動中，蓄力預判困難 | ★★ 剝盾窗口間接加速核心永久暴露的時程 |
| **L4 穿透雷射** | ★★★ 縱列同時蓄熱三節（75 HU/s），暴露窗口內延伸命中核心（100 HU/s），全遊戲最強穿透展示 | ★ 盾不在脊柱縱軸上，L4 打不到盾 | ★ 衛星不在縱軸上，L4 打不到衛星 | ★★★（僅暴露窗口內）單發同時點燃四部位 |
| **M1 追蹤飛彈** | ★★ 追蹤自動對位移動中的脊柱（隨機體漂移）|  ★★ 追蹤盾牌公轉位置，免去手動預判 | ★★★ 追蹤自動命中公轉衛星，重現 LACERA 展示 | ★ 追蹤鎖定核心時常因暴露窗短暫而錯失時機 |
| **M2 蜂群飛彈** | ★★ 廣域覆蓋三節，適合同時壓制 | ★★ 廣域覆蓋提高「剛好命中暴露瞬間」的機率 | ★★ 廣域撒佈部分命中公轉目標 | ★★★ 齊射覆蓋短暫窗口，即使時機抓得不完美也有機率命中，適合抓窗口新手 |
| **M3 穿甲魚雷** | ★★ 熱衝擊引爆對已軟化脊柱節效率高 | ★★ 軟化貫穿後熱衝擊引爆可快速破盾 | ★ 無追蹤，對移動衛星命中率低 | ★★★（軟化後）暴露窗口內熱衝擊引爆可能一發填滿大半 B_max |
| **M4 叢集炸彈** | ★★ 三節垂直排列，AoE 可能同時覆蓋 2 節 | ★ 盾牌位置隨公轉移動，AoE 落點難以固定覆蓋 | ★★ 衛星外環公轉，AoE 偶爾能覆蓋交會處 | ★ 暴露窗口短，AoE 需要玩家已在正確落點蓄力 |

**讀表方式**：每一欄都有 1–2 個 ★★★ 條目（脊柱→L4；盾→L3；衛星→L2/M1；核心→L4/M2/M3），但**沒有任何一列全部都是 ★★★**——這正是刻意的設計結果，量化驗收見第 10 節 AC-01。

### 7.2 展示 Loadout：L4 × M1「穿透-追獵」綜合流

本 Loadout 展現「盡量兼顧四個子系統」的均衡打法，而非單一最優解：

```
暴露窗口開啟（每 6s / 3s 持續，D1）：L4 對齊脊柱縱軸發射 → spine_seg_1/2/3 + singularity_core 同步 100 HU/s 蓄熱
窗口關閉期間：M1 追蹤飛彈自動鎖定公轉中的 satellite_1/2，逐步填充其 BU
盾牌進入 BLOCKING 姿態時：L4 順勢蓄熱該盾（雷射不受護甲阻擋，盾牌軟化後 M1 飛彈可用標準路徑（×1.0）填充其 BU）
```

**設計說明**：此 Loadout 不是「最優解」，而是「最均衡解」——L4 覆蓋穿透與窗口狙擊兩個子系統（各 ★★★ 與部分 ★★★），M1 覆蓋衛星與盾牌軟化貫穿路徑（★★★ 與 ★★）。玩家仍需要主動判讀窗口時機、主動閃避 Pattern B/C/D 彈幕——武器只解決部分問題，其餘交給玩家的節奏判斷，完全符合「畢業考」不給單一武器免死金牌的設計初衷。

### 7.3 任何 Loadout 均可通關（公平性保證）

| Loadout 範例 | 策略路線 | 相對展示 Loadout 的 TTB 倍率 |
|-------------|---------|--------------------------|
| L4 × M1（展示）| 穿透＋追蹤兼顧 | 1.0×（基準）|
| L2 × M2 | 精準狙擊盾/衛星＋蜂群覆蓋窗口 | ~1.1–1.3× |
| L1 × M3 | 廣域蓄熱多部位＋引爆收割 | ~1.3–1.5× |
| L3 × M1 | 優先剝盾加速核心永久暴露，追蹤清衛星 | ~1.2–1.4×（前期慢，後期因核心提早裸露而加速）|
| 任意 × M4 | AoE 覆蓋機會財，需精準落點 | ~1.4–1.6×（學習曲線最高，但合法可行）|

無 L4 的 Loadout 依然可以在暴露窗口內用其他雷射+飛彈組合攻擊核心（僅無法同時蓄熱脊柱三節），符合「等功率鐵則」——所有 Loadout 都在 weapon-system.md H.2 的「不超過最優路徑 2.0×」容差內。

---

## 8. 難度縮放 (Difficulty Scaling)

**縮放原則**：僅調整彈幕密度與（NULLSPIRE 特有的）部位運動節奏（比照 VOLTWYRM「蛇身衝刺間隔」的先例——運動/時機參數縮放不等於彈幕數值縮放，不違反鐵則）。子彈速度、部位 H_max/B_max、封鎖弧寬度（`block_arc_half_deg` 恆定 45°）跨四難度階恆定。

> **關於「D5 惡夢」標示**：`00-roster-overview.md` 將 NULLSPIRE 標示為建議難度階 D5，反映牠在「解鎖後才能挑戰」的陣容序位（需先於其他 7 隻頭目通關 D4 夢魘後解鎖），而非額外新增第五組難度縮放軸。本 Boss 內部的密度縮放仍嚴格遵循全系統統一的 D1（Normal）–D4（Nightmare）四階架構；其 D1 基準密度本身即高於前 7 隻頭目的 D1（因應其陣容終點定位），但四階間的縮放邏輯與其餘 7 隻頭目完全一致。

### Pattern A：脊柱雙螺旋

| 難度 | 每節螺旋臂數（Phase 1）| 臂數（Phase 2）|
|------|---------------------|--------------|
| D1 Normal | 2 | 3 |
| D2 Hard | 3 | 4 |
| D3 Extreme | 4 | 4（+彈速 +15%，Phase 3 限定）|
| D4 Nightmare | 4 | 4（+彈速 +30%，Phase 3 限定）|

### Pattern B：虛盾封鎖脈衝

| 難度 | 封鎖弧內彈數 | 射頻 |
|------|------------|------|
| D1 Normal | 1 | 1/2.0s |
| D2 Hard | 2 | 1/1.6s |
| D3 Extreme | 2 | 1/1.3s |
| D4 Nightmare | 3 | 1/1.0s |

### Pattern C：衛星虛矛連珠

| 難度 | 單衛星射頻 |
|------|-----------|
| D1 Normal | 1/2.2s |
| D2 Hard | 1/1.8s |
| D3 Extreme | 1/1.5s |
| D4 Nightmare | 1/1.2s |

### 部位運動節奏縮放（NULLSPIRE 特有，比照 VOLTWYRM 蛇身衝刺間隔先例）

| 難度 | `shield_orbit_speed`（雙盾公轉角速度）| T_orbit | 暴露窗長度（雙盾存活）| 暴露窗間隔 |
|------|-----------------------------------|---------|---------------------|-----------|
| D1 Normal | 30°/s | 12s | 3.0s | 6s |
| D2 Hard | 36°/s | 10s | 2.5s | 5s |
| D3 Extreme | 45°/s | 8s | 2.0s | 4s |
| D4 Nightmare | 60°/s | 6s | 1.5s | 3s |

| 難度 | `satellite_orbit_speed`（衛星公轉角速度）|
|------|----------------------------------------|
| D1 Normal | 20°/s（18s/圈）|
| D2 Hard | 24°/s（15s/圈）|
| D3 Extreme | 30°/s（12s/圈）|
| D4 Nightmare | 36°/s（10s/圈）|

> 暴露窗口 duty cycle（50%）跨四難度階恆定——縮放的是**窗口的絕對時長**（3.0s → 1.5s），不是窗口出現的機率。這確保「難度只影響反應精度要求，不影響策略是否可行」，嚴格對齊〔難度是門，不是牆〕支柱。

### Pattern D / E：奇點蝕光脈衝 / 終焉齊奏

| 難度 | Phase 1 射頻 | Phase 2 射頻 | Phase 3（終焉）射頻 |
|------|------------|------------|-------------------|
| D1 | 1/5.0s | 1/3.5s | 1/2.0s |
| D2 | 1/4.5s | 1/3.0s | 1/1.6s |
| D3 | 1/4.0s | 1/2.5s | 1/1.3s |
| D4 | 1/3.5s | 1/2.0s | 1/1.0s |

`nullspire_p3_rate_mult`（Phase 3 全部位射頻加乘，預設 1.5）跨難度階恆定不縮放——Phase 3 的壓力提升已由上表的基礎射頻縮放涵蓋，避免雙重疊加造成不可讀的彈幕密度。

---

## 9. 素材產出 (Material Drops)

### 9.1 掉落表定義

| `drop_table_id` | 對應部位 | 部位類型 | 核心素材 | shard_common（Standard / Precision / Perfect）|
|----------------|--------|---------|---------|----------------------------------------------|
| `drop_nullspire_normal` | `spine_seg_1/2/3`, `satellite_1/2` | NORMAL | `core_void` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_nullspire_armored` | `void_shield_l/r` | ARMORED | `core_void` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_nullspire_core` | `singularity_core` | BOSS_CORE | `core_void` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |

*NULLSPIRE 屬虛空系（`kaiju_void` / `KaijuTheme.Void`）巨獸，所有 8 個部位均掉落 `core_void`（材料經濟主題映射規則）。碎片計算沿用全陣容公式：`floor(shard_base × quality_mult)`；shard_base = 2，Standard = 1.0×，Precision = 1.5×（floor=3），Perfect = 2.0×（= 4）。*

### 9.2 一場狩獵預期產出（D1，Precision 品質為主）

| 通關策略 | core_void | shard_common |
|---------|-----------|-------------|
| **全破壞**（8 部位全破）| 8（Perfect 可達 16）| 8×3 + 5（全破壞完成獎勵）= **29** |
| **僅破核心**（速通，靠暴露窗口狙擊）| 1 | 3 |
| **破雙盾 + 核心**（跳過脊柱與衛星）| 3 | 9 |
| **破脊柱三節 + 核心**（L4 穿透流）| 4 | 12 |
| **破雙衛星 + 核心**（追蹤流）| 3 | 9 |

### 9.3 `essence_kaiju` 產出

`on_hunt_end(is_all_parts_broken = true)` 時觸發（8 個部位均 `BROKEN`）→ `essence_kaiju` × 1 + `shard_completeness_bonus`（= 5 碎片）。任何難度階均適用，無難度門檻（〔難度是門，不是牆〕支柱）。

### 9.4 `core_void` 的跨遊戲戰略意義

不同於前 7 隻頭目各自對應 3 種特定武器的升級素材（例如 CARAPEX → `core_carapace` 對應 L1/M2/M4），`core_void` 作為陣容**唯一的「綜合系」核心**，其設計定位是：**所有 8 種武器最終 Tier（Tier 2→3 或最高階）的共通升級素材**（沿用 `material-economy.md` 主題映射規則，虛空系不綁定特定 3 種武器，而是綁定「終局全武器」）。這一設計選擇直接呼應 NULLSPIRE 的核心命題——「沒有一種武器該被畢業考排除在外」：玩家想把武器庫的**任何一把**武器推到最高階，最終都必須回來挑戰這隻終焉頭目。

---

## 10. 驗收標準 (Acceptance Criteria)

### AC-01 「無支配武器」命題驗證（體驗性 — 阻斷）

- [ ] 靜態審核第 7.1 節武器親和表：確認每一欄（脊柱/盾/衛星/核心）皆至少有 1 個 ★★★ 條目，且**沒有任何一列**（單一武器）在四欄中拿到 ≥ 3 個 ★★★
- [ ] 5 人 Playtest（已通關前 7 隻頭目的玩家）：戰鬥中武器切換次數統計，≥ 4/5 受測者在單場戰鬥中主動切換過至少 2 種主武器或副武器組合
- [ ] 受測者問卷：「這場戰鬥中，你覺得有沒有一把武器可以從頭用到尾？」≥ 4/5 回答「沒有」或「效率會打折扣」

### AC-02 核心暴露窗口機制正確性（功能性 — 阻斷）

- [ ] `singularity_core` 的可命中性（hittable）在每一幀正確反映 4.2 節公式：當任一 `void_shield` 當前公轉角度落在其 `block_arc_half_deg`（45°）內時，`singularity_core` 判定框停用；否則啟用
- [ ] D1 基準下，雙盾存活時暴露窗口時長 = 3.0s ± 1 幀誤差，窗口間隔 = 6.0s ± 1 幀誤差（自動化測試遍歷 3 個完整公轉週期驗證）
- [ ] 任一 `void_shield` `BROKEN` 後，暴露 duty cycle 由 50% 提升至 75%（單盾殘存公式，見 4.2 節），自動化測試驗證
- [ ] 雙 `void_shield` 皆 `BROKEN` 後，`singularity_core` 恆為可命中狀態（無論任何時刻）
- [ ] 自動化測試路徑：`tests/unit/kaiju/nullspire_core_exposure_window_test.[ext]`

### AC-03 虛空盾物理遮蔽與護甲雙軌正確性（功能性 — 阻斷）

- [ ] `void_shield` 處於 BLOCKING 姿態時，任何射向 `singularity_core` 方向的雷射/飛彈判定為命中該盾（而非核心），依 kaiju-part-system.md D.3 標準 ARMORED 規則結算（雷射永遠蓄熱；飛彈需 SOFTENED 或 ARMOR_STRIPPED 才有效填充 BU）
- [ ] `void_shield` 處於暴露窗口姿態（非 BLOCKING）時，其自身仍可被命中（若玩家選擇優先攻盾），且不再遮蔽核心
- [ ] `void_shield` 的 `fire_gate = REQUIRE_BLOCKING_ARC` 正確運作：僅在 BLOCKING 姿態時觸發 Pattern B 開火；暴露姿態期間靜默
- [ ] 自動化測試路徑：`tests/unit/kaiju/nullspire_shield_blocking_gate_test.[ext]`

### AC-04 脊柱穿透走廊延伸至核心（功能性 — 阻斷）

- [ ] L4 穿透雷射於暴露窗口內、脊柱縱軸對齊時，單發同時對 `spine_seg_1/2/3` + `singularity_core` 四部位各自發出 `on_laser_hit`（各 25 HU/s），量化驗證合計 100 HU/s
- [ ] L4 於封鎖窗口內（任一盾 BLOCKING）發射時，僅命中 `spine_seg_1/2/3`（3×25 = 75 HU/s），不延伸至核心（路徑被盾物理截斷）
- [ ] 自動化測試路徑：`tests/unit/weapon/l4_nullspire_full_chain_test.[ext]`

### AC-05 衛星公轉與追蹤命中率（功能性）

- [ ] `satellite_1/2` 的 `world_position` 每幀按 `orbit`（pivot=body_center, radius=140px, speed 依難度）正確更新
- [ ] M1 追蹤飛彈對公轉衛星的實際命中率（D1，10 次射擊統計）≥ 80%（延續 LACERA AC-10.2 驗證方法論）
- [ ] 衛星虛矛（Pattern C）發射瞬間鎖定玩家當下座標（Aimed，非持續追蹤），電報時長 0.4s 於截圖測試中可辨識

### AC-06 階段轉換雙路徑正確性（功能性）

- [ ] Phase 2 觸發條件正確實作 OR 邏輯：`void_shield_l` **或** `void_shield_r` `BROKEN`，**或** `satellite_1` **且** `satellite_2` 皆 `BROKEN`，任一成立即轉換，不要求兩條件同時成立
- [ ] Phase 3 觸發條件：`void_shield_l` **且** `void_shield_r` 皆 `BROKEN`（AND 邏輯，需雙盾皆破）
- [ ] Phase 3 啟動後，`nullspire_p3_rate_mult`（1.5）正確套用於所有存活部位的射頻，且不與第 8 節難度密度縮放重複疊加
- [ ] 階段轉換為單向不可逆

### AC-07 難度密度與運動節奏縮放不變性（功能性）

- [ ] 子彈速度（Pattern A：130 px/s；Pattern B：110 px/s；Pattern C：300 px/s；Pattern D：100 px/s）跨 D1–D4 恆定
- [ ] `block_arc_half_deg`（45°）與部位 H_max/B_max 跨難度階恆定（`difficulty_invariance_test` 覆蓋）
- [ ] `shield_orbit_speed` 與 `satellite_orbit_speed` 依第 8 節表格縮放，且暴露窗口 duty cycle（50%／75%／100%）在任一難度階下數值一致，僅絕對時長隨速度縮放

### AC-08 素材掉落與 core_void 全武器映射正確性（功能性 — 阻斷）

- [ ] `drop_nullspire_normal` / `_armored` / `_core` 均正確掉落 `core_void` + `shard_common`（依品質乘數）
- [ ] Perfect 品質（SOFTENED_STAGGERED break）：`core_void` 數量 = 2
- [ ] 全破壞（8/8 部位）結算：`essence_kaiju` × 1 + `shard_completeness_bonus`（= 5）
- [ ] `core_void` 在 `material-economy.md` 武器升級對映表中正確關聯至全部 8 種武器的最終 Tier（而非僅 3 種），與其餘 7 隻頭目的主題映射規則區隔
- [ ] 自動化測試路徑：`tests/unit/economy/material_yield_quality_test`（擴充 NULLSPIRE 3 個 drop_table × 3 品質等級）

### AC-09 虛矛冷色例外的可讀性（體驗性 — UX 阻斷）

- [ ] 5 人用戶測試：D4 最高密度截圖中，受測者正確辨識「衛星虛矛（冷紫）是敵方彈幕，不是玩家判定點或安全區」的成功率 ≥ 85%（因其為全遊戲唯一冷色敵彈，門檻高於一般彈幕可讀性測試的 80%）
- [ ] 虛矛彈幕與玩家判定點（冷藍白）的並排截圖比對測試：≥ 4/5 受測者能在 1 秒內正確區分兩者（外框粗細、亮度、輪廓差異）
- [ ] 衛星蓄力電報（0.4s 冷紫閃光）在 D4 密度下的截圖測試中，≥ 80% 受測者能於電報階段預判即將發射的方向

---

*文件版本：1.0.0*
*作者：Game Designer Agent*
*最後更新：2026-07-08*
*資料定義：`assets/data/kaiju/nullspire.yaml`（inline 見第 4 節）*
*依賴掉落表 ID：`drop_nullspire_core` / `drop_nullspire_normal` / `drop_nullspire_armored`（由 material-economy 實作）*
*Schema 擴充依賴：`PartMovement.Orbit`（pivot/radius/角速度/相位）、`block_arc_half_deg`、`FireGate.REQUIRE_BLOCKING_ARC`、`hittable_gate: EXPOSURE_WINDOW_ONLY`（見第 4.5 節附註，需與 `05-prismshell.md` 對齊實作，並回報 `00-roster-overview.md` §4）*
