# 素材經濟與永久升級 (Material Economy & Permanent Upgrade) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/material-economy.md*
*最後更新: 2026-07-01*
*狀態: Draft*
*相依概念文件: design/gdd/game-concept.md | design/gdd/weapon-system.md | design/gdd/kaiju-part-system.md（待撰寫）*

---

## A. 概覽 (Overview)

素材經濟系統是殲獸戰機「破壞即技術表現也是素材來源」設計哲學的落地機制。玩家透過在戰鬥中破壞巨獸的可破壞部位（Breakable Parts）蒐集素材；素材的種類與數量由被破壞的**部位類型**（Part Type）與**破壞品質**（Break Quality，即部位破壞瞬間的狀態）共同決定，直接把操作技巧轉化為養成資源，落實〔破壞即獎勵〕支柱。蒐集到的素材投入各武器的**永久升級**（Permanent Upgrade）系統，將每把武器從 Tier 0 逐步強化至 Tier 3；不同武器的 Tier 1→2 與 Tier 2→3 需要不同種類的核心素材（Part Cores），逼使玩家主動狩獵不同巨獸、破壞不同類型部位，自然落實「橫向選擇」支柱——你想升哪把武器，就決定了你去打哪隻怪獸。永久升級進度**跨局永不丟失**，失敗一局不扣回任何素材或 Tier 進度，強調「技術學習，而非失敗懲罰」。本系統刻意保持小規模（3 層 5 類素材、1 條單一循環），回應概念文件明確的設計警告：「養成經濟曲線容易膨脹，需刻意保持簡單」。

---

## B. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：能力感（Competence）＋自主性（Autonomy）＋發現（Discovery）

**「我的精準擊破，換來我的武器成長」** — 玩家清楚感知：把部位打軟（SOFTENED）再一槍引爆，比亂打能多拿素材；這讓農刷（Farming）本身就是技術表現，而不是無腦重複。每次進入戰場前，玩家看著升級介面思考的不是「哪把武器最強」，而是「我今天想解鎖哪把武器的 Tier-3 玩法，我需要去打哪隻巨獸最有效率」——這是主動的橫向選擇，而非被動的強制最優解。

當 Tier-3 機制在實戰中觸發（L2 集束破點漣漪、M3 穿甲爆破鏈……），玩家感受到的不是「我的數字變大了」，而是「我的武器現在用起來不一樣了，我找到了一種新打法」。這是深化武器身份（Identity Deepening）帶來的成就感，而非數值膨脹帶來的虛假強大感，直接服務〔橫向選擇〕支柱的核心承諾：強化使武器更有個性，而非使其凌駕其他選項。

---

## C. 詳細規則 (Detailed Rules)

### C.1 素材分類學 (Material Taxonomy)

本系統採用**三層五類**架構，刻意保持小規模以避免概念膨脹。

#### 層級一：通用資源 (Universal Resource)

| 素材 ID | 名稱 | 英文 | 掉落來源 | 用途 |
|---------|------|------|----------|------|
| `shard_common` | 通用碎片 | Common Shard | 任何部位破壞均掉落 | 所有武器 Tier 0→1、1→2、2→3 均需消耗 |

#### 層級二：部位核心 (Part Cores)

核心素材與被破壞的**部位類型**直接綁定，實現〔破壞即獎勵〕支柱中「掉落綁定被破壞的部位」的設計判準。

| 素材 ID | 名稱 | 英文 | 掉落來源（部位類型） | 綁定武器（需此核心才能升 Tier 1→2 及 2→3） |
|---------|------|------|--------------------|--------------------------------------------|
| `core_carapace` | 甲殼核心 | Carapace Core | 強化部位（Armored Part） | L1 散波雷射、M2 蜂群飛彈、M4 叢集炸彈 |
| `core_limb` | 四肢核心 | Limb Core | 普通部位（Normal Part） | L2 集束雷射、L4 穿透雷射、M1 追蹤飛彈 |
| `core_energy` | 能量核心 | Energy Core | 核心部位（Boss Core Part） | L3 波動砲、M3 穿甲魚雷 |

**核心-武器綁定邏輯（身份對齊）**：

| 核心類型 | 綁定武器 | 邏輯依據 |
|---------|---------|---------|
| 甲殼核心（強化部位） | L1、M2、M4 | 散射廣覆蓋、蜂群飽和、叢集炸彈 — 三種武器均以廣域攻勢施壓裝甲，與甲殼核心的來源吻合 |
| 四肢核心（普通部位） | L2、L4、M1 | 集束精準、穿透縱列、追蹤鎖定 — 精準或機動導向武器，與四肢/活動部位的機動特性互映 |
| 能量核心（核心部位） | L3、M3 | 波動震盪、穿甲引爆 — 高爆發/狀態依賴武器，與巨獸核心部位的能量屬性吻合 |

#### 層級三：精魄 (Essence)

