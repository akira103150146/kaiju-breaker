# Story 005: DOTS↔Mono Bridge 值型 struct 合約

> **Epic**: Core 基礎設施（事件匯流排 + DI）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: ~2h
> **Manifest Version**: 2026-07-02
> **Last Updated**: (set by /dev-story when implementation begins)

## Context

**GDD**: `docs/architecture/architecture.md` §5.3（DOTS↔Mono Bridge）+ ADR-0002 §4（Bridge 決策）
**Requirement**: `TR-core-002`（Bridge 是事件契約 DOTS 側的唯一翻譯層，屬事件契約完整性的一部分）
*(tr-registry.yaml 尚未正式化 — TR-ID 由 ADR-0002 推導；讀 EPIC.md GDD Requirements 表獲取規格)*

**ADR Governing Implementation**: ADR-0002（主）, ADR-0005（次）
**ADR Decision Summary**: ECS Burst Job 產生命中結果至 `NativeQueue<HitEvent>`（值型 struct）；主執行緒 Bridge 排空佇列，翻譯為 managed 事件匯流排事件（`MissileHit`、`PlayerHit` 等）。Bridge 是 DOTS↔Mono 的**唯一翻譯層**（single seam），單點可測、可替換——此 Story 只定義 `Core` 側的合約（`HitEvent` struct + `IBulletSimBridge`），具體 DOTS 實作由 `BulletSim` Epic 提供。

