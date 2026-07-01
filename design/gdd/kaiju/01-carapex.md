# 鎧殼獸 / CARAPEX — 頭目設計文件

`kaiju_id: carapex` | 難度階: 1（教學關）| 三頭目陣容序位: #01

*最後更新：2026-07-01*
*狀態：Draft*
*關聯 GDD：game-concept.md | kaiju-part-system.md | weapon-system.md | material-economy.md*

---

## 1. 概覽 (Overview)

鎧殼獸 CARAPEX 是「殲獸戰機」三頭目陣容的**第一位教學型 Boss**，定位為「軟化→破壞」核心循環的完整示範場。牠是一頭甲殼類 × 甲蟲形態的重裝巨獸：厚甲緩動、攻擊模式低速且高電報，讓玩家能在最低壓力下習得「用雷射蓄熱→用飛彈引爆」這個貫穿全遊戲的打法骨架。背甲炮（ARMORED 部位）的護甲機制進一步引入 L3 波動砲的戰略地位，教導玩家「護甲是閘門，不是厚血條」。戰鬥結束後玩家應能說出：「先把它打熱，再引爆。要破背甲要用波動砲震開它。」

---

## 2. 玩家幻想 (Player Fantasy)

**「第一次把巨獸拆開的感覺」**

目標 MDA 美學（優先序）：

| 美學 | 體驗設計意圖 |
|------|------------|
| **感官愉悅（Sensation）** | 第一次 M3 熱衝擊引爆軟化大顎時的爆裂視聽；素材飛入的即時獎勵感 |
| **能力感（Competence）** | 「我弄懂了蓄熱→引爆節奏」的 Aha moment；L3 震波剝甲成功的「開鎖感」 |
| **發現（Discovery）** | 意外發現 L3 震波撕開背甲、弱點暴露的視覺獎勵 |

CARAPEX 不是要考驗玩家——它是要讓玩家**帶著自信**進入第二、三頭目。爽感來自「我理解了這套系統」，而非「我倖存了」。

---

## 3. 外形與主題 (Silhouette & Theme)

### 設計語言

| 維度 | 設計決策 |
|------|---------|
| **生物原型** | 甲殼類 × 甲蟲混合體：重型鏡面背殼（甲蟲）+ 前端大螯（龍蝦顎）|
| **像素規格** | 畫面佔寬 60–75%；縱向佔高 50–65%（螢幕級壓迫，服務〔科技對巨獸〕支柱）|
| **色系** | 暖色/病態色：深琥珀 + 鏽橙 + 病黃（主甲）；暗紅生物脈動（核心）|
| **動態** | 緩慢水平往復漂移（振幅 = 畫面寬 20%，週期 8s）；大顎周期性開合（攻擊電報）；無急衝（教學階段）|

### 色彩視覺鐵則

- **暖色 = 威脅**：所有敵彈、大顎脈動、背甲炮聚光均為暖色系（橙 / 黃 / 深紅），與玩家亮藍判定點形成 0.1s 可辨識的冷暖對比。
- **ARMOR_INTACT vs ARMOR_STRIPPED 的差異必須夠大**：
  - ARMOR_INTACT：暗黃厚甲紋理，飛彈命中時偏轉火花特效（磁偏轉感）
  - ARMOR_STRIPPED：甲殼爆開，暴露深紅弱點核心（2px 亮白外框脈動） + 右上角 2s 像素倒計時條

---

## 4. 部位組成 (Part Composition)

### 部位總表

