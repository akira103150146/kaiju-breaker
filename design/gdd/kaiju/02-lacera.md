# 刃肢獸 / LACERA
## 殲獸戰機 / KAIJU BREAKER — 第二頭目設計文件

*kaiju_id: lacera*
*頭目序號: 02*
*文件路徑: design/gdd/kaiju/02-lacera.md*
*最後更新: 2026-07-01*
*狀態: Draft*
*相依文件: game-concept.md | kaiju-part-system.md | weapon-system.md | material-economy.md*

---

## 1. 概覽 (Overview)

刃肢獸（LACERA）是殲獸戰機第二頭目，定位為「移動系（Movement Boss）」——牠擁有四條獨立揮掃的刃狀肢體（Blade Limbs），每條在各自的弧線上持續運動，形成動態的危險場域。相較於第一頭目的靜態部位設計，LACERA 的所有可破壞 NORMAL 部位均持有動態 `world_position`，使非追蹤武器（尤其 M3 穿甲魚雷（AP Torpedo）與 L2 集束雷射（Focus Beam）直接瞄準肢體時）命中率大幅下降。這場戰鬥是「武器等價設計（Sidegrade Design）」的壓力測試：M1 追蹤飛彈（Homing Missile）配合 L2 集束雷射的「專注-放心（Combo B: Focus & Forget）」在此情境下閃耀——玩家以集束雷射鎖定靜止的頭部核心蓄熱，追蹤飛彈自動清掃四條揮動的肢體，玩家全心專注閃避彈幕。破壞肢體可逐步削弱 LACERA 的攻擊節點並露出頭部核心，形成「以部位狩獵為軸心的獎勵降壓（Reward-Through-Dismemberment）」動態。

---

## 2. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：挑戰（Challenge）＋表達（Expression）＋感官愉悅（Sensation）

> 「四條刃肢像時鐘一樣在我周圍旋轉，我根本無法對準——但當我放棄逐一瞄準、讓飛彈自己去追，專心讓雷射對準那顆頭，我突然感覺到戰場的節奏。破掉第一條肢的瞬間，多一條刃不見了，彈幕少了一道牆——這不是運氣，這是決策的回報。」

**「你追不上它——但追蹤飛彈追得上」**：LACERA 的四肢持續移動是刻意的認知壓力，強迫玩家承認「直接追蹤移動目標是低效的」，然後發現系統為他們設計的解法：M1 追蹤飛彈。這是從挫敗到頓悟（Aha-Moment）的情感弧線設計。

**「砍斷一條腿，世界就安靜一點」**：每破壞一條肢體，彈幕少一個射擊節點，LACERA 的動作幅度也隨之下降——打架的「物理感」讓玩家感知到怪獸正在崩潰，而非只是等待血條歸零。服務「破壞即獎勵（Breaking is the Reward）」支柱。

---

## 3. 外形與主題 (Visual & Theme)

| 屬性 | 描述 |
|------|------|
| **尺寸** | 螢幕級（Screen-Scale）；軀幹佔畫面寬度約 40%，四肢掃及約 70% 範圍 |
| **色彩主調** | 暖色像素（病態黃綠 ＋ 橙褐）；符合「暖色＝威脅」視覺鐵則 |
| **造型靈感** | 多足節肢型——蜈蚣與劍蝦的融合；四條刃肢各帶鋒刃尖端，尾甲為厚重甲片 |
| **軀幹動態** | 本體在畫面上方 30–50% 緩慢上下浮動（振幅 ±5% 螢幕高），肢體相對軀幹獨立運動 |
| **彈幕色彩** | 橙黃主彈（`#FF8C00`）＋橙紅核心彈（`#FF4500`），高對比像素外框，任何彈幕密度下皆可辨識 |
| **視覺分層** | 玩家（冷藍白）← 彈幕（暖橙）← 怪獸（病綠橙褐）：三層色溫在混戰中一眼可分 |
| **部位破壞動畫** | 肢體爆裂後殘留刃基殘骸（stub），尖端像素碎片散落，軀幹不對稱傾斜；強化「怪獸正在失去肢體」的物理感 |
| **尾甲剝甲** | L3 波動砲（Wave Cannon）震波命中後，尾甲外層甲片像素爆飛，弱點核心以暖色脈動標記 |