**Engine**: Unity 6.3 LTS (C#) | **Risk**: LOW（介面合約本身）/ HIGH（BulletSim 實作側，不屬本 Story）
**Engine Notes**: `HitEvent` 必須是 `unmanaged` struct（Burst 友善，無 managed 引用）——`int`、`float`、`enum` 均符合。`IBulletSimBridge.DrainAndPublish` 在**主執行緒**呼叫，不在 Burst Job 中。`NativeQueue<HitEvent>` 的具體 Burst/ECS API 屬 BulletSim Epic，**不在本 Story 定義**（**[需查證 6.3 API]** 標記保留給 BulletSim 實作）。

**Control Manifest Rules (Foundation layer)**:
- Required: 跨 DOTS↔Mono 邊界 MUST 只傳值型 struct（POD），不傳 managed 引用（control-manifest §3 BulletSim + §4.1 Bridge）
- Required: Bridge MUST 是 DOTS↔Mono 事件的唯一翻譯層（control-manifest §4.2 MUST DOTS 側命中經 Bridge republish）
- Required: `IBulletSimBridge` 放置於 `Core`（介面）；具體實作由 `BulletSim` 提供（control-manifest §2 Foundation 層 / ADR-0005 邊界規則）
- Forbidden: `HitEvent` MUST NOT 含 managed 引用（string、class、delegate），必須通過 `unmanaged` 約束（control-manifest §3 BulletSim MUST NOT DOTS 型別外洩）
- Forbidden: `IBulletSimBridge` 不得在 Burst Job 內呼叫——介面方法在主執行緒執行（control-manifest §3 BulletSim）

---

## Acceptance Criteria

*From ADR-0002 §4 + architecture.md §5.3, scoped to TR-core-002 Bridge 側：*

- [ ] `HitType` 列舉定義於 `KaijuBreaker.Core`（PascalCase 值）：
  - `PlayerHit` — 敵彈命中玩家判定點
  - `MissilePartHit` — 玩家飛彈命中部位（republish 為 `MissileHit` 事件）
- [ ] `HitEvent` 定義為 `readonly struct`，通過 `where T : unmanaged` 約束：
  - `HitType HitType`
  - `int PartId`（`MissilePartHit` 時有效，`PlayerHit` 時為 -1）
  - `int KaijuId`（`MissilePartHit` 時有效，`PlayerHit` 時為 -1）
  - `WeaponId WeaponId`（`MissilePartHit` 時有效，`PlayerHit` 時為 `WeaponId` 預設值）
  - `UnityEngine.Vector3 HitPosition`（命中世界座標，兩種 HitType 皆有效）
- [ ] `IBulletSimBridge` 介面定義於 `KaijuBreaker.Core`，包含：
  - `void DrainAndPublish(IEventBus bus)` — 主執行緒呼叫；排空 `NativeQueue<HitEvent>` 並 Publish 對應的 managed 事件
- [ ] `HitEvent` 通過 `static void AssertUnmanaged<T>() where T : unmanaged` 約束的編譯期驗證
- [ ] `IBulletSimBridge` 假實作（`FakeBridge`）可在 EditMode 測試中呼叫 `DrainAndPublish(bus)` 並向 bus Publish `MissileHit` 事件，完全不依賴 `BulletSim` 組件

---

## Implementation Notes

*Derived from ADR-0002 §4 + architecture.md §5.3:*

放置於 `Assets/_Project/Scripts/Core/Bridge/` 子目錄：
- `HitType.cs`、`HitEvent.cs`、`IBulletSimBridge.cs`

**`HitEvent` unmanaged 驗證**：C# 編譯器自動強制 `where T : unmanaged`；但若日後有人誤加 managed 欄位，會在使用 `NativeQueue<HitEvent>` 時出現編譯錯誤。可加一個靜態斷言類別輔助診斷：

```csharp
// Core/Bridge/UnmanagedAssert.cs（可選工具型別）
internal static class UnmanagedAssert
{
    // 若 T 非 unmanaged，此方法無法編譯
    internal static void Check<T>() where T : unmanaged { }
}
```

在 `HitEvent.cs` 的 static constructor 或 test 中呼叫 `UnmanagedAssert.Check<HitEvent>()` 作編譯期錨點。

**`IBulletSimBridge` 語義**（對齊 architecture.md §5.3）：
- `DrainAndPublish` 由 `AppBootstrap`（Story 006）或 Bridge host MonoBehaviour 在主執行緒每幀呼叫（`Update`/`LateUpdate`，在 ECS World 完成 Job 後）
- BulletSim 實作持有 `NativeQueue<HitEvent>` 的引用；`DrainAndPublish` 排空並依 `HitType` 翻譯：
  - `HitType.MissilePartHit` → `bus.Publish(new MissileHit { PartId = evt.PartId, ... })`
  - `HitType.PlayerHit` → `bus.Publish(new PlayerHit { HitPosition = evt.HitPosition })` （`PlayerHit` struct 待 BulletSim Epic 確認後可補入 Story 002，或於 BulletSim Epic 補充 Core 事件）
- 本 Story **不定義** BulletSim 的 ECS System 或 `NativeQueue` 持有邏輯

**`Vector3` 在 unmanaged struct 中**：`UnityEngine.Vector3` 是 `struct`（包含三個 `float`），符合 `unmanaged` 約束，可放入 `HitEvent`。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- BulletSim Epic: `IBulletSimBridge` 具體實作（`NativeQueue<HitEvent>` 持有、Burst Job 填充、排空邏輯）
- Story 006: `AppBootstrap` 取得 `IBulletSimBridge` 實例並在每幀呼叫 `DrainAndPublish`
- `PlayerHit` 事件 struct 若需補入 Core（可在 BulletSim Epic 確認後追加至 Story 002 範疇）

---

## QA Test Cases

*Authored at story creation (lean mode — qa-lead gate skipped). Developer implements against these.*

- **AC-1**: `HitEvent` 通過 unmanaged 約束
  - Given: `HitEvent` struct 已定義（含 `HitType`, `int`, `int`, `WeaponId`, `Vector3` 欄位）
  - When: 在測試中呼叫 `UnmanagedAssert.Check<HitEvent>()`（或以 `where T : unmanaged` 泛型方法使用 `HitEvent`）
  - Then: 編譯成功，無錯誤
  - Edge cases: 若誤將任一欄位改為 class 引用型別 → `where T : unmanaged` 編譯失敗

- **AC-2**: `IBulletSimBridge` 假實作可向 `IEventBus` Publish 事件
  - Given: `var bus = new EventBus(); MissileHit received = default; int callCount = 0;`
    `bus.Subscribe<MissileHit>(evt => { received = evt; callCount++; });`
    假實作：`class FakeBridge : IBulletSimBridge { public void DrainAndPublish(IEventBus b) { b.Publish(new MissileHit { PartId = 3, KaijuId = 1 }); } }`
  - When: `new FakeBridge().DrainAndPublish(bus)`
  - Then: `callCount == 1`；`received.PartId == 3`；不依賴任何 `BulletSim` 組件

- **AC-3**: `HitType` 包含且僅包含兩個預期值
  - Given: `HitType` 列舉已定義
  - When: `System.Enum.GetValues(typeof(HitType))` 枚舉
  - Then: 結果集合 = { `PlayerHit`, `MissilePartHit` }，共 2 個

- **AC-4**: `HitEvent` 欄位名稱遵循 PascalCase
  - Given: `HitEvent` struct 已定義
  - When: `typeof(HitEvent).GetFields()` 枚舉欄位名
  - Then: 所有欄位名首字母大寫（`HitType`, `PartId`, `KaijuId`, `WeaponId`, `HitPosition`）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/EditMode/Core/core_bridge_contract_test.cs` — 必須存在且通過（BLOCKING）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002（`IEventBus` + 事件 struct，包含 `MissileHit`；`WeaponId` 共用型別來自 Story 001）must be DONE
- Unlocks: Story 006（App 取得 `IBulletSimBridge` 實例並每幀調用）
