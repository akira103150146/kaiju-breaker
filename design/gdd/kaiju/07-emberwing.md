# 燼使 / EMBERWING — 頭目設計文件
## 殲獸戰機 / KAIJU BREAKER · Boss #07

`kaiju_id: emberwing` | 難度階建議: 4（Nightmare 前一階）| 8 頭目陣容序位: #07
*文件路徑：`design/gdd/kaiju/07-emberwing.md`*
*最後更新：2026-07-08*
*狀態：Draft*
*相依文件：game-concept.md | weapon-system.md（LOCKED）| kaiju-part-system.md | material-economy.md | kaiju/00-roster-overview.md §3.4*
*YAML 資料定義：`assets/data/kaiju/emberwing.yaml`（由本文件第 4.3 節 inline 提供）*

---

## 1. 概覽 (Overview)

燼使（EMBERWING，`kaiju_id: emberwing`）是 8 頭目陣容中的第 7 位，主題為**餘燼系（Ember）**：一隻橫跨畫面近乎整個寬度的燃燒使者，雙翼展開占畫面寬度 **80% 以上**，弱點以「燼孔」的形式散佈在整片寬幅翼面上。牠是三頭目陣容 VOLTWYRM（縱向巨蛇，服務 L4 穿透雷射的**垂直**利基）在幾何上的鏡像對照：EMBERWING 把同樣的「巨獸幾何服務單一武器」設計哲學，套用在 **L3 波動砲的蓄力寬幅波（Charged Wide Wave）**上。

**首要設計職責**：本文件對 `weapon-system.md` 的 L3 波動砲蓄力模式（「全幅震波」）在多部位情境下的行為做出明確設計裁定——當寬幅波的判定範圍同時覆蓋多個部位時，**每個被覆蓋的部位各自獨立收到完整的 `on_l3_wave_hit`**（完整 2 秒 STAGGERED，ARMORED 部位完整 ARMOR_STRIPPED），而不是把效果拆分到多個部位上稀釋。此規則比照 `weapon-system.md` C.4 對 L4 穿透雷射「路徑上每個部位各自受 D₀，線性累積」的既有精神——STAGGERED 是狀態旗標而非可切分的傷害池，切分沒有意義。EMBERWING 的 7 個部位刻意排成一列橫跨畫面的**寬幅走廊（Wide Wave Corridor）**，讓玩家一次置中蓄力釋放就能讓全翼同時進入震盪窗口，這是本 Boss 最核心的教學展示時刻。

本文件為**內容設計文件（Boss Content Design）**，非系統 GDD。部位數值、事件契約、材料產出等系統規則以相依 GDD 為準，本文件僅引用不重定義。

---

## 2. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：感官愉悅（Sensation）＋挑戰（Challenge）＋表達（Expression）

> 「我蓄力的這一砲，能把整片天空點燃。」

玩家面對的是一道「橫向方程式」——與 VOLTWYRM 的「把戰機對正縱軸」相反，EMBERWING 要求玩家把戰機對正**橫向中線**，花 1.5 秒蓄力（在螺旋燼臂與翼根礫壁交織的彈幕縫隙中穩住不動），然後放手：一道寬達畫面 80% 以上的震波橫掃整個翼展，兩側翼根同時裂甲、四個燼孔同時進入 2 秒震盪窗口。這是全遊戲中「一次輸入、全局回饋」規模最大的時刻，服務〔科技對巨獸〕與〔破壞即獎勵〕支柱。

窄幅武器（L2 集束、M3 魚雷）依然完全可行——但玩家必須親自把戰機來回移動，一個燼孔一個燼孔地磨過去，用雙腳（雙翼？）丈量整個翼展的代價換取同樣的勝利。這個「寬版型獎勵範圍/蓄力思維，窄武器要來回」的落差，是本 Boss 對「以智取勝」核心幻想最直白的體現：懂得選對工具的玩家，體感節奏會與蠻力玩家截然不同，但兩者都能贏。

---

## 3. 外形與主題 (Silhouette & Theme)

### 設計語言

| 維度 | 設計決策 |
|------|---------|
| **生物原型** | 餘燼系使者：焦黑骨架翼骨 + 半透明琥珀色翼膜，翼緣鑲嵌會脈動發光的燼孔；中央胸腔藏著熾心 |
| **像素規格** | 畫面佔寬 **80–88%**（本 Boss 最極端的寬度指標，對照 VOLTWYRM 的縱向 70–80% 高度）；縱向佔高僅 30–40%（扁平寬幅剪影，與 VOLTWYRM 的瘦長縱剪影形成鮮明對比）|
| **色系** | 餘燼暖色系：焦黑翼骨（外框）+ 琥珀半透明翼膜（底色）+ 熾心深紅内層；子彈與部位光暈一律暖色 |
| **動態** | **刻意不做整體水平漂移**（見下方「翼展對齊鐵則」）；僅有輕微垂直呼吸擺動（±5% 畫面高度，週期 6s）+ 翼尖獨立的小幅上下振翅（Oscillate，±15px，週期 3s，左右翼相位錯開營造「呼吸感」）|

### 翼展對齊鐵則（Wide Wave Corridor 設計約束）

**EMBERWING 是全陣容中唯一沒有整體水平漂移的頭目**。CARAPEX 與 VOLTWYRM 的頭目都有水平/S 形漂移，但 EMBERWING 的 7 個部位必須維持**同一橫向水平帶（Wing Row）**上的相對位置，否則 L3 寬幅波「一次掃過一整排」的教學核心就會被巨獸自身的位移抵銷——這對窄武器玩家不公平（他們已經要承擔「來回移動」的代價，若目標本身還在整體橫移，代價會不成比例地放大）。因此本 Boss 只允許**部位局部的小幅垂直振翅**（不影響橫向對齊），整體軀幹以垂直呼吸取代水平漂移。此設計決策應寫入 Boss 場景設定文件，供關卡設計師佈局評審時確認（見 AC-06）。

### 色彩視覺鐵則

