# Story 003: 唯讀查詢介面定義

> **Epic**: Core 基礎設施（事件匯流排 + DI）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: ~2h
> **Manifest Version**: 2026-07-02
> **Last Updated**: (set by /dev-story when implementation begins)

## Context

**GDD**: `docs/architecture/architecture.md` §5.4（DI 組合）+ ADR-0002 §2（唯讀查詢介面）
**Requirement**: `TR-core-003`
*(tr-registry.yaml 尚未正式化 — TR-ID 由 ADR-0002/0005 推導；讀 EPIC.md GDD Requirements 表獲取規格)*

**ADR Governing Implementation**: ADR-0002（主）, ADR-0005（次）
**ADR Decision Summary**: 非事件的跨系統唯讀讀取走介面注入，不用事件、不用單例——`IPartStateQuery` / `IDifficultyProvider` / `ISaveService` / `IWeaponTierQuery` 定義於 `Core`；`App` 建構具體實作並注入；系統測試時注入假實作（fake/stub），滿足「獨立可測」。

**Engine**: Unity 6.3 LTS (C#) | **Risk**: LOW
**Engine Notes**: 純 C# 介面定義，不涉及 Unity API。`Vector3` 作為 `IPartStateQuery` 回傳型別需 `using UnityEngine;`，但這是 `Core` asmdef 預設可用的命名空間。`ISaveService.EnqueueSave` 的非同步存檔 worker 細節屬 Meta 系統，介面本身無 Unity 6.3 特定 API。

**Control Manifest Rules (Foundation layer)**:
- Required: 跨系統唯讀查詢 MUST 走 `Core` 介面 DI 注入，不用事件、不用單例（control-manifest §4.3）
- Required: `IPartStateQuery` → KaijuParts 實作 / Weapons + UI 注入；`IDifficultyProvider` → Difficulty 實作 / Stage + BulletSim 注入；`ISaveService` → Meta 實作 / Economy + Stage + UI 注入；`IWeaponTierQuery` → Meta/Economy 提供 / Weapons 注入（control-manifest §4.3）
- Required: 測試時注入假實作（fake/stub）驗證介面可獨立使用（control-manifest §1.3）
- Forbidden: 查詢介面 MUST NOT 放任何具體實作——介面只是合約（control-manifest §2 / §3 Core）
- Guardrail: 所有介面有 doc comment 說明消費者（注入方）與實作者（實作方）（control-manifest §1.8）

---

## Acceptance Criteria

*From ADR-0002 §2 + architecture.md §5.4, scoped to TR-core-003:*

- [ ] `IPartStateQuery` 定義，包含（對齊 ADR-0002 §2 + architecture.md §5.2 查詢側）：
  - `HeatState GetHeatState(int partId)`
  - `ArmorState GetArmorState(int partId)`
  - `UnityEngine.Vector3 GetWorldPosition(int partId)`
  - `float GetCurrentHp(int partId)`
  - `float GetMaxHp(int partId)`
- [ ] `IDifficultyProvider` 定義（唯讀，對齊 control-manifest §3 Difficulty MUST）：
  - `float BulletDensityMult { get; }`
  - `float EnemyCountMult { get; }`
- [ ] `ISaveService` 定義（對齊 ADR-0004 autosave 語義 + control-manifest §3 Meta MUST）：
  - `void EnqueueSave()` — 非同步存檔入列（`on_part_break` 即時觸發）
  - `T ReadValue<T>(string key, T defaultValue)` — 唯讀查詢存檔內容（若 Unity 6.3 泛型序列化有限制，可改為強型別多載，於實作時確認 **[需查證 6.3 API]**)
- [ ] `IWeaponTierQuery` 定義：
  - `int GetWeaponTier(WeaponId weaponId)` — 供 Weapons 讀取當前升級 Tier
- [ ] 所有介面 doc comment 明訂：「消費者（注入方）」與「實作者（實作方）」
- [ ] 任何假實作（`class FakePartStateQuery : IPartStateQuery`）可在不依賴任何系統的 EditMode 測試中編譯與執行

---

## Implementation Notes

*Derived from ADR-0002 §2 Decision + control-manifest §4.3:*

