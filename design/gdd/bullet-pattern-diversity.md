# 彈幕型別與敵人移動多元化 (Bullet Pattern & Movement Diversity) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑：design/gdd/bullet-pattern-diversity.md*
*最後更新：2026-07-03*
*狀態：Draft*
*引擎：Unity 6.3 LTS（C#）*
*相依文件：bullet-system.md（撰寫模型權威 §4）| difficulty-system.md（密度縮放權威）| stage-system.md（小怪名單 §E、Prefab 架構 §E.0）| ADR-0001（BulletSim 後端，執行期實作閘門）| ADR-0003（資料驅動 SO 設定）| design/feedback/2026-07-02-改進意見與劇情草案.md §A.1（本文件來源需求）*

---

## 1. 概覽 (Overview)

本文件回應 `design/feedback/2026-07-02-改進意見與劇情草案.md` §A.1：現況小怪彈幕僅有「單排子彈」等極少樣式、移動方式與體型也高度同質。本文件將 `EmitterPatternSO`（彈幕撰寫層）與 `MovementPatternSO`（移動撰寫層）從現行的扁平列舉，重構為**可組合（composable）的資料驅動型別系統**：彈幕拆為 **Shape（空間分佈）× Motion（飛行軌跡）× Trigger（發射時機）** 三個正交軸，移動新增 2 種原型並補上對角進場參數，敵人另加體型（視覺 + 判定框各自獨立）縮放掛鉤。三者皆為 `EnemyDef` 可掛載的 SO 欄位，設計師在 Inspector 組合即可產出新敵人樣式，不需寫程式（呼應 `bullet-system.md` §4 撰寫模型與 ADR-0003）。

**本文件是撰寫層（authoring-layer）資料模型設計，非執行期實作規格。** `EmitterPatternSO` 目前僅供小怪使用（`KaijuDef` 尚未接入），本文件所設計的 Shape/Motion/Trigger 分解同時是 `bullet-system.md` §4.2 既有頭目彈幕參數表的具體 SO 落地與擴充——頭目文件（`kaiju/01-03`）的對齊為後續工作，不在本文件範圍。**新軌跡型 Motion（Spiral／Zigzag／Arc）的執行期逐幀運算閘於 ADR-0001**（DOTS BulletSim 效能 spike，目前 Proposed，待真機驗證後 LOCK）；本文件現在設計資料結構與撰寫工具，讓 ADR-0001 一旦通過即可立即進入內容產出，不必再等一輪設計。彈幕密度（含新增的同心圓數）永遠先過 `bullet-system.md` §5.2／§7 的同屏彈數硬上限與可讀性護欄，密度多元化不得換取可讀性。

---

## 2. 玩家幻想 (Player Fantasy) — 一眼認得出的敵人 (Silhouettes You Learn)

**「每種敵人都有自己的『簽名動作』——我看輪廓、看彈型就知道該怎麼應對。」**

`bullet-system.md` 建立的「可讀的混亂」承諾（暖色＝威脅、判定點恆亮）保證玩家看得懂**單顆子彈**；本文件要處理的是更高一層的認知——玩家看得懂**整個場面的敵人組成**。當所有敵人都用同一種「單排子彈＋直線下降」時，玩家的判斷退化成「閃開所有東西」，沒有分級、沒有優先順序決策。多元化的目標不是視覺熱鬧，而是重建「**認知詞彙表**」：

- **彈型即威脅類型**：Ring／同心圓 = 站位式壓力（要找縫隙）；Wall = 需要一條安全通道；Cross = 對稱死角，逼玩家離開中軸；Zigzag／Arc = 追蹤式壓力，教玩家「這顆子彈不會走直線，我不能用直覺閃」。玩家累積經驗後，看一眼彈型就知道優先閃避策略，而非逐顆計算軌跡。
- **移動即性格**：直衝 = 蠻力威脅；懸停 = 持續火力點，優先清除；Swoop（俯衝）= 有時間窗口的爆發威脅，逼玩家預判；Hover-Strafe = 移動中的火力點，比純懸停更難鎖定。移動原型的多樣性讓玩家「看敵人走位」就能分類威脅優先序，不需等它開火才知道要不要理它。
- **體型即分量感**：更大的敵人在視覺上「應該」更硬、更危險；體型與判定框的刻意脫鉤（見 §3.4）保護這個直覺——玩家看到大型敵人時的警戒感，不會被一個同樣放大的判定框背叛（呼應 game-concept.md「判定點至上」的公平性鐵則）。

**組合是設計師的工具，不是塞滿畫面的許可證。** 可組合的 Shape × Motion × Trigger 存在是為了讓「一種新彈幕」的產出成本從「工程師寫新程式碼」降到「設計師在 Inspector 選兩個下拉選單」；但同屏可讀性上限（`bullet-system.md` §5.2、§7）與難度密度縮放（`difficulty-system.md`）是這個工具永遠服從的護欄——多元化服務的是「玩家能分辨的威脅光譜變寬」，不是「畫面元素數量變多」。