- **暖色 = 威脅**：所有敵彈（螺旋燼臂、翼根礫壁、熾心爆發）均為暖色系（橙 / 金 / 深紅），與玩家亮藍判定點維持冷暖對比。
- **SOFTENED（軟化）狀態統一使用 `#FF6600`**：所有部位（燼孔 / 翼根 / 熾心）進入軟化時，光暈統一使用暖橙 `#FF6600` 脈動（沿用專案全域 SOFTENED 色彩鐵則，非本 Boss 專屬色）。
- **ARMOR_INTACT vs ARMOR_STRIPPED（翼根）**：
  - `ARMOR_INTACT`：翼根呈焦黑鱗甲紋理，飛彈命中觸發火花偏轉特效
  - `ARMOR_STRIPPED`：鱗甲爆開像素動畫（0.15s），暴露深紅弱點核心（2px 亮白外框脈動）+ 右上角 2s 像素倒計時條（沿用 CARAPEX `dorsal_cannon` 視覺規格）
- **殘焰拖尾（Residual Fire）必須清楚可辨**：殘焰區域使用脈動的 `#FF4500`（橙紅）色塊 + 熱浪扭曲後製濾鏡，形狀為固定半徑圓形，邊緣有清晰的 1px 亮黃描邊，確保與移動中的螺旋彈區分（殘焰是**靜止**危險區，彈幕是**移動**判定點）。

---

## 4. 部位組成 (Part Composition)

### 4.1 部位一覽表

| `part_id` | 中文名 | 類型 | `H_max` | `B_max` | 相鄰部位 | `drop_table_id` | 說明 |
|-----------|-------|------|---------|---------|---------|----------------|------|
| `wing_vent_l2` | 左翼外燼孔 | NORMAL | 100 | 100 | `wing_root_l` | `drop_emberwing_vent` | 左翼尖端；**破壞受翼根閘門鎖定**（見 4.2）|
| `wing_root_l` | 左翼根 | ARMORED | 150 | 150 | `wing_vent_l2`, `wing_vent_l1` | `drop_emberwing_root` | 左側護甲閘門；鎖住 `wing_vent_l2` 的可破壞性 |
| `wing_vent_l1` | 左翼內燼孔 | NORMAL | 100 | 100 | `wing_root_l`, `heart_core` | `drop_emberwing_vent` | 靠近軀幹；**無鎖定，隨時可破** |
| `heart_core` | 熾心 | BOSS_CORE | 200 | 200 | `wing_vent_l1`, `wing_vent_r1` | `drop_emberwing_core` | 勝利條件；居中，永遠可見 |
| `wing_vent_r1` | 右翼內燼孔 | NORMAL | 100 | 100 | `heart_core`, `wing_root_r` | `drop_emberwing_vent` | 靠近軀幹；**無鎖定，隨時可破** |
| `wing_root_r` | 右翼根 | ARMORED | 150 | 150 | `wing_vent_r1`, `wing_vent_r2` | `drop_emberwing_root` | 右側護甲閘門；鎖住 `wing_vent_r2` 的可破壞性 |
| `wing_vent_r2` | 右翼外燼孔 | NORMAL | 100 | 100 | `wing_root_r` | `drop_emberwing_vent` | 右翼尖端；**破壞受翼根閘門鎖定**（見 4.2）|

**部位數量**：共 7 個部位，符合 `kaiju-part-system.md` A 節後期頭目上限（≤8）。全數使用全域旋鈕預設值，無 `H_max_override` / `B_max_override`——EMBERWING 的難度定位靠**寬幅走廊的空間張力**與彈幕密度達成，不靠部位數值堆疊。

### 4.2 翼根破壞閘門（Wing-Root Break Gate — 本 Boss 專屬跨部位規則）

這是 EMBERWING 引入的**新跨部位規則**，擴充 `kaiju-part-system.md` 既有的 ARMORED 護甲閘門機制（該機制只管束部位**自身**的飛彈填充），新增「一個部位的護甲狀態鎖住**另一個**部位的可破壞性」：

```
只要 wing_root_l.break_state == ALIVE：
    wing_vent_l2 的任何 on_missile_hit 一律 M_state_mult = 0（飛彈偏轉）
    無論 wing_vent_l2 自身 heat_state 是否已 SOFTENED
    （「軟化貫穿」旁路不適用於此鎖——這是比自身護甲更外層的閘門）

當 wing_root_l.break_state → BROKEN（永久）：
    wing_vent_l2 的破壞閘門永久解除
    此後 wing_vent_l2 依標準 NORMAL 部位規則計算 M_state_mult（0.35 / 1.0 / 1.5）
```

`wing_root_r` 與 `wing_vent_r2` 為鏡像對稱關係，規則相同。

**與翼根「軟化貫穿」的差異**：`wing_root_l` 自身作為 ARMORED 部位，仍完整適用 `kaiju-part-system.md` C.4 的兩條開甲路徑（任一雷射蓄熱至 SOFTENED＝標準路徑；L3 蓄力震波＝快速路徑）——翼根本身**不會**被本規則鎖死，只有它所守衛的外燼孔才受鎖。這確保「先破兩側翼根，才能碰外燼孔」的節奏，同時翼根本身的破壞路徑與其他 Boss 的 ARMORED 部位完全一致，不引入額外例外。

**設計意圖**：內燼孔（`wing_vent_l1` / `wing_vent_r1`，緊鄰熾心）從開戰起就可自由破壞，讓玩家在 Phase 1 就有立即可得的破壞回饋；外燼孔（翼尖）則獎勵「先攻翼根」的策略深度——這是「頭目是靈魂」支柱在寬幅版型上的落地：可選擇性不是「要不要打」，而是「要不要先解開閘門」。

**唯一觸發路徑澄清（防止誤解為需要 L3）**：`wing_root_l/r` 的 BROKEN 觸發途徑與任何 ARMORED 部位相同——玩家可用純雷射+飛彈（標準路徑，任何 loadout 皆可行）磨穿翼根 150 BU，L3 只是加速手段。**沒有 L3 的玩家同樣能解開外燼孔閘門**，只是耗時較長（見第 7 節）。

