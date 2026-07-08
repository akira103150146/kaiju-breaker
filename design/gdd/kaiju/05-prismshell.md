# 稜殼獸 / PRISMSHELL — 頭目設計文件

`kaiju_id: prismshell` | 難度階建議: 3（D3）| 主題: 晶簇系 Crystal | 8 頭目陣容序位: #05

*最後更新：2026-07-08*
*狀態：Draft*
*關聯 GDD：00-roster-overview.md §3.2 | kaiju-part-system.md | weapon-system.md | material-economy.md | 01-carapex.md（範本）| 02-lacera.md（公轉移動詞彙來源）*

---

## 1. 概覽 (Overview)

稜殼獸 PRISMSHELL 是殲獸戰機 8 頭目陣容中的**精準管理型 Boss**（D3），定位為「L2 集束光束」的旗艦展示場。牠是一顆六角晶洞形態的巨獸：**4 片旋轉裝甲晶面（facet_a–d，ARMORED）**環繞中央**多刻面亮核（prism_core，BOSS_CORE）**持續公轉，各晶面獨自折射出高速光扇彈幕；一枚針尖大小的**稜隙核（weak_node，NORMAL）**藏在晶面公轉形成的縫隙裡，唯有先讓相鄰晶面軟化或被 L3 剝甲、使該片晶面暫停折射，稜隙核才會露出可命中的窗口。這場戰鬥的核心教學是「裝甲管理 + 精準狙擊」：任何雷射都能靠蓄熱打開晶面的護甲之門（軟化貫穿路徑，見 kaiju-part-system.md C.4），但**只有精準武器能在短暫窗口內把握住那個針尖大的目標**——L2 集束光束的窄判定框與最快單體蓄熱速率，讓它成為「先開窗、後一發入魂」這套節奏最順手的武器，而散射系武器雖然一樣能通關，卻得付出明顯更高的瞄準代價。戰鬥結束後玩家應能說出：「先燒開一片晶面，縫裡的針尖弱點就會亮起來——這時候換上集束光束，一發入魂。」

---

## 2. 玩家幻想 (Player Fantasy)

**「拆解一座會反擊的水晶陣」**

目標 MDA 美學（優先序）：

| 美學 | 體驗設計意圖 |
|------|------------|
| **能力感（Competence）** | 「我看懂了縫隙」的頓悟時刻——發現軟化任一晶面就能讓稜隙核現形，並且成功用集束光束一擊命中的精準成就感 |
| **感官愉悅（Sensation）** | 晶面折射光扇冷冽刺眼的視覺美感，配上軟化爆裂、剝甲碎裂的高對比破壞特效 |
| **表達（Expression）** | 「我選擇了精準流派」——玩家可以刻意練習用窄判定框武器蹲點狙擊，而非依賴散射武器蠻力清理 |

PRISMSHELL 不像 LACERA 教「讓自動追蹤幫你處理移動目標」——牠教的是相反的信念：**「相信自己的準頭」**。晶面公轉緩慢而規律（15°/s，接近可預判的時鐘節奏），這不是不可捉摸的混亂，而是一場「讀懂節奏、精準出手」的技術展演。稜隙核的針尖判定框加上短暫的開窗時間，是整個 8 頭目陣容中對玩家「手動精準」要求最高的一次教學，直接呼應「以智取勝」核心幻想中最純粹的一面。

---

## 3. 外形與主題 (Silhouette & Theme)

### 設計語言

| 維度 | 設計決策 |
|------|---------|
| **生物原型** | 六角晶洞礦體：中央多刻面亮核 + 4 片獨立公轉的裝甲晶面，整體如一座緩慢自轉的活體水晶陣 |
| **像素規格** | 畫面佔寬 55–70%；晶面公轉半徑約畫面寬 18%（見 D.1 `prismshell_facet_orbit_radius_px`）|
| **色系** | 冷色系主體：深藍紫礦體 + 冰藍折射邊光；核心為多刻面亮白紫 |
| **動態** | 本體整體緩慢垂直浮動（呼吸感，振幅 = 畫面高 4%，週期 6s）；4 片晶面各自以 15°/s 公轉，彼此相位相差 90°（見第 4 節 YAML）|

### 色彩視覺鐵則與「冷色例外」的明確聲明

**全域規則**：敵彈暖色、玩家彈冷色——這是全遊戲的可讀性鐵則（roster-overview.md §1.6）。

**PRISMSHELL 的刻意例外**：晶面折射的光扇彈幕與核心的十字精準雷，設定上是「玩家自身雷射被晶體折射後反射回來的光」，主題上必須是冷色系（藍紫）才能成立敘事幻想。這與全域「敵彈暖色」鐵則直接衝突，因此本文件明確聲明此例外並定義緩解措施：

| 緩解手段 | 具體規格 |
|---------|---------|
| **飽和度/明度區隔（而非色相區隔）** | 玩家判定色維持淡冰藍/白（低飽和，`#BFE8FF` 系）；PRISMSHELL 彈幕改用高飽和電光紫藍（`#6A3DFF` 主體 + 1px 亮白/桃紅折射微邊），兩者色相相近但飽和度與邊框處理差異巨大 |
| **強制電報（Telegraph）** | 所有 PRISMSHELL 彈幕電報時間 ≥ 0.4s（高於全域最低 0.3s 鐵則），開火前有明顯白色蓄光閃爍，提供色相之外的時間性警示 |
| **折射拖尾簽章** | 每顆晶面彈幕自帶短暫彩虹邊緣拖尾（0.1s 殘影），是玩家彈幕沒有的視覺特徵，形狀/粒子層面的額外辨識線索 |
| **狀態回饋色不受影響** | SOFTENED（暖橙 `#FF6600`）與 ARMOR_STRIPPED（白框脈動 + 倒計時條）等**部位狀態回饋**維持全域標準暖色，不受本例外影響——只有「進攻彈幕本體顏色」破例，部位健康狀態的可讀性系統完全不變 |

