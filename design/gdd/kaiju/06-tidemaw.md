# 潮顎 / TIDEMAW
## 殲獸戰機 / KAIJU BREAKER — 第六頭目設計文件

*kaiju_id: tidemaw*
*頭目序號: 06*
*難度階: D4*
*主題 (KaijuTheme): Abyss（深淵系，新增列舉值）*
*文件路徑: design/gdd/kaiju/06-tidemaw.md*
*最後更新: 2026-07-08*
*狀態: Draft*
*相依文件: game-concept.md | kaiju-part-system.md | weapon-system.md | material-economy.md | 00-roster-overview.md §3.3*

---

## 1. 概覽 (Overview)

潮顎（TIDEMAW）是殲獸戰機第六頭目（D4 難度階），定位為「消耗戰 Boss（Attrition Boss）」——牠是一頭橫向寬體深海巨獸，身體前緣排列三張獨立的鰓顎（Gill-Maw），每張顎各自持續吐出緩速密集的瞄準泡幕（Bubble Curtain），逼玩家在畫面中持續編織走位。TIDEMAW 為整個頭目陣容引入一個全新的核心機制：**顎部破甲槽回填（ArmorRegen）**——任何一張顎若在一段時間內沒有收到破壞輸入，其累積的破甲值（BU）會緩緩衰退回填。這使得「集中單一武器猛攻一張顎」的直覺策略出現隱性代價：其餘顎在此期間得不到壓制、甚至已有的進度被慢慢抹平。M2 蜂群飛彈（Swarm Launcher）憑藉一次齊射即可同時餵養全部三張顎的特性，成為唯一能「同時壓住全部回甲」的武器——這正是本戰的教學核心：**持續壓制 vs 逐一擊破**的取捨。中央心核（heart_core，BOSS_CORE）由背甲（dorsal_plate，ARMORED）遮護，戰鬥結束的唯一路徑是先剝開背甲、再擊破心核；三張顎與鰓根（gill_root）皆為可選部位，供玩家自行決定要不要在磨心核之前先清空壓制。

---

## 2. 玩家幻想 (Player Fantasy)

**目標 MDA 美學（優先序）**：

| 美學 | 體驗設計意圖 |
|------|------------|
| **挑戰（Challenge）** | 「我以為打熱一張顎就穩了，結果另外兩張又長回來」——回甲機制製造持續性的戰術壓力，逼玩家做出資源分配決策 |
| **能力感（Competence）** | 發現 M2 蜂群飛彈能同時餵養三張顎、讓回甲徹底失效的瞬間，是本戰最大的 Aha moment |
| **感官愉悅（Sensation）** | 三道暖色泡幕交錯湧來、玩家在窄縫中連續編織的持續張力節奏 |

> 「深海巨獸的顎會自己長回來——除非你讓它連喘息的機會都沒有。這不是打得多快的問題，是打得夠不夠『廣』的問題。」

**「壓制，不是爆發」**：TIDEMAW 教的不是「誰的單發傷害最高」，而是「誰能讓敵人喘不過氣」。這是對整個武器體系裡「持續壓制型武器」（M2）的獨立展示場——在其他頭目裡 M2 常是輔助選項，這裡它是唯一能讓消耗戰穩贏的解法（其他 loadout 仍可通關，只是要付出「先犧牲、後補」的代價，見第 7 節）。

**「護甲是最後一道鎖」**：背甲（dorsal_plate）不再只是「額外素材的選項」，而是唯一通往心核的鑰匙——這是本頭目對「護甲是門，不是牆」支柱的加碼詮釋：這扇門**必須**打開，但打開它的手段（軟化貫穿或 L3 剝甲）完全自由。

---

## 3. 外形與主題 (Silhouette & Theme)

| 維度 | 設計決策 |
|------|---------|
| **生物原型** | 深海鮟鱇/巨口魚混合體：橫向寬扁身軀，前緣張開三張獨立咬肌顎，背部覆蓋厚重甲板 |
| **像素規格** | 畫面佔寬 75–85%（橫向寬體，三顎分散於前緣左/中/右）；縱向佔高 40–55% |
| **色系** | 主題 Abyss（深淵系）：深藍綠 + 墨青灰底色，生物冷光紋理；**判定相關的子彈與威脅標記一律維持暖色系**（見下方色彩鐵則決議） |
| **動態** | 軀幹整體緩慢垂直「呼吸」浮動（振幅 = 畫面高 4%，週期 6s，模擬深海生物的鰓部起伏）；三張顎本身不獨立位移（固定於軀幹前緣的相對座標），純粹作為**命名發射源**吐出泡幕 |

### 色彩視覺鐵則的深淵系決議

深淵系的美術方向天然偏向青綠色冷光（生物螢光），但遊戲的硬性規則要求「敵彈永遠是暖色，玩家彈永遠是冷色」以維持 0.1s 可辨識對比（見 kaiju-part-system.md 與 game-concept.md 視覺鐵則）。TIDEMAW 的解法：**泡幕子彈本體維持暖色（珊瑚橙 `#FF7A45` 核心），僅在描邊/拖尾附加裝飾性青色生物冷光（`#00E5C8`，不參與判定色）**。這保留「潮顎＝深海生物光」的主題辨識度，同時不違反〔暖色＝威脅〕鐵則。SOFTENED 狀態沿用全域旋鈕 `softened_color_hue = #FF6600`（kaiju-part-system.md G.3），與其他頭目一致。

---

## 4. 部位組成 (Part Composition)

### 4.1 部位總表