---

## 4. 部位組成 (Parts & Data)

### 4.1 部位總表

| 部位 ID | 中文名 | 類型 | H_max | B_max | 鄰接部位 | 掉落表 ID | 運動形式 |
|---------|--------|------|-------|-------|---------|---------|---------|
| `head_core` | 頭部核心 | BOSS_CORE | 200 HU | 200 BU | fore_limb_left, fore_limb_right, tail_carapace | `drop_lacera_core` | 跟隨軀幹浮動（相對靜止） |
| `fore_limb_left` | 左前肢刃 | NORMAL | 100 HU | 100 BU | head_core, hind_limb_left | `drop_lacera_limb` | 掃弧 ±60°，45°/s |
| `fore_limb_right` | 右前肢刃 | NORMAL | 100 HU | 100 BU | head_core, hind_limb_right | `drop_lacera_limb` | 掃弧 ±60°，45°/s，反相 |
| `hind_limb_left` | 左後肢刃 | NORMAL | 100 HU | 100 BU | fore_limb_left | `drop_lacera_limb` | 掃弧 ±90°，30°/s |
| `hind_limb_right` | 右後肢刃 | NORMAL | 100 HU | 100 BU | fore_limb_right | `drop_lacera_limb` | 掃弧 ±90°，30°/s |
| `tail_carapace` | 尾甲 | ARMORED | 150 HU | 150 BU | head_core | `drop_lacera_tail` | 擺盪 ±30°，20°/s |

> H_max / B_max 均使用全域旋鈕預設值（`null` = 不覆寫）：NORMAL=100/100，ARMORED=150/150，BOSS_CORE=200/200。詳見 kaiju-part-system.md C.3。

**頭部核心（BOSS_CORE）備注**：
- 永遠可見，有明顯暖橙核心光像素標記（符合 kaiju-part-system.md F.5 BOSS_CORE 視覺優先要求）
- 四肢揮動時會短暫遮擋射線，但核心本身不獨立移動——這是 Combo B 設計意圖的空間基礎
- 破壞即觸發 `on_boss_core_break` 勝利條件

**移動部位的隱性難度效應（非縮放機制）**：NORMAL 肢體的 H_max / B_max 與靜止部位相同，但因持續移動，雷射有效命中 uptime 降低 → 軟化時間 T_soften 自然延長；非追蹤飛彈命中率降低 → BU 填充速率自然下降。TTB 差異來自玩家行為，不來自數值縮放，完全符合「難度是門，不是牆（Difficulty is a Door）」支柱。

### 4.2 相鄰圖 (Adjacency Graph)

```
    head_core
    /    |    \
fore_L  fore_R  tail
  |       |
hind_L  hind_R
```

雙向連接（有向宣告 → 系統雙向推導，遵循 kaiju-part-system.md C.6）：
- head_core ↔ fore_limb_left
- head_core ↔ fore_limb_right
- head_core ↔ tail_carapace
- fore_limb_left ↔ hind_limb_left
- fore_limb_right ↔ hind_limb_right

（後肢只與對應前肢相鄰；尾甲只與核心相鄰——控制 M3 Tier-3 穿甲爆破鏈（AP Chain）擴散，避免單次破壞觸發過多連鎖目標）

**鏈式效果示例**：
- 破壞 `fore_limb_left` → L2 Tier-3 熱量脈衝注入 `head_core`（+60 HU，30% × 200）＋ `hind_limb_left`（+30 HU，30% × 100）
- 破壞 `tail_carapace` → M3 Tier-3 連鎖可觸達 `head_core`（15 BU），是技術玩家的連段獎勵

### 4.3 巨獸定義 YAML (`assets/data/kaiju/lacera.yaml`)

