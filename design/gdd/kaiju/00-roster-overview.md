# 巨獸 Roster 總覽與設計骨幹 (Kaiju Roster Overview & Design Spine)
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/kaiju/00-roster-overview.md*
*最後更新: 2026-07-08*
*狀態: 骨幹已定案（導演核可 +5 隻，共 8 隻）— 各隻細節 GDD 為 01–08*
*用途: 8 頭目的創意脊椎。確保每隻主題連貫、難度分佈合理、且 8 種武器的剋制都被涵蓋。所有 kaiju GDD 與資產化都對齊本文件。*

---

## 1. 設計原則 (Design Principles)

沿用首批 3 隻已凍結的設計哲學，5 隻新頭目一致遵守：

1. **每隻教一種武器/技巧** — 頭目是「武器展示場」。每隻有一個**主剋制武器**（affinity ★★★），但〔武器等功率〕鐵則保證**任何 loadout 都能通關**，只是節奏不同。
2. **部位＝命名發射源 (per-part emitter)** — 頭目不是單體發射，而是一組部位，**每個部位有自己的彈幕型與（可選）移動**。破壞一個部位＝**消音該 emitter**（雷電式）。這是核心獎勵迴圈：破部位→火力變稀→戰場變輕鬆。
3. **階段由破壞狀態驅動，不用 HP 門檻** — Phase 轉換看「哪些部位已破/已剝甲」，不看血量百分比（對齊 kaiju-part-system.md）。
4. **難度只縮放彈幕密度/數量** — TTB（time-to-break）、武器輸出、素材產出跨 D1–D4 不變（difficulty-system.md〔難度是門〕）。
5. **主題綁定核心素材** — 每隻的 `KaijuTheme` 決定其**每個部位都掉**的主題核心（material-economy.md Option A）。新增 5 個主題＝新增 5 種核心。
6. **可讀性鐵則** — 敵彈暖色、玩家彈冷色；ARMOR_INTACT↔ARMOR_STRIPPED 外形強烈區分；軟化簽章 #FF6600 ≤0.5s 可辨。

---

## 2. 8 頭目一覽表 (Roster Table)

| # | 代號 | 中文 | 主題 (Theme) | 建議難度階 | 主剋制武器 | 教學重點 | 部位數 | GDD |
|---|------|------|--------------|-----------|-----------|----------|--------|-----|
| 1 | CARAPEX | 碰殼獸 | 甲殼 Carapace | D1 教學 | L2 集束 × M3 魚雷 | soften→break 核心迴圈 + 裝甲門 | 4 | `01-carapex.md` ✅ |
| 2 | LACERA | 刃肢獸 | 肢體 Limb | D2 | M1 追蹤 | 追蹤剋制移動部位 | 6 | `02-lacera.md` ✅ |
| 3 | VOLTWYRM | 熾蛇 | 能量 Energy | D3 | L4 穿透 | 對齊部位穿透走廊 | 7 | `03-voltwyrm.md` ✅ |
| 4 | **BROODCORE** | **巢母** | **蟲群 Swarm** | **D2** | **M4 集束彈** | **AoE 一次清多個小部位 + L1 散波軟化群** | **7** | `04-broodcore.md` 🆕 |
| 5 | **PRISMSHELL** | **稜殼獸** | **晶簇 Crystal** | **D3** | **L2 集束** | **精準狙擊剝甲後的小弱點 + 裝甲管理** | **6** | `05-prismshell.md` 🆕 |
| 6 | **TIDEMAW** | **潮顎** | **深淵 Abyss** | **D4** | **M2 蜂群** | **持續壓制多個會回甲的顎（消耗戰）** | **6** | `06-tidemaw.md` 🆕 |
| 7 | **EMBERWING** | **燼使** | **餘燼 Ember** | **D4** | **L3 波動** | **蓄力寬幅波一次掃多弱點（寬版型）** | **7** | `07-emberwing.md` 🆕 |
| 8 | **NULLSPIRE** | **虛尖** | **虛空 Void** | **D5 惡夢** | **綜合（無單一支配）** | **畢業考：裝甲門+移動+穿透+壓制+精準全用上** | **8** | `08-nullspire.md` 🆕 |

