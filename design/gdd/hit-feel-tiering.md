# 打擊感分級 (Hit-Feel Tiering by Enemy Class) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/hit-feel-tiering.md*
*最後更新: 2026-07-03*
*狀態: Draft — 服務改進意見 §A.6（`design/feedback/2026-07-02-改進意見與劇情草案.md`）*
*相依文件: game-feel.md（LOCKED 邏輯基礎，不修改其既有數值）| kaiju-part-system.md | enemy-tier-system.md（權威分級定義來源）| stage-system.md*

---

## A. 概覽 (Overview)

本文件在 `game-feel.md` 既有的打擊感系統之上，新增**敵人分級（Enemy Tier）**這一軸，讓打擊回饋強度隨「打死的東西有多重要」而分級，而不是所有敵人一個樣。這是對導演反饋「所有怪的打擊感一樣，會膩」（改進意見 §A.6）的直接回應，也是 `enemy-tier-system.md`（改進意見 §A.7 的落地規格）在 Game Feel 層的對應文件——**本文件直接沿用該文件定義的 `EnemyTier`（`Trash` / `Elite` / `Mid` / `Boss`）作為分級索引鍵**（`enemy-tier-system.md` F.6 已預先指定此對接方式）。

核心手法是**疊加、不覆寫**：`game-feel.md` C.1 事件目錄與 D 節公式（頓幀/慢動作/震動/閃光）維持原樣、數值不變，本文件只做三件事：

1. 為**雜兵／菁英（`Trash`/`Elite`，沒有可破壞部位、只有純量血量 + 選用 Gate 機制）**補一套全新的、之前完全不存在的「擊殺回饋」規格。
2. 為**中型敵人（`Mid`，1–2 部位的小型頭目，`enemy-tier-system.md` C.2）**的「遭遇清除」時刻，補一個全新的小型高潮回饋——中型敵人的一般部位破壞沿用既有 `on_part_break` 事件不變，但**沒有** `on_boss_core_break` 可用（`enemy-tier-system.md` C.4 明文禁止 Mid 使用 `PartType.BossCore`），因此「這場遭遇結束了」這個時刻目前完全沒有專屬回饋。
3. 明確定義 `Boss`（涵蓋既有三隻巨獸 CARAPEX/LACERA/VOLTWYRM，以及未來的最終頭目）沿用 `game-feel.md` 既有的「Boss 死亡」數值，**完全不變**——這是本分級表的天花板基準，不做任何加成。

---

## B. 玩家幻想 (Player Fantasy)

延續 `game-feel.md` B 節的 Sensation 三層次，本文件補上第四層——**分級感（Weight of the Kill）**：

| 目標分級 | 幻想描述 |
|---------|----------|
| 雜兵（Trash） | 隨手一擊就碎——爽快、不拖泥帶水，密集清版時**不能**因為每隻小怪都頓一下而變得黏滯 |
| 菁英（Elite） | 這隻不一樣——打倒它有一個清楚的「登記」瞬間，配合它掉落武器莢艙的價值感與（可能配置的）Gate 機制「先破護甲才露弱點」的策略張力 |
| 中型（Mid） | 關卡中途的「縮小版拆解教學」——玩家在頭目戰之前就能體驗「破部位→弱點外露→遭遇清除」的完整微循環；遭遇清除那一刻該有自己的小小慶祝感，而不是悄無聲息地結束 |
| BOSS（Boss） | 全場最高潮——沿用既有系統已經是全遊戲數一數二的感官序列，不需要也不應該被稀釋或超越 |

**設計原則——juice 是稀缺資源**：頓幀、慢動作、大震動不能對每個擊殺都給滿，否則玩家會產生「juice 疲勞（juice fatigue）」，最終所有回饋都變得無感。本文件的分級表刻意讓**低頻事件拿高強度回饋、高頻事件拿低強度回饋**——雜兵每波死很多隻，回饋必須輕量；頭目一場只死一次，回饋可以奢侈。中型遭遇介於兩者之間：比雜兵/菁英稀有（每關 0–2 場），但比 Boss 更常見（每關 1 場 Boss vs 可能 0–2 場 Mid 遭遇），因此其「遭遇清除」回饋刻意落在 Elite 與 Boss 死亡之間，形成完整的四階梯度而非三階平台。

---

## C. 詳細規則 (Detailed Rules)

### C.1 四階敵人分級（沿用 enemy-tier-system.md 權威定義）

分級的**權威定義**（哪個敵人 ID／哪隻巨獸屬於哪一階、資料模型、Mid 與 Boss 判別規則）屬於 `enemy-tier-system.md`（C.1–C.5）。本文件只定義**每一階對應的打擊感規格**，直接引用其 `EnemyTier` 列舉：

| Tier | 中文 | 資料模型 | 是否有 `on_boss_core_break` | 打擊感規格章節 |
|------|------|---------|---------------------------|---------------|
| `Trash` | 雜魚 | `EnemyDef`（新增 `Tier`/`MechanicPattern` 欄位）| 否（純量 HP，見 `enemy-tier-system.md` C.2）| C.2、D.4 |
| `Elite` | 菁英 | `EnemyDef`（同上，`IsElite = true`）| 否 | C.2、D.4 |
| `Mid` | 中型 | `KaijuDef`（`Tier = Mid`，1–2 部位，**禁止** `PartType.BossCore`）| **否**（`enemy-tier-system.md` C.4 明文禁止；遭遇清除由 Stage System 監聽全部位 `BROKEN` 局部判定）| C.3.1、D.7 |
| `Boss` | BOSS / 頭目 | `KaijuDef`（`Tier = Boss`，2–8 部位，含 ≥1 `BossCore`）| **是**（既有 `on_boss_core_break`）| C.3.2 |