### 4.3 空間佈局（Wide Wave Corridor — ASCII 示意）

```
                              螢幕頂部（頭目佔寬 80–88%）
┌──────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│  [燼孔L2]    [翼根L]     [燼孔L1]      [熾心]      [燼孔R1]    [翼根R]    [燼孔R2] │
│   外/尖端     ARMORED     內/無鎖       CORE        內/無鎖     ARMORED    外/尖端  │
│   x≈10%      x≈22%       x≈36%        x≈50%       x≈64%      x≈78%      x≈90%   │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘
                              螢幕底部（玩家區域）

L3 波動砲蓄力寬幅波（判定寬度 ≥ 畫面 80%，x: 8%–92%）：
  戰機置中蓄力釋放 → 波形涵蓋全部 7 個部位的 x 座標
  → 逐一對每個重疊部位發出獨立 on_l3_wave_hit：
      wing_root_l  → ARMOR_STRIPPED + STAGGERED(2s)
      wing_root_r  → ARMOR_STRIPPED + STAGGERED(2s)
      wing_vent_l1/l2/r1/r2 + heart_core → STAGGERED(2s，飛彈 ×1.5)
  → 全翼 7 部位同時進入震盪窗口，是本 Boss 最強教學展示時刻

窄幅武器（如 L2 集束）等效路徑：
  戰機需在 x=10% ↔ x=90% 之間來回橫移，逐一對正單一部位蓄熱
  （見第 7 節「橫移代價」量化比較）
```

**相鄰鏈（線性橫向鏈，供 Tier-3 消費）**：
`wing_vent_l2` ↔ `wing_root_l` ↔ `wing_vent_l1` ↔ `heart_core` ↔ `wing_vent_r1` ↔ `wing_root_r` ↔ `wing_vent_r2`

此線性鏈與 VOLTWYRM 的縱向相鄰鏈（供 L4 展示）結構呼應，唯一差異是本鏈沿橫軸展開，供 L3 寬幅波與 L2 破點漣漪/M3 穿甲爆破鏈使用。

### 4.4 `assets/data/kaiju/emberwing.yaml`

```yaml
kaiju_id: "emberwing"
display_name: "燼使 / EMBERWING"
difficulty_tier: 4
theme: "Ember"          # KaijuTheme 新值；每部位掉落 core_ember

body_movement:
  type: Oscillate
  axis: Y
  amplitude_pct: 5
  period_s: 6.0
  # 刻意省略水平漂移欄位——見第 3 節「翼展對齊鐵則」

parts:
  - id: "wing_vent_l2"
    type: NORMAL
    H_max_override: null            # 100 HU
    B_max_override: null            # 100 BU
    adjacency: ["wing_root_l"]
    drop_table_id: "drop_emberwing_vent"
    layout: { x_pct: 10, wing_row: true }
    movement:
      type: Oscillate
      axis: Y
      amplitude_px: 15
      period_s: 3.0
      phase_offset_s: 0.0
    emitters:
      - type: RadialSpiral
        bolt_speed_px_s: 110
        angle_step_deg: 20
        color: "#FF8000"
        residual_fire:
          radius_px: 20
          duration_s: 1.5
          color: "#FF4500"
    break_gate:                     # 本 Boss 專屬新欄位（見 4.2）
      type: RequireSiblingBroken
      sibling_id: "wing_root_l"

  - id: "wing_root_l"
    type: ARMORED
    H_max_override: null            # 150 HU
    B_max_override: null            # 150 BU
    adjacency: ["wing_vent_l2", "wing_vent_l1"]
    drop_table_id: "drop_emberwing_root"
    layout: { x_pct: 22, wing_row: true }
    movement: null                  # 翼根固定不振翅，作為視覺錨點
    emitters:
      - type: AimedWall
        bolt_speed_px_s: 100
        color: "#FFCC00"
        pauses_on_armor_stripped: true

  - id: "wing_vent_l1"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["wing_root_l", "heart_core"]
    drop_table_id: "drop_emberwing_vent"
    layout: { x_pct: 36, wing_row: true }
    movement:
      type: Oscillate
      axis: Y
      amplitude_px: 15
      period_s: 3.0
      phase_offset_s: 1.0
    emitters:
      - type: RadialSpiral
        bolt_speed_px_s: 110
        angle_step_deg: 20
        color: "#FF8000"
        residual_fire:
          radius_px: 20
          duration_s: 1.5
          color: "#FF4500"
    break_gate: null                 # 無鎖定，隨時可破

  - id: "heart_core"
    type: BOSS_CORE
    H_max_override: null            # 200 HU
    B_max_override: null            # 200 BU
    adjacency: ["wing_vent_l1", "wing_vent_r1"]
    drop_table_id: "drop_emberwing_core"
    layout: { x_pct: 50, wing_row: true }
    movement: null                  # 核心固定，作為終局視覺錨點
    emitters:
      - type: AimedBurst
        bolt_speed_px_s: 95
        color: "#CC2200"

  - id: "wing_vent_r1"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["heart_core", "wing_root_r"]
    drop_table_id: "drop_emberwing_vent"
    layout: { x_pct: 64, wing_row: true }
    movement:
      type: Oscillate
      axis: Y
      amplitude_px: 15
      period_s: 3.0
      phase_offset_s: 1.0
    emitters:
      - type: RadialSpiral
        bolt_speed_px_s: 110
        angle_step_deg: 20
        color: "#FF8000"
        residual_fire:
          radius_px: 20
          duration_s: 1.5
          color: "#FF4500"
    break_gate: null

  - id: "wing_root_r"
    type: ARMORED
    H_max_override: null
    B_max_override: null
    adjacency: ["wing_vent_r1", "wing_vent_r2"]
    drop_table_id: "drop_emberwing_root"
    layout: { x_pct: 78, wing_row: true }
    movement: null
    emitters:
      - type: AimedWall
        bolt_speed_px_s: 100
        color: "#FFCC00"
        pauses_on_armor_stripped: true

  - id: "wing_vent_r2"
    type: NORMAL
    H_max_override: null
    B_max_override: null
    adjacency: ["wing_root_r"]
    drop_table_id: "drop_emberwing_vent"
    layout: { x_pct: 90, wing_row: true }
    movement:
      type: Oscillate
      axis: Y
      amplitude_px: 15
      period_s: 3.0
      phase_offset_s: 0.0
    emitters:
      - type: RadialSpiral
        bolt_speed_px_s: 110
        angle_step_deg: 20
        color: "#FF8000"
        residual_fire:
          radius_px: 20
          duration_s: 1.5
          color: "#FF4500"
    break_gate:
      type: RequireSiblingBroken
      sibling_id: "wing_root_r"
```

