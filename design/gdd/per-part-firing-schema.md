# Per-Part 射擊與部位移動：Schema 規格 (Per-Part Firing & Movement — Schema Spec)
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/per-part-firing-schema.md*
*最後更新: 2026-07-08*
*狀態: 規格定案（實作藍圖）— 彙整 8 隻 kaiju GDD 的 per-part 需求成單一權威 schema*
*用途: 讓「不同部位射不同子彈 + 部位移動」（雷電式）可資料驅動。8 隻頭目 GDD 各自的 per-part 機制在此收斂成一致欄位，供實作照做（不再有分歧版本）。*
*相依: `kaiju/00-roster-overview.md`(§4)、`kaiju/01-08`、`kaiju-part-system.md`、`enemy-roster-expansion.md`。*

---

## 0. 背景 (Why)

現況（子代理勘查）：`PartDef` 只有 7 欄位（partId/partType/H·B override/adjacency/dropTableId），**無 emitter、無 movement、無跨部位閘門**。三隻既有頭目與 5 隻新頭目 GDD 都把「每個部位＝命名發射源、破部位＝消音」寫在文字，但**從未資產化**。本規格補上資料模型，讓 BossController 能真的驅動 per-part 射擊。

**鐵則**：所有新欄位**向後相容**（可選、預設 None/空/false）；既有 3 隻 .asset 與 448 EditMode 測試不得破壞。

---

## 1. 新增列舉 (New Enums)

### 1.1 `KaijuTheme`（Core/Types/KaijuTheme.cs）— 追加 5 值
```
Carapace=0, Limb=1, Energy=2,        // 既有
Swarm=3, Crystal=4, Abyss=5, Ember=6, Void=7   // 新
```

### 1.2 `MaterialId`（Core/Types/MaterialId.cs）— 追加 5 核心
```
ShardCommon=0, CoreCarapace=1, CoreLimb=2, CoreEnergy=3, EssenceKaiju=4,   // 既有
CoreSwarm=5, CoreCrystal=6, CoreAbyss=7, CoreEmber=8, CoreVoid=9           // 新
```
`EconomyConfig`：加 5 個 `_coreForSwarm/Crystal/Abyss/Ember/Void` 序列化欄位 + `CoreForTheme` switch 補 5 case（default 維持 fail-loud）。

> ⚠️ **經濟 sink 待導演定案（不阻擋 per-part 實作）**：現有 8 武器升級成本綁定 3 原始核心（L1/M2/M4→甲殼、L2/L4/M1→肢體、L3/M3→能量）。5 新核心「升級什麼」尚無 sink。選項：(A) 新核心開一條新養成軸（如機體/utility 強化）；(B) 擴充武器升級成本改用更廣核心池；(C) 新核心先只計入圖鑑/成就。**實作階段先讓每部位正確掉對應新核心（不 crash），sink 分配另案。**

### 1.3 `EmitterPatternType`（Content/EmitterPatternSO.cs）— 追加 1 值
```
Linear, Radial, Aimed, RingBurst,    // 既有
Spiral                                // 新：放射臂隨時間旋轉；需 SpinRateDegPerSec
```
`EmitterPatternSO` 加 `_spinRateDegPerSec`(僅 Spiral 讀)。

### 1.4 `MovementType`（Content/MovementPatternSO.cs）— 追加 2 值（給新小怪）
```
StraightRush, HorizontalDrift, Hover, UTurn, Sinusoidal,   // 既有
DiveSwoop, HoverStrafe                                      // 新
```
`MovementPatternSO` 加 `_entryAngleDeg`(DiveSwoop)、`_strafeHalfWidthPx`(HoverStrafe)。

### 1.5 `EnemyTier`（Core/Types/EnemyTier.cs）— 新檔
```
public enum EnemyTier { Trash=0, Elite=1, Mid=2, Boss=3 }
```
`EnemyDef` 加 `_tier`(預設 Trash)；OnValidate 保持 `IsElite == (Tier==Elite)`。

