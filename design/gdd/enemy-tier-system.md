# 敵人分級系統 (Enemy Tier System) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/enemy-tier-system.md*
*最後更新: 2026-07-03*
*狀態: Draft*
*來源: design/feedback/2026-07-02-改進意見與劇情草案.md §A.7*
*相依概念文件: game-concept.md | kaiju-part-system.md | difficulty-system.md | stage-system.md*
*相依程式檔: Assets/_Project/Scripts/Content/EnemyDef.cs | KaijuDef.cs | DifficultyConfig.cs | Assets/_Project/Scripts/Core/Types/PartType.cs*

---

## A. 概覽 (Overview)

敵人分級系統（Enemy Tier System）定義殲獸戰機四個敵人等級——**雜魚（Trash）、菁英（Elite）、中型（Mid）、BOSS（頭目）**——各自的血量模型、護甲/機制承載量、以及與現有系統的資料模型對應。本系統的核心命題來自設計回饋 §A.7：**「有機制的不該只有 BOSS」**。雜魚與菁英不再只是血量與彈幕密度的數值差異，而是可以攜帶一個輕量、資料驅動的**小機制**（Gate 機制）；中型敵人則直接**重用**既有的可破壞部位系統（`kaiju-part-system.md`），以 1–2 個部位規模呈現「小型頭目」戰鬥；BOSS 維持既有的 2–8 部位多階段設計不變。

本系統是**分類與資料模型定義層**，不是新的戰鬥狀態機——雜魚/菁英的 Gate 機制是一個新的輕量狀態機（本文定義），中型/BOSS 則完全繼承 `kaiju-part-system.md` 既有的 HU/BU 雙軌狀態機，不重新定義。

**與難度系統正交（Orthogonal to Difficulty）**：敵人 Tier 決定的所有數值（血量、Gate 血量、部位數）在難度 D1–D4 下**恆定不縮放**，與 `kaiju-part-system.md` C.8、`difficulty-system.md` C.3 的難度不縮放規則精神一致。難度系統只縮放**子彈密度**與**敵人生成數量**（`difficulty-system.md` C.2）；Tier 系統只縮放**血量／護甲／機制複雜度**。兩個縮放軸線完全獨立，交叉組合產生最終戰場壓力（見 D.4）。

---

## B. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：挑戰梯度（Challenge Gradient）＋ 辨識感（Recognition）＋ 精通（Competence）

**「每一種敵人都在教我一件事」** —— 雜魚教動作與彈幕基礎閱讀；菁英教「這隻不一樣，我需要多看一眼」；中型教「這是縮小版的拆解教學」；BOSS 才是真正的拆解殿堂。四個等級形成一條**技能難度階梯**，玩家在雜魚身上養成的閃避直覺、在菁英身上養成的「找弱點」直覺，最終都在 BOSS 戰派上用場——這服務 `game-concept.md` Pillar 2「頭目是靈魂」，讓 BOSS 戰的核心技巧（軟化→剝甲→破壞）不是憑空出現的新規則，而是全程漸進累積的技能。

**「這隻雜魚有點麻煩」** —— 菁英怪不再只是「更肉更密的雜魚」。當玩家看到菁英光環，除了心理準備「這隻要多打幾發」，還要準備「這隻可能要換個角度打」或「先破個什麼東西」。這制造出雜魚戰鬥中罕見的**微決策**：直接優先清除，還是先繞後方打破護盾？直接服務「以智取勝」的核心幻想（呼應 `kaiju-part-system.md` 對護甲機制的定位）。

**「破部位不是 BOSS 專屬特權」** —— 中型敵人讓玩家在關卡中途就能體驗「破部位→弱點外露→擊殺」的完整微循環，而不必等到頭目戰。這降低了 BOSS 戰的學習斷層，也讓「破壞即獎勵」（Pillar 4）貫穿整個關卡節奏，而非集中在關卡尾端才爆發。

---

## C. 詳細規則 (Detailed Rules)

### C.1 四階敵人分級 (The Four Enemy Tiers)

| Tier | 中文 | 英文代碼 | 資料模型 | 典型場景 |
|------|------|---------|---------|---------|
| **Trash** | 雜魚 | `EnemyTier.Trash` | `EnemyDef`（既有，擴充）| 一般波次填充敵人 |
| **Elite** | 菁英 | `EnemyTier.Elite` | `EnemyDef`（既有，擴充）| 波次中 1 隻強化變體，必掉 Cycling Pod |
| **Mid** | 中型 | `EnemyTier.Mid` | `KaijuDef`（既有，擴充）| 關卡中段的小型頭目遭遇（1–2 部位）|
| **Boss** | BOSS / 頭目 | `EnemyTier.Boss` | `KaijuDef`（既有，不變）| 關卡尾端主力頭目戰（2–8 部位，多階段）|

**核心設計決策**：Tier 不是一個獨立的新戰鬥系統，而是**兩個既有資料模型的分類軸**：

- **Trash / Elite** 共用 `EnemyDef` + 新增的輕量 **Gate 機制**（C.3），不使用 `kaiju-part-system.md` 的 HU/BU 雙軌狀態機——雜魚/菁英存活時間以秒計，雙軌蓄熱機制的節奏（`kaiju-part-system.md` D.4 TTB 目標 15–80s）對它們而言過重。
- **Mid / Boss** 共用 `KaijuDef` + `PartDef[]`，**完全重用** `kaiju-part-system.md` C.2–C.8、D.1–D.6 既有的部位狀態機、軟化/破甲/震盪硬直公式，不重新定義。Mid 與 Boss 的唯一資料差異是部位數規模與「破壞是否觸發全域勝利」（見 C.4）。

新增列舉型別 `EnemyTier`（建議路徑 `Assets/_Project/Scripts/Core/Types/EnemyTier.cs`，比照既有 `PartType.cs`）：

```
public enum EnemyTier
{
    Trash = 0,
    Elite = 1,
    Mid   = 2,
    Boss  = 3
}
```

`EnemyDef.Tier` 欄位僅允許 `Trash` / `Elite`；`KaijuDef.Tier` 欄位僅允許 `Mid` / `Boss`（見 C.5 資料驗證）。

---

### C.2 Tier 血量/護甲/機制複雜度總表 (Tier Stat & Mechanic Summary)