| 素材 ID | 名稱 | 英文 | 掉落條件 | 用途 |
|---------|------|------|---------|------|
| `essence_kaiju` | 巨獸精魄 | Kaiju Essence | 一場戰鬥中**所有可破壞部位全數破壞**後，結算時給予 | 所有武器 Tier 2→3 各需 1 個；代表一場狩獵的「完美清除」 |

> **設計意圖**：精魄是「完成度鎖」而非「難度鎖」——任何難度階的玩家只要能破完所有部位就能獲得。契合〔難度是門，不是牆〕支柱：精魄不因難度階不同而有差異，但要求技術深度（全部位破壞）。

### C.2 素材掉落規則 (Drop Rules)

掉落由上游系統 `kaiju-part-system.md` 的 `on_part_break(part_id, part_type, break_state)` 事件觸發。

#### 部位破壞品質 (Break Quality)

`on_part_break` 觸發瞬間，部位所處的狀態決定本次破壞的品質等級，將雙軌蓄熱-破甲技術表現直接映射為素材獎勵。

| 品質等級 | 觸發條件（部位狀態） | 說明 |
|---------|-------------------|------|
| **標準 (Standard)** | NORMAL（未軟化）狀態下破壞 | 跳過蓄熱的低效路徑；仍有基礎素材，不產生零掉落懲罰 |
| **精準 (Precision)** | SOFTENED（已軟化）狀態下破壞 | 正確執行蓄熱→破甲雙軌；碎片加乘，核心正常掉落 |
| **完美 (Perfect)** | SOFTENED + STAGGERED（軟化且震盪硬直）狀態下破壞 | L3 波動砲震波配合軟化的最高技術表現；碎片最高加乘，核心雙倍 |

#### 每次部位破壞掉落表

| 輸出 | Standard | Precision | Perfect | 備注 |
|------|----------|-----------|---------|------|
| `shard_common` | `shard_base × 1.0` | `shard_base × shard_precision_mult` | `shard_base × shard_perfect_mult` | 四捨五入取整（`floor`） |
| 對應核心（見 C.1） | 1 個 | 1 個 | 2 個（若 `core_perfect_double_drop = TRUE`） | 部位類型不符則 0；僅在 Perfect 時觸發雙核心 |
| `essence_kaiju` | — | — | — | 不在此處掉落；僅於結算階段由全破壞條件觸發 |

#### 結算獎勵 (End-of-Hunt Bonus)

| 條件 | 額外獎勵 |
|------|---------|
| 本場破壞了所有可破壞部位（全破壞） | `shard_completeness_bonus` 通用碎片 + `essence_per_full_clear` 個巨獸精魄 |
| 未達全破壞 | 無額外獎勵（只保留部位中途掉落的碎片與核心） |

**預期每場狩獵掉落率（Precision 品質為主，標準四部位巨獸）**：

| 輸出 | 頻率 / 條件權重 | 說明 |
|------|----------------|------|
| `shard_common`（每破壞） | 2–4 個 / 次（依品質 1.0–2.0× 倍率） | 平均 Precision：3 個/次；4 部位/場 ≈ 12 碎片/場基線 |
| `shard_completeness_bonus` | +5 個 / 全破壞場 | 全破壞率 55% 估算 ≈ 2.75 額外碎片/場 |
| `core_carapace` | 1 個 / 強化部位破壞 | 裝甲型巨獸：2 個/場 |
| `core_limb` | 1 個 / 普通部位破壞 | 敏捷型巨獸：3 個/場 |
| `core_energy` | 1 個 / 核心部位破壞 | 能量型�iju：2 個/場 |
| `essence_kaiju` | 1 個 / 全破壞結算 | 55% 全破壞率估算 ≈ 0.55 精魄/場 |

**預期獲取次數（每層材稀有度）**：

| 素材 | 每場平均 | 首次取得期望場數 | 說明 |
|------|---------|----------------|------|
| 通用碎片 | ~14.75 / 場（含完成獎勵） | 1 場 | 立即可感知，用於前期 Tier 0→1 |
| 核心（各類型，對應巨獸） | 1–3 / 場 | 1 場 | 取決於狩獵目標選擇 |
| 巨獸精魄 | ~0.55 / 場 | 2 場 | 需全破壞；早期玩家約 3–4 場才達成首次全破壞 |

**Floor/Ceiling（防止長時間空手）**：
- Floor：任何部位破壞至少給 `shard_base × 1.0`（最低 2 碎片），無零掉落情況。
- Ceiling：精魄不累積超過需求（Tier 2→3 各需 1 個共 8 個；沒有通貨膨脹風險因為消耗固定）。
- 無「壞運氣保護（Pity Timer）」設計——精魄掉落條件為確定性的全破壞行動，而非機率；「連續未拿到」意味「連續未全破壞」，解法是技術提升，不是概率補償。

### C.3 武器永久升級結構 (Permanent Upgrade Structure)

升級介面在每局結算後開放，玩家以素材對任意武器進行永久升級。升級**單向不可逆**，進度**跨局永不丟失**。

#### 各 Tier 效果定義

