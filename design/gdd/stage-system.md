# 關卡／波段系統 (Stage & Segment System) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑：design/gdd/stage-system.md*
*最後更新：2026-07-01*
*狀態：Draft*
*相依文件：game-concept.md | weapon-system.md | kaiju-part-system.md | material-economy.md*
*相依頭目文件：kaiju/01-carapex.md | kaiju/02-lacera.md | kaiju/03-voltwyrm.md*

---

## A. 概覽 (Overview)

關卡／波段系統（Stage & Segment System）定義殲獸戰機的**短期循環（5–15 分鐘）**結構：每輪遊玩穿越一個關卡，從前菜（小怪波段）到主菜（頭目巨獸）的完整狩獵序列。

本系統有兩個核心設計意圖：

1. **刷關重玩性（Replayability via Recombination）**：每個關卡擁有一個手作波段池（Handcrafted Segment Pool）；每次運行從池中**隨機抽取並排序**若干波段，形成不同節奏組合——這是「手作池隨機組合」方案，**非程序生成**，嚴格落實 Anti-Pillar（走輕量路線，非完整 Proc Gen）。
2. **武器掉落自然流動**：波段內的「莢艙攜帶者（Pod Carrier）」小怪掉落武器莢艙（Weapon Pod），確保玩家在每個關卡至少取得 ≥1 主武器莢（雷射系 Laser）＋ ≥1 副武器莢（飛彈系 Missile），直接服務 `weapon-system.md` F.2 掉落系統需求。

**MVP 範圍**：Stage 1（礁岩前哨站）＋ 鎧殼獸 CARAPEX＝核心循環完整展示。Stage 2、3 為後續正式里程碑內容。

---

## B. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：挑戰（Challenge）＋發現（Discovery）＋感官愉悅（Sensation）

玩家感受的是節奏分明的「準備→高潮」弧線：小怪波段是熱身與武器試探期，彈幕密度逐步上升；抵達巨獸前的短暫喘息讓張力積蓄；頭目出現時，玩家已熟悉當前 loadout，準備把系統理解化為精準輸出。

在刷關層面，隨機波段組合讓每輪的暖身節奏略有不同——同樣的頭目，每次在不同的情境準備下抵達。這不是全新體驗，而是「熟悉的變體」：知道頭目在等，但不確定今天會帶什麼武器去。這正是刷關的最佳心理狀態。

---

## C. 關卡解剖 (Stage Anatomy)

一個完整關卡（Stage）由以下固定序列組成：

```
[固定引入段] → [隨機升階波段 × N] → [前頭目喘息] → [頭目競技場]
  Intro Seg      Escalating Segs     Pre-Boss Lull    Boss Arena
    固定              每輪隨機              固定             固定
```

### C.1 各區段定義

| 區段 | 性質 | 預估時長 | 說明 |
|------|------|---------|------|
| **引入段（Intro Segment）** | 固定，每輪相同 | 60–90s | 輕量敵人，建立場景氛圍；Stage 1 兼作操作教學（見 H 節） |
| **升階隨機波段（Escalating Segments）** | 隨機抽 N 個，按難度排序 | 45–90s ／段 | 從手作波段池隨機抽取，按 `difficulty_weight` 升序（輕→重）執行；詳見 D 節 |
| **前頭目喘息（Pre-Boss Lull）** | 固定 | 15–30s | 無敵人；必定生成 1 個武器莢艙；音樂切換為頭目前奏；可能出現巨獸輪廓陰影 |
| **頭目競技場（Boss Arena）** | 固定 | 依頭目設計 | 見各巨獸設計文件；玩家帶著本輪取得的 loadout 進入 |

### C.2 各關卡時長目標

| 關卡 | 每輪抽取波段數 | 波段池大小 | 預估關卡時長（不含頭目） |
|------|-------------|---------|----------------------|
| S1 教學 ← **MVP** | 3 | 5 | 7–10 min |
| S2 中期 ○ | 4 | 7 | 10–13 min |
| S3 終局 ○ | 5 | 8 | 12–15 min |

---

## D. 手作波段池與隨機重組規則 (Segment Pool & Recombination Rules)

### D.1 輕量重組演算法（非 Proc Gen）

```
本輪波段序列 = Recombine(stage_pool, N, last_run_played_segment_ids)

步驟：
  1. 從 stage_pool 中移除上一輪最後播放的波段（no-repeat window = 1）
     ⌊ 若池大小 ≤ N，跳過此步驟以確保能抽滿 ⌋
  2. 依難度階過濾：移除 min_difficulty_tier > 當前難度階的波段
     ⌊ 若過濾後剩餘 < N，放寬篩選至全池 ⌋
  3. 對剩餘波段執行 Fisher-Yates 隨機洗牌
  4. 取前 N 個波段
  5. 按 difficulty_weight 升序排列（最輕的最早執行）
  6. 輸出：固定引入段 + 排序後 N 段 + 固定前頭目喘息 + 頭目競技場
```

**設計守則**：這是「把手作牌洗過再抽幾張」的邏輯，不是生成新牌。關卡設計師掌控每張牌的所有細節；系統只決定每輪的抽取順序與排列。

### D.2 波段資料結構