```yaml
kaiju_id: "lacera"
display_name_zh: "刃肢獸"
display_name_en: "LACERA"
kaiju_tier: 2
role: "movement_boss"

body_movement:
  pattern: "vertical_drift"
  amplitude_screen_pct: 5       # ±5% 螢幕高度
  speed_cycles_per_min: 12      # 每分鐘完整上下浮動 12 次（0.2 Hz）

parts:
  - id: "head_core"
    type: BOSS_CORE
    H_max_override: null          # 全域預設 200 HU
    B_max_override: null          # 全域預設 200 BU
    adjacency: ["fore_limb_left", "fore_limb_right", "tail_carapace"]
    drop_table_id: "drop_lacera_core"
    movement:
      type: "stationary_relative"  # 相對軀幹靜止，跟隨 body_movement 浮動
      note: "四肢掃過時遮擋視線，但核心本身不獨立移動——Combo B 策略的空間依據"

  - id: "fore_limb_left"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["head_core", "hind_limb_left"]
    drop_table_id: "drop_lacera_limb"
    movement:
      type: "sweep_arc"
      pivot_bone: "shoulder_left"
      arc_half_deg: 60              # ±60°，總掃幅 120°
      speed_deg_per_s: 45.0         # 快速：0.75 秒掃過一側
      phase_rad: 0.0                # 正弦波相位基準

  - id: "fore_limb_right"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["head_core", "hind_limb_right"]
    drop_table_id: "drop_lacera_limb"
    movement:
      type: "sweep_arc"
      pivot_bone: "shoulder_right"
      arc_half_deg: 60
      speed_deg_per_s: 45.0
      phase_rad: 3.14159             # π 反相——左右前肢對向掃，避免同步

  - id: "hind_limb_left"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["fore_limb_left"]
    drop_table_id: "drop_lacera_limb"
    movement:
      type: "sweep_arc"
      pivot_bone: "hip_left"
      arc_half_deg: 90               # ±90°，總掃幅 180°（更大）
      speed_deg_per_s: 30.0          # 稍慢但範圍更廣；3 秒掃過一側
      phase_rad: 1.5708              # π/2 相位差，與前肢錯開節奏

  - id: "hind_limb_right"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["fore_limb_right"]
    drop_table_id: "drop_lacera_limb"
    movement:
      type: "sweep_arc"
      pivot_bone: "hip_right"
      arc_half_deg: 90
      speed_deg_per_s: 30.0
      phase_rad: 4.7124              # 3π/2 相位差

  - id: "tail_carapace"
    type: ARMORED
    H_max_override: null             # 全域預設 150 HU
    B_max_override: null             # 全域預設 150 BU
    adjacency: ["head_core"]
    drop_table_id: "drop_lacera_tail"
    movement:
      type: "oscillate"
      pivot_bone: "tail_base"
      arc_half_deg: 30               # ±30°，緩慢擺盪
      speed_deg_per_s: 20.0          # 最慢：1.5 秒掃過一側
      phase_rad: 0.0
    design_note: >
      drop_lacera_tail 產出 core_limb，符合肢體系巨獸（kaiju_lacera）主題規則——
      刃肢獸所有部位（NORMAL 肢體、ARMORED 尾甲、BOSS_CORE）均掉落 core_limb，
      使 LACERA 成為 Combo B（L2×M1）對應武器（L2, L4, M1）升級素材的主要農場，
      形成正反饋素材循環（見 material-economy.md C.1 巨獸主題映射）。
```

---

## 5. 攻擊模式 (Attack Patterns)

所有彈幕均以暖色輸出（橙黃 `#FF8C00` 主彈 ＋ 橙紅 `#FF4500` 聚肢彈），符合「暖色＝威脅」視覺鐵則。彈幕密度由難度縮放（見第 8 節），其餘所有參數固定。

### 模式 A：刃浪掃射（Blade Wave Barrage）

**觸發**：Phase 1 & 2，每條存活肢體獨立觸發（非同步）——掃過中心位置時射擊

**描述**：每條肢體在掃弧通過中心角時，向當前移動方向發射一道扇形彈幕。四條肢體相位各異（phase_rad 設計確保不同步），形成不規則的「刃浪」節奏：不是整齊的子彈牆，而是需要讀取時機的斷續掃蕩。

| 難度 | 每肢每次彈數 | 扇形角 | 彈間距特徵 |
|------|------------|--------|---------|
| Tier 1（易） | 3 | 60° | 間距寬，可從彈間穿過 |
| Tier 2（普） | 4 | 60° | 間距縮窄，需位置意識 |
| Tier 3（難） | 5 | 60° | 密集，需提前閃位 |
| Tier 4（夢魘） | 6 | 60° | 近乎封閉，需精確走位 |

