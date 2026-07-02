# Story 005: 手柄方案 (Gamepad Scheme)

> **Epic**: 輸入系統 (Input System)
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: TBD — fill before sprint planning
> **Manifest Version**: 2026-07-02
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; tr-registry not yet formalized; TR-ID covers L.4 gamepad-only complete play — BLOCKING)*

**ADR Governing Implementation**: ADR-0005: 專案結構與組件邊界 (primary); ADR-0003: 資料驅動調校 (secondary)

**ADR Decision Summary**: ADR-0005 — `KaijuBreaker.Input` provides concrete `IInputService` implementation for Gamepad; no cross-system component references; haptic feedback integration routes through `IEventBus` subscription (not direct GameFeel reference). ADR-0003 — all gamepad knobs (`gamepad_dead_zone`, `gamepad_max_speed`, `gamepad_input_curve`, `haptic_break_duration`, `haptic_break_intensity`) read from `InputSettings` SO; player vibration toggle stored in save JSON, not SO.

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**: Unity Input System Gamepad API [需查證 6.3 API] — verify `Gamepad.current.leftStick.ReadValue()`, `Gamepad.current.SetMotorSpeeds()`, RT/LT axis vs button API against `docs/engine-reference/unity/VERSION.md`. Haptic motor API may differ from Unity 2022.3. Input polling must use `Time.unscaledDeltaTime`. Verify `InputAction.WasPressedThisFrame()` availability for RT button mapping [需查證 6.3 API].

**Control Manifest Rules (this layer)**:
- Required: Left stick in velocity mode (not position mode); input curve formula `speed = gamepad_max_speed × pow(stick_magnitude, gamepad_input_curve)` applied before dead zone remapping; `gamepad_max_speed` must equal `keyboard_move_speed` for cross-scheme parity; vibration fully disableable via player setting (L.5 accessibility); `Time.unscaledDeltaTime` for movement
- Forbidden: Position-mode stick (conflicts with velocity-mode design in GDD F.2); hardcoded gamepad feel values (§5); static reference to Gamepad hardware (must handle hot-plug gracefully); drift if stick within dead zone
- Guardrail: stick dead zone check before applying input curve; maximum speed capped at `gamepad_max_speed` (stick_magnitude clamped to 1.0 before pow)

---

## Acceptance Criteria

*From GDD `design/gdd/input-system.md` §F.1–F.2, §I, and §L.4, scoped to this story:*

- [ ] **左搖桿速度模式移動（F.2）**：`speed = gamepad_max_speed × pow(stick_magnitude, gamepad_input_curve)`（`gamepad_input_curve=1.5`）；完全傾斜（magnitude=1.0）= `gamepad_max_speed`（280 px/s）
- [ ] **搖桿死區（F.2）**：`stick_magnitude ≤ gamepad_dead_zone`（0.12）時速度 = 0，防漂移
- [ ] **RT / R2（或 A 鈕）觸發 FireSecondary**：每次 ButtonDown 觸發一次；不支援長按連發
- [ ] **LT / L2 長按觸發 ChargePrimary**：按住 ≥ `l3_charge_time`（1.5s）後 `ChargePrimary` 完成事件；放開發出 `ReleasePrimary` 事件（僅 L3 裝備時生效）
- [ ] **Start / Menu 鈕觸發 Pause**
- [ ] **UI 動作（F.1）**：A 鈕 → UIConfirm；B 鈕 → UICancel；左搖桿 + D-Pad → UINavigate
- [ ] **震動回饋（Haptic Feedback，F.2）**：訂閱 `on_part_break` 事件；收到事件後觸發手柄馬達震動（`haptic_break_duration`、`haptic_break_intensity`）；震動可在玩家設定中完全關閉（關閉後不呼叫 motor API，不影響遊戲性，L.5 無障礙要求）
- [ ] **手柄完整遊玩（L.4）**：左搖桿 + RT + LT + Start 可完成任一難度的完整遊戲流程（playtest 驗收）
- [ ] 全部數值（`gamepad_dead_zone`、`gamepad_max_speed`、`gamepad_input_curve`、`haptic_break_duration`、`haptic_break_intensity`）僅從 `InputSettings` SO 讀取；無硬編碼

---

## Implementation Notes

*Derived from ADR-0005 and ADR-0003:*

**搖桿輸入曲線計算**：
```
// pseudocode — verify Unity 6.3 Gamepad API
Vector2 rawStick = Gamepad.current.leftStick.ReadValue(); // [needs 6.3 API check]
float magnitude = Mathf.Clamp01(rawStick.magnitude);
if (magnitude <= settings.gamepadDeadZone) { velocity = Vector2.zero; }
else {
    float speed = settings.gamepadMaxSpeed * Mathf.Pow(magnitude, settings.gamepadInputCurve);
    velocity = rawStick.normalized * speed;
}
ship.position += velocity * Time.unscaledDeltaTime;
ship.position = Clamp(ship.position, fieldBounds);
```

