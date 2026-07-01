# 難度系統 (Difficulty System) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑：design/gdd/difficulty-system.md*
*最後更新：2026-07-01*
*狀態：Draft*
*相依文件：game-concept.md | stage-system.md | weapon-system.md | kaiju-part-system.md | material-economy.md*

---

## A. 概覽 (Overview)

難度系統（Difficulty System）是殲獸戰機「**難度是門，不是牆（Difficulty is a Door）**」支柱的直接落地機制。玩家在每輪遊玩**開局前**選擇四個難度階（D1–D4）之一；系統以此在單一參數向量上縮放敵彈密度與敵人數量。所有其他遊戲內容、武器公式、部位血量、素材產量均在四個難度階下**完全恆定**。

難度系統是**存取控制層（Access Control Layer）**，而非內容解鎖層（Content Unlock Layer）：
- 它控制**遊玩壓力**（敵彈密度），不控制**遊玩深度**（內容可及性）。
- 它沒有「高難度才解鎖的東西」——完整遊戲內容在 D1 即全部開放。
- 它每輪重新選擇，玩家可在不同輪之間自由切換（但在輪中途不可更改）。

本文件是全專案所有「難度縮放」聲明的**唯一整合權威來源（Single Source of Truth）**，其他 GDD 的難度相關行為均以本文件為最終參照。

---

## B. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：挑戰（Challenge）＋ 服從放鬆（Submission at D1）＋ 精通（Competence at D4）

難度系統服務兩個截然不同的玩家需求，且同時實現：

**「以我的步調探索」**（D1–D2，服務 Submission + Achiever）
最低難度 D1 是低壓力的刷關入口（Low-Pressure Farming Entry）。新玩家與疲憊的老玩家都能在此農材料、體驗破壞部位的爽感，而不被密集彈幕趕跑。素材和武器完整開放，沒有人因為「選了低難度」而錯過任何內容。這直接服務 `game-concept.md` Target Aesthetics 的 Submission 面向：「最低難度階提供低壓力刷關入口」。

**「我要看看自己真正的極限」**（D3–D4，服務 Killers / Competence）
最高難度 D4 的惡夢彈幕是一面高牆，但它不鎖東西——它只讓核心循環變得更緊張、更有技術天花板。爬難度不是為了解鎖內容，而是為了見識自己能達到什麼境界，是純粹的自我設定挑戰，直接服務 Bartle Killers 動機。

---

## C. 詳細規則 (Detailed Rules)

### C.1 四階難度定義 (The Four Tiers)

難度階名稱與代號由 `stage-system.md` I.1 確立，本文件繼承並整合。

| 難度代號 | 中文名稱 | 英文名稱 | 設計意圖 |
|----------|---------|---------|---------|
| **D1** | 普通 | Normal | 基準難度；新手友善；所有 Stage 1 特殊引導規則啟用；彈幕密度最低，閱讀難度最低 |
| **D2** | 困難 | Hard | 有意識的挑戰；玩家感受到需要更主動讀彈；適合已通關 D1、想感受壓力成長的玩家 |
| **D3** | 極限 | Extreme | 需持續主動讀彈；閃避頻率顯著提升；雷射蓄熱 Uptime 自然開始壓縮，TTB 隱性延長 |
| **D4** | 惡夢 | Nightmare | 最高密度；子彈幕覆蓋率最大化；蓄熱 Uptime 大幅壓縮——即使是老手每場都是緊繃挑戰 |

**選擇規則**：
- 難度在**每輪開始前的主選單**選擇，選擇後鎖定至本輪結束或主動放棄。
- 難度在本輪進行中**不可變更**（見 E.1 邊界情況）。
- 完成一輪後，下一輪難度預設回填為**玩家上次選擇**的難度（`remember_last_difficulty = true`）。
- 任何難度均可選擇任何已解鎖的關卡與巨獸——無前置難度條件。

### C.2 縮放參數：什麼會改變 (What Scales)

難度系統**只縮放兩個參數**，均由 `assets/data/stages/difficulty_config.yaml` 控制：