```yaml
segment_id: "s1_02"
stage_id: "stage_01"
segment_name_zh: "砲台陣線"
segment_name_en: "Turret Line"
difficulty_weight: 2           # 1=最輕 / 5=最重；用於抽後排序
min_difficulty_tier: 1         # 此波段的最低可出現難度（D1–D4）
waves:
  - { enemy_id: "tri_shot", count: 3, formation: "horizontal_spread" }
  - { enemy_id: "tri_shot", count: 2, formation: "offset" }
pod_carrier_wave_index: 1      # 第 1 波（0-indexed）中有莢艙攜帶者；-1 = 無
pod_type_preference: "primary" # "primary" / "secondary" / "any"
pod_weapon_preference: null    # null = 從池隨機；可指定如 "L3" 作偏好（不保證）
```

### D.3 難度感知池過濾

| 難度階 | 可抽取波段 `difficulty_weight` | 備注 |
|--------|------------------------------|------|
| D1 Normal | 1–3 | 教學友善 |
| D2 Hard | 1–4 | 含稍高壓力段 |
| D3 Extreme | 2–5 | 最輕段退出 |
| D4 Nightmare | 3–5 | 只出現中高強度段 |

---

## E. 小怪名單 (Trash Enemy Roster)

所有小怪為**暖色系像素敵人**，遵守「暖色＝威脅」視覺鐵則。子彈均使用橙 / 橘 / 黃搭配清晰像素外框。

★ = MVP（Stage 1 出現）| ○ = 後續里程碑

### E.1 小怪一覽

| # | 名稱（中 / 英） | ID | 移動行為 | 射擊行為 | 主色 | 首次出現 |
|---|---------------|-----|---------|---------|------|---------|
| 1 | 衝角蟲 / Ram Grub | `ram_grub` | 直線高速衝向玩家當前位置，入場後不改向 | 無（碰撞傷害） | 暖橙 #FF8800 | ★ S1 |
| 2 | 三叉砲艦 / Tri-Shot | `tri_shot` | 水平緩漂進入，2–4s 後向下移動 | 每 2.5s 發射 3 發扇形向下（±30°） | 橙黃 #FFAA00 | ★ S1 |
| 3 | 瞄準炮台 / Aimed Gun | `aimed_gun` | 緩慢垂直向下漂移；到 30% 螢幕高度後懸停 | 每 3s 精準瞄準玩家發射 1 發；電報 0.5s 炮管旋轉 | 橘紅 #FF5500 | ★ S1 |
| 4 | 環爆子 / Ring Burst | `ring_burst` | 慢速直行向下 | 死亡時 8 方向各爆 1 彈（固定方向）；存活時無主動射擊 | 暖黃 #FFCC00 | ★ S1 |
| 5 | 護衛飛行器 / Shield Flier | `shield_flier` | 中速橫移，走 U 形迴旋路徑 | 每 4s 向下發射 2 發；正面像素護盾可吸收 3 次正面命中 | 深橙 #CC4400 | ○ S2 |
| 6 | 列陣兵 / Column Grunt | `column_grunt` | V 形或縱列（Vertical Column）隊形向下推進 | 隊形每 4s 同步齊射向前各 1 發；電報：全隊同時閃光 0.3s | 橙黃 #FFBB00 | ○ S2/S3 |

### E.2 各小怪設計說明

#### E.2.1 衝角蟲 Ram Grub `ram_grub` ★
迫使玩家側移；無彈幕壓力但有碰撞威脅。單獨使用時教「移動閃避」，與射擊型混合時製造「一邊閃衝一邊讀彈」的複合壓力。死亡時無爆炸，拉低場面混亂度。

#### E.2.2 三叉砲艦 Tri-Shot `tri_shot` ★
基本射擊教學敵。玩家學習「讀三發扇形彈 → 穿縫或橫移閃避」。發射前 0.3s 炮管閃爍電報，可讀性高。是 Stage 1 的核心壓力來源。

#### E.2.3 瞄準炮台 Aimed Gun `aimed_gun` ★
玩家學習「在發射電報時預位」而非「看到子彈再閃」。炮管旋轉指向玩家的 0.5s 電報是核心可讀性設計。懸停後成為持續威脅，鼓勵玩家優先清除。

#### E.2.4 環爆子 Ring Burst `ring_burst` ★
玩家學習「時機控制」而非直接衝入擊殺——近距擊殺反而逃不掉爆彈。血量低時外圈閃爍（預警爆裂）。混在其他敵人中時，分心清除環爆子可能把玩家推入炮台火線，製造微決策張力。

#### E.2.5 護衛飛行器 Shield Flier `shield_flier` ○（S2 起）
正面有像素護盾（暗琥珀色半透明），吸收正面攻擊 3 次；受正面攻擊時護盾發偏轉火花。需從側邊攻擊或等 U 形迴旋轉身時攻擊正面。預習 LACERA「移動部位」的「讀路徑找射擊視窗」概念，且與 L3 短脈衝「可穿透護盾」的特性形成自然關聯（為頭目戰的 L3 使用鋪墊）。

#### E.2.6 列陣兵 Column Grunt `column_grunt` ○（S2/S3）
縱列（Vertical Column）隊形是 L4 穿透雷射（Pierce Beam）在**小怪層的自然利基**——L4 單發可同時命中整列。V 形隊形則製造「掃翼 vs 穿中路」微決策。Stage 3 的「縱列穿透」波段（S3-02）使用 6 個列陣兵縱列，是 VOLTWYRM 四頸段垂直穿透走廊的**視覺與機制預告**。