**建議通關順序（MMX 式非線性選擇，僅為難度提示）**：
CARAPEX → BROODCORE → LACERA → PRISMSHELL → VOLTWYRM → TIDEMAW → EMBERWING → NULLSPIRE。

**武器剋制涵蓋檢核**（8 武器都有一隻頭目當「主場」）：
L1 散波→BROODCORE(輔) · L2 集束→CARAPEX/PRISMSHELL · L3 波動→EMBERWING · L4 穿透→VOLTWYRM · M1 追蹤→LACERA · M2 蜂群→TIDEMAW · M3 魚雷→CARAPEX · M4 集束→BROODCORE。✅ 全覆蓋。

---

## 3. 五隻新頭目設計膠囊 (New Boss Capsules)

各膠囊是 GDD 的種子；細節 10 段格式在各自 04–08 檔展開。

### 3.1 BROODCORE 巢母（蟲群系 Swarm）— D2 · 主剋制 M4 集束彈
- **玩家幻想**：面對一團會生小怪的活體巢穴。不是打一個大目標，而是**修剪一圈會生產的卵囊**——每破一個，戰場的小怪流就少一分，壓力肉眼可見地減輕。
- **外形**：中央搏動核心（BossCore），外圍環繞 **5 個卵囊衛星（spawn-sac，NORMAL）**＋一個裝甲護膜（ARMORED）。整體像脈動的蜂窩/卵團，暖橙紅。
- **部位（7）**：`brood_core`(BossCore) · `sac_n / sac_ne / sac_e / sac_se / sac_s`（暫命名，5 個 NORMAL 卵囊環）· `chitin_veil`(ARMORED 護膜，蓋在核心正面，須剝甲/軟化才露核)。
- **per-part 射擊**：每個卵囊**慢速放射孢子環（Radial 6 發，暖黃）**＋**週期生一隻 `spore_mite` 小怪**（用道中小怪池）。破卵囊＝停孢子＋停生產。護膜破前，核心只放**瞄準脈衝（Aimed 1）**；破護膜後核心露出，改放稀疏放射。
- **教學**：M4 集束彈的 AoE 一次軟化/傷及多個相鄰卵囊；L1 散波同時鋪多個卵囊。單點武器（L2/M1）要一個個清、明顯較慢——但仍可通關。
- **階段**：P1 全卵囊活躍（孢子環＋刷小怪）→ P2 破 ≥3 卵囊（剩餘卵囊放射變快補償）→ P3 護膜破、核心露出（放射孢子＋瞄準脈衝混合）。
- **素材**：主題 `core_swarm`（每部位掉）。

### 3.2 PRISMSHELL 稜殼獸（晶簇系 Crystal）— D3 · 主剋制 L2 集束
- **玩家幻想**：一顆會旋轉、折射子彈的晶洞。裝甲晶面反射出漂亮但致命的光牆；你得**剝開晶面、露出後方針尖大的弱核，用集束光束一發入魂**。
- **外形**：六角晶洞，**4 片旋轉裝甲晶面（prism facet，ARMORED）**環繞中央**多刻面亮核（BossCore）**，冷藍紫＋折射彩邊。
- **部位（6）**：`facet_a / facet_b / facet_c / facet_d`(ARMORED 旋轉晶面) · `weak_node`(NORMAL 小弱點，藏在晶面輪轉的縫隙) · `prism_core`(BossCore 亮核)。
- **per-part 射擊**：每片晶面**折射放射光扇（Radial 5，冷藍，較快）**，且晶面**緩慢公轉**（部位移動：繞中心旋轉）。剝甲（軟化/L3）後晶面暫停折射、露出縫。核心放**十字精準雷（Aimed 4 窄）**。
- **教學**：L2 集束的高精度/穿透適合狙擊小弱核與縫隙；裝甲門要靠熱軟化或 L3 剝開。散射武器對小弱點命中率低、較吃力（但可行）。
- **階段**：P1 四晶面折射＋公轉 → P2 破 2 晶面（剩餘公轉加速）→ P3 核心露出（十字精準雷密度上升）。
- **素材**：主題 `core_crystal`。