---

## 3. 詳細規則 (Detailed Rules)

### 3.1 彈幕型別分解：Shape × Motion × Trigger

現行 `EmitterPatternSO.EmitterPatternType` 是單一扁平列舉（`Linear / Radial / Aimed / RingBurst`），只能表達「這一種」樣式；每新增一種彈型都得加一個新的列舉值，無法組合。本文件將其拆為**三個正交軸**，一個 `EmitterPatternSO` = 選一個 Shape + 一個 Motion + 一個 Trigger + 對應的參數：

#### 3.1.1 Shape（發射瞬間的空間分佈）

| Shape | 說明 | 對應舊列舉 | 主要參數 |
|-------|------|-----------|---------|
| `AimedFan` | 以玩家當下位置為中心的扇形，`BulletCount` 顆彈均分於 `SpreadAngleDeg` 總角內 | `Aimed` | `BulletCount`、`SpreadAngleDeg` |
| `Ring` | 以 `RingArcDeg`（預設 360＝整圈）均分 `BulletCount` 顆彈；`RingCount` > 1 時同幀同時發射多個同心圓（見 3.1.4） | `Radial`（`RingCount=1`）、`RingBurst`（`RingArcDeg=360` + Trigger=`OnDeath`） | `BulletCount`、`RingArcDeg`、`RingCount`、`RingSpeedStepPxPerSec` |
| `Wall` | 固定方向（`WallDirectionDeg`，預設 180°＝向下）噴出橫跨 `WallWidthPx` 寬度、`BulletCount` 條彈道的彈牆 | `Linear` | `BulletCount`、`WallDirectionDeg`、`WallWidthPx` |
| `Cross` | 以 `CrossStartRotationDeg` 為起始角，對稱均分 `BulletCount` 方向噴發（典型 4-way / 8-way，對應 CARAPEX C 既有設計） | 無（新） | `BulletCount`、`CrossStartRotationDeg` |

> Shape 決定的是**單次發射瞬間**各顆子彈的初始方向；子彈離開槍口之後怎麼飛，由 Motion 決定。

#### 3.1.2 Motion（發射後的飛行軌跡）

| Motion | 說明 | 對應舊概念 | 主要參數 |
|--------|------|-----------|---------|
| `Straight` | 固定速度直線飛行（預設，無額外運算） | 現行唯一行為 | 無 |
| `Spiral` | **Emitter 本身**的瞄準參考角每幀持續旋轉 `SpiralAngularSpeedDegPerSec`；個別子彈出生後仍是直線飛行——旋轉的是「每次發射的角度」，不是單顆子彈的軌跡。多臂旋轉彈幕（VOLTWYRM 蛇陣螺旋）即此型 | `bullet-system.md` §4.2 `spiral_angular_speed` | `SpiralAngularSpeedDegPerSec` |
| `Zigzag` | 單顆子彈疊加垂直於前進方向的正弦側向偏移：`ZigzagAmplitudePx`、`ZigzagFrequencyHz`；同一波次各彈可用 `ZigzagPhaseStaggerDeg` 錯相位，避免整排子彈像鎖死的梳子一樣同步擺動 | 無（新，回應「Z 字型」需求） | `ZigzagAmplitudePx`、`ZigzagFrequencyHz`、`ZigzagPhaseStaggerDeg` |
| `Arc` | 單顆子彈的飛行方向持續旋轉 `ArcCurvatureDegPerSec`（正負代表左右彎），產生平滑彎曲彈道；`ArcCurvatureDurationS`＝0 表示整個生命週期持續彎，>0 則彎曲一段時間後轉直線 | 無（新，回應「彎曲（曲射）」需求） | `ArcCurvatureDegPerSec`、`ArcCurvatureDurationS` |

> **Spiral 與 Arc 的差異是本節最容易混淆之處，需明確區分**：Spiral 旋轉的是**發射源的瞄準角**（子彈本身直線飛，但一波波之間的方向在轉）；Arc 旋轉的是**單顆子彈自己的飛行方向**（子彈飛行途中會轉彎）。VOLTWYRM 既有「蛇陣螺旋」設計用 Spiral；新的「曲射」需求用 Arc。

#### 3.1.3 Trigger（何時發射）

| Trigger | 說明 | 對應舊概念 |
|---------|------|-----------|
| `Periodic` | 每 `FireIntervalSeconds` 秒發射一次（現行唯一行為） | 現行唯一行為 |
| `OnDeath` | 敵人死亡瞬間、於死亡位置發射一次，`FireIntervalSeconds` 被忽略 | `RingBurst` |

#### 3.1.4 N-同心圓放射（Concentric Ring Burst）

