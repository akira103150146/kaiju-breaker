# Story 001: Run 狀態機 LOADOUT → STAGE → BOSS → RESULTS

> **Epic**: 關卡系統與 Run 流程
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/stage-system.md`
**Requirement**: `TR-stage-007`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0005: 專案結構與組件邊界 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: Run 狀態機以純 C# `RunController` 實作於 `KaijuBreaker.Stage` 組件；`RunState` 列舉定義於 `KaijuBreaker.Core`（零依賴組件），可假事件驅動做 EditMode 單元測試。所有 autosave 觸發點由 `ISaveService`（介面，Core 定義）以建構子注入。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 狀態機為純 C# 邏輯，不使用 Unity API；無 post-cutoff 風險。`ISaveService.EnqueueSave()` 行為需在 meta-save 實作時對齊 Unity 6.3 `Application.persistentDataPath` 語義。

**Control Manifest Rules (Feature — Stage)**:
- Required: `MUST` 驅動 Run 狀態機 `LOADOUT → STAGE → BOSS → RESULTS`（`RunController`，純 C# 可測）
- Required: `MUST` 在對的轉換點觸發 autosave（`on_loadout_confirmed`、莢艙拾取、部位破壞、`on_hunt_end`）
- Forbidden: `MUST NOT` 引用其他 Feature 系統（`Difficulty` 唯讀例外，經介面）
- Guardrail: `MUST NOT` 持有遊戲狀態的 static 單例；系統以建構子 / 方法注入依賴

---

## Acceptance Criteria

*From GDD `design/gdd/stage-system.md` §EPIC.md TR-stage-007 and control-manifest §3 Stage:*

- [ ] `RunState` 列舉（`LOADOUT`, `STAGE`, `BOSS`, `RESULTS`）定義於 `KaijuBreaker.Core` 組件
- [ ] `RunController` 為純 C# 類別，位於 `KaijuBreaker.Stage` 組件，建構子注入 `IEventBus` 與 `ISaveService`
- [ ] 初始狀態 = `LOADOUT`；收到 `on_loadout_confirmed` 事件 → 轉換至 `STAGE`
- [ ] `STAGE` 所有升階波段 + 前頭目喘息完成後 → `RunController.EnterBoss()` 呼叫轉換至 `BOSS`
- [ ] 收到 `on_boss_core_break` 事件 → 轉換至 `RESULTS`，發出 `HuntEnded` 事件
- [ ] `RESULTS` 確認 → 轉換回 `LOADOUT`；回到 Loadout 場景
- [ ] 每次狀態轉換發出 `RunStateChanged{from, to}` 事件（`Core` struct）
- [ ] Autosave enqueue 觸發點：`on_loadout_confirmed`、`WeaponPodGrabbed`、`on_part_break`、`HuntEnded`
- [ ] `RunController` 可用假 `IEventBus` + 假 `ISaveService` 在 EditMode 測試，不依賴場景

---

## Implementation Notes

*Derived from ADR-0005 Implementation Guidelines:*

- `RunState` enum 放 `Assets/_Project/Scripts/Core/RunState.cs`（`KaijuBreaker.Core.asmdef`）。
- `RunController` 放 `Assets/_Project/Scripts/Stage/RunController.cs`（`KaijuBreaker.Stage.asmdef`）。
- 建構子簽名：`RunController(IEventBus bus, ISaveService save)`。
- 訂閱事件：`bus.Subscribe<LoadoutConfirmed>(OnLoadoutConfirmed)`、`bus.Subscribe<BossCoreBreak>(OnBossCoreBreak)`。
- 狀態轉換方法為 `private void TransitionTo(RunState next)` — 驗證合法轉換、發出 `RunStateChanged`、觸發 `save.EnqueueSave()`（保留供 meta-save 接線）。
- `EnterBoss()` 為 `public` 方法，由 Stage 流程控制器（wave/lull 排程）呼叫；不直接由事件驅動（防止意外觸發）。
- 所有 public 方法具 doc comment；cyclomatic complexity ≤ 10；方法 ≤ 40 行。
- **DI over singletons**：`App` 組件（唯一組合根）建構 `RunController` 並佈線；`Stage` 不引用 `App`。
- Autosave 訂閱在建構子完成後由 `App` 手動呼叫 `Subscribe`，或在 `RunController` 建構子內訂閱（視 `IEventBus` 生命週期而定，於 `App` 佈線時確認）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: 波段生成（WaveSpawner）與 Enemy Prefab 實例化
- Story 003: 波段隨機重組演算法（SegmentRecombinator）
- Story 004: 菁英怪生成 + 莢艙保底追蹤
- Story 005: 莢艙下降 / 循環 / 拾取行為
- Story 006: 前頭目喘息時序 + 頭目場景非同步載入
- Story 007: Stage 1 引導特殊規則

---

## QA Test Cases

*Written at story creation. The developer implements against these.*

**AC-1**: `LOADOUT → STAGE` 轉換
  - Given: `RunController` 初始狀態 = `LOADOUT`；使用假 `IEventBus`
  - When: 發布 `LoadoutConfirmed` 事件
  - Then: `RunController.CurrentState == RunState.STAGE`；`RunStateChanged{from=LOADOUT, to=STAGE}` 被發出
  - Edge cases: 在非 `LOADOUT` 狀態發布 `LoadoutConfirmed` 應拋 `InvalidOperationException`

**AC-2**: `EnterBoss()` 觸發 `STAGE → BOSS`
  - Given: 狀態 = `STAGE`
  - When: 呼叫 `RunController.EnterBoss()`
  - Then: `CurrentState == BOSS`；`RunStateChanged{from=STAGE, to=BOSS}` 發出
  - Edge cases: 在 `LOADOUT` 或 `RESULTS` 呼叫 `EnterBoss()` 應為非法操作

**AC-3**: `BOSS → RESULTS` via `on_boss_core_break`
  - Given: 狀態 = `BOSS`；假 `IEventBus`
  - When: 發布 `BossCoreBreak` 事件
  - Then: `CurrentState == RESULTS`；`HuntEnded` 事件發出；`ISaveService.EnqueueSave()` 被呼叫 1 次
  - Edge cases: `BossCoreBreak` 在 `STAGE` 狀態到達應被忽略（非 BOSS 狀態不處理）

**AC-4**: Autosave 觸發點驗證
  - Given: 假 `ISaveService` 記錄 `EnqueueSave()` 呼叫次數；狀態走完整 `LOADOUT→STAGE→BOSS→RESULTS`
  - When: 模擬 `LoadoutConfirmed`、2× `WeaponPodGrabbed`、1× `on_part_break`、`BossCoreBreak` 事件
  - Then: `EnqueueSave()` 呼叫 ≥ 4 次（每個觸發點各至少 1 次）
  - Edge cases: 同幀連續 `on_part_break` 只 enqueue 一次（佇列深度 1 覆蓋式，meta-save ADR-0004）

**AC-5**: 無效轉換防護
  - Given: 狀態 = `RESULTS`
  - When: 呼叫 `EnterBoss()`
  - Then: `InvalidOperationException` 或安全 early-return（不崩潰，記錄警告）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Stage/run_state_machine_test.cs` — EditMode 單元測試，必須全部通過 【BLOCKING】

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: core-foundation epic（`IEventBus`、`ISaveService` 介面、`RunState` enum 放 `Core` 組件必須先存在）
- Unlocks: Story 002（生成器需知曉 Run 狀態）；Story 006（頭目過渡呼叫 `RunController.EnterBoss()`）