> 此例外因偏離全域鐵則，需要額外的可讀性驗證（見第 10 節 AC-06），在 playtest 中明確確認玩家能在 0.5s 內分辨敵彈與己彈。

### ARMOR_INTACT vs ARMOR_STRIPPED / SOFTENED 視覺區隔

- **ARMOR_INTACT + INTACT（未軟化）**：晶面呈完整深藍紫多面體，持續公轉折射光扇
- **SOFTENED（任一雷射蓄熱達 theta_S）或 ARMOR_STRIPPED（L3 剝甲）**：晶面外殼出現裂紋，折射光扇**暫停**（`FireGate: SilenceWhenSoftened`，見第 5 節），晶面本身仍持續公轉但改以暗淡脈動呈現；同時緊鄰的稜隙核縫隙露出亮白色針尖標記（`HitGate` 開啟，見第 4 節）
- **weak_node 命中窗開啟時**：稜隙核以持續亮白脈動 + 極窄外框標示，明確區別於「隱藏中」的暗淡不可命中狀態

---

## 4. 部位組成 (Part Composition)

### 部位總表

| 部位 ID | 名稱 | 類型 | H_max | B_max | 相鄰部位 | drop_table_id | 弱點可見性 / 特殊機制 |
|---------|------|------|-------|-------|----------|--------------|----------|
| `prism_core` | 稜心核 | BOSS_CORE | 200 HU（預設）| 200 BU（預設）| facet_a, facet_b, facet_c, facet_d | `drop_prismshell_core` | 永遠可見（明顯多刻面亮白紫標記）|
| `facet_a` | 晶面·北 | ARMORED | 150 HU（預設）| 150 BU（預設）| prism_core, facet_b, facet_d, weak_node | `drop_prismshell_facet` | 弱點隱藏（標準 ARMORED 規則，見 kaiju-part-system.md C.4）|
| `facet_b` | 晶面·東 | ARMORED | 150 HU（預設）| 150 BU（預設）| prism_core, facet_a, facet_c, weak_node | `drop_prismshell_facet` | 同上 |
| `facet_c` | 晶面·南 | ARMORED | 150 HU（預設）| 150 BU（預設）| prism_core, facet_b, facet_d | `drop_prismshell_facet` | 同上 |
| `facet_d` | 晶面·西 | ARMORED | 150 HU（預設）| 150 BU（預設）| prism_core, facet_c, facet_a | `drop_prismshell_facet` | 同上 |
| `weak_node` | 稜隙核 | NORMAL | 100 HU（預設，不覆寫）| **40 BU（覆寫，`B_max_override`）** | facet_a, facet_b, prism_core | `drop_prismshell_node` | **HitGate 特殊機制**（見下方 4.1）；隱藏中完全不可命中，開窗後永遠可見 |

**H_max / B_max 全部使用全域旋鈕預設值，僅 `weak_node` 的 B_max 覆寫為 40 BU**——「針尖大的弱核」設計意圖：一旦軟化，任何熱衝擊引爆飛彈幾乎可一擊擊破（見第 7 節 TTB 驗算），把「開窗」的難度留在裝甲管理階段，把「擊破」留成一次乾淨俐落的獎勵時刻。

### 4.1 稜隙核 HitGate 機制（本 Boss 專屬擴充規則）

`weak_node` 是 NORMAL 部位，但額外附帶一個**命中閘門（HitGate）**——一個本文件為 PRISMSHELL 新增的部位級布林條件，獨立於 kaiju-part-system.md 既有的 ARMORED 護甲規則（因為 `weak_node` 本身不是 ARMORED，不持有 `armor_state`）：

```
HitGate(weak_node) = OPEN   if 任一 { facet_a, facet_b } 滿足：
                              heat_state == SOFTENED
                              OR armor_state == ARMOR_STRIPPED
                              OR break_state == BROKEN
                     = CLOSED  otherwise（初始狀態）
```

- **CLOSED（閘門關閉）**：`weak_node` 完全不可命中——雷射與飛彈命中一律視為擊中晶面本體外殼，`heat_delta` 與 `break_delta_base` 均不生效（H_current / B_current 凍結，不清零、不累積，維持上次數值），視覺上稜隙核呈暗淡不可見
- **OPEN（閘門開啟）**：`weak_node` 恢復標準 NORMAL 部位規則——雷射正常蓄熱（H_rate 依武器）、飛彈依 D.3 標準 M_state_mult 查表填充 BU；閘門開啟期間所有累積的 H_current / B_current 沿用先前凍結的數值繼續計算（不重置），與 kaiju-part-system.md E.2「BU 跨窗口保留」精神一致
- **設計意圖**：`weak_node` 的 `adjacency_list`（`[facet_a, facet_b, prism_core]`）同時身兼兩種用途——(1) 標準 Tier-3 相鄰效果的目標清單（L2 破點漣漪 / M3 穿甲爆破鏈可觸達 `weak_node` 與 `prism_core`）；(2) HitGate 的條件來源清單（僅檢查 `facet_a` 與 `facet_b`，因為 `weak_node` 在晶面環上的相位正好落在這兩片晶面之間，見 4.2 公轉佈局）。這是「縫隙」隱喻的具體資料實作：玩家必須精準理解「哪兩片晶面夾住了稜隙核」，而非漫無目的地破壞任意晶面
- **Schema 影響**：本機制需要在 `PartDef` 新增一個可選欄位 `HitGate`（型別建議：引用 `adjacency_list` 的子集 + 所需狀態條件，類似現有 `FireGate` 但用於「是否可被命中」而非「是否發射」）。此為新增可選欄位，預設 `null`（=永遠可命中，向後相容），不影響既有 3 隻頭目與 448 條 EditMode 測試。詳見第 6 節 Dependencies。