| 部位 ID | 中文名 | 類型 | H_max | B_max | 相鄰部位 | 掉落表 ID | 弱點可見性 | 回甲 (ArmorRegen) |
|---------|--------|------|-------|-------|---------|----------|-----------|-------------------|
| `maw_1` | 左顎 | NORMAL | 100 HU | 100 BU | gill_root | `drop_tidemaw_normal` | 永遠可見 | ✅ 見 4.4 |
| `maw_2` | 中顎 | NORMAL | 100 HU | 100 BU | gill_root | `drop_tidemaw_normal` | 永遠可見 | ✅ 見 4.4 |
| `maw_3` | 右顎 | NORMAL | 100 HU | 100 BU | gill_root | `drop_tidemaw_normal` | 永遠可見 | ✅ 見 4.4 |
| `gill_root` | 鰓根 | NORMAL | 80 HU（覆寫） | 80 BU（覆寫） | maw_1, maw_2, maw_3, dorsal_plate | `drop_tidemaw_normal` | 永遠可見 | 無（結構部位，不回甲） |
| `dorsal_plate` | 背甲 | ARMORED | 150 HU | 150 BU | gill_root, heart_core | `drop_tidemaw_armored` | 弱點隱藏（同 kaiju-part-system C.4 標準 ARMORED 規則）| 無 |
| `heart_core` | 心核 | BOSS_CORE | 200 HU | 200 BU | dorsal_plate | `drop_tidemaw_core` | **特化覆寫：`dorsal_plate` BROKEN 前判定框關閉、不可命中**（見 4.3） | 無 |

> `PartType` 列舉值全域含 `Normal / Armored / BossCore / MidCore` 四種；TIDEMAW 本戰**未使用** `MidCore`（六個部位僅涵蓋 Normal/Armored/BossCore 三種），`MidCore` 保留給其他頭目（如 NULLSPIRE）使用。

**三顎的「輕裝甲門」說明**：三張顎的類型是 `NORMAL`（非 `ARMORED`），沒有獨立的 `armor_state` 狀態機。「輕裝甲門」指的是 NORMAL 部位本身既有的熱量閘門：未軟化（`heat_state == INTACT`）時飛彈以 `B_unsoftened_mult`（0.35）低效填充，軟化後（`heat_state == SOFTENED`）以 ×1.0 正常填充（kaiju-part-system.md D.3）。這與 `dorsal_plate` 的完整 ARMORED 狀態機（未軟化時 ×0 徹底鎖死，需軟化貫穿或 L3 剝甲）形成強度對比——顎的閘門「輕」，背甲的閘門「重」。

**`gill_root` 的定位**：無獨立彈幕、無回甲機制的純結構部位，作為三顎與背甲之間的相鄰樞紐（供 Tier-3 鏈式效果擴散），也是完整狩獵（Full Clear）玩家的額外素材目標。刻意調降 H_max/B_max（80/80）使其成為整場戰鬥中最快能拿下的「甜點」部位。

### 4.2 空間佈局（ASCII 示意）與相鄰圖

```
                     ┌────────────────┐
                     │  背甲 dorsal   │   ← 頂部覆蓋，遮護心核
                     │    (ARMORED)   │
                     └───────┬────────┘
                             ↕ (垂直對齊 — L4 穿透雷射利基，見第 7 節)
                     ┌───────┴────────┐
                     │  心核 heart_core│   ← 背甲未破時判定框關閉
                     │    (BOSS_CORE) │
                     └───────┬────────┘
                             │
                     ┌───────┴────────┐
                     │  鰓根 gill_root │   ← 相鄰樞紐（連接三顎與背甲）
                     │    (NORMAL)    │
                     └──┬─────┬─────┬─┘
              ┌─────────┘     │     └─────────┐
         ┌────┴────┐    ┌─────┴────┐    ┌─────┴────┐
         │左顎 maw_1│    │中顎 maw_2│    │右顎 maw_3│
         │ (NORMAL) │    │ (NORMAL) │    │ (NORMAL) │
         └─────────┘    └──────────┘    └──────────┘
        （橫向排列於軀幹前緣，覆蓋畫面寬度約 70%）
```

雙向相鄰（有向宣告 → 系統雙向推導，遵循 kaiju-part-system.md C.6）：
- gill_root ↔ maw_1 / maw_2 / maw_3（三顎共享同一樞紐，`adjacency_max_neighbors` 預設 4，gill_root 剛好用滿）
- gill_root ↔ dorsal_plate
- dorsal_plate ↔ heart_core

**垂直對齊 L4 利基**：`dorsal_plate`（頂部）↕ `heart_core`（中層）在場景佈局中垂直對齊，供 L4 穿透雷射單發同時命中兩部位（各自蓄熱），符合 weapon-system.md F.5「每個 Boss 至少需含 ≥2 垂直對齊部位」的關卡設計約束。三顎與 gill_root 橫向排列，不參與此垂直利基。

### 4.3 特化規則：心核判定框覆寫（Heart-Core Hitbox Gate Override）

**設計決議（本頭目特化，覆寫 kaiju-part-system.md C.3 的 BOSS_CORE 預設「永遠可見」規則）**：

`heart_core` 的判定框在 `dorsal_plate.break_state != BROKEN` 期間**完全關閉**——任何武器命中一律無效（視同未命中，不消耗彈藥判定、不觸發任何事件）。`dorsal_plate` 進入 `BROKEN` 的同一幀，`heart_core` 判定框立即開啟（碎甲 VFX + 核心搏動外露），此後心核永久可命中（不會因任何後續狀態改變而重新關閉）。

**覆寫理由**：
1. 呼應「背甲護核」的敘事——背甲不只是額外素材選項，而是唯一通往勝利的鑰匙
2. 確保本戰至少有 1 個 `ARMORED` 部位是**強制**路徑（而非如 CARAPEX/LACERA 般純選擇性），讓 L3 波動砲的軟化貫穿/剝甲能力在此戰有不可迴避的展示場（weapon-system.md F.5 要求）
3. 三顎與 `gill_root` 仍完全維持「可選部位」定位（kaiju-part-system.md C.3 可選擇性原則）——**唯一被此覆寫影響的是 `dorsal_plate`（由選擇性升級為必經）**，設計師與審查者需明確知悉此為刻意例外