| Tier | 名稱 | 英文 | 效果類型 | TTB 改善上限 |
|------|------|------|---------|------------|
| **Tier 0** | 基礎型 | Base | 武器起始狀態 | — |
| **Tier 1** | 小強化 | Minor Enhancement | 運作品質提升（換彈速度、彈匣微增、H/B rate 小增） | ≤ 10% |
| **Tier 2** | 特性深化 | Identity Deepening | 強化武器本身的定義特性，不引入新機制 | ≤ 10%（累計 Tier 0→2 ≤ 10%，不疊加） |
| **Tier 3** | 機制解鎖 | Mechanic Unlock | 解鎖各武器獨特機制（定義於 weapon-system.md C.4/C.5） | ≤ 15%（累計 Tier 0→3 ≤ 15%） |

> **Tier 1 & 2 設計約束**：任何效果均不得讓武器的 TTB 優化超過 Tier 0 基準的 10%（Tier 0→2 合計）。這是防止〔橫向選擇〕支柱被升級路徑破壞的硬規則。Tier 1–2 的設計哲學是「讓武器用起來更順手、更有個性」，而非「讓武器更強」。

#### 各武器 Tier 1 & Tier 2 具體效果

| 武器 | Tier 1 效果（運作品質） | Tier 2 效果（特性深化） |
|------|----------------------|----------------------|
| **L1 散波雷射（Spread Laser）** | 射速 +8%（每束射擊頻率，廣覆蓋節奏加快） | 三束判定框寬度 +5%（散射核心強化，仍是扇形，不變為集束） |
| **L2 集束雷射（Focus Beam）** | 開火 Uptime +6%（等效換彈窗縮短） | H_rate +8%（單部位蓄熱更快；仍要求持續瞄準，不降低操作要求） |
| **L3 波動砲（Wave Cannon）** | 短脈衝（Tap）輸出 +10%（讓連發段更有存在感） | 蓄力時間 −0.15s（1.5s→1.35s；震波更快，不改變雙模式設計語言） |
| **L4 穿透雷射（Pierce Beam）** | 射擊間隔 −0.04s（0.4→0.36s，每部位熱量輸入頻率提升） | 多部位穿透時每部位 H_rate +8%（縱列優勢強化，孤立目標無額外收益） |
| **M1 追蹤飛彈（Homing Missile）** | 換彈時間 −0.3s（3.0→2.7s，循環加快） | 追蹤角度 +8°（±60°→±68°；稍強轉向，仍無 180° 反向追蹤） |
| **M2 蜂群飛彈（Swarm Launcher）** | 換彈時間 −0.5s（5.0→4.5s；最長換彈的相對改善最顯著） | 齊發覆蓋寬度 +5%（70%→73.5% 畫面寬；廣域覆蓋進一步加強） |
| **M3 穿甲魚雷（AP Torpedo）** | 換彈時間 −0.4s（4.0→3.6s；縮短無輸出 Dead Time，降低挫敗感） | 未軟化基礎破甲值 +8%（3×D₀→3.24×D₀；前置懲罰略鬆，仍遠低於軟化路徑） |
| **M4 叢集炸彈（Cluster Bomb）** | 換彈時間 −0.3s（3.5→3.2s） | AoE 半徑 +5%（15%→15.75% 螢幕高度；叢集特性強化，落點機制不變） |

### C.4 升級成本表 (Upgrade Cost Table)

#### 單把武器升級成本

| Tier 轉換 | 通用碎片 | 武器特定核心 | 巨獸精魄 |
|----------|---------|------------|---------|
| **Tier 0→1** | 8 | 0 | 0 |
| **Tier 1→2** | 12 | 5 | 0 |
| **Tier 2→3** | 25 | 8 | 1 |
| **合計（單武器 0→3）** | **45** | **13** | **1** |

#### 全 8 把武器完全升滿總成本

| 素材 | 總量 | 來源 |
|------|------|------|
| 通用碎片 | 360 | 所有部位破壞（通用） |
| 甲殼核心（L1、M2、M4 各 13） | 39 | 強化部位（Armored Parts） |
| 四肢核心（L2、L4、M1 各 13） | 39 | 普通部位（Normal Parts） |
| 能量核心（L3、M3 各 13） | 26 | 核心部位（Boss Core Parts） |
| 巨獸精魄（每把武器各 1） | 8 | 全破壞結算（任何巨獸） |

### C.5 素材循環圖：水龍頭（Faucets）與水槽（Sinks）

```
【水龍頭 Faucets — 唯一素材來源】
  部位破壞事件（on_part_break）
  ├─ 普通部位（Normal Part）    → 通用碎片（品質加乘）+ 四肢核心
  ├─ 強化部位（Armored Part）   → 通用碎片（品質加乘）+ 甲殼核心
  ├─ 核心部位（Boss Core Part） → 通用碎片（品質加乘）+ 能量核心
  └─ 全破壞結算（All Broken）   → 碎片完成獎勵 + 巨獸精魄

【玩家庫存 Inventory — 永久持有，無上限，跨局不丟失】
  通用碎片 | 甲殼核心 | 四肢核心 | 能量核心 | 巨獸精魄

【水槽 Sinks — 唯一消耗端】
  永久武器升級（Permanent Upgrade）
  ├─ Tier 0→1：消耗 通用碎片
  ├─ Tier 1→2：消耗 通用碎片 + 武器特定核心
  └─ Tier 2→3：消耗 通用碎片 + 武器特定核心 + 巨獸精魄
```