---

## F. 武器莢艙掉落 (Weapon-Pod Field Drops)

本節定義武器莢艙（Weapon Pod）如何在關卡中生成，實現 `weapon-system.md` F.2 掉落系統。

### F.1 莢艙類型與視覺

| 類型 | 外觀 | 作用 |
|------|------|------|
| 主武器莢（Primary Pod） | **冷藍色膠囊** ＋ 雷射符文圖示 | 替換當前主武器（雷射系 L1–L4；從本關卡可用池隨機抽取） |
| 副武器莢（Secondary Pod） | **橙色膠囊** ＋ 飛彈符文圖示 | 替換當前副武器（飛彈系 M1–M4；從本關卡可用池隨機抽取） |

> **視覺可讀性注意**：主武器莢用冷藍代表「科技擴充」，與敵彈（暖橙圓點）形狀和色系雙重區分；副武器莢雖為橙色但為膠囊形（敵彈為圓點），不易混淆。驗收標準見 L.6。

### F.2 莢艙攜帶者機制（Pod Carrier）

每個 `pod_carrier_wave_index ≥ 0` 的波段，在指定波次中有 1 個「莢艙攜帶者」敵人：
- 外觀：在敵機頂部顯示閃爍膠囊圖示（藍或橙，對應掉落類型，間隔 0.5s 閃爍）
- 擊殺時**必定**在死亡位置掉落對應類型的武器莢艙
- 攜帶者為普通小怪類型（非額外敵人種類），只在該波次被標記「莢艙狀態」
- 玩家戰機接觸莢艙即自動拾取，立即換裝（同 `weapon-system.md` C.3）

### F.3 每關保底機制（Guaranteed Drop Floor）

**設計目標**：任何難度、任何波段組合下，玩家進入前頭目喘息前必定擁有 ≥1 主武器莢 ＋ ≥1 副武器莢。

**實作規則**：
1. 追蹤本輪已掉落類型（`primary_dropped: bool`, `secondary_dropped: bool`）
2. 若到最後一個升階波段結束仍有某類型未掉落 → 強制將該波段的攜帶者設為未掉落類型
3. 前頭目喘息**永遠**生成 1 個額外莢艙（優先補充尚未掉落的類型；兩種均已掉落則隨機）
4. 保底機制在任何難度階均生效（難度不影響掉落規則）

### F.4 各關卡武器莢艙池（Stage Drop Pools）

| 關卡 | 主武器池（雷射系）可用 | 副武器池（飛彈系）可用 | 設計意圖 |
|------|-------------------|--------------------|---------|
| **S1 教學 ← MVP** | L1 散波雷射, L2 集束雷射 | M1 追蹤飛彈, M3 穿甲魚雷 | 限縮至 2 對 2；自然引導玩家達到 L2＋M3 展示 Loadout |
| **S2 中期 ○** | L1, L2, L3 波動砲 | M1, M2 蜂群飛彈, M3 | 引入 L3（LACERA 尾甲剝甲路徑）；M2（廣域應對移動部位） |
| **S3 終局 ○** | L1, L2, L3, L4 穿透雷射 | M1, M2, M3, M4 叢集炸彈 | 全池；L4 為展示核心武器 |

莢艙內的具體武器從對應池中等機率隨機抽取，`pod_weapon_preference` 提升指定武器出現機率（不保證）。

---

## G. 三個關卡設計 (The Three Stages)

---

### G.1 Stage 1：礁岩前哨站 REEF OUTPOST ALPHA ← **MVP 核心**

| 屬性 | 值 |
|------|-----|
| **主題** | 海底礁岩廢墟；科幻前哨基地遺址 |
| **背景視覺** | 深海藍黑底；冷藍珊瑚礁與廢棄鋼鐵架構；生物發光冷藍環境光（冷色環境 vs 暖色敵人，對比清晰） |
| **頭目** | 鎧殼獸 CARAPEX（教學型，`kaiju_id: carapex`） |
| **波段池大小 / 每輪抽取** | 5 個 / 抽 3 個 |
| **使用小怪** | `ram_grub`, `tri_shot`, `aimed_gun`, `ring_burst` |
| **預估時長（不含頭目）** | 7–10 min |
| **首要設計職責** | 前 10 分鐘完整引導（見 H 節）；L2 × M3 展示 Loadout 的自然出現 |

#### G.1.1 波段池

| 波段 ID | 名稱（中 / 英） | 難度權重 | 波次構成 | 莢艙 |
|---------|--------------|---------|---------|------|
| S1-01 | 警戒前鋒 / Scout Vanguard | 1 | W1: `ram_grub` ×4 兩列衝入；W2: `ram_grub` ×2 斜角衝入 | 無 |
| S1-02 | 砲台陣線 / Turret Line | 2 | W1: `tri_shot` ×3 水平；W2: `tri_shot` ×2 錯位 | 主武器莢（W1 攜帶） |
| S1-03 | 瞄準追擊 / Aimed Pursuit | 2 | W1: `aimed_gun` ×2 左右；W2: `aimed_gun` ×2 ＋ `ram_grub` ×2 | 副武器莢（W1 攜帶） |
| S1-04 | 環爆接觸 / Burst Contact | 3 | W1: `ring_burst` ×3 緩降；W2: `ring_burst` ×2 ＋ `tri_shot` ×1 | 無 |
| S1-05 | 混合壓力 / Mixed Pressure | 3 | W1: `tri_shot` ×2 ＋ `aimed_gun` ×1；W2: `aimed_gun` ×2 ＋ `ring_burst` ×1 | 任意（視保底需求） |