**觸發節拍**（參考）：
- 前肢（±60° / 45°/s）：掃弧來回週期 ≈ 2.67 秒；每週期觸發 2 次（通過中心各一次）
- 後肢（±90° / 30°/s）：掃弧來回週期 ≈ 6 秒；每週期觸發 2 次

### 模式 B：聚肢爆彈（Convergence Burst）

**觸發**：Phase 1，每 12–18 秒觸發一次（罕見高壓時刻）；Phase 2 後停用

**描述**：四條肢體同時強制移到中心位置（覆蓋正常掃弧動畫），約 0.5 秒後同時向玩家方向發射各自的彈簇——形成「四面合圍」的瞬間壓力高峰。已破壞的肢體不參與此模式（此為「狩獵肢體的直接獎勵」的核心體現）。

| 難度 | 每肢每次彈數 | 模式 |
|------|------------|------|
| Tier 1 | 4 | 向心扇形 90°（前方） |
| Tier 2 | 5 | 向心扇形 90° ＋ 1 枚側向補彈 |
| Tier 3 | 6 | 向心扇形 120° |
| Tier 4 | 8 | 向心扇形 120° ＋ 後補彈 2 枚 |

**視覺提示（Visual Cue）**：觸發前 0.5 秒，所有存活肢體刃尖發出橙紅蓄力閃光（charge-up 像素動畫），提供可讀的閃避準備窗口。符合「彈幕永遠讀得懂」視覺鐵則。

### 模式 C：殘肢亂舞（Berserker Whirl）

**觸發**：Phase 2（≥2 肢破壞後），取代或補充模式 A；Phase 1 不觸發

**描述**：殘存肢體移動速度提升（×1.5），射擊頻率也提升（每弧來回射擊 2 次而非 1 次）。總射擊節點雖然減少，但每個節點更難預測——是「破壞帶來的節奏改變」而非單純放鬆。M1 追蹤飛彈在此階段相對優勢更加明顯：追蹤效果不受速度增加影響。

| 難度 | 每肢每次彈數 | 射擊次數 / 弧 |
|------|------------|------------|
| Tier 1 | 3 | 1 次 |
| Tier 2 | 4 | 1 次 |
| Tier 3 | 4 | 2 次 |
| Tier 4 | 5 | 2 次 |

---

## 6. 階段 (Phases)

「狩獵肢體 = 直接獎勵降壓（Reward-Through-Dismemberment）」：每破一條肢，戰場立刻感知改變。這是「頭目是靈魂（The Boss is the Soul）」支柱的核心體現——玩家的每個戰術決策都有即時的環境反饋。

| 階段 | 觸發條件 | 彈幕射擊節點 | 核心暴露度 | 主要模式 | 玩家體驗弧線 |
|------|---------|-----------|---------|---------|-----------|
| **Phase 1：全肢掠食者** | 戰鬥開始，所有肢完好 | 4 個移動節點 | 低（四肢遮擋頻繁） | A ＋ B 週期輪替 | 最混亂；學習讀取掃弧節奏，發現 Combo B 解法 |
| **Phase 2：斷肢狂怒** | ≥2 NORMAL 肢破壞 | 2–3 個節點，速度 ×1.5 | 中（1–2 肢遮擋移除） | A ＋ C，B 停用 | 張力仍高但格局轉清；核心更易對準 |
| **Phase 3：核心裸露** | ≥3 NORMAL 肢破壞 | 0–1 個節點 | 高（核心幾乎無遮擋） | 僅殘存肢的 C（可能為空）＋ 尾甲緩慢擺盪 | 清算階段；玩家集中火力 head_core |

**尾甲的選擇性角色**：尾甲（ARMORED）在任何階段均可選擇性攻破。破壞它不改變主體彈幕模式，但因與 `head_core` 相鄰，M3 Tier-3 或 L2 Tier-3 鏈式效果可從尾甲觸達核心。最佳完整策略：先破 4 肢 ＋ 尾甲，最後集中攻 head_core 以觸發 `essence_kaiju` 全破壞獎勵。