| 參數名稱 | 縮放對象 | 觸發時機 | 套用公式 |
|---------|---------|---------|---------|
| `enemy_count_mult[tier]` | 每波敵人生成數 | 每波次生成前 | `actual_count = ceil(base_count × enemy_count_mult[tier])` |
| `bullet_density_mult[tier]` | 射擊型敵人每次發射的子彈數 | 每次敵人射擊事件 | `actual_bullets = ceil(base_bullets × bullet_density_mult[tier])` |

> **設計約束：子彈飛行速度不縮放（Bullet Speed is Invariant）**。子彈速度是每種敵人的核心設計特性（與敵人移動速度等同地位），且提高子彈速度會破壞「彈幕永遠讀得懂」的視覺鐵則——反應窗口被壓縮將使可讀性崩潰，違背〔難度是門〕支柱。高難度的壓力唯一來自**密度**（更多子彈），不來自**速度**（更快子彈）。

### C.3 恆定不縮放：什麼永遠不變 (What Never Changes)

以下所有參數在 D1–D4 下**完全恆定**。任何以「增加高難度難度感」為理由修改這些值的企圖，均視為違反〔難度是門，不是牆〕支柱的設計違規。

| 類別 | 恆定值 | 權威文件 |
|------|--------|---------|
| **部位熱量容量** | H_max（NORMAL 100 / ARMORED 150 / BOSS_CORE 200 HU） | `kaiju-part-system.md` C.3、C.8 |
| **部位破甲容量** | B_max（NORMAL 100 / ARMORED 150 / BOSS_CORE 200 BU） | `kaiju-part-system.md` C.3、C.8 |
| **部位 TTB 設計目標** | 15–25s / 30–45s / 50–80s（各部位類型） | `weapon-system.md` D.4 |
| **武器輸出公式** | D₀、H_rate、B_rate、彈匣容量、換彈時間等全部武器數值 | `weapon-system.md` G.1–G.3 |
| **軟化 / 破甲閾值** | theta_S、theta_S_exit、B_unsoftened_mult | `kaiju-part-system.md` G.1 |
| **熱量衰減率** | H_decay_rate（預設 3 HU/s） | `weapon-system.md` G.1 |
| **震盪硬直時間** | stagger_duration、stagger_break_mult | `kaiju-part-system.md` G.1 |
| **武器莢艙掉落規則** | 保底機制、掉落類型、掉落頻率 | `stage-system.md` F.2–F.4 |
| **敵人移動速度** | 所有小怪移動速度旋鈕 | `stage-system.md` K.4 |
| **子彈飛行速度** | 所有射擊型敵人的彈速旋鈕（見 C.2 設計約束） | `stage-system.md` K.4 |
| **波段池組成與抽取規則** | 波段池結構、No-repeat window | `stage-system.md` D.1 |
| **素材種類與單次產量** | shard_yield、core_yield、essence 觸發條件 | `material-economy.md` D.1、G.3 |
| **武器升級成本** | 所有 Tier 升級成本表 | `material-economy.md` C.4 |
| **精魄掉落條件** | 全破壞結算觸發（任何難度下全破壞即得精魄） | `material-economy.md` C.1 |
| **關卡 / 巨獸可及性** | 所有已解鎖內容在所有難度階完全可達 | `game-concept.md` Pillar 5 |

### C.4 隱性難度效應：自然 TTB 延長 (Implicit Difficulty Effect)

高難度下 TTB（Time-To-Break）**不靠部位數值增加，而靠玩家蓄熱 Uptime 下降自然延長**。這是刻意設計的隱性調節——難度的「更難」感來自閃避壓力增加，而非數值膨脹：

- **D1** → 玩家閃避頻率低，雷射命中率接近理論最大值 → TTB 接近設計基準
- **D4** → 玩家必須頻繁閃避，有效蓄熱 Uptime 顯著壓縮 → 實際 TTB 可比 D1 長 1.5–2.5×