| 部位 ID | 名稱 | 類型 | H_max | B_max | 相鄰部位 | drop_table_id | 弱點可見性 |
|---------|------|------|-------|-------|----------|--------------|----------|
| `chest_reactor_core` | 胸口反應爐核心 | BOSS_CORE | 200 HU | 200 BU | left_mandible, right_mandible, dorsal_cannon | `drop_carapex_core` | 永遠可見（大型亮紅標記，hitbox × 1.2）|
| `left_mandible` | 左大顎 | NORMAL | 100 HU | 100 BU | chest_reactor_core, right_mandible | `drop_carapex_normal` | 永遠可見 |
| `right_mandible` | 右大顎 | NORMAL | 100 HU | 100 BU | chest_reactor_core, left_mandible | `drop_carapex_normal` | 永遠可見 |
| `dorsal_cannon` | 背甲炮 | ARMORED | 150 HU | 150 BU | chest_reactor_core | `drop_carapex_armored` | **弱點隱藏**（ARMOR_INTACT 時 BU 鎖定，飛彈偏轉）；L3 震波 → ARMOR_STRIPPED → 2s 露出 |

**H_max / B_max 全部使用全域旋鈕預設值，無覆寫**。CARAPEX 作為教學 Boss，難度定位靠攻擊模式的低密度達成，不壓縮部位耐久。

### 空間佈局（ASCII 示意）

```
    螢幕頂部
  ─────────────────────────────────────────
  │                                       │
  │         ┌──────────────┐              │
  │         │ 背甲炮 ARMED │  ← 頂部居中  │
  │         └──────┬───────┘              │
  │                │ (垂直對齊)            │
  │  ┌──────────┐  ↕  ┌───────────────┐  │
  │  │ 左大顎 N │──┤ 胸口核心 CORE ├──│  │
  │  └──────────┘     └───────────────┘  │
  │                                       │
  ─────────────────────────────────────────
    螢幕底部（玩家區）
```

**垂直對齊情境（L4 穿透雷射利基）**：`dorsal_cannon`（頂部居中）↕ `chest_reactor_core`（中央）垂直對齊，L4 穿透雷射單發可同時命中兩部位（各 D₀ 蓄熱，總 = 2×D₀）。由於背甲炮 ARMOR_INTACT 期間 BU 鎖定，L4 利基主要在加速核心蓄熱——是 L4 的小利基，不是主要策略，符合「L4 真正的展示在 Boss 03」設計意圖。

### Tier-3 相鄰效果快速驗算

| 觸發情境 | 效果 |
|---------|------|
| L2 Tier-3 破點漣漪：左大顎 BROKEN | 核心 +60 HU（200 HU × 30%），右大顎 +30 HU（100 HU × 30%）。若核心 H_current ≥ 140 HU → 同幀觸發 SOFTENED |
| M3 Tier-3 穿甲爆破鏈：左大顎 BROKEN（SOFTENED，is_chain_break=false）| 核心 +15 BU（1.5×D₀ × 1.0 M_state_mult），右大顎 +15 BU（若 ALIVE）|
| M3 Tier-3：大顎 BROKEN，相鄰含背甲炮（ARMOR_INTACT）| 背甲炮 BU + 0（護甲偏轉鏈式傷害，M_state_mult = 0）|

### `assets/data/kaiju/carapex.yaml`

```yaml
kaiju_id: "carapex"
display_name: "鎧殼獸 / CARAPEX"
difficulty_tier: 1
parts:
  - id: "chest_reactor_core"
    type: BOSS_CORE
    H_max_override: null        # 使用全域旋鈕 H_max_boss_core = 200 HU
    B_max_override: null        # 使用全域旋鈕 B_max_boss_core = 200 BU
    adjacency: ["left_mandible", "right_mandible", "dorsal_cannon"]
    drop_table_id: "drop_carapex_core"

  - id: "left_mandible"
    type: NORMAL
    H_max_override: null        # 100 HU
    B_max_override: null        # 100 BU
    adjacency: ["chest_reactor_core", "right_mandible"]
    drop_table_id: "drop_carapex_normal"

  - id: "right_mandible"
    type: NORMAL
    H_max_override: null        # 100 HU
    B_max_override: null        # 100 BU
    adjacency: ["chest_reactor_core", "left_mandible"]
    drop_table_id: "drop_carapex_normal"

  - id: "dorsal_cannon"
    type: ARMORED
    H_max_override: null        # 150 HU
    B_max_override: null        # 150 BU
    adjacency: ["chest_reactor_core"]
    drop_table_id: "drop_carapex_armored"
```