**L4 穿透雷射（Pierce Beam）垂直窗口**（符合 weapon-system.md F.5 關卡設計需求）：Phase 1 中，head_core（頂部）與 hind_limb（底部）在各自的掃弧週期中會短暫對齊垂直軸，形成 L4 同時命中 2 個部位的機會窗口。因後肢 ±90° 弧線，這個窗口每約 6 秒出現一次，持續 0.3–0.5 秒。關卡設計師需於 Boss 評審確認垂直對齊可達性（見驗收標準 10.6）。

---

## 7. 剋制與偏好 Loadout (Loadout Analysis)

### 7.1 推薦 Loadout：Combo B 專注-放心（L2 集束雷射 × M1 追蹤飛彈）

這是 LACERA 的**設計展示 Loadout（Showcase Loadout）**，直接服務「橫向選擇（Sidegrades, not Upgrades）」支柱。

**打法節奏**：
1. 玩家以 L2 集束雷射對準**靜止的 `head_core`**（非移動肢體）持續蓄熱 → 100% 雷射 uptime
2. M1 追蹤飛彈自動追蹤掃弧中的 fore/hind limbs，逐一填充各肢 BU
3. 玩家全部注意力投入**閃避彈幕**，無需分心追蹤移動肢體
4. 隨著 M1 逐漸擊破各肢，模式 A 的射擊節點減少，彈幕壓力自然降低
5. L2 維持的蓄熱使 `head_core` 進入 SOFTENED，M1 殘余飛彈填滿 BU 觸發勝利

**為何「專注-放心」在此特別強力**：
- L2 目標（head_core）不移動 → 100% 蓄熱 uptime
- M1 目標（4 個移動肢）持續移動 → 追蹤彌補了移動帶來的命中困難
- 玩家被解放出來，只需做一件事：閃避

### 7.2 武器效率對比矩陣

| 武器 | 對移動 NORMAL 肢 | 對靜止 BOSS_CORE | 對 ARMORED 尾甲 | 整體評價 |
|------|----------------|----------------|----------------|---------|
| **L1 散波雷射** | 中（廣域蓄熱，部分命中移動目標） | 中（三束分散，靜止尚可） | 中 | ▲ 可用，廣域策略 |
| **L2 集束雷射** | 低（極窄束 × 移動 = 低 uptime） | ★ 高（靜止目標 100% uptime） | 中 | ★ Combo B 最優（鎖定 core） |
| **L3 波動砲** | 低（蓄力 1.5s 期間無法預判肢位） | 中（短脈衝有效） | ★ 唯一剝甲路徑 | ▲ 開尾甲專用，非主力蓄熱 |
| **L4 穿透雷射** | 低-中（垂直窗口偶發） | 中 | 低 | ▲ 利用垂直窗口需主動讀位 |
| **M1 追蹤飛彈** | ★ 最高（追蹤自動命中移動目標） | 中（靜止時追蹤優勢無效但仍可用） | 中（護甲偏轉） | ★ Combo B 最優（追蹤肢體） |
| **M2 蜂群飛彈** | 中（廣域撒佈，部分命中） | 低（傷害分散至各目標） | 低-中 | ▲ 可用，效率分散 |
| **M3 穿甲魚雷** | ✗ 低（無追蹤 × 中速 × 移動目標 = 高 miss 率） | 中-高（靜止核心有效） | 低（護甲偏轉魚雷） | △ 顯著較弱，但有替代路線（見 7.3） |
| **M4 叢集炸彈** | 低（AoE 落點固定，移動肢可能離開範圍） | 中（頭部若在 AoE 範圍） | 低 | ▲ 頭部叢集偶發，非設計情境 |

### 7.3 M3 在此「顯著較弱但仍公平」的閉合邏輯

**較弱的原因**：
- M3 穿甲魚雷中速直線無追蹤——對正在 45°/s 掃弧的前肢，需預判 0.5–0.8 秒後的落點位置（約 22–35° 位移），實戰命中率 < 50%
- 彈匣僅 3 枚、換彈 4 秒：miss 一枚代價高昂，高壓彈幕下瞄準窗口短暫
- 熱衝擊引爆（Heat-Shock Detonation）條件需先蓄熱肢體至 SOFTENED，而 L2 對移動肢的低 uptime 使蓄熱慢