回應「多個同心圓放射」的具體需求：`Shape=Ring` 且 `RingCount > 1` 時，同一幀同時發射 `RingCount` 組完全相同角度分佈、但速度不同的子彈波——因為速度不同，視覺上會隨時間展開成多層同心圓（外圈飛得快、內圈飛得慢，或反之，由 `RingSpeedStepPxPerSec` 正負決定）。詳細公式見 §4.1。**`RingCount` 直接乘進單次發射的總彈數，必須通過與 `BulletCount` 相同的密度縮放與同屏上限流程**（§4.4、§5.2）。

#### 3.1.5 舊列舉遷移對照（無資料流失）

現行 4 個 `EmitterPatternType` 值可無損映射到新三軸模型，10 種既有小怪（`stage-system.md` §E.1）的既有設計意圖完全保留：

| 舊 `EmitterPatternType` | 新 Shape | 新 Motion | 新 Trigger | 備註 |
|------------------------|---------|-----------|-----------|------|
| `Linear` | `Wall` | `Straight` | `Periodic` | `tri_shot` 等現行水平扇彈 |
| `Radial` | `Ring`（`RingCount=1`） | `Straight` | `Periodic` | 單圈放射 |
| `Aimed` | `AimedFan` | `Straight` | `Periodic` | `aimed_gun` 瞄準單發 |
| `RingBurst` | `Ring`（`RingArcDeg=360`, `RingCount=1`） | `Straight` | `OnDeath` | `ring_burst` 死亡 8 方向爆彈 |

---

### 3.2 一個敵人可以掛多個 Emitter（Composition at the Enemy Level）

`bullet-system.md` §4.1 已預設頭目的一個攻擊模式「= 一或多個 Emitter 的組合」（如 CARAPEX A「兩 Emitter 交替」），但現行 `EnemyDef._emitterPattern` 是**單一、非陣列**的參照，無法表達組合。本文件將其改為陣列：

```
EnemyDef
 ├─ MovementPattern : MovementPatternSO           （必填，單一，現行不變）
 └─ EmitterPatterns : EmitterSlot[]                （0..N，取代現行單一 EmitterPattern 欄位）
      EmitterSlot { Pattern: EmitterPatternSO;  PhaseOffsetSeconds: float }
```

- 陣列可為空（如 `ram_grub` 純接觸傷害敵人，現行行為不變）。
- 多個 Slot 各自獨立依自己的 `FireIntervalSeconds` / `Trigger` 運作；`PhaseOffsetSeconds` 讓設計師錯開多個 Emitter 的初始發射時間，避免同幀重疊造成瞬間彈數尖峰（見 §5 邊界情況）。
- 同一 `EmitterPatternSO` 資產可被多個不同種敵人、甚至同一敵人的多個 Slot 共用（如兩個 Slot 都指向同一個 `AimedFan` 資產但 `PhaseOffsetSeconds` 不同，形成交替瞄準彈）。

### 3.3 敵人移動原型（Movement Archetypes）

現行 `MovementType` 保留全部 5 值（`StraightRush / HorizontalDrift / Hover / UTurn / Sinusoidal`），新增 2 個原型 + 1 個共用參數：

| 新增項 | 型別 | 說明 |
|--------|------|------|
| `DiveSwoop` | 新 `MovementType` 值 | 三階段：① 以 `MoveSpeedPxPerSec` 直線下降至 `SwoopTriggerYPct`（螢幕高度百分比）；② 電報 `SwoopTelegraphS` 秒（閃爍，不移動，鎖定當下玩家 X 座標為 `SwoopTargetX`）；③ 以 `MoveSpeedPxPerSec × SwoopDiveSpeedMult` 朝鎖定點直線俯衝，衝出螢幕底部後正常離場。是敵人**本體**的鎖定+衝刺（沿用 `kamikaze` 既有「鎖定後衝鋒」設計precedent），非子彈追蹤，不牴觸彈幕「原則上非追蹤」的可讀性鐵則 |
| `HoverStrafe` | 新 `MovementType` 值 | 延伸現行 `Hover`：下降至 `HoverStrafe` 目標 Y（沿用 Hover 的到位邏輯）後，於 `[EntryX − HoverStrafeRangePx, EntryX + HoverStrafeRangePx]` 區間以 `HoverStrafeSpeedPxPerSec` 來回橫移（碰到邊界反向），讓懸停射手類敵人（如 `aimed_gun`）不再是死靶 |
| `EntryAngleDeg` | 共用參數（加在 `StraightRush` / `HorizontalDrift`） | 進場方向相對「正下方」的簽名偏角，範圍 [-60°, 60°]，0° = 現行行為（直衝入場位置 / 水平漂入）。讓「直衝」「漂入」型敵人不必新增列舉值即可有斜向進場變化 |

### 3.4 體型多元化掛鉤（Body-Size Hook）— 視覺與判定框脫鉤

`EnemyDef` 新增兩個獨立欄位：