### 3.3 TIDEMAW 潮顎（深淵系 Abyss）— D4 · 主剋制 M2 蜂群
- **玩家幻想**：深海巨獸的多張顎，**每張顎的甲會自己長回來**——除非你用蜂群飛彈的「多發小破壞」把每張顎**同時壓住**。這是消耗戰、壓制戰。
- **外形**：橫向寬體海魔，**4 張鰓顎（gill-maw）**沿身體排列，中央**心核（BossCore）**，深藍綠＋生物冷光。
- **部位（6）**：`maw_1 / maw_2 / maw_3`(NORMAL 會回甲的顎，帶輕裝甲門) · `dorsal_plate`(ARMORED 背甲護核) · `heart_core`(BossCore) · `gill_root`(NORMAL 連接根)。
- **per-part 射擊**：每張顎吐**慢速密集瞄準牆（Aimed 寬扇，暖青，慢彈填滿畫面）**——泡幕壓迫，逼你在縫中走位。顎的破甲槽**若一段時間無破壞輸入會回填**（新機制：per-part 破甲衰退/regen）。破背甲後心核露出。
- **教學**：M2 蜂群一次撒多發、能**同時對多張顎補破壞**壓住回甲；集中單體武器只能壓住一張、其他長回來，明顯吃力。
- **階段**：P1 四顎泡幕（回甲慢）→ P2 破 2 顎（剩餘回甲加快、泡幕變密）→ P3 背甲破、心核露（心核加放放射環）。
- **素材**：主題 `core_abyss`。

### 3.4 EMBERWING 燼使（餘燼系 Ember）— D4 · 主剋制 L3 波動
- **玩家幻想**：一隻橫跨整個畫面的燃燒羽翼使者。弱點**散佈在極寬的雙翼上**——窄武器要一個個磨，而蓄力的 L3 寬幅波一次橫掃一整排。
- **外形**：巨大寬展雙翼（占畫面寬 80%+），翼面上散佈燼孔，中央熾心，暖橙紅＋餘燼粒子。
- **部位（7）**：`wing_vent_l1 / wing_vent_l2 / wing_vent_r1 / wing_vent_r2`(NORMAL 翼面燼孔 ×4) · `wing_root_l / wing_root_r`(ARMORED 翼根護甲 ×2) · `heart_core`(BossCore 熾心)。
- **per-part 射擊**：每個燼孔放**螺旋燼臂（Radial 螺旋，暖橙）＋殘焰拖尾**（殘留熱區域，短暫存在）。翼根裝甲鎖住外側燼孔的破壞（要先剝翼根）。熾心放**瞄準爆發（Aimed 扇）**。
- **教學**：L3 蓄力寬幅波一次掃過一整片翼、同時軟化多個燼孔＋剝翼根；點狀武器需來回。寬版型獎勵「範圍/蓄力」思維。
- **階段**：P1 四燼孔螺旋（翼根鎖）→ P2 剝翼根、外燼孔可破（螺旋反向）→ P3 熾心爆發（殘焰密度上升）。
- **素材**：主題 `core_ember`。

