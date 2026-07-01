# 熾蛇 / VOLTWYRM — 頭目設計文件
## 殲獸戰機 / KAIJU BREAKER · Boss #03

*文件路徑：`design/gdd/kaiju/03-voltwyrm.md`*
*最後更新：2026-07-01*
*狀態：Draft*
*相依文件：game-concept.md | weapon-system.md（LOCKED）| kaiju-part-system.md | material-economy.md*
*YAML 資料定義：`assets/data/kaiju/voltwyrm.yaml`（由本文件第 4.3 節 inline 提供）*

---

## 1. 概覽 (Overview)

熾蛇（VOLTWYRM，`kaiju_id: voltwyrm`）是三頭目陣容中的第三位，也是最終頭目：一條由高壓能量段疊成的縱向巨蛇，主體沿畫面中軸自上而下垂懸，核心（核心節 / Energy Core，`core_node`）藏於最頂端，受左右兩片能量護盾（左能量盾 `shield_left` / 右能量盾 `shield_right`）夾護。蛇身由四個縱列頸段（`neck_seg_1`~`neck_seg_4`）自核心向下延伸，構成遊戲中唯一的**完整縱向穿透走廊（Vertical Pierce Corridor）**。

**首要設計職責**：解決 `weapon-system.md` 開放問題 #2（「L4 穿透雷射的垂直部位依賴」）。四個縱列頸段使 L4 在單次發射中同時命中四個部位，實現 100 HU/s 跨鏈同步蓄熱，遠超任何其他主武器在同等時間內的多部位處理速率。雙能量護盾則召喚 L3 波動砲（Wave Cannon）的護甲剝甲能力，讓組合 C「震盪-飽和（Blast & Saturate）」達到最高效率。

本文件為**內容設計文件（Boss Content Design）**，非系統 GDD。部位數值、事件契約、材料產出等系統規則以相依 GDD 為準，本文件僅引用不重定義。

---

## 2. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：挑戰（Challenge）＋感官愉悅（Sensation）＋表達（Expression）

> 「這隻蛇是為我的穿透雷射量身訂做的靶。」

玩家面對的是一道「縱向方程式」：在密如螺旋銀河的彈幕中找到安全縫隙，把戰機對正蛇身長軸，按下 L4 的板機——單發能量線刺穿四個發光節段，把整條蛇同時點燃至橙紅色。那一瞬間，所有頸段同步脈動，是遊戲中視覺回饋最壯觀的「蓄熱全中」時刻。服務〔破壞即獎勵〕與〔科技對巨獸〕兩大支柱。

護盾的存在讓幻想更立體：護盾是「門」，不是牆——等到 L3 蓄力震波（Wave Cannon Charge）轟開護盾、弱點窗口（Stagger Window）出現的瞬間，M2 蜂群飛彈（Swarm Launcher）八枚齊射洗入，這是「Combo C 震盪-飽和」的最高爽點。服務「以智取勝」的核心幻想與〔頭目是靈魂〕支柱。

作為三頭目陣容的最終關卡，VOLTWYRM 承擔「彈幕掌握度驗收」的角色：難度 4（Nightmare）的最高彈幕密度在此全開，但視覺鐵則「彈幕永遠讀得懂（暖色＝威脅）」必須在最高密度下依然成立。

---

## 3. 外形與主題 (Visual Theme)