> **重要澄清（修正本文件初版的錯誤假設）**：`enemy-tier-system.md` C.5 明訂既有三隻巨獸（CARAPEX／LACERA／VOLTWYRM）**預設 `Tier = Boss`**，以保持其既有行為不變——它們**不是**「中型」。`Mid` 階是全新提案的內容類型（1–2 部位小型遭遇，例：`enemy-tier-system.md` C.6 的「甲殼中型魔物」「高速掠食型」），尚未建立實際資產。因此 `Boss` 階的打擊感在本文件中對**現有三隻巨獸與未來最終頭目一視同仁**——這正是 `game-feel.md` 現況（所有巨獸死亡數值相同），本文件對此**不做任何修改**，只補上 `Trash`/`Elite`/`Mid` 三個目前完全空白的分級。

### C.2 雜兵／菁英擊殺回饋（Trash / Elite）——全新規格

雜兵與菁英使用 `enemy-tier-system.md` C.2 定義的純量 HP 模型（不涉及 `kaiju-part-system.md` 的熱量/破甲雙槽），死亡即消滅。目前系統中**完全沒有**與其死亡綁定的 VFX/SFX/震動/頓幀規格（`EnemyDef` 只定義 HP/接觸傷害/點數，不涉及死亡回饋）。本節新增邏輯事件 `on_enemy_killed`，其資料契約如下（**設計層規格；實際 C# struct 由 stage-system 相關 epic 實作**，命名與現有 `PartEvents.cs` 慣例一致）：

```
on_enemy_killed(
    enemy_id:        string,       // EnemyDef.EnemyId
    tier:            EnemyTier,    // Trash | Elite（Mid/Boss 由既有/新增部位事件覆蓋，見 C.3）
    world_position:  Vector2,      // 死亡座標，供爆炸 VFX 生成點
    tint_color:      Color         // Elite 沿用 EnemyDef.EliteAuraColor；Trash 用敵人基礎色
)
```

**觸發時機**：雜兵/菁英血量歸零的同幀（`enemy-tier-system.md` D.6 `HP_current <= 0`），由 Stage 系統發出（對應現有 `on_part_break` 的角色，但服務雜兵/菁英而非巨獸部位）。

**Trash 階回饋**（見 D.4 / G.6 旋鈕）：
- 無頓幀、無螢幕震動、無閃光——高頻事件不占用 juice 預算
- 小型爆炸粒子（6–10 顆），沿用敵人基礎色，壽命短（≈0.25s）
- 短促「啵」聲一次性音效，音高隨機化避免密集清版時聽覺疲勞（見 D.5）

**Elite 階回饋**（見 D.4 / G.6 旋鈕）：
- 輕量頓幀（遠低於部位破壞的 115ms）+ 中等螢幕震動（量級對齊既有「護甲剝除」5px，作為熟悉的中等強度錨點）
- 中型爆炸粒子（14–28 顆）+ 少量碎片，**色調沿用 `EnemyDef.EliteAuraColor`**（延續存活時的琥珀光環視覺語言到死亡瞬間，強化「這隻不一樣」的辨識）
- 中型爆炸音效 + 一個獨立的「菁英擊殺」短鈴聲分層（呼應其掉落循環武器莢艙的價值感）
- 輕微全白閃光（遠低於部位破壞的 0.92）
- **不觸發慢動作**——慢動作是稀缺資源，保留給 `Boss` 核心擊破（見 C.3.2 設計理由）

**與 Gate 機制（`on_gate_broken`）的關係——未涵蓋，列為待決**：`enemy-tier-system.md` C.3.2 定義菁英可配置 Gate（護甲層），破除時發出 `on_gate_broken`，概念上對應巨獸的「護甲剝除（ARMOR_STRIPPED）」（`game-feel.md` C.1 該列已有 5px 震動＋撕裂音效＋弱點框規格）。本文件**尚未**為 `on_gate_broken` 定義對應回饋——是否直接沿用巨獸護甲剝除的既有規格（`shake_mag_armor_strip = 5px`），或需要獨立的雜兵/菁英專屬版本，列為 I 節待決問題 #2。

### C.3 巨獸級回饋（Mid / Boss）

#### C.3.1 Mid 階——遭遇清除回饋（全新）

中型敵人的**一般部位破壞**（`Normal`/`Armored`，`enemy-tier-system.md` C.4 禁止其使用 `BossCore`）沿用 `game-feel.md` C.1「部位破壞（Part Break）」列既有數值，**完全不變**——與 `Boss` 階巨獸的一般部位破壞用同一套規格，維持戰鬥節奏一致（見 D.7 設計理由）。

但中型遭遇**沒有** `on_boss_core_break` 可用來標示「這場戰鬥結束了」——`enemy-tier-system.md` C.4 定義：全部存活部位 `BROKEN` 後，由 Stage System 判定「遭遇清除（Encounter Cleared）」，觸發**局部**效果。本文件為此新增一個邏輯事件 `on_mid_encounter_cleared`（**設計層規格；由 Stage System 在偵測到 Mid `KaijuDef` 全部部位 `BROKEN` 後發出**，且**必須**在最後一個部位自身的 `on_part_break` 回饋序列播放完（至少頓幀結束）後才觸發，避免與該部位自己的爆破序列搶拍，見 E.2）：