### 4.2 空間佈局與公轉幾何（ASCII 示意）

```
              螢幕頂部
      ─────────────────────────────
      │                           │
      │        facet_a (北)       │   ← 0°，半徑 100px
      │       ↗ 順時針公轉 ↘       │
      │  facet_d          facet_b │   ← 270° / 90°
      │  (西)   [prism_core]  (東)│      weak_node 相位 45°
      │           核心固定        │      （夾在 facet_a 與 facet_b 之間，
      │       ↘             ↗    │       半徑 55px，較晶面環更靠內）
      │        facet_c (南)       │   ← 180°
      │                           │
      ─────────────────────────────
        螢幕底部（玩家區）
```

**L4 穿透雷射的常態化垂直窗口**（呼應 weapon-system.md E.3「每個 Boss 須保證至少 1 組垂直對齊部位」）：由於 4 片晶面固定相距 90°，`facet_a`↔`facet_c` 與 `facet_b`↔`facet_d` **恆為兩條互相垂直的直徑**，隨著公轉持續旋轉。當任一直徑掃過螢幕垂直軸（Y 軸）的瞬間，L4 穿透雷射沿垂直方向單發即可同時命中「2 片晶面 + `prism_core`」共 3 個部位。以基礎公轉速度 15°/s 計算，兩條直徑交替每 90° 掃過垂直軸一次 → **理論上每 6 秒即出現一次三部位穿透窗口**，比 CARAPEX 的單次靜態對齊更常態化（但主展示武器仍留給 VOLTWYRM，見第 7 節）。

**Phase 2 後的窗口變化**：若玩家優先破壞相鄰晶面（如 `facet_a` + `facet_b`），剩餘的 `facet_c` + `facet_d` 不再構成完整直徑，垂直穿透窗口消失；若破壞對角晶面（如 `facet_a` + `facet_c`），剩餘 `facet_b` + `facet_d` 仍構成一條完整直徑，窗口保留但頻率減半（僅剩一條直徑，每 12s 一次，公轉加速後約每 7.5s 一次）。這是刻意保留給玩家的**破壞順序策略深度**，不強制要求，僅作為隱性獎勵。

### `assets/data/kaiju/prismshell.yaml`

```yaml
kaiju_id: "prismshell"
display_name_zh: "稜殼獸"
display_name_en: "PRISMSHELL"
kaiju_tier: 3
theme: "Crystal"          # KaijuTheme 新增值；每部位掉 core_crystal
role: "precision_boss"

body_movement:
  pattern: "vertical_drift"
  amplitude_screen_pct: 4       # ±4% 螢幕高度，緩慢呼吸感
  speed_cycles_per_min: 10

parts:
  - id: "prism_core"
    type: BOSS_CORE
    H_max_override: null          # 全域預設 200 HU
    B_max_override: null          # 全域預設 200 BU
    adjacency: ["facet_a", "facet_b", "facet_c", "facet_d"]
    drop_table_id: "drop_prismshell_core"
    movement:
      type: "stationary_relative"   # 相對軀幹靜止，是所有晶面公轉的 pivot
      note: "所有晶面與 weak_node 均以此為圓心公轉"

  - id: "facet_a"
    type: ARMORED
    H_max_override: null            # 全域預設 150 HU
    B_max_override: null            # 全域預設 150 BU
    adjacency: ["prism_core", "facet_b", "facet_d", "weak_node"]
    drop_table_id: "drop_prismshell_facet"
    movement:
      type: "orbit"
      pivot_part: "prism_core"
      radius_px: 100
      speed_deg_per_s: 15.0          # 基礎公轉速度（旋鈕 prismshell_orbit_speed_base）
      phase_deg: 0.0                 # 北位起始
    fire_gate: "SilenceWhenSoftened"  # 軟化或剝甲時暫停折射（見第 5 節）
    design_note: >
      facet_a 與 facet_b 是 weak_node 的 HitGate 條件來源
      （見文件 §4.1）——軟化或剝甲任一片即開啟 weak_node 命中窗。

  - id: "facet_b"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["prism_core", "facet_a", "facet_c", "weak_node"]
    drop_table_id: "drop_prismshell_facet"
    movement:
      type: "orbit"
      pivot_part: "prism_core"
      radius_px: 100
      speed_deg_per_s: 15.0
      phase_deg: 90.0                # 東位起始
    fire_gate: "SilenceWhenSoftened"

  - id: "facet_c"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["prism_core", "facet_b", "facet_d"]
    drop_table_id: "drop_prismshell_facet"
    movement:
      type: "orbit"
      pivot_part: "prism_core"
      radius_px: 100
      speed_deg_per_s: 15.0
      phase_deg: 180.0               # 南位起始
    fire_gate: "SilenceWhenSoftened"

  - id: "facet_d"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["prism_core", "facet_c", "facet_a"]
    drop_table_id: "drop_prismshell_facet"
    movement:
      type: "orbit"
      pivot_part: "prism_core"
      radius_px: 100
      speed_deg_per_s: 15.0
      phase_deg: 270.0               # 西位起始
    fire_gate: "SilenceWhenSoftened"

  - id: "weak_node"
    type: NORMAL
    H_max_override: null            # 全域預設 100 HU（theta_S=100 正常適用）
    B_max_override: 40              # 覆寫：針尖弱核，B_max 遠低於標準 100 BU
    adjacency: ["facet_a", "facet_b", "prism_core"]
    drop_table_id: "drop_prismshell_node"
    movement:
      type: "orbit"
      pivot_part: "prism_core"
      radius_px: 55                 # 較晶面環更靠內，象徵「藏在縫隙裡」
      speed_deg_per_s: 15.0         # 與晶面環同步（隨 Phase 加速一併調整）
      phase_deg: 45.0               # 夾在 facet_a(0°) 與 facet_b(90°) 之間
    hit_gate:
      condition: "ANY"
      sources: ["facet_a", "facet_b"]
      required_states: ["SOFTENED", "ARMOR_STRIPPED", "BROKEN"]
    design_note: >
      HitGate 關閉時 weak_node 完全不可命中（雷射/飛彈無效，H/B 凍結不清零）；
      開啟時比照標準 NORMAL 部位規則運作。B_max=40 使其一旦軟化，
      單發 M3 熱衝擊引爆（60 BU）即可一擊擊破，見文件 §7 TTB 驗算。
```

