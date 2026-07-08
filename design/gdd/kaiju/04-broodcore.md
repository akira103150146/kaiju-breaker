# 巢母 / BROODCORE — 頭目設計文件

`kaiju_id: broodcore` | 難度階: 2（建議）｜ 8 頭目陣容序位: #04 ｜ 建議通關順序第 2 站

*最後更新：2026-07-08*
*狀態：Draft*
*關聯 GDD：00-roster-overview.md §3.1 | kaiju-part-system.md | weapon-system.md（LOCKED）| material-economy.md*

---

## 1. 概覽 (Overview)

巢母 BROODCORE 是「殲獸戰機」8 頭目陣容中的**蟲群系（Swarm）**頭目，定位為「AoE 群體壓制」的教學場。牠不是一個難纏的單體，而是一座活體卵巢：中央搏動核心（BossCore）被一片護甲護膜（ARMORED）遮蔽，外圍環繞 5 個持續放射孢子並週期性孵化小怪的卵囊衛星（NORMAL）。玩家面對的核心壓力不是單一高傷害攻擊，而是**持續累加的多源威脅**——5 個孢子環同時放射、小怪流不斷補充。M4 集束炸彈的範圍傷害與 L1 散波雷射的多束覆蓋在此有天生舞台：一次攻擊行動能同時軟化甚至同時擊破相鄰的卵囊，讓玩家直觀理解「AoE 武器如何對抗群體威脅」；單點武器（L2 集束、M3 魚雷）依然完全可行，只是需要逐一清理、節奏較慢。戰鬥結束後玩家應能說出：「這隻頭目教我用範圍武器一次清乾淨一群卵囊，比一個一個打乾脆多了。」

---

## 2. 玩家幻想 (Player Fantasy)

**「修剪一座活著的蜂巢」**

目標 MDA 美學（優先序）：

| 美學 | 體驗設計意圖 |
|------|------------|
| **挑戰（Challenge）** | 5 個同時運作的威脅源 + 持續小怪流，逼玩家學會用範圍思維管理戰場，而非逐一鎖定 |
| **感官愉悅（Sensation）** | M4 炸彈落下、2 個相鄰卵囊同時爆裂噴出素材的「雙破」瞬間；孢子環停止時戰場瞬間安靜下來的解壓感 |
| **能力感（Competence）** | 「我打爆越多卵囊，戰場的小怪和彈幕就越少」——破壞與壓力降低的因果關係全程可讀可感 |

BROODCORE 不是要用高傷害懲罰玩家，而是用**數量**製造壓力——解法永遠是「破壞」，只是這次獎勵範圍武器的效率。玩家離場時應該有「我掌握了戰場，而不是我僥倖存活」的感受。

---

## 3. 外形與主題 (Silhouette & Theme)

### 設計語言

| 維度 | 設計決策 |
|------|---------|
| **生物原型** | 蟲卵巢母：脈動的有機蜂窩核心 + 5 個環繞的膜質卵囊 + 一片幾丁質（甲殼素）護膜覆蓋核心正面 |
| **像素規格** | 畫面佔寬 55–65%；縱向佔高 45–55%（比 CARAPEX 略緊湊，服務「群體多點」而非「單體巨大」的壓迫感）|
| **色系** | 暖色/病態色：暗橙紅底色（母體）+ 病態黃綠脈管紋理（卵囊）+ 深琥珀幾丁質（護膜）|
| **動態** | 呼吸式脈動（整體縮放 ±5%，週期 4s，服務「活著的巢穴」幻想）+ 緩慢水平漂移（振幅 = 畫面寬 10%，週期 12s，比 CARAPEX 更「扎根」、較不主動）；卵囊與核心本身**無獨立部位移動**（PartMovement = None，全部位）——本頭目的教學重點是「per-part 消音節奏」與「AoE 群體效率」，移動掌握留給 LACERA / PRISMSHELL |

### 色彩視覺鐵則

- **暖色 = 威脅**：孢子彈幕、核心脈衝、母體脈動皆為暖色系（黃 / 橙 / 深紅），與玩家亮藍判定點形成清楚冷暖對比
- **ARMOR_INTACT vs ARMOR_STRIPPED 差異**（`chitin_veil`）：
  - ARMOR_INTACT：深琥珀幾丁質厚甲紋理，飛彈命中偏轉火花（未軟化時）
  - ARMOR_STRIPPED：甲殼裂開，暴露深紅核心弱點框（2px 亮白外框脈動）+ 右上角 2s 倒計時像素條
- **SOFTENED 卵囊**：橙紅脈動光暈（與 CARAPEX 一致的軟化語言，跨頭目視覺統一）
- **spore_mite 小怪**：沿用道中小怪池既有配色與判定規則，不在本文件重新定義

---

## 4. 部位組成 (Part Composition)

### 部位總表（7 部位）