---

## 5. 攻擊模式 (Attack Patterns)

**全域規則**：所有子彈為暖色系，遵守「彈幕永遠讀得懂」視覺鐵則。子彈速度跨四難度階**恆定**；僅彈數與射速依難度縮放（見第 8 節）。

---

### 模式 A：螯牙交叉 (Mandible Cross-fire)

| 屬性 | 值 |
|------|-----|
| **發射源** | left_mandible / right_mandible（交替發射）|
| **彈形** | 3 發扇形（中心瞄準玩家 ±25° 扇角）|
| **子彈速度** | 120 px/s（以 720p 計 ≈ 6s 穿越螢幕；教學速度）|
| **射頻（D1）** | 每顎 1 次/2.5s；兩顎交替 → 玩家每 1.25s 見一次發射 |
| **電報** | 對應大顎脈動為明亮琥珀色，持續 0.5s 後發射 |
| **子彈色** | 橙 #FF8000，清晰像素外框 |
| **觸發條件** | 對應大顎 `break_state == ALIVE` |
| **設計目的** | 教「讀彈→移到彈幕間隙」；交替節奏簡單可預測；大顎破壞即停止，破壞有即時回報 |

---

### 模式 B：背甲礫散 (Dorsal Gravel Spray)

| 屬性 | 值 |
|------|-----|
| **發射源** | `dorsal_cannon` |
| **彈形（ARMOR_INTACT 時）** | 5 發水平寬扇（覆蓋畫面寬度 50%，向下噴散）|
| **彈形（ARMOR_STRIPPED + STAGGERED）** | **停止發射**（硬直 2s）；以亮白脈動閃光標記弱點暴露，倒計時像素條出現 |
| **子彈速度** | 100 px/s |
| **射頻（D1）** | 1 波/9s |
| **子彈色** | 黃 #FFCC00（ARMOR_INTACT）；STAGGERED 期間無子彈 |
| **電報** | 發射前 0.8s，背甲炮頂部出現向下聚光特效 |
| **觸發條件** | `dorsal_cannon` `break_state == ALIVE` |
| **設計目的** | 教「背甲炮有規律威脅，但 L3 震波可以讓它短暫沉默並暴露弱點」|

**ARMOR_STRIPPED 視覺規格（VFX 實作指示）**：
- 甲殼爆裂像素動畫（0.15s 內完成），暴露深紅弱點核心（2px 亮白外框脈動）
- HUD 右上角顯示 2s 倒計時像素條（橙色，與 `stagger_duration` 同步）
- 音效：低沉爆裂聲（甲殼開啟）+ 高頻叮聲（弱點提示）
- 倒計時歸零：甲殼合攏動畫（0.2s），弱點隱藏，倒計時消失

---

### 模式 C：核心光刃 (Core Pulse)

| 屬性 | 值 |
|------|-----|
| **發射源** | `chest_reactor_core` |
| **彈形（Phase 1–2：有大顎存活）** | 1 發瞄準玩家方向的慢速子彈 |
| **彈形（Phase 3：雙顎全 BROKEN）** | 4 發十字形（上下左右，非追蹤，固定方向）|
| **子彈速度** | 90 px/s（最慢；核心子彈最容易閃避）|
| **射頻（D1，Phase 1–2）** | 1 次/4.0s |
| **射頻（D1，Phase 3）** | 1 次/3.0s |
| **子彈色** | 深紅 #CC2200 |
| **電報** | 核心脈動（視覺本身即電報），脈動到峰值後 0.6s 發射 |
| **觸發條件** | 始終啟用；Phase 依存活部位切換彈形 |
| **設計目的** | Phase 1–2：核心有存在感但低威脅；Phase 3：「破掉大顎改變了戰鬥」的直觀體現；全程可讀 |

