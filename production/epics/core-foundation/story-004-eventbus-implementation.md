# Story 004: EventBus 具體實作（同步分發 + 穩態零 GC）

> **Epic**: Core 基礎設施（事件匯流排 + DI）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: ~4h
> **Manifest Version**: 2026-07-02
> **Last Updated**: (set by /dev-story when implementation begins)

## Context

**GDD**: `docs/architecture/architecture.md` §5.2（雙軌事件流）+ ADR-0002 §1（型別化事件匯流排）
**Requirement**: `TR-core-001`
*(tr-registry.yaml 尚未正式化 — TR-ID 由 ADR-0002 推導；讀 EPIC.md GDD Requirements 表獲取規格)*

**ADR Governing Implementation**: ADR-0002（主）, ADR-0005（次）
**ADR Decision Summary**: `EventBus` 以同步分發實作 `IEventBus`——事件於 `Publish` 呼叫**當幀同步**派送所有訂閱者；`in T` 傳遞避免 struct 複製；穩態訂閱後連續 Publish 零 managed heap allocation；重入（鏈式破壞）以 dirty-flag 或快照迭代處理，不遞迴。

**Engine**: Unity 6.3 LTS (C#) | **Risk**: LOW
**Engine Notes**: `Dictionary<Type, Delegate>` 首次 `TryGetValue` 可能有初始化 alloc；穩態不應再 alloc。`GC.GetTotalMemory(false)` 在 Unity Test Framework (NUnit) EditMode 可用，但需 warm-up pass 後計算（排除 JIT/type-init 影響）。`Action<T>` 泛型委派對 struct 型別 T 不 box——此為 C# 泛型 reification 的既有保證，Unity 6.3 未改動。

**Control Manifest Rules (Foundation layer)**:
- Required: 同步分發 MUST — 事件於發布當幀同步派送（control-manifest §4.1）
- Required: 穩態零 GC MUST — 穩態 `Publish` 呼叫不產生 managed allocation（control-manifest §1.7 + ADR-0002 後果）
- Required: `EventBus` 以 DI 注入（建構子），可被假實作替換（control-manifest §1.3）
- Forbidden: MUST NOT 用 static 持有遊戲狀態（control-manifest §1.3 + ADR-0005 §3）
- Forbidden: MUST NOT 讓系統直接引用 `EventBus` 具體類別——系統只依賴 `IEventBus` 介面（control-manifest §1.4）
- Guardrail: 鏈式重入（handler 內 Publish）MUST NOT 遞迴——以 `is_chain_break` 旗標配合快照迭代防遞迴（ADR-0002 後果 §負面）

---

## Acceptance Criteria

*From ADR-0002 §1 Decision + architecture.md §5.2, scoped to TR-core-001:*

- [ ] `EventBus : IEventBus` 具體類別實作全三個方法：`Publish<T>`, `Subscribe<T>`, `Unsubscribe<T>`
- [ ] `Publish<T>(in T evt)` 在呼叫當幀同步執行所有已訂閱 `Action<T>` handler
- [ ] 訂閱同一事件型別的**多個** handler 皆按訂閱順序被呼叫
- [ ] `Unsubscribe<T>` 後，該 handler 不再收到事件；其他 handler 不受影響
- [ ] 穩態零 GC：訂閱完成後連續 `Publish<PartBroke>` 1000 次，GC allocation 為 0（或 < 32 bytes 熱路徑容許誤差）
- [ ] 鏈式重入（handler 在收到事件時再次 `Publish` 同型別事件）不拋例外、不陷入無限遞迴
- [ ] `EventBus` 以 `new EventBus()` 建構，不使用任何 static 狀態；可被 `FakeEventBus : IEventBus` 在測試中替換
- [ ] 傳入 null handler 的 `Subscribe` 拋 `ArgumentNullException`

---

## Implementation Notes

*Derived from ADR-0002 §1 + control-manifest §4.1:*

建議內部結構：

```
private readonly Dictionary<Type, List<Delegate>> _handlers = new();
```

**Publish 實作要點**：
- `TryGetValue(typeof(T), out var list)` 後，以**快照迭代**（`list.ToArray()` 或臨時 copy）處理重入——但 `ToArray()` 每次 Publish 都 alloc！改用 index 迭代 + dirty-flag 模式：
  ```
  // 推薦（零 alloc 穩態）：
  for (int i = 0; i < list.Count; i++)
      ((Action<T>)list[i]).Invoke(evt);
  ```
  若 handler 在迭代中呼叫 Unsubscribe，以 `_pendingRemove` list 收集，迭代結束後 flush。

**零 GC 關鍵**：
- `Dictionary.TryGetValue` 在 key 已存在時不 alloc（.NET Dictionary 穩態行為）
- `Action<T>` 的 `Invoke(T)` 對 struct T 不 box（泛型 reification）
- 避免在 Publish 熱路徑做任何 LINQ、lambda capture 或 `foreach` over `Dictionary`

**鏈式重入處理**（對齊 ADR-0002 §負面後果 + control-manifest §4.2）：
- `PartBroke` 的 handler 可能觸發 `Weapons` 系統的 L2/M3 鏈式效果，後者再次 `Publish<PartBroke>(new PartBroke { IsChainBreak = true })`
- 以 `_isDispatching` bool flag + `_pendingRemove` 模式處理：正在 dispatch 時的 Unsubscribe 入 pending，dispatch 結束後 flush
- 重入 Publish 直接遞迴執行（非佇列化）——GDD 要求同幀語義；但 `is_chain_break` 由 `KaijuParts` 設旗，防業務邏輯遞迴（EventBus 本身不限制重入深度，業務層負責終止條件）

**EventBus 生命週期**：在 `App`（Story 006）的 `AppBootstrap.Awake()` 建構；隨 Bootstrap 場景生命週期。不使用 DontDestroyOnLoad 的 static reference（用實例欄位持有）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: `AppBootstrap` 建構 `EventBus` 實例並注入系統
- Story 005: `IBulletSimBridge.DrainAndPublish` 呼叫 `IEventBus.Publish`（介面層，不在此）
- 各系統 Subscribe 具體事件（屬各系統 Epic 的故事）

---

## QA Test Cases

*Authored at story creation (lean mode — qa-lead gate skipped). Developer implements against these.*

- **AC-1**: 訂閱後發布事件，handler 收到正確 payload
  - Given: `var bus = new EventBus(); int callCount = 0; LaserHit received = default;`
    `bus.Subscribe<LaserHit>(evt => { callCount++; received = evt; });`
  - When: `bus.Publish(new LaserHit { PartId = 7, KaijuId = 1, HeatDelta = 0.5f })`
  - Then: `callCount == 1`；`received.PartId == 7`；`received.HeatDelta == 0.5f`
  - Edge cases: Publish 前未 Subscribe → callCount 維持 0，不拋例外

- **AC-2**: 多訂閱者皆收到事件
  - Given: `bus.Subscribe<MissileHit>(handlerA); bus.Subscribe<MissileHit>(handlerB);`
  - When: `bus.Publish(new MissileHit(...))`
  - Then: handlerA 與 handlerB 各被呼叫一次（`callCountA == 1`, `callCountB == 1`）

- **AC-3**: Unsubscribe 後 handler 不再收到事件
  - Given: `bus.Subscribe<LaserHit>(handler); bus.Unsubscribe<LaserHit>(handler);`
  - When: `bus.Publish(new LaserHit(...))`
  - Then: handler 不被呼叫（`callCount == 0`）
  - Edge cases: Unsubscribe 未訂閱的 handler → 靜默忽略，不拋例外

- **AC-4**: 穩態零 GC（訂閱後 Publish 不 alloc）
  - Given: `bus.Subscribe<PartBroke>(handler);`（warm-up：先呼叫一次 Publish 觸發型別初始化）
    `long before = GC.GetTotalMemory(false);`
  - When: 連續 `bus.Publish(new PartBroke(...))` 1000 次
  - Then: `GC.GetTotalMemory(false) - before < 32L`（0 alloc 或 JIT 殘留 < 32 bytes）
  - Edge cases: 每次 Publish 創建新 PartBroke struct → struct 在 stack，不 alloc

- **AC-5**: 鏈式重入不崩潰、不無限遞迴
  - Given: `int depth = 0;`
    `bus.Subscribe<PartBroke>(evt => { depth++; if (!evt.IsChainBreak) bus.Publish(new PartBroke { IsChainBreak = true }); });`
  - When: `bus.Publish(new PartBroke { IsChainBreak = false })`
  - Then: handler 被呼叫 2 次（外層 + 重入各一次）；`depth == 2`；無 StackOverflowException
  - Edge cases: 三層深度重入（A → B → C）→ 同樣不崩潰，handler 按進入順序執行

- **AC-6**: null handler 拋 ArgumentNullException
  - Given: `bus.Subscribe<LaserHit>(null)`
  - When: 呼叫
  - Then: 拋 `System.ArgumentNullException`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/EditMode/Core/core_eventbus_impl_test.cs` — 必須存在且通過（BLOCKING）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002（`IEventBus` 介面 + 事件 struct 定義）must be DONE
- Unlocks: Story 006（App 組合根建構 `EventBus` 實例並注入系統）