**衍生的速攻路線（Speed-Run vs 消耗-清場）**：由於三顎/鰓根對勝利非必要，玩家可從 T=0 完全無視三顎泡幕、直攻 `dorsal_plate` → `heart_core`，理論最短通關時間 ≈ dorsal_plate TTB（30–45s）+ heart_core TTB（50–80s）≈ 80–125s，但代價是**全程承受三張顎的滿密度泡幕**（見第 6 節）。反之，先清三顎（或至少壓制住）可大幅降低後段背甲/心核階段的閃避壓力，但增加前段耗時。兩條路線皆合法，呼應「頭目是靈魂」支柱的核心張力。

### 4.4 新機制：顎部破甲槽回填公式 (Maw Break-Gauge Regen — ArmorRegen)

這是本頭目對 kaiju-part-system.md 雙軌機制的**擴充**，僅適用於定義了 `ArmorRegen` 資料的部位（本戰僅 `maw_1`/`maw_2`/`maw_3`；`gill_root`/`dorsal_plate`/`heart_core` 的 `ArmorRegen = null`，不受影響）。

**核心原則**：ArmorRegen **只作用於破甲槽（B_current），不影響熱量槽（H_current）**——蓄熱軌與破甲軌保持 kaiju-part-system.md A 定義的雙軌獨立性。ArmorRegen 也**不是**部位復活機制：`part_regen_enabled` 全域恆為 `false`（kaiju-part-system.md C.7/G.3）完全不變——BROKEN 仍是不可逆終態，回甲只作用於**尚未 BROKEN**的部位的 BU 數值本身。

**狀態新增欄位**（僅套用於持有 `ArmorRegen` 資料的部位）：

| 欄位 | 型別 | 初始值 | 說明 |
|------|------|--------|------|
| `t_since_break_hit` | float (s) | 0 | 距離上一次「有效破壞輸入」的經過時間 |

**有效破壞輸入（重置 `t_since_break_hit` 為 0）的事件**：
- `on_missile_hit` 且結算後 `B_fill > 0`（即 `M_state_mult > 0`，偏轉命中不算）
- `on_l3_wave_hit`（L3 蓄力震波命中，即使該部位非 ARMORED 亦視為有效輸入）

**注意**：`on_laser_hit`（雷射蓄熱）**不會**重置 `t_since_break_hit`——即使玩家持續用雷射把顎維持在 SOFTENED，只要沒有飛彈/L3 命中填充 BU，破甲槽仍會回填。這確保回甲是「破甲軌專屬」機制，不能被單純蓄熱繞過。

**每幀更新公式**：

```
if break_state == BROKEN or part.ArmorRegen == null:
    return   （BROKEN 部位或無回甲資料的部位跳過）

if 本幀收到有效破壞輸入:
    t_since_break_hit ← 0
else:
    t_since_break_hit ← t_since_break_hit + Δt

if t_since_break_hit >= armor_regen_grace_s:
    B_current ← clamp( B_current − armor_regen_rate × Δt,  0,  B_max )
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `armor_regen_grace_s` | float | \[3.0, 7.0\] s | 無效破壞輸入的寬限期；超過此秒數才開始回填（全域旋鈕，預設 **5.0s**）|
| `armor_regen_rate` | float | \[4, 10\] BU/s | 寬限期後的每秒回填速率（全域旋鈕，預設 **6 BU/s**）|
| `t_since_break_hit` | float | \[0, ∞) s | 距上次有效破壞輸入的經過時間 |
| `B_current` | float | \[0, B_max\] BU | 回填後不得為負，clamp 至 0 |

**難度不縮放**：`armor_regen_grace_s` 與 `armor_regen_rate` 在 D1–D4 完全恆定（比照 kaiju-part-system.md C.8 全域部位數值不縮放規則）；唯一隨難度變化的是三顎泡幕的彈幕密度（見第 8 節）。

**Phase 2 加速回甲**（見第 6 節）：≥2 張顎 BROKEN 後，剩餘那張顎的 `armor_regen_rate` 套用 `tidemaw_phase2_regen_mult`（預設 1.3，範圍 1.1–1.6）→ 6 → 7.8 BU/s，模擬「最後一張顎更頑強地想長回來」。

**運算範例一（單顎，脫離攻擊後回填）**：
```
情境：maw_2 的 B_current = 40 BU，玩家將火力轉向 maw_1，持續 6 秒未命中 maw_2

t=0s→4.999s：t_since_break_hit 從 0 累積到 <5.0s → 未達寬限期，B_current 維持 40 BU
t=5.0s：t_since_break_hit 達到 armor_regen_grace_s（5.0s）→ 開始回填
t=5.0s→6.0s（1 秒回填）：B_current = clamp(40 − 6×1, 0, 100) = 34 BU
```

**運算範例二（為何 M2 蜂群飛彈能同時壓住三顎，而單體武器不能）**：

假設三顎皆已被雷射軟化維持 SOFTENED（M_state_mult = 1.0），比較兩種 loadout 在同一 20 秒視窗內的表現：

```
【loadout A：單體武器持續鎖定 maw_1（例如 M1 追蹤飛彈，軟化後持續輸出 ≈ 10 BU/s）】
- maw_1：10 BU/s × 20s = 200 BU（實際 clamp 於 100 BU，約 10s 即觸發 BROKEN）
- maw_2、maw_3：全程 0 次命中，B_current 維持 0（沒有已累積的 BU 可回填，純粹「原地不動」）
- 若玩家清完 maw_1 後依序轉打 maw_2 → maw_3，三顎全清總耗時 ≈ 10s × 3 ≈ 30s（循序清場）
- 這段期間，尚未攻擊到的顎持續以全密度吐出泡幕——玩家承受的「多顎同時開火」時間窗口最長

