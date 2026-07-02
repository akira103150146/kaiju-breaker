# Story 001: 觸控手感原型驗證 Spike (Touch Feel Prototype Spike)

> **Epic**: 輸入系統 (Input System)
> **Status**: Ready
> **Layer**: Feature
> **Type**: Visual/Feel
> **Estimate**: TBD — fill before sprint planning
> **Manifest Version**: 2026-07-02
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; tr-registry not yet formalized; TR-ID derived from GDD §L.1)*

**ADR Governing Implementation**: N/A — prototype spike; this story builds a throwaway HTML prototype to validate the relative-offset drag hypothesis before any Unity implementation begins. ADR-0003 will govern placement of validated feel values in `InputSettings` SO starting at Story 002.

**ADR Decision Summary**: No architectural ADR governs this spike. Validated values (`touch_visual_offset_y`, `touch_follow_lerp`) from the evidence doc become the initial defaults in `InputSettings` SO (governed by ADR-0003) when Story 002 defines the SO class.

**Engine**: HTML/JavaScript prototype (no Unity engine for this story) | **Risk**: LOW for the spike; MEDIUM for subsequent Unity integration
**Engine Notes**: Existing prototype at `prototypes/weapon-feel-concept/prototype.html` uses direct cursor-to-ship mapping — this spike replaces that with relative-offset drag. No Unity APIs involved. Unity Input System risk (Unity 6.3 [需查證 6.3 API]) applies from Story 003 onwards.

**Control Manifest Rules (this layer)**:
- Required: N/A for HTML spike — Unity Input System control-manifest rules apply from Story 002 onwards
- Forbidden: Do not merge Story 003 (touch-scheme Unity implementation) until this spike's evidence doc is signed off — L.1 is a PRE-MVP BLOCKING gate
- Guardrail: This story is the highest-priority unresolved risk in the epic; block no other work except Story 003 feel-value lock-in

---

## Acceptance Criteria

*From GDD `design/gdd/input-system.md` §L.1, scoped to this story:*

- [ ] **觸控專用原型建立**：在 `prototypes/weapon-feel-concept/prototype.html` 基礎上，實作 GDD D.1 的三步驟相對偏移拖曳模型（`touchstart` 記錄錨點、`touchmove` 位移 delta + `touch_visual_offset_y` 垂直偏移、指數平滑跟隨 `touch_follow_lerp` × dt_normalized），可在實機（iOS / Android 手機瀏覽器）觸控測試。
- [ ] **遮蔽問題解決**：5 位測試者（含至少 1 位首次遊玩觸控彈幕遊戲的新手）在全程測試中反應「我始終知道船艦在哪裡」，且能指出判定點（hitbox dot）位置。5/5 為目標；4/5 為最低接受門檻。
- [ ] **彈幕閃避可行性**：5 位測試者在最低難度下，觸控操控完成第一個 Boss 戰（至少一次通過），平均死亡次數 ≤5。若平均 >10，須調整 `touch_visual_offset_y` 與 `touch_follow_lerp` 後重測。
- [ ] **L3 蓄力觸控可行性**：5 位測試者中至少 4 位能在 Boss 戰壓力下（有彈幕回避需求）成功觸發 L3 全幅震波至少一次。

---

## Implementation Notes

*This is a spike — throwaway prototype only, no Unity source files modified.*

1. **起點**：僅修改 `prototypes/weapon-feel-concept/prototype.html`（或在 `prototypes/` 建立新子目錄）。不觸碰 `src/` 任何 Unity 或 C# 檔案。

2. **實作 GDD D.1 三步驟（JavaScript）**：
   - `touchstart`：記錄 `anchorPoint = {x: e.touches[0].clientX, y: e.touches[0].clientY}`；`offsetOrigin = {...shipPosition}`；船艦不跳位。
   - `touchmove`：`target = {x: offsetOrigin.x + (finger.x - anchor.x), y: offsetOrigin.y + (finger.y - anchor.y) - touch_visual_offset_y}`；`ship = lerp(ship, target, touch_follow_lerp × dtNorm)`（dtNorm = dt / (1/60)）。
   - `touchend`：船艦停在當前位置，不歸位。