#### G.1.2 強度曲線

```
強度
 ↑
 5 |                                          ████ CARAPEX Phase3
 4 |                                 ████████
 3 |                 ████████████████
 2 |      ██████████
 1 | ████
 0 +──────────────────────────────────────────────────────→ 時間
   Intro  SegA   SegB   SegC   Lull   P1     P2     P3
   0:00   1:30   3:30   6:00   8:00   8:30   12:00  16:00
```

#### G.1.3 敘事提示（Narrative Beats）

| 時間點 | 視覺敘事 |
|--------|---------|
| 引入段 | 廢棄前哨站的生物發光信號閃爍（冷藍），暗示巨大生物存在 |
| 波段推進中 | 背景礁岩深處傳來低頻震動光（橙色脈動），由弱漸強 |
| 前頭目喘息 | 巨大陰影從螢幕上方緩緩覆蓋——CARAPEX 的甲殼輪廓出現 |
| 頭目入場 | 鎧殼獸橫跨畫面緩慢現身（60–75% 畫面寬度，尺寸壓迫） |
| 首次部位破壞 | 大顎爆裂像素動畫 ＋ 素材閃入 HUD ＋ 爆破 SFX 爽感高峰 |

#### G.1.4 音樂／音效提示（Audio Cues）

| 時間點 | 音頻提示 |
|--------|---------|
| 引入段開始 | 深海低頻環境音 ＋ 金屬撞擊回聲 |
| 每波敵人出現 | 短促高頻警報 SFX（像素街機感） |
| 武器莢艙出現 | 冷色系明亮提示音（與敵彈低頻對比，清晰可辨） |
| 前頭目喘息 | 音樂驟降 → 靜默 3s → 低頻鼓點 |
| CARAPEX 入場 | 切換重拍像素 BGM；甲殼重量感節奏 |
| 部位破壞（PART BREAK） | 撕裂爆破 SFX ＋ 素材入袋提示音（短促愉悅音） |

---

### G.2 Stage 2：深淵峽谷 ABYSSAL RIFT ○（後續里程碑）

| 屬性 | 值 |
|------|-----|
| **主題** | 深海海溝；熱液噴口廢墟；地質壓力感 |
| **背景視覺** | 深藍黑底；低飽和橙紅岩漿裂縫（暖色但低飽和，不干擾彈幕可讀性）；峽谷壁兩側收窄視覺感 |
| **頭目** | 刃肢獸 LACERA（移動系，`kaiju_id: lacera`） |
| **波段池大小 / 每輪抽取** | 7 個 / 抽 4 個 |
| **使用小怪** | 全 6 種；引入 `shield_flier` 與 `column_grunt` |
| **預估時長（不含頭目）** | 10–13 min |
| **首要設計職責** | 引入 L3 波動砲（LACERA 尾甲剝甲依賴 L3）；以 `shield_flier` 預習「追蹤追移動目標」概念 |

#### G.2.1 波段池

| 波段 ID | 名稱（中 / 英） | 難度權重 | 波次構成 | 莢艙 |
|---------|--------------|---------|---------|------|
| S2-01 | 雙翼掃蕩 / Wing Sweep | 1 | W1: `column_grunt` ×4 V 形；W2: `ram_grub` ×3 斜衝 | 無 |
| S2-02 | 砲台縱深 / Turret Depth | 2 | W1: `tri_shot` ×3 ＋ `aimed_gun` ×1；W2: `tri_shot` ×2 ＋ `aimed_gun` ×2 | 主武器莢（W1 攜帶） |
| S2-03 | 護衛巡邏 / Shield Patrol | 2 | W1: `shield_flier` ×3 橫移；W2: `shield_flier` ×2 ＋ `aimed_gun` ×2 | 副武器莢（W2；偏好 M1） |
| S2-04 | 環爆壓制 / Burst Press | 3 | W1: `ring_burst` ×4 ＋ `ram_grub` ×2；W2: `ring_burst` ×3 ＋ `tri_shot` ×2 | 無 |
| S2-05 | 列陣突破 / Formation Break | 3 | W1: `column_grunt` ×6 縱列；W2: `shield_flier` ×3；W3: `aimed_gun` ×3 ＋ `ram_grub` ×2 | 主武器莢（W2；偏好 L3） |
| S2-06 | 護衛飽和 / Guard Saturation | 3 | W1: `shield_flier` ×4 ＋ `tri_shot` ×2；W2: `shield_flier` ×3 ＋ `aimed_gun` ×2 | 副武器莢（W2 攜帶） |
| S2-07 | 全面進攻 / Full Assault | 4 | W1: `ram_grub` ×4 ＋ `aimed_gun` ×2；W2: `shield_flier` ×3 ＋ `tri_shot` ×2；W3: `column_grunt` ×4 ＋ `ring_burst` ×2 | 無 |

#### G.2.2 強度曲線