| Tier | 血量模型 | 「護甲」對應概念 | 機制承載量（設計指引）| 部位數 |
|------|---------|-----------------|----------------------|--------|
| Trash | 純量 HP（`HP_base[hp_tier]`，既有 `HpTier` T1/T2）| 選用：0–1 個 Gate（C.3）| 0–1 個小機制（多數雜魚無 Gate，行為多樣性來自既有的 移動/彈幕 SO 組合，見回饋 §A.1）| 0（不用部位系統）|
| Elite | 純量 HP（`HP_base[hp_tier] × EliteHpMult`，既有）| **建議必配**：1 個 Gate（C.3）| 1 個小機制（Gate）＋ 既有彈幕密度加成（`EliteDensityMult`）| 0（不用部位系統）|
| Mid | 部位池總和（`Σ H_max_i + B_max_i`，重用 `kaiju-part-system.md` C.3）| 0–1 個 `ARMORED` 部位 | 1–2 個機制（部位軟化/破甲 ＋ 選用的破壞後反應鉤子，見 E.2）| 1–2（設計指引，非強制）|
| Boss | 部位池總和（同上）| ≥1 個 `ARMORED` 部位（既有 `kaiju-part-system.md` F.5 約束）| 多部位分階（既有部位相鄰/連鎖機制，`kaiju-part-system.md` C.6, D.5–D.6）| 2–8（既有 `kaiju-part-system.md` A 定義，典型 2–5，後期至多 8）|

**部位數不是 Mid / Boss 的權威判別依據**——見 C.4。

---

### C.3 Gate 機制 (Gate Mechanic) — Trash / Elite 專用小機制

Gate 是雜魚/菁英專用的**輕量、單一狀態的護甲層**，概念上對應 `kaiju-part-system.md` 的 `ARMORED` 部位（「弱點被遮蔽，需要特定手段才能開門」），但實作上刻意簡化為**單一純量血量池**，不使用 HU/BU 雙軌與軟化狀態，以符合雜魚/菁英數秒級的存活節奏。

#### C.3.1 Gate 型別 (Gate Types)

| GateType | 行為 | 既有先例 |
|----------|------|---------|
| `None` | 無 Gate；敵人全程可直接命中弱點/本體 | 目前多數雜魚（`ram_grub`、`tri_shot` 等）|
| `DirectionalShield`（方向型護盾）| 正面 `GateBlockAngleDeg` 角度錐內命中無效（0 傷害），需從角度錐外命中才能消耗 `GateHp` | **正式化既有 `shield_flier` 行為**（`stage-system.md` E.2.5：正面護盾吸收 3 次命中）|
| `ScalarGate`（純量護甲池）| 全方向命中皆消耗 `GateHp`，無方向限制 | **新機制**，用於菁英「先破護甲才露弱點」（回饋 §A.7 範例）|

#### C.3.2 Gate 狀態機

```
GateState: SEALED → EXPOSED（單向終態，不可逆，比照 kaiju-part-system.md BROKEN 的不可逆精神）

SEALED:
  收到命中事件 hit_damage：
    if GateType == DirectionalShield and hit_angle 落於正面 ±(GateBlockAngleDeg/2) 錐內:
        gate_damage_applied = 0   （护盾偏轉，命中對本體也無效——見 E.7）
    else:
        gate_damage_applied = hit_damage
        GateHp_current = clamp(GateHp_current - gate_damage_applied, 0, GateHp_max)

  if GateHp_current <= 0:
      GateState = EXPOSED
      emit on_gate_broken(enemy_id, post_gate_effect)

EXPOSED:
  所有後續命中直接作用於本體 HP；若 PostGateEffect == ExposeWeakPoint，
  後續命中傷害 × WeakPointVulnMult（見 D.6）
```

**與 `kaiju-part-system.md` 的類比對照表**（供跨系統理解，非共用程式碼）：

| kaiju-part-system 概念 | Gate 機制對應 |
|------------------------|--------------|
| `ARMOR_INTACT` / `ARMOR_STRIPPED` | `SEALED` / `EXPOSED` |
| L3 蓄力震波唯一剝甲路徑 | 任意武器命中皆可消耗 `GateHp`（雜魚/菁英不要求特定武器序列，維持節奏輕量）|
| `stagger_break_mult`（剝甲後效率加成）| `WeakPointVulnMult`（剝離後傷害加成）|
| 護甲不可再生 | `GateState` 單向不可逆（同精神）|

---

### C.4 Mid / Boss 判別的權威規則 (Mid vs Boss — The Authoritative Distinction)

**部位數只是設計指引，不是判別依據。** 真正決定一隻 `KaijuDef` 是 Mid 還是 Boss 的規則是：

> **該敵人的部位破壞是否連結到 `on_boss_core_break`（全域勝利事件，`kaiju-part-system.md` C.5）。**

- **Boss**：至少一個部位 `PartType == BossCore`，其破壞觸發既有的 `on_boss_core_break` → 遊戲狀態系統啟動**整輪勝利**結算（`kaiju-part-system.md` F.4、E.6）。
- **Mid**：**不使用 `PartType.BossCore`**。中型敵人的所有部位為 `Normal` 或 `Armored`；「這場中型遭遇是否清除」由 **Stage System** 監聽該中型敵人所有部位的 `on_part_break` 事件、在全部存活部位皆 `BROKEN` 時判定「遭遇清除（Encounter Cleared）」，觸發**局部**效果（例如移除場地阻擋、開啟下個波次、播放中型敵人專屬擊殺特效）——**不觸碰** `on_boss_core_break`，不影響整輪勝負。

**設計理由**：`on_boss_core_break` 目前是全專案唯一的「整輪勝利」訊號（`kaiju-part-system.md` F.4）。若中型敵人重用 `PartType.BossCore`，會意外讓「破一個中型小怪的部位」提前結束整輪遊戲——這是嚴重的功能性 bug 風險。因此本設計刻意**不擴充 `PartType` 列舉**，改由 Stage System 在消費層自行定義「中型遭遇清除」條件，完全不觸碰 `kaiju-part-system.md` 既有的權威事件契約。

> 若後續設計認為中型敵人也需要一個「核心部位」語意（例如美術/UI 需要標示哪個部位是「打這個就結束遭遇」），建議的路徑是新增 `PartType.MidCore`（平行於 `BossCore`，但發出新事件 `on_mid_core_break` 而非 `on_boss_core_break`）。這是**跨系統變更**，需要 `kaiju-part-system.md` owner（本 Agent）與技術總監審閱後才能定案——列為本文件 I.1 待確認事項，不在本版本中預先實作。