---

## 5. 攻擊模式 (Attack Patterns)

**全域規則**：所有子彈遵守「彈幕永遠讀得懂」視覺鐵則，惟本 Boss 的彈幕色系刻意破例走冷色（見第 3 節「冷色例外」）。子彈速度跨四難度階**恆定**；僅彈數與射速依難度縮放（見第 8 節）。

---

### 模式 A：晶面折射光扇 (Facet Refraction Fan)

| 屬性 | 值 |
|------|-----|
| **發射源** | `facet_a` / `facet_b` / `facet_c` / `facet_d`（各自獨立計時，互不同步）|
| **彈形** | Radial 5 發扇形（100° 總擴散角，朝晶面當前公轉外側方向噴發）|
| **子彈速度** | 160 px/s（冷色高速威脅，較 CARAPEX 基準 90–120 px/s 更快，靠更長電報維持可讀性）|
| **射頻（D1）** | 每晶面 1 次/2.0s（4 片獨立 → 玩家平均每 0.5s 見一次某片晶面開火）|
| **電報** | 晶面開火前 0.4s 亮白蓄光閃爍（高於全域最低 0.3s 鐵則）|
| **子彈色** | 電光紫藍 `#6A3DFF`，1px 亮白/桃紅折射微邊 + 0.1s 彩虹拖尾殘影 |
| **觸發條件** | `facet_[x]` 存活 AND `heat_state == INTACT` AND `armor_state == ARMOR_INTACT`；`FireGate: SilenceWhenSoftened` — 只要該晶面進入 SOFTENED 或 ARMOR_STRIPPED 即暫停折射（不消音，是暫停，狀態退回後恢復）；`BROKEN` 則永久停止 |
| **設計目的** | 教「讀懂緩慢但規律的公轉節奏」；軟化/剝甲晶面帶來的「暫停折射」是立即可感知的雙重獎勵——彈幕變少 **且** 稜隙核縫隙開啟 |

---

### 模式 B：稜心十字精準雷 (Core Cross Beam)

| 屬性 | 值 |
|------|-----|
| **發射源** | `prism_core` |
| **彈形（Phase 1–2）** | Aimed 4 發窄雷：以玩家當下位置為基準瞄準一發，另 3 發依 90°/180°/270° 偏移形成固定十字，鎖定後不再追蹤 |
| **彈形（Phase 3：4 片晶面均 BROKEN）** | D1/D2 維持 4 窄十字；D3/D4 擴增為 8 方向（十字 + 對角），呼應 CARAPEX 模式 C 的密度升級手法 |
| **子彈速度** | 140 px/s |
| **射頻（D1，Phase 1–2）** | 1 次/5.0s |
| **射頻（D1，Phase 3）** | 1 次/3.0s |
| **子彈色** | 桃紫白 `#CC66FF`，比晶面彈幕更亮更飽和，讓玩家一眼區分「核心開火」與「晶面開火」|
| **電報** | 核心整體亮白蓄光脈動 0.6s 後發射 |
| **觸發條件** | 始終啟用；Phase 依存活晶面數切換密度 |
| **設計目的** | 核心全程維持存在感（不可被無視），Phase 3 密度上升具體化「晶面盡毀、核心獨自承擔全部威脅」的敘事張力 |

---

### 模式 C：共鳴爆閃 (Prism Resonance Ring)

| 屬性 | 值 |
|------|-----|
| **發射源** | `prism_core`（匯聚存活晶面折射能量）|
| **彈形** | RingBurst：以核心為中心的整圈放射彈幕，彈數依存活晶面數量動態調整（見下方公式）|
| **子彈速度** | 130 px/s |
| **射頻（D1）** | 1 次/12s（稀有的高張力釋放時刻，服務「緊繃→釋放」sawtooth 節奏）|
| **彈數公式** | `count = clamp(base_count − 3 × broken_facet_count, floor_count, base_count)`；D1：`base_count=12, floor_count=4` |
| **電報** | 1.0s 全晶面 + 核心同步亮白蓄光（本 Boss 最長電報，最不可能被忽略）|
| **子彈色** | 電光紫藍（同模式 A）+ 電報瞬間全域白色閃光 |
| **觸發條件** | `prism_core` 存活即持續觸發，不受晶面存活數影響（僅影響彈數，見上）|
| **設計目的** | 提供直接可量化的「破壞=獎勵」回饋——每破一片晶面，下一次共鳴爆閃就少 3 發，玩家能明確感受到自己的破壞如何削弱 Boss 的終極招式 |

### 模式觸發條件彙總

| 模式 | 啟動 | 暫停 / 停止 |
|------|------|------|
| A 晶面折射光扇 | 對應晶面 ALIVE + ARMOR_INTACT + INTACT | 軟化/剝甲時暫停（`SilenceWhenSoftened`）；`BROKEN` 永久停止 |
| B 稜心十字精準雷 | 始終 | `prism_core` BROKEN（戰鬥結束）|
| C 共鳴爆閃 | `prism_core` 存活 | `prism_core` BROKEN；彈數隨晶面破壞遞減至 floor |

