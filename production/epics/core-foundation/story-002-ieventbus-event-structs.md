# Story 002: IEventBus 介面與 GDD 事件 readonly struct 定義

> **Epic**: Core 基礎設施（事件匯流排 + DI）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: ~3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: (set by /dev-story when implementation begins)

## Context

**GDD**: `docs/architecture/architecture.md` §5.2（雙軌事件流）+ ADR-0002 §1（型別化事件匯流排）
**Requirement**: `TR-core-001`, `TR-core-002`
*(tr-registry.yaml 尚未正式化 — TR-ID 由 ADR-0002/0005 推導；讀 EPIC.md GDD Requirements 表獲取規格)*

**ADR Governing Implementation**: ADR-0002（主）, ADR-0005（次）
**ADR Decision Summary**: `IEventBus` 與所有事件型別定義於 `Core`；事件為 `readonly struct`（`IGameEvent`），以 `Publish<T>(in T evt)` 同步同幀分發，`in` 傳遞避免複製，穩態零 GC；事件命名映射 GDD 契約（`on_part_break` → `PartBroke`，無 `On` 前綴）。

**Engine**: Unity 6.3 LTS (C#) | **Risk**: LOW
**Engine Notes**: 純 C# 泛型介面與 `readonly struct`，無 Unity 6.3 post-cutoff API。`in` 參數修飾符為 C# 7.2+ 標準功能，Unity 6 完整支援。`Action<T>` 泛型委派不 box struct（C# 泛型 reification 保證）。

**Control Manifest Rules (Foundation layer)**:
- Required: 事件型別 MUST 為 `readonly struct` 實作 `IGameEvent`（control-manifest §4.1）
- Required: 事件命名 MUST 遵循 GDD 契約 PascalCase 映射，無 `On` 前綴（control-manifest §1.1 / §6 裁決）
- Required: MUST 用 `IEventBus.Publish<T>(in T)` / `Subscribe<T>(Action<T>)`（control-manifest §4.1）
- Forbidden: MUST NOT 用 C# `event`/`Action` 直接互訂（control-manifest §4.1）
- Forbidden: `Core` MUST NOT 依賴任何系統組件（control-manifest §2 Foundation 層）
- Guardrail: 所有 struct 欄位型別使用 Story 001 共用型別（`PartType`, `BreakQuality`, `WeaponId`）

---

## Acceptance Criteria

*From `docs/architecture/architecture.md` §5.2 + ADR-0002 §1/§3, scoped to TR-core-001 / TR-core-002:*

- [ ] `IGameEvent` marker interface 定義於 `KaijuBreaker.Core`（空介面，作為泛型約束）
- [ ] `IEventBus` 定義，包含三個方法：
  - `void Publish<T>(in T evt) where T : struct, IGameEvent`
  - `void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent`
  - `void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent`
- [ ] 下列 GDD 事件 struct 全部定義為 `readonly struct : IGameEvent`，各自獨立 `.cs` 檔：
  - `LaserHit`：`PartId (int)`, `KaijuId (int)`, `HeatDelta (float)` — 對應 `on_laser_hit`
  - `MissileHit`：`PartId (int)`, `KaijuId (int)`, `BreakDeltaBase (float)`, `WeaponId (WeaponId)` — 對應 `on_missile_hit`
  - `L3WaveHit`：`PartId (int)`, `KaijuId (int)` — 對應 `on_l3_wave_hit`
  - `PartSoftened`：`PartId (int)`, `KaijuId (int)` — 對應 `on_part_softened`
  - `PartSoftenedExit`：`PartId (int)`, `KaijuId (int)` — 對應 `on_part_softened_exit`
  - `PartStaggered`：`PartId (int)`, `KaijuId (int)` — 對應 `on_part_staggered`
  - `PartStaggerEnd`：`PartId (int)`, `KaijuId (int)` — 對應 `on_part_stagger_end`
  - `PartBroke`：`PartId (int)`, `KaijuId (int)`, `PartType (PartType)`, `WorldPosition (UnityEngine.Vector3)`, `DropTableId (int)`, `BreakQuality (BreakQuality)`, `AdjacencyList (int[])`, `IsChainBreak (bool)` — 對應 `on_part_break`
  - `BossCoreBroke`：`KaijuId (int)`, `WorldPosition (UnityEngine.Vector3)` — 對應 `on_boss_core_break`
- [ ] 無任何事件 struct 名稱以 `On` 開頭（control-manifest §1.1 裁決）
- [ ] `IEventBus` 泛型約束 `where T : struct, IGameEvent` 可在編譯期阻止以 class 型別呼叫

---

## Implementation Notes

*Derived from ADR-0002 §1 Decision + control-manifest §4.1:*

放置於 `Assets/_Project/Scripts/Core/Events/` 子目錄：
- `IGameEvent.cs`、`IEventBus.cs` 各自獨立檔
- 每個事件 struct 獨立 `.cs` 檔（`PartBroke.cs`、`LaserHit.cs` 等）

**`PartBroke.AdjacencyList` 型別注意**：GDD payload 需 adjacency list。`int[]` 是 managed 陣列，在 `readonly struct` 中合法，但意味著 `PartBroke` 本身不是 blittable（`unmanaged`）struct——這是可接受的，因為 `PartBroke` 走 managed `IEventBus`（非 DOTS NativeQueue）。若日後需跨 DOTS 邊界，該事件需另外設計。目前 `int[]` 是最直覺的實作；若平台 GC 壓力出現可改為固定大小 struct buffer。

**事件命名權威**（control-manifest §1.1 + §6 裁決）：
- 事件 `struct` 型別名：無前綴（`PartBroke`，非 `OnPartBroke`）
- C# `event`/`Action` 委派欄位若有才加 `On` 前綴（此 Story 無此需求）

**`IEventBus` 泛型 Unsubscribe**：以相同 `Action<T>` 實例配對；若傳入未訂閱的 handler，靜默忽略（不拋例外）。

此 Story **只定義介面與 struct 型別**，不實作分發邏輯（Story 004）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: `EventBus` 具體類別（訂閱管理、同步分發、零 GC 實作）
- Story 005: `HitEvent` struct 與 `IBulletSimBridge`（DOTS 側橋接合約）
- Story 006: App 佈線 `IEventBus` 實例注入各系統

---

## QA Test Cases

*Authored at story creation (lean mode — qa-lead gate skipped). Developer implements against these.*

- **AC-1**: `IEventBus` 泛型約束阻止 class 型別
  - Given: `IEventBus` 已定義，`Publish<T> where T : struct, IGameEvent`
  - When: 嘗試以非 struct 型別（如 `string`）呼叫 `Publish<string>` 並編譯
  - Then: 編譯錯誤（型別約束不滿足），無法通過

- **AC-2**: `PartBroke` 包含 GDD 所有必要欄位
  - Given: `PartBroke` struct 已定義
  - When: `typeof(PartBroke).GetFields()` 枚舉所有欄位
  - Then: 包含 `PartId`, `KaijuId`, `PartType`, `WorldPosition`, `DropTableId`, `BreakQuality`, `AdjacencyList`, `IsChainBreak`（共 8 欄位）
  - Edge cases: 多一個或少一個欄位均應失敗

- **AC-3**: 所有事件 struct 為 `readonly struct`
  - Given: 事件 struct 清單（LaserHit, MissileHit, L3WaveHit, PartSoftened, PartSoftenedExit, PartStaggered, PartStaggerEnd, PartBroke, BossCoreBroke）
  - When: 以 `type.IsValueType && type.GetCustomAttribute<System.Runtime.CompilerServices.IsReadOnlyAttribute>()` 或同等方式驗證
  - Then: 全部為 `readonly struct`，無一例外
  - Edge cases: 若誤定義為 `struct`（可寫），應失敗

- **AC-4**: 事件型別名稱無 `On` 前綴
  - Given: 所有實作 `IGameEvent` 的型別
  - When: 讀取各型別的 `Name` 屬性
  - Then: 無任何名稱以字串 `"On"` 開頭

- **AC-5**: `BreakQuality` 欄位使用 Core 共用型別
  - Given: `PartBroke.BreakQuality` 欄位
  - When: 讀取欄位型別 (`FieldInfo.FieldType`)
  - Then: 型別為 `KaijuBreaker.Core.BreakQuality`（非 int 或 string）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/EditMode/Core/core_eventbus_interface_test.cs` — 必須存在且通過（BLOCKING）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（Core .asmdef + 共用型別，包含 `PartType`, `BreakQuality`, `WeaponId`）must be DONE
- Unlocks: Story 004（EventBus 具體實作）、Story 005（Bridge 合約使用事件 struct）