---

### C.5 資料模型與驗證規則 (Data Model & Validation)

#### `EnemyDef`（既有，擴充）

| 欄位 | 狀態 | 允許值 | 驗證規則 |
|------|------|--------|---------|
| `Tier` | **新增** | `Trash` \| `Elite`（禁止 `Mid`/`Boss`）| OnValidate 報錯：`Tier` 不得為 `Mid`/`Boss` |
| `HpTier`、`ContactDamage`、`PointValue` | 既有 | 不變 | 不變 |
| `IsElite`、`EliteHpMult`、`EliteDensityMult`、`EliteShardBonus`、`EliteAuraColor` | 既有 | 不變 | 不變；建議：`IsElite == true` 時 `Tier` 應同步設為 `Elite`（OnValidate 警告，非阻斷，容忍過渡期資料）|
| `MovementPattern`、`EmitterPattern` | 既有 | 不變 | 不變 |
| `MechanicPattern` | **新增** | `MechanicPatternSO` 或 `null` | 可為 null（多數雜魚無機制）；`Tier == Elite` 且為 null 時 OnValidate **警告**（非阻斷）：「菁英建議至少配置一個機制，見 enemy-tier-system.md §C.2」|

#### `MechanicPatternSO`（全新 ScriptableObject，比照 `MovementPatternSO`/`EmitterPatternSO` 架構）

| 欄位 | 型別 | 預設值 | 安全範圍 | 說明 |
|------|------|--------|----------|------|
| `MechanicId` | string | — | — | 資產識別字串，供多個敵人共用同一機制資產 |
| `GateType` | enum | `None` | `{None, DirectionalShield, ScalarGate}` | 見 C.3.1 |
| `GateHp` | float | 20 | [5, 80] | Gate 血量池（純量 HP 單位，見 D.2）|
| `GateBlockAngleDeg` | float | 60° | [30°, 90°] | 僅 `DirectionalShield` 使用；正面阻擋錐角度（見 D.3）|
| `PostGateEffect` | enum | `None` | `{None, ExposeWeakPoint, SpeedDebuff}` | Gate 破除後效果 |
| `WeakPointVulnMult` | float | 1.5 | [1.2, 2.5] | 僅 `PostGateEffect == ExposeWeakPoint` 使用（見 D.6）|
| `SpeedDebuffPct` | float | 0.4 | [0.2, 0.6] | 僅 `PostGateEffect == SpeedDebuff` 使用；移動速度降低比例 |

#### `KaijuDef`（既有，擴充）

| 欄位 | 狀態 | 允許值 | 驗證規則 |
|------|------|--------|---------|
| `Tier` | **新增** | `Mid` \| `Boss`（禁止 `Trash`/`Elite`）| 預設 `Boss`（保持既有 3 個骨架資產 Carapex/Lacera/Voltwyrm 行為不變）|
| `Parts[]` | 既有，驗證規則調整 | — | **既有規則調整**：`hasBossCore` 檢查僅在 `Tier == Boss` 時強制；`Tier == Mid` 時 `Parts[]` 不得含 `PartType.BossCore`（違反 C.4 判別規則，OnValidate 報錯）|
| `KaijuId` | 既有 | 不變 | 不變 |

#### `EnemyTierBalanceConfig`（全新全域旋鈕 SO，比照 `DifficultyConfig` 陣列索引模式）

供 Trash/Elite 的 `HP_base[hp_tier]` 提供單一權威來源，避免每個 `EnemyDef` 各自寫死 HP 數字：

| 欄位 | 型別 | 預設值 | 安全範圍 | 說明 |
|------|------|--------|----------|------|
| `HpBaseByTier` | float[2]（index 0=T1, 1=T2）| `{30, 70}` | T1: [15,60] / T2: [40,120] | 見 D.1；路徑 `assets/data/balance/enemy-tier-balance-config.yaml` |

---

### C.6 範例敵人 (Example Enemies per Tier)

以下範例用於將本文件的抽象規則具體化，供內容制作與驗收（H.7）參照。除已標「既有」者外，均為本文件新提案，尚未建立實際 SO 資產。

| Tier | 敵人 | 資料模型 | 機制 | 備注 |
|------|------|---------|------|------|
| Trash | `ram_grub`（衝角蟲）| `EnemyDef`，`GateType = None` | 無 | 既有（`stage-system.md` E.2.1）；純移動威脅，教學用 |
| Trash | `shield_flier`（護衛飛行器）| `EnemyDef`，`GateType = DirectionalShield` | 正面護盾 3 次命中量（`GateHp` 換算，見 C.3.1）| 既有行為，本文件正式納入 Gate 分類架構 |
| Trash | `ring_burst`（環爆子）| `EnemyDef`，`GateType = None` | 死亡 8 方向爆彈（既有行為，非 Gate 機制）| 既有；示範「機制」不侷限於 Gate，行為多樣性本身即是雜魚層級的機制表現（回饋 §A.1）|
| Elite | `tri_shot_elite`（三叉砲艦・菁英）| `EnemyDef`，`GateType = ScalarGate` | `GateHp = 25`，`PostGateEffect = ExposeWeakPoint`，`WeakPointVulnMult = 1.5` | **新提案**；示範回饋 §A.7 原文「需先破部位才露出弱點」的菁英版本 |
| Elite | `shield_flier_elite`（護衛飛行器・菁英）| `EnemyDef`，`GateType = DirectionalShield` | `GateHp` 提高（既有 `EliteHpMult` 疊加）＋ 既有 `EliteDensityMult` 彈幕加成 | 既有變體＋本文件的 Gate 分類回溯套用 |
| Elite | `kamikaze_elite`（自爆衝鋒・菁英）| `EnemyDef`，`GateType = None` | 既有 AoE 範圍 ×1.5（`stage-system.md` E.2.9）| 示範 E.1：菁英不強制配置 Gate，密度/範圍加成本身即是機制差異化 |
| Mid | 「甲殼中型魔物」（Stage 1，回饋 §B 劇情草案「破鉗爪露弱點」）| `KaijuDef`，`Tier = Mid` | 2 部位：`claw`（`Armored`）＋ `body`（`Normal`）| **新提案**；破鉗爪（`Armored`）後 `body` 判定框全開，呼應劇情草案頭目描述 |
| Mid | 「高速掠食型」（Stage 2，回饋 §B「破腿降速」）| `KaijuDef`，`Tier = Mid` | 1 部位：`leg`（`Armored`）| **新提案**；破腿後降速效果依 E.2 待對齊的行為反應鉤子實作，本文件僅定義部位資料 |
| Boss | `kaiju_dragon_alpha`（`kaiju-part-system.md` C.6 範例）| `KaijuDef`，`Tier = Boss` | 4 部位：`head`(`BossCore`) + `neck`(`Normal`) + `left_horn`/`right_horn`(`Armored`)| 既有範例資產，本文件僅補上 `Tier = Boss` 欄位 |
| Boss | 「巨型母體 Kaiju」（Stage 5 最終頭目，回饋 §B「多部位分階破壞」）| `KaijuDef`，`Tier = Boss` | 最多 8 部位（既有 `kaiju-part-system.md` A 上限）| **新提案**；全遊戲機制總集，需最高部位數上限支撐 |