---

## 6. 階段 (Phases)

階段由**部位破壞狀態**驅動，非血量閾值，落實「破壞改變戰鬥」設計哲學。

### Phase 1：「六稜初綻形態」 *(4 片晶面 ALIVE)*

- **觸發**：戰鬥開始
- **攻擊組合**：模式 A（4 片晶面獨立折射）+ 模式 B（低密度，1/5.0s）+ 模式 C（基礎密度，12 發/12s）
- **設計意圖**：玩家熟悉公轉節奏，並發現「軟化任一晶面 → 該面暫停折射 + 稜隙核縫隙露出」這個核心頓悟。稜隙核在此階段是「額外獎勵」而非必要目標
- **核心爽點時刻**：首次軟化 `facet_a` 或 `facet_b`（開啟 weak_node HitGate）→ 切換瞄準稜隙核 → M3 熱衝擊引爆一擊擊破，應在玩家首輪遊玩約 2–4 分鐘內發生

### Phase 2：「破稜警戒形態」 *(≥2 片晶面 BROKEN)*

- **觸發**：任意 2 片晶面 `break_state == BROKEN`
- **攻擊組合**：剩餘晶面模式 A（公轉速度 × `prismshell_phase2_orbit_speed_mult` = 1.6，見第 8 節旋鈕）+ 模式 B（維持 Phase1-2 密度）+ 模式 C（彈數依剩餘晶面數遞減）
- **設計意圖**：剩餘晶面公轉加快，逼玩家更精準地掐時機軟化/剝甲；但彈幕來源整體變少，是「破壞降壓」與「殘存威脅升級」的張力並存
- **教學關鍵點**：若玩家先破了 `facet_a`/`facet_b`（weak_node 的 HitGate 來源），稜隙核從此**永久保持可命中**（因為 BROKEN 狀態也滿足 HitGate 條件）——這是鼓勵玩家優先破壞這兩片晶面的隱性引導，但非強制

### Phase 3：「裸核終焰形態」 *(4 片晶面均 BROKEN)*

- **觸發**：`facet_a` 至 `facet_d` 全數 `BROKEN`
- **攻擊組合**：模式 A 完全停止（無晶面可發射）；模式 B 切換至高密度（1/3.0s，D3/D4 增至 8 方向）；模式 C 以 floor 彈數（4 發）持續
- **設計意圖**：晶面盡毀後，`prism_core` 獨自承擔全部威脅，彈幕組成變得乾淨但精準要求提高（十字精準雷密度上升）。`weak_node` 此時必然已進入永久 HitGate OPEN（因至少一片晶面 BROKEN），若尚未破壞可隨時補刀
- **可選部位張力**：玩家可選擇跳過部分晶面直攻 `prism_core`（BOSS_CORE 才是唯一勝利條件），但會犧牲晶面/稜隙核的素材產出——「頭目是靈魂」支柱的核心張力在此具體化為「要不要拆完整座水晶陣」的選擇

---

## 7. 剋制與偏好 Loadout (Weapon Affinity)

### 武器表現速覽

| 武器 | PRISMSHELL 表現 | 原因 |
|------|------------|------|
| **L2 集束雷射** | ★★★ 最佳主武器 | 37.5 HU/s 單體最快蓄熱，開任一晶面窗口最快；窄判定框最適合狙擊移動中的稜隙核與最終稜心核；本 Boss 展示 Loadout 主武器 |
| **M3 穿甲魚雷** | ★★★ 最佳副武器 | 晶面公轉緩慢（15°/s）可預判落點；熱衝擊引爆（60 BU）對稜隙核（B_max=40）近乎一擊必殺；是本 Boss「精準蓄熱-引爆」展示路線的關鍵拼圖 |
| **L3 波動砲** | ★★★ 高效專門工具（非唯一路徑）| 蓄力震波可瞬間剝甲任一晶面並附帶 2 秒 1.5× 高效窗口，是開窗最快的手段——但任何雷射靠軟化貫穿（kaiju-part-system.md C.4）一樣能開甲，L3 只是效率最高、非必要 |
| **M1 追蹤飛彈** | ★★★ 穩定可靠 | 自動追蹤持續公轉的晶面，免去手動預判角度；對稜隙核的短暫窗口一樣能自動修正瞄準 |
| **L1 散波雷射** | ★★ 平行開窗 | 三束同時軟化多片晶面（各 1/3 速率），能同時鋪開窗機會，但單面軟化較慢；對稜隙核極小判定框而言，散射多半打偏 |
| **M2 蜂群飛彈** | ★★ 廣域但效率分散 | 多發同時覆蓋數片晶面，方便清點狀進度；但對稜隙核短暫窗口的單點瞄準效率不足，個別命中填充量較低 |
| **L4 穿透雷射** | ★★ 常態化利基 | 利用晶面「恆為 90° 相距」的幾何特性，約每 6 秒出現一次同時穿透 2 晶面 + 核心的垂直窗口（見第 4.2 節），比 CARAPEX 更常態化，但真正主展示留給 VOLTWYRM（L4 主剋制頭目）|
| **M4 叢集炸彈** | ★ 最弱情境 | AoE 落點固定，但晶面與稜隙核持續公轉，容易滾出爆炸半徑；對稜隙核這種瞬時小窗口目標尤其低效；主展示留給 BROODCORE（M4 主剋制頭目）|

### 展示 Loadout：L2 × M3「精準開窗-引爆」

本 Loadout 是 PRISMSHELL 的**設計展示組合**，最直觀地展現「裝甲管理 + 精準狙擊」雙重教學。

**戰鬥序列（L2 × M3，D1）**：