3. **調校旋鈕（以 HTML input[type=range] slider 暴露於 prototype UI）**：
   - `touch_visual_offset_y`：初始 80px；安全範圍 40–120（GDD K.1）
   - `touch_follow_lerp`：初始 0.92；安全範圍 0.80–0.99（GDD K.1）
   - `touch_dead_zone_px`：初始 4px；安全範圍 2–10（GDD K.1）

4. **Playtest 記錄**：為每位受試者記錄設備機型、OS、遊玩時間、死亡次數、L3 成功觸發次數、遮蔽感受問卷回答。

5. **Spike 輸出**：在 `production/qa/evidence/touch-feel-spike-evidence.md` 記錄最終通過的旋鈕值、5 人測試結果、每次迭代的旋鈕調整記錄、通過/失敗判定、lead 簽核。**這些最終值成為 Story 002 `InputSettings` SO 的 `touch_visual_offset_y` 與 `touch_follow_lerp` 預設值。**

6. **迭代失敗處理**：未達標準時調整旋鈕後重測，不算作整體失敗——此為迭代驗證流程。記錄每次迭代結果至 evidence doc。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002** (action-map-so)：Unity `.inputactions` 資產、`InputSettings` SO 類別、`IInputService` 介面
- **Story 003** (touch-scheme)：Unity Input System 觸控方案完整實作；`touch_follow_lerp` 與 `touch_visual_offset_y` 最終值必須等本 spike 通過後方可鎖定

---

## QA Test Cases

*This is a Visual/Feel story — manual verification steps apply.*

- **AC-1**: 觸控原型在實機以相對偏移拖曳控制船艦
  - Setup: 在 iOS 或 Android 裝置瀏覽器（Chrome / Safari）開啟修改後的 prototype；確認 sliders 可見
  - Verify: 手指放下後船艦不跳至手指位置；拖動時船艦跟隨位移 delta 並始終顯示在手指正上方
  - Pass condition: 船艦與手指偏移穩定可見；無明顯起點跳位或持續抖動（死區生效）

- **AC-2**: 遮蔽問題解決（5 人 playtest）
  - Setup: 5 位受試者；至少 1 位首次遊玩觸控彈幕；各自遊玩 ≥5 分鐘後接受問卷：「你始終知道船艦在哪裡嗎？」「請指出判定點的位置。」
  - Verify: 記錄每人問卷回答；觀察遊玩中是否出現「船在哪裡？」困惑行為
  - Pass condition: ≥4/5 受試者兩題均通過（答 Yes 且能正確指向判定點）；5/5 為目標

- **AC-3**: 彈幕閃避可行性（Boss 戰平均死亡 ≤5）
  - Setup: 5 位受試者在最低難度各嘗試 Boss 戰；記錄從開始至首次通關的死亡次數
  - Verify: 計算 5 人死亡次數平均值
  - Pass condition: 平均死亡次數 ≤5；若均值 >10，調整 `touch_visual_offset_y` 與 `touch_follow_lerp` 後重測整輪

- **AC-4**: L3 蓄力觸控可行性（彈幕壓力下）
  - Setup: Boss 戰進行中（有彈幕）；5 位受試者各嘗試長按蓄力按鈕觸發 L3 全幅震波
  - Verify: 記錄每人是否至少成功觸發一次
  - Pass condition: ≥4/5 受試者成功觸發至少一次

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/qa/evidence/touch-feel-spike-evidence.md` — playtest report with per-tester results, final validated `touch_visual_offset_y` and `touch_follow_lerp` values, pass/fail judgment per criterion, lead sign-off

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (independent of all Unity implementation; core-foundation assemblies not required)
- Unlocks: Story 003 (touch-scheme) feel-value lock-in — the `touch_visual_offset_y` and `touch_follow_lerp` defaults in `InputSettings` SO must come from this spike's signed evidence doc before Story 003 can ship