| 設計維度 | 描述 |
|---------|------|
| **體型構成** | 縱向巨蛇；全長佔畫面高度 70–80%；蛇身由像素化能量段堆疊，非生物皮膚 |
| **色彩方案** | 主體：高壓電弧黃白（極亮核心內層）→ 外緣漸變橙紅（暖色威脅）；符合視覺鐵則「冷色＝你，暖色＝威脅」 |
| **能量護盾** | 左右各一片六角形能量晶格；靜止時深紫藍色半透明（冷色，外觀有別於敵彈）；L3 震盪後轉暖橙並出現破裂裂紋（`ARMOR_STRIPPED` 視覺提示） |
| **核心節** | 位於蛇身頂端，持續脈動的白金色能量核，外包旋轉光環；BOSS_CORE 標記（判定框偏大 × 1.2，醒目，易於辨識） |
| **頸段** | 每節均有流動的能量脈衝在像素內上下移動；`SOFTENED` 時轉橙紅並加速脈動（全節同步橙紅脈動＝L4 全中的最強視覺獎勵） |
| **彈幕美學** | 所有敵彈用暖色系（橙、金、白）＋黑色像素外框，在深色背景下高對比；符合「彈幕永遠讀得懂」視覺鐵則 |
| **尺寸壓迫** | 蛇身在玩家戰機進入視野前已充滿上半部畫面，實現〔科技對巨獸〕支柱的尺寸壓迫感 |

---

## 4. 部位組成 (Part Composition)

### 4.1 部位一覽表

| `part_id` | 中文名 | 類型 | `H_max` | `B_max` | 相鄰部位 | `drop_table_id` | 說明 |
|-----------|-------|------|---------|---------|---------|----------------|------|
| `neck_seg_1` | 頸段一 | NORMAL | 100 | 100 | `neck_seg_2` | `drop_voltwyrm_seg` | 蛇身最底部，最靠近玩家；L4 穿透入口 |
| `neck_seg_2` | 頸段二 | NORMAL | 100 | 100 | `neck_seg_1`, `neck_seg_3` | `drop_voltwyrm_seg` | 縱列中段 |
| `neck_seg_3` | 頸段三 | NORMAL | 100 | 100 | `neck_seg_2`, `neck_seg_4` | `drop_voltwyrm_seg` | 縱列中段 |
| `neck_seg_4` | 頸段四 | NORMAL | 100 | 100 | `neck_seg_3`, `core_node` | `drop_voltwyrm_seg` | 核心正下方；縱列終段；破壞後 M3 鏈式可達核心 |
| `shield_left` | 左能量盾 | ARMORED | 150 | 150 | `core_node` | `drop_voltwyrm_shield` | 左側護甲閘門；唯 L3 震波可剝甲（`ARMOR_STRIPPED`） |
| `shield_right` | 右能量盾 | ARMORED | 150 | 150 | `core_node` | `drop_voltwyrm_shield` | 右側護甲閘門；同上 |
| `core_node` | 核心節 | BOSS_CORE | 200 | 200 | `neck_seg_4`, `shield_left`, `shield_right` | `drop_voltwyrm_core` | 勝利條件；頂端；受護盾夾護 |

**部位數量**：本 Boss 共 7 個部位，符合 `kaiju-part-system.md` A 節已更新的後期頭目上限（最多 8 個部位）。設計依據：VOLTWYRM 為三頭目陣容最終頭目，高部位數是其難度定位的核心表達；L4 穿透展示最少需要 4 個縱列段。

### 4.2 縱列相鄰鏈（垂直穿透走廊 / Vertical Pierce Corridor）

```
畫面上方（核心端）
┌─────────────────────────────────────────────┐
│  [shield_left]  [core_node]  [shield_right]  ← 頂端；護盾夾護核心
│                      ↕  （相鄰）
│                 [neck_seg_4]
│                      ↕
│                 [neck_seg_3]
│                      ↕
│                 [neck_seg_2]
│                      ↕
│                 [neck_seg_1]                  ← 底端；最靠近玩家
└─────────────────────────────────────────────┘
畫面下方（玩家區域）

L4 穿透雷射單次射擊路徑（由下至上）：
  ↑  neck_seg_1 → neck_seg_2 → neck_seg_3 → neck_seg_4
     護盾雙方 BROKEN 後，延伸命中 core_node
```

**關鍵設計點**：L4 穿透雷射（`l4_fire_interval` = 0.4s，`l4_h_rate` = 25 HU/s / 部位）在單次命中路徑上對四個頸段各自施加 25 HU/s，實現 **100 HU/s 跨鏈同步蓄熱**，約 4.5 秒（理論）將四段同步軟化。L2 集束雷射最快只能對單部位施加 37.5 HU/s（依序移動蓄熱所有四段需 ~12s 以上）。此差距是本 Boss 對 `weapon-system.md` 開放問題 #2 的直接設計解答。