```
on_mid_encounter_cleared(
    kaiju_id:        int,
    world_position:  Vector2       // 建議取遭遇中心點或最後破壞部位座標
)
```

**回饋規格**（見 D.7 / G.6 旋鈕）——刻意**不**包含頓幀與慢動作（觸發它的最後一個部位自己的 `on_part_break` 已經給過 115ms 頓幀＋慢動作，緊接著再給一次會造成連續凍結的黏滯感）：
- 額外一層螢幕震動脈衝（介於 Elite 5px 與 Boss 死亡 24px 之間，見 D.7）
- 額外一波爆炸粒子（在最後部位自己的碎片之上，追加更多，象徵「全部清空」的視覺總結）
- 輕度二次全白閃光（在最後部位自己的閃光衰減到一半左右時疊加，形成「兩段式」的收尾節奏，而非重複同一個閃光高峰）
- 專屬短音效 `sfxMidClear`——短促二音上揚，明確短於 Boss 死亡的四音琶音 `sfxWin`，讓中型遭遇有自己的慶祝身份，同時不搶走真正 Boss 戰的風頭

#### C.3.2 Boss 階——沿用 game-feel.md，不修改

`Boss` 階（既有三隻巨獸 CARAPEX/LACERA/VOLTWYRM，以及未來的最終頭目）的一般部位破壞與核心擊破（`on_boss_core_break`）**完全沿用** `game-feel.md` C.1/D.1–D.4/G.1–G.3 既有數值，本文件不做任何修改、不新增任何加成旋鈕。這是整個分級表的天花板——`Trash`/`Elite`/`Mid` 三階的所有新規格，數值上都刻意低於此天花板（見 D 節各公式的範圍註記）。

> **關於「BOSS 再更誇張，可續推」**：改進意見原文暗示未來或許希望最終頭目（劇情草案 §B「巨型母體 Kaiju」）比早期 Boss（CARAPEX 等）感受更強一階。但 `enemy-tier-system.md` 目前的 `EnemyTier` 列舉**沒有**區分「一般 Boss」與「最終頭目」——兩者都歸為同一個 `Boss` 階。本文件**不**單方面在 `Boss` 階內再細分子階級（那需要修改 `enemy-tier-system.md` 的資料模型，超出本文件職權），已列為 I 節待決問題 #1，留待導演與 `enemy-tier-system.md` owner 協調決定是否需要。

### C.4 與可及性（Reduce-Motion）系統的接軌

`game-feel.md` H.1 定義的四個無障礙倍率（`shake_accessibility_mult`／`slowmo_accessibility_mult`／`hitstop_accessibility_mult`／`flash_accessibility_mult`）為**唯一**的無障礙開關，存放於可變設定層（非本文件、非唯讀 `GameFeelConfig` 的固定調校值）。本文件新增的所有分級旋鈕（雜兵/菁英擊殺的頓幀/震動/閃光、中型遭遇清除的震動/閃光）**全部**乘上對應的既有無障礙倍率，不新增獨立的無障礙開關。

**具體效果**：Reduce-Motion 開啟後，`Elite` 雜兵擊殺的震動降至 25%、閃光完全消失、頓幀縮至 50%；`Mid` 遭遇清除的震動脈衝與二次閃光同樣依既有倍率縮放（二次閃光在 Reduce-Motion 下與所有閃光一樣完全停用）；`Trash` 雜兵擊殺本來就無震動/頓幀/閃光，無需額外處理；`Boss` 階不變（沿用 `game-feel.md` H.1 既有行為）。

---

## D. 公式 (Formulas)

### D.4 雜兵擊殺回饋分級表（Trash / Elite，查表式）

雜兵擊殺回饋不是連續公式，而是**每階固定的回饋剖面（tier profile）**，加上既有無障礙倍率：

```
final_value(tier, channel) = tier_profile[tier][channel] × accessibility_mult[channel]
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `tier` | enum | `{Trash, Elite}`（`enemy-tier-system.md` `EnemyTier`） | 敵人分級 |
| `channel` | enum | `{hitstop_ms, shake_px, flash, particle_count}` | 回饋通道 |
| `tier_profile[tier][channel]` | float/int | 見下表 | 每階每通道的固定基準值 |
| `accessibility_mult[channel]` | float | [0, 1]（既有） | 對應 `game-feel.md` G.1/G.3/G.5 的既有無障礙倍率 |

**分級表（G 節旋鈕預設值）**：

| 通道 | Trash | Elite | 對齊錨點 |
|------|-------|-------|---------|
| `hitstop_ms` | 0 | 30 | Elite 遠低於部位破壞 115ms 上限，避免與巨獸事件混淆 |
| `shake_px` | 0 | 5 | 對齊既有「護甲剝除」震幅（5px），玩家已熟悉此強度的意義 |
| `flash` | 0 | 0.15 | 遠低於部位破壞 0.92，避免視覺誤讀為巨獸事件 |
| `particle_count` | 8 | 20 | Trash 為 Elite 的 40%，符合「輕/中」直覺比例 |
| `slow_mo` | 停用 | 停用 | 見 C.2 設計理由——慢動作保留給 `Boss` 核心擊破 |

**輸出範圍**：`shake_px` 受 `game-feel.md` `shake_magnitude_cap`（24px）保護（5px 遠低於此值，無須額外 clamp，但實作仍應套用該全域 clamp 作為防呆）；`hitstop_ms` 遠低於 150ms 非 boss 上限。

**範例**：Elite 雜兵死亡，Reduce-Motion 關閉：`shake_px = 5 × 1.0 = 5px`；Reduce-Motion 開啟（`shake_accessibility_mult = 0.25`）：`5 × 0.25 = 1.25px`。

---

### D.5 SFX 音高隨機化（避免高頻事件聽覺疲勞）

```
playback_pitch = base_pitch × (1 + random(-pitch_variance, +pitch_variance))
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `base_pitch` | float | 1.0（音效原始音高） | 音效素材本身的基準音高倍率 |
| `pitch_variance` | float | [0, 0.25]（見 G 節） | 隨機化幅度；預設 0.10（±10%） |
| `random(-x, x)` | float | 均勻分布 | 每次擊殺獨立取樣 |
| `playback_pitch` | float | [0.75, 1.25]（預設參數下實際落在 [0.90, 1.10]） | 該次播放實際音高倍率 |