```
T = 0s      ：L2 開始集束照射 facet_a（H_rate = 37.5 HU/s）
T ≈ 5–7s    ：facet_a.H_current 達 100 HU → SOFTENED
              → facet_a 折射暫停（FireGate: SilenceWhenSoftened）
              → weak_node.HitGate 條件滿足 → OPEN（稜隙核亮起）
T ≈ 7–9s    ：玩家切換 L2 瞄準線至 weak_node（同一晶面環半徑內側，追瞄難度低）
T ≈ 10–12s  ：weak_node.H_current 達 100 HU → SOFTENED
T ≈ 12–13s  ：發射 1 枚 M3 → 熱衝擊引爆：B_fill = 60 BU
              → B_current = clamp(0+60, 0, 40) = 40 → 達 required_break_threshold(40)
              → weak_node BROKEN！素材掉落（core_crystal × 1, shard_common × 3 [Precision]）
T ≈ 13–20s  ：玩家轉回 facet_a，繼續 M3 熱衝擊引爆（60 BU/發）：
              第 1 發 = 60 BU；第 2 發 = 120 BU；第 3 發 = 180 ≥ 150 → facet_a BROKEN！
```

**TTB 驗算**：
- `weak_node`：理論 ~13s（含開窗等待）；由於 B_max 刻意壓低至 40，屬設計上的「快速獎勵型」目標，明顯低於標準 NORMAL 15–25s 目標區間——此為刻意例外，見第 9 節旋鈕說明
- `facet_a`（ARMORED）：理論 ~9–11s（軟化 ~7s + 3 發 M3 ~2s）；實戰含公轉追瞄與閃避 downtime 約 25–35s，落在 kaiju-part-system.md D.3 ARMORED 目標區間（30–45s）下緣 ✓

### 無 L2×M3 Loadout 仍可通關（公平性保證）

PRISMSHELL 在 D1 對**所有合法 Loadout** 均可通關，不存在強制解：

| Loadout 範例 | 策略路線 | 預估 TTB（單片晶面，D1）|
|-------------|---------|-------------------|
| L2 × M3 | 精準開窗-引爆（展示路線）| ~25–35s |
| L1 × M2 | 廣域平行軟化多片晶面，蜂群逐一清點 | ~35–42s |
| L2 × M1 | 集束開窗 + 追蹤穩定填充 | ~28–36s |
| L3 × M4 | L3 快速剝甲開窗；M4 AoE 掃過核心區域 | ~30–38s（晶面），惟稜隙核命中率明顯較低 |
| 任意 × M1 | 穩定但視主雷射蓄熱速率而定 | ~30–40s（仍在目標範圍內）|

**M4 特化情境**：即使 M4 對稜隙核效率最低，玩家仍可完全略過稜隙核、只集火晶面與核心通關——這正是「optional parts 是張力，不是強制」設計意圖的體現。

### 與 LACERA 的教學反差（8 頭目陣容的 Loadout 生態定位）

| Boss | 設計展示 Loadout | 核心教學反差 |
|------|---------------------|--------------|
| **#02 LACERA** | L2 × M1（專注-放心）| 「讓追蹤飛彈幫你處理移動目標，你只需要專心閃避」——依賴輔助 |
| **#05 PRISMSHELL（本文）**| L2 × M3（精準開窗-引爆）| 「相信自己的準頭，手動掐準每一次開窗與引爆」——純粹技術 |

兩隻頭目都用到 L2 集束雷射，但教學意圖刻意相反：LACERA 教「善用系統輔助」，PRISMSHELL 教「信任手動精準」。這組反差完整覆蓋了「橫向選擇」支柱下 L2 的兩種使用哲學。

---

## 8. 難度縮放 (Difficulty Scaling)

**縮放原則**：僅調整子彈密度（數量與射速）與模式 C 的彈數公式基準值，不改變子彈速度、部位數值、公轉速度或 HitGate 條件。完全服從 `kaiju-part-system.md` C.8 難度不縮放規則。

### 模式 A：晶面折射光扇

| 難度 | 每次每晶面彈數 | 每晶面射頻 |
|------|--------------|-----------|
| D1 Normal | 4 | 1/2.0s |
| D2 Hard | 5 | 1/1.8s |
| D3 Extreme | 6 | 1/1.6s |
| D4 Nightmare | 7 | 1/1.4s |

### 模式 B：稜心十字精準雷

| 難度 | Phase 1–2 彈形 | Phase 1–2 射頻 | Phase 3 彈形 | Phase 3 射頻 |
|------|--------------|--------------|------------|------------|
| D1 | 4 窄十字 | 1/5.0s | 4 窄十字 | 1/3.0s |
| D2 | 4 窄十字 | 1/4.5s | 4 窄十字 | 1/2.6s |
| D3 | 4 窄十字 | 1/4.0s | 8 方向（十字+對角）| 1/2.2s |
| D4 | 4 窄十字 | 1/3.5s | 8 方向（十字+對角）| 1/1.8s |

### 模式 C：共鳴爆閃

| 難度 | base_count（4 晶面全存活）| floor_count | 每破 1 晶面遞減 | 射頻 |
|------|---------------------------|-------------|----------------|------|
| D1 | 12 | 4 | −3 | 1/12s |
| D2 | 14 | 4 | −3 | 1/11s |
| D3 | 16 | 4 | −3 | 1/10s |
| D4 | 18 | 4 | −3 | 1/9s |

> 每破 1 晶面遞減量（−3）與 floor_count（4）為狀態驅動的固定旋鈕，不隨難度縮放；僅 base_count 與射頻隨難度變化。

### Phase 2 公轉加速（各難度恆定）

Phase 2 觸發時，剩餘晶面公轉速度 × `prismshell_phase2_orbit_speed_mult`（預設 1.6，範圍 1.3–2.0），此係數不隨難度縮放，存於外部旋鈕，不硬編碼。

### 恆定不縮放的所有項目