### 4.3 `assets/data/kaiju/voltwyrm.yaml` 部位定義

```yaml
kaiju_id: "voltwyrm"
parts:
  - id: "neck_seg_1"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["neck_seg_2"]
    drop_table_id: "drop_voltwyrm_seg"

  - id: "neck_seg_2"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["neck_seg_1", "neck_seg_3"]
    drop_table_id: "drop_voltwyrm_seg"

  - id: "neck_seg_3"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["neck_seg_2", "neck_seg_4"]
    drop_table_id: "drop_voltwyrm_seg"

  - id: "neck_seg_4"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["neck_seg_3", "core_node"]
    drop_table_id: "drop_voltwyrm_seg"

  - id: "shield_left"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["core_node"]
    drop_table_id: "drop_voltwyrm_shield"

  - id: "shield_right"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["core_node"]
    drop_table_id: "drop_voltwyrm_shield"

  - id: "core_node"
    type: BOSS_CORE
    H_max_override: null
    B_max_override: null
    adjacency: ["neck_seg_4", "shield_left", "shield_right"]
    drop_table_id: "drop_voltwyrm_core"
```

*圖建立規則遵循 `kaiju-part-system.md C.6`：宣告邊雙向推導，重複邊自動去重。`core_node` 擁有 3 個相鄰部位（`neck_seg_4`, `shield_left`, `shield_right`），在 `adjacency_max_neighbors`（預設 4）限制內。*

---

## 5. 攻擊模式 (Attack Patterns)

所有彈幕遵循視覺鐵則：**暖色（橙／金／白）子彈＋黑色像素外框**，玩家判定點恆顯一格，清晰可讀。三種模式設計為「在任何密度等級下仍保持可讀性」，密度（Density）是唯一難度縮放軸。

### Pattern A：「蛇陣螺旋 (Serpent Spiral)」

| 屬性 | 描述 |
|------|------|
| **發射源** | 全體存活頸段（`neck_seg_1`~`neck_seg_4`）；以不同相位差同步旋轉，頸段愈少臂數愈稀疏 |
| **彈型** | 多臂旋轉螺旋，橙金色圓點彈，顯著黑色外框 |
| **節奏** | 每臂旋轉周期 0.8 秒；臂間間隙寬度固定（= 360° ÷ 臂數的均等角），確保可走位 |
| **讀法** | 間隙角度恆定，玩家可提前預判移動路線；旋轉速度不加速（密度只加臂數） |
| **觸發條件** | 持續（`ALIVE` 階段全程）；頸段 `BROKEN` 後對應發射點消失，自然降低螺旋密度 |
| **密度縮放** | 見第 8 節難度表 |

### Pattern B：「能量瞄準牆 (Energy Aimed Wall)」

| 屬性 | 描述 |
|------|------|
| **發射源** | 左能量盾 / 右能量盾（各自獨立發射，護盾 `BROKEN` 後對應方停止） |
| **彈型** | 橫向能量彈行，向玩家當前位置收束但附帶固定散角（非完美精準），製造「牆中有縫」結構 |
| **節奏** | 每次發射前有 0.3 秒充能閃爍（亮黃色預告）；充能動畫在開火前即可讀 |
| **讀法** | 充能特效位於護盾位置（固定），玩家可在充能期間移至側翼安全位；散角固定，牆的形狀可預測 |
| **觸發條件** | 護盾 `ALIVE`（`ARMOR_INTACT` 或 `ARMOR_STRIPPED` 均觸發）；護盾 `BROKEN` 後停止 |
| **密度縮放** | 見第 8 節難度表 |

### Pattern C：「護盾爆裂環 (Shield Burst Ring)」