| 部位 ID | 名稱 | 類型 | H_max | B_max | 相鄰部位 | drop_table_id | 弱點可見性 |
|---------|------|------|-------|-------|----------|--------------|----------|
| `brood_core` | 巢母核心 | BOSS_CORE | 200 HU | 200 BU | `chitin_veil` | `drop_broodcore_core` | 永遠可見（大型亮紅標記，hitbox × 1.2）|
| `sac_n` | 卵囊·頂 | NORMAL | 100 HU | 100 BU | `sac_ne`, `sac_s` | `drop_broodcore_normal` | 永遠可見 |
| `sac_ne` | 卵囊·右上 | NORMAL | 100 HU | 100 BU | `sac_n`, `sac_e` | `drop_broodcore_normal` | 永遠可見 |
| `sac_e` | 卵囊·右 | NORMAL | 100 HU | 100 BU | `sac_ne`, `sac_se` | `drop_broodcore_normal` | 永遠可見 |
| `sac_se` | 卵囊·右下 | NORMAL | 100 HU | 100 BU | `sac_e`, `sac_s` | `drop_broodcore_normal` | 永遠可見 |
| `sac_s` | 卵囊·底 | NORMAL | 100 HU | 100 BU | `sac_se`, `sac_n` | `drop_broodcore_normal` | 永遠可見 |
| `chitin_veil` | 幾丁護膜 | ARMORED | 150 HU | 150 BU | `brood_core` | `drop_broodcore_armored` | **弱點隱藏**（ARMOR_INTACT 且未軟化時判定框不可命中）；達 SOFTENED 或 L3 震波剝甲後露出弱點框 |

**H_max / B_max 全部使用全域旋鈕預設值，無覆寫**。與 CARAPEX 相同的設計立場：BROODCORE 的難度定位靠**部位數量與per-part 消音節奏**達成，不透過壓縮個別部位血量製造挑戰。

**相鄰圖拓撲**：5 個卵囊構成一個**五邊環（pentagon ring）**——`sac_n → sac_ne → sac_e → sac_se → sac_s → (回到 sac_n)`，每個卵囊恰有 2 個鄰居（在 `adjacency_max_neighbors`=4 上限內）。`chitin_veil` 只與 `brood_core` 相鄰（護膜直接覆蓋核心，不與卵囊環相鄰）。這個環狀拓撲是 L2 Tier-3「破點漣漪」與 M3 Tier-3「穿甲爆破鏈」在卵囊環上連續傳播的空間基礎。

### 空間佈局（ASCII 示意）

```
    螢幕頂部（最遠，環頂）
  ─────────────────────────────────────────
  │                ┌──────────┐              │
  │                │  sac_n   │              │
  │                └────┬─────┘              │
  │     ┌──────────┐    │    ┌──────────┐    │
  │     │  sac_s   │    │    │  sac_ne  │    │
  │     └────┬─────┘    │    └────┬─────┘    │
  │          │     ┌────┴────┐    │          │
  │          └─────┤ brood_  ├────┘          │
  │     ┌──────────┤  core   ├──────────┐    │
  │     │  sac_se  │ (CORE)  │  sac_e   │    │
  │     └──────────┴────┬────┴──────────┘    │
  │                ┌─────┴─────┐              │
  │                │chitin_veil│ ← 正面護膜   │
  │                └───────────┘              │
  ─────────────────────────────────────────
    螢幕底部（玩家區，最近）
```

**垂直對齊情境（L4 穿透雷射小利基）**：`sac_n`（環頂，最遠）↕ `brood_core`（中央）↕ `chitin_veil`（正面，最近）三部位共享同一 x 軸中心線。玩家正對巢母中線發射 L4 可單發同時命中三部位，各自獲得蓄熱（雷射不受護甲阻擋，`chitin_veil` 與 `brood_core` 皆同步蓄熱）。與 CARAPEX 相同定位：這是 L4 的小利基，不是主要展示——L4 真正的舞台在 VOLTWYRM（垂直穿透走廊）。

### `assets/data/kaiju/broodcore.yaml`

> 本檔案示範 00-roster-overview.md §4 提出的 schema 擴充（`Emitters` / `PartMovement` / `FireGate`）。所有新欄位皆可選、預設 null/None，向後相容既有 3 隻 .asset。