| 項目 | 原因 |
|------|------|
| 部位 H_max / B_max（含 weak_node 的 40 BU 覆寫）| 部位系統難度不縮放（kaiju-part-system.md C.8）|
| 晶面公轉速度（15°/s 基礎、1.6× Phase2 加速）| 移動節奏縮放會破壞 L4 垂直窗口的可預期性與教學一致性 |
| weak_node HitGate 條件 | 開窗邏輯不因難度改變，確保教學路徑跨難度一致 |
| 子彈速度（模式 A：160 px/s；模式 B：140 px/s；模式 C：130 px/s）| 速度恆定，僅密度縮放 |
| 素材掉落數量與品質 | 難度不影響素材系統（kaiju-part-system.md C.8）|

---

## 9. 素材產出 (Material Drops)

### 掉落表定義

| drop_table_id | 對應部位 | 部位類型 | 核心素材 | shard_common（Standard / Precision / Perfect）|
|--------------|---------|---------|---------|----------------------------------------------|
| `drop_prismshell_facet` | facet_a/b/c/d | ARMORED | `core_crystal` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_prismshell_node` | weak_node | NORMAL | `core_crystal` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_prismshell_core` | prism_core | BOSS_CORE | `core_crystal` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |

*PRISMSHELL 屬晶簇系（kaiju_prismshell）巨獸，所有 6 個部位均掉落 `core_crystal`（依 roster-overview.md §1.5 主題核心映射規則）。碎片計算沿用 material-economy.md D.1：`shard_base = 2`，Standard = 1.0×，Precision = 1.5×（floor=3），Perfect = 2.0×（= 4）。*

> **待對齊事項**：`core_crystal` 對應的武器升級目標（如 CARAPEX 的 `core_carapace` → L1/M2/M4）尚未在 material-economy.md 中定義——該文件目前僅涵蓋前 3 隻已鎖定頭目的主題映射（`core_carapace`／`core_limb`／`core_energy` 恰好互斥覆蓋全部 8 種武器）。新增 5 個主題（含 `core_crystal`）需要 material-economy 負責人決定：是延伸為多對多映射（多個主題可餵同一武器），還是重新切分。本文件僅保證 `core_crystal` 正確掉落，武器對映留待 material-economy.md 更新後補齊（見第 10 節 Dependencies）。

### 一場狩獵預期產出（D1，Precision 品質為主）

| 通關策略 | core_crystal | shard_common |
|---------|--------------|-------------|
| **全破壞**（6 部位全破）| 6 | 18 + 5 完成獎勵 = **23** |
| **僅破核心**（Speed Run）| 1 | 3 |
| **4 晶面 + 核心**（跳過稜隙核）| 5 | 15 |
| **2 晶面 + 稜隙核 + 核心** | 4 | 12 |

### `essence_kaiju` 產出

`on_hunt_end(is_all_parts_broken = true)` 時觸發（6 個部位均 BROKEN）→ `essence_kaiju` × 1 + `shard_completeness_bonus`（= 5 碎片）。任何難度階均適用，無難度門檻。

---

## 10. 驗收標準 (Acceptance Criteria)

### AC-01 精準教學循環感知（體驗性 — 阻斷）

- [ ] 5 人用戶測試（已完成 CARAPEX/LACERA 教學）：首輪 D1 通關後，受測者能不提示描述「軟化晶面 → 稜隙核現形 → 換武器精準狙擊」核心循環，達成率 ≥ 70%
- [ ] 首次 `weak_node` HitGate 開啟事件發生於遊戲開始後 4 分鐘內（記錄時間戳）
- [ ] 受測者能在 5 分鐘內自行發現「破壞 `facet_a`/`facet_b` 是稜隙核開窗的關鍵」（無需明確提示）

### AC-02 ARMORED 晶面護甲閘門正確性（功能性 — 阻斷）

- [ ] `facet_a–d` ARMOR_INTACT + INTACT 期間：任何飛彈命中 `B_fill = 0`（偏轉動畫觸發）
- [ ] 任一雷射蓄熱使晶面達 `heat_state == SOFTENED` 後：飛彈命中依 M_state_mult = 1.0 正常填充（軟化貫穿路徑，無需 L3）
- [ ] L3 蓄力震波命中晶面 → `armor_state = ARMOR_STRIPPED`，`stagger_timer = 2.0s`，飛彈填充 M_state_mult = 1.5
- [ ] `FireGate: SilenceWhenSoftened` 正確：晶面進入 SOFTENED 或 ARMOR_STRIPPED 時模式 A 立即暫停；狀態退回 INTACT + ARMOR_INTACT 時模式 A 恢復
- [ ] 自動化測試：`tests/unit/part-system/prismshell_facet_fire_gate_test`

### AC-03 稜隙核 HitGate 正確性（功能性 — 阻斷，新機制）

- [ ] HitGate CLOSED 時：`weak_node` 的任何 `on_laser_hit` / `on_missile_hit` 事件均不改變 H_current / B_current（凍結，不清零）
- [ ] `facet_a` 或 `facet_b` 任一進入 SOFTENED / ARMOR_STRIPPED / BROKEN → HitGate 立即切換 OPEN（同幀生效）
- [ ] HitGate OPEN 後 `weak_node` 依標準 NORMAL 部位規則運作（D.1–D.3 公式）
- [ ] `facet_a` 與 `facet_b` 均恢復 ARMOR_INTACT + INTACT 且均未 BROKEN 時，HitGate 切回 CLOSED（H/B 凍結於當前值，不清零）
- [ ] 一旦 `facet_a` 或 `facet_b` 任一 BROKEN，HitGate 永久保持 OPEN（不因另一片恢復完整而關閉）
- [ ] 自動化測試：`tests/unit/part-system/prismshell_weak_node_hit_gate_test`（覆蓋 OPEN/CLOSED 全轉換路徑 + 永久開啟情境）

