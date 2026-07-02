# Story 004: 鍵盤＋滑鼠方案 (KB+Mouse Scheme)

> **Epic**: 輸入系統 (Input System)
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: TBD — fill before sprint planning
> **Manifest Version**: 2026-07-02
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-003`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; tr-registry not yet formalized; TR-ID covers L.3 keyboard-only complete play — BLOCKING)*

**ADR Governing Implementation**: ADR-0005: 專案結構與組件邊界 (primary); ADR-0003: 資料驅動調校 (secondary)

**ADR Decision Summary**: ADR-0005 — `KaijuBreaker.Input` asmdef provides concrete `IInputService` implementation for KB+Mouse; no other Feature assemblies referenced; cross-system communication via `IEventBus` and `IInputService` query interface. ADR-0003 — `mouse_follow_lerp` and `keyboard_move_speed` read from `InputSettings` SO; zero hardcoded values.

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**: Unity Input System `Mouse.current` and `Keyboard.current` API [需查證 6.3 API] — verify against `docs/engine-reference/unity/VERSION.md`. Mouse world-position conversion from screen to game-world coordinates depends on Camera setup [需查證]. Input polling must use `Time.unscaledDeltaTime` to preserve dodge inputs during hitstop (timeScale=0).

**Control Manifest Rules (this layer)**:
- Required: Keyboard WASD + Space + Z must each complete full game flow without mouse (L.3 parity requirement); all feel values (`mouse_follow_lerp`, `keyboard_move_speed`) from `InputSettings` SO; `Time.unscaledDeltaTime` for movement updates; cursor hidden during gameplay
- Forbidden: hover-only UI interactions (cross-platform parity, §3 Input); `keyboard_move_speed` must match `gamepad_max_speed` for cross-scheme parity (both 280 px/s per GDD K.2/K.3); no hardcoded movement values (§5)
- Guardrail: WASD diagonal speed must be normalized (no magnitude > `keyboard_move_speed`); mouse-follow and WASD must share the same boundary clamp path

---

## Acceptance Criteria

*From GDD `design/gdd/input-system.md` §E.1–E.3, §I, and §L.3, scoped to this story:*

- [ ] **滑鼠跟隨移動（E.1）**：船艦以 `mouse_follow_lerp` 係數（`Time.unscaledDeltaTime` 驅動）移動至滑鼠游標的遊戲世界座標；遊戲進行時系統游標隱藏，以船艦本身作視覺替代
- [ ] **游標夾界（E.1）**：船艦 clamp 至場地邊界；滑鼠游標可移動到場地外不中斷操作
- [ ] **WASD / 方向鍵替代方案（E.1）**：船艦以 `keyboard_move_speed` px/s 持續往按壓方向移動；對角按壓時速度向量正規化（magnitude 不超過 `keyboard_move_speed`）
- [ ] **FireSecondary — 左鍵點擊（E.2）**：每次點擊發射一次；不支援長按連發（每次 MouseButtonDown 觸發，MouseButtonHeld 不重複觸發）
- [ ] **FireSecondary — 空白鍵（E.2）**：等效替代鍵；與左鍵點擊相同語義（ButtonDown，非 ButtonHeld）
- [ ] **ChargePrimary — Z 鍵長按（E.3）**：按住 ≥ `l3_charge_time` 後 `ChargePrimary` 事件完成；放開發出 `ReleasePrimary` 事件（僅 L3 裝備時生效）
- [ ] **ChargePrimary — 右鍵長按（E.3）**：等效替代鍵；語義與 Z 鍵相同
- [ ] **鍵盤單一方案完整遊玩（L.3）**：WASD + 空白鍵 + Z 鍵（不使用滑鼠）可完成任一難度的完整遊戲流程，從 Loadout 選擇到擊倒 Boss（playtest 驗收）
- [ ] 全部數值（`mouse_follow_lerp`、`keyboard_move_speed`）僅從 `InputSettings` SO 讀取；無硬編碼

---

## Implementation Notes

*Derived from ADR-0005 and ADR-0003:*

**滑鼠跟隨實作**：使用 Unity Input System `Mouse.current.position.ReadValue()` [需查證 6.3 API]，再以 Camera 轉換為遊戲世界座標（`Camera.main.ScreenToWorldPoint` 或等效 API [需查證]）。跟隨公式：
```
// pseudocode
Vector2 mouseWorld = Camera.ScreenToWorld(Mouse.current.position);
ship.position = Vector2.Lerp(ship.position, Clamp(mouseWorld, fieldBounds), settings.mouseFollowLerp * dtNorm);
// dtNorm = Time.unscaledDeltaTime / (1f/60f)
```

**游標隱藏**：`Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;`（遊戲中）。Pause 選單開啟時恢復 `Cursor.visible = true`（由 UI 系統或 Pause 狀態機負責；Input 系統提供 scheme 狀態查詢 API，不直接控制 UI）。[需查證 6.3 Cursor API]

**WASD 速度正規化**：
```
// pseudocode
Vector2 dir = new Vector2(keyboard.d - keyboard.a, keyboard.w - keyboard.s).normalized;
ship.velocity = dir * settings.keyboardMoveSpeed;
ship.position += ship.velocity * Time.unscaledDeltaTime;
ship.position = Clamp(ship.position, fieldBounds);
```

**FireSecondary ButtonDown 語義**：使用 `WasPressedThisFrame()` [需查證 6.3 Input System API]，而非 `IsPressed()`，以確保每次按下只觸發一次，防止長按連發。

**ChargePrimary 長按計時**：在 Input 系統內以 `Time.unscaledDeltaTime` 累計按住時間；達到 `settings.l3ChargeTime` 後發出完成事件。Z 鍵與右鍵以 OR 邏輯合併（任一按住即計時，任一放開即中斷），[需查證 InputAction 多 binding 長按的 Interaction 設定]。

**跨方案等價注意**：`keyboard_move_speed` == `gamepad_max_speed`（均 280 px/s，GDD K.2/K.3）。若 QA playtest 發現鍵盤速度感覺與手柄不等價，需先確認是否從同一 SO 欄位讀取，再回報 GDD 調整。**不得各自硬編碼不同數值。**

**測試路徑（ADR-0005）**：EditMode 測試（速度正規化公式、邊界夾取）放 `Assets/_Project/Tests/Input/`；PlayMode 測試（實際按鍵觸發）放 `Assets/_Project/Tests/PlayMode/`。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002** (action-map-so)：`IInputService` 介面定義、`InputSettings` SO 類別
- **Story 003** (touch-scheme)：觸控拖曳、on-screen buttons
- **Story 005** (gamepad-scheme)：搖桿速度曲線、手柄 haptic feedback
- **Story 006** (remap-accessibility)：鍵盤重映射 UI、Z/Space 鍵替換為其他按鍵、存檔讀寫

---

## QA Test Cases

*Integration story — automated test specs apply.*

- **AC-1**: WASD 對角方向速度正規化
  - Given: `InputSettings` fixture `keyboard_move_speed=280`
  - When: 同時模擬按下 W + D（右上對角）
  - Then: 移動速度向量 magnitude == 280f（±0.5 容差）；方向角 ≈ 45°（±1°）
  - Edge cases: 無按鍵 → velocity = Vector2.zero；單方向 D → velocity.x = 280, velocity.y = 0；W+A+D → W + 正規化後水平分量

- **AC-2**: 滑鼠跟隨平滑（lerp 逼近）
  - Given: 船艦在世界座標 (0, 0)；`mouse_follow_lerp=0.98`；`dtNorm=1.0`
  - When: 滑鼠世界位置 = (100, 100)，執行一幀 Update
  - Then: 船艦位置 ≈ lerp((0,0), (100,100), 0.98) = (98, 98)（±1 容差）；不超過 (100,100)
  - Edge cases: `mouse_follow_lerp=1.0` → 船艦瞬間抵達；滑鼠在場地外 → 船艦 clamp 至邊界

- **AC-3**: Space 鍵 FireSecondary ButtonDown 語義（非長按連發）
  - Given: `FakeEventBus` 注入；Space 鍵模擬
  - When: 按下 Space（`WasPressedThisFrame`），保持按住 3 幀
  - Then: `FireSecondaryPressed` 事件僅在第 1 幀觸發一次；第 2、3 幀無重複事件
  - Edge cases: 放開再按 → 再觸發一次（正確）；左鍵點擊同幀與 Space 同時 → 觸發兩次（各自獨立）

- **AC-4**: Z 鍵長按 ChargePrimary 完成
  - Given: `InputSettings` fixture `l3_charge_time=1.5f`；`FakeEventBus`；L3 武器裝備狀態 = true
  - When: 模擬 Z 鍵按下，累計 `unscaledDeltaTime` 經過 1.5s
  - Then: 1.5s 時 `ChargePrimaryCompleted` 事件發送；放開 Z 鍵後發送 `ReleasePrimary` 事件
  - Edge cases: L3 未裝備時 Z 鍵長按 → 不發送任何 Charge 事件；1.4s 時放開 → 僅發 `ReleasePrimary`（未完成蓄力，弱脈衝由 Weapons 處理）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Input/kb_mouse_scheme_test.cs` — EditMode unit tests for formula logic (speed normalization, boundary clamp)
- `Assets/_Project/Tests/PlayMode/kb_mouse_integration_test.cs` — PlayMode tests for actual input events

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (action-map-so) must be DONE (`IInputService` interface and `InputSettings` SO required)
- Unlocks: Story 006 (remap-accessibility) — KB+Mouse rebinding and conflict detection require this scheme to exist