```yaml
kaiju_id: "broodcore"
display_name: "巢母 / BROODCORE"
difficulty_tier: 2
theme: "Swarm"                 # KaijuTheme 新值 → 每部位掉 core_swarm
body_movement:
  type: "Breathe+Drift"        # 呼吸縮放 ±5% + 水平漂移，整體層級（非 per-part）
  breathe_period_s: 4.0
  drift_amplitude_pct: 10
  drift_period_s: 12.0

parts:
  - id: "brood_core"
    type: BOSS_CORE
    H_max_override: null        # 200 HU（全域旋鈕）
    B_max_override: null        # 200 BU（全域旋鈕）
    adjacency: ["chitin_veil"]
    drop_table_id: "drop_broodcore_core"
    movement: { type: "None" }
    fire_gate: "RequirePartBroken(chitin_veil)"   # 新 FireGate 值：依另一部位 break_state 切換 emitter
    emitters:
      - id: "core_pulse_pre_veil"
        pattern: "Aimed"
        active_when: "chitin_veil.break_state == ALIVE"
      - id: "core_pulse_post_veil"
        pattern: "Aimed+RingBurst（交替）"
        active_when: "chitin_veil.break_state == BROKEN"

  - id: "sac_n"
    type: NORMAL
    H_max_override: null        # 100 HU
    B_max_override: null        # 100 BU
    adjacency: ["sac_ne", "sac_s"]
    drop_table_id: "drop_broodcore_normal"
    movement: { type: "None" }
    fire_gate: "AliveOnly"
    emitters:
      - id: "spore_ring"
        pattern: "RingBurst"
    spawner:
      unit_id: "spore_mite"     # 沿用道中小怪池，本文件不重新定義其 HP/傷害/移動
      active_when: "break_state == ALIVE"

  - id: "sac_ne"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["sac_n", "sac_e"]
    drop_table_id: "drop_broodcore_normal"
    movement: { type: "None" }
    fire_gate: "AliveOnly"
    emitters: [{ id: "spore_ring", pattern: "RingBurst" }]
    spawner: { unit_id: "spore_mite", active_when: "break_state == ALIVE" }

  - id: "sac_e"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["sac_ne", "sac_se"]
    drop_table_id: "drop_broodcore_normal"
    movement: { type: "None" }
    fire_gate: "AliveOnly"
    emitters: [{ id: "spore_ring", pattern: "RingBurst" }]
    spawner: { unit_id: "spore_mite", active_when: "break_state == ALIVE" }

  - id: "sac_se"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["sac_e", "sac_s"]
    drop_table_id: "drop_broodcore_normal"
    movement: { type: "None" }
    fire_gate: "AliveOnly"
    emitters: [{ id: "spore_ring", pattern: "RingBurst" }]
    spawner: { unit_id: "spore_mite", active_when: "break_state == ALIVE" }

  - id: "sac_s"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["sac_se", "sac_n"]
    drop_table_id: "drop_broodcore_normal"
    movement: { type: "None" }
    fire_gate: "AliveOnly"
    emitters: [{ id: "spore_ring", pattern: "RingBurst" }]
    spawner: { unit_id: "spore_mite", active_when: "break_state == ALIVE" }

  - id: "chitin_veil"
    type: ARMORED
    H_max_override: null        # 150 HU
    B_max_override: null        # 150 BU
    adjacency: ["brood_core"]
    drop_table_id: "drop_broodcore_armored"
    movement: { type: "None" }
    fire_gate: "N/A"             # 純防禦部位，無獨立 emitter（roster §4：Emitters 可選，空＝不發射）
    emitters: []
```

---

## 5. 攻擊模式 (Attack Patterns)

**全域規則**：所有子彈為暖色系，遵守「彈幕永遠讀得懂」視覺鐵則。子彈速度跨四難度階**恆定**；僅彈數／射速／小怪生成頻率依難度縮放（見第 8 節）。

---

### 模式 A：卵囊孢子環 (Spore Ring Burst)

| 屬性 | 值 |
|------|-----|
| **發射源** | `sac_n` / `sac_ne` / `sac_e` / `sac_se` / `sac_s`（各自獨立 emitter，同時運作）|
| **彈形** | RingBurst 6 發，均勻 360° 環狀（每 60° 一發），非瞄準 |
| **子彈速度** | 80 px/s（全難度恆定；全武器庫最慢彈速之一，教學/群體壓力並重）|
| **射頻（D1，每卵囊獨立）** | 1 環/4.0s；5 個卵囊相位錯開（每卵囊延遲 0.8s = 4.0s / 5），使孢子環在時間軸上連續交錯而非同時齊發 |
| **電報** | 對應卵囊發射前 0.4s 鼓脹發光（膜質半透明黃綠→亮黃）|
| **子彈色** | 暖黃 #FFDD33，清晰像素外框 |
| **觸發條件** | 對應卵囊 `break_state == ALIVE` |
| **設計目的** | 教「每個卵囊是獨立發射源」——破壞任一卵囊立即消音該環；同時是 M4/L1 群體軟化/清除的主要教學靶標（相鄰卵囊環在空間上夠近，AoE 可同時覆蓋） |

---

### 模式 B：卵囊孵化流 (Brood Spawn Stream)

| 屬性 | 值 |
|------|-----|
| **發射源** | `sac_n` / `sac_ne` / `sac_e` / `sac_se` / `sac_s`（各自獨立生成計時器，同時運作）|
| **生成型** | 非子彈——於卵囊世界座標生成 1 隻 `spore_mite`（沿用道中小怪池既有 HP/傷害/移動，本文件不重新定義）|
| **生成速度** | N/A（非彈幕移動速度；`spore_mite` 移動行為見小怪池定義）|
| **生成頻率（D1，每卵囊獨立）** | 1 隻/7.0s；同樣相位錯開（每卵囊延遲 1.4s）|
| **同時存在上限** | 4 隻 `spore_mite`（全域，跨所有卵囊共用）；超過上限的生成請求**延後不丟失**，直到場上數量 < 上限才補發 |
| **電報** | 對應卵囊發射前 0.5s 出現膜質破裂微光（與模式 A 電報視覺區隔——偏白閃而非鼓脹）|
| **子彈色（小怪本體）** | 沿用 `spore_mite` 既有配色（道中小怪池）|
| **觸發條件** | 對應卵囊 `break_state == ALIVE`；卵囊 BROKEN 後立即停止（已生成的小怪不受影響，維持存活直到自然被擊殺或飛出畫面）|
| **設計目的** | 這是本頭目的核心獎勵迴圈：小怪流隨存活卵囊數量遞減而肉眼可見地變少——「破壞卵囊＝戰場立即變輕鬆」的直接因果 |

