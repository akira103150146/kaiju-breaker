# 敵人 Roster 擴充：新小怪 + 菁英階正式化 (Enemy Roster Expansion)
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/enemy-roster-expansion.md*
*最後更新: 2026-07-08*
*狀態: 草稿完整（導演核可「更多小怪菁英」）*
*用途: 在既有凍結 10 隻雜兵（stage-system.md §E.1）之外新增 6 種小怪，並正式化菁英階（EnemyTier）。新小怪刻意「預告」新頭目的機制（如既有 side_weaver 預告 LACERA、column_grunt 預告 VOLTWYRM）。*
*相依: `stage-system.md`（§E.1 既有 10 隻）、`enemy-tier-system.md`（EnemyTier 定義）、`bullet-pattern-diversity.md`（移動/彈幕型）、`00-roster-overview.md`（8 頭目）。*

---

## 1. 概覽 (Overview)

既有 10 隻雜兵覆蓋了「純衝撞 / 三叉扇 / 瞄準 / 死亡環 / 護盾 / 列陣 / 蛇行 / 分裂 / 自爆 / 高速掠過」。本擴充新增 **6 種小怪**，每種都：(a) 引入一個**新的移動或彈幕組合**，(b) **預告一隻新頭目**的核心機制，讓玩家在道中先學會應對。並把**菁英階**從 stat-override 概念正式化為 `EnemyTier` 列舉 + 菁英規則（菁英＝武器莢艙來源）。

**設計鐵則**（沿用）：敵彈暖色、telegraph ≥ 0.3s、非追蹤（子彈直線/固定角，不鎖玩家）、難度只縮放密度/數量。變化來自「移動 SO × emitter SO × 小機制」的組合，而非增加單體複雜度。

---

## 2. 新小怪一覽 (New Mob Table)

| # | id | 中文 | 移動原型 | Emitter | 特殊機制 | HpTier | 預告頭目 |
|---|----|------|----------|---------|----------|--------|----------|
| 11 | `spore_mite` | 孢子蟲 | StraightRush（慢，飄向玩家） | 無（接觸） | 由 BROODCORE 卵囊/`splitter` 生成；量大命賤 | T1 | BROODCORE |
| 12 | `spiral_turret` | 螺旋砲塔 | Hover（到位懸停） | **Spiral（新）**：旋轉放射臂 | 定點旋轉噴嘴，持續螺旋 | T2 | EMBERWING / NULLSPIRE |
| 13 | `diver` | 俯衝機 | **DiveSwoop（新）**：弧線俯衝掠過再拉起 | Aimed 1（俯衝頂點） | 高速俯衝壓迫，逼走位 | T1 | （通用高壓） |
| 14 | `prism_drone` | 稜鏡機 | HorizontalDrift | Radial 5（折射扇，冷藍） | **正面折射甲**：擋 3 次正面命中（沿用 shield_flier DirectionalShield），L3/軟化破 | T2 | PRISMSHELL |
| 15 | `bubbler` | 吐泡獸 | Hover（低位懸停） | Aimed 寬扇（**慢密泡幕**，暖青） | 慢速密集彈牆，填縫壓迫 | T2 | TIDEMAW |
| 16 | `void_lancer` | 虛空槍手 | **HoverStrafe（新）**：懸停＋橫向平移 | Aimed 窄 2（虛空矛，冷紫） | 邊平移邊狙擊，難預判落點 | T1 | NULLSPIRE |

> `spore_mite` 也給 `splitter` 的分裂沿用（取代原 `splitter_mini`，統一小怪資產）。

### 需要的新列舉值（給 schema / 實作）
- **`MovementType`** 新增：`DiveSwoop`（進場弧線俯衝、掠過後拉起離場，帶 `EntryAngleDeg`）、`HoverStrafe`（到位後於一段 X 區間來回平移）。對齊 `bullet-pattern-diversity.md` 既有提案。
- **`EmitterPatternType`** 新增：`Spiral`（放射臂隨時間旋轉；需 `SpinRateDegPerSec` 參數）。同時供頭目部位（EMBERWING 燼臂、NULLSPIRE 脊柱、PRISMSHELL 折射）使用。
- 兩者皆**向後相容**（新 enum 值追加在尾端，既有 SO 不受影響）。