**設計原則**：水龍頭單一（部位破壞）、水槽單一（永久升級）——系統拓撲有意保持最小，消除通貨膨脹風險，也讓玩家一眼看懂「打什麼 → 得什麼 → 升什麼」。

### C.6 素材與狩獵多樣性設計 (Hunt Diversity)

不同巨獸的部位組成不同，使特定核心集中於特定巨獸，自然推動玩家根據升級目標選擇狩獵對象。

| 巨獸類型（示例） | 部位組成 | 主要核心產出 | 適合升級的武器 |
|----------------|---------|------------|--------------|
| **裝甲型巨獸** (Armored Kaiju) | 2 強化 + 1 普通 + 1 核心 | 甲殼核心 × 2/場 | L1、M2、M4 |
| **敏捷型巨獸** (Agile Kaiju) | 3 普通 + 1 核心 | 四肢核心 × 3/場 | L2、L4、M1 |
| **能量型巨獸** (Energy Kaiju) | 1 強化 + 1 普通 + 2 核心 | 能量核心 × 2/場 | L3、M3 |

> 此表為示例框架；每隻具名巨獸的精確部位組成由 `kaiju-part-system.md` 定義。**核心設計意圖**：你想升什麼武器，就決定你去打哪隻怪獸，以及你需要破哪種部位。素材循環與 loadout 選擇深度連結，獎勵多元狩獵策略而非單一路線農刷。

---

## D. 公式 (Formulas)

### D.1 素材產量公式 (Material Yield Formula)

每次 `on_part_break(part_id, part_type, break_state)` 事件觸發時執行：

**通用碎片產量**：
```
shard_yield = floor(shard_base × quality_shard_mult[break_state])

quality_shard_mult = {
    NORMAL_break:             1.0,
    SOFTENED_break:           shard_precision_mult,      // 預設 1.5
    SOFTENED_STAGGERED_break: shard_perfect_mult,        // 預設 2.0
}
```

**部位核心產量**：
```
core_type = part_type_to_core_map[part_type]
// 映射：NORMAL_PART    → core_limb
//       ARMORED_PART   → core_carapace
//       BOSS_CORE_PART → core_energy

if core_perfect_double_drop AND break_state == SOFTENED_STAGGERED:
    core_yield = 2
else:
    core_yield = 1   // Standard 與 Precision 品質均給 1 核心（非 0，避免挫折感）
```

> 注意：NORMAL_break 的 Standard 品質仍給 1 核心——最低有 1 核心掉落確保無零懲罰。核心數量的差異（1 vs 2）只在 Perfect 品質時才出現，激勵但不強制技術完美。

**結算精魄獎勵**：
```
// 在 on_hunt_end(is_all_parts_broken) 事件中執行：
if is_all_parts_broken:
    essence_yield = essence_per_full_clear        // 預設 1
    shard_bonus   = shard_completeness_bonus      // 預設 5
```

### D.2 升級成本公式 (Upgrade Cost Formula)

```
// 查表取得成本（見 C.4 成本表）
cost_shard[tier_transition]   = upgrade_cost_shard[tier_transition]
cost_core[tier_transition]    = upgrade_cost_core[tier_transition]
cost_essence[tier_transition] = upgrade_cost_essence[tier_transition]

// 升級可行性判定
can_upgrade(weapon, tier_transition) =
    inventory[shard_common]              >= cost_shard[tier_transition]
    AND inventory[weapon.core_type]      >= cost_core[tier_transition]
    AND inventory[essence_kaiju]         >= cost_essence[tier_transition]
    AND weapon.current_tier              == tier_transition.from_tier
```

### D.3 預期獵數推算 (Expected Hunt Count)

**基準假設**（Precision 品質為主；標準四部位巨獸：2 普通 + 1 強化 + 1 核心）：

**變數定義**：

| 變數 | 預設值 | 說明 |
|------|--------|------|
| `shard_base` | 2 | 每次破壞的基礎碎片 |
| `quality_shard_mult_avg` | 1.5 | Precision 品質均值（玩家穩定蓄熱後的主要品質等級） |
| `completeness_rate` | 0.55 | 平均全破壞率（早期 ~0.40，熟練後 ~0.70；設計基準取中值） |
| `shard_completeness_bonus` | 5 | 全破壞結算碎片獎勵 |
| `parts_per_hunt` | 4 | 每場四部位巨獸 |

**每場平均產量**：
```
avg_shards_per_hunt = parts_per_hunt × shard_base × quality_shard_mult_avg
                    + completeness_rate × shard_completeness_bonus
                    = 4 × 2 × 1.5 + 0.55 × 5
                    = 12 + 2.75 ≈ 14.75 碎片/場

avg_core_per_hunt:
    core_limb      ≈ 2.0  （2 普通部位各給 1 核心，Precision 品質基準）
    core_carapace  ≈ 1.0  （1 強化部位）
    core_energy    ≈ 1.0  （1 核心部位）
avg_essence_per_hunt ≈ 0.55  （全破壞率 × 1 精魄）
```

