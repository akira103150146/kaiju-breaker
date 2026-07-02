# Story 006: 頭目入場與 Run 場景過渡

> **Epic**: 關卡系統與 Run 流程
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/stage-system.md`
**Requirement**: `TR-stage-007`（STAGE → BOSS 狀態轉換）; `TR-stage-001`（前頭目喘息為固定段落）
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0005: 專案結構與組件邊界 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: Pre-Boss Lull 為 `RunController` 內部計時相位（非獨立 RunState），由 `StageDef.pre_boss_lull_duration`（`StageConfig` SO）控制時長。Boss Arena 場景以附加（additive）方式非同步預載，於喘息開始時啟動，確保轉換無頓幀。`RunController` 在喘息結束且場景就緒後呼叫 `EnterBoss()`；跨場景通訊全走 `IEventBus`。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `SceneManager.LoadSceneAsync(bossFightSceneName, LoadSceneMode.Additive)` 於 Unity 6.3 穩定。若使用 Addressables，需查 `docs/engine-reference/unity/VERSION.md` 確認 Addressables 版本 API（標記 **[需查證 6.3 API]**）。非同步場景讀取的 `allowSceneActivation` 控制需在實作時確認。

**Control Manifest Rules (Feature — Stage)**:
- Required: `MUST` 波段隨機重組 + 莢艙保底掉落；Pre-Boss Lull 非同步預載 `kaiju_<id>`
- Required: `MUST` `RunController.EnterBoss()` 呼叫觸發 `STAGE → BOSS` 狀態轉換（Story 001 定義）
- Required: 場景生命週期管理於 `App`（唯一組合根）或專屬 `SceneLoader` 服務；Stage 系統不直接呼叫 `SceneManager`
- Forbidden: `MUST NOT` 引用 KaijuParts / kaiju-roster 系統組件

---

## Acceptance Criteria

*From GDD `design/gdd/stage-system.md` §C.1, §G.1.3, §G.1.4, §L.1:*

- [ ] 最後升階波段完成後，`RunController` 進入 Pre-Boss Lull 相位：`WaveSpawner` 停止生成敵人
- [ ] Lull 開始同時：發布 `PreBossLullStarted{kaijuId}` 事件；GameFeel / Audio 訂閱切換音樂
- [ ] Lull 計時 `pre_boss_lull_duration`（預設 20s，來自 `StageConfig` SO）
- [ ] Lull 開始同時：非同步 additive 預載 Boss Arena 場景（`kaiju_<kaijuId>` scene name，由 `StageDef.bossSceneName` 提供）
- [ ] `PodDropTracker.SpawnPreBossLullPods()` 在 Lull 開始時呼叫（Story 004 邏輯）
- [ ] Boss 輪廓陰影在 Lull 期間顯示（`BossSilhouetteController`；placeholder sprite 可接受 MVP）
- [ ] Lull 到期 **AND** Boss Arena 場景已就緒（`AsyncOperation.isDone`）→ `RunController.EnterBoss()` 呼叫
- [ ] `RunController.EnterBoss()` 觸發 `RunState.STAGE → RunState.BOSS`（Story 001）；發布 `BossArenaEntered{kaijuId}` 事件
- [ ] 收到 `BossCoreBreak` 事件 → `RunController` 轉換至 `RESULTS`；發布 `HuntEnded`（Story 001 覆蓋）
- [ ] `HuntEnded` 後：Boss Arena 場景非同步卸載（additive unload）；返回 LOADOUT 場景
- [ ] Stage 場景到 Boss Arena 場景切換無明顯頓幀（additive 預載確保）

---

## Implementation Notes

*Derived from ADR-0005 Implementation Guidelines:*

- Pre-Boss Lull 為 `RunController` 的 `STAGE` 狀態內的子相位（`private enum StagePhase { Segments, PreBossLull }`），不是獨立 `RunState`。
- `StageConfig` SO（放 `Content`）持有：`preBossLullDuration`、`bossSceneName`（或由 `StageDef` 帶入）。
- `ISceneLoader` 介面（Core 定義）：`void LoadAdditiveAsync(string sceneName, Action onComplete)`；實作放 `App`（唯一引用 `SceneManager`）。`RunController` 建構子注入 `ISceneLoader`（測試時注入假實作，立即回呼 `onComplete`）。
- Lull 流程：
  1. `OnLastSegmentEnded()` → `_stagePhase = PreBossLull`；`waveSpawner.Stop()`；發布 `PreBossLullStarted`；呼叫 `sceneLoader.LoadAdditiveAsync(bossScene, OnBossSceneReady)`；啟動 `_lullTimer`
  2. `Update`: `_lullTimer` 遞減；到 0 且 `_bossSceneReady` → `EnterBoss()`
  3. 若場景在 lull 結束前未就緒，等待 `_bossSceneReady=true`（最多額外等待 3s，超時記錄警告）
- `BossSilhouetteController`（Stage 場景 GameObject）：訂閱 `PreBossLullStarted` → 淡入陰影 sprite；於 `BossArenaEntered` 後淡出（或隨場景卸載）。
- RESULTS → LOADOUT：`RunController.ConfirmResults()` 由 UI 呼叫（hud-ui epic）；Stage 系統發布 `RunEnded`；`App` 訂閱後卸載 Boss Arena 場景並載入 Loadout 場景。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `RunController` 狀態機邏輯與 autosave 觸發
- Story 004: Pre-Boss Lull 的 Pod 保底生成（本 Story 呼叫 `PodDropTracker.SpawnPreBossLullPods()`，Story 004 實作其邏輯）
- CARAPEX 攻擊模式 / 部位系統: kaiju-roster epic、kaiju-parts epic
- Results 畫面 UI 渲染: hud-ui epic
- 素材計算: economy epic

---

## QA Test Cases

*Integration story — PlayMode 自動化測試規格：*

**AC-1**: 最後段落完成後 Lull 啟動 + 波次停止
  - Given: RunController 狀態=STAGE；最後 SegmentDef 所有波次完成
  - When: `WaveSpawner` 最後一波完成後呼叫 `RunController.OnLastSegmentEnded()`
  - Then: `WaveSpawner.IsActive=false`（無新敵人生成）；`PreBossLullStarted` 事件發布；lullTimer 啟動
  - Edge cases: 若 Lull 已啟動，重複呼叫 `OnLastSegmentEnded` 無副作用

**AC-2**: Boss 場景 additive 預載 + 轉換無 stall
  - Given: 假 `ISceneLoader`（立即回呼 `onComplete`）；`pre_boss_lull_duration=20s`
  - When: Lull 啟動後模擬 20s 流逝
  - Then: `RunController.EnterBoss()` 在 20s ± 0.2s 被呼叫；`RunState == BOSS`
  - Edge cases: 場景載入慢（假延遲 5s）→ RunController 等待至 `_bossSceneReady=true` 才 `EnterBoss`

**AC-3**: `BossArenaEntered` 事件正確發布
  - Given: `RunController.EnterBoss()` 被呼叫；`kaijuId="carapex"`
  - When: 狀態轉換至 BOSS
  - Then: `BossArenaEntered{kaijuId="carapex"}` 事件發布（供 KaijuParts 系統訂閱啟動部位機）
  - Edge cases: `kaijuId` 與 `StageDef.bossKaijuId` 一致

**AC-4**: `BossCoreBreak` → RESULTS 轉換
  - Given: `RunController` 狀態=BOSS；假 `IEventBus`
  - When: 發布 `BossCoreBreak` 事件
  - Then: `RunController.CurrentState == RESULTS`；`HuntEnded` 事件發布（由 Story 001 驗證詳情）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/PlayMode/Stage/boss_transition_test.cs` — PlayMode 整合測試，必須全部通過 【BLOCKING】

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（`RunController.EnterBoss()` 定義）
- Depends on: Story 004（Pre-Boss Lull Pod 保底邏輯已實作）
- Depends on: Story 005（Pod 物件可由 Lull Pod 事件生成）
- Depends on: content-config epic（`StageConfig` / `StageDef` SO 含 `preBossLullDuration`、`bossSceneName`、`bossKaijuId`）
- Depends on: kaiju-roster epic（Boss Arena 場景存在可非同步載入）
- Unlocks: Story 007（完整 Stage Run 存在，Onboarding 才能以端對端方式驗證）