---

## 3. 新小怪設計膠囊 (New Mob Capsules)

### 3.1 `spore_mite` 孢子蟲（T1）
- **行為**：從卵囊/分裂點生成，慢速（~120 px/s）飄向玩家當前位置後直線落下，不轉向。無主動火力，接觸傷害低。
- **威脅**：單隻無害，成群才有壓力——BROODCORE 的卵囊持續生產，玩家若不破卵囊會被孢子蟲淹沒。這正是「破部位＝減壓」的道中預告。
- **參數**：HP T1（低），contact_damage 小，pointValue 低。移動 StraightRush（intro_speed_mult 1.0，慢速）。

### 3.2 `spiral_turret` 螺旋砲塔（T2）
- **行為**：HorizontalDrift 進場 → 到位 Hover 定點；噴嘴以 `SpinRateDegPerSec` 旋轉，持續放射 Spiral 臂（每臂數發暖橙彈）。
- **威脅**：螺旋彈幕製造「旋轉的安全縫」，玩家要順著縫移動——預告 EMBERWING 燼臂與 NULLSPIRE 脊柱螺旋。定點故可被集中火力優先清除。
- **參數**：Spiral emitter（BulletCountBase 每臂 3、臂數 3、SpinRate 90°/s、暖橙、telegraph 0.4s）。HP T2。

### 3.3 `diver` 俯衝機（T1）
- **行為**：DiveSwoop——從畫面上緣以 `EntryAngleDeg`（~30–45°）弧線俯衝，掠過玩家高度後拉起往側邊離場（不停留）。俯衝頂點放 1 發 Aimed。
- **威脅**：高速穿越製造瞬間壓力＋一發精準彈，逼玩家即時橫移；不糾纏。單發火力低但軌跡兇。
- **參數**：移動 DiveSwoop（速度 ~300 px/s、EntryAngleDeg 35）。Aimed 1（0.3s telegraph）。HP T1。

### 3.4 `prism_drone` 稜鏡機（T2）
- **行為**：HorizontalDrift 橫向漂移；放 Radial 5 折射扇（冷藍，較快）。**正面折射甲**吸收 3 次正面命中（DirectionalShield，沿用 `shield_flier` 機制），L3 穿透或熱軟化可破甲。
- **威脅**：折射扇 + 正面甲＝「先破甲再打」的道中版，預告 PRISMSHELL 的裝甲晶面。冷藍彈是晶簇系刻意例外——需高飽和 + telegraph 與玩家冷彈區分。
- **參數**：Radial 5（速度較快、telegraph 0.3s）。DirectionalShield GateHp=3 frontal。HP T2。

### 3.5 `bubbler` 吐泡獸（T2）
- **行為**：Hover 低位懸停；週期吐 Aimed 寬扇的**慢速密集泡幕**（暖青，慢彈久留，填滿縫隙）。
- **威脅**：慢密彈牆考驗微走位與耐心，預告 TIDEMAW 的泡幕壓制。彈速慢＝可讀但佔空間，逼玩家找縫穿。
- **參數**：Aimed 寬扇（BulletCountBase 6、bulletSpeed 慢 ~90 px/s、lifetime 長、telegraph 0.5s）。HP T2。

### 3.6 `void_lancer` 虛空槍手（T1）
- **行為**：HoverStrafe——降到定高後在一段 X 區間來回平移，邊移邊放 Aimed 窄 2（虛空矛，冷紫）。
- **威脅**：移動中射擊使落點難預判，預告 NULLSPIRE 公轉衛星。冷紫矛為虛空系例外色，需亮＋telegraph。
- **參數**：HoverStrafe（strafe X 幅 ~2.5 world、平移速度中）。Aimed 窄 2（telegraph 0.35s）。HP T1。

---