| 欄位 | 說明 |
|------|------|
| `BodyScale` | 精靈 / 動畫的視覺縮放倍率，套用於敵人 Transform。純呈現用途，不影響任何數值（HP / 傷害仍由 `HpTier` / `EliteHpMult` 決定，不因體型連動）|
| `HitboxScaleMult` | 碰撞判定半徑縮放倍率，**與 `BodyScale` 完全獨立** |

**設計規則（公平性鐵則延伸）**：`HitboxScaleMult` 應 ≤ `BodyScale`——敵人的碰撞範圍不可大於玩家肉眼看到的輪廓範圍。這延伸自 `bullet-system.md`「判定點至上」的公平性精神：玩家的受擊判定是恆定單點小格；作為對稱設計，敵人的受擊/接觸判定也不該比其視覺輪廓更大、更難預期。體型變大是「這隻敵人看起來更硬」的敘事訊號，不是「你會被看不見的範圍打到」的懲罰。

### 3.5 Prefab 組成模型（落地 ADR-0003 / stage-system.md §E.0）

一個敵人 Prefab 仍由 `stage-system.md` §E.0 既定的四組件驅動，本文件擴充其中兩項：

| 組件 | 現況 | 本文件擴充 |
|------|------|-----------|
| 視覺（Visual）| 精靈 + 動畫 | 套用 `EnemyDef.BodyScale`（§3.4） |
| `MovementPatternSO` | 5 種原型 | +2 新原型 + `EntryAngleDeg`（§3.3） |
| `EmitterPatternSO` | 單一參照、4 扁平型別 | 陣列參照（§3.2）；每項為 Shape×Motion×Trigger 組合（§3.1） |
| `EnemyDef` 數值欄位 | `hp_tier`、接觸傷害、點數、`is_elite` | + `BodyScale`、`HitboxScaleMult`（§3.4） |

波次（Wave）資料維持現行「只紀錄 Prefab 引用」的原則不變（`stage-system.md` §D.2）——本文件不改變波次資料結構，只擴充 Prefab 內部三個 SO 能表達的行為範圍。菁英變體（`_elite` 後綴）沿用既有共用 SO 慣例：菁英與基礎型共用同一組 `MovementPatternSO` / `EmitterPatterns`，僅 `EliteDensityMult`（作用於陣列中每個 Emitter Slot）、`EliteHpMult`、`BodyScale × 1.1`（既有菁英視覺放大慣例，`stage-system.md` §E.3.1）疊加。

---

## 4. 公式 (Formulas)

### 4.1 Ring / Cross Shape 角度分佈

**expression**: `angle_i = arc_start_deg + ring_arc_deg × (i / max(bullet_count, 1))  (Ring, arc<360 時)`
`angle_i = arc_start_deg + 360° × (i / bullet_count)  (Ring 整圈或 Cross)`

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `i` | int | [0, bullet_count−1] | 該波次中第 i 顆子彈的索引 |
| `bullet_count` | int | [1, 20]（同 `EmitterPatternSO` 現行範圍）| 密度縮放後的實際彈數，見 4.4 |
| `ring_arc_deg` | float | [1, 360] | Ring 的總涵蓋角；360＝整圈 |
| `arc_start_deg` | float | [0, 360) | 起始角；`Ring` 用 `RingArcDeg`/`AimMode` 決定固定值，`Cross` 用 `CrossStartRotationDeg` |
| `angle_i` | float | [0°, 360°)（world frame，取模）| 第 i 顆子彈的初始飛行方向 |

**output range**：`angle_i` 恆落於 `[arc_start_deg, arc_start_deg + ring_arc_deg]` 並對 360° 取模；無截斷需求，角度本身天然循環有界。

**worked example**：`Cross`，`bullet_count=4`，`arc_start_deg=0` → `angle_0=0°, angle_1=90°, angle_2=180°, angle_3=270°`（對稱 4-way，CARAPEX C Phase3 現行設計的資料落地）。

---

### 4.2 N-同心圓速度分層 (Concentric Ring Speed Offset)

**expression**: `speed_w = bullet_speed_base + w × ring_speed_step_px_s`，`w ∈ [0, ring_count − 1]`

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `w` | int | [0, ring_count−1] | 第 w 層同心圓（0＝最先定義的一層）|
| `ring_count` | int | [1, 4] | 同幀同時發射的同心圓層數（1＝現行單圈行為）|
| `bullet_speed_base` | float (px/s) | > 0，沿用 `BulletSpeedPxPerSec` | 基準子彈速度；難度恆定不縮放（`bullet-system.md` §4.4）|
| `ring_speed_step_px_s` | float (px/s) | [−40, 40] | 每層之間的速度差；正值＝外圈快、內圈慢（展開式）；負值＝內圈快、外圈慢（收縮視覺較不建議，可讀性風險見 §5）|
| `speed_w` | float (px/s) | > 0（`OnValidate` 應保證任一層 `speed_w > 0`）| 第 w 層子彈的實際飛行速度 |