**輸出範圍**：由 `pitch_variance` 安全範圍保證不超過 [0.75, 1.25]，避免音高偏移過大聽起來像故障。

**範例**：`pitch_variance = 0.10`，某次擊殺取樣 `random(-0.10, 0.10) = 0.06` → `playback_pitch = 1.0 × 1.06 = 1.06`。

---

### D.6 同幀多重雜兵擊殺預算（Simultaneous Kill-Feel Budget）

```
if simultaneous_kills_this_frame <= kill_feel_simultaneous_budget:
    每隻雜兵套用完整 tier_profile（D.4）
else:
    前 kill_feel_simultaneous_budget 隻套用完整 tier_profile
    其餘（第 budget+1 隻起）套用「輕量剖面」：
        particle_count → floor(tier_profile.particle_count × 0.4)
        hitstop_ms, shake_px, flash → 0（一律不觸發，避免與已觸發的完整剖面衝突/疊加）
        SFX → 靜音（避免同幀 N 個音效疊加造成爆音），改由第一隻的音效代表整批
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `simultaneous_kills_this_frame` | int | 0–∞ | 同一幀觸發 `on_enemy_killed` 的雜兵/菁英數 |
| `kill_feel_simultaneous_budget` | int | [2, 8]（見 G 節，預設 4） | 每幀可享完整回饋剖面的擊殺數上限 |

**輸出範圍**：確保清版瞬間（例如 M4 叢集炸彈一次清掉一整波雜兵）不會產生 N 倍疊加的震動/音效轟炸，同時仍保留視覺上「這裡發生了一大群擊殺」的印象（輕量剖面仍有縮小版粒子）。

**範例**：`kill_feel_simultaneous_budget = 4`，某幀 M4 炸彈同時擊殺 7 隻 Trash 雜兵：前 4 隻各自生成 8 顆粒子＋各自的隨機音高「啵」聲；後 3 隻只各生成 `floor(8 × 0.4) = 3` 顆粒子，無額外音效。

---

### D.7 中型遭遇清除回饋（Mid Encounter Cleared，查表式）

`on_mid_encounter_cleared` 觸發時，額外疊加以下固定剖面（**在**觸發它的最後部位自身 `on_part_break` 序列的頓幀結束之後才生效，見 C.3.1 / E.2）：

```
mid_clear_shake_final = min(mid_clear_shake_mag, shake_magnitude_cap) × shake_accessibility_mult
mid_clear_flash_final = min(mid_clear_flash, flash_max_alpha) × flash_accessibility_mult
mid_clear_particle_final = mid_clear_particle_bonus
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `mid_clear_shake_mag` | float | [8, 18] px（見 G 節，預設 15px） | 介於 Elite（5px）與 Boss 死亡（24px）之間，經 `game-feel.md` 既有 `shake_magnitude_cap` 保護（取最小值防呆） |
| `shake_magnitude_cap` | float | 24px（既有，`game-feel.md` G.1） | 沿用既有硬性護欄，不放寬 |
| `mid_clear_flash` | float | [0.2, 0.6]（見 G 節，預設 0.4） | 與最後部位自己的 0.92 閃光形成「兩段式」節奏，而非同時疊加到單一更高峰值 |
| `flash_max_alpha` | float | 0.85（既有，`game-feel.md` G.5） | 沿用既有上限 |
| `mid_clear_particle_bonus` | int | [10, 30]（見 G 節，預設 18） | 追加於最後部位自身碎片之上，象徵「全部清空」的視覺總結 |
| `shake_accessibility_mult` / `flash_accessibility_mult` | float | [0, 1]（既有） | `game-feel.md` H.1 既有無障礙倍率 |

**輸出範圍**：震動與閃光皆受既有全域護欄 clamp 保護；無頓幀/慢動作分量（見 C.3.1 設計理由），因此不涉及 `hitstop`/`slowmo` 安全範圍。

**範例**：`mid_clear_shake_mag = 15px`，Reduce-Motion 關閉：`min(15, 24) × 1.0 = 15px`（明顯高於 Elite 的 5px，但低於 Boss 死亡的 24px，符合設計目標的中段定位）。

---