| 屬性 | 描述 |
|------|------|
| **發射源** | 各護盾被命中時（任何雷射蓄熱或飛彈命中均可觸發）；獨立計算，左右護盾各自觸發 |
| **彈型** | 以護盾為圓心向外均勻擴散的能量彈環；橙紅色圓點彈，12 顆/環，間距均勻 |
| **節奏** | 觸發時立即擴散；L3 震波命中觸發時發射 2 環連環（第二環延遲 0.4s）；護盾 `BROKEN` 時觸發終環（20 顆/環，加大） |
| **讀法** | 環的起點位置固定（護盾所在處），擴散速度恆定，玩家通過觀察起點即可預判路徑；環間距設計確保走位可行 |
| **觸發條件** | 護盾受到任何 `on_laser_hit` 或 `on_missile_hit` 事件時；`BROKEN` 時觸發終環 |
| **密度縮放** | 見第 8 節難度表 |

---

## 6. 階段 (Phases)

階段由**部位破壞狀態**驅動，不依賴獨立血量計。轉換為單向不可逆。

### 階段一「全蛇展體 (Full Display)」

| 觸發條件 | 初始狀態（所有部位 `ALIVE`） |
|---------|--------------------------|
| **蛇身行為** | 緩慢 S 形漂移（上下 ±25% 畫面高度），維持縱列穿透窗口中軸穩定；蛇身隨漂移輕微扭動，縱列對齊仍可實現 |
| **攻擊組合** | Pattern A（螺旋，基礎臂數）＋ Pattern B（兩護盾交替開火） |
| **設計意圖** | 引導玩家發現 L4 縱列穿透走廊（蛇身對齊中軸 = 隱性提示）；護盾的定期充能閃爍提醒玩家 L3 的必要性 |

### 階段二「蛇體受創 (Wounded Serpent)」

| 觸發條件 | 任意 1 個頸段（`neck_seg_1`~`neck_seg_4`）進入 `BROKEN` |
|---------|------------------------------------------------------|
| **蛇身行為** | 漂移幅度提升（±35%）；每 10 秒（基準難度）一次短距橫向衝刺（0.5 秒急速橫移），製造 L4 對齊的短暫失準風險，不脫離縱列中軸 |
| **攻擊組合** | Pattern A（螺旋臂數 +1）＋ Pattern B（兩護盾**同時**開火，不再交替）＋ Pattern C（護盾主動觸發，間隔 4 秒，不等命中） |
| **設計意圖** | 首段破壞即時升壓，強化「解決護盾比繼續打頸段更迫切」的決策壓力；護盾同時開火使能量牆頻率加倍 |

### 階段三「核心暴露 (Core Exposed)」

| 觸發條件 | `neck_seg_1`~`neck_seg_4` 全 `BROKEN`，**或** `shield_left` & `shield_right` 均 `BROKEN` |
|---------|--------------------------------------------------------------------------------------|
| **蛇身行為** | 殘餘存活頸段高頻橫向抖動（若尚存），增加 L4 對齊難度；`core_node` **靜止不動**（強調終局靶心） |
| **攻擊組合** | Pattern A（最高難度臂數）＋ Pattern B（最短間隔；若護盾均已破則停止）＋ Pattern C（L3 震波觸發三環）＋ **核心直射**：`core_node` 向玩家連射 6 顆窄束橙白瞄準彈（0.6 秒一波，難度縮放見第 8 節） |
| **設計意圖** | 最高密度壓力測試「彈幕永遠讀得懂」的極限；`core_node` 靜止是清晰的終局錨點，緩解高密度下的方向迷失感 |

---

## 7. 剋制與偏好 Loadout (Loadout Analysis)

### 7.1 展示 Loadout（最優路徑）

#### Loadout S1：「縱刺-飽和 (Pierce & Saturate)」