這是合法的隱性調節：`kaiju-part-system.md` 的所有公式在各難度下以完全相同的數值運行；TTB 延長是玩家 `Δt_laser_active`（雷射命中幀數）縮短的結果，而非 `H_max` 或 `B_max` 改變。詳見 `kaiju-part-system.md` C.8 與 `weapon-system.md` F.4。

### C.5 獎勵與難度：等值原則 (Rewards are Difficulty-Neutral)

**所有素材、武器、內容在 D1–D4 下完全等量可達。**

| 項目 | 難度影響 |
|------|---------|
| 通用碎片（Common Shard）單次破壞產量 | 無差異（`difficulty_yield_bonus = 0.0`，見 `material-economy.md` G.3）|
| 巨獸核心（Kaiju Core）掉落 | 無差異（掉落條件為部位破壞，不依難度）|
| 精魄（Essence）觸發條件 | 無差異（全破壞即觸發，D1–D4 均可）|
| 武器種類與升級解鎖 | 所有武器在所有難度可從場地掉落取得；升級成本一律相同 |
| 關卡 / 巨獸可及性 | 所有已解鎖內容在所有難度階完全可達 |

**爬難度的唯一激勵**：

1. **挑戰與精通感**（Bartle Killers / Competence）——「我能在 D4 打出全破壞嗎？」
2. **夢想層（Dream Layer，可選）**：排行榜 + 每日挑戰（見 `game-concept.md` Retention Hooks）記錄最高難度成就。夢想層**不鎖定任何功能性內容**，僅作為社群比較與自我挑戰動機。

> **這是「難度是門，不是牆」的最終表達：沒有任何武器、巨獸、素材或功能因為難度不夠高而被鎖住。爬難度是選擇，不是要求。**

### C.6 D1 作為可及性入口 (Accessibility — The D1 Promise)

D1（普通 Normal）是「最低難度階提供低壓力刷關入口」的具體承諾：

- 所有 Stage 1 特殊引導規則在 D1 完整啟用（`ram_grub_intro_speed_mult = 0.7` 引入段減速；HUD 一次性提示），見 `stage-system.md` H.2。
- 波段池過濾：D1 僅抽取 `difficulty_weight ≤ 3` 的波段，避免高壓段在新手期出現，見 `stage-system.md` D.3。
- D1 子彈密度基準（×1.0）是全遊戲所有視覺可讀性驗收測試的**基準情境**。
- 首次啟動遊戲，預設難度為 D1（不要求玩家主動降低難度才能舒適遊玩）。
- **色彩可及性與無障礙設計**（Colorblind Support / Accessibility）不由本系統控制，延後至遊戲感受（Game Feel）/ UX 文件統一處理。

---

## D. 公式 (Formulas)

### D.1 每波次敵人數量公式 (Enemy Count per Wave)

**命名表達式**：

```
actual_count = ceil( base_count × enemy_count_mult[tier] )
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `base_count` | int | [1, 11] | 波段 YAML 中設計師設定的基礎敵人數；上限受 E.4 場地容量約束（≤ floor(20/1.75) = 11） |
| `enemy_count_mult[tier]` | float | {1.00, 1.25, 1.50, 1.75} | 對應當前難度階的敵人數量乘數（查 `difficulty_config.yaml`）|
| `tier` | enum | {D1, D2, D3, D4} | 本輪玩家選擇的難度階（開局鎖定）|
| `actual_count` | int | [1, 20] | 本波次實際生成的敵人數；ceiling 取整，最低 1；上限受 `enemy_cap_per_scene` 截斷 |

**輸出範圍**：`actual_count ≥ 1`（ceil 保護；`base_count > 0` 時必然 ≥ 1）；不超過 `enemy_cap_per_scene = 20`（場地上限截斷）。

**運算範例**（S1-02「砲台陣線」W1 波次，`base_count = 3`，D3 難度）：
```
actual_count = ceil(3 × 1.50) = ceil(4.50) = 5
```

---

### D.2 每次射擊子彈數公式 (Bullets per Shot)

**命名表達式**：

```
actual_bullets = ceil( base_bullets × bullet_density_mult[tier] )
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `base_bullets` | int | [1, 8] | 敵人設計的基礎每次射擊子彈數（由 `enemy_config.yaml` 定義）|
| `bullet_density_mult[tier]` | float | {1.00, 1.25, 1.50, 2.00} | 對應當前難度階的子彈密度乘數（查 `difficulty_config.yaml`）|
| `tier` | enum | {D1, D2, D3, D4} | 本輪玩家選擇的難度階（開局鎖定）|
| `actual_bullets` | int | [1, 16] | 本次射擊實際發射子彈數；ceil 取整 |