【loadout B：M2 蜂群飛彈（8 枚微型飛彈同時扇形發射，均分覆蓋三顎，每顎約 2–3 枚）】
- weapon-system.md 定義：M2 全彈命中單一目標時效持續輸出 ≈ 25 BU/s；本戰三顎横向分佈於彈幕覆蓋範圍內，
  比照 M4 集束炸彈的多目標均分規則（weapon-system.md C.6 M4 條目），25 BU/s 平均分配到 3 個同時命中目標
  → 每顎各得 ≈ 25/3 ≈ 8.3 BU/s，且**三顎同時**受益
- 彈匣週期（8 齊射，換彈 5.0s）與 `armor_regen_grace_s`（5.0s）**刻意校準為相等**：只要玩家持續開火，
  下一輪齊射必定在上一輪的寬限期屆滿前抵達，三顎的 `t_since_break_hit` 永遠不會超過寬限期 → 回填公式永不觸發
- 三顎並行填充：100 BU ÷ 8.3 BU/s ≈ 12s，**三顎幾乎同時 BROKEN**
- 對比 loadout A 的 ≈30s 循序清場，M2 的並行壓制快上近 2.5 倍，且「三顎同時安靜」帶來的壓力驟降遠比
  「一顎接一顎慢慢安靜」更具戲劇性回饋
```

**運算範例三（回甲陷阱：拙劣的人工輪替單體武器）**：

```
情境：玩家嘗試手動在三顎間輪流開火（不使用 M2），試圖「雨露均霑」
- 以 M3 穿甲魚雷為例：彈匣 3 枚，換彈 4s；假設玩家每輪對每張顎各發射 1 枚後才輪到下一張，
  完整輪替一圈的間隔 ≈ 4s（換彈）× 3（顎數）= 12s
- 12s > armor_regen_grace_s（5.0s）→ 每張顎在被再次命中前，有 12 − 5 = 7s 處於回填狀態
  → 每輪流失 = 7 × 6 = 42 BU
- 每次命中填充（SOFTENED，熱衝擊引爆前提不成立時的一般命中）≈ 30 BU（3×D₀×10，未觸發熱衝擊）
- 淨變化 = +30 − 42 = **−12 BU／輪**——B_current 永遠無法累積，陷入原地打轉
- 結論：單體武器若試圖「平均分攤」到三顎且輪替間隔超過寬限期，數學上是負收益陷阱；
  正確的單體武器策略是**放棄平均分攤，全力鎖定一張顎直到 BROKEN 為止**（見 loadout A），
  這樣寬限期對「正在攻擊的目標」永遠不會啟動，只是整體耗時較長、較不具「多顎同時安靜」的爽感
```

### `assets/data/kaiju/tidemaw.yaml`

```yaml
kaiju_id: "tidemaw"
display_name_zh: "潮顎"
display_name_en: "TIDEMAW"
kaiju_tier: 4
role: "attrition_boss"
kaiju_theme: "Abyss"   # KaijuTheme 新增列舉值；每部位掉落 core_abyss

body_movement:
  pattern: "vertical_breathe"
  amplitude_screen_pct: 4        # ±4% 螢幕高度
  speed_cycles_per_min: 10       # 每分鐘完整呼吸浮動 10 次（~6s 週期）

parts:
  - id: "maw_1"
    type: NORMAL
    H_max_override: null          # 全域預設 100 HU
    B_max_override: null          # 全域預設 100 BU
    adjacency: ["gill_root"]
    drop_table_id: "drop_tidemaw_normal"
    emitters:
      - id: "maw_1_curtain"
        pattern: "aimed_wide_fan_bubble"
        fire_gate: "AliveOnly"    # 破壞即消音；軟化不影響射速
        phase_offset_s: 0.0
    armor_regen:
      grace_s: 5.0                # armor_regen_grace_s
      rate_bu_per_s: 6.0           # armor_regen_rate
      phase2_mult: 1.3             # tidemaw_phase2_regen_mult（≥2 顎破後套用於存活顎）

  - id: "maw_2"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["gill_root"]
    drop_table_id: "drop_tidemaw_normal"
    emitters:
      - id: "maw_2_curtain"
        pattern: "aimed_wide_fan_bubble"
        fire_gate: "AliveOnly"
        phase_offset_s: 1.2        # 相位錯開，避免三顎同幀重疊
    armor_regen:
      grace_s: 5.0
      rate_bu_per_s: 6.0
      phase2_mult: 1.3

  - id: "maw_3"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["gill_root"]
    drop_table_id: "drop_tidemaw_normal"
    emitters:
      - id: "maw_3_curtain"
        pattern: "aimed_wide_fan_bubble"
        fire_gate: "AliveOnly"
        phase_offset_s: 2.4
    armor_regen:
      grace_s: 5.0
      rate_bu_per_s: 6.0
      phase2_mult: 1.3

  - id: "gill_root"
    type: NORMAL
    H_max_override: 80            # 覆寫：較小的結構型部位
    B_max_override: 80
    adjacency: ["maw_1", "maw_2", "maw_3", "dorsal_plate"]
    drop_table_id: "drop_tidemaw_normal"
    emitters: []                  # 無獨立發射源（純結構樞紐）
    armor_regen: null             # 不套用回甲機制

  - id: "dorsal_plate"
    type: ARMORED
    H_max_override: null          # 全域預設 150 HU
    B_max_override: null          # 全域預設 150 BU
    adjacency: ["gill_root", "heart_core"]
    drop_table_id: "drop_tidemaw_armored"
    emitters: []                  # 無獨立彈幕（純護核閘門）
    armor_regen: null
    design_note: >
      本頭目特化覆寫：heart_core 判定框在 dorsal_plate BROKEN 前關閉，
      使 dorsal_plate 由「選擇性部位」升級為「勝利必經閘門」（見 §4.3）。

  - id: "heart_core"
    type: BOSS_CORE
    H_max_override: null          # 全域預設 200 HU
    B_max_override: null          # 全域預設 200 BU
    adjacency: ["dorsal_plate"]
    drop_table_id: "drop_tidemaw_core"
    hitbox_gate: "RequireAdjacentBroken:dorsal_plate"  # 特化欄位：見 §4.3
    emitters:
      - id: "heart_core_ring"
        pattern: "radial_ring"
        fire_gate: "RequireAdjacentBroken:dorsal_plate"  # P3 專屬，之前完全靜默
    armor_regen: null