**仍公平的原因**：
- 玩家可選擇以 M3 瞄準**靜止的 `head_core`**——犧牲肢體清除，主攻勝利條件；此路線合法，只是效率低於 Combo B
- M3 Tier-3 穿甲爆破鏈：破壞尾甲後觸達 `head_core`（相鄰），是 M3 玩家的技術回饋窗口
- M3 + L1（散波多部位蓄熱）= 可行的「跳過肢體、直打核心」路線
- 這是〔橫向選擇〕的核心承諾：M3 在此情境不是「最優」，但絕非「廢棄選項」——它有自己的路線，只是更要求玩家適應

### 7.4 L2「對移動肢低效」的設計意圖

L2 集束雷射直接瞄準移動肢時確實低效，但 Combo B 的核心是「**不**用 L2 瞄準移動肢」。設計傳達的訊息是：

> 「你可以用錯誤的方式掙扎（L2 追著肢體跑），也可以用對的方式輕鬆（L2 鎖定靜止核心，M1 自動追肢體）。發現這個差異，就是這關的玩法頓悟。」

L2 在此情境不是廢棄選項，只是「需要更有技巧的使用方式」——完全符合〔橫向選擇〕不排除任何武器的設計承諾。

---

## 8. 難度縮放 (Difficulty Scaling)

嚴格落實「難度是門，不是牆（Difficulty is a Door）」支柱：**唯一縮放維度 = 彈幕密度（Bullet Density）**。

| 縮放項目 | Tier 1 | Tier 2 | Tier 3 | Tier 4 |
|---------|--------|--------|--------|--------|
| 模式 A 每肢彈數 | 3 | 4 | 5 | 6 |
| 模式 B 每肢彈數 | 4 | 5 | 6 | 8 |
| 模式 C 每肢彈數 | 3 | 4 | 4 | 5 |
| 模式 C 射擊次數 / 弧 | 1 | 1 | 2 | 2 |

**恆定不縮放的所有項目**：

| 項目 | 原因 |
|------|------|
| 部位 H_max / B_max | 部位系統難度不縮放（kaiju-part-system.md C.8） |
| 肢體移動速度（°/s） | 移動速度縮放會破壞 M1 showcase 的清晰對比 |
| 模式觸發週期與間隔 | 節奏縮放影響可讀性，違背「彈幕永遠讀得懂」鐵則 |
| 彈幕扇形角 | 形狀改變影響玩家對模式的識別與記憶 |
| 尾甲 stagger 窗口長度 | 繼承全域 `stagger_duration`（2.0s），不因難度改變 |
| 素材掉落數量與品質 | 難度不影響素材系統（kaiju-part-system.md C.8） |
| Phase 切換閾值（≥2 肢，≥3 肢） | 破壞門檻固定，獎勵路線對所有難度開放 |

**難度的隱性效應**：高難度下彈幕更密 → 玩家閃避頻率上升 → 雷射有效命中 uptime 下降 → TTB 自然延長。此隱性機制已在 kaiju-part-system.md C.8 確認，無需本系統額外設定。

---

## 9. 素材產出 (Material Output)

### 9.1 掉落表定義

| 掉落表 ID | 對應部位 | 部位類型 | 核心素材輸出 | 備注 |
|---------|---------|---------|-----------|------|
| `drop_lacera_core` | head_core | BOSS_CORE | `core_limb` | 肢體系主題（kaiju_lacera）規則：勝利條件觸發，必然掉落 |
| `drop_lacera_limb` | fore_limb_left/right, hind_limb_left/right | NORMAL | `core_limb` | 肢體系主題規則：各肢獨立掉落 |
| `drop_lacera_tail` | tail_carapace | ARMORED | `core_limb` | 肢體系主題規則：LACERA 所有部位統一掉落 `core_limb`（見 material-economy.md C.1） |