| 武器組合 | L4 穿透雷射（Pierce Beam）× M2 蜂群飛彈（Swarm Launcher） |
|---------|--------------------------------------------------------|
| **優勢機制** | L4 單發同時命中 `neck_seg_1`~`neck_seg_4`，100 HU/s 跨鏈同步蓄熱；~4.5s（理論）/ ~8s（實戰）四段同步達 `SOFTENED`；M2 八枚飛彈扇形覆蓋縱列，單次齊射對多段同時施加破甲 |
| **運作節奏** | L4 持射對正縱列 → 四段同步橙紅脈動（視覺確認 `SOFTENED`）→ M2 齊射掃過縱列 → 多段 BU 同時增長 → 最快頸段率先 `BROKEN` → L2 Tier-3 漣漪傳熱給鄰段（若已升級）→ 逐段清除 |
| **護盾策略** | M2 廣域覆蓋在護盾 `ARMOR_STRIPPED` 窗口（2 秒）可同時洗入；可在 L4 蓄熱間隙換裝 L3 處理護盾，或搭配 M4 叢集炸彈對準頸段頂部 AoE |
| **L4 明顯優於其他雷射的量化依據** | 4 段同步 = 100 HU/s（L4）vs 最快單段 37.5 HU/s（L2）；L2 順序蓄熱四段需 ~12s+，L4 同步蓄熱只需 ~8s；效率差距 ≥ 1.5×（實戰），≥ 4×（純理論同步率） |

#### Loadout S2：「震盪-飽和 (Blast & Saturate，組合 C)」

| 武器組合 | L3 波動砲（Wave Cannon）× M2 蜂群飛彈（Swarm Launcher） |
|---------|------------------------------------------------------|
| **優勢機制** | L3 蓄力震波（Hold 1.5s）命中護盾 → `ARMOR_STRIPPED` + `stagger_timer`（2s）→ M2 即刻齊射（`M_state_mult` = 1.5）→ 護盾 BU 快速積累（8 枚 × D₀/8 × 10 × 1.5 = 15 BU / 齊射；~10 次齊射 = 150 BU = `B_max_armored` → `BROKEN`） |
| **運作節奏** | L3 蓄力（1.5s）→ 震波命中護盾（`ARMOR_STRIPPED` + Pattern C 環爆）→ M2 立刻齊射 → 2s 窗口 → BU 積累 → 重複直至 `BROKEN` |
| **護盾攻破的戰術意義** | 護盾 `BROKEN` 後 Pattern B 停止，彈幕密度大幅降低；`shield_left/right` 與 `core_node` 相鄰，M3 Tier-3 穿甲爆破鏈可從護盾 BROKEN 傳至 `core_node`（15 BU 啟動核心破甲） |
| **缺點** | L3 蓄力 1.5s 需要在密集 Pattern A 螺旋間找安全位；難度 4 下預判要求最高 |

### 7.2 替代 Loadout（公平可行路徑）

所有 Loadout 均可通關，符合〔橫向選擇〕支柱與 `weapon-system.md H.2`（任何 Loadout 的 TTB 不超過最優路徑 2.0×）。

| Loadout | 路徑摘要 | 挑戰點 | 可行性 |
|---------|---------|--------|--------|
| L2 集束 × M3 穿甲 | 依序聚焦各頸段蓄熱（L2 最快 37.5 HU/s）→ M3 熱衝擊引爆（60 BU，兩發破一段）；護盾問題需借助場地掉落 L3 或接受 Pattern B 持續壓力 | 頸段需逐一蓄熱，無同步效率；L2 在此 Boss 的縱列優勢完全被 L4 覆蓋，但單部位引爆速度（~8s）仍達標 | 可行，TTB 約 1.6–1.8× 最優 |
| L1 散波 × M1 追蹤 | 散射掃過縱列多段同時蓄熱（25 HU/s 三束全中），追蹤飛彈自動鎖定 `SOFTENED` 段 | 散射分散至多段時每段僅 8.3 HU/s（單束），蓄熱最慢；護盾需場地掉落 L3 | 可行，TTB 約 1.8–2.0× 最優（邊界） |
| L4 × M3 穿甲 | L4 加速全鏈蓄熱後，M3 引爆最先 `SOFTENED` 的段（即時 60 BU，不需多輪） | M3 無追蹤，需精確對位蛇身段；魚雷速度中等，高密度彈幕下難以在窗口期完成對位 | 可行，高技術要求，但高爽感路徑 |
| L1 / L2 × M4 叢集 | 蓄熱後以 M4 AoE 覆蓋頸段頂部叢集（neck_seg_3/4 + 可能觸及 core_node） | M4 落點在畫面前方 25–40%，對縱列上段需戰機靠近蛇身；高密度下貼近蛇身危險高 | 可行，需熟悉 M4 落點控制 |

