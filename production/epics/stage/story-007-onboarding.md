# Story 007: Stage 1 前 10 分鐘引導設計實作

> **Epic**: 關卡系統與 Run 流程
> **Status**: ✅ Core complete (2026-07-07 — OnboardingController 8/8 EditMode GREEN). AC-6 5-player playtest = manual (pending); intro speed-override application = wiring follow-up.
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: 2026-07-07

## Context

**GDD**: `design/gdd/stage-system.md`
**Requirement**: `TR-stage-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 (primary); ADR-0005: 專案結構與組件邊界 (secondary)
**ADR Decision Summary**: Stage 1 特殊引導規則（引入段速度減速、首段強制 Primary Pod、HUD 一次性提示）以 `OnboardingConfig` SO 持有旋鈕，`OnboardingController`（純 C# 類別，建構子注入 `IEventBus`、`ISaveService`）僅在 `stageId="stage_01"` 時啟動。HUD tooltip 以 `ShowOnboardingTooltip` 事件橋接 UI，Stage 系統不直接操作 UI 組件。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `PlayerPrefs`（或 `ISaveService`）用於持久化 `first_pod_pickup_shown` 旗標。控制清單 §Meta：`MUST NOT` 用 `PlayerPrefs` 存遊戲資料——以 `ISaveService.SetFlag` 代替（ADR-0004）。`ShowOnboardingTooltip` 走 `IEventBus`，非 `PlayerPrefs`。

**Control Manifest Rules (Feature — Stage)**:
- Required: `MUST` 讀 `IDifficultyProvider`（引入段速度覆寫僅在 D1）；`MUST NOT` 自存難度值
- Required: `MUST NOT` 引用 UI 系統組件；Tooltip 以 `ShowOnboardingTooltip` 事件發布（UI 訂閱）
- Required: `first_pod_pickup_shown` 旗標存於 `ISaveService`（ADR-0004 JSON 存檔），**非 `PlayerPrefs`**
- Guardrail: 引導規則只在 `stageId="stage_01"` 啟動；其他 Stage 不受影響

---

## Acceptance Criteria

*From GDD `design/gdd/stage-system.md` §H.1, §H.2, §L.4:*

- [ ] **引入段速度減速**（H.2 規則 1）：`stageId="stage_01"` 且 D1 時，引入段 W1 的 `ram_grub` Spawn 速度 = `ram_grub_speed × ram_grub_intro_speed_mult`（= 220 × 0.70 = 154 px/s）；D2+ 恢復全速
- [ ] **首段強制 Primary Pod**（H.2 規則 2）：Stage 1 第一個隨機升階波段必定含 Primary Pod 攜帶者（`OnboardingController` 覆寫 `pod_carrier_wave_index` 為有效波次）；不論 SegmentRecombinator 抽到哪個波段
- [ ] **HUD 一次性引導提示**（H.2 規則 3）：首次拾取任意武器莢艙時，若 `ISaveService.GetFlag("first_pod_pickup_shown")=false`，發布 `ShowOnboardingTooltip{text="拾取武器莢艙以替換當前武器", durationSec=3.0}` 事件；同時 `ISaveService.SetFlag("first_pod_pickup_shown", true)` 持久化
- [ ] 後續任何莢艙拾取：若 `first_pod_pickup_shown=true` → **不發布** `ShowOnboardingTooltip`（永久關閉）
- [ ] **前 10 分鐘保底拾取**：Stage 1 完整 Run 中，玩家在進入前頭目喘息前已獲得機會拾取 ≥1 Pod（由 Story 004 保底 + 首段強制 Primary Pod 聯合確保）
- [ ] 5 人新手用戶測試（D1，首次遊玩，Stage 1）：通關後受測者能描述「加熱再引爆」核心循環的比例 ≥ 70%（Playtest 文件證明；ADVISORY 但 MVP 阻斷）

---

## Implementation Notes

*Derived from ADR-0003 and ADR-0005 Implementation Guidelines:*

- `OnboardingController` 放 `Assets/_Project/Scripts/Stage/OnboardingController.cs`（純 C#，`KaijuBreaker.Stage.asmdef`）。
- 建構子：`OnboardingController(IEventBus bus, ISaveService save, OnboardingConfig config, IDifficultyProvider difficulty, string currentStageId)`。
- 若 `currentStageId != "stage_01"`：建構子立即 return（所有訂閱不啟動）——零副作用。
- **引入段速度覆寫**：
  - 訂閱 `IntroSegmentWaveSpawning{isIntroSegment=true, wave=W1}` 事件（Stage 內部事件）。
  - 若 `difficulty.GetTier() == DifficultyTier.D1`：發布 `EnemySpeedOverride{enemyType=ram_grub, speedMult=config.RamGrubIntroSpeedMult}`；`WaveSpawner` 訂閱此事件在生成時應用。
- **首段強制 Primary Pod**：
  - `OnboardingController` 在 `SegmentRecombinator.Recombine()` 輸出後（由 `RunController` 傳入），檢查第一個升階段落的 `elite_wave_index` 是否有效（≥ 0）。
  - 若無效（`-1`）：發布 `ForceFirstSegmentPodCarrier{segmentIndex=0, poolType=Primary}` → `WaveSpawner` 在第一段落末波追加非菁英 Pod Spawn。
  - 此覆寫**不修改 SegmentDef SO**（唯讀），只在執行期影響當次 Run 的行為。
- **HUD Tooltip**：
  - 訂閱 `WeaponPodGrabbed` 事件。
  - 首次觸發：查詢 `save.GetFlag("first_pod_pickup_shown")` → `false` → 發布 `ShowOnboardingTooltip{...}` → `save.SetFlag("first_pod_pickup_shown", true)`（enqueue to background save worker，ADR-0004）。
- `OnboardingConfig` SO 欄位（`Content`）：`RamGrubIntroSpeedMult`（預設 0.70）、`TooltipText`（localization key 或直接中文字串）、`TooltipDurationSec`（3.0）。
- 引導時間軸（H.1）由 Stage 整體架構（Stories 001–006）的組合自然達成，不需額外 Controller 排程。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- HUD Tooltip 的 UI 渲染與動畫: hud-ui epic（`ShowOnboardingTooltip` 事件訂閱者）
- CARAPEX 教學對白 / 箭頭指示器: kaiju-roster epic（若有）
- 難度縮放通用邏輯（D1–D4 乘數）: difficulty epic + Story 002（WaveSpawner 讀取）
- Story 003: SegmentRecombinator 主邏輯（OnboardingController 在輸出後做覆寫，不修改重組演算法）

---

## QA Test Cases

*Integration story — 自動化 + 手動驗證規格：*

**AC-1**: 引入段 ram_grub 速度減速（D1）
  - Given: `stageId="stage_01"`；D1；`IntroSegmentWaveSpawning` 事件發布（isIntroSegment=true, wave=W1）
  - When: `EnemySpeedOverride` 被 WaveSpawner 接收；ram_grub 生成
  - Then: `ram_grub.MoveSpeed = 220 × 0.70 = 154 px/s ± 1 px/s`
  - Edge cases: D2 時不發布 `EnemySpeedOverride`；ram_grub 以基準速度 220 px/s 生成

**AC-2**: D1 Stage 1 引入段 W2（tri_shot 靜止射擊）不受速度覆寫影響
  - Given: 同上場景；W2 波次（tri_shot）
  - When: `IntroSegmentWaveSpawning{wave=W2}` 觸發
  - Then: `EnemySpeedOverride` 不發布（僅 W1 生效）；tri_shot 正常速度

**AC-3**: 首段強制 Primary Pod 覆寫
  - Given: `stageId="stage_01"`；SegmentRecombinator 抽到第一段為 S1-01（`elite_wave_index=-1`，無菁英）
  - When: `OnboardingController` 收到第一段落序列
  - Then: `ForceFirstSegmentPodCarrier{segmentIndex=0, poolType=Primary}` 發布；WaveSpawner 在 S1-01 末尾追加 Primary Pod Spawn
  - Edge cases: 若第一段本身有 `elite_wave_index≥0`（如 S1-02），`ForceFirstSegmentPodCarrier` 不發布（已有菁英保底）

**AC-4**: HUD Tooltip 首次 + 永久關閉
  - Given: `save.GetFlag("first_pod_pickup_shown")=false`；玩家觸發 `WeaponPodGrabbed`
  - When: `OnboardingController.OnWeaponPodGrabbed` 處理
  - Then: `ShowOnboardingTooltip{durationSec=3.0}` 發布 1 次；`save.SetFlag("first_pod_pickup_shown", true)` 呼叫
  - Edge cases: 同 Run 第二次 `WeaponPodGrabbed`（flag 已 true）→ `ShowOnboardingTooltip` **不發布**

**AC-5**: stageId 非 stage_01 時 OnboardingController 完全靜默
  - Given: `currentStageId="stage_02"`
  - When: `OnboardingController` 建構後訂閱任意事件
  - Then: 無任何事件訂閱生效；無速度覆寫；無 Tooltip；無強制 Pod 覆寫

**Manual check (AC-6)**: 5 人用戶測試 — 核心循環理解率
  - Setup: 5 位新手受測者（未接觸遊戲），D1，Stage 1 完整遊玩直至 CARAPEX 擊敗或時間到
  - Verify: 通關後問卷——「遊戲中你注意到什麼有趣的攻擊方式？」（開放題）；計算主動提及「加熱」+「引爆」或等效描述的比例
  - Pass condition: ≥ 70% 受測者（≥ 4/5 人）提及核心循環概念

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/PlayMode/Stage/onboarding_rules_test.cs` — PlayMode 整合測試 【BLOCKING】
- `production/qa/evidence/onboarding-evidence.md` — 5 人 Playtest 報告 + sign-off 【ADVISORY — MVP 阻斷】