> **實作備註（給資料模型負責人）**：`emitters[].residual_fire.color`、`break_gate`、`layout.x_pct`、`movement.phase_offset_s` 為本文件新增欄位，擴充 `kaiju/00-roster-overview.md` §4 提出的 `PartDef.Emitters[]` / `PartMovement` 草案。`break_gate`（跨部位破壞閘門）與該文件描述的 `FireGate`（發射閘門，控制是否開火）是**不同概念**——`break_gate` 管束的是「能不能被打破」，不影響該部位自身是否持續開火；請勿混用同一 schema 欄位實作兩者。`residual_fire.spawn_interval_s` 依難度縮放，見第 8 節難度表（不在此 YAML 靜態宣告，由難度系統於執行期查表）。

---

## 5. 攻擊模式 (Attack Patterns)

**全域規則**：所有子彈為暖色系，子彈速度跨四難度階**恆定**；僅彈數/射速/殘焰生成頻率依難度縮放（見第 8 節）。**Fire-gate 消音鐵則**：任一部位 `break_state → BROKEN` 時，其自身 emitter 立即停止發射（不需額外設定，沿用 `kaiju-part-system.md` E.7/E.8 全域規則）。

---

### 模式 A：燼孔螺旋（Ember Vent Spiral）

| 屬性 | 值 |
|------|-----|
| **發射源** | `wing_vent_l1` / `wing_vent_l2` / `wing_vent_r1` / `wing_vent_r2`（各自獨立發射，互不同步）|
| **彈形** | 放射螺旋（Radial Spiral）：每個燼孔以固定角度增量（`angle_step` = 20°）連續發射燼彈，配合部位自身微幅振翅，形成緩慢旋轉的多臂螺旋外觀 |
| **子彈速度** | 110 px/s（四難度恆定）|
| **發射率（D1）** | 1 臂等效密度，脈衝間隔 0.30s |
| **附帶效果：殘焰拖尾** | 每個燼孔獨立計時，每隔一段間隔（D1 = 1.2s）在自身當前位置生成 1 個**殘焰區域**（靜止危險區，半徑 20px，存續 1.5s，`#FF4500` 脈動，1px 亮黃描邊）；殘焰不移動、不追蹤，純粹是「地面熔岩」式空間壓迫 |
| **電報** | 燼孔啟動瞬間（開戰或閘門解除後首次開火）有 0.4s 明亮爆閃預告；螺旋本身持續發射不重複電報（沿用「視覺本身即電報」設計）|
| **子彈色** | 燼彈 `#FF8000`；殘焰區域 `#FF4500` |
| **觸發條件** | 對應燼孔 `break_state == ALIVE`（**外燼孔從開戰起即發射，不受翼根閘門影響——翼根只鎖「可破壞性」，不鎖「開火」，見 4.2**）|
| **設計目的** | 教「多個獨立移動的小威脅同時存在」；殘焰教「戰場會累積靜態危險，不能靠一次閃避解決」；破壞任一燼孔即消音對應螺旋+停止新殘焰生成（既有殘焰仍會自然消失，不會被清除）|

---

### 模式 B：翼根礫壁（Wing-Root Ember Wall）

| 屬性 | 值 |
|------|-----|
| **發射源** | `wing_root_l` / `wing_root_r`（ARMORED，各自獨立發射）|
| **彈形（ARMOR_INTACT）** | 4 發水平散開扇形（向下噴散，覆蓋自身 x 座標左右各 ~15% 畫面寬）|
| **彈形（ARMOR_STRIPPED + STAGGERED）** | **暫停發射**（硬直 2.0s）；亮白脈動閃光標記弱點暴露，右上角 2s 倒計時像素條（沿用 CARAPEX `dorsal_cannon` 規格）|
| **子彈速度** | 100 px/s（四難度恆定）|
| **發射率（D1）** | 1 波 / 6.0s |
| **電報** | 發射前 0.6s，翼根出現向下聚光特效 |
| **子彈色** | 金黃 `#FFCC00`（ARMOR_INTACT）；STAGGERED 期間無子彈 |
| **觸發條件** | 對應翼根 `break_state == ALIVE`；`BROKEN` 後永久消音（該側翼根礫壁停止，不影響另一側）|
| **設計目的** | 教「護甲部位有規律威脅，但可用雷射軟化貫穿或 L3 剝甲讓它沉默」；同時是本 Boss 唯一明確標示「先攻我，才能碰翼尖」的視覺提示來源 |

---

### 模式 C：熾心爆發（Heart-Core Aimed Burst）

| 屬性 | 值 |
|------|-----|
| **發射源** | `heart_core` |
| **彈形（Phase 1–2：任一翼根尚存）** | 瞄準玩家方向的窄束子彈（D1–D2：單發；D3–D4：雙發 ±10°）|
| **彈形（Phase 3：雙翼根均 BROKEN）** | 扇形瞄準爆發（D1–D2：3 發 ±20°；D3–D4：5 發 ±35°）|
| **子彈速度** | 95 px/s（全 Boss 最慢，最容易閃避）|
| **發射率（D1，Phase 1–2）** | 1 次 / 3.5s |
| **發射率（D1，Phase 3）** | 1 次 / 2.0s |
| **子彈色** | 深紅 `#CC2200`（沿用 CARAPEX `chest_reactor_core` 色，全陣容「核心＝深紅」的視覺一致性）|
| **電報** | 核心脈動本身即電報，脈動峰值後 0.6s 發射 |
| **觸發條件** | 始終啟用；彈形依雙翼根破壞狀態切換 |
| **設計目的** | Phase 1–2：核心有存在感但低威脅，作為視覺錨點；Phase 3：雙翼根瓦解後核心「反撲」，直觀體現「你拆掉了它的翼，它豁出去了」|