### 3.5 NULLSPIRE 虛尖（虛空系 Void）— D5 惡夢 · 綜合畢業考
- **玩家幻想**：終焉之戰。一座不斷變形的虛空方尖碑，把前七隻教過的一切**同時**丟給你——旋轉裝甲盾（碰殼）、公轉衛星發射器（刃肢/稜殼）、對齊穿透脊柱（熾蛇）、多點壓制（潮顎）、精準弱核。沒有單一武器能支配。
- **外形**：漂浮黑曜方尖碑，垂直脊柱＋兩片旋轉虛空盾＋三顆公轉衛星，中央被盾夾住的奇點核，冷紫黑＋虛空扭曲邊。
- **部位（8）**：`spine_seg_1 / spine_seg_2 / spine_seg_3`(NORMAL 對齊脊柱，穿透走廊) · `void_shield_l / void_shield_r`(ARMORED 旋轉盾，會轉去擋) · `satellite_1 / satellite_2`(NORMAL 公轉衛星發射器) · `singularity_core`(BossCore 奇點)。
- **per-part 射擊**：脊柱段放**螺旋（Radial）**（對齊時可 L4 穿透一整列）；旋轉盾**公轉去擋玩家彈**（部位移動：旋轉，兼防禦，須剝甲）；衛星放**瞄準虛空矛（Aimed 窄，冷紫）**且**繞碑公轉**；奇點被盾夾住，露出窗口才可打。
- **教學**：綜合——沒有主剋制。玩家得靈活切換：L4 對脊柱、破盾管理、追蹤/精準清衛星、壓制節奏。
- **階段**：P1 盾旋轉＋衛星公轉＋脊柱螺旋 → P2 破 1 盾＋2 衛星（剩餘加速）→ P3 奇點露出（全部位殘存火力齊發，最終衝刺）。
- **素材**：主題 `core_void`。

---

## 4. 資料模型影響 (Data-Model Impact — 給實作)

實現「不同部位射不同子彈 + 部位移動」需要擴充 schema（現況：`PartDef` 無 emitter/movement 參照，頭目 per-part 射擊只停在文字——見子代理勘查 §F）。**新增（詳見 per-part 射擊 schema story）**：

- **`PartDef`** 新增：
  - `EmitterPatternSO[] Emitters`（可選，null/空＝該部位不發射）— 支援一個部位多 emitter。
  - `PartMovement`（可選）：移動型（None/Orbit 公轉/SweepArc 掃臂/Oscillate 擺盪/Spin 自轉）+ pivot/半徑/角速度/相位參數。對齊 LACERA YAML 的 `movement:` 區塊與 PRISMSHELL/NULLSPIRE 的公轉、EMBERWING 的翼擺。
  - `FireGate`：發射閘門條件（AliveOnly / RequireArmorStripped / SilenceWhenSoftened…）— 讓「剝甲前不發射」「破部位＝消音」資料驅動。
  - （TIDEMAW 用）`ArmorRegen`：per-part 破甲槽回填速率（無破壞輸入時）。
- **`KaijuDef`** 新增：`BodyMovement`（整體漂移/呼吸，對齊既有 BossController.IdleMotion，但改資料驅動）。
- **`KaijuTheme` enum** 加 5 值：`Swarm, Crystal, Abyss, Ember, Void`；`EconomyConfig` 主題→核心對映補 `core_swarm/crystal/abyss/ember/void`。
- **`EnemyTier` enum**（`.cs` 尚未建立）：`Trash/Elite/Mid/Boss` 正式化，供 hit-feel 分級與菁英莢艙掉落。
- 可能新增 `MovementType`（DiveSwoop/Spiral）與 emitter 型（Spiral）供新小怪與部位螺旋彈幕。

> 原則：全部**向後相容擴充**（新欄位可選、預設 null/None），既有 3 隻 .asset 與 448 EditMode 測試不受影響。

---

## 5. 驗收 (Roster-level Acceptance)

- [ ] 8 隻頭目在選頭目 hub（MMX 式）皆可選、可打、可勝（沿用 BossController roster）。
- [ ] 每隻至少 1 個部位擁有獨立 emitter；破該部位後其彈幕停止（per-part 消音可觀測）。
- [ ] 每隻的主剋制武器體感更順，但**所有 8 種 loadout 都能通關**（等功率鐵則）。
- [ ] 難度 D1–D4 只改彈幕密度/數量；TTB 與素材產出不變。
- [ ] 每隻每部位掉對應主題核心；full-clear 給 essence。
- [ ] 新增 schema 欄位向後相容，既有測試全綠。
```