```
強度
 ↑
 5 |                                                   ████ LACERA Phase1→3
 4 |                            ████████████
 3 |            ████████████████
 2 |   ████████
 1 | ██
 0 +──────────────────────────────────────────────────────→ 時間
   Intro SegA  SegB  SegC  SegD  Lull  P1    P2    P3
   0:00  1:30  4:00  7:00  10:00 12:30 13:00 18:00 23:00
```

#### G.2.3 關鍵設計節點

- **L3 預習**：S2-03「護衛巡邏」的 `shield_flier` 護盾能被 L3 短脈衝穿透（正面護盾對 L3 無效），是頭目戰「L3 剝甲」機制的縮小版展示。
- **追蹤飛彈預習**：`shield_flier` 的 U 形移動讓玩家體驗「追移動目標難」→ 自然誘導嘗試 M1。
- **L3 莢艙偏好**：S2-05 的攜帶者偏好 L3（`pod_weapon_preference: "L3"`），確保玩家在 LACERA 前有機會取得 L3 試用剝甲。

---

### G.3 Stage 3：高壓電弧站 VOLTAGE SPIRE ○（後續里程碑）

| 屬性 | 值 |
|------|-----|
| **主題** | 浮空能量發電站；高壓電弧迸發；廢棄高科技設施 |
| **背景視覺** | 深藍鋼鐵底；背景冷色電弧（環境 VFX，非彈幕）；大型橙色能量纜線橫亙畫面 |
| **頭目** | 熾蛇 VOLTWYRM（縱列型，`kaiju_id: voltwyrm`） |
| **波段池大小 / 每輪抽取** | 8 個 / 抽 5 個 |
| **使用小怪** | 全 6 種；`column_grunt` 縱列隊形為主題核心 |
| **預估時長（不含頭目）** | 12–15 min |
| **首要設計職責** | 在小怪層強化「縱列穿透」概念；讓玩家抵達 VOLTWYRM 前已有「L4＝縱列高效」直覺 |

#### G.3.1 波段池

| 波段 ID | 名稱（中 / 英） | 難度權重 | 波次構成 | 莢艙 |
|---------|--------------|---------|---------|------|
| S3-01 | 電弧前驅 / Arc Vanguard | 2 | W1: `ram_grub` ×4 斜衝 ＋ `tri_shot` ×2；W2: `aimed_gun` ×3 | 無 |
| S3-02 | 縱列穿透 / Pierce Column | 2 | W1: `column_grunt` ×6 完整縱列（一字排開垂直）；W2: `column_grunt` ×4 縱列 ＋ `ram_grub` ×2 側翼 | 主武器莢（W1；偏好 L4） |
| S3-03 | 護盾矩陣 / Shield Matrix | 3 | W1: `shield_flier` ×4 兩排橫移；W2: `shield_flier` ×3 ＋ `aimed_gun` ×2；W3: `shield_flier` ×2 ＋ `ring_burst` ×2 | 副武器莢（W2；偏好 M2） |
| S3-04 | 環爆飽和 / Burst Saturation | 3 | W1: `ring_burst` ×5 交錯；W2: `ring_burst` ×3 ＋ `aimed_gun` ×4 | 無 |
| S3-05 | 縱列掩護 / Column Cover | 3 | W1: `column_grunt` ×5 縱列；W2: `aimed_gun` ×3 側翼；W3: `column_grunt` ×4 ＋ `tri_shot` ×2 | 主武器莢（W3 攜帶） |
| S3-06 | 蜂群壓制 / Swarm Press | 4 | W1: `ram_grub` ×6 三列衝入；W2: `tri_shot` ×4 ＋ `aimed_gun` ×2；W3: `ring_burst` ×4 ＋ `ram_grub` ×3 | 副武器莢（W1 攜帶） |
| S3-07 | 護衛電網 / Guard Grid | 4 | W1: `shield_flier` ×5 兩層；W2: `shield_flier` ×3 ＋ `column_grunt` ×3 縱列；W3: `aimed_gun` ×4 ＋ `ring_burst` ×3 | 無 |
| S3-08 | 終局壓制 / Final Siege | 5 | W1: `tri_shot` ×4 ＋ `aimed_gun` ×3；W2: `shield_flier` ×4 ＋ `column_grunt` ×4 縱列；W3: 全 6 種各 2 | 任意（視保底需求） |

#### G.3.2 強度曲線

```
強度
 ↑
 5 |                                                          ████ VOLTWYRM
 4 |                             ████████████████████
 3 |             ████████████████
 2 |    ████████
 1 | ██
 0 +──────────────────────────────────────────────────────→ 時間
   Intro A    B    C    D    E    Lull  P1   P2    P3
   0:00  2:30 5:00 8:00 11:00 13:30 15:00 16:00 21:00 27:00
```

#### G.3.3 縱列概念預告（L4 Foreshadowing）

S3-02「縱列穿透」是本關卡的**設計關鍵波段**：`column_grunt` ×6 排成完整縱列，L4 穿透雷射一發可同時命中全列（6×D₀ 有效輸出），與 L1/L2 的逐個清除效率差距顯著。這是 VOLTWYRM 四頸段垂直穿透走廊在小怪層的**直觀預告**——玩家在遇到巨獸之前，已在小怪戰中建立「L4＝縱列殺傷」的直覺。

縱列對齊要求：S3-02 中 `column_grunt` 的橫向 x 座標偏差 ≤ 5px（確保 L4 穿透判定能覆蓋全列）。

---