---

## D. 公式 (Formulas)

### D.1 雜魚/菁英有效血量公式 (Trash/Elite Effective HP)

**命名表達式**：

```
HP_eff(enemy) = HP_base[hp_tier] × TierHpMult(tier)

TierHpMult(Trash) = 1.0
TierHpMult(Elite) = EliteHpMult   （既有旋鈕，見 EnemyDef.cs）
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `hp_tier` | enum | `{T1, T2}` | 既有 `HpTier`（EnemyDef.cs）|
| `HP_base[hp_tier]` | float | T1: [15,60] / T2: [40,120] HP | 全域旋鈕（`EnemyTierBalanceConfig.HpBaseByTier`，新增）|
| `tier` | enum | `{Trash, Elite}` | 本敵人的 `EnemyTier`（新增欄位）|
| `TierHpMult(tier)` | float | `{1.0} ∪ [1.5, 4.0]` | Trash 恆為 1.0；Elite 使用既有 `EliteHpMult` 旋鈕 |
| `HP_eff` | float | [15, 480] HP | 本敵人的有效血量上限 |

**輸出範圍**：`HP_eff > 0`；難度不縮放（D1–D4 下數值恆定，見 A 段落聲明）。

**運算範例**（`tri_shot_elite`，`hp_tier = T1`，`HP_base[T1] = 30`，`EliteHpMult = 2.5`）：
```
HP_eff = 30 × 2.5 = 75 HP
```

---

### D.2 Gate 血量削減與外露公式 (Gate Depletion & Exposure — ScalarGate)

**命名表達式**：

```
GateHp(t) = clamp( GateHp(t-1) − gate_damage_applied(t),  0,  GateHp_max )

if GateHp(t) <= 0 and GateState == SEALED:
    GateState ← EXPOSED
    overflow = GateHp(t-1) − gate_damage_applied(t)   （overflow ≤ 0，見 E.4 溢位處理）
    body_damage_carry = -overflow                      （轉入本體傷害，同幀生效）
    emit on_gate_broken(enemy_id, post_gate_effect)
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `GateHp(t-1)` | float | [0, GateHp_max] | 上一幀 Gate 血量 |
| `gate_damage_applied(t)` | float | [0, ∞) | 本次命中對 Gate 造成的傷害；`DirectionalShield` 型別在阻擋錐內恆為 0（見 D.3）|
| `GateHp_max` | float | [5, 80] | `MechanicPatternSO.GateHp`（新增旋鈕）|
| `body_damage_carry` | float | [0, ∞) | Gate 破除當幀，超出 `GateHp_max` 的多餘傷害轉入本體 HP（見 E.4）|

**輸出範圍**：`GateHp(t)` clamp 至 `[0, GateHp_max]`；`GateState` 一旦轉為 `EXPOSED` 為單向終態，不可逆。

**運算範例**（`GateHp_max = 20`，`GateHp_current = 8`，本次命中 `gate_damage_applied = 15`）：
```
GateHp(t) = clamp(8 − 15, 0, 20) = 0  → GateState = EXPOSED
overflow = 8 − 15 = −7
body_damage_carry = 7   （超出的 7 點傷害同幀轉入本體 HP，見 D.6）
```

---

### D.3 方向型護盾命中判定公式 (Directional Shield Hit Evaluation)

**命名表達式**：

```
angle_diff = abs( hit_angle − enemy_forward_angle )   （正規化至 [0°, 180°]）

if GateType == DirectionalShield and GateState == SEALED:
    if angle_diff <= GateBlockAngleDeg / 2:
        gate_damage_applied = 0        （正面阻擋錐內，護盾偏轉）
        body_damage_applied = 0        （命中完全無效，不傷及本體）
    else:
        gate_damage_applied = hit_damage   （錐外命中正常消耗 Gate，套用 D.2）
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `hit_angle` | float | [0°, 360°) | 命中來源相對敵人的世界角度（由碰撞/子彈系統提供，本文件僅定義消費邏輯）|
| `enemy_forward_angle` | float | [0°, 360°) | 敵人朝向角度（既有移動系統提供）|
| `angle_diff` | float | [0°, 180°] | 命中角與朝向角的最小夾角 |
| `GateBlockAngleDeg` | float | [30°, 90°] | `MechanicPatternSO.GateBlockAngleDeg`（新增旋鈕）；預設 60° |

**輸出範圍**：布林式二選一結果（阻擋 / 不阻擋），無中間值。此判定邏輯依賴命中角度資料，實作介面由子彈/碰撞系統提供（見 F 相依）。

**運算範例**（`shield_flier`，`GateBlockAngleDeg = 60°`，敵人朝向 90°（面朝下），命中來源角度 100°）：
```
angle_diff = abs(100 − 90) = 10°
10° <= 30°（60/2）→ 命中落於阻擋錐內 → gate_damage_applied = 0（護盾偏轉）
```

---

### D.4 Tier × 難度子彈密度合成公式 (Tier × Difficulty Bullet Density Composition)

本公式**擴充** `difficulty-system.md` D.2，將既有的 `EliteDensityMult`（Tier 軸）與 `bullet_density_mult[tier難度]`（難度軸）明確合成，確認兩軸完全獨立、相乘不互相覆寫。

**命名表達式**：

```
actual_bullets = ceil( base_bullets × TierDensityMult(enemy_tier) × bullet_density_mult[difficulty_tier] )