## E. 邊界情況 (Edge Cases)

### E.1 雜兵擊殺與巨獸事件同幀

**情況**：玩家的 M3 魚雷同時擊破一個巨獸部位（`on_part_break`）與一隻剛好在爆炸範圍內死亡的 `Trash` 雜兵（`on_enemy_killed`）。

**處理**：兩者是獨立事件，各自的頓幀/震動/閃光**取最大值疊加**（沿用 `game-feel.md` D.1/D.3 的「取最大不相加」模型，本文件不新增獨立的震動/閃光累加軌道）——即使 `Trash` 雜兵理論上貢獻 0，也不影響巨獸事件的 115ms/震幅/閃光。粒子與音效各自獨立播放（不衝突，因為是視覺上分離的兩個爆炸位置）。

### E.2 中型遭遇清除與最後部位破壞的時序衝突

**情況**：中型敵人的最後一個存活部位破壞（`on_part_break`）與該遭遇的 `on_mid_encounter_cleared` 幾乎同時發生——理論上兩者可能被實作成同一幀觸發。

**處理**：**強制排序，不同時觸發完整序列**。`on_mid_encounter_cleared` 的回饋（D.7）**必須**延遲至觸發它的 `on_part_break` 自身的頓幀計時器歸零之後才開始播放（沿用 `game-feel.md` D.4 頓幀先於慢動作/粒子的排序精神）。若強制同幀疊加，最後部位的 115ms 頓幀會與遭遇清除的震動/閃光糊在一起，玩家會把兩個本該分層的訊號感知為同一團模糊的視覺噪音。實作備忘：Stage System 應在收到最後部位的 `on_part_break` 後啟動一個至少 `hitstop_part_break_ms`（115ms，沿用既有值）的延遲計時器，計時結束才發出 `on_mid_encounter_cleared`。

### E.3 Boss 階巨獸的一般部位破壞不受本文件影響

**情況**：CARAPEX（`Boss` 階）的一般部位（大顎，`Normal` 部位類型）破壞，是否套用本文件任何新規則？

**處理**：**不套用**。C.3.2 明訂 `Boss` 階完全沿用 `game-feel.md` 既有數值，本文件對其零修改——這與 `Mid` 階的一般部位破壞（同樣沿用 `game-feel.md` 既有數值，見 C.3.1）行為一致，兩者的差異只在於**遭遇結束時**：`Boss` 有 `on_boss_core_break`（既有全套回饋），`Mid` 有全新的 `on_mid_encounter_cleared`（D.7 較輕量的回饋）。

### E.4 Elite 雜兵死亡與武器莢艙掉落視覺重疊

**情況**：`Elite` 雜兵死亡同時觸發（a）本文件的擊殺爆炸 VFX，與（b）`stage-system.md` §E.3 的循環武器莢艙下降動畫（兩者生成點相同：`world_position`）。

**處理**：本文件的擊殺 VFX（粒子＋閃光＋震動）在死亡當幀播放完畢（壽命 <0.5s）；武器莢艙的下降動畫由 `stage-system.md` D.2「階段 1 — 下降」定義，屬於**下一個視覺階段**，不與擊殺爆炸同時搶畫面。若兩者的具體時序協調（例如莢艙延遲 0.2s 出現，等擊殺爆炸粒子消散）需要更精確定義，留待 stage-system.md 或實作階段補充——本文件只確保擊殺 VFX 本身不無限延遲（受粒子壽命約束）。

### E.5 Mid 遭遇的部位數在指引範圍邊界（1 部位 vs 2 部位）

**情況**：`enemy-tier-system.md` C.6 範例顯示 Mid 遭遇可能只有 1 個部位（「高速掠食型」）或 2 個部位（「甲殼中型魔物」）。單部位的 Mid 遭遇，其「最後部位破壞」與「遭遇清除」是否等於**同一個部位**？

**處理**：是，且不影響 D.7 公式——1 部位的 Mid 遭遇，該部位的 `on_part_break` 觸發後，經 E.2 的延遲規則，緊接著觸發 `on_mid_encounter_cleared`。回饋剖面不因部位數（1 或 2）而改變（D.7 的旋鈕是遭遇層級的固定值，不隨部位數縮放）——這是刻意簡化：`enemy-tier-system.md` D.5「部位預算校準」已確保 Mid 遭遇的 `EncounterWeight` 落在合理區間，額外依部位數再細分回饋強度會增加不必要的旋鈕複雜度，留待實測後若有需要再擴充。

### E.6 密集雜兵擊殺的效能保護

**情況**：D1 難度早期波段，畫面上可能同時有 10+ 隻 `Trash` 雜兵在極短時間內（非同一幀，但相鄰數幀內）依序死亡（例如玩家的 L2 集束雷射掃過一整排 `ram_grub`）。

**處理**：D.6 的「同幀預算」只處理**同一幀**內的多重擊殺；跨數幀但密集發生的連續擊殺不受該公式限制（每幀各自的擊殺數通常遠低於 `kill_feel_simultaneous_budget`），維持各自完整的 `Trash` 剖面（本來就很輕量：0 頓幀/0 震動，只有粒子與音效，不會造成效能或可讀性問題）。若實測發現連續密集擊殺仍有音效堆疊問題，可調降 G.6 節的 `mob_kill_sfx_max_per_frame`（沿用 `game-feel.md` G.4 `softened_sfx_max_per_frame` 的既有防擁擠模式，新增同名機制於雜兵擊殺音效）。