## H. 引導設計 (Onboarding — Stage 1 前 10 分鐘)

**目標**：前 10 分鐘完整示範核心循環（移動 → 射擊 → 武器拾取 → 破部位頭目 → 素材掉落 → 升級），對應 `game-concept.md` Flow State Design 的 Onboarding Curve。

### H.1 引導時間軸

| 時間 | 階段 | 設計行為 | 傳達的系統教學 |
|------|------|---------|-------------|
| 0:00–1:30 | **引入段 W1（固定）** | 2 隻 `ram_grub` 以 × 0.7 慢速衝入（154 px/s）；玩家只需側移 | **移動教學**：不射擊也能通過 |
| 1:00–1:30 | 引入段 W2 | 3 隻 `tri_shot` 靜止在上方開始射擊 | **射擊教學**：子彈出現，玩家本能開火 |
| 1:30–3:30 | 第一個隨機波段（難度權重 ≤ 2，**必定含主武器莢攜帶者**） | 莢艙攜帶者被擊殺 → 主武器莢掉落 | **第一把武器拾取**：HUD 3s 提示 + 武器切換效果立即可感知 |
| 3:30–6:00 | 第二個隨機波段 | 難度稍升；可能出現 `aimed_gun` 或 `ring_burst` | **複合敵人互動**：讀彈 ＋ 時機控制 |
| 6:00–8:00 | 第三個隨機波段（保底確保副武器莢已出現） | 保底機制確認副武器莢已掉落 | **完整 Loadout**：玩家帶 1 主 ＋ 1 副進入頭目 |
| 8:00–8:30 | **前頭目喘息（固定）** | 無敵人；生成 1 個額外莢艙；CARAPEX 陰影 | 音樂轉換；緊張感積累；最後換裝機會 |
| 8:30+ | **CARAPEX 頭目 Phase 1** | 左右大顎 NORMAL ＋ 背甲炮 ARMORED ＋ 核心 BOSS_CORE 同時可見 | **蓄熱→破甲展示**：L2 集束照射大顎 → 橙紅脈動 → M3 引爆 |
| ~11:00 | **首次大顎破壞（PART BREAK）** | 爆裂像素動畫 ＋ 素材飛入 HUD ＋ 爆破 SFX | **破壞即獎勵**：素材入袋 ＋ 計數器更新 |
| 戰鬥結束後 | **結算畫面** | 素材合計 ＋ 升級介面自動彈出；第一個武器可立即升 Tier 1→2 | **永久強化展示**：Tier 顏色與外觀變化，確認養成循環端到端跑通 |

### H.2 Stage 1 特殊引導規則

1. **引入段速度減速**：D1 難度下 `ram_grub` 的第一波速度乘以 `ram_grub_intro_speed_mult`（預設 0.7）；D2+ 恢復全速。僅作用於固定引入段第 1 波。
2. **第一個隨機波段保底主武器莢**：無論隨機抽取結果如何，Stage 1 第一個隨機波段**必定**包含主武器莢攜帶者（強制將 `pod_carrier_wave_index` 設為有效值，覆寫原段落設定）。
3. **HUD 引導提示（一次性）**：第一次拾取武器莢艙時，HUD 顯示 3s 淡入淡出提示：「拾取武器莢艙以替換當前武器」；後續拾取不再顯示。此提示在 `player_prefs.first_pod_pickup_shown = true` 後永久關閉。

---

## I. 難度縮放 (Difficulty Scaling)

嚴格落實「難度是門，不是牆（Difficulty is a Door）」支柱：**只縮放彈幕密度與小怪數量，絕不鎖定內容、波段組合或武器掉落。**

### I.1 縮放參數表

| 難度階 | 名稱 | 敵人數量乘數 | 子彈密度乘數 | 玩家感知效果 |
|--------|------|------------|------------|------------|
| D1 | 普通 Normal | ×1.0 | ×1.0 | 基準；教學友善 |
| D2 | 困難 Hard | ×1.25 | ×1.25 | 有意識的挑戰 |
| D3 | 極限 Extreme | ×1.50 | ×1.50 | 需主動讀彈 |
| D4 | 惡夢 Nightmare | ×1.75 | ×2.00 | 最高密度；蓄熱 Uptime 自然下降（隱性 TTB 延長） |

### I.2 縮放作用範圍

**縮放生效（Stage System 控制）**：
- 每波次敵人生成數：`actual_count = ceil(base_count × difficulty_enemy_mult)`
- 射擊類敵人每次發射的子彈數：`actual_bullets = ceil(base_bullets × difficulty_bullet_mult)`

**恆定不縮放**：
- 波段池組成與抽取規則
- 武器莢艙保底掉落規則
- 敵人移動速度（移動速度是部分敵人的核心設計特性）
- 波段 `difficulty_weight` 排序邏輯
- 頭目部位 H_max / B_max（由 `kaiju-part-system.md` 控制）
- 波段切換節奏與時序

### I.3 難度讀取方式（關卡系統）

```
關卡開始時：
  difficulty_tier = player_selected_difficulty   // 玩家開局前選擇
  enemy_count_mult  = DIFFICULTY_ENEMY_MULT[difficulty_tier]
  bullet_density_mult = DIFFICULTY_BULLET_MULT[difficulty_tier]

每波次生成時：
  actual_count = ceil(base_count × enemy_count_mult)
```