### 模式觸發條件彙總

| 模式 | 啟動 | 停止 |
|------|------|------|
| A 螯牙交叉 | 對應大顎 `ALIVE` | 對應大顎 `BROKEN` |
| B 背甲礫散（射擊） | `dorsal_cannon` ALIVE + ARMOR_INTACT | `dorsal_cannon` BROKEN（永久）；ARMOR_STRIPPED 期間暫停 2s |
| C 核心光刃 | 始終 | `chest_reactor_core` BROKEN（戰鬥結束）|

---

## 6. 階段 (Phases)

階段由**部位破壞狀態**驅動，非血量閾值，落實「破壞改變戰鬥」設計哲學。

### Phase 1：「鎧甲形態」 *(All Parts ALIVE)*

- **觸發**：戰鬥開始
- **攻擊組合**：模式 A（雙顎交替）+ 模式 B（背甲炮 9s 週期）+ 模式 C（核心慢速 1 aimed/4s）
- **設計意圖**：玩家習得三個模式的節奏；開始用 L2 蓄熱大顎。背甲炮存在但暫不是焦點——它提示「有個東西在頂部攻擊你，且不容易破」。
- **核心爽點時刻**：首次 L2 蓄熱左大顎 → SOFTENED 橙紅脈動 → M3 熱衝擊引爆爆破。這是整個遊戲最關鍵的第一個「Aha moment」，應在玩家首輪遊玩約 2–5 分鐘內發生。

### Phase 2：「碎顎形態」 *(一顎或雙顎 BROKEN)*

- **觸發**：任意大顎 BROKEN
- **攻擊組合**：剩餘大顎的模式 A（如有）+ 模式 B（背甲炮射頻 +15%，約 1 波/7.8s）+ 模式 C（節奏同 Phase 1）
- **設計意圖**：背甲炮在大顎威脅降低後「趁虛而入」，暗示玩家「現在是破背甲的機會」。玩家首次嘗試 L3 蓄力震波 → ARMOR_STRIPPED → 2s 窗口輸出飛彈。
- **教學關鍵點**：L3 震波命中後背甲炮停止射擊、弱點亮起——視覺上「開一扇門讓你打」的感受。BU 跨窗口保留（見 `kaiju-part-system.md` E.2），玩家不需要一次窗口就破完背甲炮，降低挫折感。

### Phase 3：「核心狂怒形態」 *(雙顎均 BROKEN)*

- **觸發**：`left_mandible` 與 `right_mandible` 均 BROKEN
- **攻擊組合**：模式 B（若 `dorsal_cannon` 仍 ALIVE）+ 模式 C 切換為 4-way cross
- **設計意圖**：移除了模式 A 的節奏安全感，核心成為主威脅。玩家面對靜默決策：「要集火核心快速結束，還是繼續磨背甲炮拿 core_carapace？」這是〔頭目是靈魂〕支柱的核心張力——optional parts 的代價與獎勵在此具體化。
- **可選部位張力**：
  - 背甲炮已在 Phase 2 被破 → Phase 3 只剩 4-way cross，非常乾淨
  - 背甲炮仍 ALIVE → Phase 3 需同時應對背甲炮射擊 + 核心 4-way；代價更高，獎勵是 core_carapace

---

## 7. 剋制與偏好 Loadout (Weapon Affinity)

### 武器表現速覽

