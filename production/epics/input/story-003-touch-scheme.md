# Story 003: 觸控方案實作 (Touch Scheme Implementation)

> **Epic**: 輸入系統 (Input System)
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: TBD — fill before sprint planning
> **Manifest Version**: 2026-07-02
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; tr-registry not yet formalized; TR-ID covers L.1 touch feel validation — implementation side after spike gate)*

**ADR Governing Implementation**: ADR-0005: 專案結構與組件邊界 (primary); ADR-0003: 資料驅動調校 (secondary)

**ADR Decision Summary**: ADR-0005 — `KaijuBreaker.Input` asmdef; no cross-system component references; cross-system output (ship position, fire events) via `IEventBus` and `IInputService` implementation injected by `App`. ADR-0003 — all feel values (`touch_visual_offset_y`, `touch_follow_lerp`, `touch_dead_zone_px`, `ship_margin_px`, `l3_charge_time`) read from `InputSettings` SO; zero hardcoded magic numbers.

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**: Unity Input System touch API [需查證 6.3 API] — verify `EnhancedTouchSupport.Enable()`, `Touch.activeTouches`, and `TouchPhase` API against `docs/engine-reference/unity/VERSION.md`. Do NOT assume Unity 2022 touch API is identical to Unity 6.3. Input polling must use `Time.unscaledDeltaTime` [需查證 unscaledDeltaTime availability in Input System callbacks].

> **FEEL VALUE LOCK**: `touch_follow_lerp` and `touch_visual_offset_y` in `InputSettings` SO must be updated from Story 001 spike evidence doc before this story ships. Implement structure with GDD K.1 placeholder defaults; do NOT finalize feel values until `production/qa/evidence/touch-feel-spike-evidence.md` is signed off.

**Control Manifest Rules (this layer)**:
- Required: Unity Input System for touch abstraction (control-manifest §3 Input); input polling via `Time.unscaledDeltaTime`; all feel values from `InputSettings` SO (ADR-0003); touch scheme integrates with `IEventBus` for cross-system events
- Forbidden: hover-only UI interactions (§3 Input); direct reference to other Feature assemblies from `KaijuBreaker.Input` (§1.4); difficulty scaling of any touch feel value (§3 Input); static singleton holding touch state (§1.3)
- Guardrail: Touch polling not to exceed its share of the 16.6 ms frame budget; unscaledDeltaTime preserves dodge inputs during hitstop

---

## Acceptance Criteria

*From GDD `design/gdd/input-system.md` §D.1–D.4, scoped to this story:*

- [ ] **相對偏移拖曳移動（D.1）**：`TouchStart` 記錄錨點；`TouchMove` 計算位移 delta + `touch_visual_offset_y` 向上偏移；船艦以指數平滑（`touch_follow_lerp × dt_normalized`）接近目標位置（非瞬間跳到）
- [ ] **拖曳可從螢幕任意位置開始**：除右側按鈕覆蓋區域外，整個螢幕皆為有效拖曳起點
- [ ] **觸控死區**：手指從錨點移動 ≤ `touch_dead_zone_px` 時，船艦位置不更新
- [ ] **場地邊界夾取**：船艦位置 clamp 至場地邊界（縮 `ship_margin_px`）；手指可移動超出邊界，船艦不越界
- [ ] **TouchEnd**：手指離開螢幕後船艦停在當前位置，不歸位
- [ ] **副武器按鈕（D.2）**：固定於螢幕右下角，距邊緣 `touch_button_margin_pt`；尺寸 ≥ `touch_button_min_size_pt`（60 pt）；點擊發出 `FireSecondary` 動作事件；提供視覺按下反饋（縮放 + 音效 trigger）
- [ ] **L3 蓄力按鈕（D.3）**：非 L3 裝備時隱藏；收到 L3 武器裝備事件後 200 ms 淡入顯示（右上角）；長按 ≥ `l3_charge_time` 後 `ChargePrimary` 事件完成；放開發出 `ReleasePrimary` 事件；換裝非 L3 武器後按鈕隱藏
- [ ] **feel values 取自 Story 001 spike 結果**：`touch_visual_offset_y` 與 `touch_follow_lerp` 的 `input_settings_default.asset` 值更新為 spike evidence doc 驗證值後，此 acceptance criterion 方可勾選

---

## Implementation Notes

*Derived from ADR-0005 and ADR-0003:*

**觸控事件來源**：優先使用 Unity Input System `EnhancedTouchSupport` API（`Touch.activeTouches`，[需查證 6.3 API]）而非 legacy `Input.GetTouch()`。若 6.3 有破壞性變更，查 `docs/engine-reference/unity/` 後更新本 notes。