### 模式觸發條件彙總

| 模式 | 啟動 | 停止 |
|------|------|------|
| A 燼孔螺旋（單一燼孔）| 對應燼孔 `ALIVE` | 對應燼孔 `BROKEN`（永久消音，含殘焰生成）|
| B 翼根礫壁（單一翼根）| 對應翼根 `ALIVE` + `ARMOR_INTACT` | 對應翼根 `BROKEN`（永久）；`ARMOR_STRIPPED` 期間暫停 2.0s |
| C 熾心爆發 | 始終 | `heart_core` `BROKEN`（戰鬥結束）|

---

## 6. 階段 (Phases)

階段由**部位破壞狀態**驅動，非血量閾值，且轉換單向不可逆。

### Phase 1：「雙翼展體形態」*(All Parts ALIVE)*

- **觸發**：戰鬥開始
- **攻擊組合**：模式 A（4 燼孔全螺旋）+ 模式 B（雙翼根礫壁，ARMOR_INTACT）+ 模式 C（Phase 1–2 節奏）
- **設計意圖**：全景展示「寬幅走廊」——玩家一眼看到 7 個發光弱點橫跨整個畫面。內燼孔（`wing_vent_l1/r1`）從第一秒起即可自由破壞，給予即時破壞回饋；翼根礫壁的規律扇形提示玩家「這兩處有護甲，且鎖著翼尖」。
- **核心爽點時刻**：玩家第一次置中蓄力 L3、放手看見全翼 7 部位同時亮起 STAGGERED 白框——這是整場戰鬥的關鍵 Aha moment，設計目標是在戰鬥開始後 30–60 秒內發生第一次（早於任何單一部位被破壞）。

### Phase 2：「翼根洞開形態」*(任一側 `wing_root` BROKEN)*

- **觸發**：`wing_root_l` 或 `wing_root_r` 任一達 `BROKEN`
- **攻擊組合**：
  - 破壞側：對應外燼孔（`wing_vent_l2` 或 `wing_vent_r2`）閘門解除，成為可破壞目標；該側兩個燼孔（內+外）螺旋方向反轉（`angle_step` 由 +20° → −20°，作為「這側狀態已改變」的清楚視覺信號）
  - 存活翼根：礫壁射頻提升 ×`emberwing_phase2_root_speed_mult`（預設 1.15，範圍 1.10–1.30），呼應 CARAPEX Phase 2 的補償設計慣例
  - 模式 C：節奏不變（仍為 Phase 1–2 表）
- **設計意圖**：破壞翼根立即帶來「解鎖 + 視覺反轉」雙重回饋，讓玩家清楚知道自己解開了什麼；存活翼根的補償加速維持整體壓力不因單側瓦解而驟降。

### Phase 3：「熾心裸露形態」*(雙側 `wing_root` 均 BROKEN)*

- **觸發**：`wing_root_l` 與 `wing_root_r` 均達 `BROKEN`
- **攻擊組合**：模式 A（全部 4 燼孔螺旋均已反轉，若燼孔本身尚存）+ 模式 B（雙側均永久停止）+ 模式 C（切換至 Phase 3 扇形爆發，D1: 3 way/2.0s → D4: 5 way/1.1s）+ 殘焰生成密度提升 ×`emberwing_phase3_residual_density_mult`（預設 1.3，範圍 1.2–1.5，即生成間隔 ÷ 1.3）
- **設計意圖**：兩側翼根同時瓦解後，礫壁徹底靜默，戰場的威脅來源收斂到「殘存燼孔螺旋（若有）+ 熾心扇形反撲」，形成與 CARAPEX Phase 3「移除模式 A 安全感、核心成為主威脅」相同的結構性收束，服務〔頭目是靈魂〕支柱的終局張力。
- **可選部位張力**：若玩家在此階段仍有燼孔存活，可繼續選擇「先清完全部燼孔換取完整素材/精魄」或「直接集火熾心速勝」——兩條路徑都合法。

---

## 7. 剋制與偏好 Loadout (Weapon Affinity)

### 武器表現速覽

| 武器 | EMBERWING 表現 | 原因 |
|------|----------------|------|
| **L3 波動砲** | ★★★ 主剋制武器 | 蓄力寬幅波（判定寬度 ≥ 80% 畫面）置中釋放可同時命中全部 7 個部位，各自獲得完整 STAGGERED（2s，飛彈 ×1.5）；雙翼根可望在極短時間內同時 ARMOR_STRIPPED，是本 Boss 唯一能「一次操作影響全場」的武器 |
| **M2 蜂群飛彈** | ★★★ 最佳副武器搭檔 | 8 枚齊射覆蓋 ~70% 畫面寬度，能在 L3 剛開啟的全翼震盪窗口內對多個部位同時填充 BU（Combo C「震盪-飽和」的寬幅升級版）|
| **L1 散波雷射** | ★★ 可行替代 | 3 束扇形可同時蓄熱多個相鄰燼孔，配合任一副武器走「標準軟化路徑」開翼根閘門，節奏穩健但不如 L3 一次性 |
| **L2 集束雷射** | ★★ 可行但吃橫移 | 單部位蓄熱效率最高（37.5 HU/s），但必須把戰機來回移動橫跨整個翼展逐一對正 7 個部位；見下方「橫移代價」量化比較 |
| **M1 追蹤飛彈** | ★★ 安全穩定 | 追蹤優勢在寬幅版型上發揮有限（部位大多靜止於各自 x 座標，只做小幅垂直振翅），但提供持續輸出的低壓選項 |
| **M3 穿甲魚雷** | ★★ 局部強力 | 對已軟化的單一燼孔（尤其兩個無鎖內燼孔）熱衝擊引爆效果不變；受限於瞄準需求，需玩家精確對位翼展上的目標 |
| **M4 叢集炸彈** | ★ 前緣利基 | 落點固定於艦前方 25–40% 螢幕高度；若 EMBERWING 佈局將部分燼孔壓低到此高度可小範圍命中 2 目標，但整體翼展偏上方，實際覆蓋率低於其他 Boss |
| **L4 穿透雷射** | ★ 最弱選項（刻意設計）| EMBERWING 的 7 個部位沿**橫軸**排列、縱向無重疊，L4 的縱向穿透優勢完全喪失，等功率約束下淪為「路徑上通常只命中 1 個部位」的最差選項——這正是 `weapon-system.md` E.3 所預期的「若 Boss 部位全橫向排列，L4 優勢喪失」情境示範，與 VOLTWYRM 的 L4 高光形成刻意的鏡像對照 |