**升滿單一武器所需獵數估算（以 L1 散波雷射為例，需甲殼核心）**：
```
hunts_shards   = ceil(45 / 14.75) ≈ 4 場
hunts_cores    = ceil(13 / 1.0)   = 13 場（裝甲型巨獸，甲殼核心 1/場）
hunts_essence  = ceil(1 / 0.55)   ≈ 2 場
瓶頸（Bottleneck）= max(4, 13, 2) = 13 場（≈ 5–6 小時）

// 若狩獵裝甲型巨獸（甲殼核心 2/場）：
hunts_cores    = ceil(13 / 2.0)   = 7 場
瓶頸           = 7 場（≈ 3 小時）
```

**升滿所有 8 把武器所需獵數估算**：
```
total_shard_hunts    = ceil(360 / 14.75) ≈ 25 場
total_carapace_hunts = ceil(39 / 2.0)   ≈ 20 場（裝甲型巨獸）
total_limb_hunts     = ceil(39 / 3.0)   ≈ 13 場（敏捷型巨獸）
total_energy_hunts   = ceil(26 / 2.0)   ≈ 13 場（能量型巨獸）
total_essence_hunts  = ceil(8 / 0.55)   ≈ 15 場（跨所有巨獸）

合計（考慮各巨獸需分別狩獵，去除碎片重疊）≈ 35–50 場
```

**估計遊玩時間**：35–50 場 × 25 分鐘/場 ≈ **15–21 小時**（設計目標下限）

> 加上爬難度階的反覆挑戰、完成度成就驅動（破完所有部位）、多元 loadout 實驗，實際黏著時間接近「數十小時」目標。`upgrade_cost_*` 和 `yield_*` 調校旋鈕提供在不改變架構的前提下縮放整體曲線長度的能力，無需重新設計系統。

### D.4 升級後 TTB 上界驗證公式 (Post-Upgrade TTB Ceiling)

```
// 對每把武器的每種 loadout 組合與每種部位類型執行驗證：
TTB_floor(loadout, part_type) = TTB_tier0(loadout, part_type) × (1 - max_ttb_improvement_pct)
// 預設 max_ttb_improvement_pct = 0.15（Tier 0→3 全升合計上限）

assert TTB_tier3(loadout, part_type) >= TTB_floor(loadout, part_type)
```

此公式與 weapon-system.md H.7 的自動化測試（`tier3_identity_depth_test`）共享同一測試套件，跨文件驗證。

### D.5 MVP 子集成本（快速參考）

| MVP 配置（2 武器 × 2 Tier） | 碎片需求 | 核心需求 | 精魄需求 |
|---------------------------|---------|---------|---------|
| Tier 0→1 × 2 把武器 | 16 | 0 | 0 |
| Tier 1→2 × 2 把武器 | 24 | 10 | 0 |
| **MVP 合計（0→2 全升）** | **40** | **10** | **0** |

> MVP 40 碎片 ÷ 14.75/場 ≈ **3 場狩獵**即可體驗完整養成循環。第 1 場：感知素材累積。第 2 場：完成 Tier 0→1。第 3 場：完成 Tier 1→2。循環驗證成本可控。

---

## E. 邊界情況 (Edge Cases)

### E.1 戰鬥失敗或中途退出時的素材保留

**情況**：玩家已破壞部分部位並蒐集素材，但在擊倒巨獸前死亡或強制退出。

**規則**：
- 部位破壞瞬間觸發的素材（碎片、核心）**立即永久保留**，不隨失敗丟失。
- 結算階段的全破壞精魄和完成度碎片獎勵**不給予**（任務未完成）。
- 永久升級已消耗的素材**不退還**（升級是單向永久決定）。
- 設計依據：「永久養成進度永不丟失」原則；失敗只損失本輪的完成度獎勵，不回滾已得素材，降低挫折感並維持「再試一次」動力。

### E.2 Standard 品質破壞（未軟化強破）的素材懲罰

**情況**：玩家在部位 NORMAL 狀態（未軟化）下成功破壞（例如對強化部位硬磨飛彈 35% 效率）。

**規則**：
- 素材品質為 Standard（`shard_base × 1.0`；核心 1 個）。
- 不產生「零素材」懲罰——即使粗破也能取得基礎素材，確保無零掉落挫折。
- 設計意圖：未軟化破壞的代價已由戰鬥機制施加（更長 TTB、更低彈藥效率），素材降級是自然隱性結果，無需額外數值懲罰。

### E.3 部位未觸及（Untouched Parts）

**情況**：玩家全場未攻擊某部位（例如只集中打核心部位），巨獸死亡時其他部位完整。