LACERA 屬**肢體系（kaiju_lacera）**巨獸，所有部位（NORMAL 肢體、ARMORED 尾甲、BOSS_CORE 頭核）均掉落 `core_limb`，使 LACERA 成為 Combo B（L2×M1）對應武器升級素材的主要農場——「打展示這個 Loadout 的怪獸，就能升展示這個 Loadout 的武器」。

所有部位亦依 Break Quality 產出 `shard_common`（Standard × 1.0，Precision × 1.5，Perfect × 2.0，公式見 material-economy.md D.1）。

### 9.2 每場預期產量

以 Precision（SOFTENED 狀態）破壞品質為基準，假設玩家破壞所有 6 部位：

| 素材 | 每場預期產量 | 說明 |
|------|-----------|------|
| `shard_common` | 12–18 個 | 6 部位 × ~3 碎片（Precision）＋ 結算 5（全破）≈ 23；部位依難度策略可少 |
| `core_limb` | 5–6 個 / 場 | 4 NORMAL 肢各 1 ＋ 尾甲 1 ＋ head_core 1（肢體系主題：所有部位均給 core_limb）；Perfect 破壞時各部位可給 2 |
| `essence_kaiju` | ~0.55 / 場 | 全破壞 6 部位後結算；需先破尾甲再擊核心 |

**全破壞最佳策略**：先清 4 NORMAL 肢 → L3 剝甲 ＋ 攻破尾甲 → 最後集中攻 head_core（此順序確保尾甲在核心觸發勝利前被破壞）。

### 9.3 Core Limb 循環對齊（Limb Core Farming Loop）

LACERA 是 `core_limb` 的主要農場巨獸（每場 5–6 枚：4 NORMAL 肢體 + ARMORED 尾甲 + BOSS_CORE 頭核各 1），對應 Combo B 武器升級需求：

| 武器 | Tier 1→2 需 core_limb | Tier 2→3 需 core_limb | 建議 LACERA 狩獵次數（取最高瓶頸） |
|------|---------------------|---------------------|-------------------------------|
| L2 集束雷射 | 5 | 8 | T1→2 ≈ 1 場；T2→3 ≈ 2 場（5 枚/場基準，含 head_core） |
| M1 追蹤飛彈 | 5 | 8 | 同上 |
| L4 穿透雷射 | 5 | 8 | 同上 |

→ 素材循環與 Boss 主題完全對齊：「想升展示 Loadout 的武器，就去打展示這個 Loadout 的怪獸」。

---

## 10. 驗收標準 (Acceptance Criteria)

### 10.1 移動部位系統正確性（功能性 — 阻斷）

- [ ] 所有 NORMAL 肢體的 `world_position` 每幀按各自 `sweep_arc` 規格更新（pivot_bone + arc_half_deg + speed_deg_per_s + phase_rad 均正確）
- [ ] M1 追蹤飛彈在 ±60° 追蹤角內能追蹤各 limb 的實際動態 `world_position`（非固定座標）
- [ ] 部位破壞後 `world_position` 更新停止；`on_part_break` 攜帶的 `world_position` 為破壞瞬間的動態位置（素材掉落在正確的動態位置而非初始位置）
- [ ] 軀幹 `body_movement`（vertical_drift）正確帶動 head_core 和 stationary_relative 部位的座標

### 10.2 武器差異化驗證（體驗性 — 設計主要關注）

- [ ] M3 穿甲魚雷對 NORMAL 移動肢的**實際命中率**（Tier 1 難度，10 次射擊統計）< 50%（對靜止目標同條件應 > 85%）——確認移動部位帶來顯著命中難度差異，未超出設計預期
- [ ] M1 追蹤飛彈對 NORMAL 移動肢的實際命中率 ≥ 80%（驗證追蹤自動解決移動命中困難）
- [ ] Combo B（L2 × M1，武器均 Tier 1）完整戰鬥 TTB（含 4 肢 ＋ head_core）落在 60–90 秒範圍（Tier 1 難度基準）
- [ ] 上述 TTB 測試覆蓋至少 5 次完整戰鬥並取平均值

### 10.3 階段降壓感知（體驗性）