## 4. 菁英階正式化 (Elite Tier Formalization)

### 4.1 `EnemyTier` 列舉（新建 `.cs`）
```csharp
public enum EnemyTier { Trash = 0, Elite = 1, Mid = 2, Boss = 3 }
```
沿用 `enemy-tier-system.md` C.1。`EnemyDef` 現有 `IsElite` 布林保留（表面相容），另加 `Tier` 欄位供 hit-feel 分級 + 掉落 + 生成邏輯讀取（`IsElite == (Tier == Elite)`，OnValidate 保持一致）。

### 4.2 菁英規則
- **菁英＝base 變體**：共用同一 Movement/Emitter SO，只由 `EnemyDef` stat override 區分：`EliteHpMult`(2.5)、`EliteDensityMult`(1.5)、`EliteShardBonus`(+3)、`EliteAuraColor`(#FFAA33 暖金光暈)。
- **菁英是武器莢艙唯一來源**（stage-system.md / PodDropTracker）：擊殺菁英→掉循環武器莢艙（pool-typed，徘徊可達帶 ~12s）。雜兵掉碎片/P/M，不掉莢艙。
- **菁英帶 1 個 Gate**（建議）：ScalarGate（一段護盾 HP 先破才露弱點）或 DirectionalShield，讓菁英「值得停下來打」。
- **菁英光暈 + 死亡回饋**：EliteAuraColor 外框；死亡走 hit-feel 的 Elite 檔（介於 Trash 與 Mid 之間）。

### 4.3 菁英名冊（base → elite）
可菁英化的基底（既有＋新）：`tri_shot_elite`、`aimed_gun_elite`、`shield_flier_elite`、`side_weaver_elite`、`column_grunt_elite`、`splitter_elite`、`spiral_turret_elite`、`prism_drone_elite`、`bubbler_elite`、`void_lancer_elite`。
- `ram_grub` / `kamikaze` / `spore_mite` / `diver`（純衝撞或掠過型）**不適合當莢艙菁英**（打不到、不停留）；`kamikaze_elite` 例外＝AoE ×1.5 的自爆威脅型（不掉莢艙，純威脅）。

---

## 5. 道中編排提示 (Encounter Composition Notes)

- **預告原則**：每個新頭目所在關卡的道中，混入其「預告小怪」（如 TIDEMAW 關前放 `bubbler`、PRISMSHELL 關前放 `prism_drone`），讓玩家先學機制。
- **菁英節奏**：每段落 ≥1 菁英當莢艙來源（PodDropTracker 保底）。
- **可讀性**：同屏暖色敵彈 + 冷色玩家彈；晶簇/虛空系的冷藍/冷紫敵彈為刻意例外，一律高飽和 + telegraph ≥0.3s 以免與玩家彈混淆。

---

## 6. 驗收標準 (Acceptance Criteria)

- [ ] AC-01：6 種新小怪各有 Movement SO + Emitter SO（`spore_mite` 除外＝接觸型）+ EnemyDef，可在道中生成、移動、發射。
- [ ] AC-02：新增 `MovementType.DiveSwoop`/`HoverStrafe` 與 `EmitterPatternType.Spiral` 三值，執行系統正確驅動；既有 448 EditMode 測試不受影響（向後相容）。
- [ ] AC-03：`EnemyTier` 列舉建立；`EnemyDef.Tier` 與 `IsElite` 一致；菁英擊殺掉循環武器莢艙、雜兵不掉。
- [ ] AC-04：菁英套用 EliteHpMult/EliteDensityMult/EliteShardBonus/EliteAuraColor；帶 1 Gate。
- [ ] AC-05：`prism_drone` 正面折射甲吸收 3 次正面命中、L3/軟化可破（沿用 DirectionalShield）。
- [ ] AC-06：難度 D1–D4 只改新小怪的彈幕密度/生成數；彈速、HP、掉落不變。
- [ ] AC-07：新小怪暖色彈（晶簇/虛空冷色例外者高飽和 + telegraph ≥0.3s），非追蹤。
```
