# 武器分級成長 + D₀ 等功率資料模型 (Weapon Tiering & Equal-Power Data Model)
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/weapon-tiering-and-equal-power.md*
*建立日期: 2026-07-03*
*狀態: 待導演審閱 — 尚未核准；核准後才可撰寫 SO 欄位程式碼與 H.1/H.2/H.7 測試*
*來源文件: design/gdd/weapon-system.md（LOCKED，本文件不修改其內容，僅補上缺失的分級/欄位資料）| design/balance/weapon-d0-equal-power-analysis.md（本文件的直接前置分析，§3/§4 建議值於此文件中被採納、修正或推翻——逐項標明）| design/feedback/2026-07-02-改進意見與劇情草案.md §A.3*
*來源程式: Assets/_Project/Scripts/Content/WeaponDef.cs, WeaponBalanceConfig.cs, EmitterPatternSO.cs | Assets/_Project/Scripts/Weapons/*.cs（L1SpreadLaser, MissileWeaponBase, M2SwarmLauncher 等既有實作，本文件對照其行為撰寫）*
*已核准之導演決策（2026-07-03，本文件據此展開，不重新討論）：*
1. *M2 蜂群飛彈採 **Option B** — 多重齊射（burst salvo）成為所有 Tier 的基礎行為，不再只是 Tier-3 專屬；同時解決「T1 彈匣 vs T3 彈匣」互相矛盾的問題。*
2. *L1 散波雷射的波束數階梯**綁定既有 Tier 0–3**：Tier 0/1/2/3 = 2/3/4/5 道波束；等功率的「每束熱量均分」規則必須在任何波束數下都成立（全中總熱量恆等於表定滿速率）。*

---

## A. 概覽 (Overview)

本文件是 `weapon-d0-equal-power-analysis.md` 的直接延伸，把該文件第 4 節列出的資料模型缺口（`EffectiveHitRate`、各武器 `ShotInterval`、`EqualPowerBandTolerance`）轉為可實作的 SO 欄位規格，並疊加兩項導演已核准的分級成長設計：M2 蜂群飛彈的「Option B」基礎機制重構，以及 L1 散波雷射波束數與既有 Tier 0–3 的正式綁定。同時，本文件把這兩項分級成長套用到「等功率 Sustained_Output 公式」上逐一驗算——**這個驗算過程本身額外挖出兩個先前未被發現的問題**（L3 Tier-3「共鳴擴散」嚴重超標 +80%、M4 Tier-3「子母炸彈」超標 +20%），一併列入待裁決清單。最後，本文件把「子彈大小分級」（回饋意見 §A.3 的另一半訴求）定義為資料模型：玩家武器逐 Tier 的視覺/輸出成長，以及敵彈的大/中/小威脅分級。所有數值均為**可再調的預設值**——本文件的交付重點是公式正確性與完全資料驅動的結構（每個旋鈕都是帶安全範圍的 SO 欄位），不是鎖定最終數字。

---

## B. 玩家幻想 (Player Fantasy)

本文件不引入新的玩家可感知機制，而是把既有武器系統的兩個幻想承諾**做到言行一致**：

**「這把武器是我的，而且它在長大」** — 回饋意見的核心訴求是玩家強化武器時要有看得見、打起來有感覺的成長曲線（散彈 2→3→4→5 條的具體訴求），不只是後端數值表格裡看不見的百分比。本文件把這個訴求轉成資料模型：每把武器在 Tier 0–3 之間有明確定義的視覺/結構成長點，讓玩家的強化投資有直接可讀的回饋。

**「八把武器真的一樣強」** — `weapon-system.md` B 章承諾「橫向選擇」是本系統的核心支柱：玩家選武器是身份選擇，不是效率計算。這個承諾能否兌現，完全取決於 D₀ 等功率約束是否真的對全部 8 把武器成立——本文件把上一份分析文件標出的每一個資料缺口補上明確數字，讓這個支柱從「文件裡的主張」變成「測試能驗證的事實」。

---

## C. 詳細規則 (Detailed Rules)

### C.1 本文件與既有文件的關係

- `weapon-system.md`（LOCKED）— 本文件的數值來源與規則主體，不修改其內容。
- `weapon-d0-equal-power-analysis.md`（Draft，尚未核准）— 本文件是它的直接延伸；該文件第 3/4 節的建議值，凡是本文件明確採納者標記「採納」，凡是本文件因應 M2 Option B / L1 分級決策而推翻或修正者標記「修正」。
- 本文件核准後，`gameplay-programmer`（或對應實作者）依 G 章欄位清單新增/修改 `WeaponDef.cs`、`WeaponBalanceConfig.cs`、`EmitterPatternSO.cs`，並依 H 章撰寫 `weapon_dps_equivalence_test`、`weapon_loadout_matrix_test`、`tier3_identity_depth_test`。

### C.2 M2 蜂群飛彈 — Option B 基礎機制重構

**問題回顧**（詳見分析文件 §3.2 M2 段落）：M2 原始 Sustained_Output 僅 0.200×D₀（缺口 -78%，全武器庫最嚴重），且現有安全範圍內任何單一旋鈕都補不滿缺口；分析文件提出的「Option B」延伸雛型（3×8 枚齊射）會讓基礎彈匣（24 發）反過來**超過** Tier-3 彈匣（12 發），造成「升級後彈匣變少」的設計矛盾——這正是本文件必須解決的衝突。

**重構後設計 — 「連環蜂巢」（Chain Hive）**：

放棄「Tier-3 才有齊射拆分」的舊模型，改為：**齊射拆分本身就是 M2 從 Tier 0 開始的核心行為**，Tier 0–2 與 Tier-3 使用**完全相同的彈匣結構**（不再有「T1 彈匣」與「T3 彈匣」兩套互斥數字），差異只在於 Tier-3 為齊射加上智慧鎖定，是「深化身份」而非「加量」。

| 屬性 | Tier 0–2（基礎） | Tier-3（連環蜂巢：飽和點名） |
|------|------------------|------------------------------|
| 每次扳機的齊射結構 | 3 次齊射，每次 8 枚，齊射間 `m2_inter_salvo_interval`（0.4s）微冷卻 | **相同**：3 次齊射，每次 8 枚，同樣的微冷卻節奏 |
| 彈匣總量（一次扳機耗盡） | 24 枚（= 3×8） | **相同**：24 枚 |
| 每枚微型飛彈輸出 | `m2_dmg_per_missile_mult`（0.25×D₀） | **相同**：0.25×D₀ |
| 換彈時間 | `m2_reload_time`（5.0s，維持全飛彈系最長換彈） | **相同**：5.0s |
| 目標選擇 | 每次齊射都沿玩家瞄準錐（`m2_cone_width_pct`）散佈 | **若當前戰場有任一部位處於 SOFTENED**：該次齊射的扇形改為**集中偏向 BU 進度最高的已軟化部位**（不再單純沿玩家瞄準方向散佈）；無軟化部位時行為與基礎 Tier 完全相同 |

**設計意圖**：Tier-3 的「飽和點名」不改動任何 D₀ 預算公式的輸入（彈匣量、每枚輸出、齊射間隔、換彈時間全部不變），純粹是**目標選擇品質**的提升——呼應「這把武器讀懂戰場」的雙軌機制幻想（C.2 B 節），且與 M1 Tier-3「熱源引導」（第 3 枚飛彈自動鎖定最熱部位）走同一套設計語言，維持全武器庫 Tier-3 機制的一致性。這個設計直接滿足導演決策「T3 深化身份、不只是加量」的要求，且讓 H.7（Tier-3 不可讓 TTB 縮短超過 15%）在數學上**自動成立**（見 D.4）。

**淘汰的舊欄位**：`m2_t3_mag_count`（12）與 `m2_t3_burst_micro_cd`（1.0s）不再需要——由新的 tier-通用欄位 `m2_salvo_count`／`m2_inter_salvo_interval` 取代（見 G.3）。

**與現有程式的落差**：`M2SwarmLauncher.cs` 目前的 `MagCapacity`／`TryFire`／`Tick` 已經實作了「拆成多次齊射、齊射間有微冷卻、Tick 自動觸發下一次齊射」的骨架（目前僅在 `CurrentTier == 3` 時啟用，拆成 2 次 6 枚）——本重構只需要把這個骨架從「僅 Tier-3 啟用、寫死 2 次」改為「所有 Tier 都啟用、齊射次數與間隔改讀 `m2_salvo_count`／`m2_inter_salvo_interval`」，並在 `PerMicroBreakDelta` 改讀新的 `m2_dmg_per_missile_mult` 欄位（取代目前寫死的 `Balance.BuPerD0 / Def.M2MicroCount`）。Tier-3 額外的目標選擇邏輯（鎖定已軟化部位）是**新增**行為，需要 `IPartStateQuery` 提供「取得 BU 進度最高的已軟化部位」查詢（比照 `GetHottestAlivePartId` 的既有模式新增 `GetMostSoftenedPartId` 或等效介面）。

### C.3 L1 散波雷射 — 波束數階梯綁定 Tier 0–3

**現況**：`L1SpreadLaser.cs` 目前波束數**不是資料欄位**，而是呼叫端（scene shell）依 Tier 決定要打幾條 raycast 後傳入陣列長度；程式註解明講「3 at Tier < 3, 4 at Tier 3」，是硬編碼在呼叫端的邏輯,不是 `WeaponDef` 的可調欄位。

**新設計**：新增 `l1_beam_count_by_tier`（4 個整數：Tier 0/1/2/3 → 2/3/4/5），做為波束數的**唯一權威來源**——scene shell 改讀這個陣列決定要打幾條 raycast，不再自行判斷 Tier。

**等功率不變式（本文件最關鍵的一條規則）**：無論波束數為何，**全中情境下的總熱量輸出恆等於 `l1_h_rate_full`**（25 HU/s = 1.00×D₀）——`L1SpreadLaser.FireFrame` 現有實作已經是 `perBeamDelta = L1HRateFull * deltaTime / beamCount`（總量除以波束數，逐束均分），這個公式**在任何波束數下都自動保持全中總量不變**,不需要為 4 個 Tier 各寫一個總速率。也就是說：

| Tier | 波束數 | 全中時每束熱量速率 | 全中時總熱量速率 |
|------|--------|---------------------|-------------------|
| 0 | 2 | 12.5 HU/s | 25 HU/s = 1.00×D₀ |
| 1 | 3 | 8.33 HU/s | 25 HU/s = 1.00×D₀ |
| 2 | 4 | 6.25 HU/s | 25 HU/s = 1.00×D₀ |
| 3 | 5 | 5.0 HU/s | 25 HU/s = 1.00×D₀ |

**僅中央束命中**（`l1_h_rate_center`，8.3 HU/s）維持 Tier 不變值，不隨波束數縮放——這是刻意保留的「精準度不足時的最差情境」錨點，波束變多不代表中央束更強，只代表覆蓋更廣。

**Tier-3「全幅掃蕩」殘熱焰機制不變**（波束擴充只是把既有的「4 道波束」規則泛化成階梯，殘熱焰仍是 Tier-3 專屬深化機制，見 D.4 驗算——此機制對單目標持續全中的 Sustained_Output **無貢獻**，因為殘熱只在雷射離開部位後才生效，天然滿足 H.7）。

**跨文件影響**：`bullet-system.md` §8.1 目前寫「L1 = 3–4 條扇形 raycast」——與新的 2→3→4→5 階梯不一致，需要在該文件補一則非破壞性附錄（比照 `weapon-system.md` F.1 已有的附錄慣例），本文件不越權直接修改該文件。

### C.4 其他武器的逐 Tier 成長

回饋意見要求「依強化程度不同大小」**不只限於散彈**。既有程式的實作模式（除 L1 外）全部是「Tier 0–2 數值不變、Tier-3 才加機制」的二元分支（`CurrentTier == 3 ? … : …`），這與 L1 的四階連續階梯不同——**本文件不強行把所有武器都改成 L1 式的連續階梯**，理由：

1. 只有 L1 的「波束數」是回饋意見明確點名的例子，且它天生適合連續階梯（波束是離散、可數的視覺單位）。
2. 其餘武器（單束雷射、單發飛彈/魚雷/炸彈）沒有天然的「可數視覺單位」隨 Tier 遞增——強行拆出 T1/T2 中繼值只會製造更多可能違反 H.7 的組合（見 D.4 的 L3／M4 案例，兩者都是在把「加量」當成「深化」時才出問題）。

因此，其餘 6 把武器的成長模型維持二元分支，但**新增純視覺成長**（不影響 D₀ 公式輸入）滿足「玩家看得到強化」的訴求：

| 武器 | Tier 0–2 視覺成長旋鈕（新增） | Tier-3 既有機制（不變，見 weapon-system.md C.4/C.5） |
|------|-------------------------------|--------------------------------------------------------|
| L2 集束雷射 | `l2_beam_width_mult_by_tier`（束寬視覺倍率） | 破點漣漪 + 微追蹤 |
| L3 波動砲 | `l3_charge_visual_scale_by_tier`（蓄力球視覺縮放） | 共鳴擴散（**本文件 D.4 發現嚴重超標，見 I 章開放問題 #1**） |
| L4 穿透雷射 | `l4_beam_width_mult_by_tier`（貫穿線視覺粗細） | 熱殘影 |
| M1 追蹤飛彈 | `m1_missile_scale_by_tier`（飛彈視覺縮放） | 熱源引導（第 3 枚鎖熱） |
| M3 穿甲魚雷 | `m3_torpedo_scale_by_tier`（魚雷視覺縮放） | 穿甲爆破鏈 |
| M4 叢集炸彈 | `m4_bomb_scale_by_tier`（炸彈視覺縮放） | 子母炸彈（**本文件 D.4 發現輕微超標，建議收緊 `m4_t3_child_dmg_pct`**） |

M2 蜂群飛彈不需要獨立的視覺縮放階梯——它的「成長感」已經由 C.2 的齊射結構本身提供（Tier 0 起就是 3 次連續齊射的密集蜂群視覺，Tier-3 疊加目標鎖定的行為變化，玩家能感覺到「這批飛彈變聰明了」而非單純變大）。

### C.5 子彈大小分級資料模型

回饋意見 §A.3「敵方：子彈要有大顆/小顆之分（視覺+判定+威脅感分級）」——本節定義資料模型；實際視覺渲染受 BulletSim（ADR-0001，效能 spike 閘門中）限制，暫不實作，但資料欄位現在就能定義並被 Stage/敵人 prefab 設定消費。

**設計原則**：依 `bullet-system.md` §6.1「玩家判定 = 單點」，敵彈的「大小」實際上定義的是**敵彈自身的碰撞半徑**（點 vs 圓的距離判定），因此子彈大小直接等於威脅範圍，不是純美術裝飾。視覺半徑刻意略大於真實判定半徑（約 +25% 寬容邊界），依循彈幕遊戲類型慣例的「寬容判定」公平性設計，也呼應 `bullet-system.md` §7「可讀性護欄」的精神——玩家看到的威脅範圍應該略大於真實危險範圍，而不是相反。

**新增全域 SO：`EnemyBulletSizeConfig`**（3 個分級，供全部敵彈 prefab 共用同一套基準，避免每個 `EmitterPatternSO` 各自重複輸入半徑數字）：

| 分級 | 判定半徑 (px) | 視覺半徑 (px) | 威脅權重 (Threat Weight) | 典型用途 |
|------|---------------|----------------|---------------------------|----------|
| Small | 4（範圍 [3, 6]） | 5（範圍 [4, 8]） | 1.0 | 密集雜兵彈幕、同心圓放射（回饋意見第 1 點）；數量多、單顆威脅低 |
| Medium | 7（範圍 [6, 10]） | 9（範圍 [7, 12]） | 1.8 | 一般瞄準彈、Z 字彈幕 |
| Large | 12（範圍 [10, 18]） | 15（範圍 [12, 22]） | 3.0 | 曲射彈、菁英/BOSS 高電報大彈 |

`EmitterPatternSO` 新增一個欄位 `_sizeClass`（enum: Small/Medium/Large，預設 Medium），索引到 `EnemyBulletSizeConfig`——不在每個 pattern 資產裡重複填半徑數字（ADR-0003「單一事實來源」原則）。`ThreatWeight` 目前只作為未來密度/難度調校的鉤子（例如「每次齊射的威脅總預算」機制），本文件不展開其消費端設計，留待 `difficulty-system.md` 或 pattern 設計文件決定是否採用。

**Small/Medium/Large 與現有 `EmitterPatternType` 的建議搭配**（指引,非硬規則——依回饋意見第 1 點「寫進各敵人的 prefab」,每隻敵人自行決定）：RingBurst/密集 Radial 傾向 Small；Aimed 瞄準彈傾向 Medium；高電報、低頻率的曲射或 BOSS 專屬彈傾向 Large。

---

## D. 公式 (Formulas)

### D.1 主公式 — 修正後的 Sustained_Output

**沿用 `weapon-system.md` D.1 的定義，補上分析文件 §4 指出缺失的兩個修正項**：命中率修正、彈匣週期的顯式時間輸入。

```
Sustained_Output = (Total_Output_per_Mag_raw × EffectiveHitRate) / (Mag_Duration + Reload_Time)
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `Total_Output_per_Mag_raw` | float | > 0（單位：×D₀ 或等效 PU） | 一整個彈匣/週期在最優命中、100% 命中率下的總輸出 |
| `EffectiveHitRate` | float | (0, 1] | 命中率修正係數；反映武器判定寬容度/目標移動速度造成的實戰折損 |
| `Mag_Duration` | float | ≥ 0（秒） | 打完一整個彈匣/週期所需時間 |
| `Reload_Time` | float | ≥ 0（秒） | 換彈/冷卻時間 |
| `Sustained_Output` | float | 不設硬夾限，測試斷言落在 `D0Reference × [1-tol, 1+tol]` | 持續等效輸出，單位與 `D0Reference` 相同（PU/s） |

**輸出範圍**：公式本身不夾限（unbounded），由 H.1 測試斷言結果落在容忍帶內；容忍帶寬度由新欄位 `EqualPowerBandTolerance`（見 G.1）控制，不再是寫死在測試檔案裡的 `±10%`。

**範例（M3 穿甲魚雷，未軟化基礎值，採納分析文件 §3.1 建議的 `EffectiveHitRate=0.80`）**：

```
Total_Output_per_Mag_raw = 9×D₀（3 枚 × 3×D₀）
EffectiveHitRate = 0.80
Mag_Duration = 3 × m3_shot_interval(1.0s) = 3.0s
Reload_Time = 4.0s
Sustained_Output = (9×0.80) / (3.0+4.0) = 7.2/7.0 ≈ 1.029×D₀ ✅
```

### D.2 連續雷射（L1 全中／L2／L4）— Mag_Duration 恆為 0

```
Sustained_Output = H_rate_full × EffectiveHitRate / HuPerD0
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `H_rate_full` | float | > 0（HU/s） | 該雷射的表定滿速率（`l1_h_rate_full` / `l2_h_rate` / `l4_h_rate`） |
| `HuPerD0` | float | [15, 40]（`WeaponBalanceConfig`） | 熱量換算常數 |

**輸出範圍**：任意觀察窗口 T 下 `Mag_Duration + Reload_Time = T`，`Total_Output = H_rate×EffectiveHitRate×T`，兩者相除窗口 T 自行消去（沿用分析文件假設 B）——結果恆等於 `H_rate×EffectiveHitRate/HuPerD0`，與窗口長度無關，故本文件不再要求連續雷射有「30 秒視窗」測試邏輯,直接用比值斷言。

**範例（L2 集束雷射，採納分析文件 §3.1 建議的 `EffectiveHitRate=0.65`）**：

```
Sustained_Output = 37.5 × 0.65 / 25 = 24.375/25 = 0.975×D₀ ✅
```

**L1 特例**：因 C.3 的等功率不變式，`H_rate_full` 恆等於 25 HU/s，**與 Tier 無關**（波束數只改變逐束分配，不改變總和）——L1 在全部 4 個 Tier 下的 Sustained_Output 都是 25×1.0/25 = **1.00×D₀**，這是本文件對「分級成長不得破壞等功率」要求的最乾淨示範。

### D.3 L3 波動砲 — 雙模式分別公式化

**短脈衝（Tap）模式** — 沿用 D.2 連續雷射公式，但**明確排除在 ±tolerance 斷言之外**（採納分析文件開放問題 #2 的建議立場，作為本文件的預設工作假設，待導演最終拍板——見 I 章 #2）：

```
Sustained_Output_tap = l3_tap_output_mult × HuPerD0 / HuPerD0 = l3_tap_output_mult × D₀ = 0.60×D₀
```

（`l3_tap_output_mult` 本身已是 ×D₀ 單位，換算後即為自身數值——這條公式只是確認 Tap 模式恆等於 0.60×D₀，不受任何新欄位影響。）

**蓄力震波（Charge）模式** — 沿用主公式的離散彈匣讀法：

```
Sustained_Output_charge = l3_charge_output_mult / (l3_charge_time + l3_charge_cooldown)
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `l3_charge_output_mult` | float | [2.0, 3.0] | 蓄力震波輸出（×D₀），已是既有欄位 |
| `l3_charge_time` | float | [1.2, 2.0] | 蓄力所需長按時間（秒），既有欄位 |
| `l3_charge_cooldown` | float | [1.5, 2.5] | 釋放後冷卻時間（秒），既有欄位 |

**建議調整既有欄位預設值**（採納分析文件 §3.2 建議，兩者都壓到各自安全範圍下限）：`l3_charge_time` 1.5→**1.2**，`l3_charge_cooldown` 2.0→**1.5**。

**範例**：

```
新週期 = 1.2 + 1.5 = 2.7s
Sustained_Output_charge = 2.5 / 2.7 ≈ 0.926×D₀ ✅
```

蓄力模式成為 L3 的等功率驗證基準（H.1 只斷言 Charge 模式；Tap 模式的 0.60×D₀ 作為文件記錄的「刻意弱化填充模式」數值，不進 H.1 斷言迴圈）。

**注意（修正分析文件 §4 的一項提案）**：分析文件建議新增 `L3TapInterval` 欄位；但 `L3WaveCannon.cs` 的現有實作把 Tap 模式做成「持續按住＝連續輸出」（`heatDelta = L3TapOutputMult × HuPerD0 × heldTime`），本質上已經是 D.2 的連續速率模型，**不存在「發與發之間的間隔」這個概念**——`L3TapInterval` 是分析文件基於「Tap 是離散連發」的誤讀所提出的欄位，本文件建議**不新增此欄位**，改用上面的連續速率公式直接驗證，已經能與 D.2 的 15 HU/s（= 0.6×D₀×25）交叉核對一致。

### D.4 離散彈匣飛彈（M1／M3／M4）— Mag_Duration = 發數 × ShotInterval

```
Mag_Duration = ShotCount × ShotInterval
Total_Output_per_Mag_raw = ShotCount × Output_per_Shot
Sustained_Output = (Total_Output_per_Mag_raw × EffectiveHitRate) / (Mag_Duration + Reload_Time)
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `ShotCount` | int | ≥ 1 | 一個彈匣週期內的射擊次數（非個別飛彈數——M1 一次射擊發射 2 枚算 1 次 ShotCount） |
| `ShotInterval` | float | 依武器而異，見 G.2 | 每次射擊之間的間隔（秒），**本文件新增的核心缺口欄位** |
| `Output_per_Shot` | float | > 0（×D₀） | 每次射擊的總輸出（多枚飛彈時已加總） |

**M1 追蹤飛彈範例（採納分析文件路徑 A：`m1_shot_interval = 0.10s`）**：

```
ShotCount = 3（6 發彈匣 ÷ 2 枚/發）
Output_per_Shot = 2 × 0.5×D₀ = 1.0×D₀
Mag_Duration = 3 × 0.10 = 0.3s
Total_Output_per_Mag_raw = 3.0×D₀
Sustained_Output = 3.0×1.0 / (0.3+3.0) = 3.0/3.3 ≈ 0.909×D₀ ✅
```

**M4 叢集炸彈範例（採納分析文件路徑 A：`m4_shot_interval = 0.20s`）**：

```
ShotCount = 4，Output_per_Shot = m4_total_output_cap_mult × D₀ = 1.0×D₀
Mag_Duration = 4 × 0.20 = 0.8s
Total_Output_per_Mag_raw = 4.0×D₀
Sustained_Output = 4.0×1.0 / (0.8+3.5) = 4.0/4.3 ≈ 0.930×D₀ ✅
```

**路徑 A vs 路徑 B**：本文件預設採路徑 A（`ShotInterval` 落在快速連發區間，不需連動調整彈匣量/換彈時間），因為它是**不牽動其他旋鈕、改動面最小**的方案；但兩條路徑數字上都能落入區間（見分析文件 §3.2），**真實手感間隔仍待原型測量**——見 I 章 #2（沿用分析文件開放問題 #1 編號但已整併路徑選擇的預設立場）。

### D.5 M2 蜂群飛彈（Chain Hive 重構）— 多重齊射公式

```
Mag_Duration = (SalvoCount - 1) × InterSalvoInterval
Total_Output_per_Mag_raw = SalvoCount × MicroCount × DmgPerMissileMult
Sustained_Output = Total_Output_per_Mag_raw / (Mag_Duration + Reload_Time)
```

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `SalvoCount` | int | [2, 4]，預設 3 | 一次扳機內的齊射次數，**Tier 0–3 共用同一個值**（C.2 重構後不再分 T1/T3） |
| `MicroCount` | int | 固定 = 8 | 每次齊射的微型飛彈數（既有欄位 `m2_micro_count`，不變） |
| `DmgPerMissileMult` | float | [0.18, 0.32]，預設 0.25 | 每枚微型飛彈輸出（×D₀），**新增欄位**，取代原本隱含的 `D0/8` |
| `InterSalvoInterval` | float | [0.3, 0.6]，預設 0.4s | 齊射間微冷卻，**新增欄位**，Tier 0–3 共用 |

**輸出範圍**：EffectiveHitRate 對 M2 維持 1.0（多發低價值命中，命中率折損已隱含在「多發才等於 1×D₀」的設計裡，不重複打折）。

**範例（Tier 0–2 與 Tier-3 數字完全相同——這正是解決「彈匣互相矛盾」問題的關鍵）**：

```
SalvoCount = 3, MicroCount = 8, DmgPerMissileMult = 0.25×D₀
Total_Output_per_Mag_raw = 3 × 8 × 0.25 = 6.0×D₀
Mag_Duration = (3-1) × 0.4 = 0.8s
Reload_Time = 5.0s（m2_reload_time，維持全飛彈系最長）
Sustained_Output = 6.0 / (0.8+5.0) = 6.0/5.8 ≈ 1.034×D₀ ✅

Tier-3「飽和點名」：SalvoCount/MicroCount/DmgPerMissileMult/InterSalvoInterval/Reload_Time
全部不變 → Sustained_Output(Tier-3) = Sustained_Output(Tier 0-2) = 1.034×D₀（完全相同）
```

### D.6 Tier-3 深化機制的等功率健檢（H.7 前置驗算）

本文件對 8 把武器的 Tier-3 機制逐一檢查「是否在不知不覺間變成加量」，結果如下（詳細算式見各段）：

| 武器 | Tier-3 機制 | 對 Sustained_Output 的影響 | 結論 |
|------|-------------|------------------------------|------|
| L1 散波雷射 | 殘熱焰 | 0%（殘熱只在雷射離開部位後生效，對單目標持續全中無貢獻） | ✅ 乾淨通過 |
| L2 集束雷射 | 微追蹤 + 熱量漣漪 | 微追蹤是命中率輔助非速率；漣漪只在部位破壞瞬間觸發（一次性事件，非穩態） | ✅ 乾淨通過 |
| L3 波動砲 | 共鳴擴散（熱量注入 50%） | **+80%**（見下方算式） | ❌ **嚴重超標，需導演裁決，見 I 章 #1** |
| L4 穿透雷射 | 熱殘影 | 0%（與 L1 同理，只在雷射離開部位後生效） | ✅ 乾淨通過 |
| M1 追蹤飛彈 | 熱源引導（2→3 枚/發） | +3%（見下方算式；彈匣總量不變，只是拆分方式變，總輸出天然守恆） | ✅ 通過（在 15% 內） |
| M2 蜂群飛彈 | 飽和點名（目標選擇） | 0%（C.2/D.5 重構後 Tier-3 與基礎 Tier 公式輸入完全相同） | ✅ 乾淨通過（設計即保證） |
| M3 穿甲魚雷 | 穿甲爆破鏈 | 0%（只在部位破壞瞬間觸發，一次性事件，非穩態輸出） | ✅ 乾淨通過 |
| M4 叢集炸彈 | 子母炸彈 | **+20%**（見下方算式） | ⚠️ 輕微超標，建議收緊，見下方 |

**L3 共鳴擴散算式**（Tier-3，沿用 D.3 蓄力週期）：

```
基礎注入 HU = l3_t3_heat_inject_pct(0.50) × H_max_normal(100) = 50 HU
換算為 ×D₀ 單位 = 50 / HuPerD0(25) = 2.0×D₀
Total_Output_per_Mag_raw(Tier-3) = l3_charge_output_mult(2.5) + 2.0 = 4.5×D₀
Sustained_Output(Tier-3) = 4.5 / 2.7 ≈ 1.667×D₀
相對 Tier 0-2 基準(0.926×D₀)：+80%（遠超 H.7 的 15% 上限，也遠超等功率帶）
```

**修正選項**（供導演裁決，見 I 章 #1）：(a) 大幅調降 `l3_t3_heat_inject_pct`（需壓到約 9–10% 才能回到 15% 內，遠低於現有安全範圍下限 30%，代表安全範圍本身需要重新定義）；(b) 把注入目標從「同一部位」改為「相鄰部位」（比照 L2 漣漪的模式重新設計，使其與現行「一次性事件不計入穩態」規則一致）；(c) 沿用 GDD 已有先例（M3 熱衝擊引爆），明確把共鳴擴散注入定位為「情境爆發值，非持續值」，不受 H.1/H.7 穩態約束。本文件推薦選項 (c)，理由：實作改動最小、與既有設計語言（蓄力本身就是週期性爆發動作，不是穩態 DPS）最一致。

**M1 熱源引導算式**（Tier-3）：

```
彈匣總量固定 = m1_mag_size(6) 枚，與 Tier 無關
Tier 0-2：ShotCount=3（6÷2），Output_per_Shot=1.0×D₀，Total=3.0×D₀，
          Mag_Duration=3×0.10=0.3s，Sustained=3.0/(0.3+3.0)=0.909×D₀
Tier-3：ShotCount=2（6÷3），Output_per_Shot=1.5×D₀，Total=3.0×D₀（總量守恆——
        彈匣量不變，只是每次射擊耗彈量從2變3），
        Mag_Duration=2×0.10=0.2s，Sustained=3.0/(0.2+3.0)=0.938×D₀
差異：(0.938-0.909)/0.909 ≈ +3.2%（遠低於 15% 上限）
```

M1 是「加量但總彈匣守恆」設計能自動滿足 H.7 的乾淨示範——因為彈匣總容量不隨 Tier 改變，Tier-3 只是改變同一批彈藥的分配方式（每次打更多、次數更少），Total_Output_per_Mag 天然不變，唯一變動來自 Mag_Duration 略微縮短（射擊次數變少 → 總耗時略降），落在合理誤差內。

**M4 子母炸彈算式**（Tier-3）：

```
Tier 0-2：4 枚 × m4_total_output_cap_mult(1.0×D₀ 封頂) = 4.0×D₀，
          Mag_Duration=4×0.20=0.8s，Sustained=4.0/(0.8+3.5)=0.930×D₀
Tier-3：每枚母彈裂解為 6 枚子彈，各自輸出 m4_t3_child_dmg_pct(0.20×D₀)，
        每枚母彈總輸出 = 6×0.20=1.2×D₀（不受總量封頂限制——子彈各自獨立）
        4 枚母彈 × 1.2×D₀ = 4.8×D₀，Sustained=4.8/(0.8+3.5)=1.116×D₀
差異：(1.116-0.930)/0.930 ≈ +20%（超過 15% 上限）
```

**建議修正**（既有欄位收緊，非新欄位）：`m4_t3_child_dmg_pct` 由 0.20 調降至 **0.18**（安全範圍 [0.15, 0.30] 內）：每枚母彈總輸出 = 6×0.18=1.08×D₀，4 枚=4.32×D₀，Sustained=4.32/4.3≈1.005×D₀，差異 = (1.005-0.930)/0.930 ≈ +8.1%，回到 15% 內，且仍在等功率帶 [0.9,1.1] 內。

---

## E. 邊界情況 (Edge Cases)

### E.1 M2 齊射進行中換裝／部位全部破壞

若玩家在 3 次齊射的微冷卻期間換裝（拾取新武器莢艙），既有 `Disable()`/collider 清除邏輯（`WeaponBehaviourBase.OnPartBroke`）已處理「目標部位破壞後清除快取」；本重構新增的待發齊射（pending salvo）在換裝時應直接捨棄，不得帶到下一把武器身上——沿用 `M2SwarmLauncher.Tick` 現有的 `_pendingBurstBTargets` 生命週期模式，換裝時呼叫 `Disable()` 應清空待發狀態（實作時需明確新增此清除邏輯，目前程式碼未涵蓋此路徑）。

### E.2 M2 飽和點名找不到已軟化部位

若觸發第 2、3 次齊射時戰場上沒有任何 SOFTENED 部位（例如 Boss 只有 1 個部位且尚未蓄熱到位），Tier-3 的目標鎖定邏輯**退化為基礎 Tier 行為**（沿玩家瞄準錐散佈），不報錯、不跳過該次齊射——與 M1 Tier-3「無存活部位則跳過第 3 枚但仍消耗彈藥」的既有邊界處理一致（消耗彈藥但不強制找到目標）。

### E.3 L1 波束數變更時的既有殘熱追蹤器（ResidualHeatTracker）

C.3 的階梯只改變波束數量，不改變 `ResidualHeatTracker` 的介面（`Register(partId, kaijuId, channel, rate, duration)`）——Tier 0（2 束）與 Tier 3（5 束）在全中時每束各自登記一次殘熱（若 Tier == 3），彼此獨立疊加登記但依 E.6（`weapon-system.md`）「取最大值不相加」的既有規則被 tracker 內部去重，行為不需改動。

### E.4 L3 若導演選擇 I 章 #1 的選項 (b)（注入改打相鄰部位）

若最終裁決不是選項 (c)（維持情境爆發值），而是選項 (b)（改打相鄰部位），需要額外定義「相鄰部位」的判定來源——`kaiju-part-system.md` 目前的部位鄰接關係定義（若存在）需要被 L3WaveCannon 消費；本文件不預先假設該介面存在，留待導演裁決後由 `kaiju-part-system.md` 補充。

### E.5 敵彈大小分級與菁英彈幕密度倍率的交互

`EmitterPatternSO.EliteDensityMult` 只縮放子彈**數量**，不縮放 `_sizeClass`——菁英怪不會自動獲得更大顆的彈幕，大小分級是敵人 prefab 設計時的獨立選擇（回饋意見「寫進各敵人的 prefab」的精神）。若菁英版本想要「更大更少」而非「一樣大更多」的變體，需另外在 prefab 層級指定不同的 `EmitterPatternSO` 資產，本文件的資料模型不阻止這種用法。

### E.6 D0Reference 改變時的所有換算連動

`EqualPowerBandTolerance` 是相對百分比，不受 `D0Reference` 絕對值影響；但 D.5 M2 公式與 D.4 M1/M4 公式中所有「×D₀」項都會隨 `D0Reference` 線性縮放——調整 `D0Reference` 後，`HuPerD0`／`BuPerD0` 兩個換算常數**不會**自動連動（它們是獨立欄位），需要一併重新校驗，這與既有 `weapon-system.md` G.1 的既有風險相同，本文件不新增額外風險，僅提醒。

---

## F. 系統相依 (Dependencies)

### F.1 武器系統（weapon-system.md）— 權威來源

本文件所有公式與規則的權威定義來自該文件 C/D/G 章；本文件只補上缺失欄位與分級成長規格，不推翻其鎖定內容。**反向依賴**：`weapon-system.md` 應在下次非破壞性修訂時，比照 F.1 已有的附錄慣例，註記「波束數改由 `l1_beam_count_by_tier` 決定」與「M2 齊射結構改由 C.2 本文件定義」。

### F.2 D₀ 等功率分析文件（weapon-d0-equal-power-analysis.md）— 前置分析

本文件是其直接延伸；該文件核准後才代表 §3/§4 的建議值成為本文件的基準輸入。兩份文件應一併送審——若導演只核准其中一份，H.1/H.2/H.7 測試仍無法撰寫（缺口互相依賴：分析文件定義「為什麼需要這些欄位」，本文件定義「這些欄位長什麼樣、加上分級成長後數字如何」）。

### F.3 彈幕系統（bullet-system.md）— 敵彈大小消費端

`EnemyBulletSizeConfig` 與 `EmitterPatternSO._sizeClass` 由該系統的碰撞判定（§6.1）與生成路徑消費；C.5 的判定半徑直接影響該文件 §6.2 空間網格格寬的估算基準（「格寬約等於最大子彈直徑的數倍」——Large 分級的判定/視覺半徑一旦鎖定，該文件的網格格寬需要重新核對，本文件僅提供輸入數字，不重算該文件的網格參數）。

### F.4 部位系統（kaiju-part-system.md）

C.2 M2 Tier-3「飽和點名」需要新增「取得 BU 進度最高的已軟化部位」查詢（`IPartStateQuery` 擴充）——這是本文件對該系統介面提出的新需求，需該文件/其實作方確認可行性後才能定案。

### F.5 難度系統（difficulty-system.md）

C.5 的 `ThreatWeight` 欄位是為該系統預留的鉤子（密度/威脅預算機制），本文件不定義其消費邏輯，只確保資料存在。

---

## G. 調校旋鈕 (Tuning Knobs)

**所有數值必須存放於對應 SO（`WeaponDef` / `WeaponBalanceConfig` / `EmitterPatternSO` / 新增 `EnemyBulletSizeConfig`），禁止硬編碼（ADR-0003）。**

### G.1 `WeaponBalanceConfig`（全域）新增欄位

| 欄位名 | 預設值 | 安全範圍 | 說明 | 對照分析文件 §4 |
|--------|--------|----------|------|-------------------|
| `EqualPowerBandTolerance` | 0.10 | [0.05, 0.15] | H.1/H.2 斷言用的等功率容忍帶寬度，取代目前寫死在測試碼裡的 ±10% | 採納 §4 表格「Nice-to-have」項，本文件升級為必要項 |

### G.2 `WeaponDef` 新增欄位 — 每武器命中率與射擊節奏

| 欄位名 | 適用武器 | 預設值 | 安全範圍 | 說明 | 對照分析文件 §4 |
|--------|----------|--------|----------|------|-------------------|
| `EffectiveHitRate` | 逐武器（8 個獨立值） | L1=1.0, L2=**0.65**, L3(tap)=1.0, L3(charge)=1.0, L4=1.0, M1=1.0, M2=1.0, M3=**0.80**, M4=1.0 | (0, 1] | 命中率折損係數，套用於 `Total_Output_per_Mag_raw` | 採納 §3.1/§4 建議值 |
| `M1ShotInterval` | M1 | 0.10s（路徑 A） | [0.08, 0.40] | 每次射擊（2/3 枚）之間的間隔；**待原型實測確認**，見 I #2 | 採納，路徑 A 為預設立場 |
| `M3ShotInterval` | M3 | 1.0s | [0.8, 1.5] | 正式化 GDD D.1 範例隱含的「3 枚間隔≈3s」假設 | 採納 §4 建議值 |
| `M4ShotInterval` | M4 | 0.20s（路徑 A） | [0.15, 0.50] | 每次拋投之間的間隔；**待原型實測確認**，見 I #2 | 採納，路徑 A 為預設立場 |

**不新增**：`L3TapInterval`（見 D.3 說明，Tap 模式是連續速率模型，此欄位是分析文件的誤讀，本文件明確推翻）。

### G.3 `WeaponDef` 新增欄位 — L1 波束階梯

| 欄位名 | 型別 | 預設值 | 安全範圍 | 說明 |
|--------|------|--------|----------|------|
| `L1BeamCountByTier` | int[4] | {2, 3, 4, 5} | 每格 [2, 6]，且陣列須嚴格遞增 | Tier 0/1/2/3 各自的波束數，唯一權威來源（取代 scene shell 的硬編碼判斷）|

### G.4 `WeaponDef` 新增/修改欄位 — M2 Chain Hive 重構

| 欄位名 | 動作 | 預設值 | 安全範圍 | 說明 |
|--------|------|--------|----------|------|
| `M2SalvoCount` | 新增 | 3 | [2, 4] | 每次扳機的齊射次數，Tier 0–3 共用 |
| `M2DmgPerMissileMult` | 新增 | 0.25（×D₀） | [0.18, 0.32] | 每枚微型飛彈輸出，取代原隱含 D₀/8 |
| `M2InterSalvoInterval` | 新增 | 0.4s | [0.3, 0.6] | 齊射間微冷卻，Tier 0–3 共用 |
| `M2T3PrioritySoftenedTargeting` | 新增 | true | bool（開關，QA 可停用） | Tier-3「飽和點名」目標鎖定行為開關 |
| `M2T3MagCount` | **淘汰** | — | — | 由 `M2SalvoCount × M2MicroCount` 取代（24，Tier 通用） |
| `M2T3BurstMicroCd` | **淘汰** | — | — | 由 `M2InterSalvoInterval` 取代（Tier 通用） |

### G.5 `WeaponDef` 新增欄位 — 逐 Tier 視覺成長（非戰力數值）

| 欄位名 | 適用武器 | 型別 | 預設值 | 安全範圍 | 說明 |
|--------|----------|------|--------|----------|------|
| `L2BeamWidthMultByTier` | L2 | float[4] | {1.0, 1.15, 1.3, 1.5} | 每格 [1.0, 1.6] | 束寬視覺倍率，不影響判定/輸出 |
| `L3ChargeVisualScaleByTier` | L3 | float[4] | {1.0, 1.15, 1.3, 1.5} | 每格 [1.0, 1.6] | 蓄力球視覺縮放 |
| `L4BeamWidthMultByTier` | L4 | float[4] | {1.0, 1.15, 1.3, 1.5} | 每格 [1.0, 1.6] | 貫穿線視覺粗細 |
| `M1MissileScaleByTier` | M1 | float[4] | {1.0, 1.15, 1.3, 1.5} | 每格 [1.0, 1.6] | 飛彈視覺縮放 |
| `M3TorpedoScaleByTier` | M3 | float[4] | {1.0, 1.15, 1.3, 1.5} | 每格 [1.0, 1.6] | 魚雷視覺縮放 |
| `M4BombScaleByTier` | M4 | float[4] | {1.0, 1.15, 1.3, 1.5} | 每格 [1.0, 1.6] | 炸彈視覺縮放 |

> 這批欄位刻意**不影響任何 D₀ 公式輸入**——是純粹的美術/回饋縮放，套用時機由 VFX/美術實作決定（Sprite scale 或 Shader 參數），系統設計層級只保證「這是一個安全、獨立於戰力數值的旋鈕」。

### G.6 既有欄位建議修改值（非新增，隨本文件一併送審）

| 欄位 | 現值 | 建議值 | 安全範圍 | 理由 |
|------|------|--------|----------|------|
| `l3_charge_time` | 1.5s | **1.2s** | [1.2, 2.0] | D.3：壓縮蓄力週期以達成等功率 |
| `l3_charge_cooldown` | 2.0s | **1.5s** | [1.5, 2.5] | 同上 |
| `m4_t3_child_dmg_pct` | 0.20 | **0.18** | [0.15, 0.30] | D.6：修正 Tier-3 子母炸彈 +20% 超標為 +8.1% |
| `l3_t3_heat_inject_pct` | 0.50 | **待導演裁決**（見 I #1，可能低至 0.09–0.10，遠低於現有安全範圍下限 0.30，代表範圍本身需重新定義） | 待定 | D.6：+80% 嚴重超標，需先決定修正策略（調數字 / 改目標 / 定位為爆發值）才能定出安全範圍 |

### G.7 `EmitterPatternSO` 新增欄位

| 欄位名 | 型別 | 預設值 | 安全範圍 | 說明 |
|--------|------|--------|----------|------|
| `SizeClass` | enum {Small, Medium, Large} | Medium | — | 索引到 `EnemyBulletSizeConfig` |

### G.8 新增 SO：`EnemyBulletSizeConfig`（全域，3 列固定資料）

| 欄位名 | 型別 | 預設值（Small/Medium/Large） | 安全範圍 | 說明 |
|--------|------|-------------------------------|----------|------|
| `HitboxRadiusPx` | float[3] | 4 / 7 / 12 | Small [3,6] / Medium [6,10] / Large [10,18] | 真實碰撞半徑（點 vs 圓判定） |
| `VisualRadiusPx` | float[3] | 5 / 9 / 15 | Small [4,8] / Medium [7,12] / Large [12,22] | 顯示半徑，恆 ≥ 判定半徑（寬容邊界） |
| `ThreatWeight` | float[3] | 1.0 / 1.8 / 3.0 | [0.5, 5.0] | 保留給未來密度/難度預算機制的鉤子，本文件不定義消費端 |

---

## H. 驗收標準 (Acceptance Criteria)

### H.1 等功率等價（延伸 weapon-system.md H.1）

- [ ] 一旦 G.2–G.6 欄位在 SO 資料中核准並填入數值，`weapon_dps_equivalence_test` 對以下組合逐一計算 `Sustained_Output` 並斷言落在 `D0Reference × [1-EqualPowerBandTolerance, 1+EqualPowerBandTolerance]` 內：
  - L1（全部 4 個 Tier，各自代入對應 `L1BeamCountByTier[tier]`，驗證 D.2 的「總和恆定」不變式在 4 個 Tier 下都成立）
  - L2（Tier 無關，單一斷言）
  - L3（**僅 Charge 模式**；Tap 模式明確排除在此測試外，但仍需一條獨立測試記錄其 0.60×D₀ 數值不隨版本漂移）
  - L4（Tier 無關，單一斷言；多部位穿透場景另立測試，見 I #3）
  - M1（Tier 0-2 與 Tier-3 各一個斷言，驗證 D.6 的 +3.2% 差異在容忍帶內）
  - M2（Tier 0-2 與 Tier-3 各一個斷言，驗證兩者數字**完全相同** 1.034×D₀）
  - M3（單一斷言，`EffectiveHitRate=0.80` 套用後）
  - M4（Tier 0-2 與 Tier-3 各一個斷言，驗證採納 G.6 建議修改值後 Tier-3 落在 1.005×D₀，容忍帶內）
- [ ] 若導演對 I #1（L3 共鳴擴散）選擇非「情境爆發值」的裁決，本測試需新增對應斷言；若選擇維持爆發值定位，本測試明確排除該數值，並在測試註解引用本文件 D.6 與該裁決。

### H.2 無主導 loadout（延伸 weapon-system.md H.2）

- [ ] `weapon_loadout_matrix_test` 在 Tier 0（基礎）與 Tier 3（滿等）**各跑一次完整 8×8 矩陣**（而非只跑一次），驗證分級成長沒有在滿等狀態下製造出新的主導組合——這是本文件對 H.2 範圍的擴充，因為 Tier-3 之前不受此矩陣測試覆蓋。
- [ ] 兩個矩陣的斷言與既有規則相同：任何 loadout 的 TTB 不超過最快 loadout 的 2.0×；無 loadout 在三種部位類型 TTB 排名皆前三。

### H.3 Tier-3 深化身份，不放大數值（延伸 weapon-system.md H.7）

- [ ] `tier3_identity_depth_test` 對全部 8 把武器逐一比較 Tier 0-2 vs Tier-3 的 Sustained_Output（不只是 TTB，因為本文件的分級成長主要影響 Sustained_Output 的輸入項），斷言差異 ≤ 15%。
- [ ] 測試資料直接取自本文件 D.6 表格的 8 列結果作為預期值（迴歸基準）；任何未來調整這些欄位的 PR，若使某一列的差異超過 15%，測試須失敗並在錯誤訊息中指出是哪個欄位改動導致。
- [ ] L3 的斷言邏輯需先等待 I #1 裁決結果才能寫死（目前草稿邏輯：若選項 (c)，排除共鳴擴散注入值於此比較之外，只比較 Charge 基礎輸出；若選項 (a)/(b)，需將修正後的注入值納入比較）。

### H.4 L1 波束階梯視覺可讀性（體驗性，非阻斷）

- [ ] Tier 0（2 束）到 Tier 3（5 束）的視覺密度變化，在原型截圖比對中應清楚可辨（呼應回饋意見「散彈 2→3→4→5」的初衷是玩家看得出差異）。
- [ ] 驗收方法：比照 `weapon-system.md` H.5 的做法，設計師主持截圖比對，5 人測試辨識「這是強化後的散彈」成功率 ≥ 80%。

---

## I. 開放問題，提交導演確認 (Open Questions)

| 優先級 | 問題 | 阻斷里程碑 | 解答方式 |
|--------|------|------------|----------|
| **極高** | **L3 波動砲 Tier-3「共鳴擴散」量化後 +80% 嚴重超標**（D.6）。三個修正選項：(a) 大幅調降 `l3_t3_heat_inject_pct` 至 9–10%（遠低於現有安全範圍）；(b) 改打相鄰部位（重新設計機制）；(c) 定位為情境爆發值，比照 M3 熱衝擊引爆先例，不受 H.1/H.7 穩態約束。本文件推薦 (c)。 | Alpha（阻斷 H.1/H.7 測試撰寫） | 導演直接裁決三選一 |
| 高 | M1/M4 的真實 `ShotInterval` 應該是多少？本文件預設路徑 A（0.10s / 0.20s，不動其他旋鈕），但這是數學上可行的猜測，非實測值——分析文件已指出這是對最終數字影響最大的未知數。 | Vertical Slice | 原型手感測試提供實測數據 |
| 中 | M4 子母炸彈的 `m4_t3_child_dmg_pct` 建議收緊至 0.18（見 G.6）——是否接受這個小幅削弱？子彈視覺數量（6 顆）與單顆威力的取捨是否有更好的解法（例如改成 5 顆、每顆 0.216）？ | Vertical Slice | 導演/平衡設計快速確認，數字本身可再議 |
| 中 | L2（0.65）與 M3（0.80）的 `EffectiveHitRate` 沿用分析文件建議值，尚未實測驗證——是否要求先做原型測量再鎖定？ | Vertical Slice | 沿用分析文件既有開放問題，本文件不重複展開 |
| 中 | M2 Tier-3「飽和點名」需要的新查詢介面（F.4：「取得 BU 進度最高的已軟化部位」）是否可行？是否該由现有 `GetHottestAlivePartId` 的姊妹方法補上，還是需要更通用的部位排序介面？ | Vertical Slice | `kaiju-part-system.md` 負責人確認介面可行性 |
| 低 | L1 波束階梯的視覺成長（G.5 的通用 `[Weapon]ScaleByTier` 系列欄位）是否要等 BulletSim 視覺管線就緒後才能真正呈現，還是可以先用簡單的 Sprite scale 做 placeholder？ | 不阻斷（純美術排程問題） | 美術/VFX 負責人排程確認 |
| 低 | `EnemyBulletSizeConfig` 的 `ThreatWeight` 鉤子目前無消費端——是否要在本輪就併入 `difficulty-system.md` 的密度公式，還是先擱置到彈幕多元化（回饋意見第 1 點）正式立項時再一併設計？ | 不阻斷 | 待回饋意見第 1 點展開時一併決定 |

---

*文件版本：0.1.0（Draft for Director Review）*
*作者：Systems Designer Agent*
*關聯文件：design/gdd/weapon-system.md（LOCKED）| design/balance/weapon-d0-equal-power-analysis.md（前置分析，待核准）| design/feedback/2026-07-02-改進意見與劇情草案.md §A.3（需求來源）| Assets/_Project/Scripts/Content/WeaponDef.cs, WeaponBalanceConfig.cs, EmitterPatternSO.cs | Assets/_Project/Scripts/Weapons/*.cs*
*下一步：導演核准本文件（含 §D.6/I 章的 L3、M4 修正裁決）後，撰寫/修改 `WeaponDef.cs`、`WeaponBalanceConfig.cs`、`EmitterPatternSO.cs`、新增 `EnemyBulletSizeConfig.cs`，再撰寫 `tests/unit/weapon/weapon_dps_equivalence_test`、`weapon_loadout_matrix_test`、`tier3_identity_depth_test`（Unity Test Framework，`Assets/_Project/Tests/EditMode/`，依 ADR-0005 路徑慣例）*