所有乘數存於 `assets/data/stages/difficulty_config.yaml`，禁止硬編碼。

---

## J. 系統相依 (Dependencies)

| 相依系統 | 方向 | 說明 |
|---------|------|------|
| `weapon-system.md` F.2 | Stage 提供掉落池 → 武器系統消費莢艙拾取事件 | 關卡定義 drop pool；拾取邏輯由武器系統執行 |
| `kaiju-part-system.md` | Boss Arena 觸發 → 部位系統接管 | 頭目入場後，部位系統開始接收武器命中事件 |
| `material-economy.md` | 部位破壞後 → 素材系統結算 | 透過 `on_part_break` 事件鏈觸發；Stage System 不直接呼叫 |
| `kaiju/01-carapex.md` | Stage 1 Boss Arena 設定 | CARAPEX 攻擊模式、部位構成、難度縮放 |
| `kaiju/02-lacera.md` | Stage 2 Boss Arena 設定 | LACERA 移動部位、攻擊模式、L3 剝甲尾甲 |
| `kaiju/03-voltwyrm.md` | Stage 3 Boss Arena 設定 | VOLTWYRM 縱列穿透走廊、護盾設計 |
| `weapon-system.md` F.5 / E.3 | L4 垂直部位需求 | Stage 1 CARAPEX 有垂直對齊（`dorsal_cannon` ↕ `chest_reactor_core`，已由 `01-carapex.md` AC-06 確認）；Stage 3 VOLTWYRM 縱列走廊完整（已由 `03-voltwyrm.md` 確認） |

---

## K. 調校旋鈕 (Tuning Knobs)

**所有數值存放於 `assets/data/stages/` 目錄，禁止硬編碼。**

### K.1 全域難度旋鈕（`difficulty_config.yaml`）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `difficulty_enemy_mult[D1]` | 1.00 | — | 閘門 | D1 敵人數量乘數（基準） |
| `difficulty_enemy_mult[D2]` | 1.25 | 1.10–1.50 | 曲線 | D2 乘數 |
| `difficulty_enemy_mult[D3]` | 1.50 | 1.25–1.75 | 曲線 | D3 乘數 |
| `difficulty_enemy_mult[D4]` | 1.75 | 1.50–2.00 | 曲線 | D4 乘數 |
| `difficulty_bullet_mult[D1]` | 1.00 | — | 閘門 | D1 子彈密度乘數（基準） |
| `difficulty_bullet_mult[D2]` | 1.25 | 1.10–1.50 | 曲線 | D2 子彈密度乘數 |
| `difficulty_bullet_mult[D3]` | 1.50 | 1.25–1.75 | 曲線 | D3 乘數 |
| `difficulty_bullet_mult[D4]` | 2.00 | 1.75–2.50 | 曲線 | D4 乘數；子彈密度獨立上調幅度高於敵人數量 |

### K.2 波段池旋鈕（`stage_config.yaml`）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `stage1_segment_draw_count` | 3 | 2–4 | 閘門 | S1 每輪抽取波段數 |
| `stage2_segment_draw_count` | 4 | 3–5 | 閘門 | S2 每輪抽取波段數 |
| `stage3_segment_draw_count` | 5 | 4–6 | 閘門 | S3 每輪抽取波段數 |
| `no_repeat_window` | 1 | 1–2 | 手感 | 前 N 輪不重複同一波段（跨輪記憶） |
| `pre_boss_lull_duration` | 20s | 15–30 | 手感 | 前頭目喘息時長 |

### K.3 武器莢艙旋鈕（`pod_drop_config.yaml`）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `guaranteed_primary_per_stage` | 1 | 1–2 | 閘門 | 每關最低主武器莢數量（保底） |
| `guaranteed_secondary_per_stage` | 1 | 1–2 | 閘門 | 每關最低副武器莢數量（保底） |
| `pre_boss_lull_pod_count` | 1 | 1–2 | 手感 | 前頭目喘息生成的額外莢艙數 |
| `pod_carrier_flash_interval` | 0.5s | 0.3–0.8 | 手感 | 攜帶者頂部圖示閃爍頻率 |

### K.4 小怪旋鈕（`enemy_config.yaml`）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `ram_grub_speed` | 220 px/s | 150–300 | 手感 | 衝角蟲衝速 |
| `ram_grub_intro_speed_mult` | 0.70 | 0.50–0.90 | 手感 | Stage 1 引入段 W1 速度乘數 |
| `tri_shot_fire_interval` | 2.5s | 2.0–4.0 | 手感 | 三叉砲艦射擊間隔 |
| `tri_shot_bullet_speed` | 130 px/s | 100–160 | 手感 | 三叉砲艦子彈速度 |
| `tri_shot_spread_angle_deg` | 30 | 20–45 | 手感 | 扇形左右散角（±度） |
| `aimed_gun_fire_interval` | 3.0s | 2.5–4.0 | 手感 | 瞄準炮台射擊間隔 |
| `aimed_gun_telegraph_duration` | 0.5s | 0.3–0.8 | 手感 | 炮管旋轉電報持續時間 |
| `aimed_gun_bullet_speed` | 150 px/s | 120–180 | 手感 | 瞄準子彈速度 |
| `ring_burst_move_speed` | 60 px/s | 40–90 | 手感 | 環爆子移動速度 |
| `ring_burst_explosion_speed` | 100 px/s | 80–130 | 手感 | 死亡爆彈速度 |
| `ring_burst_explosion_dirs` | 8 | 4 / 8 / 12 | 閘門 | 死亡爆彈方向數 |
| `shield_flier_move_speed` | 120 px/s | 90–160 | 手感 | 護衛飛行器橫移速度 |
| `shield_flier_shield_hp` | 3 hits | 2–5 | 曲線 | 護盾可吸收正面攻擊次數 |
| `shield_flier_fire_interval` | 4.0s | 3.0–5.0 | 手感 | 護衛飛行器射擊間隔 |
| `column_grunt_move_speed` | 60 px/s | 40–90 | 手感 | 列陣兵推進速度 |
| `column_grunt_fire_interval` | 4.0s | 3.0–5.0 | 手感 | 列陣兵齊射間隔 |
| `column_grunt_bullet_speed` | 110 px/s | 90–140 | 手感 | 列陣兵子彈速度 |
| `column_grunt_x_tolerance_px` | 5 | 0–10 | 閘門 | 縱列隊形橫向偏差上限（L4 穿透對齊保證） |