**規則**：
- 未破壞的部位不掉落任何素材。
- 全破壞精魄不觸發。
- 設計意圖：素材缺口自然引導玩家回頭嘗試不同打法，無需說教式提示。長期跳過某部位意味長期缺少對應核心，是玩家可自行發現並修正的資訊。

### E.4 核心素材過剩（Excess Core Accumulation）

**情況**：玩家因長期狩獵同一巨獸，積累了大量特定核心，但對應武器已全升滿。

**規則**：
- 過剩核心永久存放，不過期、不降解。
- 目前版本無核心互轉機制（核心 → 其他核心），避免增加系統複雜度。
- 調校旋鈕 `allow_core_conversion`（預設 FALSE）預留此功能開關，待原型後視數據評估。

### E.5 精魄稀缺（玩家連續無法全破壞）

**情況**：玩家卡在 Tier 2 無法升 Tier 3，因連續多場未能全破壞所有部位。

**規則**：
- 無補償機制（Pity Timer）——精魄掉落條件為確定性的全破壞行動，不涉及概率。
- 每把武器 Tier 2→3 只需 1 個精魄，這是刻意設定的低門檻：要求玩家至少完成一次全破壞狩獵，但不要求連續達成。
- 若原型測試顯示玩家長期無法全破壞，應首先檢視部位 B_max 數值、TTB 目標是否符合 weapon-system.md D.4 設計範圍，而非降低精魄需求。

### E.6 武器莢艙（Field Drop）取得的武器與升級路徑的互動

**情況**：玩家在場地撿到一把武器，但其升級所需核心尚未農齊。

**規則**：
- 場地武器莢艙取得與永久升級系統完全獨立——你可以使用任何 Tier 的武器而不需先升級它。
- 核心需求的「導購」作用是設計意圖：若玩家想升 Tier 3 前先嘗試武器的 Tier 0 手感，完全合法，也是驗證「值不值得農核心」的好方法。

---

## F. 系統相依 (Dependencies)

### F.1 可破壞部位系統（kaiju-part-system.md）——核心上游依賴

素材經濟系統**完全依賴**此系統提供的事件，且不控制部位血量、狀態機或判定。

所需事件簽名（需與 kaiju-part-system.md 作者協調確認）：
```
on_part_break(part_id: int, part_type: PartType, break_state: BreakState)
    // PartType:   NORMAL_PART | ARMORED_PART | BOSS_CORE_PART
    // BreakState: NORMAL | SOFTENED | SOFTENED_STAGGERED

on_hunt_end(is_all_parts_broken: bool)
    // 觸發全破壞精魄結算
```

### F.2 武器系統（weapon-system.md）——核心下游依賴

- 本系統控制並推送各武器的當前 Tier 等級；weapon-system.md 依此讀取對應 Tier 的調校旋鈕數值。
- Tier-3 機制（各武器獨特機制，詳見 weapon-system.md C.4/C.5）在玩家支付 Tier 2→3 成本後由本系統解鎖。
- TTB 上界驗證（D.4）與 weapon-system.md H.7 的測試共享同一自動化測試套件。

### F.3 難度系統（Difficulty System）

- 難度**不直接影響**素材產量公式或升級成本（〔難度是門，不是牆〕支柱）。
- 高難度彈幕密度間接降低玩家蓄熱 Uptime（更多閃避 → 更少精準命中 → 平均品質等級降低），這是自然隱性調節，非刻意懲罰。
- 精魄的全破壞條件適用於所有難度階，不設難度門檻。

### F.4 UI / HUD 系統（UI System）

升級介面（結算後）需顯示：
- 每把武器的當前 Tier（0–3）及升至下一 Tier 所需素材
- 玩家各素材庫存
- 所需核心對應的「建議狩獵巨獸」提示（哪隻巨獸有此部位類型）
- Tier-3 機制預覽（解鎖前以模糊效果顯示，製造好奇心）

戰鬥內 HUD：
- 部位破壞後即時素材飛入動畫（強化「破壞即獎勵」感知）
- 品質等級提示（Precision / Perfect 破壞時額外視覺反饋）

### F.5 存檔系統（Save System）

素材庫存與武器 Tier 等級需永久存檔，跨局不丟失。所需資料結構（供存檔系統參考）：
```
PlayerProgress {
    weapon_tiers:       Map<WeaponID, int>        // 0–3
    material_inventory: Map<MaterialID, int>       // 無上限正整數
}
```

---

## G. 調校旋鈕 (Tuning Knobs)

**所有數值必須存放於 `assets/data/economy/` 目錄下的外部資料檔案，禁止硬編碼。**

### G.1 素材產量旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `shard_base` | 2 | 1–4 | 曲線 | 每次部位破壞的基礎通用碎片數 |
| `shard_precision_mult` | 1.5 | 1.2–2.0 | 曲線 | Precision（SOFTENED）品質的碎片倍率 |
| `shard_perfect_mult` | 2.0 | 1.5–3.0 | 曲線 | Perfect（SOFTENED+STAGGERED）品質的碎片倍率 |
| `shard_completeness_bonus` | 5 | 3–10 | 曲線 | 全破壞結算的額外通用碎片獎勵 |
| `core_perfect_double_drop` | TRUE | — | 閘門 | Perfect 品質時核心掉落量翻倍（FALSE = 恆定 1 核心） |
| `essence_per_full_clear` | 1 | 1–2 | 閘門 | 全破壞結算的精魄數量（不建議超過 2，精魄需求固定為 8 個） |

