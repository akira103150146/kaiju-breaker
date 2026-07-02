# Story 005: EmitterPatternSO + MovementPatternSO + EnemyDef ScriptableObjects

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: M (4h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/stage-system.md` §E.0（敵人定義架構）
**Requirement**: `TR-content-002`, `TR-content-001`, `TR-content-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: `EmitterPatternSO` 為彈幕發射模式的**靜態資料容器**（Authoring Layer）；`MovementPatternSO` 為敵人移動行為的靜態描述。兩者均唯讀。`EnemyDef` 含敵人統計基值並引用前兩者。

> **Secondary ADR Note**: ADR-0001（BulletSim DOTS 架構）狀態為 **Proposed**（尚未 Accepted）。本故事只建立 `EmitterPatternSO` 的 C# 類（Authoring Layer），不實作 Burst Blob 烘焙（Blob 轉換屬 ADR-0001 範疇，待 ADR-0001 Accept 後再實作）。

**Engine**: Unity 6.3 | **Risk**: LOW（SO 本體）/ MEDIUM（若未來加入 Blob 烘焙，需待 ADR-0001 驗證）
**Engine Notes**: 本故事僅建 SO 類；`Unity.Entities`（DOTS）不在此引用，Blob 轉換延至 ADR-0001 Accept 後的 BulletSim 故事處理。

**Control Manifest Rules (this layer)**:
- Required: `KaijuBreaker.Content.asmdef`；三類 SO 均為純資料容器；`OnValidate` 驗證安全範圍
- Forbidden: 引用 `Unity.Entities`、DOTS Job System、或任何系統組件；運行期 AI 或移動邏輯
- Guardrail: `EnemyDef` 僅含統計基值 + SO 引用；移動/發射行為邏輯在 `KaijuBreaker.Stage` / `KaijuBreaker.BulletSim` 系統

---

## Acceptance Criteria

*From `stage-system.md` §E.0 and §K.4 (mob knobs), governed by ADR-0003:*

**EmitterPatternSO**
- [ ] `EmitterPatternSO` C# class 位於 `Assets/_Project/Scripts/Content/`；`[CreateAssetMenu(menuName = "KaijuBreaker/EmitterPatternSO")]`
- [ ] 持有發射模式基本欄位：`PatternType`（`EmitterPatternType` enum：`Linear`/`Radial`/`Aimed`/`RingBurst`）、`BulletCountBase`（int）、`FireIntervalSeconds`（float）、`BulletSpeedPxPerSec`（float）、`SpreadAngleDeg`（float）、`BulletLifetimeSeconds`（float）；含 Tier-3 Elite 密度欄位：`EliteDensityMult`（float，1.0）
- [ ] `EmitterPatternSO.OnValidate()` 斷言：`BulletCountBase` ∈ [1, 20]；`FireIntervalSeconds` > 0；`SpreadAngleDeg` ∈ [0, 360]；`EliteDensityMult` ∈ [1.0, 3.0]
- [ ] MVP Stage 1 發射模式 assets（位於 `Assets/_Project/Content/Kaiju/`）：`TriShot_Emitter.asset`（3 bullets，2.5s，130 px/s，30°）、`AimedGun_Emitter.asset`（1 bullet，3.5s，110 px/s，0°）、`RingBurst_Emitter.asset`（8 bullets，Radial，4.0s，100 px/s）

**MovementPatternSO**
- [ ] `MovementPatternSO` C# class 位於同組件；`[CreateAssetMenu(menuName = "KaijuBreaker/MovementPatternSO")]`
- [ ] 持有移動行為欄位：`MovementType`（`MovementType` enum：`StraightRush`/`HorizontalDrift`/`Hover`/`UTurn`/`Sinusoidal`）、`MoveSpeedPxPerSec`（float）、`IntroSpeedMult`（float，1.0 = 無特殊入場速度）、`AmplitudePx`（float，蛇行振幅，StraightRush 時為 0）、`FrequencyHz`（float，蛇行頻率）
- [ ] `MovementPatternSO.OnValidate()` 斷言：`MoveSpeedPxPerSec` > 0；`IntroSpeedMult` ∈ (0.0, 2.0]；`AmplitudePx` ≥ 0
- [ ] MVP Stage 1 移動模式 assets（`Assets/_Project/Content/Kaiju/`）：`RamGrub_Movement.asset`（StraightRush，220 px/s，IntroSpeedMult=0.7）、`TriShot_Movement.asset`（HorizontalDrift，80 px/s）、`AimedGun_Movement.asset`（Hover，60 px/s）、`RingBurst_Movement.asset`（UTurn，100 px/s）

**EnemyDef**
- [ ] `EnemyDef` C# class 位於同組件；`[CreateAssetMenu(menuName = "KaijuBreaker/EnemyDef")]`
- [ ] 持有 stage-system.md E.0 統計欄位：`EnemyId`（string）、`HpTier`（`HpTier` enum：`T1`/`T2`）、`ContactDamage`（float）、`PointValue`（int）、`IsElite`（bool）、`EliteHpMult`（float，1.0）、`EliteDensityMult`（float，1.0）、`EliteShardBonus`（int，0）、`EliteAuraColor`（`Color`）；SO 引用：`MovementPattern`（`MovementPatternSO`）、`EmitterPattern`（`EmitterPatternSO`，nullable—RamGrub 無發射器）
- [ ] `EnemyDef.OnValidate()` 斷言：`EnemyId` 非空；`ContactDamage` > 0；`IsElite` 為 true 時 `EliteHpMult` ≥ 1.0；`MovementPattern` 非 null
- [ ] MVP Stage 1 EnemyDef assets（`Assets/_Project/Content/Kaiju/`）：`RamGrub.asset`（T1，contact 15，pts 10）、`TriShot.asset`（T1，contact 10，pts 15）、`AimedGun.asset`（T2，contact 12，pts 20）、`RingBurst.asset`（T2，contact 10，pts 25）；Elite 變體：`RamGrub_Elite.asset`、`TriShot_Elite.asset`、`AimedGun_Elite.asset`、`RingBurst_Elite.asset`（EliteHpMult/EliteDensityMult 從 stage-system.md K.4 填值）

---

## Implementation Notes

*Derived from ADR-0003 and stage-system.md §E.0:*

`EmitterPatternSO` 不含任何 `NativeArray`、`BlobAssetReference` 或 DOTS 類型（那是 ADR-0001 的 BulletSim 故事範疇）。本故事的 `EmitterPatternSO` 只是標準 Unity ScriptableObject，供 Stage 系統（Unity 物件導向部分）在非 DOTS 路徑下讀取；DOTS 路徑烘焙留給 ADR-0001。

`EmitterPattern` 欄位在 `EnemyDef` 可為 null（RamGrub 無射擊行為）；`OnValidate` 對 null EmitterPattern 不報錯，Stage 系統負責空值處理。

Elite 變體建議複製基礎 EnemyDef.asset 再修改 `IsElite = true`、乘數欄位，`MovementPattern` 與 `EmitterPattern` SO 引用與基礎版共用（不複製 SO，只在 EnemyDef 層區分）。

資產放 `Assets/_Project/Content/Kaiju/` 下的 `Emitters/`、`Movements/`、`Enemies/` 子目錄以維持可讀性。

---

## Out of Scope

- [ADR-0001 BulletSim 故事]: `EmitterPatternSO` → Burst-friendly Blob 烘焙轉換（ADR-0001 Proposed 中，待 Accept）
- [Story 008]: ContentRegistry 依 EnemyId 查找 EnemyDef
- [Story 009]: 三類 SO 的 fixture 工廠
- Stage 2/3 新敵人類型的 SO assets（Stage 2/3 內容故事補充；本故事只建 Stage 1 MVP 集合）
- AI 行為樹或路徑系統（`KaijuBreaker.Stage` 系統職責）
- `ShieldFlier`、`ColumnGrunt` 等 Stage 1 後半新敵人（stage-system.md K.4 後半段，納入下一批 Stage 內容故事）

---

## QA Test Cases

*Config/Data — manual smoke check steps:*

- **AC-1**: MVP EmitterPattern assets 欄位正確
  - Setup: 選取 `TriShot_Emitter.asset`
  - Verify: `BulletCountBase = 3`、`FireIntervalSeconds = 2.5`、`BulletSpeedPxPerSec = 130`、`SpreadAngleDeg = 30`、`PatternType = Aimed`
  - Pass condition: 值與 stage-system.md K.4 tri_shot 旋鈕完全一致

- **AC-2**: EmitterPatternSO OnValidate 越界偵測
  - Setup: 選取任一 EmitterPattern.asset；將 `FireIntervalSeconds` 改為 `0`
  - Verify: Console `LogError` 含 `FireIntervalSeconds`（必須 > 0）
  - Pass condition: 還原正值後無錯誤

- **AC-3**: MVP MovementPattern assets 欄位正確
  - Setup: 選取 `RamGrub_Movement.asset`
  - Verify: `MovementType = StraightRush`、`MoveSpeedPxPerSec = 220`、`IntroSpeedMult = 0.7`
  - Pass condition: 符合 K.4 ram_grub 旋鈕

- **AC-4**: EnemyDef Elite OnValidate
  - Setup: 開啟 `TriShot_Elite.asset`；將 `EliteHpMult` 改為 `0.5`（低於 1.0，而 IsElite = true）
  - Verify: Console `LogError` 含 `EliteHpMult`（Elite 時 Mult 不得 < 1.0）
  - Pass condition: 還原 ≥ 1.0 後無錯誤

- **AC-5**: RamGrub EmitterPattern null 不觸發 OnValidate 錯誤
  - Setup: 開啟 `RamGrub.asset`；確認 `EmitterPattern` 欄位為 None（null）
  - Verify: Console 無因 EmitterPattern = null 產生的 `LogError`
  - Pass condition: OnValidate 不對 null EmitterPattern 報錯

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass — `production/qa/smoke-content-config.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None（可與 001–004 並行開發）
- Unlocks: Story 006 (StageDef 引用 EnemyDef / EmitterPatternSO 類型), Story 008, Story 009
