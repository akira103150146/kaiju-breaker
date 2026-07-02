# Story 006: 重映射與無障礙基線 (Remapping & Accessibility Baseline)

> **Epic**: 輸入系統 (Input System)
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: TBD — fill before sprint planning
> **Manifest Version**: 2026-07-02
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-005`, `TR-input-006`, `TR-input-002`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; tr-registry not yet formalized; TR-IDs cover L.5 accessibility baseline [BLOCKING], L.6 charge interrupt grace [Advisory], L.7 mis-tap rate [Advisory], and L.2 cross-scheme parity [BLOCKING pre-VS])*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 (primary — SO defaults + save JSON player overrides); ADR-0005: 專案結構與組件邊界 (secondary)

**ADR Decision Summary**: ADR-0003 — player rebinding, charge mode (Hold/Toggle), vibration toggle, and touch layout stored in save JSON (ADR-0004), never written back to `InputSettings` SO; SO holds designer defaults only. ADR-0005 — remapping logic lives within `KaijuBreaker.Input`; remapping UI lives in `KaijuBreaker.UI`; communication between them via `IEventBus` or query interface, not direct reference.

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**: Unity Input System runtime rebinding API (`InputActionRebindingExtensions.PerformInteractiveRebinding`) [需查證 6.3 API] — verify rebinding UI pattern and how to serialize custom binding overrides (JSON override string vs full binding save) against `docs/engine-reference/unity/VERSION.md`. Conflict detection API (if any built-in) [需查證]. `InputAction.ApplyBindingOverride()` save/restore pattern [需查證 6.3 API].

**Control Manifest Rules (this layer)**:
- Required: All player rebinding persisted to save JSON via `ISaveService` (ADR-0004); SO defaults never overwritten (ADR-0003 §2); conflict detection blocks save of duplicate bindings (GDD H.1); L3 Toggle mode functionally equivalent to Hold mode (L.5); READY text label preserved in all implementations (L.5); vibration fully disableable (L.5)
- Forbidden: Writing rebinding data back to `InputSettings` SO (§5); hover-only UI for rebinding interface (§3 Input); silently accepting conflicting bindings (GDD H.1 explicitly requires warning + block save)
- Guardrail: Save round-trip for rebinding must survive app restart (Integration test required); L.2 cross-scheme parity is a playtest gate — this story is DONE only when playtest evidence is collected

---

## Acceptance Criteria

*From GDD `design/gdd/input-system.md` §H.1–H.5, §I, §L.2, §L.5–L.7, scoped to this story:*

- [ ] **全面重映射 — 鍵盤（H.1）**：FireSecondary、ChargePrimary / ReleasePrimary、Pause、全部 UI 動作可在設定介面重新映射至任意鍵
- [ ] **全面重映射 — 滑鼠（H.1）**：FireSecondary 與 ChargePrimary 可在左鍵 / 右鍵 / 中鍵間互換
- [ ] **全面重映射 — 手柄（H.1）**：全部動作可重映射；左搖桿 X / Y 軸可分別翻轉（Axis Invert）
- [ ] **觸控布局（H.1）**：設定中提供左手模式 / 右手模式切換（副武器按鈕位置）；蓄力按鈕位置選項（左上 / 右上）；`touch_visual_offset_y` 可在設定範圍內玩家自訂
- [ ] **映射衝突偵測（H.1）**：兩個不同動作映射至同一按鍵時，介面顯示衝突警告；不允許儲存衝突映射；原映射保持不變
- [ ] **L3 蓄力 Toggle 模式（H.2，L.5）**：第一次按下 ChargePrimary 按鍵開始計時；`l3_charge_time` 結束後自動發送 ReleasePrimary 事件（全幅震波），無需持續按壓；與 Hold 模式功能等價（相同的蓄力時間、相同的震波效果）
- [ ] **震動回饋可完全關閉（F.2，L.5）**：設定開關關閉後，手柄馬達不被呼叫，視覺回饋（蓄力環 + READY 文字）完整替代觸覺回饋
- [ ] **READY 文字標示保留（D.3，L.5）**：蓄力完成狀態以「READY」文字（或等效非顏色辨識手段）標示；任何實作版本不得以顏色變化作為唯一提示
- [ ] **重映射設定存入 save JSON（ADR-0004）**：所有玩家覆寫（按鍵 binding、觸控布局、蓄力模式、震動開關、`touch_visual_offset_y` 玩家調整值）寫入存檔；`InputSettings` SO 不被修改
- [ ] **L3 蓄力中斷寬限（Advisory，L.6）**：蓄力進行至 1.0s 時短暫放開，在 `l3_charge_interrupt_grace_period`（0.3s）內重新按下，計時從 1.0s 繼續（不歸零）；10 次手動測試 ≥8 次成功則保留此設計；若成功率過低則移除並更新 `InputSettings` SO 的預設值
- [ ] **跨方案等價驗收（L.2，BLOCKING pre-Vertical Slice）**：三方案玩家各完成原型 Boss 戰（標準難度）；觸控部位破壞數中位數 ≥ KB+Mouse 中位數 × 0.8；三方案 L3 成功率各 ≥80%（10 次嘗試 ≥8 次）——playtest 驗收，結果記錄於 `production/qa/evidence/cross-scheme-parity-evidence.md`

---

## Implementation Notes

*Derived from ADR-0003 (primary) and ADR-0005:*

**重映射資料流**：Unity Input System 提供 `InputActionRebindingExtensions.PerformInteractiveRebinding()` [需查證 6.3 API] 觸發玩家按鍵聆聽流程。完成後以 `action.ApplyBindingOverride(bindingIndex, path)` [需查證] 套用；序列化為 JSON override string 並由 `ISaveService` 寫入 save。載入時從 save 讀取 override string，呼叫 `InputActionAsset.LoadBindingOverridesFromJson()` [需查證 6.3 API] 還原。

**衝突偵測邏輯**：重映射 UI 嘗試儲存前，掃描所有 Action 的 effective binding path；若 path 已被另一 Action 使用，顯示警告 UI 並 abort 儲存。此邏輯在 `KaijuBreaker.Input` 組件內；衝突警告文字由 `KaijuBreaker.UI` 渲染（透過事件通知，不直接引用）。

**Toggle 模式實作**：
```
// pseudocode
if (chargeMode == Toggle) {
    if (WasPressedThisFrame(ChargePrimaryAction)) {
        isChargingToggle = !isChargingToggle;
        if (isChargingToggle) StartChargeTimer();
    }
    if (isChargingToggle && chargeTimer >= l3ChargeTime) {
        FireReleasePrimary();
        isChargingToggle = false;
    }
}
```
Toggle 模式與 Hold 模式共用相同的 `ChargeProgress` 計算，保證 HUD 蓄力環顯示一致。

**蓄力中斷寬限（L.6，Advisory）**：
```
// pseudocode — only active if l3_charge_interrupt_grace_period > 0
if (ReleasedChargePrimary && chargeProgress > 0) {
    gracePeriodTimer = l3ChargeInterruptGracePeriod;
}
if (gracePeriodTimer > 0 && WasPressedThisFrame(ChargePrimary)) {
    // resume from current chargeProgress, do not reset timer
    gracePeriodTimer = 0;
}
```
此設計為 GDD 假說——實作後進行 10 次手動測試，結果記錄於 evidence doc。若成功率 <8/10，disable 此功能（設 `l3_charge_interrupt_grace_period = 0` 於 SO 並記錄 ADR）。

**存檔邊界**：`InputSettings` SO 欄位 = 設計師設定（build 時固定）；save JSON `inputOverrides` key = 玩家在遊戲內的所有設定變更。載入時 save 覆蓋 SO 預設——但若 save 缺少某個 key（新欄位版本遷移），fallback 到 SO 預設（ADR-0004 的版本遷移邏輯）。

**跨方案等價 playtest 協調**：此 story 的 L.2 playtest 需在 Stories 003、004、005 全部 DONE 後執行。lead programmer 需協調 QA 安排三方案各自的 playtest session；evidence doc 由 qa-lead 簽核。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 003** (touch-scheme)：觸控核心拖曳邏輯、按鈕 UI 位置初始設定
- **Story 004** (kb-mouse-scheme)：鍵盤 / 滑鼠移動及開火實作
- **Story 005** (gamepad-scheme)：搖桿速度曲線、震動事件訂閱實作
- 重映射 UI 的視覺渲染細節（由 UI 系統 Story 負責；本 story 提供邏輯 + 事件介面）
- 新武器或難度系統 gameplay 內容

---

## QA Test Cases

*Integration story — automated test specs and manual verification steps apply.*

- **AC-1**: 重映射衝突偵測
  - Given: FireSecondary 當前 binding = Space；準備將 ChargePrimary 也映射至 Space
  - When: 觸發重映射保存流程
  - Then: 衝突偵測旗標 = true；`IEventBus` 發送 BindingConflictDetected 事件（UI 顯示警告）；FireSecondary = Space binding 不變；ChargePrimary binding 不更新
  - Edge cases: 同一動作重映射至相同鍵（FireSecondary → Space 再次映射 Space）→ 不視為衝突（自我重映射）；不同動作不同鍵 → 無衝突，正常儲存

- **AC-2**: L3 Toggle 模式功能等價
  - Given: `ISaveService` stub 返回 `chargeMode = Toggle`；`InputSettings` fixture `l3_charge_time=1.5f`；`FakeEventBus`；L3 裝備
  - When: 按下 ChargePrimary 一次（不持續按壓），等待 1.5s
  - Then: 1.5s 後自動發送 `ReleasePrimary` 事件（全幅震波）；`ChargeProgress` 在 0→1 期間正確線性增長
  - Edge cases: Toggle 中再按一次 ChargePrimary（取消蓄力）→ timer 停止，不觸發震波；Hold 模式放開即觸發（兩模式共用相同計時器，無副作用）

- **AC-3**: 重映射存檔 round-trip（save → reload → binding 恢復）
  - Given: 玩家將 FireSecondary 從 Space 改為 X 鍵並儲存
  - When: 模擬重新啟動：清除 InputActionAsset overrides，從 `ISaveService` 讀取並重新套用
  - Then: FireSecondary binding = X 鍵（而非預設 Space）；`InputSettings` SO `fireSecondaryDefaultBinding` 欄位不被修改
  - Edge cases: 存檔損毀（缺少 `inputOverrides` key）→ 使用 SO 預設值；版本遷移（存檔含舊版 binding key，新版不存在）→ fallback 到 SO 預設，不崩潰

- **AC-4**: 蓄力中斷寬限（Advisory，L.6 — manual verification）
  - Setup: 在遊戲中以任意方案蓄力至 1.0s；手動放開蓄力按鍵；在 0.25s 內重新按下
  - Verify: 蓄力環顯示繼續從 1.0s 進度計時，不歸零；READY 文字最終在 1.5s 時出現
  - Pass condition: 10 次重複測試 ≥8 次蓄力環不歸零；若 <8/10 → disable 功能並記錄

- **AC-5 (Manual)**: 跨方案等價 playtest（L.2 — BLOCKING pre-Vertical Slice）
  - Setup: 三方案玩家各自完成原型 Boss 戰（標準難度）；記錄部位破壞數與 L3 成功次數（10 次嘗試）
  - Verify: 計算觸控部位破壞數中位數 vs KB+Mouse 中位數；計算三方案 L3 成功率
  - Pass condition: 觸控中位數 ≥ KB+Mouse 中位數 × 0.8；三方案各自 L3 成功率 ≥ 80%；結果記錄於 `production/qa/evidence/cross-scheme-parity-evidence.md` + lead 簽核

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Input/remap_accessibility_test.cs` — EditMode tests for conflict detection logic, Toggle mode state machine, save round-trip
- `Assets/_Project/Tests/PlayMode/remap_integration_test.cs` — PlayMode tests for binding override apply/restore
- `production/qa/evidence/cross-scheme-parity-evidence.md` — L.2 playtest report (触控 vs KB+Mouse parity) with lead sign-off [BLOCKING pre-Vertical Slice]
- `production/qa/evidence/charge-interrupt-grace-evidence.md` — L.6 manual test results (10 iterations) [Advisory]

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003 (touch-scheme) must be DONE; Story 004 (kb-mouse-scheme) must be DONE; Story 005 (gamepad-scheme) must be DONE — rebinding requires all three schemes to exist; L.2 parity playtest requires all three schemes implemented
- Unlocks: Epic 輸入系統 complete (all stories Done + L.2 parity evidence signed off)