TierDensityMult(Trash) = 1.0
TierDensityMult(Elite) = EliteDensityMult   （既有旋鈕）
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `base_bullets` | int | [1, 8] | 敵人設計基礎每次射擊子彈數（既有，`EmitterPatternSO`）|
| `TierDensityMult(enemy_tier)` | float | `{1.0} ∪ [1.2, 2.5]` | Trash 恆為 1.0；Elite 使用既有 `EliteDensityMult` |
| `bullet_density_mult[difficulty_tier]` | float | `{1.00, 1.25, 1.50, 2.00}` | 既有難度旋鈕（`difficulty-system.md` G.1），與 Tier 軸完全獨立 |
| `actual_bullets` | int | [1, 40]（理論上限）| 本次射擊實際發射子彈數 |

**輸出範圍**：`actual_bullets ≥ 1`；理論上限 = `8 × 2.5 × 2.00 = 40`（菁英 × D4 極端組合），**必須**通過可讀性驗收（見 H.4）；設計師應避免此組合出現在單一波次密度峰值之外。

**運算範例**（`tri_shot_elite`，`base_bullets = 3`，`EliteDensityMult = 1.5`，D3 難度 `bullet_density_mult = 1.50`）：
```
actual_bullets = ceil(3 × 1.5 × 1.50) = ceil(6.75) = 7
```

---

### D.5 中型/BOSS 部位預算校準公式 (Mid/Boss Part Budget — Design Tool)

本公式**非遊戲內執行公式**，是設計師用來估算一場中型/BOSS 遭遇「總體量」的校準工具，複用 `kaiju-part-system.md` C.3 的既有部位容量常數，不重新定義任何部位數值。

**命名表達式**：

```
EncounterWeight(kaiju) = Σ_i ( H_max_i + B_max_i )   for all parts i in kaiju.Parts
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `H_max_i` | float | [80, 280] HU | 第 i 個部位的熱量容量（既有，依 `PartType` 決定，`kaiju-part-system.md` C.3）|
| `B_max_i` | float | [80, 280] BU | 第 i 個部位的破甲容量（既有）|
| `EncounterWeight` | float | [160, 2240]（理論範圍）| 遭遇總體量估算值，僅供設計參考，非執行期數值 |

**輸出範圍**：非 clamp 範圍；純加總。設計指引：Mid 遭遇 `EncounterWeight` 建議落在 [160, 700]（對應 1–2 個中量部位）；Boss 遭遇建議落在 [500, 2240]（對應 2–8 部位，含至少 1 個 `BossCore`）。兩者刻意有重疊區間——如 C.4 所述，真正判別依據是事件連結而非數值。

**運算範例**（Mid 敵人「甲殼中型魔物」，1 個 `ARMORED` 鉗爪部位 H_max=150/B_max=150，1 個 `Normal` 身體部位 H_max=100/B_max=100）：
```
EncounterWeight = (150+150) + (100+100) = 500
```

---

### D.6 Gate 破除後傷害套用公式 (Post-Gate Damage Application)

**命名表達式**：

```
if GateState == EXPOSED:
    if PostGateEffect == ExposeWeakPoint:
        body_damage_applied = incoming_damage × WeakPointVulnMult
    else:
        body_damage_applied = incoming_damage   （SpeedDebuff / None 不改變傷害倍率）

HP_current = clamp( HP_current − body_damage_applied,  0,  HP_eff )
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `incoming_damage` | float | (0, ∞) | 本次命中對本體造成的原始傷害（由武器/子彈系統提供）|
| `WeakPointVulnMult` | float | [1.2, 2.5] | `MechanicPatternSO.WeakPointVulnMult`（見 C.5）|
| `body_damage_applied` | float | (0, ∞) | 實際扣除本體 HP 的傷害量 |
| `HP_current` | float | [0, HP_eff] | 本體當前血量（clamp 保護）|

**輸出範圍**：`HP_current` clamp 至 `[0, HP_eff]`；`HP_current <= 0` 觸發既有擊殺結算（掉落、分數、菁英額外碎片等既有邏輯，不在本文件重複定義）。

**運算範例**（Gate 已 EXPOSED，`PostGateEffect = ExposeWeakPoint`，`WeakPointVulnMult = 1.5`，`incoming_damage = 20`，`HP_current = 75`）：
```
body_damage_applied = 20 × 1.5 = 30
HP_current = clamp(75 − 30, 0, 75) = 45
```

---

## E. 邊界情況 (Edge Cases)

### E.1 菁英未配置任何機制 (Elite with No Mechanic)

**情況**：`Tier == Elite` 但 `MechanicPattern == null`（`GateType == None`）。

**處理**：**允許**，非阻斷錯誤——僅在 `OnValidate` 顯示警告，提醒設計師「菁英建議配置機制」（見 C.5）。理由：不應強制每個菁英都有 Gate；部分菁英的「特殊性」可能單純來自血量/密度差異或既有的行為變體（如 `kamikaze_elite` 的 AoE 範圍加成）。此為設計指引，非強制規則。

### E.2 中型敵人部位破壞後的行為反應鉤子 (Mid-Tier Part-Break Behaviour Hooks)

**情況**：劇情草案（回饋 §B）描述「高速掠食型（破腿降速）」——破壞特定部位應觸發移動速度下降等 AI 行為反應。

**處理**：`kaiju-part-system.md` 的 `on_part_break` 事件（C.5）目前只定義 VFX/SFX/素材掉落消費者，**未定義 AI/移動反應消費者**。本文件**不在此新增**該消費者邏輯（超出系統設計師職權範圍，屬於敵人 AI/Stage 行為設計）。建議：中型/BOSS 的 `KaijuDef` 未來可選配「部位破壞行為反應表」（part_id → 效果，如移動速度乘數、攻擊模式切換），由 Stage System 或未來的 Kaiju-AI GDD 訂定契約後訂閱 `on_part_break`。列為 I.2 待確認事項。

### E.3 高密度組合造成可讀性風險 (Tier × Difficulty Density Stacking)