### 7.3 護盾的特殊情況

**護甲剝甲唯一路徑**（繼承 `kaiju-part-system.md C.4`）：護盾（ARMORED 部位）的 `ARMOR_STRIPPED` 狀態只能由 L3 波動砲蓄力震波觸發。玩家若全程使用 L1/L2/L4，護盾 BU 恆為 0（飛彈偏轉）。

| 是否持有 L3 | 護盾策略 | 後果 |
|------------|---------|------|
| 持有 L3 | 直接震波剝甲，2s 窗口 M2 洗入；效率最高 | Pattern B 可提前消除 |
| 無 L3（持 L1/L2/L4） | 跳過護盾，專攻頸段和核心 | Pattern B 持續開火（能量牆壓力不解除），作為跳過護盾的合理代價 |

設計意圖：護盾是**壓力來源**，不是勝利必要條件。跳過護盾合法，但不破護盾的玩家要接受更高彈幕壓力；此設計實現〔難度是門，不是牆〕的精神——技術深度給獎勵，而非鎖死內容。

---

## 8. 難度縮放 (Difficulty Scaling)

遵循〔難度是門，不是牆〕支柱：**僅調整彈幕密度參數，所有部位數值（H_max / B_max）、內容與素材產出完全不變。**

| 參數 | 難度 1（Easy） | 難度 2（Normal） | 難度 3（Hard） | 難度 4（Nightmare） |
|-----|-------------|----------------|-------------|-------------------|
| **Pattern A 螺旋臂數（Phase 1）** | 1 臂 | 2 臂 | 3 臂 | 4 臂 |
| **Pattern A 螺旋臂數（Phase 2）** | 2 臂 | 3 臂 | 4 臂 | 4 臂 + 彈速 +15% |
| **Pattern A 螺旋臂數（Phase 3）** | 3 臂 | 4 臂 | 4 臂 + 彈速 +15% | 4 臂 + 彈速 +30% |
| **Pattern B 發射間隔（每護盾）** | 3.0s | 2.0s | 1.5s | 1.0s |
| **Pattern B 每波行數** | 1 行 | 2 行 | 3 行 | 3 行 |
| **Pattern C 觸發規則** | 護盾 `BROKEN` 時 ×1 環 | 每次命中 ×1 環 | 每次命中 ×1 環；L3 震波觸發 ×2 環 | 每次命中 ×2 環；L3 震波觸發 ×3 環 |
| **Phase 3 核心直射** | 關閉 | 開啟（間隔 0.8s） | 開啟（間隔 0.6s） | 開啟（間隔 0.5s） |
| **Phase 2 蛇身衝刺間隔** | 15s | 10s | 7s | 5s |

**難度對部位系統的隱性影響**（無需修改任何部位旋鈕，繼承 `kaiju-part-system.md C.8`）：
- 高難度螺旋臂數增加 → 玩家閃避頻率提升 → L4 縱列對齊 Uptime 下降 → 實際 TTB 自然延長
- Pattern B 間隔縮短 → L3 蓄力（1.5s）的安全窗口縮短 → 護盾更難攻破
- 以上均為合法隱性難度調節，不違背「難度是門，不是牆」支柱

---

## 9. 素材產出 (Material Output)

### 9.1 掉落表定義

掉落品質邏輯以 `material-economy.md D.1` 為準（本文件僅宣告 `part_id → drop_table_id` 映射）。