```

---

## 5. 攻擊模式 (Attack Patterns)

**全域規則**：所有敵彈判定色維持暖色系（珊瑚橙 `#FF7A45` 核心），裝飾性青色生物冷光描邊/拖尾不參與判定（見第 3 節色彩決議）。子彈速度跨 D1–D4 恆定；僅彈數與射頻依難度縮放（見第 8 節）。

---

### 模式 A：顎泡幕 (Gill-Maw Bubble Curtain)

| 屬性 | 值 |
|------|-----|
| **發射源** | `maw_1` / `maw_2` / `maw_3`（三者各自獨立觸發，相位錯開，見下方 phase_offset）|
| **彈形** | Aimed 寬扇 7 發（D1），扇角 ±40°（總 80°），瞄準玩家當前位置，彈間距均分形成密集「牆」；子彈外形為圓形氣泡狀（泡幕視覺） |
| **子彈速度** | 75 px/s（恆定，全難度不變；刻意偏慢，逼玩家在泡幕縫隙間「編織」走位而非單純瞬間閃避）|
| **射頻（D1）** | 每顎 1 次/3.5s；三顎相位錯開 `phase_offset_s`（0s / 1.2s / 2.4s）避免同幀重疊、維持可讀性 |
| **telegraph** | 0.5s（顎部張開的橙色蓄力閃光 + 低頻開合音效前置）|
| **子彈色** | 珊瑚橙 `#FF7A45`（判定色）＋ 青色生物冷光描邊/拖尾 `#00E5C8`（裝飾，不參與判定）|
| **觸發條件** | 對應顎 `break_state == ALIVE`（`FireGate: AliveOnly`）；**不受 `heat_state`/回甲進度影響**——軟化或部分破甲皆不減緩射速，只有真正 BROKEN 才會消音 |
| **設計目的** | 建立「唯有徹底破壞才能讓它閉嘴」的壓制感；三顎交錯節奏訓練玩家持續橫向編織移動；為 ArmorRegen 機制提供具體的持續壓力背景 |

---

### 模式 B：心核放射環 (Heart-Core Radial Ring) — Phase 3 專屬

| 屬性 | 值 |
|------|-----|
| **發射源** | `heart_core` |
| **彈形** | Radial 環形彈幕，8 發（D1），360° 均分 |
| **子彈速度** | 100 px/s |
| **射頻（D1）** | 1 次/3.5s |
| **telegraph** | 0.6s（核心脈動至峰值後發射，脈動本身即電報）|
| **子彈色** | 深紅珊瑚 `#CC3300` |
| **觸發條件** | `dorsal_plate.break_state == BROKEN`（`FireGate: RequireAdjacentBroken:dorsal_plate`）；P1/P2 期間 `heart_core` 完全靜默、判定框亦關閉（見 §4.3）|
| **設計目的** | P3「核心裸露」階段的終局壓力來源；環形彈幕逼玩家持續移動而非固定站位輸出，呼應核心永遠可見、判定框放大（`hitbox_size_multiplier_core` = 1.2）的終局目標感 |

### 模式觸發條件彙總

| 模式 | 啟動 | 停止 |
|------|------|------|
| A 顎泡幕（每顎獨立）| 對應顎 `ALIVE` | 對應顎 `BROKEN`（永久消音）|
| B 心核放射環 | `dorsal_plate BROKEN` | `heart_core BROKEN`（戰鬥結束）|

> `dorsal_plate` 與 `gill_root` 本身不具備獨立攻擊模式——`dorsal_plate` 是純護核閘門，`gill_root` 是純結構樞紐（延續 LACERA `tail_carapace` 無獨立彈幕的先例）。

---

## 6. 階段 (Phases)

階段由**部位破壞狀態**驅動，非血量閾值，落實「破壞改變戰鬥」設計哲學（kaiju-part-system.md 全域原則）。

### Phase 1：「三顎齊鳴」 *(maw_1/2/3 皆 ALIVE)*

- **觸發**：戰鬥開始
- **攻擊組合**：模式 A × 3（相位錯開 1.2s，覆蓋畫面寬度 70%）
- **回甲參數**：`armor_regen_grace_s` = 5.0s，`armor_regen_rate` = 6 BU/s（基準值，三顎相同）
- **設計意圖**：建立「持續壓制 vs 逐一擊破」的核心抉擇；玩家開始感受回甲機制帶來的隱性代價——集中攻擊一顎，另外兩顎的泡幕仍全力運作

### Phase 2：「雙顎凋亡」 *(maw_1/2/3 中 ≥2 張 BROKEN)*

- **觸發**：三顎中任兩張達成 BROKEN
- **攻擊組合**：剩餘一張顎的模式 A（射頻 ×1.15，泡幕彈數 +2，見第 8 節難度表疊加）
- **回甲加速**：剩餘顎的 `armor_regen_rate` 套用 `tidemaw_phase2_regen_mult`（1.3）→ 6 → 7.8 BU/s——「最後一張顎更頑強地想長回來」
- **設計意圖**：戰場整體壓力下降（少了 2 個射擊節點），但最後一顎的回甲更快，逼玩家在此階段**不能鬆懈輸出**，否則先前的破甲進度會被更快抹平；此時 M2 loadout 的優勢收斂為「單顎壓制」，與單體武器差距縮小（因為只剩 1 個目標，不再需要多目標同時覆蓋）