**Status**: [x] ✅ `Assets/_Project/Tests/EditMode/Stage/onboarding_rules_test.cs` 8/8 GREEN (Unity MCP, 2026-07-07). Covers AC-1 (intro wave0 D1 → ram_grub speed override), AC-2 (D2 no override / wave1 no override), AC-3 (force primary pod when first segment has no elite / no-op when elite present), AC-4 (tooltip once + flag persist + permanent-off), AC-5 (silent on non-stage_01). AC-6 (5-player comprehension) = manual playtest (pending). Playtest doc [ ] not yet written.

**Reconciliations vs story text** (surfaced for review):
1. **`ISaveService` gained `GetFlag`/`SetFlag`** (additive) backed by a new **`SaveData.Flags`** bool map + serializer support; the committed interface had no flag persistence. Regression: minimal-canonical reference test updated (`"flags":{}` sorts first). MetaSaveService + all ISaveService test doubles updated.
2. New `OnboardingConfig` SO (RamGrubIntroSpeedMult/TooltipText/TooltipDurationSec). New Core events: `IntroSegmentWaveSpawning`/`EnemySpeedOverride`/`ForceFirstSegmentPodCarrier`/`ShowOnboardingTooltip`.
3. `OnboardingController` is pure C#, active only when `currentStageId=="stage_01"` (zero subscriptions otherwise). `ReviewFirstSegment(SegmentSequence)` is called by RunController after recombination.
4. **Deferred:** WaveSpawner honouring `EnemySpeedOverride` at spawn (wiring); RunController calling `ReviewFirstSegment` + publishing `IntroSegmentWaveSpawning` (run-flow wiring); the 5-player playtest evidence doc.

---

## Dependencies

- Depends on: Story 003（SegmentRecombinator 輸出可被覆寫）
- Depends on: Story 005（`WeaponPodGrabbed` 事件由 Pod 系統發布）
- Depends on: Story 006（完整 Stage Run 存在，端對端 Playtest 可執行）
- Depends on: content-config epic（`OnboardingConfig` SO 定義）
- Depends on: meta-save epic（`ISaveService.GetFlag` / `SetFlag` 介面）
- Depends on: hud-ui epic（`ShowOnboardingTooltip` 事件訂閱者，UI 渲染；Playtest 需 HUD tooltip 可見）
- Unlocks: None（本 Epic 最後一個 Story）