**輸出範圍**：`actual_bullets ≥ 1`；D4 乘數 ×2.0 使所有射擊型敵人的單次發射量翻倍。理論上限 = `base_bullets_max(8) × 2.0 = 16`；視覺可讀性驗收（H.7）確保此上限下彈幕仍可辨識。

**運算範例**（`tri_shot` 三叉砲艦，`base_bullets = 3`，D4 難度）：
```
actual_bullets = ceil(3 × 2.00) = ceil(6.00) = 6
```

---

### D.3 難度縮放乘數總表 (Multiplier Reference Table)

**命名表達式**（查表；此表為 `stage-system.md` I.1 的整合再陳述，數值以本文件為準）：

| `tier` | 中文名稱（英文）| `enemy_count_mult` | `bullet_density_mult` | 玩家感知效果 |
|--------|--------------|-------------------|-----------------------|------------|
| D1 | 普通（Normal）| 1.00 | 1.00 | 基準；彈幕可輕鬆閱讀；教學友善 |
| D2 | 困難（Hard）  | 1.25 | 1.25 | 需有意識讀彈；壓力可感知 |
| D3 | 極限（Extreme）| 1.50 | 1.50 | 持續主動讀彈；蓄熱 Uptime 開始受壓 |
| D4 | 惡夢（Nightmare）| 1.75 | 2.00 | 子彈密度翻倍；Uptime 大幅壓縮；蓄熱難以維持 |

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `tier` | enum | {D1, D2, D3, D4} | 本輪玩家選擇的難度階 |
| `enemy_count_mult` | float | {1.00, 1.25, 1.50, 1.75} | 每波敵人數量乘數 |
| `bullet_density_mult` | float | {1.00, 1.25, 1.50, 2.00} | 每次射擊子彈數乘數 |

**輸出範圍**：均為有限離散集合，不連續。設計師不可插值或使用中間值（例如「D3.5」不存在）。

**D4 不對稱性說明**：`enemy_count_mult`（1.75）與 `bullet_density_mult`（2.00）刻意不對齊。D4 的策略是「同數量的敵人射出更密集的彈幕」——增加的是**彈幕管理壓力**，而非場面敵人數量壓力。這確保 D4 的核心挑戰是「讀彈精準度」，避免場面敵人過多導致效能問題與視覺混亂。

---

### D.4 隱性 TTB 延長估算公式 (Implicit TTB Extension — Design Tool)

本公式**非遊戲內執行公式**，僅作為設計師的平衡驗證工具，確認高難度 TTB 自然延長的程度合理，不需調整部位數值。

**命名表達式**：

```
T_soften_effective(tier) = theta_S / max( H_rate × uptime_ratio(tier) − H_decay_rate,  ε )

TTB_effective(tier) = T_soften_effective(tier) + T_break_effective(tier)
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `theta_S` | float | [80, 120] HU | 軟化入口閾值（全域旋鈕，預設 100 HU；難度不縮放） |
| `H_rate` | float | [8, 50] HU/s | 武器在命中狀態下的熱量速率（難度不縮放） |
| `H_decay_rate` | float | [1, 8] HU/s | 熱量冷卻速率（預設 3 HU/s；難度不縮放） |
| `uptime_ratio(tier)` | float | (0, 1] | 玩家在該難度下能持續命中目標的時間比例（設計估算值，依玩家技術變動；非遊戲內參數） |
| `ε` | float | 0.01 | 防除零保護 |
| `T_soften_effective(tier)` | float | [0, ∞) s | 考慮實際命中率後的有效蓄熱時間（設計工具） |
| `TTB_effective(tier)` | float | [0, ∞) s | 考慮實際命中率後的有效 TTB（設計工具，非遊戲內計算） |

**輸出範圍**：`TTB_effective ≥ TTB_base`（D1 基準）；`uptime_ratio` 設計目標：D4 最低不低於 0.40，確保巨獸戰不因 uptime 崩潰而無法推進。

**運算範例**（L2 集束雷射，NORMAL 部位，D4，`uptime_ratio = 0.50`）：
```
// 難度不縮放的固定值：theta_S = 100 HU，H_rate = 37.5 HU/s，H_decay_rate = 3 HU/s

