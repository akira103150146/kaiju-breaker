# Story 002: SO 驅動雜兵 Prefab 生成（從 SegmentDef 波次引用）

> **Epic**: 關卡系統與 Run 流程
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/stage-system.md`
**Requirement**: `TR-stage-001`（部分：波次執行與 Prefab 生成）
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 (primary); ADR-0005: 專案結構與組件邊界 (secondary)
**ADR Decision Summary**: `SegmentDef` 波次資料只紀錄 Prefab 引用與佈局描述，行為完全住在 Prefab 所附的 `MovementPatternSO` 與 `EmitterPatternSO` 中（ADR-0003「所有調校旋鈕以 ScriptableObject 表達」）。`WaveSpawner` 為純 Stage 組件，建構子注入 `IDifficultyProvider`（唯讀介面）與 `EnemyConfig`（SO），不直接引用其他 Feature 系統。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `Object.Instantiate` 用於 Prefab 生成；預期 Unity 6.3 API 穩定。熱路徑生成應改為物件池（初期 MVP 可用 Instantiate，效能剖析後補池）。`Time.time` 做波次計時。

**Control Manifest Rules (Feature — Stage)**:
- Required: `MUST` 讀 `IDifficultyProvider` 取密度 / 敵量乘數，不自存
- Required: 波次資料 `MUST` 只紀錄引用，行為完全住在 Prefab 與 SO 中（ADR-0003 落地）
- Forbidden: `MUST NOT` 引用其他 Feature 系統；`MUST NOT` 硬編碼 gameplay / balance 數值
- Guardrail: `MUST NOT` 熱路徑 `Instantiate`/`Destroy`；初期 MVP 可用，後續物件池改善

---

## Acceptance Criteria

*From GDD `design/gdd/stage-system.md` §D.2, §E.0, §E.1, §L.1:*

- [ ] `WaveSpawner` 讀 `SegmentDef.waves[]`，按 `spawnTime` 偏移依序生成每波敵人 Prefab
- [ ] 生成數量：`actual_count = Mathf.CeilToInt(wave.count × IDifficultyProvider.GetEnemyCountMult())`；乘數來自 `IDifficultyProvider`（不自存）
- [ ] 每個生成的 Enemy 持有 `MovementPatternSO` 引用（由 Prefab 組件接收，驅動入場路徑與移動行為）
- [ ] 每個生成的 Enemy 持有 `EmitterPatternSO` 引用（資料已接線；執行期射擊見「Out of Scope」）
- [ ] `EnemyDef` SO 提供 `hp_tier`、接觸傷害值、點數價值、`is_elite` 旗標（於生成時讀取）
- [ ] `SegmentDef.waves[]` 中**不含任何行為邏輯或數值**——僅 `enemyPrefabId`（字串引用）、`spawnPosition`（佈局枚舉）、`spawnTime`（float）、`count`（int）
- [ ] Stage 1 四種 MVP 小怪（`ram_grub`, `tri_shot`, `aimed_gun`, `ring_burst`）從 S1 波段池正確生成
- [ ] `spawnTime` 偏移對應段落開始後的秒數（容差 ±0.1s）

---

## Implementation Notes

*Derived from ADR-0003 and ADR-0005 Implementation Guidelines:*

- `WaveSpawner` 放 `Assets/_Project/Scripts/Stage/WaveSpawner.cs`（`KaijuBreaker.Stage.asmdef`）。
- 建構子注入：`WaveSpawner(IEventBus bus, IDifficultyProvider difficulty, EnemyConfig enemyConfig)`。
- `EnemyConfig` SO（`KaijuBreaker.Content.asmdef`）持有字串 ID → Prefab 對照表（`Dictionary<string, GameObject>`，Editor 填寫）。
- 生成流程：
  1. 讀 `SegmentDef.waves[]` → 按 `spawnTime` 排序（或由設計師保序）
  2. 計時器驅動（`Update` 或 Coroutine），到達 `spawnTime` 後執行波次
  3. 取 `wave.count`，乘以 `difficulty.GetEnemyCountMult()`，取上限
  4. 查 `enemyConfig.GetPrefab(wave.enemyPrefabId)` → `Object.Instantiate`
  5. 取得生成實例上的 `EnemyController` 組件 → 呼叫 `Init(EnemyDef def, MovementPatternSO movement, EmitterPatternSO emitter)` 注入 SO
- `EnemyDef` 同樣從 `EnemyConfig` 以 `enemyPrefabId` 查取（或由 Prefab 自帶預設，Init 可覆寫）。
- `spawnPosition` 映射至佈局函數（`horizontal_spread`、`center` 等），由 `SpawnLayoutHelper` 靜態工具解算座標。
- 所有 public 方法具 doc comment；`WaveSpawner` 可注入假 `IDifficultyProvider` fixture 做整合測試。

> **⚠️ BLOCKED（部分）**: `EmitterPatternSO` **執行期射擊**（SO → 彈幕 Spawn）依賴 `BulletSim`，而 BulletSim 後端受 ADR-0001（Proposed，待手機效能閘門）控制。本 Story 僅覆蓋 Prefab 生成與 SO 接線——敵人的實際子彈發射整合列入「Out of Scope」，待 ADR-0001 Accepted 後另行 Story 補完。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `RunController` 狀態機（WaveSpawner 被 RunController 啟動，但本 Story 假設 RunController 存在）
- Story 003: 波段隨機重組（哪些 SegmentDef 被選中由 SegmentRecombinator 決定）
- Story 004: 菁英怪特殊規則（is_elite 旗標已讀取，但菁英 HP 倍率 + 莢艙掉落由 Story 004 實作）
- **EmitterPatternSO 執行期射擊（enemy bullet emission）**: 依賴 BulletSim ADR-0001（Proposed），**depends-on-blocked**；敵人生成後只移動，不發射子彈，直至 ADR-0001 閘門通過

---

## QA Test Cases

*Integration story — PlayMode 自動化測試規格：*

**AC-1**: D1 基準生成數量
  - Given: `SegmentDef` wave = `{ enemyPrefabId: "tri_shot", count: 3 }`；D1（`GetEnemyCountMult()=1.0`）
  - When: Wave 執行
  - Then: 場景中生成 3 個 tri_shot 實例
  - Edge cases: count=0 應生成 0 個實例（不崩潰）

**AC-2**: D3 難度乘數縮放
  - Given: 同上 wave；D3（`GetEnemyCountMult()=1.5`）
  - When: Wave 執行
  - Then: `ceil(3 × 1.5) = 5` 個 tri_shot 實例
  - Edge cases: 浮點上限應用 `Mathf.CeilToInt`（非 `Round`）

**AC-3**: SO 引用接線驗證
  - Given: 任意 SegmentDef wave 生成敵人實例
  - When: 檢查每個實例的 `EnemyController` 組件
  - Then: `MovementPatternSO != null`；`EmitterPatternSO != null`；`EnemyDef != null`
  - Edge cases: Prefab ID 不存在 → 記錄錯誤，不崩潰；返回無效實例計數 0

**AC-4**: `spawnTime` 偏移正確
  - Given: SegmentDef with wave A at `spawnTime=0.0`、wave B at `spawnTime=4.0`；segment start at `t=0`
  - When: Segment 執行 5s
  - Then: wave A 在 `t ≈ 0.0s ± 0.1s` 生成；wave B 在 `t ≈ 4.0s ± 0.1s` 生成
  - Edge cases: `spawnTime` 精度在 `Time.deltaTime` 範圍內允許誤差

**AC-5**: SegmentDef 行為欄位驗證（ADR-0003 合規）
  - Given: 讀取所有 Stage 1 SegmentDef SO 資產
  - When: 反射或 `OnValidate` 檢查 waves[] 元素
  - Then: 無任何 wave 元素包含行為邏輯欄位（速度、HP、角度等）；僅含 `enemyPrefabId`, `spawnPosition`, `spawnTime`, `count`

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/PlayMode/Stage/enemy_spawning_test.cs` — PlayMode 整合測試，必須全部通過 【BLOCKING】

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（`RunController` 存在，WaveSpawner 被其啟動）
- Depends on: content-config epic（`StageDef`, `SegmentDef`, `EnemyDef`, `EnemyConfig`, `MovementPatternSO`, `EmitterPatternSO` SO 定義已存在於 `KaijuBreaker.Content`）
- Depends on: difficulty epic（`IDifficultyProvider` 介面定義與實作）
- **depends-on-blocked**: ADR-0001（BulletSim，Proposed）— `EmitterPatternSO` 執行期 bullet emission 整合待 ADR-0001 Accepted 後另行 Story
- Unlocks: Story 003（重組演算法輸出段落序列供 WaveSpawner 消費）；Story 004（菁英生成在 WaveSpawner 之上）
