# Story 003: WeaponController Base & Dual-Track Event Bus Wiring

> **Epic**: 武器系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-weapon-001`
*(TR-IDs inferred from GDD §H — tr-registry.yaml not yet populated)*

**ADR Governing Implementation**: ADR-0002: 事件架構與系統間通訊 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: `Weapons` 經 `IEventBus.Publish<T>()` 發 `LaserHit` / `MissileHit` / `L3WaveHit` readonly struct 事件（同步當幀）；以建構子注入 `IEventBus`、`IWeaponTierQuery`、`IPartStateQuery`、`WeaponBalanceConfig`；不直接引用 `KaijuParts` 組件；所有平衡值從 `WeaponDef` tier slot 讀取，無硬編碼。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 抽象基礎類別設計為純 C# 可測試（繼承 `MonoBehaviour` 的薄包裝層分開），DI 以建構子或 Zenject / 手工注入均可；具體選型於實作決定並記錄。`Physics2D.RaycastAll` 或 `OverlapCapsuleAll` 建議查驗 Unity 6.3 API 簽名（`docs/engine-reference/unity/VERSION.md`）。

**Control Manifest Rules (this layer)**:
- Required: MUST 系統以建構子 / 方法注入依賴（`IEventBus`、查詢介面、config SO）(§1.3)
- Required: MUST 事件為 `Core` 的 `readonly struct`，用 `IEventBus.Publish<T>(in T)` 發布 (§4.1)
- Required: MUST 接收 `on_part_break` 時僅清自身碰撞體 / 觸發 Tier-3 效果，不改部位狀態 (§3 Weapons)
- Required: MUST 一系統一 `.asmdef`；Weapons 只依賴 `Core` + `Content` (§1.4)
- Forbidden: MUST NOT 引用 `KaijuParts` 組件 (§3 Weapons、§1.4)
- Forbidden: MUST NOT 自行發出 `on_part_break` (§3 Weapons)
- Forbidden: MUST NOT 用 C# `event`/`Action` 直接互訂（破壞組件邊界）(§4.1)

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §C.1/§F.1，scoped to this story:*

- [ ] `LaserWeaponBase`（或等效抽象）提供：注入 `IEventBus`、`IWeaponTierQuery`、`IPartStateQuery`、`WeaponBalanceConfig`、`WeaponDef`；`EmitLaserHit(PartId partId, KaijuId kaijuId, float heatDelta)` 發布 `LaserHit` struct 事件
- [ ] `MissileWeaponBase`（或等效抽象）提供：同上注入；彈匣 / 換彈狀態機（`mag_current`, `is_reloading`）；`EmitMissileHit(PartId partId, KaijuId kaijuId, float breakDeltaBase)` 發布 `MissileHit` struct 事件（含 `weapon_id`）
- [ ] 兩個 Base 類別均訂閱 `on_part_break` 事件，接收後執行「清自身碰撞體」——後續 Tier-3 處理由子類別 override（Story 008/009）
- [ ] `WeaponDef` 的當前 Tier slot 查詢流程：`IWeaponTierQuery.GetTier(weaponId)` → index → `WeaponDef.TierKnobs[index]`，所有旋鈕值從此路徑讀取，無魔數
- [ ] `KaijuBreaker.Weapons.asmdef` 存在，僅引用 `KaijuBreaker.Core` + `KaijuBreaker.Content`（不引用 KaijuParts 或其他 Feature 組件）

---

## Implementation Notes

*Derived from ADR-0002 §1–§3 and ADR-0003 §2:*

- **事件 struct 定義在 `Core`**：`LaserHit { PartId PartId; KaijuId KaijuId; float HeatDelta; }` 等——Weapons 只 `Publish`，不自己定義。
- **DI 組合根**：`App` 組件注入真實實作；測試時傳入 spy `IEventBus` + stub `IWeaponTierQuery`（回傳固定 Tier 0）。
- **彈匣狀態機**（MissileWeaponBase）：狀態 `READY` → `FIRING` → `RELOADING` → `READY`；`Reload_Time` 從 `WeaponDef.TierKnobs[tier].reloadTime` 讀取，不硬編碼。
- **`on_part_break` 訂閱**：在 `OnEnable`（或等效生命週期）用 `IEventBus.Subscribe<PartBroke>` 登錄；在 `OnDisable` 取消訂閱，防記憶體洩漏。
- **彈幕零 GC**：`LaserHit` / `MissileHit` 為 `readonly struct` + `in` 傳遞；`EmitLaserHit` 方法不在 hot path 裡配置 managed 物件。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: L1/L2/L4 的具體 raycast 邏輯與熱量 delta 計算
- Story 005: L3 雙模式（tap / charge）與 `L3WaveHit` 事件
- Story 006: M1/M2/M4 的飛彈生成、追蹤與 AoE 邏輯
- Story 007: M3 的熱衝擊門檻查詢
- Stories 008–009: Tier-3 `on_part_break` handler override

---

## QA Test Cases

*Integration story — automated test specs; inject fake IEventBus + fake query interfaces.*

**Test: AC-1 — LaserWeaponBase 發布 LaserHit 事件**
- Given: Fake `IEventBus` spy（記錄所有 `Publish<LaserHit>` 呼叫）；stub `IWeaponTierQuery`（回傳 Tier 0）；`WeaponDef` fixture（l2HRate=37.5）
- When: 呼叫 `EmitLaserHit(partId: 1, kaijuId: 0, heatDelta: 37.5f * 0.016f)`
- Then: spy 記錄到恰好 1 次 `LaserHit`，且 `LaserHit.PartId == 1`、`LaserHit.HeatDelta ≈ 0.6f`
- Edge cases: heatDelta = 0（停止命中時不應呼叫 Emit）

**Test: AC-2 — MissileWeaponBase 彈匣狀態機**
- Given: Fake `IEventBus` spy；M1 WeaponDef fixture（m1_mag_size=6, m1_reload_time=3.0s）；stub `IWeaponTierQuery`（Tier 0）
- When: 發射 3 次（共耗盡 6 枚彈匣）；呼叫 `TryFire()` 第 4 次
- Then: 第 4 次 `TryFire()` 回傳 false（彈匣空，正在換彈）；經過 3s 後 `TryFire()` 回傳 true
- Edge cases: 換彈中途拾取新武器莢艙（Story 010 scope）；彈匣耗盡前 `is_reloading = false`

**Test: AC-3 — on_part_break 觸發碰撞體清除（基底行為）**
- Given: 具體子類別 stub 繼承 `LaserWeaponBase`；fake `IEventBus` 可模擬發布 `PartBroke` 事件
- When: 透過 fake bus 發布 `PartBroke { PartId = 1 }`
- Then: 基底 handler 觸發，`ClearCollider(1)` 被呼叫（可用 callback spy 驗證）；部位狀態未被修改
- Edge cases: 連續兩次 `PartBroke` 同一 partId（冪等；第 2 次 no-op）

**Test: AC-4 — asmdef 無 KaijuParts 引用**
- Given: `KaijuBreaker.Weapons.asmdef` 讀取
- When: 檢查 `references` 陣列
- Then: 不包含 `KaijuBreaker.KaijuParts` 字串
- Edge cases: N/A（靜態驗證）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_controller_base_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef + WeaponBalanceConfig SO 類別；Core 事件 struct LaserHit / MissileHit 需先定義)
- Unlocks: Story 004, Story 005, Story 006, Story 007 (均繼承此 Base)