### G.2 升級成本旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `upgrade_cost_shard_t0t1` | 8 | 4–15 | 曲線 | Tier 0→1 通用碎片需求 |
| `upgrade_cost_shard_t1t2` | 12 | 8–20 | 曲線 | Tier 1→2 通用碎片需求 |
| `upgrade_cost_shard_t2t3` | 25 | 15–40 | 曲線 | Tier 2→3 通用碎片需求 |
| `upgrade_cost_core_t1t2` | 5 | 3–10 | 曲線 | Tier 1→2 武器特定核心需求 |
| `upgrade_cost_core_t2t3` | 8 | 5–15 | 曲線 | Tier 2→3 武器特定核心需求 |
| `upgrade_cost_essence_t2t3` | 1 | 1–2 | 閘門 | Tier 2→3 巨獸精魄需求（不建議超過 2） |

### G.3 進程曲線旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `max_ttb_improvement_pct` | 0.15 | 0.10–0.20 | 閘門 | Tier 0→3 全升的 TTB 最大改善上限（15%）；超過此值觸發 H.3 警報 |
| `allow_core_conversion` | FALSE | — | 閘門 | 是否允許核心間互轉（預設關閉，待原型數據評估） |
| `difficulty_yield_bonus` | 0.0 | 0.0–0.2 | 手感 | 較高難度階的額外碎片倍率加成（預設 0 = 難度不影響素材量，符合〔難度是門〕支柱） |

### G.4 MVP 子集旋鈕

| 旋鈕名稱 | MVP 預設值 | 全版本值 | 說明 |
|----------|----------|---------|------|
| `enabled_weapon_tiers` | {0, 1, 2} | {0, 1, 2, 3} | MVP 不開放 Tier 3（或選 1 把武器示範） |
| `enabled_material_types` | {shard_common, core_limb, core_carapace} | 全 5 類 | MVP 僅啟用 2 武器對應核心，暫不開啟能量核心與精魄 |
| `active_weapon_pool` | {L1, M1} | 全 8 把 | MVP 僅啟用 2 把武器（1 主 1 副）驗證核心循環 |

---

## H. 驗收標準 (Acceptance Criteria)

### H.1 端到端循環（功能性 — 阻斷）

- [ ] 「破壞部位 → 素材 HUD 提示 → 結算介面正確記帳 → 消耗素材升級 → 武器 Tier 更新 → 進入戰場後升級效果生效」全流程無資料丟失。
- [ ] 素材跨局存檔：重啟遊戲後，庫存與武器 Tier 與退出前完全相同。
- [ ] 戰鬥失敗退出後再進遊戲：部位破壞期間取得的素材全數保留，Tier 進度保留，無任何回滾。

### H.2 素材產量公式正確性（功能性 — 阻斷）

- [ ] Standard 品質破壞：`shard_yield = floor(shard_base × 1.0)`；核心 = 1。
- [ ] Precision 品質破壞：`shard_yield = floor(shard_base × shard_precision_mult)`；核心 = 1。
- [ ] Perfect 品質破壞：`shard_yield = floor(shard_base × shard_perfect_mult)`；核心 = 2（若 `core_perfect_double_drop = TRUE`）。
- [ ] 部位類型與核心映射：普通部位 → 四肢核心；強化部位 → 甲殼核心；核心部位 → 能量核心；無混型錯誤。
- [ ] 自動化測試：`tests/unit/economy/material_yield_quality_test.[ext]`，覆蓋 3 品質等級 × 3 部位類型 × 核心映射 = 9 種主要情境。

### H.3 無主導 loadout 的升級效果（功能性 — 阻斷，跨文件）

- [ ] 任何武器升至 Tier 3 後，其 TTB（對普通部位）相對 Tier 0 改善 ≤ 15%（對應 weapon-system.md H.7）。
- [ ] 全 8 武器升至 Tier 3 後，64 組 loadout 的 TTB 矩陣中，任何 loadout 的 TTB 不超過最快 loadout 的 2.0×（對應 weapon-system.md H.2）。
- [ ] 不存在某 loadout 在**所有**部位類型（普通/強化/核心）上均排名前三——此即確認最終養成不破壞橫向選擇。
- [ ] 驗證工具：`tests/unit/weapon/weapon_loadout_matrix_test.[ext]`（weapon-system.md 已定義），**本 GDD 的 Tier 1/2 調校效果須作為測試輸入參數，確認所有武器在升至最高 Tier 後仍維持 TTB 等價約束**。

### H.4 素材類型分流正確性（功能性）