---

## F. 系統相依 (Dependencies)

| 系統 | 相依類型 | 本文件消費的事件/資料 | 本文件向下游提供的效果 |
|------|----------|----------------------|----------------------|
| **game-feel.md（LOCKED 邏輯基礎）** | 基礎（必要，本文件疊加、不修改其數值） | C.1 事件目錄、D.1–D.4 公式、G.1–G.5 旋鈕（含全部既有無障礙倍率）、C.6 可讀性護欄 | 無修改；`Boss` 階（C.3.2）完全沿用其既有數值 |
| **enemy-tier-system.md（權威分級定義來源）** | **資料來源（必要）** | `EnemyTier` 列舉（`Trash`/`Elite`/`Mid`/`Boss`）、C.4 Mid/Boss 判別權威規則、C.6 範例敵人 | 本文件是其 F.6「弱耦合，供未來對接」預告的落地文件——**本文件應反向登記於該文件 F.6，將「未來的打擊感 GDD」改為具體路徑 `design/gdd/hit-feel-tiering.md`**（見本次變更，已同步執行） |
| **kaiju-part-system.md** | 事件來源（必要） | `on_part_break`（`Mid`/`Boss` 一般部位）、`on_boss_core_break`（僅 `Boss`，`part_type = BossCore` 時觸發，見其 E.6） | 無 |
| **stage-system.md** | **新增需求（本文件對其提出，兩項）** | `EnemyDef.IsElite`、`EnemyDef.EliteAuraColor`（`Elite` 擊殺 VFX 沿用其色調） | 本文件要求 stage-system 的死亡/清除邏輯發出兩個新事件：(1) 雜兵/菁英死亡 `on_enemy_killed`（C.2）；(2) 中型遭遇清除 `on_mid_encounter_cleared`（C.3.1，需監聽 Mid `KaijuDef` 全部部位 `BROKEN` 狀態，且遵守 E.2 的延遲排序規則）——**這是本文件新增給 stage-system.md 的相依，該文件應在其 Dependencies 章節反向記錄**，並與其既有 F.4「中型遭遇的清除判定」職責歸屬（見 `enemy-tier-system.md` F.4）對齊 |
| **material-economy.md** | 協調（非阻斷） | 無直接資料消費 | 需與 E.4 的莢艙下降動畫時序協調，避免視覺搶畫面 |
| **GameFeelConfig.cs（Content）** | 資料容器（必要） | 無 | 本文件提議在其中新增「G.6 敵人分級打擊感」欄位群（見 G 節），遵循既有 ADR-0003 資料驅動模式與 OnValidate 護欄慣例 |
| **Unity 6.3 LTS** | 引擎 API | `ParticleSystem`（爆炸粒子）、`AudioSource.pitch`（D.5 音高隨機化） | 雜兵/菁英擊殺粒子（D.4/D.6）與 Mid 遭遇清除粒子（D.7）需與整體 `≤200` draw call 全域預算（`.claude/docs/technical-preferences.md`）協調——多隻雜兵同幀死亡時的粒子池化策略建議由 `unity-specialist` 於實作前審視 |

---

## G. 調校旋鈕 (Tuning Knobs)

**所有數值存放於 `GameFeelConfig`（ScriptableObject），禁止硬編碼。** 提議新增「G.6 敵人分級打擊感」欄位群，附加於既有 `Assets/_Project/Scripts/Content/GameFeelConfig.cs`（沿用其 PascalCase 屬性／`_camelCase` 私有欄位／`OnValidate` 護欄慣例）。

### G.6 敵人分級打擊感旋鈕（新增）

#### 雜兵／菁英擊殺剖面（Trash / Elite）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `mob_kill_hitstop_ms_trash` | 0 ms | [0, 20] | 閘門 | Trash 雜兵擊殺頓幀；預設 0（高頻事件不占頓幀預算） |
| `mob_kill_hitstop_ms_elite` | 30 ms | [0, 60] | 手感 | Elite 菁英擊殺頓幀；不得超過部位破壞的 115ms（防與巨獸事件混淆） |
| `mob_kill_shake_px_trash` | 0 px | [0, 3] | 閘門 | Trash 雜兵擊殺震動 |
| `mob_kill_shake_px_elite` | 5 px | [3, 8] | 手感 | Elite 菁英擊殺震動；對齊既有「護甲剝除」5px 錨點 |
| `mob_kill_flash_trash` | 0.0 | [0, 0.1] | 閘門 | Trash 雜兵擊殺閃光 |
| `mob_kill_flash_elite` | 0.15 | [0, 0.3] | 手感 | Elite 菁英擊殺閃光；遠低於部位破壞 0.92 |
| `mob_kill_particle_count_trash` | 8 | [4, 12] | 手感 | Trash 雜兵擊殺粒子數 |
| `mob_kill_particle_count_elite` | 20 | [14, 28] | 手感 | Elite 菁英擊殺粒子數 |
| `mob_kill_sfx_pitch_variance` | 0.10 | [0, 0.25] | 手感 | D.5 音高隨機化幅度；Trash/Elite 共用，避免高頻擊殺聽覺疲勞 |
| `mob_kill_sfx_max_per_frame` | 3 | [1, 6] | 手感 | 單幀最多同時播放雜兵/菁英擊殺音效數，防音效擁擠（沿用 `game-feel.md` G.4 `softened_sfx_max_per_frame` 模式） |
| `kill_feel_simultaneous_budget` | 4 | [2, 8] | 閘門 | D.6 同幀完整回饋剖面上限；超過者套用輕量剖面 |