---

### 模式 C：核心防禦脈衝 (Core Defense Pulse)

| 屬性 | 值 |
|------|-----|
| **發射源** | `brood_core`（受 `chitin_veil` 的 `break_state` 閘控，切換 emitter）|
| **彈形（`chitin_veil` ALIVE，護膜完整期）** | Aimed 1（單發瞄準玩家）|
| **彈形（`chitin_veil` BROKEN，核心露出後）** | Aimed 1 與 RingBurst 6（稀疏）交替混合——每輪循環先發 1 枚瞄準彈，再於循環中段發 1 次稀疏環 |
| **子彈速度** | 90 px/s（Aimed 部分）／80 px/s（RingBurst 部分，與模式 A 一致的孢子速度語言）|
| **射頻（D1，護膜完整期）** | 1 次/4.5s |
| **射頻（D1，核心露出後，循環長度）** | 3.5s／循環（t=0 發 Aimed，t≈1.75s 發 RingBurst）|
| **子彈色** | 深紅 #CC3300（Aimed）／暖橙 #FF8800（RingBurst，區別於卵囊環的暖黃，標示「核心已暴露」的新威脅層級）|
| **電報** | 核心脈動本身即電報，Aimed 0.6s／RingBurst 0.5s（脈動峰值後發射）|
| **觸發條件** | 始終啟用；彈形依 `chitin_veil.break_state` 切換（見上方 `fire_gate: RequirePartBroken`）|
| **設計目的** | 護膜完整期：核心存在感低但不缺席；護膜破除後核心從「被保護的目標」變為「主動威脅」，直觀呈現「拆開護膜改變了戰鬥」|

### 模式觸發條件彙總

| 模式 | 啟動 | 停止 |
|------|------|------|
| A 卵囊孢子環（每卵囊獨立）| 對應卵囊 `ALIVE` | 對應卵囊 `BROKEN`（永久消音）|
| B 卵囊孵化流（每卵囊獨立）| 對應卵囊 `ALIVE` | 對應卵囊 `BROKEN`（永久停止孵化；已生成小怪不受影響）|
| C 核心防禦脈衝 | 始終 | `brood_core` `BROKEN`（戰鬥結束）；彈形依 `chitin_veil` 狀態切換，不停止 |

---

## 6. 階段 (Phases)

階段由**部位破壞狀態**驅動，且採用**兩條獨立旗標並行**的模型（而非嚴格線性 P1→P2→P3），落實「階段由破壞狀態驅動，不用 HP 門檻」原則，也允許玩家以不同順序破壞部位仍能得到合理的戰鬥節奏。

### 旗標一：卵囊存活數（驅動模式 A 密度補償）

### 旗標二：`chitin_veil.break_state`（驅動模式 C 彈形切換）

兩旗標各自獨立生效、可同時疊加。以下以「典型推進順序」描述三個敘事階段，但實際觸發條件僅取決於上述兩旗標的當下值：

### Phase 1：「群巢形態」*(5 個卵囊皆 ALIVE)*

- **觸發**：戰鬥開始
- **攻擊組合**：模式 A（5 環錯開放射）+ 模式 B（5 卵囊錯開孵化）+ 模式 C（`chitin_veil` ALIVE → Aimed 1，1/4.5s）
- **設計意圖**：玩家理解「這是一群獨立目標，不是一個血條」；第一次用 AoE 武器（M4/L1）同時軟化 2 個相鄰卵囊，是本頭目的第一個 Aha moment，應在遊玩 2–5 分鐘內發生
- **核心爽點時刻**：M4 落點命中 2 個已軟化相鄰卵囊，兩者同時進入 BROKEN——「雙破」的視覺與音效爆發

### Phase 2：「卵囊凋零形態」*(存活卵囊 ≤ 2，即 ≥3 個 BROKEN)*

- **觸發**：`sac_*` 中任意 3 個以上進入 `BROKEN`（不論哪 3 個，環狀拓撲保證任意組合都合理）
- **攻擊組合**：剩餘卵囊的模式 A 射頻 × `broodcore_p2_sac_ring_speed_mult`（預設 1.25，範圍 1.15–1.40）補償剩餘威脅密度；模式 B 頻率不變（避免小怪數量失控）；模式 C 依 `chitin_veil` 狀態獨立運作
- **設計意圖**：剩餘卵囊「加速放射」暗示玩家「你正在贏，但殘存目標變得更兇」——這是壓力遞減與局部反撲並存的節奏設計，避免後期彈幕過度稀疏導致無聊
- **教學關鍵點**：小怪流已明顯稀疏（存活孵化源只剩 2 個或更少），玩家能直接感受「破壞減少總體壓力」的核心因果