**震動回饋 vs 無障礙**：
- Input 系統訂閱 `IEventBus.Subscribe<PartBroke>(OnPartBroke)`（事件型別來自 `KaijuBreaker.Core`）
- `OnPartBroke`：若玩家震動設定 = on，呼叫 `Gamepad.current.SetMotorSpeeds(settings.hapticBreakIntensity, settings.hapticBreakIntensity)` [需查證 6.3 API]，並排程 `settings.hapticBreakDuration` 秒後停止
- **震動開關**狀態從 save JSON 讀取（ADR-0004），不存於 SO；DI 注入 `ISaveService` 以查詢開關狀態
- Input 系統不直接引用 `KaijuBreaker.GameFeel`——震動由 Input 系統自行執行（手柄 motor 是輸入設備的回饋，非 GameFeel 的職責）

**ChargePrimary LT 長按**：LT/L2 在 Unity Input System 中為類比 axis（0–1），需設定 Interaction（Hold，`pressPoint > 0.5f`，duration >= `l3ChargeTime`）[需查證 6.3 InputAction Interaction 設定]；或在 Update 中手動累計 `LT.ReadValue() > 0.5f` 的 `unscaledDeltaTime`。選擇後記錄於 commit。

**手柄熱插拔**：以 `Gamepad.current != null` 防空指標；Gamepad 拔插時 `InputUser.onChange` 事件可用 [需查證 6.3 API]；拔插後不崩潰，scheme 自動退回前一方案或彈出提示。

**測試路徑（ADR-0005）**：速度曲線公式（純數學，EditMode）放 `Assets/_Project/Tests/Input/`；實際 Gamepad 事件整合（PlayMode）放 `Assets/_Project/Tests/PlayMode/`。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002** (action-map-so)：`IInputService` 介面定義、`InputSettings` SO 類別
- **Story 003** (touch-scheme)：觸控移動、on-screen buttons
- **Story 004** (kb-mouse-scheme)：鍵盤 / 滑鼠實作
- **Story 006** (remap-accessibility)：手柄重映射 UI、搖桿軸翻轉選項、震動開關 UI、存檔讀寫

---

## QA Test Cases

*Integration story — automated test specs apply.*

- **AC-1**: 搖桿輸入曲線公式驗證（EditMode 純數學）
  - Given: `InputSettings` fixture `gamepad_max_speed=280`、`gamepad_input_curve=1.5`、`gamepad_dead_zone=0.12`
  - When: `stick_magnitude = 0.5`
  - Then: `speed = 280 × pow(0.5, 1.5) ≈ 99.0f`（±0.5 容差）
  - Edge cases: `magnitude = 0.12`（死區邊緣）→ speed = 0；`magnitude = 0.11`（死區內）→ speed = 0；`magnitude = 1.0`（完全傾斜）→ speed = 280f；`magnitude = 1.01`（clamp 後 1.0）→ speed = 280f

- **AC-2**: 搖桿死區防漂移
  - Given: `gamepad_dead_zone = 0.12`；模擬搖桿 magnitude = 0.10（模擬手柄漂移）
  - When: 執行一幀 Update
  - Then: 船艦速度 = Vector2.zero；船艦位置不變
  - Edge cases: magnitude = 0.13（剛超死區）→ speed > 0；magnitude = 0.0 → speed = 0

- **AC-3**: 震動可完全關閉（無障礙）
  - Given: `ISaveService` stub 返回 haptic_enabled = false；`FakeGamepad` mock 注入；`FakeEventBus`
  - When: 發送 `PartBroke` 事件
  - Then: 不呼叫 `FakeGamepad.SetMotorSpeeds()`（零震動調用）
  - Edge cases: haptic_enabled = true → `SetMotorSpeeds` 被呼叫一次，`haptic_break_duration` 秒後呼叫 `SetMotorSpeeds(0, 0)` 停止

- **AC-4**: RT 鍵 FireSecondary ButtonDown（非長按連發）
  - Given: `FakeEventBus`
  - When: 模擬 RT `WasPressedThisFrame` = true，保持 3 幀
  - Then: `FireSecondaryPressed` 事件僅第 1 幀觸發一次；第 2、3 幀無事件
  - Edge cases: 放開再按 → 再觸發一次；A 鈕亦可觸發 FireSecondary（兩個 binding OR）

- **AC-5**: LT 長按 ChargePrimary 完成
  - Given: `InputSettings` fixture `l3_charge_time=1.5f`；L3 裝備狀態 = true；`FakeEventBus`
  - When: 模擬 LT 值 > 0.5 持續 1.5s（`unscaledDeltaTime` 累計）
  - Then: 1.5s 時 `ChargePrimaryCompleted` 事件；放開後 `ReleasePrimary` 事件
  - Edge cases: LT 放開於 1.4s → 僅 `ReleasePrimary`（未完成）；L3 未裝備 → LT 無任何 Charge 事件

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Input/gamepad_scheme_test.cs` — EditMode unit tests for speed curve formula
- `Assets/_Project/Tests/PlayMode/gamepad_integration_test.cs` — PlayMode tests for input events

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (action-map-so) must be DONE (`IInputService` interface and `InputSettings` SO required)
- Unlocks: Story 006 (remap-accessibility) — gamepad rebinding, axis-invert options, and vibration toggle UI require this scheme to exist