- [ ] 破壞普通部位 → 只掉通用碎片 + 四肢核心，無其他核心混入。
- [ ] 破壞強化部位 → 只掉通用碎片 + 甲殼核心。
- [ ] 破壞核心部位 → 只掉通用碎片 + 能量核心。
- [ ] 全破壞結算 → 精魄數 = `essence_per_full_clear`；碎片追加 `shard_completeness_bonus`。
- [ ] 非全破壞結算 → 無精魄，無完成度碎片加成。

### H.5 進程曲線可玩性（體驗性）

- [ ] 第一把武器 Tier 0→1：在 **2 場**標準狩獵後可完成（碎片需求 8，平均 ~14.75/場）。
- [ ] 第一個 Tier 2→3 解鎖：在 **7–13 場**狩獵後可達成（含核心與精魄瓶頸）。
- [ ] 升滿全部 8 把武器：原型 playtest 中記錄玩家素材累積速率，確認 15–21 小時目標在 ±30% 誤差內。

### H.6 最終養成不產生主導 loadout（體驗性 + 功能性 — 阻斷，跨文件）

- [ ] 設計師以全 Tier 3 loadout 完成所有巨獸的試玩測試後，能指出每種武器在至少 1 個部位情境或巨獸類型中仍有「最佳或近最佳」表現，確認無武器因升級而被排除於最優選擇集合之外。
- [ ] 此標準為 weapon-system.md H.2 的直接延伸，需在 Full Vision 里程碑前完成驗證。

### H.7 MVP 子集閉環（MVP 里程碑 — 阻斷）

- [ ] MVP 設定（2 武器 × 至多 Tier 2 × 1 巨獸 × 2–3 部位）：H.1 端到端循環完整跑通。
- [ ] 玩家可在第 1 場狩獵內感知素材數量增加，在第 2–3 場後完成第一次武器升級。
- [ ] 升級後效果可被玩家感知（Tier 1 效果通過 UI 明確標注數值變化，即使手感差異細微）。

---

## I. 開放問題與設計風險 (Open Questions & Design Risks)

| 優先級 | 問題 | 阻斷里程碑 | 解答方式 |
|--------|------|------------|---------|
| **高** | `on_part_break` 事件是否能可靠提供 `break_state`（SOFTENED/STAGGERED）欄位？需 kaiju-part-system.md 作者確認事件簽名。 | Prototype | 與 kaiju-part-system GDD 協調確認事件欄位定義；本 GDD 的 D.1 公式以此為前提 |
| **高** | Tier 1 & 2 的 ≤10% TTB 改善是否足以讓玩家**感知**升級進步？若效果太小，養成動力可能受損。 | Vertical Slice | Playtest 前後問卷：玩家能否描述 Tier 1→2 帶來的差異；若「感覺不到」率 > 40%，考慮加強 UX 數值顯示或提升 Tier 2 上限至 12% |
| **中** | `core_perfect_double_drop = TRUE` 是否讓玩家過分追求 L3 震波（STAGGERED）觸發，導致 loadout 向 C 組合（震盪-飽和）集中？ | Vertical Slice | 監測 playtest 的 loadout 選擇分布；若 L3 使用率持續 > 55%，調降 Perfect 核心雙倍為機率性（70% 機率給 2，30% 給 1） |
| **中** | 能量核心依賴破壞核心部位（Boss Core Part）——若此部位 TTB 設計偏長（50–80s 目標上限），玩家實際命中率低，能量核心稀缺性可能超出設計預期，使 L3/M3 長期落後其他武器的升級進度。 | Vertical Slice | 確保核心部位 TTB 在 weapon-system.md D.4 目標範圍（50–80s）內；若 L3/M3 核心積累速率低於其他核心 50% 以上，評估為能量型巨獸增加額外核心部位或降低 Boss Core B_max |
| **低** | 過剩核心問題（E.4）是否在全升完後造成「素材無處可用」的失落感？ | Full Vision | 分析 playtest 數據的素材積累分布；若某核心積累量超過需求量 3× 以上，評估開啟 `allow_core_conversion` 或引入輕量的「素材轉換」小遊戲 |

---

## 附錄：MVP vs 全版本範圍對照

| 功能 | MVP | Vertical Slice | Full Vision |
|------|-----|----------------|-------------|
| 武器數量 | 2（L1 + M1） | 4（2 主 + 2 副） | 8（全部） |
| 升級 Tier 上限 | Tier 2（可選 1 把示範 Tier 3） | Tier 3 | Tier 3 |
| 素材類型 | 碎片 + 2 種核心 | 碎片 + 全 3 種核心 | 全 5 類 |
| 精魄系統 | 關閉 | 開啟（1 巨獸驗證） | 開啟（全 3–5 巨獸） |
| 狩獵多樣性 | 1 巨獸 | 2–3 巨獸（含 1 裝甲型＋1 敏捷型） | 全 3–5 巨獸（含能量型） |
| 端到端循環驗證 | H.7 | H.1–H.5 | H.1–H.6 |

---

*文件版本：1.0.0*
*作者：Economy Designer Agent*
*關聯 GDD：game-concept.md | weapon-system.md | kaiju-part-system.md（待撰寫，上游依賴）*