### Phase 3：「背甲盡碎、心核裸露」 *(dorsal_plate BROKEN)*

- **觸發**：`dorsal_plate.break_state == BROKEN`（無論此時三顎/鰓根狀態如何，見 §4.3 速攻路線）
- **攻擊組合**：模式 B（心核放射環）+ 任何仍存活的顎持續其模式 A（若玩家選擇速攻路線跳過顎，此時可能仍有 1–3 張顎同時開火）
- **設計意圖**：終局清算階段；`heart_core` 判定框開啟、恆為可見且判定框放大（`hitbox_size_multiplier_core` = 1.2），玩家全力集火心核直到 BROKEN 觸發 `on_boss_core_break`

**速攻 vs 清場的總耗時對照**（D1，理論值，見 §4.3）：

| 策略 | 前段耗時 | 中後段耗時 | 總耗時（理論）| 過程風險 |
|------|---------|-----------|--------------|---------|
| 速攻（無視三顎+鰓根，直攻背甲/核心）| 0s | dorsal_plate 30–45s + heart_core 50–80s | ≈ 80–125s | 全程承受最多 3 條泡幕同時開火 |
| 全清場（先破三顎+鰓根，再攻背甲/核心）| 三顎+鰓根 ≈ 12–40s（依 loadout，見第 7 節）| 同上 80–125s | ≈ 95–165s | 前段風險高，後段近乎無干擾 |

兩條路線皆合法通關；選擇權完全交給玩家，呼應「頭目是靈魂」支柱。

---

## 7. 剋制與偏好 Loadout (Weapon Affinity)

### 武器表現速覽

| 武器 | TIDEMAW 表現 | 原因 |
|------|------------|------|
| **M2 蜂群飛彈** | ★★★ 主剋制武器 | 8 枚微型飛彈同時扇形覆蓋三顎，彈匣週期（5.0s）與 `armor_regen_grace_s`（5.0s）刻意校準相等，可讓三顎的回甲永遠不啟動，實現真正的「同時壓制」（見 §4.4 運算範例二）|
| **M1 追蹤飛彈** | ★★ 部分並行 | 雙發追蹤可同時鎖定 2 個不同目標（若在追蹤角範圍內），對雙顎有一定並行壓制效果，但無法覆蓋全部三顎 |
| **L1 散波雷射** | ★★ 關鍵輔助 | 三束雷射可同時軟化多張顎（熱量軌），啟用「軟化貫穿」使任何後續飛彈都能有效填充 BU；本身不填 BU，但為全隊 loadout 提供效率基礎 |
| **L2 集束雷射** | ★★ 單體蓄熱最速 | 精準快速軟化單一目標，適合「全力鎖定一張顎」的單體策略（見 §4.4 運算範例二 loadout A）|
| **L3 波動砲** | ★★ 背甲剝甲＋單顎爆發 | 唯一能剝除 `dorsal_plate` 護甲的快速路徑；對顎命中時給予 stagger_break_mult（1.5）單發爆發，適合單體衝刺打法 |
| **L4 穿透雷射** | ★ 小利基 | `dorsal_plate` ↕ `heart_core` 垂直對齊，單發同時蓄熱兩部位；三顎橫向排列不在此利基範圍內 |
| **M3 穿甲魚雷** | ★★ 單顎爆發最速 | 熱衝擊引爆（SOFTENED 前提）單發 60 BU，最快速清空「全力鎖定」的單一目標；但無追蹤、彈匣僅 3 枚，人工輪替多顎極易落入回甲陷阱（見 §4.4 運算範例三）|
| **M4 集束炸彈** | ★★ 情境並行 | 落點 AoE 若同時覆蓋 2 張相鄰顎，可比照 M2 的多目標均分邏輯提供部分並行壓制；命中顎數依落點與顎間距而定，不如 M2 穩定 |

### 展示 Loadout：M2 蜂群飛彈 ×（任一雷射）「壓制-磨蝕」

本 Loadout 是 TIDEMAW 的**設計展示組合**，直接演示「持續壓制優於逐一擊破」的核心教學。

**打法節奏（M2 × L1，三顎並行清場）**：
```
T = 0s      ：L1 散波雷射同時掃過三顎，各自進入 SOFTENED（軟化貫穿生效）
T = 0–12s   ：M2 每 5s 一次齊射，8 枚均分三顎，三顎並行填充 BU（≈8.3 BU/s／顎）
T ≈ 12s     ：三顎 B_current 幾乎同時達 100 BU → 三顎近乎同時 BROKEN（三條泡幕同時消音）
T = 12–?s   ：轉打 gill_root（NORMAL，較小 H/B_max，10–18s 內清空，可選）
T = ?–?+35s ：L3 剝除 dorsal_plate 背甲（或持續軟化貫穿路徑，30–45s）
T = 之後     ：heart_core 判定框開啟，集火至 BROKEN（50–80s）→ 勝利
```

### 無 M2 Loadout 仍可通關（公平性保證）

TIDEMAW 在 D1 對**所有合法 Loadout** 均可通關，不存在強制解——差異只在於「並行壓制」與「逐一擊破」的耗時與過程風險（見 §4.4 運算範例）：

| Loadout 範例 | 策略路線 | 三顎清場預估耗時 |
|-------------|---------|-----------------|
| M2 × L1 | 並行壓制（展示路線）| ≈ 12–18s（三顎近乎同時破）|
| L2 × M3 | 全力鎖定單顎，逐一擊破 | ≈ 10s／顎 × 3 ≈ 30–35s（循序）|
| L3 × M1 | L3 剝甲式衝刺 + M1 雙發並行 2 顎 | ≈ 20–28s（前兩顎並行，第三顎補刀）|
| 任意速攻（跳過三顎/鰓根）| 直攻背甲/核心 | 0s（三顎不清，但全程承受泡幕）|