### 展示 Loadout：L3 × M2「寬幅震盪-飽和（Wide Blast & Saturate）」

**戰鬥序列（L3 × M2，D1，戰機置中）**：

```
T = 0.0s     ：戰機移至畫面水平中線，開始蓄力 L3（hold）
T = 1.5s     ：蓄力完成，釋放全幅震波（判定 x: 8%–92%，涵蓋全部 7 個部位）
             ：wing_root_l / wing_root_r → ARMOR_STRIPPED + STAGGERED(2.0s)
             ：wing_vent_l1/l2/r1/r2 + heart_core → STAGGERED(2.0s，飛彈 ×1.5)
T = 1.6s     ：立即發射 M2 蜂群飛彈（8 枚，扇形覆蓋 ~70% 畫面寬，約命中 5–6 個目標）
             ：受 STAGGERED 覆蓋的目標各自以 ×1.5 效率填充 BU（翼根：D₀/8×10×1.5 ≈ 1.875 BU/枚；
               燼孔/熾心同理）——實際每個目標分配到的枚數依落彈分佈約 1–2 枚
T = 3.5s     ：STAGGERED 視窗結束（翼根恢復 ARMOR_INTACT，B_current 保留不清零）
T = 3.6–5.5s ：2.0s L3 冷卻中，可換打 M2（換彈 5s，此時多半仍在填彈）或補一輪雷射蓄熱
T = 5.5s     ：L3 再次可蓄力，重複循環
```

**近似 TTB 說明**：本序列的設計重點不是「單一部位最快 TTB」，而是「**全場聚合破壞速率（Aggregate Break Rate）**最高」——單次 L3+M2 循環讓 5–7 個部位同時獲得部分 BU 進度，多輪循環後這些部位會**接近同時**跨過各自的 `required_break_threshold`，產生密集的連續破壞高潮，而非單點打穿再移到下一個。精確落彈分佈（每枚飛彈實際命中哪個部位）取決於執行期彈幕生成實作，本節數字為示意近似值，非逐幀保證。

### 「橫移代價」量化比較（L2 集束雷射路徑）

| 步驟 | 耗時（近似）|
|------|-----------|
| 對正 `wing_vent_l2`（x=10%）蓄熱至 SOFTENED（37.5 HU/s，θ_S=100 HU）| ~2.9s 理論 / ~5–6s 實戰 |
| 橫移至 `wing_root_l`（x=22%，約 12% 畫面寬）| +1.0–1.5s（含閃避 downtime）|
| 橫移至 `wing_vent_l1`（x=36%）→ `heart_core`（x=50%）→ `wing_vent_r1`（x=64%）→ `wing_root_r`（x=78%）→ `wing_vent_r2`（x=90%）| 每段 +1.0–1.5s，共 5 段 ≈ +5–7.5s |
| **横跨全翼展總移動代價** | **約 +7–9s**（相對於 L3 置中一次觸及全部 7 部位）|

**結論**：L2×M3 等窄幅組合在 EMBERWING 上完全可行、且單部位蓄熱效率仍是全武器庫最高，但玩家需親自承擔「橫移丈量翼展」的時間代價（約 7–9 秒的額外橫移，分攤在整場戰鬥中）。這正是「窄武器可行但較慢，寬版型獎勵範圍/蓄力思維」的直接量化體現，符合〔難度是門，不是牆〕與等功率鐵則——沒有任何 loadout 被鎖死，只是節奏不同。

### 無 L3 × M2 Loadout 仍可通關（公平性保證）

| Loadout 範例 | 策略路線 | 相對節奏 |
|-------------|---------|---------|
| L3 × M2（展示路線）| 置中蓄力 → 全翼震盪 → 蜂群齊射洗入 | 基準（全場聚合最快）|
| L1 × M3 | 散波同時蓄熱相鄰燼孔 → 逐一熱衝擊引爆；翼根走標準軟化路徑開閘 | 略慢，約基準 1.3–1.5× |
| L2 × M1 | 精準蓄熱單一部位 + 追蹤飛彈持續填充；需自行橫移對正各部位 | 慢，約基準 1.6–1.8×（見橫移代價表）|
| 任意雷射 × M2（無 L3）| 靠雷射軟化開翼根閘門（標準路徑，×1.0 效率）→ M2 寬幅齊射清內外燼孔 | 中等，約基準 1.4–1.6×；翼根 TTB 落在系統標準 ARMORED 軟化貫穿路徑 30–45s 範圍 |

**無 L3 的 Loadout**：兩側翼根依然可破（標準軟化貫穿路徑，任一雷射皆可行），只是效率 ×1.0 而非 L3 窗口的 ×1.5，翼根 TTB 落在 `kaiju-part-system.md` C.3 定義的 30–45s 標準範圍內，而非 L3 快速路徑的更短耗時。玩家永遠不會被「沒有 L3 就打不破翼根」卡關。

---

## 8. 難度縮放 (Difficulty Scaling)

**縮放原則**：僅調整子彈密度（數量/射速/殘焰生成頻率），不改變子彈速度、部位數值、傷害、`stagger_duration` 或翼展佈局座標。完全服從 `kaiju-part-system.md` C.8 難度不縮放規則。

### 模式 A：燼孔螺旋