- [ ] 破壞第一條 NORMAL 肢後，玩家無需提示即可感知彈幕減少（模式 A 少一個射擊節點）——5 人 playtest 中 ≥ 4 人自主描述「感覺輕鬆了」或類似反應
- [ ] Phase 2 觸發（≥2 肢破壞）後，殘肢速度加快在視覺上可感知——視覺設計師於 Boss 評審確認
- [ ] 破壞所有 4 肢後（進入 Phase 3），玩家在 30 秒內能對準 head_core 成功攻擊（確認核心暴露度達設計意圖）

### 10.4 彈幕可讀性（視覺 — UX 阻斷，繼承 game-concept.md 視覺鐵則）

- [ ] LACERA 所有彈幕（模式 A/B/C）使用暖色（橙黃 `#FF8C00` 或近似色）＋清晰像素外框
- [ ] 在 Tier 4（夢魘）最高密度下，玩家判定點（冷藍白）與 LACERA 彈幕（暖橙）的色溫對比仍一眼可辨——5 人測試截圖辨識率 ≥ 80%（繼承 kaiju-part-system.md H.2 測試方法）
- [ ] 模式 B 觸發前的 0.5 秒 charge-up 閃光在截圖靜態測試中被 ≥ 80% 受測者正確識別為「即將發生攻擊」

### 10.5 素材掉落正確性（功能性 — 阻斷）

- [ ] NORMAL 肢破壞 → 掉落 `core_limb`（via `drop_lacera_limb`；肢體系主題規則）
- [ ] BOSS_CORE 破壞 → 掉落 `core_limb`（via `drop_lacera_core`；肢體系主題規則）
- [ ] ARMORED 尾甲破壞 → 掉落 `core_limb`（via `drop_lacera_tail`；肢體系主題規則，與其他部位一致）
- [ ] 全破壞 6 部位後結算 → `essence_kaiju` × 1 ＋ `shard_completeness_bonus`（5 碎片）正確觸發
- [ ] 戰鬥失敗（未擊倒 head_core）時，已破壞部位的 `core_limb` / `shard_common` 永久保留（kaiju-part-system.md C.7 ＋ material-economy.md E.1 聯合驗收）

### 10.6 L4 穿透雷射垂直窗口存在性（功能性，繼承 weapon-system.md F.5）

- [ ] 在一次完整 Phase 1 戰鬥中，head_core 與任意 hind_limb 的 Y 座標差距 ≤ 20 像素的垂直對齊窗口，出現次數 ≥ 8 次（確保 L4 在此 Boss 有有效情境）
- [ ] 關卡設計師於 Boss 設計評審中確認並記錄此窗口的典型節奏，標記於關卡評審文件

---

## 附錄：開放問題 (Open Questions)

| 優先級 | 問題 | 阻斷里程碑 | 解答方式 |
|--------|------|------------|---------|
| **高** | 移動速度 45°/s（前肢）是否帶來的命中難度差異是否落在「顯著但不絕望」範圍？需原型實測（目標 M3 < 50% 命中率，M1 > 80%） | Prototype | 原型測試 10 次 M1 / M3 對 fore_limb 的命中率 |
| **高** | Phase 2 殘肢速度 ×1.5 後，模式 C 的彈幕密度是否製造「更難但更清晰」的體驗，還是「更混亂更難讀」？ | Vertical Slice | 5 人 playtest 後問卷：「Phase 2 比 Phase 1 更清晰還是更混亂？」目標 ≥ 3/5 回答「更清晰」 |
| **中** | LACERA 為三頭目陣容中 `core_limb` 的**唯一來源**（肢體系主題規則）——每場 5–6 枚的高產量是否使玩家跳過其他巨獸、只農 LACERA？ | Vertical Slice | 監測 playtest 的巨獸選擇分布；若碎片需求帶動玩家仍需交替狩獵，則高 core_limb 產率不成問題；若 LACERA 農刷率 > 70% 且影響 core_carapace / core_energy 的自然積累，考慮微調 NORMAL 肢體的核心掉落率（不改變核心類型） |

---

*文件版本：1.0.0*
*作者：Game Designer Agent*
*kaiju_id：lacera | 頭目序號：02*
*關聯 GDD：game-concept.md | kaiju-part-system.md（LOCKED 狀態機）| weapon-system.md（LOCKED 武器數值）| material-economy.md（drop_table 覆寫需協調）*
