# Story 010: Loadout System — 1+1 Equip & Weapon Pod Pickup

> **Epic**: 武器系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-weapon-002`
*(TR-IDs inferred from GDD §H — tr-registry.yaml not yet populated)*

**ADR Governing Implementation**: ADR-0002: 事件架構與系統間通訊 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: `LoadoutController` 以 `IWeaponTierQuery` 讀 Tier（ISaveService 提供）、注入 `IEventBus` 並在莢艙拾取後觸發 autosave enqueue；莢艙拾取事件以 event bus 廣播（如需跨系統通知）；玩家初始 loadout 從 `ISaveService` 讀取，武器 `WeaponDef` 引用存於 `App` 組合根；兩池（Primary/Laser, Secondary/Missile）不互通，以 `WeaponDef.WeaponType` 驗證插槽。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 莢艙拾取以 `OnTriggerEnter2D` 偵測（trigger collider on pod prefab）——查驗 Unity 6.3 2D trigger 行為是否有變化（`docs/engine-reference/unity/VERSION.md`）。`LoadoutController` 核心邏輯設計為純 C# 可測試（拾取回呼以介面注入，不依賴 MonoBehaviour.OnTriggerEnter2D）。

**Control Manifest Rules (this layer)**:
- Required: MUST 主武器插槽只接受 `WeaponType.Laser`；副武器插槽只接受 `WeaponType.Missile`（GDD C.1）(§1.2)
- Required: MUST 初始 loadout 從 `ISaveService`（ADR-0002 §2）讀取，不硬編碼預設武器 (§1.2)
- Required: MUST 莢艙拾取後觸發 autosave enqueue（Stage Epic 職責；Weapons 僅確保時機正確）(§3 Stage)
- Required: MUST 系統以建構子 / 方法注入依賴（ISaveService, IEventBus, WeaponDef refs）(§1.3)
- Forbidden: MUST NOT 引用其他 Feature 系統組件（Stage, Economy 等）(§1.4)
- Forbidden: MUST NOT 維持武器庫存——拾取即替換，前一把丟棄（GDD C.1）(§3 Weapons)

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §C.1 / §C.3 / §F.3，scoped to this story:*

- [ ] `LoadoutController` 持有 1 個主武器引用（`WeaponType.Laser`）+ 1 個副武器引用（`WeaponType.Missile`）；兩者可分別取得當前啟動的 `WeaponDef`
- [ ] 武器莢艙（Weapon Pod）接觸玩家機體時，立即換裝同類型武器（`weaponType` 匹配則替換，不匹配則忽略）；前一把武器停用（無庫存系統）
- [ ] 拾取生效立即（同幀），新武器從 Tier 0 行為開始（Tier 等級由 `IWeaponTierQuery` 提供，不重置）
- [ ] 初始 loadout 於進入 Run 前由玩家永久選擇，透過 `ISaveService.GetInitialLoadout()` 讀取（`App` 在 Run 開始時注入）
- [ ] 玩家可刻意繞過莢艙以保留當前武器（拾取為觸碰觸發，無自動拾取半徑）
- [ ] `WeaponPod` prefab 標示 `PodType`（Primary / Secondary）及 `WeaponDef` 引用，由關卡設定決定掉落池（本 story 實作拾取邏輯，掉落池由 Stage Epic 負責）

---

## Implementation Notes

*Derived from ADR-0002 §2–§3 and GDD §C.1 / §C.3 / §F.2 / §F.3:*

- **`LoadoutController` 純 C# 核心**：`EquipWeapon(WeaponDef def, WeaponSlot slot)` 方法——替換引用、停用舊武器 MonoBehaviour、啟用新武器 MonoBehaviour；返回被替換的武器（可供 GameFeel 觸發「拾取音效」事件）。
- **拾取橋接**：`WeaponPodPickup` MonoBehaviour 負責 `OnTriggerEnter2D`，解析後呼叫 `LoadoutController.EquipWeapon()`——MonoBehaviour 薄包裝，核心邏輯在純 C# 類別中，確保可測試。
- **插槽驗證**：`if (pod.weaponDef.WeaponType != slot.RequiredType) return;`——靜默忽略（玩家不會拾取錯誤類型莢艙，因莢艙拾取為觸碰觸發）。
- **Autosave 觸發**：拾取後發布 `WeaponEquipped { WeaponId, Slot }` 事件至 `IEventBus`（`Core` 定義此 struct），`Stage` 訂閱此事件觸發 autosave enqueue——Weapons 不直接呼叫 `ISaveService.EnqueueSave()`。
- **F.2 循環莢艙機制**：莢艙依池分型（Primary/Secondary），`pod_cycle_interval` 輪替顯示——此為 Stage/掉落系統職責，本 story 只實作「接觸即拾取」的拾取端。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Stories 003–009: 各武器行為（`LoadoutController` 持有引用，但不實作發射邏輯）
- Stage Epic: 武器莢艙掉落池、循環顯示、保底掉落頻率（GDD F.2）
- Meta Epic: 初始 loadout 永久選擇介面、武器解鎖記錄（GDD F.3）
- TR-weapon-008: 三組標誌性搭配節奏感差異（主觀驗收，無 ADR，由設計師確認）

---

## QA Test Cases

*Integration story — automated test specs.*

**Test: AC-1 — 莢艙拾取替換正確類型武器**
- Given: `LoadoutController`（primary = L1, secondary = M1）；fake `IEventBus` spy；Laser Pod（weaponType=Laser, weaponDef=L2）
- When: `LoadoutController.EquipWeapon(L2_def, WeaponSlot.Primary)`
- Then: `LoadoutController.GetActiveWeapon(Primary)` 回傳 L2_def；L1 停用回呼被呼叫；spy 收到 `WeaponEquipped(WeaponId=L2, Slot=Primary)` 事件
- Edge cases: Missile Pod 嘗試裝入 Primary 插槽（weaponType=Missile）→ 靜默忽略，L1 仍為 active

**Test: AC-2 — 副武器拾取不影響主武器**
- Given: `LoadoutController`（primary=L2, secondary=M1）；Missile Pod（weaponDef=M3）
- When: `EquipWeapon(M3_def, WeaponSlot.Secondary)`
- Then: `GetActiveWeapon(Secondary)` 回傳 M3_def；`GetActiveWeapon(Primary)` 仍回傳 L2_def
- Edge cases: 連續拾取兩個 Missile Pod → 只保留最後拾取的（無庫存）

**Test: AC-3 — 初始 loadout 從 ISaveService 讀取**
- Given: ISaveService stub（GetInitialLoadout 回傳 Primary=L1, Secondary=M2）；fake bus spy
- When: `LoadoutController.Initialize(saveService)` 於 Run 開始時呼叫
- Then: `GetActiveWeapon(Primary)` = L1_def；`GetActiveWeapon(Secondary)` = M2_def；無硬編碼預設值
- Edge cases: ISaveService 回傳 null（首次遊玩）→ 使用 fallback WeaponDef（首把 Laser / Missile），fallback 從 `WeaponBalanceConfig` 讀取（非硬編碼）

**Test: AC-4 — 同類型插槽驗證（WeaponType 錯配靜默忽略）**
- Given: `LoadoutController`（primary=L1, secondary=M1）
- When: 嘗試 `EquipWeapon(M3_def, WeaponSlot.Primary)`（Missile 裝 Laser 插槽）
- Then: `GetActiveWeapon(Primary)` 仍回傳 L1_def；spy 無 `WeaponEquipped` 事件
- Edge cases: 相同武器再次拾取（L1 拾取 L1 Pod）→ 仍觸發 EquipWeapon（彈匣補滿：此行為待 Story 006/007 確認）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_loadout_system_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef 含 weaponType 欄位), Story 003 (IEventBus DI 佈線模式), Stories 004–007 (具體武器實作須存在以供 LoadoutController 持有引用)
- Unlocks: None (此為 Weapons Epic 最終故事)