---

## 2. `PartMovement`（新 [Serializable] 結構，部位移動）

供頭目部位移動（LACERA 掃臂、PRISMSHELL 晶面公轉、NULLSPIRE 盾旋轉/衛星公轉、EMBERWING 翼擺）。純資料，執行系統依型別讀參數。

```csharp
public enum PartMovementType { None=0, Orbit=1, SweepArc=2, Oscillate=3, Spin=4 }

[Serializable] public struct PartMovement {
    PartMovementType Type;        // None = 靜態（預設）
    Vector2 PivotOffset;          // Orbit/SweepArc 的樞紐（相對頭目根，world）
    float RadiusWorld;            // Orbit 半徑
    float AngularSpeedDeg;        // Orbit 公轉 / Spin 自轉 / SweepArc·Oscillate 角速度（度/秒）
    float ArcHalfDeg;             // SweepArc/Oscillate 半擺幅（Orbit/Spin 忽略）
    float PhaseDeg;               // 初始相位（多部位反相：LACERA 四肢 0/180/90/270）
}
```
對映 GDD：LACERA 肢=SweepArc(arc 60/90, speed 45/30, phase 0/π/…)、PRISMSHELL 晶面=Orbit(15°/s, 90° 間隔)、NULLSPIRE 盾=Spin/Orbit、衛星=Orbit、EMBERWING 翼=Oscillate。

---

## 3. `PartEmitter`（新 [Serializable] 結構，部位發射源）

一個部位可掛 **0..N** 個發射源。射擊閘門讓「破部位/剝甲/軟化」資料驅動地開關火力。

```csharp
public enum PartFireGate {
    AliveOnly=0,             // 部位存活即發射（破壞=消音）— 預設
    SilenceWhenSoftened=1,   // 軟化時暫停（PRISMSHELL 晶面剝甲露縫）
    RequireArmorStripped=2,  // 需先剝甲才發射
    RequireGatePartBroken=3, // 需 GatePartId 指定的部位已破才啟用（BROODCORE 護膜破後核心換模式）
}

[Serializable] public struct PartEmitter {
    EmitterPatternSO Pattern;   // 彈幕型（含 Spiral）
    PartFireGate Gate;          // 發射閘門
    string GatePartId;          // RequireGatePartBroken 用；否則空
    string SpawnEnemyId;        // 非空 = 這個「發射源」改為週期生小怪（BROODCORE 卵囊生 spore_mite）
    int SpawnCap;               // 該部位同時存在小怪上限
}
```
`PartDef` 加 `PartEmitter[] Emitters`（可選，空=不發射）。

---

## 4. 跨部位閘門 (Cross-Part Gates) — `PartGate`

三隻頭目需要「某部位的可破/可打取決於另一部位狀態」：EMBERWING 外翼孔需同側翼根剝甲、TIDEMAW 心核在背甲破前隱藏、PRISMSHELL weak_node 需鄰面軟化。統一成：

```csharp
public enum PartGateKind { None=0, HittableWhen=1, BreakableWhen=2 }
public enum PartGateCond { GatePartBroken=0, GatePartArmorStripped=1, GatePartSoftened=2 }

// 加在 PartDef：
PartGateKind GateKind;      // None=無閘門（預設）
PartGateCond GateCond;
string[] GatePartIds;       // 需滿足條件的來源部位（PRISMSHELL weak_node 列鄰面）
bool RequireAllGates;       // true=全部滿足 / false=任一滿足
```
- EMBERWING `wing_vent_l2`：BreakableWhen / GatePartArmorStripped / [wing_root_l]。
- TIDEMAW `heart_core`：HittableWhen / GatePartBroken / [dorsal_plate]。
- PRISMSHELL `weak_node`：HittableWhen / GatePartSoftened / [facet_a, facet_b] / RequireAll=false。