### Phase 3：「核心暴露形態」*(`chitin_veil` BROKEN)*

- **觸發**：`chitin_veil` 進入 `BROKEN`（可透過軟化貫穿標準路徑或 L3 快速剝甲兩條路徑達成，見 kaiju-part-system.md C.4）
- **攻擊組合**：模式 C 切換為「Aimed 1 + RingBurst 6 交替混合」（3.5s 循環）；若同時符合旗標一條件，模式 A 仍套用 P2 加速
- **設計意圖**：核心從「被護膜保護的終點目標」變成「主動放出稀疏放射的威脅」，是戰鬥後段的決斷點——玩家可以選擇集火核心結束戰鬥，或先清完剩餘卵囊拿滿素材
- **可選部位張力**：若玩家選擇先破護膜再處理卵囊環（非典型順序），Phase 3 攻擊組合會與仍有 4–5 個卵囊存活的模式 A/B 疊加，戰場密度明顯更高——這是刻意保留的「非線性破壞順序有不同代價」設計空間，呼應「頭目是靈魂」支柱

---

## 7. 剋制與偏好 Loadout (Weapon Affinity)

### 武器表現速覽

| 武器 | BROODCORE 表現 | 原因 |
|------|------------|------|
| **M4 叢集炸彈** | ★★★ 主剋制 | 落點 AoE（半徑 = 螢幕高度 15%）天然覆蓋卵囊環上相鄰 2 個目標；配合 L1/其他武器預先軟化後，一次投擲能同時推進甚至同時擊破 2 個卵囊的「雙破」時刻，是本頭目的教學核心 |
| **L1 散波雷射** | ★★★ 教學次要 | 三束扇形可同時對準卵囊環上相鄰 3 個目標，各以 D₀/3 蓄熱（= 8.3 HU/s，依 weapon-system.md「僅中央命中」基準值），實現「一次蓄熱多個部位」——與 M4 是天生搭檔 |
| **L3 波動砲** | ★★★ 必要特殊工具 | `chitin_veil` 唯一的快速剝甲路徑；不使用 L3 則只能靠標準軟化貫穿路徑（較慢但仍可行）破除護膜 |
| **L2 集束雷射** | ★★ 精準但不利用群體特性 | 單束最快蓄熱單一卵囊或護膜，但完全發揮不到「同時覆蓋多目標」的頭目設計語言；仍是效率最高的單體軟化手段 |
| **M2 蜂群飛彈** | ★★ 廣域鋪墊型 | 8 枚扇形覆蓋畫面寬 70%，可同時對多個卵囊做初步軟化貢獻，但單枚輸出僅 D₀/8，全效率遠低於 M3 單體引爆或 M4 集中投擲 |
| **M1 追蹤飛彈** | ★★ 安全穩定 | 卵囊固定不動，追蹤優勢未充分發揮，但提供持續、低壓的輸出選項，適合新手 |
| **M3 穿甲魚雷** | ★★ 單體爽感 | 「蓄熱→熱衝擊引爆」對任一已軟化卵囊立即擊破，單體清理效率全武器庫最高，但一次只清一個——不利用本頭目的群體教學特性，仍是完全可行的策略 |
| **L4 穿透雷射** | ★ 小利基 | `sac_n` ↕ `brood_core` ↕ `chitin_veil` 垂直對齊，L4 單發同時蓄熱三部位；利基有限，主展示留給 VOLTWYRM |

### 展示 Loadout：L1 × M4「廣域軟化-集束收割」

本 Loadout 是 BROODCORE 的**設計展示組合**，最直觀地展現「AoE 一次處理多個相鄰卵囊」的頭目核心教學。

**戰鬥序列（L1 × M4，卵囊環 `sac_ne` / `sac_e` / `sac_se`，D2）**：

```
T = 0s      ：玩家將 L1 三束扇形對準 sac_ne / sac_e / sac_se（環上相鄰三卵囊）
              每束各自命中一個目標，H_rate ≈ 8.3 HU/s（= D₀/3，依 weapon-system.md
              「僅中央命中」單束基準值套用於三個不同目標各自命中一束的情境）

T ≈ 19–27s  ：T_soften = θ_S / (H_rate − H_decay_rate) = 100 / (8.3 − 3) ≈ 18.9s（理論）
              三卵囊實戰約 19–27s 內陸續進入 SOFTENED（橙紅脈動同時亮起）

T ≈ 27s+    ：換至 M4，對 sac_ne + sac_e（相鄰，落在 M4 AoE 半徑 15% 螢幕高度內）
              持續投擲：N=2 → break_delta_base = (D₀/2) × 10 = 5 BU/目標/發（原始值）
              M_state_mult = 1.0（SOFTENED）→ B_fill = 5 BU/目標/發
              一個 4 發彈匣（mag_size=4）可對兩目標各推進 20 BU；重複投擲數個彈匣
              （彈匣間 3.5s 換彈）持續累積，直到雙方 B_current 同時越過 100 BU 閾值 →
              「雙破」瞬間：sac_ne 與 sac_e 同幀觸發 BROKEN，素材同時噴出
```