**output range**：`speed_w` 下限由 `bullet_speed_base − (ring_count−1)×|ring_speed_step_px_s|` 決定，設計時須確保恆為正值（子彈不可靜止或倒退）；上限無需硬夾，但過快的外圈會提早飛出可讀性判斷窗口，建議 `speed_w ≤ bullet_speed_base × 1.6`。

**worked example**：`bullet_speed_base=100 px/s`，`ring_count=3`，`ring_speed_step_px_s=20` → 三層速度分別為 100 / 120 / 140 px/s，同幀從同一原點發射，視覺上隨時間展開成三層漸疏的同心圓。

---

### 4.3 Zigzag 側向偏移 (Zigzag Lateral Offset)

**expression**: `lateral_offset(t) = zigzag_amplitude_px × sin(2π × zigzag_frequency_hz × t + phase_i)`，`phase_i = i × zigzag_phase_stagger_deg × (π / 180)`

實際位置：`position(t) = spawn_position + straight_dir × bullet_speed × t + perp_dir × lateral_offset(t)`（`perp_dir` 為 `straight_dir` 逆時針 90° 的單位向量）

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `t` | float (s) | ≥ 0 | 子彈自出生起的存活時間 |
| `zigzag_amplitude_px` | float (px) | [0, 40]（手機最小螢幕寬度安全上限，見 §7）| 側向擺動半幅 |
| `zigzag_frequency_hz` | float (Hz) | [0.3, 2.0] | 擺動頻率；恆定不隨難度縮放 |
| `i` | int | [0, bullet_count−1] | 同波次中該彈的索引，用於錯相位 |
| `zigzag_phase_stagger_deg` | float (deg) | [0, 180] | 相鄰彈之間的相位差；0＝全體同步擺動（梳子感）、90–180＝明顯錯開（魚群感）|
| `lateral_offset(t)` | float (px) | [−zigzag_amplitude_px, +zigzag_amplitude_px] | 疊加於直線路徑上的側向偏移量 |

**output range**：`lateral_offset` 恆被 `[−amplitude, +amplitude]` 夾住（正弦函數天然有界），不需額外 clamp；但 `amplitude` 本身的安全上限（40px）需在 SO `OnValidate` 中設定，防止在最小手機螢幕寬度下把子彈甩出可讀走位帶。

**worked example**：`zigzag_amplitude_px=24`，`zigzag_frequency_hz=1.0`，`zigzag_phase_stagger_deg=90`，`bullet_count=4`：四顆彈的相位分別為 0° / 90° / 180° / 270°，t=0.25s 時（一個週期的 1/4）各彈的側向偏移依序為 +24px / 0px / −24px / 0px，形成交錯的波浪陣列而非同步平移的一排。

---

### 4.4 Arc 曲率飛行方向旋轉 (Arc Curvature)

**expression**：`heading(t) = heading_0 + arc_curvature_deg_s × min(t, arc_curvature_duration_s_effective)`

其中 `arc_curvature_duration_s_effective = arc_curvature_duration_s` 若 `> 0`，否則（＝0）視為 `bullet_lifetime_seconds`（整個生命週期持續彎曲）。

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `heading_0` | float (deg) | [0°, 360°) | 出生瞬間的初始飛行方向（由 Shape 決定）|
| `arc_curvature_deg_s` | float (deg/s) | [−120, 120] | 曲率角速度；正＝順時針彎、負＝逆時針彎 |
| `arc_curvature_duration_s` | float (s) | [0, bullet_lifetime_seconds] | 彎曲持續時間；0＝整個生命週期持續彎（預設）|
| `t` | float (s) | ≥ 0 | 子彈自出生起存活時間 |
| `heading(t)` | float (deg) | [0°, 360°)（取模）| t 時刻的飛行方向；速度大小不變，只有方向旋轉 |

**output range**：`heading(t)` 對 360° 取模後天然有界；速度純量 `bullet_speed` 不受 Arc 影響，只有方向向量旋轉，符合 Burst-friendly「每幀等量運算」的效能前提（無需額外分支）。

**worked example**：`heading_0=180°`（垂直向下），`arc_curvature_deg_s=45`，`arc_curvature_duration_s=0`（全程彎曲）：t=1s 時方向已轉為 225°（偏向左下 45°），子彈畫出一道平滑左彎的「曲射」弧線，呼應設計意見原文「彎曲（曲射）」。

---

### 4.5 Spiral 發射源旋轉 (Spiral Emitter Rotation)

**expression**：`aim_reference_deg(t) = aim_reference_deg(0) + spiral_angular_speed_deg_s × t`（取模 360°）