| `drop_table_id` | 對應部位 | 核心類型 | 掉落對應關係 |
|----------------|--------|---------|------------|
| `drop_voltwyrm_seg` | `neck_seg_1`~`neck_seg_4`（NORMAL） | `core_energy`（能量核心） | 能量系巨獸主題（kaiju_voltwyrm）→ `core_energy`（繼承 `material-economy.md C.1` 巨獸主題映射） |
| `drop_voltwyrm_shield` | `shield_left`, `shield_right`（ARMORED） | `core_energy`（能量核心） | 能量系巨獸主題 → `core_energy` |
| `drop_voltwyrm_core` | `core_node`（BOSS_CORE） | `core_energy`（能量核心） | 能量系巨獸主題 → `core_energy` |

*`shard_common`（通用碎片）在上述所有掉落表中均產出，品質加乘適用：Standard × 1.0，Precision × 1.5，Perfect × 2.0。*

### 9.2 全場狩獵預期產出（Precision 品質為主、全 7 部位）

| 素材 | 預期數量 / 場 | 計算基礎 |
|------|------------|---------|
| `shard_common` | ~26 個（基準）| 7 部位 × 2 × 1.5 + 5（全破壞獎勵）= 21 + 5 = 26 |
| `core_energy`（能量核心） | 7 個（全破場基準）| 4 頸段（NORMAL）+ 2 護盾（ARMORED）+ 1 核心節（BOSS_CORE）各 ×1（Precision 品質基準）；Perfect 破壞時各部位可給 2，最高 14 個 |
| `essence_kaiju`（巨獸精魄） | 1 個（全破壞條件） | 7 / 7 部位全 `BROKEN` 後結算 |

**Voltwyrm 的素材定位（能量系巨獸）**：
- `core_energy` 的主要農刷目標（7 個/場全破），是 L3 波動砲與 M3 穿甲魚雷 Tier 升級的唯一來源
- 高部位數（7 個）使每場全破壞的碎片與核心產出均為三頭目最高，是後期深度農刷玩家的最終考驗
- `shard_common` 副產出豐富（~26 個/全破壞場），同時服務所有武器的 Tier 0→1 與 Tier 1→2 碎片需求

### 9.3 全破壞條件（All-Part-Break / 精魄觸發）

| 條件 | 細節 |
|------|------|
| **觸發** | `neck_seg_1`~`neck_seg_4` + `shield_left` + `shield_right` + `core_node`，共 7 / 7 部位均達 `BROKEN` |
| **結算時機** | `on_boss_core_break` 觸發後，`on_hunt_end(is_all_parts_broken=true)` 執行精魄與完成度碎片結算 |
| **順序要求** | 護盾可在核心前或後 `BROKEN`，但精魄結算在 `on_boss_core_break` 之後才執行；若破核心時護盾仍 `ALIVE`，精魄**不觸發** |
| **UI 提示** | 勝利動畫前應顯示「尚有 N 個部位未破壞」提示，避免玩家誤以為已達成全破壞 |

---

## 10. 驗收標準 (Acceptance Criteria)

### 10.1 L4 穿透縱列優勢驗證（阻斷 Vertical Slice）

- [ ] **量化自動化測試**：L4 × M2 Loadout 對 VOLTWYRM 四段縱列的理論同步蓄熱速率 = `4 × l4_h_rate` = 100 HU/s；對無縱列對齊 Boss（Boss #1 等，部位橫列）的 L4 蓄熱速率 ≤ `l4_h_rate` = 25 HU/s（僅命中 1 段）——差距倍率 ≥ 4×。自動化測試路徑：`tests/unit/weapon/l4_vertical_advantage_voltwyrm_test.[ext]`
- [ ] **TTB 實戰差異 Playtest**：L4 × M2 在 VOLTWYRM 上，從開戰到四段頸段全 `BROKEN` 的 TTB ≤ 同 Loadout 在非縱列 Boss 上逐段相同部位類型 TTB × 0.55（L4 在此 Boss 至少快 45%）；Playtest 記錄 5 場取均值確認
- [ ] **玩家感知測試**：5 人 Playtest 後，不知設計意圖的受測者自發描述「穿透」「同時命中多個部位」或等效概念的比例 ≥ 60%