**無法被回甲卡關的保證**：任何「全力鎖定單一目標直到 BROKEN」的策略完全不受 ArmorRegen 影響（寬限期只對「已離開的目標」生效），因此**沒有任何 loadout 會被回甲機制鎖死**——回甲只會讓「試圖同時兼顧多個目標卻做不到」的打法變慢或無效，不會讓任何合法策略變得不可能。

---

## 8. 難度縮放 (Difficulty Scaling)

**縮放原則**：僅調整子彈密度（數量與射速），不改變子彈速度、部位數值、`armor_regen_grace_s`/`armor_regen_rate`、或 stagger_duration。完全服從 kaiju-part-system.md C.8 難度不縮放規則。

### 模式 A：顎泡幕（每顎獨立）

| 難度 | 每次齊射彈數 | 扇角 | 每顎射頻 |
|------|------------|------|---------|
| D1 Normal | 7 | ±40° | 1/3.5s |
| D2 Hard | 9 | ±40° | 1/3.2s |
| D3 Extreme | 11 | ±40° | 1/2.8s |
| D4 Nightmare | 13 | ±40° | 1/2.4s |

> Phase 2（剩餘單顎）疊加 `tidemaw_phase2_speed_mult`（預設 1.15，範圍 1.10–1.30，比照 CARAPEX `carapex_phase2_dorsal_speed_mult` 命名慣例）：射頻再 ×1.15，彈數額外 +2（各難度基礎值上再加）。

### 模式 B：心核放射環（Phase 3 專屬）

| 難度 | 每次環形彈數 | 射頻 |
|------|------------|------|
| D1 Normal | 8 | 1/3.5s |
| D2 Hard | 10 | 1/3.0s |
| D3 Extreme | 12 | 1/2.6s |
| D4 Nightmare | 14 | 1/2.2s |

### 恆定不縮放項目

| 項目 | 恆定值 | 原因 |
|------|--------|------|
| 泡幕子彈速度 | 75 px/s | 速度縮放會破壞「編織走位」的可讀節奏 |
| 放射環子彈速度 | 100 px/s | 同上 |
| `armor_regen_grace_s` | 5.0s | 回甲寬限期不隨難度改變；此值與 M2 預設彈匣週期的校準關係必須跨難度保持一致，否則 M2 的「教學展示」在高難度會失準 |
| `armor_regen_rate` | 6 BU/s（P2 加速後 7.8 BU/s）| 同上 |
| 部位 H_max/B_max/required_break_threshold | 全域旋鈕值 | kaiju-part-system.md C.8 |
| `heart_core` 判定框開啟條件 | `dorsal_plate BROKEN` | 覆寫規則不受難度影響 |

**難度的隱性效應**：高難度下泡幕更密 → 玩家閃避頻率上升 → 雷射/飛彈有效命中 uptime 下降 → 三顎與心核的實際 TTB 自然延長，無需額外設定，完全符合「難度是門，不是牆」支柱。

---

## 9. 素材產出 (Material Drops)

### 掉落表定義

| drop_table_id | 對應部位 | 部位類型 | 核心素材 | shard_common（Standard/Precision/Perfect）|
|--------------|---------|---------|---------|-------------------------------------------|
| `drop_tidemaw_normal` | maw_1, maw_2, maw_3, gill_root | NORMAL | `core_abyss` × 1（Perfect = ×2）| ×2 / ×3 / ×4 |
| `drop_tidemaw_armored` | dorsal_plate | ARMORED | `core_abyss` × 1（Perfect = ×2）| ×2 / ×3 / ×4 |
| `drop_tidemaw_core` | heart_core | BOSS_CORE | `core_abyss` × 1（Perfect = ×2）| ×2 / ×3 / ×4 |

*TIDEMAW 屬深淵系（kaiju_tidemaw，`KaijuTheme.Abyss`）巨獸，所有部位均掉落 `core_abyss`（material-economy.md C.1 巨獸主題映射規則）。碎片計算沿用 `floor(shard_base × quality_mult)`；shard_base = 2，Standard = 1.0×，Precision = 1.5×（floor=3），Perfect = 2.0×（=4）。*

### 一場狩獵預期產出（D1，Precision 品質為主）

| 通關策略 | core_abyss | shard_common |
|---------|-----------|-------------|
| **全破壞**（6 部位全破）| 6 | 18 + 5 完成獎勵 = **23** |
| **速攻**（僅背甲＋核心，跳過三顎/鰓根）| 2 | 6 |
| **雙顎＋背甲＋核心**（跳過 1 顎/鰓根）| 4 | 12 |

### `essence_kaiju` 產出

`on_hunt_end(is_all_parts_broken = true)` 時觸發（6 個部位均 BROKEN）→ `essence_kaiju` × 1 + `shard_completeness_bonus`（= 5 碎片）。任何難度階均適用，無難度門檻。

### core_abyss 的跨遊戲戰略意義

依 `material-economy.md` C.1 巨獸主題映射，`core_abyss` 是 TIDEMAW 專屬（深淵系）核心素材，用於升級對應武器（依 material-economy.md 屆時定義的映射表）。TIDEMAW 作為 D4 難度階頭目，是 `core_abyss` 的**唯一來源**——想升該系武器的玩家必須主動挑戰本戰的消耗戰機制，形成「越硬的怪，素材越關鍵」的農刷驅動力。

---

## 10. 驗收標準 (Acceptance Criteria)

### AC-01 ArmorRegen 機制正確性（功能性 — 阻斷）