#### 中型遭遇清除剖面（Mid）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `mid_clear_shake_mag` | 15 px | [8, 18] | 手感 | D.7 震動脈衝；經 `shake_magnitude_cap`（24px）clamp 防呆 |
| `mid_clear_flash` | 0.4 | [0.2, 0.6] | 手感 | D.7 二次閃光；經 `flash_max_alpha`（0.85）clamp 防呆 |
| `mid_clear_particle_bonus` | 18 | [10, 30] | 手感 | D.7 追加粒子數 |
| `mid_clear_delay_after_hitstop` | 沿用 `hitstop_part_break_ms`（115ms，`game-feel.md` G.3） | — | 閘門 | E.2 強制排序延遲；非獨立旋鈕，直接引用既有值以保持單一事實來源 |

> **刻意不存在的旋鈕**：`Mid` 階不提供 `hitstop`/`slowmo` 相關欄位（見 C.3.1 設計理由——避免與觸發它的最後部位自身頓幀/慢動作連續堆疊）。`Boss` 階不新增任何欄位（C.3.2，完全沿用 `game-feel.md` 既有 G.1–G.3，包含其 `shake_magnitude_cap` 硬性護欄）。

---

## H. 驗收標準 (Acceptance Criteria)

> 本節大部分屬於**體驗性（Visual/Feel）**——依專案測試標準（`.claude/docs/coding-standards.md`「Testing Standards」表），Visual/Feel 類型的必要證據是**螢幕截圖 + 負責人簽核**（`production/qa/evidence/`），非自動化測試；標記【功能性】的項目為可自動化的閘門檢查。

### H.1 雜兵／菁英擊殺回饋正確性（功能性 — 阻斷）

- [ ] `Trash` 死亡：頓幀 = 0ms、震動 = 0px、閃光 = 0（三者實測皆為零，不觸發任何時間縮放或螢幕偏移）
- [ ] `Elite` 死亡：頓幀在 `mob_kill_hitstop_ms_elite` ± 5ms 容差內；震動峰值等於 `mob_kill_shake_px_elite`（乘上當前無障礙倍率）
- [ ] `Trash`/`Elite` 死亡**皆不觸發慢動作**（`Time.timeScale` 全程維持 1.0，若無其他巨獸事件同時發生）
- [ ] 自動化測試：`Assets/_Project/Tests/EditMode/GameFeel/mob_kill_tier_profile_test.cs`

### H.2 同幀取最大值 / 不疊加（功能性 — 阻斷）

- [ ] `Elite` 擊殺頓幀與巨獸部位破壞頓幀同幀觸發時，最終頓幀 = `max(30ms, 115ms) = 115ms`（不相加）
- [ ] D.6 同幀預算：同幀第 `kill_feel_simultaneous_budget + 1` 隻起的雜兵套用輕量剖面（粒子數為完整剖面的 40%，頓幀/震動/閃光/SFX 皆為零）
- [ ] 自動化測試：`Assets/_Project/Tests/EditMode/GameFeel/kill_feel_max_not_additive_test.cs`

### H.3 中型遭遇清除排序與 Boss 數值不變（功能性 — 阻斷）

- [ ] `on_mid_encounter_cleared` 的回饋（震動/閃光/粒子）確實延遲至觸發它的最後部位 `on_part_break` 頓幀計時器（115ms）結束後才開始（E.2）
- [ ] `mid_clear_shake_mag` 峰值不超過 `shake_magnitude_cap`（24px），即使旋鈕調至安全範圍上限（18px）
- [ ] `Boss` 階（任一現有 `KaijuDef`，`Tier = Boss`）核心擊破的頓幀/慢動作/粒子數與 `game-feel.md` 現行數值**逐位元相同**（回歸測試，確保本文件未意外改動既有巨獸手感）
- [ ] `Mid` 階（`Tier = Mid`）的一般部位破壞（非最後一個）與 `Boss` 階一般部位破壞使用完全相同的 `game-feel.md` 既有數值（回歸測試，驗證 C.3.1「沿用不變」的宣稱）
- [ ] 自動化測試：`Assets/_Project/Tests/EditMode/GameFeel/mid_encounter_cleared_sequencing_test.cs`

### H.4 視覺辨識度（體驗性 — Advisory）

- [ ] 設計師/QA 依序遊玩並擊殺 Trash → Elite → Mid 遭遇清除 → Boss 核心（可用測試場景模擬），主觀確認四者的打擊感**強度可清楚分辨排序**（Trash < Elite < Mid < Boss），評分 ≥ 4/5
- [ ] 5 人用戶測試：在含密集彈幕的靜態截圖／短影片中，受測者能正確判斷「剛才死掉的是雜兵還是菁英還是中型遭遇清除」，成功率 ≥ 70%（略低於 `game-feel.md` I.1 的 SOFTENED 80% 門檻，因本項屬 Advisory 非阻斷）
- [ ] D1 難度早期密集雜兵波段（10+ 隻連續死亡）截圖／錄影：畫面未出現明顯的螢幕震動或音效堆疊「轟炸感」（QA 主觀確認，服務 E.6）
- [ ] `Elite` 擊殺 VFX 色調與其存活時的 `EliteAuraColor` 光環肉眼可辨為同一色系（美術/QA 確認，服務 C.2 視覺延續性設計）
- [ ] `Mid` 遭遇清除的「兩段式」閃光節奏（最後部位的 0.92 峰值 → 衰減中疊加 0.4 二次脈衝）在截圖/錄影中可辨識為兩個獨立的視覺事件，而非單一更強的閃光（QA 主觀確認，服務 C.3.1 設計意圖）