> 需與 kaiju-part-system 的破壞判定接：BreakableWhen 未滿足時，該部位的破甲輸入不生效（或 hitbox 關）；HittableWhen 未滿足時 hitbox 關。BossController/PartStateSystem 讀取。

---

## 5. `ArmorRegen`（TIDEMAW 破甲槽回填）— `PartDef` 可選

```csharp
[Serializable] public struct ArmorRegen {
    bool Enabled;              // 預設 false
    float GraceSeconds;        // 無破壞輸入後多久開始回填（TIDEMAW 5.0）
    float RegenRatePerSec;     // 回填速率 BU/s（TIDEMAW 6.0；Phase2 由階段覆寫 7.8）
}
```
規則（TIDEMAW GDD）：每部位記 `t_since_break_hit`；**飛彈/L3 破壞命中歸零**（雷射熱命中不歸零，回填只針對破甲軌）。`t ≥ Grace` 後 `B_current` 以 RegenRate 衰退，clamp≥0，**永不復活已 BROKEN 部位**。由 PartStateSystem.Tick 執行。

---

## 6. `KaijuDef.BodyMovement`（整體移動）— 可選

把現行 `BossController.IdleMotion`（硬編呼吸/漂移）改資料驅動：
```csharp
[Serializable] public struct BodyMovement {
    float DriftAmpX, DriftAmpY;   // 漂移幅（world）
    float DriftFreqX, DriftFreqY; // 頻率（Hz）
}
```
`KaijuDef` 加 `BodyMovement Body`。BROODCORE 搏動、TIDEMAW 橫向巡游、NULLSPIRE 漂浮皆可調。

---

## 7. 執行系統影響 (Runtime — task #6)

- **BossController/BossPart**：每部位依 `Emitters[]` 週期發射（用既有 `EnemyBulletPool` Mono 池化 kinematic 暖色彈）；依 `PartFireGate` + 部位 break/armor/heat 狀態閘門開火；`SpawnEnemyId` 者改生小怪。依 `PartMovement` 每幀更新部位 local 位置。跨部位閘門查詢 PartStateSystem。
- **Spiral / DiveSwoop / HoverStrafe**：`EnemyEmission` / `EnemyMovement` 純函式加對應分支（可測）。
- **PartStateSystem**：`ArmorRegen` 回填、`PartGate` 破壞/命中閘門。
- 全部**新增可選**，既有測試不動。

---

## 8. 實作順序 (Implementation Order)

1. **enums**（§1）+ EconomyConfig 5 core 對映 → 編譯綠、既有測試綠。
2. **PartDef/KaijuDef 欄位**（§2–6）+ OnValidate → 編譯綠。
3. **EditMode 測試**：PartMovement 數學、PartEmitter gate、PartGate 條件、ArmorRegen 衰退（決定性、注入時間）。
4. **執行接線**（§7，task #6）：BossController per-part 發射 + 移動；新 emitter/movement 分支；PartStateSystem regen/gate。
5. **資產化**：8 隻 KaijuDef .asset 填 per-part emitter/movement（既有 3 隻補、新 5 隻建）；6 新小怪 SO+prefab。
6. **可玩驗證 + 重建 EXE/APK**。

---

## 9. 驗收 (Acceptance)

- [ ] 5 新 KaijuTheme + 5 core + EconomyConfig 對映；每部位掉對應核心；既有測試全綠。
- [ ] PartDef 可掛多 emitter + movement + 跨部位閘門 + armor regen（皆可選、向後相容）。
- [ ] BossController 依資料驅動 per-part 發射；破部位→該 emitter 停（可觀測）。
- [ ] 部位移動（公轉/掃臂/旋轉/擺盪）依資料執行。
- [ ] 新 emitter(Spiral)/movement(DiveSwoop/HoverStrafe) 執行正確。
- [ ] 經濟 sink 決策記錄（新核心用途）— 待導演。
```