- [ ] 持有 `ArmorRegen` 資料的部位（三顎）：`t_since_break_hit` 每幀正確累積，並在收到 `on_missile_hit`(B_fill>0) 或 `on_l3_wave_hit` 時重置為 0
- [ ] `t_since_break_hit >= armor_regen_grace_s`（5.0s）後，`B_current` 以 `armor_regen_rate`（6 BU/s，P2 後 7.8 BU/s）每幀遞減，clamp 至 0
- [ ] `on_laser_hit`（雷射蓄熱）**不**重置 `t_since_break_hit`（雙軌獨立性驗證）
- [ ] `gill_root`/`dorsal_plate`/`heart_core`（`ArmorRegen == null`）完全不受回甲公式影響
- [ ] BROKEN 部位跳過回甲更新（不可逆終態，與 `part_regen_enabled` 全域規則相容）
- [ ] 自動化測試：`tests/unit/kaiju/tidemaw_armor_regen_test`

### AC-02 M2 教學時刻驗證（體驗性 — 阻斷）

- [ ] 5 人用戶測試：使用 M2×L1 loadout 通關後，受測者能在不提示下描述「蜂群飛彈能同時壓住三張顎」的機制認知，達成率 ≥ 70%
- [ ] 實測：M2×L1 loadout 三顎清場耗時（D1）落在 §7 展示序列的 12–18s 範圍內
- [ ] 實測：純單體武器（L2×M3）循序清場三顎耗時（D1）落在 30–35s 範圍內，且明顯長於 M2 loadout（差距 ≥ 1.5 倍）

### AC-03 公平性——所有 Loadout 皆可通關（功能性 — 阻斷）

- [ ] 8 種武器的 64 組 loadout 矩陣測試中，TIDEMAW（D1）皆可在合理 TTB 內通關，無任何組合被 ArmorRegen 機制鎖死
- [ ] 「全力鎖定單一目標直到 BROKEN」策略在自動化測試中驗證不受回甲影響（寬限期對正在被攻擊的目標永不觸發）
- [ ] 自動化測試：`tests/unit/weapon/weapon_loadout_matrix_test`（新增 tidemaw 案例）

### AC-04 FireGate 消音正確性（功能性 — 阻斷）

- [ ] 對應顎 `break_state == BROKEN` 後，該顎的模式 A 立即永久停止發射（無延遲、無殘留彈幕）
- [ ] 顎的 `heat_state`/`B_current` 進度不影響模式 A 的射頻或彈數（僅 `break_state` 決定開火與否）
- [ ] 自動化測試：`tests/unit/kaiju/tidemaw_firegate_test`

### AC-05 心核判定框覆寫正確性（功能性 — 阻斷）

- [ ] `dorsal_plate.break_state != BROKEN` 期間，任何武器對 `heart_core` 的命中判定一律無效（不觸發 `on_laser_hit`/`on_missile_hit`）
- [ ] `dorsal_plate` 進入 `BROKEN` 的同一幀，`heart_core` 判定框立即開啟
- [ ] `heart_core` 開啟後不會因任何後續事件重新關閉
- [ ] 自動化測試：`tests/unit/kaiju/tidemaw_core_gate_test`

### AC-06 難度密度縮放正確性（功能性）

- [ ] 泡幕/放射環子彈速度（75 px/s、100 px/s）在 D1–D4 下恆定
- [ ] `armor_regen_grace_s`、`armor_regen_rate` 在 D1–D4 下讀取值完全相同
- [ ] 各模式彈數與射頻依第 8 節表格縮放；靜態審核各難度 `spawn_config` 參數
- [ ] `difficulty_invariance_test` 覆蓋 TIDEMAW 案例

### AC-07 彈幕可讀性（體驗性 — UX 阻斷）

- [ ] 5 人用戶測試：含各難度泡幕截圖中，受測者辨識「敵彈 vs 安全間隙」準確率 ≥ 80%
- [ ] 泡幕子彈判定色（珊瑚橙 `#FF7A45`）在 D4 最高密度下仍可與玩家判定點（冷色）形成 0.1s 可辨識對比
- [ ] telegraph（0.5s 顎部蓄力閃光、0.6s 心核脈動）在截圖靜態測試中被 ≥ 80% 受測者正確識別為「即將發生攻擊」

### AC-08 素材掉落正確性（功能性 — 阻斷）

- [ ] 6 個 `drop_table_id` 均正確攜帶、觸發 `core_abyss` + `shard_common` 掉落（依品質乘數）
- [ ] Perfect 品質核心數量 = 2（`core_perfect_double_drop = TRUE`）
- [ ] 全破壞結算：`essence_kaiju` × 1 + `shard_completeness_bonus`（= 5）
- [ ] 自動化測試：`tests/unit/economy/material_yield_quality_test`（新增 tidemaw 6 drop_table 案例）

### AC-09 垂直對齊 L4 利基（功能性）

- [ ] `dorsal_plate` 與 `heart_core` 在場景佈局中確認垂直對齊（同一 x 軸 ±5%）
- [ ] L4 穿透雷射單發確認可同時命中兩部位（各自接收蓄熱事件，`heart_core` 判定框開啟後方可實際命中，但蓄熱事件本身不受判定框開關限制——設計師需於實作時確認蓄熱與命中判定的分離邏輯）
- [ ] 由關卡設計師於 Boss 佈局評審時確認，記錄於 Boss 場景設定文件

---

*文件版本：1.0.0*
*作者：Game Designer Agent*
*kaiju_id：tidemaw | 頭目序號：06 | 難度階：D4*
*關聯 GDD：game-concept.md | kaiju-part-system.md（LOCKED 狀態機，本文件擴充 ArmorRegen）| weapon-system.md（LOCKED 武器數值）| material-economy.md（drop_table 覆寫需協調）| 00-roster-overview.md §3.3（設計膠囊來源）*