**TTB 誠實說明**：M4 單發對 2 目標的原始填充量（5 BU/發）本身並不高於單體武器（M3 軟化引爆單擊 60 BU），這是刻意的「等功率」設計——M4 用**同時性**換取**單擊爆發力**（呼應 weapon-system.md M2 的相同取捨）。真正的教學價值不是「M4 最快」，而是：①一次瞄準行動能同時對 2 個目標產生進度，減少彈幕壓力下反覆換目標瞄準的風險；②當兩個相鄰卵囊都被磨到接近閾值時（不論靠 M4 反覆投擲或搭配其他武器混傷），最後一發 M4 能製造「同時擊破 2 個」的爽快時刻。全程清理 3 個卵囊（`sac_ne`/`sac_e`/`sac_se`）的總耗時落在約 55–75s，與逐一使用 L2×M3 單體清理 3 個卵囊（每個 15–25s×3 ≈ 45–75s）落在相近區間——這正是「等功率鐵則」保證的結果：策略不同，總時間相近，選擇權在玩家。

### 公平性保證：任意 Loadout 均可通關

BROODCORE 在任何難度階對**所有合法 Loadout** 均可通關，不存在強制解：

| Loadout 範例 | 策略路線 | 5 卵囊 + 護膜 + 核心全破預估總時長 |
|-------------|---------|-----------------------------------|
| L1 × M4 | 廣域軟化 + 集束雙破（展示路線）| ~4–6 分鐘（含閃避 downtime）|
| L2 × M3 | 逐一精準蓄熱引爆 | ~4–6 分鐘 |
| L3 × M2 | L3 快速剝護膜；M2 廣域初步軟化卵囊環，M1/M3 補刀 | ~4.5–6.5 分鐘 |
| 任意 × M1 | 穩定持續輸出，追蹤減少走位壓力 | ~5–7 分鐘（仍在可接受範圍）|
| 無 L3 的 Loadout | 護膜靠軟化貫穿標準路徑破除（較慢但保證可行）| 護膜 TTB 較長（30–45s 上緣），其餘不受影響 |

**無 M4/L1 的 Loadout 仍可通關**：玩家可選擇對 5 個卵囊逐一使用任何雷射+飛彈組合破除（每個 15–25s），總耗時較長但完全在合理範圍內——這正是「optional parts 是張力，不是強制」與「頭目是靈魂」支柱的直接體現：核心破壞才是勝利條件，卵囊環是額外素材與效率教學，不是通關門檻。

---

## 8. 難度縮放 (Difficulty Scaling)

**縮放原則**：僅調整子彈密度（數量／射速）與小怪生成頻率／上限，不改變子彈速度、部位數值、傷害或 `stagger_duration`。完全服從 `kaiju-part-system.md` C.8 難度不縮放規則。

### 模式 A：卵囊孢子環（每卵囊獨立）

| 難度 | 每環彈數 | 每卵囊射頻 | 效果感知 |
|------|---------|-----------|---------|
| D1 Normal | 6 | 1/4.0s | 5 環錯開，節奏寬鬆，適合初次接觸群體壓制 |
| D2 Hard | 6 | 1/3.4s | 建議難度階；密度提升但仍可讀 |
| D3 Extreme | 8 | 1/3.0s | 環變密，AoE 軟化的效率優勢更明顯 |
| D4 Nightmare | 8 | 1/2.5s | 高密度背景壓制，逼玩家更依賴破壞減量策略 |

### 模式 B：卵囊孵化流（每卵囊獨立）

| 難度 | 每卵囊生成間隔 | 同時存在上限（全域）|
|------|--------------|-------------------|
| D1 Normal | 1/7.0s | 4 |
| D2 Hard | 1/6.0s | 5 |
| D3 Extreme | 1/5.0s | 6 |
| D4 Nightmare | 1/4.0s | 8 |

### 模式 C：核心防禦脈衝

| 難度 | 護膜完整期射頻 | 核心露出後循環長度 |
|------|--------------|-------------------|
| D1 | 1/4.5s | 3.5s |
| D2 | 1/4.0s | 3.0s |
| D3 | 1/3.5s | 2.5s |
| D4 | 1/3.0s | 2.0s |

> D3/D4 核心露出後的 RingBurst 彈數可由 6 提升至 8（沿用模式 A 的彈數縮放邏輯），維持視覺一致性。

### Phase 2 卵囊加速（各難度共用同一係數）

Phase 2 觸發時（存活卵囊 ≤ 2），模式 A 射頻 × `broodcore_p2_sac_ring_speed_mult`（預設 1.25，範圍 1.15–1.30）。此係數不依難度變化，保持跨難度一致的「凋零加速」感受。

---

## 9. 素材產出 (Material Drops)

### 掉落表定義

| drop_table_id | 對應部位 | 部位類型 | 核心素材 | shard_common（Standard / Precision / Perfect）|
|--------------|---------|---------|---------|----------------------------------------------|
| `drop_broodcore_normal` | `sac_n/ne/e/se/s`（各自獨立）| NORMAL | `core_swarm` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_broodcore_armored` | `chitin_veil` | ARMORED | `core_swarm` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_broodcore_core` | `brood_core` | BOSS_CORE | `core_swarm` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |

*BROODCORE 屬蟲群系（`Swarm` 主題）巨獸，所有部位均掉落 `core_swarm`（material-economy.md C.1 巨獸主題映射規則）。碎片計算：`floor(shard_base × quality_mult)`；shard_base = 2，Standard = 1.0×，Precision = 1.5×（floor=3），Perfect = 2.0×（= 4）。*

**重要邊界**：`spore_mite` 小怪使用**道中小怪池既有掉落規則**，不消費上表任何 `drop_table_id`，也不計入本頭目的部位破壞獎勵——擊殺孵化出的小怪與破壞卵囊本體是兩套獨立的獎勵系統。

### 一場狩獵預期產出（D1–D4 皆同，Precision 品質為主）

| 通關策略 | core_swarm | shard_common |
|---------|-----------|-------------|
| **全破壞**（7 部位全破：5 卵囊 + 護膜 + 核心）| 7 | 21 + 5 完成獎勵 = **26** |
| **僅破核心**（Speed Run，護膜必須先破除才能命中核心，見第 4 節相鄰圖）| 2（核心 + 護膜，護膜為核心的唯一路徑）| 6 |
| **3 卵囊 + 護膜 + 核心**（跳過 2 卵囊）| 5 | 15 |
| **5 卵囊 + 核心**（跳過護膜，靠標準軟化貫穿路徑清核心前必經護膜——見邊界情況）| 6（核心 TTB 較長，因需持續維持護膜軟化狀態）| 18 |

> 因 `chitin_veil` 與 `brood_core` 相鄰且護膜物理覆蓋核心正面（見第 4 節），**核心無法跳過護膜直接擊破**——這與 CARAPEX（背甲炮與核心是各自獨立可命中目標）不同，是 BROODCORE 刻意的空間層級設計：護膜是核心的**強制前置關卡**，不是選配部位。「Speed Run 僅破核心」策略因此必然連帶破除護膜，最低通關掉落固定為 2 個 `core_swarm`（護膜 + 核心）。

### `essence_kaiju` 產出

`on_hunt_end(is_all_parts_broken = true)` 時觸發（全部 7 個部位均 BROKEN）→ `essence_kaiju` × 1 + `shard_completeness_bonus`（= 5 碎片）。任何難度階均適用，無難度門檻。

### core_swarm 的跨遊戲戰略意義

依 `material-economy.md` C.1，`core_swarm` 是升級**蟲群系相關武器路線**的專屬素材（例如強化 M4 集束彈、M2 蜂群飛彈——兩者皆為「多目標／範圍」定位武器，與 BROODCORE 的教學主題直接呼應）。BROODCORE 是 8 頭目陣容中唯一掉落 `core_swarm` 的巨獸——想升級 M4/M2 的玩家必須主動狩獵 BROODCORE，延續「怪獵式農刷循環」的設計傳統。

---

## 10. 驗收標準 (Acceptance Criteria)

### AC-01 群體教學循環感知（體驗性 — 阻斷）

- [ ] 5 人新手用戶測試：首輪 D2 通關後，受測者能在不提示下描述「破壞卵囊會同時減少彈幕和小怪」的因果關係，達成率 ≥ 70%
- [ ] 受測者能觀察到 M4 或 L1 同時影響 2 個以上卵囊的效果（軟化脈動同時亮起 或 同時擊破）
- [ ] 首次任一卵囊破壞發生於遊戲開始後 3 分鐘內（記錄時間戳）

### AC-02 ARMORED 護膜閘門正確性（功能性 — 阻斷）

- [ ] `chitin_veil` ARMOR_INTACT 且未軟化期間：任何飛彈命中 `B_fill = 0`（偏轉動畫觸發）
- [ ] 任一雷射使 `chitin_veil` 達 SOFTENED（H_current ≥ θ_S）後：飛彈以 M_state_mult = 1.0 正常填充 BU（軟化貫穿標準路徑，見 kaiju-part-system.md C.4）
- [ ] L3 蓄力震波命中 → `armor_state = ARMOR_STRIPPED`，`stagger_timer = 2.0s`，飛彈以 × 1.5 填充
- [ ] `chitin_veil` BROKEN 後：`brood_core` 的 emitter 立即由 `core_pulse_pre_veil`（Aimed 1）切換為 `core_pulse_post_veil`（Aimed+RingBurst 交替），切換發生於同幀
- [ ] 自動化測試：`tests/unit/part-system/armored_part_gate_test`（新增 broodcore 案例覆蓋 `chitin_veil` 全流程）

### AC-03 BOSS_CORE 勝利條件與核心暴露正確性（功能性 — 阻斷）

- [ ] `brood_core` B_current ≥ 200 BU → BROKEN → `on_boss_core_break` 發出 → 勝利結算啟動
- [ ] 即使 5 個卵囊皆 ALIVE，只要 `chitin_veil` 與 `brood_core` 依序破壞，核心破壞仍觸發勝利
- [ ] 勝利結算前 `on_part_break`（BOSS_CORE）必先於 `on_boss_core_break` 發出（事件順序保證）
- [ ] `chitin_veil` 與 `brood_core` 的相鄰／覆蓋關係在場景佈局中確認（`chitin_veil` 碰撞體位於 `brood_core` 正面，見第 4 節）