| 武器 | CARAPEX 表現 | 原因 |
|------|------------|------|
| **L2 集束雷射** | ★★★ 最佳主武器 | 大顎是靜止大目標；37.5 HU/s 對 100 HU → T_soften ≈ 3s 理論（5–6s 實戰）；單部位最快蓄熱 |
| **M3 穿甲魚雷** | ★★★ 最佳副武器 | 配合 L2 軟化，熱衝擊引爆 = 60 BU；2 枚破大顎（NORMAL 100 BU）；是整場主要爆破爽感來源 |
| **L3 波動砲** | ★★★ 必要特殊工具 | 唯一剝甲路徑；不帶 L3 則 dorsal_cannon 實質上不可破（ARMOR_INTACT 時 BU = 0）|
| **L1 散波雷射** | ★★ 可行替代 | 25 HU/s 同時蓄熱雙顎；T_soften ≈ 4.5s，慢於 L2 但可多部位並行蓄熱；配 M3 節奏稍慢但可行 |
| **M2 蜂群飛彈** | ★★ 背甲炮窗口特化 | ARMOR_STRIPPED 2s 內 8 枚齊發對弱點大量填充 BU；其餘時間在大顎上效率低於 M3 |
| **L4 穿透雷射** | ★ 小利基 | dorsal_cannon ↕ chest_reactor_core 垂直對齊，L4 同時蓄兩部位；但 BU 鎖定使利基僅在蓄熱軌 |
| **M1 追蹤飛彈** | ★★ 安全穩定 | 大顎慢速，追蹤優勢未充分發揮；但可提供新手穩定持續輸出，是「學習期」的低壓選項 |
| **M4 叢集炸彈** | ★ 頂部利基 | 背甲炮位於頂部（M4 落點範圍約 25–40% 螢幕高度，可覆蓋）；必須配合 L3 剝甲窗口；學習曲線最高，非教學 Boss 首推 |

### 展示 Loadout：L2 × M3「精準軟化-收割」

本 Loadout 是 CARAPEX 的**設計展示組合**，最直觀地展現軟化→破壞雙軌機制。

> 注意：weapon-system.md C.6 定義的三組標誌搭配中，Combo A (L1×M3) 和 Combo B (L2×M1) 各有不同定位。L2×M3 是跨 Combo 的最優組合，在本 Boss 的靜止大顎情境下超過任何單組。設計師應在 UI/教學材料中將此組合稱為「教學推薦 Loadout」，不必強行對應現有 Combo 標籤。

**戰鬥序列（L2 × M3，NORMAL 大顎，D1）**：

```
T = 0s     ：L2 開始集束照射左大顎（H_rate = 37.5 HU/s）
T = 5–6s   ：H_current 達 100 HU → SOFTENED（橙紅脈動啟動）
T = 6–7s   ：發射第 1 枚 M3 → 熱衝擊引爆：B_fill = 60 BU（M_state_mult 1.0，6×D₀×10）
T = 7.5s   ：換彈期間 L2 繼續蓄熱維持 SOFTENED（H_decay = 3 HU/s，熱量仍高）
T = 8.5s   ：發射第 2 枚 M3 → B_fill = 60 BU → B_current = 120 > 100 → BROKEN！
           ：爆破 VFX + SFX 爆發；素材掉落（core_carapace × 1, shard_common × 3 [Precision]）
           ：若 L2 Tier-3 解鎖 → 破點漣漪：核心 +60 HU、右大顎 +30 HU（可能即時 SOFTENED）
```

**TTB 驗算**：~8.5s 理論（實戰 15–18s，含閃避 downtime）。符合 weapon-system.md D.4 NORMAL 部位 15–25s 目標範圍 ✓

### 無 L2×M3 Loadout 仍可通關（公平性保證）

CARAPEX 在 D1 對**所有合法 Loadout** 均可通關，不存在強制解：

| Loadout 範例 | 策略路線 | 預估 TTB（大顎，D1）|
|-------------|---------|-------------------|
| L2 × M3 | 精準蓄熱引爆（展示路線）| ~15–18s |
| L1 × M3 | 散射雙顎蓄熱，輪流引爆 | ~20–22s |
| L2 × M1 | 集束蓄熱 + 追蹤持續填充 | ~18–22s |
| L3 × M2 | 大顎蓄熱靠 L3 短脈衝；背甲炮 = 輕鬆剝甲 | ~25s（大顎），但 dorsal_cannon 快速 |
| 任意 × M1 | 穩定但慢 | ~22–25s（仍在目標範圍內）|