T_soften_effective(D4) = 100 / max(37.5 × 0.50 − 3, 0.01)
                       = 100 / max(15.75, 0.01)
                       ≈ 6.35 秒

// 對比 D1 理論值（uptime_ratio = 1.0）：
T_soften_effective(D1) = 100 / max(37.5 × 1.0 − 3, 0.01) ≈ 2.90 秒

// D4 的蓄熱需時約為 D1 的 2.2×——但 H_max 完全沒有改變，
// 只是玩家的命中時間縮短了。這是隱性調節的核心機制。
```

---

## E. 邊界情況 (Edge Cases)

### E.1 輪中途更改難度請求 (Mid-Run Difficulty Change Request)

**情況**：玩家在輪進行中（波段期間或頭目戰期間）嘗試更改難度設定。

**處理**：UI 設定選單中，難度選項在輪進行中顯示為**灰化不可選（Greyed-Out）**，並顯示提示「請在下一輪開始前選擇難度」。玩家可瀏覽各難度的說明文字，但無法在輪中途切換。新難度選擇在本輪結算或放棄後、進入主選單時生效。

### E.2 D4 波段池過濾後可抽取波段數不足 (D4 Segment Pool Exhaustion)

**情況**：D4 僅可抽取 `min_difficulty_tier ≤ D4` 且 `difficulty_weight ≥ 3` 的波段（見 `stage-system.md` D.3）。若某關卡高強度波段數 < 需抽取數 N，波段池枯竭。

**處理**：繼承 `stage-system.md` D.1 的安全閥規則——若過濾後剩餘波段 < N，**放寬篩選至全池**（接受所有 `min_difficulty_tier` 的波段），確保關卡可完成性不被難度過濾破壞。此為防守性設計，不影響一般運作。

### E.3 非 D1 難度下的首次遊玩 (First-Play on Non-D1 Difficulty)

**情況**：玩家以 D2–D4 難度進行人生首次遊玩（主動選擇高於 D1 的難度）。

**處理**：Stage 1 的 HUD 一次性提示（`first_pod_pickup_shown` flag）依然啟用——此提示由「是否首次遊玩」判斷，**與難度無關**。Stage 1 速度減速規則（`ram_grub_intro_speed_mult`）只在 `difficulty_tier == D1` 時啟用；D2–D4 首次遊玩不啟用速度減速，因為玩家主動選擇了更高壓力，不應被教學邏輯強制降速。

### E.4 高乘數造成 actual_count 超過場地容量 (Enemy Cap Overflow)

**情況**：D4 × 高 `base_count` 可能在場地生成過多敵人，導致效能問題或彈幕不可讀。

**處理**：關卡設計師在設定 `base_count` 時須遵守設計約束：`base_count ≤ floor(enemy_cap_per_scene / difficulty_enemy_mult[D4]) = floor(20 / 1.75) = 11`。系統於生成時以 `enemy_cap_per_scene = 20` 強制截斷，超出部分不生成。設計師靜態審核須確認所有波段的 `base_count` 符合此上限。

### E.5 D4 素材品質的隱性影響 (Implicit Material Quality Reduction at D4)

**情況**：D4 高密度彈幕迫使玩家更多閃避，減少雷射蓄熱時間，使更多部位以「Standard（未軟化）」而非「Precision / Perfect」品質破壞，隱性降低每場素材產量。

**處理**：這是設計**接受且預期的隱性調節**，不是 bug，也不需要補償機制。此行為已在 `material-economy.md` F.3 明確記錄（「高難度彈幕密度間接降低蓄熱 Uptime → 品質等級降低，這是自然隱性調節，非刻意懲罰」）。`difficulty_yield_bonus` 恆為 0.0——**禁止**引入「高難度 = 更高素材基礎獎勵」的補償機制，違反等值原則（C.5）。

---

## F. 系統相依 (Dependencies)

| 相依系統 | 方向 | 說明 |
|---------|------|------|
| `stage-system.md`（波次生成） | 難度系統 → Stage System（輸出） | Stage System 在每波次生成前讀取 `enemy_count_mult` 與 `bullet_density_mult`；本文件為這兩個參數的最終整合定義來源 |
| `stage-system.md`（波段過濾） | 難度系統（tier 值）→ Stage System（消費） | 波段 YAML 的 `min_difficulty_tier` 欄位與 `tier` 比對，決定哪些波段可在本輪抽取；Stage System 執行過濾邏輯 |
| `kaiju-part-system.md` | 難度系統**不接觸**（明確隔離） | 部位系統所有數值難度不縮放；難度系統不向部位系統發送任何縮放指令（見 `kaiju-part-system.md` F.6）|
| `weapon-system.md` | 難度系統**不接觸**（明確隔離） | 武器數值難度不縮放；有效輸出下降完全由玩家 uptime 行為決定（見 `weapon-system.md` F.4）|
| `material-economy.md` | 難度系統**不接觸**（明確隔離） | 素材產量公式難度不縮放；`difficulty_yield_bonus = 0.0`（見 `material-economy.md` F.3、G.3）|
| **UI / 主選單系統** | UI → 難度系統（輸入） | 玩家在主選單難度選擇畫面設定 `difficulty_tier`；本系統持有本輪難度值供各子系統查詢 |
| **存檔系統（Save System）** | 難度系統 → 存檔（輸出） | 持久化 `last_selected_difficulty`，供下一輪開局預設回填 |
| `difficulty_config.yaml` | 靜態資料來源 | 路徑：`assets/data/stages/difficulty_config.yaml`；所有乘數的唯一儲存位置；禁止硬編碼 |

### F.1 資料流：難度如何傳遞至各子系統 (Data Flow)

```
[玩家於主選單選擇難度]
         │
         ▼
 difficulty_tier ∈ {D1, D2, D3, D4}   ← 本輪鎖定
         │
         ├──────────────────────────────────────────────────────┐
         │                                                      │
         ▼                                                      ▼
 [Stage System]                                     [Save System]
  讀取 difficulty_config.yaml                        持久化
  ├─ enemy_count_mult[tier]                          last_selected_difficulty
  │     └→ 每波生成前套用 D.1 公式
  └─ bullet_density_mult[tier]
        └→ 每次敵人射擊事件套用 D.2 公式

 [Stage System] 波段過濾
  └─ min_difficulty_tier ≤ tier → 過濾不可出現的波段

 ╔══════════════════════════════════════════════════════╗
 ║  以下系統不接受任何來自難度系統的輸入               ║
 ║  kaiju-part-system  / weapon-system                  ║
 ║  material-economy   / 武器升級成本                   ║
 ║  → 所有旋鈕在 D1–D4 下靜態恆定，運行期不變          ║
 ╚══════════════════════════════════════════════════════╝