**移動計算**（需在 `Update()` 中以 `Time.unscaledDeltaTime` 驅動）：
```
// pseudocode — verify Unity 6.3 API before writing
anchorPoint = TouchStart.position          // world/screen coords consistent with ship coord space
offsetOrigin = ship.currentWorldPosition
targetPos = offsetOrigin + (currentFinger - anchorPoint) + Vector2(0, -settings.touchVisualOffsetY)
ship.targetPosition = targetPos (clamped to field bounds minus shipMarginPx)
ship.position = Vector2.Lerp(ship.position, ship.targetPosition, settings.touchFollowLerp * dtNorm)
// dtNorm = Time.unscaledDeltaTime / (1f/60f)
```

**按鈕覆蓋區域 vs 拖曳區域**：拖曳起點過濾需排除副武器按鈕 Rect（右下）和蓄力按鈕 Rect（右上）。在 `TouchStart` 時做一次 Rect.Contains 判斷——命中按鈕 Rect 的 touch 不啟動拖曳。

**L3 裝備狀態監聽**：訂閱 Core 事件匯流排上的武器裝備事件（事件型別定義於 `KaijuBreaker.Core`）；不直接引用 Weapons 組件（ADR-0005 §2 鐵則）。

**按鈕 pt 單位**：`touch_button_min_size_pt` 與 `touch_button_margin_pt` 的螢幕 pixel 換算：`px = pt × Screen.dpi / 72f`（[需查證 Unity 6.3 Screen.dpi 可靠性]）。

**FEEL VALUE NOTE**：`InputSettings` SO 預設值為 Story 001 spike 占位。Story 001 evidence doc 簽核後，用 evidence doc 記載的最終旋鈕值更新 `input_settings_default.asset`。在此之前，結構與邏輯可完整實作；feel tuning 留待 spike 結果。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 001** (touch-feel-spike)：feel value 驗證；`touch_follow_lerp` / `touch_visual_offset_y` 最終值
- **Story 002** (action-map-so)：`IInputService` 介面定義、`InputSettings` SO 類別、`.inputactions` 資產
- **Story 004** (kb-mouse-scheme)：滑鼠跟隨、WASD 移動實作
- **Story 005** (gamepad-scheme)：搖桿移動、手柄映射實作
- **Story 006** (remap-accessibility)：觸控布局左右手切換、蓄力按鈕位置選項、存檔寫入

---

## QA Test Cases

*Integration story — automated test specs apply.*

- **AC-1**: 相對偏移拖曳移動計算（含死區與平滑）
  - Given: 船艦在世界座標 (100, 100)；`InputSettings` fixture `touch_visual_offset_y=80`、`touch_follow_lerp=0.9`、`touch_dead_zone_px=4`；`dtNorm=1.0`（60fps 基準）
  - When: 模擬 `TouchStart` at screen (200, 200)，`TouchMove` to (220, 230)（delta (20, 30)）
  - Then: 目標位置 = (100+20, 100+30+80) = (120, 210)；一幀後船艦位置 ≈ lerp((100,100), (120,210), 0.9) = (118, 199)（±2 容差）
  - Edge cases: delta magnitude < 4px（死區內）→ 船艦位置不變；手指移至場地外 → 目標 clamp 至邊界；TouchEnd → 船艦停在 lerp 當前值不歸位

- **AC-2**: 場地邊界夾取
  - Given: 場地寬度 320px；`ship_margin_px=12`；船艦在 (300, 100)
  - When: TouchMove delta 造成目標 x = 340
  - Then: 船艦 x clamp 至 320-12 = 308（不越界）；手指繼續往外拖不影響夾取結果

- **AC-3**: L3 蓄力按鈕依裝備狀態顯示/隱藏
  - Given: 初始狀態無武器裝備；蓄力按鈕 `gameObject.activeSelf == false`
  - When: 發送 L3 武器裝備事件（via `IEventBus`）
  - Then: 200 ms 後蓄力按鈕 `activeSelf == true`，透明度從 0 漸變到 1（[測試時可 skip 動畫，驗證最終 active 狀態]）
  - Edge cases: 換裝非 L3 武器事件 → 按鈕 `activeSelf == false`；連續兩次 L3 裝備事件不觸發雙重淡入

- **AC-4**: 副武器按鈕觸發 FireSecondary 事件
  - Given: `FakeEventBus` 注入；副武器按鈕 Rect 在螢幕右下角
  - When: 模擬 Touch at 按鈕 Rect 內部座標
  - Then: `FakeEventBus` 收到 `FireSecondaryPressed` 事件一次（此幀）；下幀無重複觸發
  - Edge cases: Touch 起始在按鈕 Rect 外、拖曳進入按鈕 → 不觸發（按鈕為點擊，非拖曳進入）；按鈕 Rect 外 touch → 開始拖曳移動，不觸發 FireSecondary

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/PlayMode/touch_scheme_test.cs` — PlayMode integration tests; must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (action-map-so) must be DONE (`IInputService` interface and `InputSettings` SO class required); Story 001 (touch-feel-spike) evidence doc signed off before `touch_follow_lerp` / `touch_visual_offset_y` values can be finalized in `input_settings_default.asset`
- Unlocks: Story 006 (remap-accessibility) touch layout options