**無 L3 的 Loadout**：dorsal_cannon 幾乎不可破，但玩家可選擇直接集火核心（BOSS_CORE）通關——這正是「optional parts 是張力，不是強制」的設計意圖。

### 三 Boss 陣容的 Loadout 生態定位

本文件僅設計 Boss #01，但 CARAPEX 的定位應服務整個三 Boss 生態：

| Boss | 設計簡報 Loadout 偏好 | CARAPEX 的反差 |
|------|---------------------|--------------|
| **#01 CARAPEX（本文）** | L2 × M3（精準蓄熱引爆）+ L3 剝甲 | 靜止大目標，精準 > 廣域 |
| **#02 （待設計）** | 待定（建議廣域覆蓋 / 追蹤方向）| CARAPEX 中不展示 M1/M2 最優情境 |
| **#03 （待設計）** | L4 穿透（多部位垂直列）| CARAPEX 只給 L4 小利基，留主展示給 #03 |

---

## 8. 難度縮放 (Difficulty Scaling)

**縮放原則**：僅調整子彈密度（數量與射速），不改變子彈速度、部位數值、傷害或 stagger_duration。完全服從 `kaiju-part-system.md` C.8 難度不縮放規則與〔難度是門，不是牆〕支柱。

### 模式 A：螯牙交叉

| 難度 | 每次齊射彈數 | 每顎射頻 | 效果感知 |
|------|------------|---------|---------|
| D1 Normal | 3 | 1/2.5s | 彈幕寬鬆，有充裕空間學習蓄熱 |
| D2 Hard | 3 | 1/2.0s | 頻率提升，需要主動規避 |
| D3 Extreme | 5 | 1/2.0s | 密度增加，彈幕間隙縮窄 |
| D4 Nightmare | 5 | 1/1.5s | 接近連續壓制，蓄熱 uptime 下降（隱性 TTB 延長）|

### 模式 B：背甲礫散

| 難度 | 每波彈數 | 射頻 |
|------|---------|------|
| D1 Normal | 5 | 1/9s |
| D2 Hard | 7 | 1/8s |
| D3 Extreme | 9 | 1/7s |
| D4 Nightmare | 11 | 1/6s |

> ARMOR_STRIPPED 期間（2s）各難度均暫停模式 B。`stagger_duration` = 2.0s 恆定不縮放。

### 模式 C：核心光刃

| 難度 | Phase 1–2 彈形 | Phase 1–2 射頻 | Phase 3 彈形 | Phase 3 射頻 |
|------|--------------|--------------|------------|------------|
| D1 | 1 aimed | 1/4.0s | 4-way cross（固定方向）| 1/3.0s |
| D2 | 1 aimed | 1/3.5s | 4-way cross | 1/2.5s |
| D3 | 2 aimed（±15°）| 1/3.0s | 4-way + 4-diagonal（8 方向，45° 間隔，非追蹤）| 1/2.0s |
| D4 | 2 aimed（±15°）| 1/2.5s | 4-way + 4-diagonal | 1/1.5s |

> D3/D4 Phase 3 的 8 方向為固定方向，可讀性優先。所有 C 模式子彈均非追蹤。

### Phase 2 背甲炮加速（各難度）

Phase 2 觸發時，模式 B 射頻 × 1.15（約 −1 秒週期）。此係數存於旋鈕 `carapex_phase2_dorsal_speed_mult`（預設 1.15，範圍 1.10–1.30），不硬編碼。

---

## 9. 素材產出 (Material Drops)

### 掉落表定義

