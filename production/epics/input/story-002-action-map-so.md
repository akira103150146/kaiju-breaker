# Story 002: 抽象動作映射與 InputSettings SO (Abstract Input Action Map & InputSettings SO)

> **Epic**: 輸入系統 (Input System)
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: TBD — fill before sprint planning
> **Manifest Version**: 2026-07-02
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-007`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time; tr-registry not yet formalized; TR-ID derived from GDD §L requirement: input feel values data-driven in `InputSettings`, no difficulty scaling)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 — ScriptableObject 為唯一調校資料來源 (primary); ADR-0005: 專案結構與組件邊界 (secondary)

**ADR Decision Summary**: ADR-0003 — all GDD tuning knobs expressed as ScriptableObject assets in `Assets/_Project/Content/`; runtime read-only; player overrides go to save JSON (ADR-0004), never written back to SO; tests inject fake SO fixtures. ADR-0005 — one `.asmdef` per system; `KaijuBreaker.Input` depends only on `KaijuBreaker.Core` + `KaijuBreaker.Content`; `IInputService` interface lives in `KaijuBreaker.Core`; `InputSettings` SO class lives in `KaijuBreaker.Content`; tests in `Assets/_Project/Tests/Input/`.

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**: Unity Input System package API [需查證 6.3 API] — verify `.inputactions` asset schema and `InputActionAsset` binding API against `docs/engine-reference/unity/VERSION.md` before implementation. Do NOT assume API signatures from training data. `PlayerInput` component vs manual `InputAction.ReadValue<>()` approach — verify preferred pattern for Unity 6.3.

**Control Manifest Rules (this layer)**:
- Required: `InputSettings` SO in `Assets/_Project/Content/`; `OnValidate` for all GDD safe-range checks (control-manifest §1.2); `IInputService` interface in `KaijuBreaker.Core` (§1.4); `KaijuBreaker.Input` asmdef references only Core + Content (§1.4)
- Forbidden: No hardcoded magic numbers for any feel value (§1.2, §5); no difficulty scaling of input values (control-manifest §3 Input); no static singletons holding game state (§1.3)
- Guardrail: All values must survive a fake-SO injection test — if a system cannot be constructed without Unity Input System in EditMode, the DI seam is missing

---

## Acceptance Criteria

*From GDD `design/gdd/input-system.md` §C, §K, and the data-driven requirement, scoped to this story:*

- [ ] `KaijuBreaker.Input` `.asmdef` 建立，依 ADR-0005 references 清單僅包含 `KaijuBreaker.Core` 與 `KaijuBreaker.Content`（無其他 Feature 系統引用）
- [ ] Unity `.inputactions` 資產定義全部 8 個抽象動作（Move、FireSecondary、ChargePrimary、ReleasePrimary、Pause、UIConfirm、UICancel、UINavigate），三方案（Touch、Keyboard+Mouse、Gamepad）bindings 完整對應 GDD §C 動作對照表
- [ ] `InputSettings` SO 類別（`KaijuBreaker.Content` 組件）涵蓋 GDD K.1–K.3 全部調校旋鈕（≥15 個欄位），每個欄位有 `OnValidate` 對 GDD 安全範圍的 Editor 斷言
- [ ] `input_settings_default.asset` SO 資產以 GDD K.1–K.3 表格中的預設值初始化（`touch_visual_offset_y=80`、`touch_follow_lerp=0.92`、`gamepad_max_speed=280` 等）；`touch_follow_lerp` 與 `touch_visual_offset_y` 預設值為 Story 001 spike 結果的占位——在 Story 001 spike evidence doc 簽核後，應更新為 spike 驗證值
- [ ] `IInputService` 介面定義於 `KaijuBreaker.Core`，公開消費者所需的最小唯讀 API（至少：`PlayerWorldPosition: Vector2`、`IsFireSecondaryDown: bool`、`IsCharging: bool`、`ChargeProgress: float [0–1]`、`ActiveScheme: InputScheme enum`）
- [ ] `InputScheme` enum（Touch / KeyboardMouse / Gamepad）定義於 `KaijuBreaker.Core`
- [ ] 所有 gameplay 數值（lerp 係數、速度、死區）僅從 `InputSettings` SO 讀取；無任何硬編碼魔數存在於 `KaijuBreaker.Input` 組件

---

## Implementation Notes

*Derived from ADR-0003 and ADR-0005:*

**SO 定義位置（ADR-0003 §1）**：`InputSettings` 類別放 `Assets/_Project/Content/Input/InputSettings.cs`；資產實例 `Assets/_Project/Content/Input/input_settings_default.asset`。GDD 的 `assets/data/input/input_settings.json` 是引擎無關佔位——Unity 實作以 SO 取代。

**OnValidate 安全範圍斷言（ADR-0003 §4）**：對 GDD K.1–K.3 每個旋鈕的 `(min, max)` 安全範圍，在 `OnValidate` 中使用 `Debug.LogError` 或 `Debug.LogWarning` 提示 Inspector 越界。不 throw exception（避免 Inspector 崩潰）。

**靜態/可變分離（ADR-0003 §2）**：`InputSettings` SO = 設計師設定、runtime 唯讀。玩家覆寫（左手模式、蓄力 Toggle/Hold 模式、震動開關、按鍵重映射）存入 save JSON（ADR-0004）——**絕不寫回 SO**。

**IInputService 介面（ADR-0005 §3）**：介面放 `KaijuBreaker.Core` 防止 Input 組件成為其他 Feature 系統的直接依賴。消費者（如 Ship、Weapons、UI）只依賴 `IInputService` 介面，不引用 `KaijuBreaker.Input` 具體類別。

**asmdef references 清單**（`.asmdef` JSON）：
```json
{
  "name": "KaijuBreaker.Input",
  "references": [
    "KaijuBreaker.Core",
    "KaijuBreaker.Content"
  ],
  "includePlatforms": [],
  "excludePlatforms": []
}
```
輸入包套件引用（`com.unity.inputsystem`）加入此 asmdef 的 `references` 清單 [需查證 6.3 API asmdef reference 格式]。

**測試路徑（ADR-0005 §5）**：EditMode 測試放 `Assets/_Project/Tests/Input/`（不需場景或 Input System runtime 的純邏輯測試）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 001** (touch-feel-spike)：`touch_visual_offset_y` / `touch_follow_lerp` 最終驗證值；spike prototype 建立
- **Story 003** (touch-scheme)：`IInputService` 具體實作（觸控方案邏輯）
- **Story 004** (kb-mouse-scheme)：`IInputService` 具體實作（鍵鼠方案邏輯）
- **Story 005** (gamepad-scheme)：`IInputService` 具體實作（手柄方案邏輯）
- **Story 006** (remap-accessibility)：重映射 UI、Hold/Toggle 模式切換、存檔讀寫玩家覆寫

---

## QA Test Cases

*Logic story — automated test specs apply.*

- **AC-1**: InputSettings SO 安全範圍驗證（OnValidate）
  - Given: 一個 `InputSettings` fixture `.asset`，`touch_follow_lerp` 設為 0.99（合法上限）
  - When: 在 EditMode 呼叫 `OnValidate()`
  - Then: Console 無錯誤或警告輸出
  - Edge cases: `touch_follow_lerp = 1.01`（超上限）→ OnValidate 輸出 LogError；`touch_follow_lerp = 0.79`（低下限）→ 同；`touch_dead_zone_px = 1`（低於安全下限 2）→ LogWarning

- **AC-2**: IInputService 介面可注入假實作（DI seam 存在）
  - Given: 一個接受 `IInputService` 建構子注入的消費者類別（例如假 ShipController）
  - When: 注入 `FakeInputService`（stub，傳回固定值），呼叫 `PlayerWorldPosition`
  - Then: 傳回 fake 設定的 Vector2 值；不需 Unity Input System runtime 即可執行
  - Edge cases: FakeInputService 實作 `IsCharging = true` → ChargeProgress 可查詢；ActiveScheme 傳回 enum 值

- **AC-3**: `.inputactions` 資產動作完整性
  - Given: 載入 `.inputactions` 資產
  - When: 查詢 action map 內動作清單
  - Then: 包含 Move、FireSecondary、ChargePrimary、ReleasePrimary、Pause、UIConfirm、UICancel、UINavigate 共 8 個動作
  - Edge cases: 每個動作至少有 Gamepad binding；Move 動作為 Vector2 類型（非 Button）

- **AC-4**: InputSettings 預設值吻合 GDD K.1–K.3
  - Given: 讀取 `input_settings_default.asset`
  - When: 逐一比對欄位值與 GDD 表格預設值
  - Then: `gamepad_max_speed == 280f`；`gamepad_input_curve == 1.5f`；`gamepad_dead_zone == 0.12f`；`mouse_follow_lerp == 0.98f`；`keyboard_move_speed == 280f`（±epsilon）
  - Edge cases: `touch_follow_lerp` 與 `touch_visual_offset_y` 欄位存在且在安全範圍內（最終值待 Story 001 spike 後更新）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Input/input_action_map_so_test.cs` — EditMode unit tests; must exist and pass in Unity Test Framework

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `KaijuBreaker.Core` 與 `KaijuBreaker.Content` 組件已存在（core-foundation epic must be DONE）
- Unlocks: Story 003 (touch-scheme), Story 004 (kb-mouse-scheme), Story 005 (gamepad-scheme) — all require `IInputService` interface and `InputSettings` SO class