每次 `Trigger=Periodic` 觸發時，該次波次的 `arc_start_deg`（4.1）改讀 `aim_reference_deg(t_fire)` 而非固定值；子彈出生後飛行方向不再改變（`Motion=Straight` 的行為疊加在旋轉後的起始角上）。

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `t` | float (s) | ≥ 0 | 該 Emitter 自啟用起的累計時間 |
| `spiral_angular_speed_deg_s` | float (deg/s) | [10, 720] | 每秒旋轉角度；恆定不隨難度縮放（沿用 `bullet-system.md` §4.4「速度/角速度永不縮放」原則）。小怪建議子範圍 [30, 150]（可讀性優先）；頭目沿用既有規格（如 VOLTWYRM 450 deg/s／0.8s 一周）|
| `t_fire` | float (s) | ≥ 0 | 本次波次實際觸發的時間點 |
| `aim_reference_deg(t)` | float (deg) | [0°, 360°) | t 時刻的瞄準參考角，取模有界 |

**output range**：取模後天然有界。**權威協調**：`spiral_angular_speed_deg_s` 與 `fire_interval_seconds` 的比例決定臂間視覺間隙是否連續——設計建議 `fire_interval_seconds ≲ (360° / spiral_angular_speed_deg_s) / max(bullet_count, 1)`，否則旋轉彈幕會出現肉眼可見的空隙斷層（非硬性驗證規則，屬撰寫建議）。

**worked example**：`spiral_angular_speed_deg_s=90`，`bullet_count=3`（Ring, `ring_arc_deg=360`），`fire_interval_seconds=0.5s`：每次觸發時起始角前進 45°，三臂等距（120° 間隔）隨時間平滑旋轉，形成三臂旋轉風車彈幕。

---

### 4.6 密度縮放整合：Ring 多層與難度乘數的疊加

延伸 `bullet-system.md` §4.4 既有公式，加入 `ring_count` 作為第二個彈數放大來源：

**expression**：`actual_bullet_count = ceil( bullet_count_base × ring_count × bullet_density_mult[tier] )`

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `bullet_count_base` | int | [1, 20] | 單層單波次基礎彈數（`EmitterPatternSO.BulletCountBase`）|
| `ring_count` | int | [1, 4] | 同心圓層數（§4.2）；非 Ring Shape 恆為 1 |
| `bullet_density_mult[tier]` | float | {1.00, 1.25, 1.50, 2.00} | `difficulty-system.md` D.2/D.3 權威定義，本文件只讀取 |
| `actual_bullet_count` | int | 送入 §5.2 同屏硬上限前的中繼值，允許 ceil ±1 誤差 | 單次觸發實際生成的總彈數 |

**output range**：`actual_bullet_count` 本身無上限，但生成前必經 `bullet-system.md` §5.2「同屏彈數硬上限」與 §7「可讀性截斷優先於密度」把關——`readability_cap_priority` 永遠優先於本公式的輸出（承接既有鐵則，不新設另一套上限邏輯）。

**worked example**：D4（`bullet_density_mult=2.00`），`bullet_count_base=6`，`ring_count=2`（雙層同心圓）→ `actual_bullet_count = ceil(6 × 2 × 2.00) = 24` 顆／次——此數值必須送入 §5.2 硬上限檢查，若當下同屏已接近上限，依既有處置順序截斷（絕不犧牲可讀性）。

---

### 4.7 DiveSwoop 俯衝鎖定向量

**expression**：Phase 3 起飛方向 `dive_dir = normalize( (locked_target_x, screen_bottom_y) − (current_x, current_y_at_trigger) )`；`dive_speed = move_speed_px_s × swoop_dive_speed_mult`

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `swoop_trigger_y_pct` | float | (0.0, 1.0) | 觸發俯衝電報的螢幕高度百分比（0＝頂部，1＝底部）|
| `swoop_telegraph_s` | float (s) | [0.2, 1.0]（≥ `bullet-system.md` `telegraph_min_s` 精神一致）| 電報時長；期間鎖定 `locked_target_x` |
| `locked_target_x` | float (px) | 場地寬度內 | 電報結束瞬間玩家 X 座標，鎖定後不再更新（非持續追蹤）|
| `swoop_dive_speed_mult` | float | [1.3, 2.2] | 俯衝相對基準速度的倍率 |
| `dive_dir` | unit vector | 單位向量 | 俯衝方向，鎖定後不變 |
| `dive_speed` | float (px/s) | > move_speed_px_s | 俯衝實際速度純量 |

**output range**：`dive_dir` 為單位向量天然有界；`dive_speed` 下限被 `swoop_dive_speed_mult ≥ 1.3` 保證高於基準（俯衝必須明顯快於平時下降，才有「爆發威脅」的存在意義）。

**worked example**：`move_speed_px_s=120`，`swoop_dive_speed_mult=1.8`，電報結束時鎖定 `locked_target_x=200`，敵人當時位於 `(180, 300)`，`screen_bottom_y=960` → `dive_dir = normalize((20, 660)) ≈ (0.03, 0.9995)`（近乎垂直略偏右），`dive_speed = 216 px/s`。