**情況**：D.4 顯示菁英 × D4 的理論子彈數上限可達 `base_bullets × 2.5 × 2.00`，遠高於一般雜魚 × D4（`× 1.0 × 2.00`）。

**處理**：這是**刻意允許但需驗收把關**的組合（見 H.4）。若可讀性測試未達標，優先調降的旋鈕順序：① `bullet_density_mult[D4]`（難度旋鈕，`difficulty-system.md` G.1）② `EliteDensityMult`（既有 Tier 旋鈕）——**不**更改子彈顏色或玩家判定框設計，比照 `difficulty-system.md` H.7 的既有調校原則。

### E.4 Gate 破除當幀的傷害溢位 (Gate-Break Overflow Damage)

**情況**：D.2 中，單次命中傷害超過 `GateHp` 剩餘量（例如剩 8 點 Gate 血量，命中造成 15 點傷害）。

**處理**：多出的傷害**同幀轉入本體 HP**（`body_damage_carry`，見 D.2 運算範例），不浪費、不需要下一幀額外命中才能生效。此設計避免玩家「精準打光 Gate 血量後還要多打一發才能傷本體」的挫敗感，也與 `kaiju-part-system.md` E.8（同幀多次命中）的「不浪費傷害」精神一致。

### E.5 菁英在 Gate 破除前被擊殺 (Elite Killed Before Gate Breaks)

**情況**：玩家高輸出直接把菁英本體 HP 打到 0，但 Gate 尚未進入 `EXPOSED`（例如 `DirectionalShield` 型別玩家全程只打正面）。

**處理**：**合法結果**。Gate 機制是「額外機會」不是「強制關卡」——菁英本體 HP（D.1 `HP_eff`）與 Gate 血量（D.2 `GateHp`）是兩個獨立池；`ScalarGate` 型別下，命中本身**同時**消耗 Gate 與（若 Gate 已 EXPOSED）本體傷害，但 `DirectionalShield` 型別下正面命中被 D.3 完全阻擋（`gate_damage_applied = 0` 且 `body_damage_applied = 0`）——因此 `DirectionalShield` 菁英若玩家全程只打正面，本體傷害恆為 0，菁英不會死；玩家必須繞角度或等待 U 形迴旋轉身（既有 `shield_flier` 設計精神，`stage-system.md` E.2.5）。**`ScalarGate` 型別**才會出現「本體 HP 先於 Gate 破除歸零」的情況（因為每次命中不論 Gate 是否 EXPOSED，都會先扣 Gate、Gate 見底才轉入本體，故本體傷害恆晚於 Gate 全滿時刻）——故此邊界情況實際只適用於 `DirectionalShield` 型別的「玩家從未觸發阻擋角度外命中」情境，答案是**菁英存活直到玩家改變打法**，非 bug。

### E.6 中型/BOSS 不受 `enemy_count_mult` 影響 (Mid/Boss Excluded from Difficulty Enemy-Count Scaling)

**情況**：`difficulty-system.md` D.1 的 `actual_count = ceil(base_count × enemy_count_mult[tier])` 是否適用於 Mid/Boss 生成數？

**處理**：**不適用**。Mid/Boss 為`KaijuDef` 驅動的單一遭遇實體（每場戰鬥固定 1 隻），不透過波次 `base_count` 機制生成，`enemy_count_mult` 只作用於 `EnemyDef`（Trash/Elite）驅動的波次填充敵人。此為 Tier 系統與難度系統維持正交的必要條件（見 A 段落聲明）——中型/BOSS 的「量」永遠是 1，難度只影響它周遭雜魚/菁英波次的密度與數量。

### E.7 方向型護盾阻擋是否波及本體判定框 (Directional Shield and Body Hitbox Overlap)

**情況**：`DirectionalShield` 阻擋錐內的命中，子彈本身是否應該消失（視覺上撞到護盾）還是穿透？

**處理**：本文件只定義**傷害判定**（`gate_damage_applied = 0`，`body_damage_applied = 0`），子彈消滅/彈開等**視覺與碰撞體行為**由子彈系統與美術動畫決定，超出本文件範圍——建議子彈在阻擋錐內命中時消滅並播放「護盾偏轉」火花特效（既有 `shield_flier` 描述：「受正面攻擊時護盾發偏轉火花」，`stage-system.md` E.2.5），維持行為一致性。

### E.8 中型敵人破壞判定的全域勝利隔離測試盲區 (Mid-Tier Isolation from Global Victory — Regression Risk)

**情況**：若未來有工程師誤將某個 Mid 敵人的部位設為 `PartType.BossCore`（例如複製既有 Boss `KaijuDef` 資產作為 Mid 起點時忘記刪除），會導致玩家提前觸發整輪勝利。

**處理**：這是 C.4 判別規則被違反的高風險路徑，**必須**有自動化驗證擋下（見 H.6 驗收標準），而非僅靠設計文件約束。`KaijuDef.OnValidate` 須在 `Tier == Mid` 時，掃描 `Parts[]`，若發現任何 `PartType.BossCore` 立即報錯阻斷（見 C.5 驗證規則）。

---

## F. 系統相依 (Dependencies)

### F.1 可破壞部位系統（`kaiju-part-system.md`）—— 雙向相依（必要）

- **本系統消費**：Mid/Boss 的 `PartDef`、`PartType`、H_max/B_max 常數、`on_part_break` 事件契約——**完全重用，不修改**。
- **本系統明確不變更**：`PartType` 列舉（`Assets/_Project/Scripts/Core/Types/PartType.cs`）、`on_boss_core_break` 事件語意（見 C.4 判別規則）。若未來新增 `PartType.MidCore`，需回頭更新 `kaiju-part-system.md`（見 I.1）。
- **反向相依**：`kaiju-part-system.md` 應在其 F 節（系統相依）補一筆指向本文件，說明 `KaijuDef.Tier` 欄位的存在（Mid vs Boss），確保雙向可追溯（比照 `.claude/rules/design-docs.md`「Dependencies 必須雙向」規則）。

### F.2 武器系統（`weapon-system.md`）—— 傷害輸入來源（必要）

- 雜魚/菁英的 `incoming_damage`（D.6）與 `hit_damage`（D.2/D.3）數值來源於玩家武器輸出，但**不使用** `weapon-system.md` 的 D₀/HU/BU 換算——雜魚/菁英傷害經濟是獨立的純量 HP 系統（見 D.1 附註：`HP_base` 需與武器單發傷害對齊，列為 I.3 待確認事項）。