### AC-04 卵囊孵化與小怪池整合正確性（功能性 — 阻斷）

- [ ] 每個卵囊 `sac_*` 的孵化計時器獨立運作，`break_state == BROKEN` 後立即停止孵化（新請求不再產生），已生成小怪不受影響
- [ ] 同時存在 `spore_mite` 數量超過難度上限時，新生成請求延後而非丟棄，待場上數量低於上限後補發
- [ ] `spore_mite` 使用道中小怪池既有 HP/傷害/移動/掉落規則，不消費 `drop_broodcore_*` 掉落表
- [ ] 自動化測試：`tests/unit/kaiju/broodcore_spawn_gate_test`（覆蓋 5 卵囊獨立計時器 + 上限佇列邏輯）

### AC-05 素材掉落正確性（功能性 — 阻斷）

- [ ] `drop_broodcore_normal` × 5（5 卵囊）：各掉 `core_swarm` + `shard_common`（依品質乘數）
- [ ] `drop_broodcore_armored`、`drop_broodcore_core`：各掉 `core_swarm` + `shard_common`
- [ ] Perfect 品質（SOFTENED_STAGGERED break）：核心數量 = 2（`core_perfect_double_drop = TRUE`）
- [ ] 全破壞結算：`essence_kaiju` × 1 + `shard_completeness_bonus`（= 5）
- [ ] 自動化測試：`tests/unit/economy/material_yield_quality_test`（新增 broodcore 3 drop_table × 3 品質等級案例）

### AC-06 彈幕可讀性（體驗性 — UX 阻斷）

- [ ] 5 人用戶測試：D4 最高密度截圖中，受測者辨識「敵彈 vs 安全間隙」準確率 ≥ 80%
- [ ] SOFTENED 卵囊橙紅脈動在 D4 最高彈幕+小怪密度下仍可辨識
- [ ] `spore_mite` 小怪與模式 A/C 子彈在視覺上可明確區分（小怪為實體判定，子彈為純彈幕判定）
- [ ] ARMOR_STRIPPED 護膜弱點框在 D4 密度下清晰可見，倒計時像素條不被彈幕完全遮蓋

### AC-07 L4 垂直對齊利基（功能性）

- [ ] `sac_n` / `brood_core` / `chitin_veil` 三部位在場景佈局中確認共享同一 x 軸中心線（±5%）
- [ ] L4 穿透雷射單發確認可同時命中三部位，各自接收蓄熱事件
- [ ] 由關卡設計師在 Boss 佈局評審時確認，記錄於 Boss 場景設定文件

### AC-08 階段轉換與難度縮放正確性（功能性）

- [ ] 存活卵囊 ≤ 2（≥3 個 BROKEN）→ 模式 A 剩餘卵囊射頻 × `broodcore_p2_sac_ring_speed_mult`（1.25），此係數存於外部旋鈕，不硬編碼
- [ ] `chitin_veil` BROKEN → 模式 C 彈形切換，且與旗標一（卵囊存活數）獨立疊加（可同時生效）
- [ ] 子彈速度（模式 A：80 px/s；模式 C：90/80 px/s）在 D1–D4 下恆定；靜態審核各難度 `spawn_config` 參數
- [ ] 部位 H_max / B_max / `stagger_duration` 在難度切換後讀取值不變（`difficulty_invariance_test` 覆蓋）

### AC-09 L1 × M4 展示 Loadout 群體清理驗算（功能性）

- [ ] L1 × M4，`sac_ne`/`sac_e`/`sac_se` 三卵囊，D2：三卵囊同時軟化時間實測值 ∈ \[19s, 30s\]（依第 7 節公式）
- [ ] M4 對已軟化的相鄰 2 卵囊（N=2）單發 BU 填充值 = 5 BU/目標（`(D₀/2) × 10 × M_state_mult`，M_state_mult=1.0 時）
- [ ] 全 3 卵囊清理總耗時實測值 ∈ \[45s, 90s\]，與 L2×M3 逐一清理 3 卵囊的預估區間（45–75s）重疊，驗證等功率鐵則
- [ ] 包含在 64 組 loadout TTB 矩陣自動化測試中（`tests/unit/weapon/weapon_loadout_matrix_test`，新增 broodcore 案例）

---

*文件版本：1.0.0*
*作者：Game Designer Agent*
*最後更新：2026-07-08*
*資料定義：`assets/data/kaiju/broodcore.yaml`（inline 見第 4 節，含新 schema 欄位 `movement` / `fire_gate` / `emitters` / `spawner` 示範）*
*依賴掉落表 ID：`drop_broodcore_core` / `drop_broodcore_normal` / `drop_broodcore_armored`（由 material-economy 實作）*
*新 FireGate 值需求：`RequirePartBroken(part_id)`（供 `brood_core` 依 `chitin_veil` 狀態切換 emitter，見 00-roster-overview.md §4 schema 擴充清單）*