---

### 4.8 HoverStrafe 位置

**expression**：`x(t) = entry_x + hover_strafe_range_px × triangle_wave( hover_strafe_speed_px_s × t / hover_strafe_range_px )`

`triangle_wave(u)` 為週期 4、振幅 [−1, 1] 的三角波（`u` 每經過 1 個單位，方向反轉一次），實作上等同「以 `hover_strafe_speed_px_s` 定速移動，碰到 `entry_x ± hover_strafe_range_px` 邊界即反向」的 ping-pong 位移。

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `entry_x` | float (px) | 場地寬度內 | 進場完成、開始 Hover 時的 X 座標（中心點）|
| `hover_strafe_range_px` | float (px) | [40, 160] | 左右巡邏半幅 |
| `hover_strafe_speed_px_s` | float (px/s) | [30, 100] | 橫移速度；恆定不隨難度縮放（與其餘移動速度一致，`stage-system.md` §I.2）|
| `t` | float (s) | ≥ 0 | 自進入 Hover 階段起的時間 |
| `x(t)` | float (px) | [entry_x − hover_strafe_range_px, entry_x + hover_strafe_range_px] | t 時刻的 X 座標，三角波天然有界 |

**output range**：`x(t)` 恆落於巡邏區間內，ping-pong 反向不需額外 clamp（三角波定義本身即為邊界反射）。

**worked example**：`entry_x=240`，`hover_strafe_range_px=80`，`hover_strafe_speed_px_s=50`：敵人在 `[160, 320]` 之間以 50px/s 來回巡邏，單趟耗時 `80×2/50=3.2s`。

---

## 5. 邊界情況 (Edge Cases)

| 邊界情況 | 處置規則 |
|---------|---------|
| **`Motion=Spiral` 搭配 `Trigger=OnDeath`** | 禁止組合並於 `OnValidate` 報錯。Spiral 的意義建立在「隨時間持續旋轉、跨多次波次觀察」；`OnDeath` 只觸發一次，沒有「隨時間」可言，組合在語意上無效 |
| **`ring_count > 1` 在 D4 密度下推高 `actual_bullet_count`（§4.6）** | 不特殊處理——沿用 `bullet-system.md` §5.2／§7 既有硬上限與「可讀性截斷優先於密度乘數」規則；本文件不新增第二套上限邏輯，只確保新算式的輸出一樣送進同一道關卡 |
| **`zigzag_amplitude_px` 在最小手機直向螢幕寬度下把子彈甩出安全走位帶** | `OnValidate` 依 `zigzag_amplitude_px` 安全上限（40px，見 §7）攔截；若单一 Emitter 的 `bullet_count × 2×amplitude` 超過典型手機可視寬度的 60%，設計師應改用較窄的 `SpreadAngleDeg` 或降低 `bullet_count`，而非放大振幅 |
| **`Motion=Arc` 曲率過強造成子彈迴旋「回頭」朝上飛回玩家後方畫面外再繞回** | 不視為 bug，但列為需人工把關的可讀性風險：`arc_curvature_deg_s` 安全上限（120 deg/s）搭配 `bullet_lifetime_seconds` 上限，設計時應手動檢查「該子彈的最大轉向角 = curvature × min(duration, lifetime)」是否會超過 180°（形成完全掉頭）；超過 180° 的「回旋鏢彈」是一個**明確排除於本次範圍外**的進階變體，留待未來需求評估（見 §9 開放問題）|
| **`HitboxScaleMult > BodyScale`（判定框大於視覺輪廓）** | `OnValidate` 報錯攔截——牴觸 §3.4 公平性規則，視為資料錯誤，不允許存檔 |
| **`Cross` 的 `bullet_count` 不能整除 360（如 5、7）** | `OnValidate` 警告（非阻斷）：Cross 承諾「對稱」視覺語言，非對稱間距會破壞玩家對 Cross 型的既有認知；建議值 {2, 3, 4, 6, 8} |
| **`Wall` 的 `bullet_count = 1`** | 允許但 `OnValidate` 提示：退化為單發直線彈，已可由 `AimedFan`（`SpreadAngleDeg=0`）表達，建議設計師改用該型別以維持語意清晰，非強制錯誤 |
| **`EmitterPatterns` 陣列中多個 Slot 的 `PhaseOffsetSeconds` 相同（同幀齊發）** | 允許——這是刻意設計選項（如 CARAPEX 的「聚肢齊射」即需要多 Emitter 同幀觸發）。僅在 §4.6 的密度加總超過同屏上限時才被既有護欄截斷，本身不是錯誤 |
| **菁英變體（`EliteDensityMult`）作用於陣列中每個 Emitter Slot** | 統一套用同一個 `EliteDensityMult` 到陣列內所有 Slot 的 `bullet_count`；若未來需要「菁英只加強其中一個 Emitter」的精細控制，屬本文件範圍外的後續擴充（見 §9）|
| **接觸型敵人（如 `ram_grub`）`EmitterPatterns` 為空陣列** | 沿用現行行為：Stage 系統偵測空陣列即跳過彈幕邏輯，不視為錯誤（現行 `EmitterPattern == null` 的判斷邏輯直接延伸為「陣列長度為 0」）|
| **`DiveSwoop` 電報期間（`swoop_telegraph_s`）敵人恰好被玩家擊殺** | 敵人立即進入既有死亡流程（含其 `EmitterPatterns` 中 `Trigger=OnDeath` 的彈幕，若有），Phase 3 俯衝不再觸發——與現行「死亡即停用行為」邏輯一致，不需新增特例 |
| **`HoverStrafe` 的巡邏範圍 `hover_strafe_range_px` 使敵人巡邏出戰場左右邊界** | `hover_strafe_range_px` 上限（160px，見 §7）搭配典型戰場寬度設計時人工檢查；不做執行期動態夾附（避免和 `entry_x`〔波次生成時的隨機進場位置〕產生非預期的邊界抖動），視為波次設計階段的既有責任（`stage-system.md` §D 波段撰寫規範）|