### F.3 難度系統（`difficulty-system.md`）—— 正交但需合成（必要）

- **本系統消費**：`bullet_density_mult[difficulty_tier]`（D.4 合成公式的其中一項乘數）。
- **難度系統應更新**：D.2「每次射擊子彈數公式」建議加註「Elite 額外乘上 `EliteDensityMult`，完整合成公式見 `enemy-tier-system.md` D.4」，避免兩份文件對同一公式各自表述不同步。
- **明確隔離**：`HP_eff`、`GateHp`、部位數在 D1–D4 下完全恆定，難度系統不得向本系統的任何旋鈕寫入縮放係數（比照 `kaiju-part-system.md` F.6 的隔離精神）。

### F.4 關卡系統（`stage-system.md`）—— 敵人名冊與遭遇流程擁有者（必要）

- **本系統消費**：既有雜魚名冊（E.1 十種雜魚）與菁英變體命名慣例（`_elite` 後綴，E.3）作為 Trash/Elite 範例基礎。
- **Stage System 的責任**：中型遭遇的「清除」判定（C.4）、波次是否因中型/BOSS 出場而暫停一般雜魚生成器、中型/BOSS 專屬進場演出——均屬 Stage System 職責，本文件只定義資料模型與判別規則，不定義流程時序。
- **待對齊**：`stage-system.md` E 節（小怪名單）與 F 節（波次資料）應在確認本文件方向後，新增「中型敵人名單」與「中型遭遇觸發規則」小節。

### F.5 素材經濟系統（`material-economy.md`）—— 掉落接收端

- 既有 `elite_shard_bonus` 機制不變；Mid/Boss 掉落沿用 `kaiju-part-system.md` 既有的 `drop_table_id` 機制（見 F.1），本文件不新增掉落規則。

### F.6 打擊感分級（回饋 §A.6，未來 GDD）—— 弱耦合，供未來對接

- 回饋 §A.6「打擊感依怪物種類分級」與本文件的 Tier 分類**高度相關但職責不同**：本文件定義「這隻怪是什麼」，未來的打擊感 GDD 定義「打中/打死不同 Tier 時螢幕該有什麼反應」。建議未來打擊感 GDD 直接以本文件的 `EnemyTier` 作為分級索引鍵，避免另起一套獨立分級。

---

## G. 調校旋鈕 (Tuning Knobs)

**所有數值存放於外部資料檔，禁止硬編碼（ADR-0003）。**

### G.1 `EnemyTierBalanceConfig` 全域旋鈕（新增，路徑 `assets/data/balance/enemy-tier-balance-config.yaml`）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `HpBaseByTier[T1]` | 30 HP | 15–60 | 曲線 | 雜魚 T1 基礎血量（D.1）|
| `HpBaseByTier[T2]` | 70 HP | 40–120 | 曲線 | 雜魚 T2 基礎血量（D.1）|

### G.2 `EnemyDef` 既有旋鈕（引用，本文件補充安全範圍文件化）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `EliteHpMult` | 2.5 | 1.5–4.0 | 曲線 | 既有欄位；本文件補上安全範圍（原始程式碼僅要求 ≥1.0）|
| `EliteDensityMult` | 1.5 | 1.2–2.5 | 曲線 | 既有欄位；上限對齊 D.4 可讀性驗收（H.4）|
| `EliteShardBonus` | +3 | 1–8 | 手感 | 既有欄位，無範圍變更 |

### G.3 `MechanicPatternSO` 旋鈕（新增）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `GateHp` | 20 | 5–80 | 曲線 | Gate 血量池（D.2）；雜魚建議下段 5–20，菁英建議上段 20–80 |
| `GateBlockAngleDeg` | 60° | 30°–90° | 手感 | 方向型護盾阻擋錐角度（D.3）；越小越考驗玩家繞位精準度 |
| `WeakPointVulnMult` | 1.5 | 1.2–2.5 | 曲線 | Gate 破除後傷害加成（D.6）|
| `SpeedDebuffPct` | 0.4 | 0.2–0.6 | 曲線 | Gate 破除後移動速度降低比例（若 `PostGateEffect == SpeedDebuff`）|

### G.4 Tier 分類規則旋鈕（設計指引，非數值）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `MidTierPartCountGuideline` | 1–2 | 1–3 | 閘門（指引）| 中型敵人建議部位數（C.2）；超出建議範圍需設計師確認是否應歸類為 Boss |
| `BossTierPartCountRange` | 2–8 | 既有（`kaiju-part-system.md` A）| 閘門 | 沿用既有規則，不重新定義 |
| `EliteMechanicCoverageTarget` | 80% | 60–100% | 指引 | 建議至少 80% 的菁英 `EnemyDef` 資產配置非 `None` 的 `MechanicPattern`（見 H.5，advisory 非阻斷）|

**調校安全指引**：
- `GateBlockAngleDeg` 過小（< 30°）會讓 `DirectionalShield` 幾乎形同無護盾（任何角度都能命中），過大（> 90°）則可能讓玩家找不到有效命中角，違反「彈幕永遠讀得懂」的公平性鐵則——變更前須搭配可讀性/可玩性測試。
- `WeakPointVulnMult` 調高前，須確認菁英 TTK（Time-To-Kill）不會因 Gate 破除後爆發過猛而讓機制形同虛設（玩家一瞬間打光血量，Gate 機制的「找角度/先破護甲」決策價值消失）。

---

## H. 驗收標準 (Acceptance Criteria)

### H.1 EnemyTier 資料模型正確性（功能性 — 阻斷）

- [ ] `EnemyDef.Tier` 僅接受 `Trash`/`Elite`；設為 `Mid`/`Boss` 時 `OnValidate` 報錯
- [ ] `KaijuDef.Tier` 僅接受 `Mid`/`Boss`；設為 `Trash`/`Elite` 時 `OnValidate` 報錯
- [ ] `KaijuDef.Tier == Boss` 時，`Parts[]` 必須含至少一個 `BossCore`（沿用既有規則）
- [ ] `KaijuDef.Tier == Mid` 時，`Parts[]` **不得**含任何 `BossCore`（新規則，見 C.4/E.8）
- [ ] 自動化測試：`tests/unit/enemy-tier/tier_data_model_validation_test`