| drop_table_id | 對應部位 | 部位類型 | 核心素材 | shard_common（Standard / Precision / Perfect）|
|--------------|---------|---------|---------|----------------------------------------------|
| `drop_carapex_normal` | left_mandible, right_mandible | NORMAL | `core_carapace` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_carapex_armored` | dorsal_cannon | ARMORED | `core_carapace` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_carapex_core` | chest_reactor_core | BOSS_CORE | `core_carapace` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |

*CARAPEX 屬甲殼系（kaiju_carapex）巨獸，所有部位均掉落 `core_carapace`（材料經濟 C.1 巨獸主題映射規則）。碎片計算：`floor(shard_base × quality_mult)`；shard_base = 2，Standard = 1.0×，Precision = 1.5×（floor=3），Perfect = 2.0×（= 4）。依 material-economy.md D.1。*

### 一場狩獵預期產出（D1，Precision 品質為主）

| 通關策略 | core_carapace | shard_common |
|---------|--------------|-------------|
| **全破壞**（4 部位全破）| 4 | 12 + 5 完成獎勵 = **17** |
| **僅破核心**（Speed Run）| 1 | 3 |
| **雙顎 + 核心**（跳過背甲）| 3 | 9 |
| **單顎 + 背甲 + 核心** | 3 | 9 |

### `essence_kaiju` 產出

`on_hunt_end(is_all_parts_broken = true)` 時觸發（4 個部位均 BROKEN）→ `essence_kaiju` × 1 + `shard_completeness_bonus`（= 5 碎片）。

任何難度階均適用，無難度門檻（〔難度是門，不是牆〕支柱）。

### core_carapace 的跨遊戲戰略意義

依 `material-economy.md` C.1，`core_carapace` 用於升級 **L1 散波雷射、M2 蜂群飛彈、M4 叢集炸彈**。CARAPEX 屬**甲殼系（kaiju_carapex）**巨獸，是三頭目陣容中唯一掉落 `core_carapace` 的巨獸——想升上述三種武器的玩家必須主動狩獵 CARAPEX。這是「怪獵式農刷循環」的直接體現：你想升什麼武器，就決定你去打哪隻巨獸（而非哪種部位類型）。LACERA 的尾甲與 VOLTWYRM 的護盾雖也是 ARMORED 部位，但它們各自掉落 `core_limb` 和 `core_energy`，不影響 CARAPEX 作為甲殼核心唯一來源的地位。

---

## 10. 驗收標準 (Acceptance Criteria)

### AC-01 教學循環感知（體驗性 — 阻斷）

- [ ] 5 人新手用戶測試（未接觸過遊戲）：首輪 D1 通關後，受測者能在不提示下描述「雷射打熱 → 飛彈引爆」核心循環，達成率 ≥ 70%
- [ ] 首次大顎破壞發生於遊戲開始後 3 分鐘內（記錄時間戳）
- [ ] 受測者能在 5 分鐘內自行發現 L3 剝甲機制（或通過嘗試 L3 后注意到背甲炮行為改變）

### AC-02 ARMORED 部位護甲閘門正確性（功能性 — 阻斷）

- [ ] `dorsal_cannon` ARMOR_INTACT 期間：任何飛彈命中 `B_fill = 0`（偏轉動畫觸發）
- [ ] L3 蓄力震波命中 → `armor_state = ARMOR_STRIPPED`，`stagger_timer = 2.0s`
- [ ] ARMOR_STRIPPED 視覺（弱點露出 + 倒計時像素條）在命中後 0.3s 內出現（≤ `stagger_visual_onset_max_s`）
- [ ] ARMOR_STRIPPED 期間：`B_fill = break_delta_base × stagger_break_mult`（× 1.5）
- [ ] `stagger_timer` 歸零後：`armor_state = ARMOR_INTACT`，`B_current` 保留不清零
- [ ] 自動化測試：`tests/unit/part-system/armored_part_gate_test`（覆蓋 dorsal_cannon 全流程）

### AC-03 BOSS_CORE 勝利條件（功能性 — 阻斷）