放置於 `Assets/_Project/Scripts/Core/Interfaces/` 子目錄：
- `IPartStateQuery.cs`、`IDifficultyProvider.cs`、`ISaveService.cs`、`IWeaponTierQuery.cs` 各自獨立檔

**`IPartStateQuery` 消費者與實作者**（doc comment 必須記載）：
- 消費者（注入方）：`KaijuBreaker.Weapons`（追蹤飛彈目標、L2/M3 Tier-3 觸發條件）、`KaijuBreaker.UI`（血條 H_current/H_max）
- 實作者：`KaijuBreaker.KaijuParts`

**`IDifficultyProvider` 消費者與實作者**：
- 消費者：`KaijuBreaker.Stage`（敵量密度）、`KaijuBreaker.BulletSim`（彈密度乘數）
- 實作者：`KaijuBreaker.Difficulty`

**`ISaveService` 消費者與實作者**：
- 消費者：`KaijuBreaker.Economy`（存素材結果）、`KaijuBreaker.Stage`（autosave 觸發點）、`KaijuBreaker.UI`（讀庫存顯示）
- 實作者：`KaijuBreaker.Meta`

**`IWeaponTierQuery` 消費者與實作者**：
- 消費者：`KaijuBreaker.Weapons`（讀 Tier 套用 `WeaponDef` 旋鈕）
- 實作者：`KaijuBreaker.Meta`（武器所有權與 Tier 持久化）

**型別依賴**：`IPartStateQuery` 使用 `HeatState`、`ArmorState`（Story 001 共用型別）；`IWeaponTierQuery` 使用 `WeaponId`（Story 001 共用型別）。確保 Story 001 DONE 後再實作此 Story。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: `App` 組合根建構具體實作並注入各系統（實際佈線）
- KaijuParts Epic: `KaijuParts` 系統實作 `IPartStateQuery` 的具體邏輯
- Meta Epic: `Meta` 系統實作 `ISaveService` 的原子存檔 worker

---

## QA Test Cases

*Authored at story creation (lean mode — qa-lead gate skipped). Developer implements against these.*

- **AC-1**: `IPartStateQuery` 假實作可獨立注入
  - Given: `class FakePartStateQuery : IPartStateQuery`，`GetCurrentHp(int)` 固定返回 `100f`；`GetWorldPosition(int)` 返回 `Vector3.zero`
  - When: 在 EditMode 測試中，以 `IPartStateQuery pq = new FakePartStateQuery();` 呼叫 `pq.GetCurrentHp(0)`
  - Then: 返回 `100f`；編譯與執行均無錯誤，無需引用 `KaijuParts` 系統組件
  - Edge cases: 呼叫未知 partId（如 -1）→ FakePartStateQuery 返回預設值而不拋例外

- **AC-2**: `IDifficultyProvider` 唯讀乘數可被假實作返回
  - Given: `class FakeDifficultyProvider : IDifficultyProvider`，`BulletDensityMult = 1.5f`，`EnemyCountMult = 2.0f`
  - When: 讀取 `BulletDensityMult` 與 `EnemyCountMult`
  - Then: 分別返回 `1.5f` 與 `2.0f`；屬性為唯讀（只有 getter，無 setter）

- **AC-3**: `IWeaponTierQuery` 假實作對 WeaponId 返回 Tier
  - Given: `class FakeWeaponTierQuery : IWeaponTierQuery`，`GetWeaponTier(WeaponId.L2)` 返回 `2`
  - When: `query.GetWeaponTier(WeaponId.L2)`
  - Then: 返回 `2`
  - Edge cases: 傳入 WeaponId enum 未定義的整數值（cast）→ FakeWeaponTierQuery 返回 `0`（或拋 ArgumentOutOfRangeException，由介面 doc comment 明訂其一）

- **AC-4**: 所有介面 doc comment 包含消費者與實作者資訊
  - Given: 四個介面的 XML doc comment
  - When: 以肉眼或文件生成工具確認
  - Then: 每個介面 `<summary>` 含「Consumer:」與「Implementor:」標記，或等價描述

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/EditMode/Core/core_query_interfaces_test.cs` — 必須存在且通過（BLOCKING）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（Core .asmdef + 共用型別：`HeatState`、`ArmorState`、`WeaponId`）must be DONE
- Unlocks: Story 006（App 組合根注入這些介面的具體實作）