### H.2 Gate 機制狀態機正確性（功能性 — 阻斷）

- [ ] `ScalarGate`：任意角度命中皆消耗 `GateHp`；`GateHp <= 0` 時 `GateState` 轉為 `EXPOSED` 且不可逆
- [ ] `DirectionalShield`：阻擋錐內命中 `gate_damage_applied = 0` 且 `body_damage_applied = 0`；錐外命中正常消耗 `GateHp`
- [ ] Gate 破除當幀溢位傷害正確轉入本體（D.2/E.4）：`body_damage_carry = -(overflow)` 且 overflow ≤ 0
- [ ] `EXPOSED` 後命中若 `PostGateEffect == ExposeWeakPoint`，本體傷害 = `incoming_damage × WeakPointVulnMult`（D.6）
- [ ] 自動化測試：`tests/unit/enemy-tier/gate_mechanic_state_machine_test`

### H.3 Tier 數值難度不縮放（功能性 — 阻斷）

- [ ] 在 D1–D4 下讀取 `HP_base[T1/T2]`、`EliteHpMult`、`GateHp`、Mid/Boss 部位數，斷言四個難度下數值完全相同
- [ ] 自動化測試：`tests/unit/enemy-tier/tier_difficulty_invariance_test`（比照 `kaiju-part-system.md` H.7 既有測試模式）

### H.4 Tier × 難度子彈密度合成正確性（功能性）

- [ ] D.4 合成公式 `actual_bullets = ceil(base_bullets × TierDensityMult × bullet_density_mult[difficulty])` 在 Trash/Elite × D1–D4 的 8 種組合下數值正確（允許 ceil 誤差 ±1）
- [ ] 菁英 × D4 極端組合（`EliteDensityMult` 上限 × `bullet_density_mult[D4]` 上限）下，5 人截圖辨識率 ≥ 70%（沿用 `difficulty-system.md` H.7 的 D4 可讀性標準）
- [ ] 自動化測試：`tests/unit/enemy-tier/tier_difficulty_density_composition_test`

### H.5 菁英機制覆蓋率（體驗性 — Advisory，非阻斷）

- [ ] 統計所有 `Tier == Elite` 的 `EnemyDef` 資產中，`MechanicPattern != null` 的比例 ≥ `EliteMechanicCoverageTarget`（預設 80%）
- [ ] 驗收方法：`production/qa/smoke-[date].md` 靜態資產審核（Config/Data 類型，Advisory gate，比照 `.claude/docs/coding-standards.md` 測試分級表）

### H.6 中型敵人全域勝利隔離（功能性 — 阻斷，回歸測試）

- [ ] `Tier == Mid` 的 `KaijuDef` 破壞任一部位，**不得**觸發 `on_boss_core_break`
- [ ] `Tier == Mid` 的 `KaijuDef` 全部部位 `BROKEN` 後，Stage System 收到「遭遇清除」局部訊號，且遊戲狀態系統**未**進入整輪勝利結算
- [ ] 整合測試：`tests/integration/enemy-tier/mid_tier_victory_isolation_test`（**高優先級**，防止 E.8 描述的回歸風險）

### H.7 範例敵人可用性（Config/Data — Advisory）

- [ ] C.6 列出的每個範例敵人（2–3 個／Tier）皆可在 Unity Editor 建立對應 SO 資產，`OnValidate` 無報錯
- [ ] 驗收方法：`production/qa/smoke-[date].md`

---

## I. 待確認事項 (Open Questions for Director Review)

以下決策已在本文件中採用**建議方案**並據以撰寫規則/公式，但涉及跨系統契約或需要導演/技術總監拍板，正式定案前標記為開放：

1. **`PartType.MidCore` 是否需要新增？** 本文件預設中型敵人**不需要**核心部位語意，「遭遇清除」由 Stage System 監聽全部部位 `BROKEN` 判定（C.4）。若日後美術/UI 需要明確標示「打這個部位就結束遭遇」的核心語意，需新增 `PartType.MidCore` + `on_mid_core_break` 事件，並回頭修改 `kaiju-part-system.md`（跨系統變更，需該文件 owner 與技術總監審閱）。

2. **中型敵人「破部位觸發 AI/移動反應」（如破腿降速）的契約歸屬？** 本文件明確排除此範疇（見 E.2）——`on_part_break` 目前只有 VFX/SFX/掉落消費者。是否新增一份 Kaiju-AI 或 Stage-System 擴充規格來承接「part_id → 行為反應」的資料表，待導演決定是否現在需要，或留到中型敵人內容製作階段再補。

3. **雜魚/菁英 HP 數值單位如何與武器傷害對齊？** `HP_base[T1/T2]`（D.1）目前是獨立於 D₀/HU/BU 經濟之外的純量佔位數字，僅依 `EnemyDef.cs` 既有註解「2–3 / 4–6 次 L1 命中擊殺」的設計意圖推算。實際數值需等 `bullet-system.md` 或 `weapon-system.md` 補上玩家武器對雜魚的單發傷害公式後，才能做最終校準（目前兩份文件均未定義此經濟）——列為與武器系統的待對齊事項。

4. **Mid 與 Boss 的部位數指引區間刻意重疊（1–3 vs 2–8）是否可接受？** 本文件採用「事件連結」而非「部位數」作為權威判別依據（C.4），部位數僅為設計指引。若導演偏好更明確的數值分界（例如強制 Mid ≤ 2、Boss ≥ 3），可直接調整 G.4 的 `MidTierPartCountGuideline` 而不影響判別邏輯本身。

5. **`shield_flier` 既有實作是否要回溯遷移為 `MechanicPatternSO` 資產？** 本文件將 `shield_flier` 的既有硬編碼護盾行為（3 次正面命中）正式化為 `DirectionalShield` 型別的設計先例（C.3.1），但未確認既有 `shield_flier` prefab/程式碼是否要重構遷移到新的 `MechanicPatternSO` 架構，或保留原樣、僅新敵人使用新架構。建議技術總監評估遷移成本後決定。

---

*文件版本：1.0.0*
*作者：Systems Designer Agent*
*關聯 GDD：game-concept.md | kaiju-part-system.md | difficulty-system.md | stage-system.md | material-economy.md*
*來源意見：design/feedback/2026-07-02-改進意見與劇情草案.md §A.7*