- [ ] `chest_reactor_core` B_current ≥ 200 BU → BROKEN → `on_boss_core_break` 發出 → 勝利結算啟動
- [ ] 即使三個可選部位（雙顎 + 背甲炮）均 ALIVE，核心破壞仍觸發勝利
- [ ] 勝利結算前 `on_part_break`（BOSS_CORE）必先於 `on_boss_core_break` 發出（事件順序保證）

### AC-04 素材掉落正確性（功能性 — 阻斷）

- [ ] `drop_carapex_normal` × 2（雙顎）：各掉 `core_carapace` + `shard_common`（依品質乘數；甲殼系主題規則）
- [ ] `drop_carapex_armored`：掉 `core_carapace` + `shard_common`
- [ ] `drop_carapex_core`：掉 `core_carapace` + `shard_common`
- [ ] Perfect 品質（SOFTENED_STAGGERED_break）：核心數量 = 2（`core_perfect_double_drop = TRUE`）
- [ ] 全破壞結算：`essence_kaiju` × 1 + `shard_completeness_bonus`（= 5）
- [ ] 自動化測試：`tests/unit/economy/material_yield_quality_test`（覆蓋 3 drop_table × 3 品質等級）

### AC-05 彈幕可讀性（體驗性 — UX 阻斷）

- [ ] 5 人用戶測試：含各模式彈幕截圖中，受測者辨識「敵彈 vs 安全間隙」準確率 ≥ 80%
- [ ] SOFTENED 部位橙紅脈動在 D4 最高密度下仍可辨識（彈幕遮蓋時間 ≤ 50%）
- [ ] ARMOR_STRIPPED 弱點露出在 D4 彈幕密度下清晰可見，倒計時像素條不被彈幕完全遮蓋

### AC-06 垂直對齊 L4 利基（功能性）

- [ ] `dorsal_cannon` 與 `chest_reactor_core` 在場景佈局中確認垂直對齊（同一 x 軸 ±5%）
- [ ] L4 穿透雷射單發確認可同時命中兩部位（各自接收蓄熱事件）
- [ ] 由關卡設計師在 Boss 佈局評審時確認，記錄於 Boss 場景設定文件

### AC-07 階段轉換正確性（功能性）

- [ ] 任意大顎 BROKEN → 模式 A 發射源更新，模式 B 射頻 × `carapex_phase2_dorsal_speed_mult`（1.15）
- [ ] 雙顎均 BROKEN → 模式 C 彈形切換至 4-way cross（D1/D2）或 8-way（D3/D4）
- [ ] `dorsal_cannon` BROKEN → 模式 B 永久停止，不影響其他模式
- [ ] `carapex_phase2_dorsal_speed_mult` 存於外部旋鈕，不硬編碼

### AC-08 難度密度縮放正確性（功能性）

- [ ] 子彈速度（模式 A：120 px/s；模式 B：100 px/s；模式 C：90 px/s）在 D1–D4 下恆定
- [ ] 各模式彈數與射頻依第 8 節表格縮放；靜態審核各難度 `spawn_config` 參數
- [ ] 部位 H_max / B_max / stagger_duration 在難度切換後讀取值不變（`difficulty_invariance_test` 覆蓋）

### AC-09 L2 × M3 展示 Loadout TTB 驗算（功能性）

- [ ] L2 × M3，NORMAL 大顎，D1：TTB 實測值 ∈ \[15s, 25s\]（weapon-system.md D.4 目標範圍）
- [ ] 包含在 64 組 loadout TTB 矩陣自動化測試中（`tests/unit/weapon/weapon_loadout_matrix_test`）

---

*文件版本：1.0.0*
*作者：Game Designer Agent*
*最後更新：2026-07-01*
*資料定義：`assets/data/kaiju/carapex.yaml`（inline 見第 4 節）*
*依賴掉落表 ID：`drop_carapex_core` / `drop_carapex_normal` / `drop_carapex_armored`（由 material-economy 實作）*