---

## 6. 系統相依 (Dependencies)

| 相依系統 | 方向 | 說明 |
|---------|------|------|
| `bullet-system.md` §4 | 雙向 | 本文件是其撰寫模型（Emitter 參數表）的具體 SO 落地與擴充；本文件新增的 Shape/Motion/Trigger 三軸**取代並細化** §4.2 原本的單一 `shape` 列舉——**需回頭更新 `bullet-system.md` §4.2/4.3 對照表**，使其與本文件一致（列為待辦，見 §9）。密度縮放讀取方向不變：本文件只讀 `bullet_density_mult[tier]`，不重新定義 |
| `difficulty-system.md` D.2/D.3 | 難度 → 本文件（輸入） | `bullet_density_mult[tier]` 權威仍在難度系統；本文件 §4.6 只是把 `ring_count` 接進既有公式的乘數鏈，不新增獨立密度來源 |
| `stage-system.md` §E（小怪名單）、§E.0（Prefab 架構）| 雙向 | §E.0 三組件 Prefab 架構為本文件擴充對象（§3.5）；本文件不改變波次資料結構（§D.2 仍只存 Prefab 引用）。**既有 10 種小怪的移動／彈幕文字描述（§E.1/§E.2）尚未套用新原型，需後續一輪更新分配哪些敵人使用 `DiveSwoop`／`HoverStrafe`／新 Shape 組合**（列為待辦，見 §9），本文件只提供可用的型別詞彙表 |
| `ADR-0001`（BulletSim 後端）| 執行期實作閘門 | `Motion=Spiral/Zigzag/Arc` 的逐幀軌跡運算（§4.3–4.5）屬於彈幕模擬 Job 的計算內容，其效能成本必須併入 ADR-0001 手機 sustain 驗證範圍。**本文件的 SO 欄位與撰寫工具本身現在即可實作（不受阻），但敵人在關卡中實際「動起來」的 Motion 執行邏輯，需待 ADR-0001 轉 Accepted** |
| `ADR-0003`（資料驅動 SO 設定）| 本文件遵循 | 所有新欄位（Shape/Motion/Trigger 參數、`BodyScale`、`HitboxScaleMult`、`EmitterSlot` 陣列、移動新參數）均為 SO 欄位，無硬編碼數值 |
| `kaiju/01-carapex.md`・`02-lacera.md`・`03-voltwyrm.md` | 未來受益方（本文件不修改）| 三頭目現行以純文字描述的彈幕模式（螯牙交叉、蛇陣螺旋等）語意上已對應本文件的 Shape/Motion 組合（如 `Cross`+`Straight`、`Ring`+`Spiral`），但 `KaijuDef.cs` 目前尚未接上 `EmitterPatternSO`——頭目資產化與本文件對齊為明確的後續工作，不在本次範圍內 |
| `game-concept.md`（視覺 / 可讀性鐵則）| 本文件遵循 | 新 Motion 型別（尤其 Zigzag／Arc）仍須維持暖色調色盤、單點玩家判定恆亮、電報保證等既有硬性護欄；§3.4 體型脫鉤規則是對「判定點至上」公平性精神的敵人側延伸 |

---

---

## 7. 調校旋鈕 (Tuning Knobs)

（待撰寫）

---

## 8. 驗收標準 (Acceptance Criteria)

（待撰寫）

---

*文件版本：0.1.0（草稿進行中）*
*作者：Systems Designer Agent*
*狀態：Draft — 撰寫層設計；執行期實作（Motion 軌跡運算）閘於 ADR-0001*