### AC-04 BOSS_CORE 勝利條件（功能性 — 阻斷）

- [ ] `prism_core` B_current ≥ 200 BU → BROKEN → `on_boss_core_break` 發出 → 勝利結算啟動
- [ ] 即使 4 晶面與 `weak_node` 均 ALIVE，`prism_core` 破壞仍觸發勝利
- [ ] 勝利結算前 `on_part_break`（BOSS_CORE）必先於 `on_boss_core_break` 發出

### AC-05 素材掉落正確性（功能性 — 阻斷）

- [ ] `drop_prismshell_facet` × 4（晶面）：各掉 `core_crystal` + `shard_common`（依品質乘數）
- [ ] `drop_prismshell_node`：掉 `core_crystal` + `shard_common`（B_max=40 的覆寫不影響掉落倍率計算）
- [ ] `drop_prismshell_core`：掉 `core_crystal` + `shard_common`
- [ ] 全破壞結算（6 部位）：`essence_kaiju` × 1 + `shard_completeness_bonus`（= 5）
- [ ] 自動化測試：`tests/unit/economy/material_yield_quality_test`（擴充覆蓋 3 個新 drop_table × 3 品質等級）

### AC-06 彈幕可讀性與冷色例外驗證（體驗性 — UX 阻斷）

- [ ] 5 人用戶測試：含 PRISMSHELL 各模式彈幕截圖中，受測者辨識「敵彈（電光紫藍）vs 己彈（淡冰藍）」準確率 ≥ 80%（因色相相近，此項為額外驗證重點）
- [ ] SOFTENED 晶面的暖橙 `#FF6600` 狀態回饋在 D4 最高密度下仍可辨識（不受冷色例外影響）
- [ ] `weak_node` HitGate OPEN 時的亮白脈動標記在 D4 彈幕密度下清晰可見，不被彈幕完全遮蓋
- [ ] 模式 C 共鳴爆閃的 1.0s 電報在截圖靜態測試中被 ≥ 90% 受測者正確識別為「即將發生大型攻擊」（因是全 Boss 最長電報，門檻設定高於一般模式）

### AC-07 階段轉換與公轉加速正確性（功能性）

- [ ] 任意 2 片晶面 BROKEN → 剩餘晶面公轉速度 × `prismshell_phase2_orbit_speed_mult`（1.6），`weak_node` 若仍存活則同步調整公轉速度以維持相位鎖定
- [ ] 4 片晶面均 BROKEN → 模式 A 完全停止；模式 B 切換 Phase 3 密度；模式 C 彈數降至 floor_count
- [ ] `prismshell_phase2_orbit_speed_mult` 存於外部旋鈕，不硬編碼
- [ ] 模式 C 彈數公式 `clamp(base_count − 3×broken_facet_count, floor_count, base_count)` 正確反映即時晶面破壞數

### AC-08 難度密度縮放正確性（功能性）

- [ ] 子彈速度（模式 A：160 px/s；模式 B：140 px/s；模式 C：130 px/s）與晶面公轉速度（15°/s 基礎）在 D1–D4 下恆定
- [ ] 各模式彈數與射頻依第 8 節表格縮放；靜態審核各難度 `spawn_config` 參數
- [ ] 部位 H_max / B_max（含 weak_node 40 BU 覆寫）在難度切換後讀取值不變（`difficulty_invariance_test` 覆蓋）

### AC-09 L2 × M3 展示 Loadout TTB 驗算（功能性）

- [ ] L2 × M3，ARMORED 晶面，D1：TTB 實測值 ∈ [25s, 40s]（kaiju-part-system.md D.3 ARMORED 目標區間附近）
- [ ] L2 × M3，`weak_node`（HitGate 已開啟情境下），D1：TTB 實測值 ∈ [8s, 18s]（刻意低於標準 NORMAL 15–25s 區間，設計上的快速獎勵型目標，非缺陷）
- [ ] 包含在 64 組 loadout TTB 矩陣自動化測試中（`tests/unit/weapon/weapon_loadout_matrix_test`）

---

## 附錄：Schema 待辦事項 (Dependencies / Schema Impact)

本文件引入 2 項尚未正式落地的 `PartDef` 擴充欄位，需與 roster-overview.md §4 的既有擴充計畫一併實作：

1. **`PartDef.HitGate`（新增，可選）**：布林命中閘門，引用同巨獸內其他部位的 `adjacency_list` 子集 + 所需狀態組合（`SOFTENED` / `ARMOR_STRIPPED` / `BROKEN`）。預設 `null`（= 永遠可命中），向後相容既有 3 隻頭目與 448 條 EditMode 測試。目前僅 `weak_node` 使用（見第 4.1 節）
2. **`PartMovement.Orbit`**：pivot（可為另一部位 ID，而非固定世界座標）+ `radius_px` + `speed_deg_per_s` + `phase_deg`。與 LACERA 既有 `sweep_arc` / `oscillate` 並列為 `PartMovement` 的第三種具體型態，對齊 roster-overview.md §4 的 `PartMovement` 擴充計畫
3. **`KaijuTheme.Crystal`**：新增列舉值，`EconomyConfig` 需補上 `Crystal → core_crystal` 映射（material-economy.md 待更新，見第 9 節「待對齊事項」）

以上 3 項均為向後相容擴充（新欄位可選、預設 null/None），不影響既有已鎖定內容。

---

*文件版本：1.0.0*
*作者：Game Designer Agent*
*最後更新：2026-07-08*
*資料定義：`assets/data/kaiju/prismshell.yaml`（inline 見第 4 節）*
*依賴掉落表 ID：`drop_prismshell_core` / `drop_prismshell_facet` / `drop_prismshell_node`（由 material-economy 實作）*