| 難度 | 螺旋密度（`angle_step` 恆定 20°）| 脈衝間隔 | 殘焰生成間隔 |
|------|-------------------------------|---------|------------|
| D1 Normal | 基礎密度 | 0.30s | 1.2s |
| D2 Hard | +1 檔 | 0.26s | 1.0s |
| D3 Extreme | +2 檔 | 0.22s | 0.8s |
| D4 Nightmare | +3 檔 | 0.18s | 0.6s |

> 子彈速度（110 px/s）與殘焰半徑/存續時間（20px / 1.5s）四難度恆定；僅發射間隔與殘焰生成頻率縮放。

### 模式 B：翼根礫壁

| 難度 | 每波彈數 | 射頻 |
|------|---------|------|
| D1 Normal | 4 | 1/6.0s |
| D2 Hard | 5 | 1/5.0s |
| D3 Extreme | 6 | 1/4.0s |
| D4 Nightmare | 7 | 1/3.2s |

> `ARMOR_STRIPPED` 期間（2.0s）各難度均暫停模式 B。`stagger_duration` = 2.0s 恆定不縮放。

### 模式 C：熾心爆發

| 難度 | Phase 1–2 彈形 | Phase 1–2 射頻 | Phase 3 彈形 | Phase 3 射頻 |
|------|--------------|--------------|------------|------------|
| D1 | 1 aimed | 1/3.5s | 3-way（±20°）| 1/2.0s |
| D2 | 1 aimed | 1/3.2s | 3-way（±20°）| 1/1.7s |
| D3 | 2 aimed（±10°）| 1/2.8s | 5-way（±35°）| 1/1.4s |
| D4 | 2 aimed（±10°）| 1/2.4s | 5-way（±35°）| 1/1.1s |

### Phase 補償旋鈕（各難度共用）

| 旋鈕 | 預設值 | 範圍 | 說明 |
|------|--------|------|------|
| `emberwing_phase2_root_speed_mult` | 1.15 | 1.10–1.30 | Phase 2 存活翼根礫壁射頻加速倍率（沿用 CARAPEX `carapex_phase2_dorsal_speed_mult` 慣例）|
| `emberwing_phase3_residual_density_mult` | 1.3 | 1.2–1.5 | Phase 3 殘焰生成間隔除以此值（生成更密集）|

兩者存於外部旋鈕，不硬編碼，四難度共用同一組補償倍率（補償幅度不隨難度額外縮放，維持系統簡潔）。

---

## 9. 素材產出 (Material Drops)

### 掉落表定義

| `drop_table_id` | 對應部位 | 部位類型 | 核心素材 | `shard_common`（Standard / Precision / Perfect）|
|-----------------|---------|---------|---------|--------------------------------------------------|
| `drop_emberwing_vent` | `wing_vent_l1/l2/r1/r2` | NORMAL | `core_ember` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_emberwing_root` | `wing_root_l/r` | ARMORED | `core_ember` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |
| `drop_emberwing_core` | `heart_core` | BOSS_CORE | `core_ember` × 1（Perfect = × 2）| × 2 / × 3 / × 4 |

*EMBERWING 屬餘燼系（`kaiju_ember`）巨獸，所有部位均掉落 `core_ember`（`material-economy.md` C.1 巨獸主題映射規則）。碎片計算：`floor(shard_base × quality_mult)`；`shard_base` = 2，Standard = 1.0×，Precision = 1.5×（floor=3），Perfect = 2.0×（= 4），依 `material-economy.md` D.1。*

### 一場狩獵預期產出（D1–D4 恆定，Precision 品質為主，全 7 部位）

| 通關策略 | `core_ember` | `shard_common` |
|---------|-------------|-----------------|
| **全破壞**（7 部位全破）| 7 | 21 + 5（完成度獎勵）= **26** |
| **僅破熾心**（Speed Run，跳過雙翼）| 1 | 3 |
| **雙內燼孔 + 熾心**（跳過翼根與翼尖）| 3 | 9 |
| **雙翼根 + 雙內燼孔 + 熾心**（不追外燼孔）| 5 | 15 |

### `essence_kaiju` 產出

`on_hunt_end(is_all_parts_broken = true)` 時觸發（7 個部位均 `BROKEN`）→ `essence_kaiju` × 1 + `shard_completeness_bonus`（= 5 碎片）。任何難度階均適用，無難度門檻（〔難度是門，不是牆〕支柱）。

### `core_ember` 的跨遊戲戰略意義

依 `material-economy.md` C.1 映射規則（具體升級對象待該文件為 5 個新主題核心補完映射表時確認），EMBERWING 是 8 頭目陣容中**唯一**掉落 `core_ember` 的巨獸——EMBERWING 的雙翼閘門設計（翼根鎖外燼孔）意味著玩家若只想拿到基礎 `core_ember` 數量，可用「速攻熾心」策略跳過雙翼；但若要拿滿 7 部位的完整產出與 `essence_kaiju`，必須完整解開兩側翼根閘門——這是「農刷目標決定打法深度」的直接體現。

---

## 10. 驗收標準 (Acceptance Criteria)

### AC-01 寬幅教學循環感知（體驗性 — 阻斷）

- [ ] 5 人新手用戶測試：受測者首次置中蓄力釋放 L3 並看到全翼 7 部位同時進入 STAGGERED 後，能在不提示下描述「一砲同時影響了好幾個部位/整片翼」或等效概念，達成率 ≥ 70%
- [ ] 首次全翼同時 STAGGERED 事件發生於戰鬥開始後 30–60 秒內（記錄時間戳）
- [ ] 受測者能在 5 分鐘內自行發現「翼根閘門鎖外燼孔」的關係（或透過破壞翼根後外燼孔可破而注意到）

### AC-02 翼根護甲與跨部位破壞閘門正確性（功能性 — 阻斷）

- [ ] `wing_root_l/r` 標準 ARMORED 護甲行為（軟化貫穿路徑 + L3 快速路徑）與 `kaiju-part-system.md` C.4 完全一致，無例外
- [ ] `wing_vent_l2` / `wing_vent_r2` 在對應 `wing_root` `break_state == ALIVE` 期間，任何 `on_missile_hit` 的 `M_state_mult` 恆為 0，**即使自身 `heat_state == SOFTENED` 亦然**（「軟化貫穿」旁路對此鎖不生效）
- [ ] 對應 `wing_root` `break_state → BROKEN` 後，同幀起 `wing_vent_l2` / `wing_vent_r2` 的 `M_state_mult` 查表回歸標準 NORMAL 規則（0.35 / 1.0 / 1.5）
- [ ] `wing_vent_l1` / `wing_vent_r1`（無鎖燼孔）從戰鬥開始起即可依標準規則被破壞，不受任何閘門影響
- [ ] 自動化測試：`tests/unit/kaiju/emberwing_wing_root_break_gate_test`（覆蓋鎖定/解鎖前後的完整命中序列）

### AC-03 BOSS_CORE 勝利條件（功能性 — 阻斷）

- [ ] `heart_core` `B_current ≥ 200 BU` → `BROKEN` → `on_boss_core_break` 發出 → 勝利結算啟動
- [ ] 即使 6 個可選部位（4 燼孔 + 2 翼根）均 `ALIVE`，核心破壞仍觸發勝利
- [ ] 勝利結算前 `on_part_break`（BOSS_CORE）必先於 `on_boss_core_break` 發出

### AC-04 素材掉落正確性（功能性 — 阻斷）

- [ ] `drop_emberwing_vent` × 4、`drop_emberwing_root` × 2、`drop_emberwing_core` × 1：各掉 `core_ember` + `shard_common`（依品質乘數）
- [ ] Perfect 品質（`SOFTENED_STAGGERED` 破壞）：核心數量 = 2
- [ ] 全破壞結算：`essence_kaiju` × 1 + `shard_completeness_bonus`（= 5）
- [ ] 自動化測試：`tests/unit/economy/material_yield_quality_test` 擴充 EMBERWING 3 組掉落表 × 3 品質等級案例

### AC-05 彈幕可讀性（體驗性 — UX 阻斷）

- [ ] 5 人用戶測試：受測者辨識「敵彈 vs 安全間隙」準確率 ≥ 80%
- [ ] SOFTENED 部位 `#FF6600` 脈動在 D4 最高密度下仍可辨識（彈幕遮蓋時間 ≤ 50%）
- [ ] 殘焰拖尾（靜止危險區）與螺旋彈（移動判定點）的視覺區分：受測者正確分類「這是會動的彈 / 這是留在原地的火」準確率 ≥ 85%
- [ ] `ARMOR_STRIPPED` 弱點露出在 D4 密度下清晰可見，倒計時像素條不被彈幕完全遮蓋