```

---

## G. 調校旋鈕 (Tuning Knobs)

**所有難度乘數必須存放於 `assets/data/stages/difficulty_config.yaml`，禁止硬編碼。**

> **注意**：G.1 的乘數旋鈕已在 `stage-system.md` K.1 中定義。本節為整合再陳述與安全範圍確認，不重複擁有定義；修改這些數值應在 `difficulty_config.yaml` 進行，`stage-system.md` 與本文件同步參照。

### G.1 難度乘數旋鈕（difficulty_config.yaml）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `difficulty_enemy_mult[D1]` | 1.00 | — | 閘門 | D1 基準；**不可修改** |
| `difficulty_enemy_mult[D2]` | 1.25 | 1.10–1.50 | 曲線 | D2 敵人數量乘數 |
| `difficulty_enemy_mult[D3]` | 1.50 | 1.25–1.75 | 曲線 | D3 敵人數量乘數 |
| `difficulty_enemy_mult[D4]` | 1.75 | 1.50–2.00 | 曲線 | D4 敵人數量乘數；刻意低於 bullet_density_mult[D4]（見 D.3 不對稱性說明）|
| `difficulty_bullet_mult[D1]` | 1.00 | — | 閘門 | D1 基準；**不可修改** |
| `difficulty_bullet_mult[D2]` | 1.25 | 1.10–1.50 | 曲線 | D2 子彈密度乘數 |
| `difficulty_bullet_mult[D3]` | 1.50 | 1.25–1.75 | 曲線 | D3 子彈密度乘數 |
| `difficulty_bullet_mult[D4]` | 2.00 | 1.75–2.50 | 曲線 | D4 子彈密度乘數；允許超過 enemy_count_mult[D4] 以強調彈幕壓力優先 |

**調校安全指引**：
- `bullet_density_mult[D4]` 調高前，必先執行 H.7 D4 可讀性驗收測試，確認彈幕仍可辨識。
- 任何難度乘數的修改，需重新執行 H.1 乘數正確性測試。
- D4 `enemy_count_mult` 不可超過 `floor(enemy_cap_per_scene / 最大 base_count) = 1.81`（以 `enemy_cap_per_scene = 20`，`base_count_max = 11` 計算），以防截斷導致場地感知敵人數恆定。

### G.2 UI 行為旋鈕（difficulty_ui_config.yaml 或主設定檔）

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `default_difficulty_on_first_launch` | D1 | — | 閘門 | 首次啟動遊戲的預設難度；**必須為 D1**（見 C.6 D1 承諾） |
| `remember_last_difficulty` | true | — | 閘門 | 開局前預填上次選擇的難度 |
| `mid_run_difficulty_change_allowed` | false | — | 閘門 | 輪中途更改難度；**必須為 false**（見 E.1）|
| `enemy_cap_per_scene` | 20 | 15–25 | 閘門 | 場地最大同時敵人數；D.1 公式的截斷上限（見 E.4）|

---

## H. 驗收標準 (Acceptance Criteria)

### H.1 乘數正確應用（功能性 — 阻斷）

- [ ] 在 D1–D4 四個難度下，實際生成的敵人數 `actual_count` 與 `difficulty_config.yaml` 中的 `enemy_count_mult` 完全一致（允許 ceil 取整誤差 ±1）
- [ ] 在 D1–D4 四個難度下，射擊型敵人實際射出的子彈數與 `bullet_density_mult` 完全一致（允許 ceil 取整誤差 ±1）
- [ ] 自動化測試：`tests/unit/difficulty/multiplier_application_test`——對 D1–D4 × 至少 3 種 `base_count` 值（1 / 5 / 11）分別驗證，共 ≥ 24 個測試案例（4 難度 × 3 base_count × 2 參數）

### H.2 部位 TTB 在 D1–D4 下數值恆定（功能性 — **阻斷**）

- [ ] 讀取 D1 / D2 / D3 / D4 環境下，部位系統全域旋鈕（H_max、B_max、theta_S、theta_S_exit、H_decay_rate、B_unsoftened_mult、required_break_threshold_\*）的值，斷言在 D1–D4 下**完全相同**（數值相等）
- [ ] 自動化測試：`tests/unit/difficulty/part_ttb_invariance_test`——在模擬 D1/D2/D3/D4 環境下，各執行「L2 × M1 × NORMAL 部位完整 TTB 模擬（從 H=0 / B=0 到 BROKEN）」，斷言四個難度下的 `TTB_base`（部位數值理論值）完全相等
- [ ] 測試輸出 **4 × 3 矩陣**（4 難度 × 3 部位類型 NORMAL / ARMORED / BOSS_CORE）；確認每列（同部位類型、跨難度）數值完全一致；矩陣輸出存入 `production/qa/evidence/ttb_invariance_matrix.txt` 供設計師審閱

### H.3 武器輸出在 D1–D4 下數值恆定（功能性 — **阻斷**）

- [ ] 讀取 D1–D4 環境下，武器系統全部調校旋鈕（`D0_reference`、H_rate_\*、B_rate_\*、彈匣容量、換彈時間等）的值，斷言在 D1–D4 下**完全相同**
- [ ] 自動化測試：`tests/unit/difficulty/weapon_output_invariance_test`——對 8 把武器各自模擬 30 秒持續輸出（最優命中條件），斷言 D1–D4 下 `Sustained_Output(weapon)` 完全相同；此測試與 `weapon-system.md` H.1（等功率等價測試）共享基礎設施，增加「跨難度恆定」斷言層
- [ ] 測試輸出 **4 × 8 矩陣**（4 難度 × 8 武器）的 `Sustained_Output` 值；確認每列（同武器、跨難度）數值完全一致

### H.4 素材產量在 D1–D4 下等量（功能性 — 阻斷）

- [ ] 在 D1–D4 下模擬同一場景（Precision 品質的 NORMAL 部位破壞事件），`shard_yield` 和 `core_yield` 輸出完全相同
- [ ] `difficulty_yield_bonus` 讀取值在 D1–D4 下恆為 0.0
- [ ] 自動化測試：`tests/unit/difficulty/material_yield_invariance_test`——3 品質等級 × 3 部位類型 × 4 難度 = 36 個測試案例；確認同品質/部位類型跨難度產量完全相同

### H.5 內容可及性（功能性 — 阻斷）

- [ ] D1–D4 下，所有已解鎖的關卡（Stage 1–3）均可從主選單選擇並進入，無任何難度前置條件
- [ ] D1–D4 下，所有武器莢艙掉落保底規則均正常生效（繼承 `stage-system.md` L.2 保底測試）
- [ ] 確認主選單「關卡選擇」介面在 D1–D4 下的 UI 邏輯不存在任何基於難度的鎖定判斷（靜態代碼審核或自動化 UI 測試）

### H.6 D1 可及性承諾（體驗性 — Stage 1 MVP 阻斷）

- [ ] 5 人新手測試（D1，首次遊玩）：Stage 1 通關後，受測者可不看任何外部教學完成「移動 → 射擊 → 拾取武器 → 破壞部位 → 素材入袋」完整循環，達成率 ≥ 80%（繼承 `stage-system.md` L.4 標準）
- [ ] D1 彈幕密度（×1.0）下，所有小怪子彈色溫在靜態截圖中與玩家判定點一眼可辨：5 人截圖辨識率 ≥ 80%（繼承 `stage-system.md` L.6 標準，D1 為基準情境）

### H.7 D4 彈幕可讀性下界（體驗性 — Vertical Slice）

- [ ] D4 最高密度下，敵彈顏色（暖橙 / 橙黃）與玩家判定點仍可在 0.5 秒內一眼區分：5 人截圖辨識率 ≥ 70%（D1 標準 80%，D4 允許適度降低但不低於 70%）
- [ ] D4 下，莢艙攜帶者頂部閃爍圖示在最密集波次中的識別率 ≥ 70%（繼承 `stage-system.md` L.2 D4 標準）
- [ ] **若 D4 可讀性未達標**：優先調降 `difficulty_bullet_mult[D4]`（在安全範圍 1.75–2.50 內），**不**更改子彈顏色或玩家判定點設計

### H.8 難度選擇 UI 行為（功能性）

- [ ] 首次啟動遊戲，預設難度為 D1，UI 顯示 D1 選中狀態，其他難度未選
- [ ] 完成或放棄一輪後進入主選單，難度預填為該輪所選難度
- [ ] 輪進行中開啟設定選單，難度選項顯示為灰化，無法互動，並顯示提示文字

---

*文件版本：1.0.0*
*作者：Systems Designer Agent*
*最後更新：2026-07-01*
*狀態：Draft*
*關聯 GDD：game-concept.md | stage-system.md | weapon-system.md | kaiju-part-system.md | material-economy.md*