### 10.2 護盾護甲閘門正確性（阻斷）

- [ ] 護盾（`shield_left`, `shield_right`）在 `ARMOR_INTACT` 期間，任何飛彈命中的 BU 填充 = 0（飛彈偏轉；繼承 `kaiju-part-system.md H.3`）
- [ ] L3 蓄力震波命中護盾 → `ARMOR_STRIPPED` + `stagger_timer` 啟動（2.0s）→ Pattern C 環爆立即觸發（見第 5 節）
- [ ] `ARMOR_STRIPPED` 期間 M2 齊射 BU 填充 = `break_delta_base × stagger_break_mult`（× 1.5）
- [ ] 護盾 `BROKEN` 後 Pattern B 停止發射（對應護盾）；Pattern C 終環（20 顆）觸發一次

### 10.3 縱列鏈相鄰效果（功能性）

- [ ] 任意 `neck_seg` `BROKEN` 後，L2 Tier-3（若玩家持有）向所有存活相鄰段注入 `l2_t3_adjacent_heat_pct × H_max`（= 30 HU，`H_max` = 100）；繼承 `kaiju-part-system.md H.6` 測試
- [ ] `neck_seg_4` `BROKEN` 後，M3 Tier-3 穿甲爆破鏈可傳至 `core_node`（`is_chain_break=true`，15 BU 啟動核心破甲）；`shield_left/right` 若仍 `ARMOR_INTACT`，鏈式 BU = 0（偏轉）
- [ ] `core_node` 被 M3 鏈式破甲命中後，BU 確實累積（不視為 `is_chain_break` 遞迴）；`core_node` 自身的 `on_part_break` 不再觸發第二層鏈式

### 10.4 彈幕可讀性（體驗性 — UX 阻斷）

- [ ] 難度 4 Phase 3（最高密度）靜態截圖測試：不熟悉遊戲的受測者正確辨識玩家判定點（vs 敵彈）的成功率 ≥ 80%（5 人；繼承 `kaiju-part-system.md H.2`）
- [ ] Pattern A 四臂螺旋（最高密度）的臂間間隙寬度，允許玩家以最大移動速度在 0.5 秒內穿越
- [ ] 護盾（冷色深紫藍）外觀不被誤認為敵彈：受測者識別「這是可以被攻擊的護盾」 vs「這是子彈」的正確率 ≥ 90%

### 10.5 階段轉換觸發（功能性）

- [ ] 任意 1 個 `neck_seg` `BROKEN` → Phase 2 立即生效（Pattern B 切換為雙護盾同時開火）
- [ ] `neck_seg_1`~`neck_seg_4` 全 `BROKEN`，**或** `shield_left` & `shield_right` 均 `BROKEN` → Phase 3 立即生效（核心直射啟用）
- [ ] 階段轉換為單向不可逆（不因部位狀態後續變化而回退）

### 10.6 全破壞精魄結算（功能性）

- [ ] 7 / 7 部位全 `BROKEN` 後通關結算：`essence_kaiju` = `essence_per_full_clear`（預設 1）；`shard_completeness_bonus`（預設 5）正確追加
- [ ] 6 / 7 部位（任一遺漏）後通關：無精魄，無完成度碎片加成
- [ ] UI 在勝利結算前正確提示剩餘未破壞部位數

### 10.7 部位數量確認（設計約束 — 阻斷 Vertical Slice 評審）

- [x] `kaiju-part-system.md` A 節已更新：後期高難度 Boss 允許最多 8 個部位；VOLTWYRM 的 7 個部位在此範圍內，無需例外核准
- [ ] 7 部位載入時間與物件池壓力不超過目標平台效能預算（由 `lead-programmer` 確認，在效能測試中記錄）

---

*文件版本：1.0.0*
*作者：Game Designer Agent*
*關聯 GDD：game-concept.md | weapon-system.md（LOCKED）| kaiju-part-system.md | material-economy.md*