### AC-06 翼展對齊與 L3 寬幅波多部位命中驗證（功能性 — 阻斷 Vertical Slice）

- [ ] 場景佈局確認：7 個部位的 x 座標分佈於畫面 8%–92% 範圍內，且**縱向（y）差異在 L3 判定寬度容許誤差內**（由關卡設計師於 Boss 場景設定評審時確認並記錄）
- [ ] EMBERWING 全程無整體水平漂移（僅垂直呼吸 + 局部振翅），確保寬幅波對齊不受巨獸自身位移干擾
- [ ] 自動化測試：L3 蓄力寬幅波置中釋放時，對 EMBERWING 的理論同時命中部位數 ≥ 5（含至少 1 個翼根）；對非寬幅佈局 Boss（如 CARAPEX）同一波形理論命中部位數 ≤ 2——量化驗證本 Boss 對 L3 的幾何優勢。測試路徑：`tests/unit/weapon/l3_wide_wave_advantage_emberwing_test`
- [ ] 玩家感知測試：5 人 Playtest 後，受測者自發描述「一次打中很多個」或等效概念的比例 ≥ 60%

### AC-07 階段轉換正確性（功能性）

- [ ] 任一側 `wing_root` `BROKEN` → 該側對應外燼孔閘門解除、該側兩燼孔螺旋方向反轉、存活翼根礫壁射頻 × `emberwing_phase2_root_speed_mult`（1.15）
- [ ] 雙側 `wing_root` 均 `BROKEN` → 模式 C 切換至 Phase 3 扇形爆發、殘焰生成間隔 ÷ `emberwing_phase3_residual_density_mult`（1.3）、模式 B 完全停止
- [ ] `emberwing_phase2_root_speed_mult` 與 `emberwing_phase3_residual_density_mult` 存於外部旋鈕，不硬編碼
- [ ] 階段轉換為單向不可逆

### AC-08 難度密度縮放正確性（功能性）

- [ ] 子彈速度（模式 A：110 px/s；模式 B：100 px/s；模式 C：95 px/s）在 D1–D4 下恆定
- [ ] 各模式彈數/射頻/殘焰生成間隔依第 8 節表格縮放；靜態審核各難度 `spawn_config` 參數
- [ ] 部位 `H_max` / `B_max` / `stagger_duration` / 翼展座標（`x_pct`）在難度切換後讀取值不變

### AC-09 L3 × M2 展示 Loadout 全場聚合破壞速率驗算（功能性）

- [ ] L3 × M2，D1：從開戰到雙翼根 + 全部 4 燼孔均 `BROKEN` 的實測 TTB，優於同 loadout 下「窄幅路徑」（L2 單點依序清除同集合部位，含橫移時間）至少 30%
- [ ] 包含在 loadout TTB 矩陣自動化測試中，EMBERWING 專屬案例：`tests/unit/weapon/weapon_loadout_matrix_test`（新增 `emberwing` 情境）

---

*文件版本：1.0.0*
*作者：Game Designer Agent*
*最後更新：2026-07-08*
*資料定義：`assets/data/kaiju/emberwing.yaml`（inline 見第 4.4 節）*
*依賴掉落表 ID：`drop_emberwing_core` / `drop_emberwing_vent` / `drop_emberwing_root`（由 material-economy 實作）*
*新增跨系統規則備查：翼根破壞閘門（`break_gate`，見 4.2）為本文件引入的新 PartDef 概念，需與 `kaiju-part-system.md` 維護者對齊 schema（不與既有 `FireGate` 混用）*