---

## L. 驗收標準 (Acceptance Criteria)

### L.1 關卡結構完整性（功能性 — 阻斷）

- [ ] 每個關卡的執行序列為：引入段 → 隨機波段 × N → 前頭目喘息 → 頭目競技場；順序不可打亂
- [ ] 隨機抽取的 N 個波段依 `difficulty_weight` 升序排列（最輕最早）
- [ ] No-repeat window 生效：前一輪最後執行的波段，下一輪不出現於抽取結果
- [ ] 自動化測試：`tests/unit/stage/segment_recombination_test`——100 次隨機生成驗證：抽取數量正確、排序正確、no-repeat 生效、難度過濾正確

### L.2 武器莢艙保底（功能性 — 阻斷）

- [ ] 任何難度、任何波段組合：玩家在抵達前頭目喘息時，已獲得 ≥1 主武器莢 ＋ ≥1 副武器莢
- [ ] 前頭目喘息生成 1 個額外莢艙，類型符合保底補充邏輯
- [ ] 自動化測試：`tests/unit/stage/weapon_pod_guarantee_test`——200 輪（各種隨機種子）確認保底條件恆成立
- [ ] 莢艙攜帶者視覺標記在 D4 最高彈幕密度下仍可辨識（5 人截圖辨識率 ≥ 80%）

### L.3 難度縮放一致性（功能性）

- [ ] D1–D4 下，實際生效的敵人數量乘數與子彈密度乘數與 `difficulty_config.yaml` 定義一致
- [ ] 敵人移動速度、波段池、武器莢艙掉落規則在 D1–D4 下恆定（`difficulty_invariance_test` 覆蓋）
- [ ] Stage 1 引入段 W1 D1 難度：`ram_grub` 速度 ＝ `ram_grub_speed × ram_grub_intro_speed_mult`（= 154 px/s）

### L.4 引導設計達成率（體驗性 — Stage 1 MVP 阻斷）

- [ ] 5 人新手用戶測試（D1，首次遊玩）：Stage 1 通關後受測者能自主描述「加熱再引爆」的核心循環，達成率 ≥ 70%（繼承 `01-carapex.md` AC-01 標準）
- [ ] 前 10 分鐘內玩家必定拾取至少 1 個武器莢艙（L.2 保底 ＋ 第一個隨機波段保底主武器莢的實作確認）
- [ ] CARAPEX 頭目第一次部位破壞（任意部位）發生於頭目入場後 5 分鐘內（記錄時間戳；反映新手可及性）

### L.5 縱列預告效果（體驗性 — Stage 3 Vertical Slice）

- [ ] S3-02「縱列穿透」：`column_grunt` ×6 縱列的橫向 x 偏差 ≤ 5px（確保 L4 可一發穿透全列）
- [ ] 持有 L4（Tier 1）的玩家對完整縱列 ×6，單發 TTK ≤ 持有 L1/L2 同條件 TTK 的 40%（驗證縱列情境的顯著效率優勢）
- [ ] 5 人測試：在 Stage 3 波段中（遭遇 VOLTWYRM 之前）自發描述「穿透多個敵人」或「一發打多個」的比例 ≥ 60%

### L.6 視覺可讀性（UX — 阻斷，繼承 game-concept.md 視覺鐵則）

- [ ] 所有 6 種小怪的子彈在 D4 最高密度下，與玩家判定點的色溫對比一眼可辨（5 人截圖辨識率 ≥ 80%）
- [ ] 武器莢艙（冷藍 / 橙膠囊）在最密集彈幕中不被誤認為敵彈（圓點）：5 人測試誤認率 ≤ 10%
- [ ] 莢艙攜帶者頂部閃爍圖示在含多個敵人的波次中可識別（5 人截圖辨識率 ≥ 80%）

---

*文件版本：1.0.0*
*作者：Level Designer Agent*
*最後更新：2026-07-01*
*狀態：Draft*
*MVP 範圍：Stage 1（礁岩前哨站 REEF OUTPOST ALPHA）＋ CARAPEX 頭目競技場*
*關聯 GDD：game-concept.md | weapon-system.md | kaiju-part-system.md | material-economy.md | kaiju/01-carapex.md | kaiju/02-lacera.md | kaiju/03-voltwyrm.md*