### H.5 效能協調（功能性 — Advisory）

- [ ] 同幀 `kill_feel_simultaneous_budget` 上限測試（8 隻同時死亡）：無明顯掉幀（PC 60fps 基準）
- [ ] `Mid` 遭遇清除粒子（`mid_clear_particle_bonus` 上限 30）疊加最後部位自身碎片（22+）時，實測 draw call 仍在全域 `≤200` 預算內

### H.6 全值資料驅動（功能性 — 阻斷）

- [ ] `GameFeelConfig` 的「G.6 敵人分級打擊感」欄位群存在，且遵循既有 `OnValidate` 護欄慣例（例：`mid_clear_shake_mag` 超出 [8, 18] 時報錯，比照既有欄位模式）
- [ ] 修改任一 G.6 旋鈕值（如 `mob_kill_shake_px_elite = 0`）後重新進入關卡，效果立即反映，無硬編碼繞過

### H.7 與 enemy-tier-system.md 的一致性（功能性 — 阻斷，跨文件回歸）

- [ ] 本文件的 `Mid`/`Boss` 判別假設與 `enemy-tier-system.md` C.4/H.6 保持一致：任何 `Tier = Mid` 的 `KaijuDef` 資產，其部位破壞**不得**觸發 `on_boss_core_break`（若觸發，代表 `enemy-tier-system.md` H.6 的隔離測試已失敗，本文件的 `on_mid_encounter_cleared` 路徑也隨之失真——本文件依賴該測試作為前置條件，不重複實作，但納入本文件的整合測試套件依賴清單）

---

## I. 待決問題（Open Questions for Director Review）

1. **`Boss` 階內部是否需要「最終頭目」子分級**：改進意見原文「BOSS 再更誇張，可續推」暗示未來最終頭目（劇情草案 §B「巨型母體 Kaiju」）或許該比早期 Boss（CARAPEX 等）感受更強一階。但 `enemy-tier-system.md` 的 `EnemyTier` 目前不分「一般 Boss」與「最終頭目」。若要支援此差異，需要 `enemy-tier-system.md` 新增類似 `KaijuDef.IsRunFinale: bool` 的欄位（獨立於 `Tier`），本文件才能在其上疊加加成（沿用本文件初版設計的 clamp-within-existing-range 手法）。是否要啟動這項跨文件協調？或維持現狀——全部 `Boss` 階一視同仁，等真正需要時再擴充？

2. **Gate 破除（`on_gate_broken`）是否需要獨立打擊感規格**：`enemy-tier-system.md` C.3.2 定義菁英 Gate 破除事件，概念上對應巨獸的「護甲剝除」（`game-feel.md` C.1 已有 5px 震動＋撕裂音效＋弱點框）。本文件尚未定義 `on_gate_broken` 的回饋——直接沿用護甲剝除規格，還是設計獨立版本？

3. **雜兵擊殺 SFX 資產範圍**：C.2 提議 Trash 用單一「啵」聲＋音高隨機化，Elite 用「中型爆炸＋菁英鈴聲」雙層音效，Mid 遭遇清除用專屬 `sfxMidClear` 二音上揚。這三組音效目前皆為**全新資產**（尚未存在於 art-bible.md 或既有音效清單）——是否納入下一輪美術/音訊資產排程？或先用既有音效的縮小/變調版本頂上，等資產排上再換？

4. **`kill_feel_simultaneous_budget` 是否該隨難度階（D1–D4）動態調整**：`enemy-tier-system.md` A 段落聲明 Tier 數值難度不縮放，但難度確實影響雜兵密度（`enemy-tier-system.md` D.4）。是否代表 `kill_feel_simultaneous_budget` 也該隨難度提高（例如 D4 用 6，D1 用 3），避免高密度時仍嫌擁擠，或低密度時反而顯得吝嗇？若是，這會是難度系統與本文件的一項新交叉點，需要 `difficulty-system.md` 同步記錄。

5. **關卡最後一波清版的整體收束感**：波段結束時常有「最後一擊清光殘餘雜兵」的瞬間（例如 stage-system.md 的保底 Cycling Pod 生成邏輯）。是否需要一個「波段清空（wave cleared）」的額外收束回饋（不同於個別雜兵擊殺，也不同於 D.7 的 Mid 遭遇清除），例如一個統一的小型音效尾韻或 HUD 提示？這超出本文件「依敵人分級給回饋」的原始範圍，但與改進意見整體「打擊感要有層次」的精神相關，值得記錄供未來評估。

---

*文件版本：1.1.0（Draft — 已依 enemy-tier-system.md 權威定義修正 Tier 對應）*
*作者：Systems Designer Agent*
*關聯 GDD：game-feel.md（LOCKED 邏輯基礎）| kaiju-part-system.md | enemy-tier-system.md（權威分級定義）| stage-system.md*
*來源反饋：`design/feedback/2026-07-02-改進意見與劇情草案.md` §A.6*
