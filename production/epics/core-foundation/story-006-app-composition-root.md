# Story 006: App 組合根 DI 佈線合約

> **Epic**: Core 基礎設施（事件匯流排 + DI）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: ~3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: (set by /dev-story when implementation begins)

## Context

**GDD**: `docs/architecture/architecture.md` §4.1（Bootstrap 場景）+ §5.4（DI 組合）
**Requirement**: `TR-core-005`
*(tr-registry.yaml 尚未正式化 — TR-ID 由 ADR-0005 推導；讀 EPIC.md GDD Requirements 表獲取規格)*

**ADR Governing Implementation**: ADR-0005（主：唯一組合根）, ADR-0002（次：IEventBus 佈線）
**ADR Decision Summary**: `KaijuBreaker.App` 是**唯一**引用全部系統組件的 `.asmdef`；`AppBootstrap : MonoBehaviour` 於 Bootstrap 場景（常駐）的 `Awake` 中建構 `EventBus`、各查詢介面的具體實作，並以建構子/方法注入接線——禁止 static 持有遊戲狀態；每系統可脫離 App 以 `FakeEventBus` 在 EditMode 獨立測試。

**Engine**: Unity 6.3 LTS (C#) | **Risk**: LOW
**Engine Notes**: `DontDestroyOnLoad(gameObject)` 確保 Bootstrap 場景常駐——此 API 在 Unity 6.x 穩定無變更。`Awake` 生命週期順序在 Bootstrap 場景中為確定性的（此場景是 `BuildSettings` 中 Index 0）。若需跨 Scene 持有服務，以 `DontDestroyOnLoad` 而非 static 欄位實現；服務實例存於 `AppBootstrap` 的**實例欄位**（非 static）。**[需查證 6.3 API]**: `Application.quitting` 事件（用於同步存檔安全網）在 Unity 6.x 的行為，實作時確認。

**Control Manifest Rules (Foundation / Composition layer)**:
- Required: `App` MUST 引用全部系統 `.asmdef`（唯一允許此操作的組件）（control-manifest §2 Composition 層 + ADR-0005 §3）
- Required: `AppBootstrap` MUST 以 DI（建構子/方法注入）傳遞 `IEventBus` + 查詢介面，不用 static 存取點（control-manifest §1.3）
- Required: 組合根建構完成後，全專案無 static 欄位持有遊戲狀態（control-manifest §1.3 MUST NOT）
- Required: Bootstrap 場景常駐（`DontDestroyOnLoad`）；`App` 不含遊戲邏輯（control-manifest §2 Composition）
- Forbidden: MUST NOT 使用 Unity `FindObjectOfType` 作跨系統查找替代 DI（ADR-0005 §3 精神）
- Guardrail: `App.asmdef` references 清單為全部系統——任何新系統 Epic 完成後需更新此清單（協調規則）

---

## Acceptance Criteria

*From ADR-0005 §3 + architecture.md §4.1/§5.4, scoped to TR-core-005:*

- [ ] `KaijuBreaker.App.asmdef` 建立於 `Assets/_Project/Scripts/App/`，`references` 包含全部系統組件（`KaijuBreaker.Core`, `KaijuBreaker.Content`, `KaijuBreaker.Weapons`, `KaijuBreaker.KaijuParts`… 等——此 Story 建立骨架時僅含已完成的 `Core`；後續各系統 Epic 完成後追加引用）
- [ ] `AppBootstrap : MonoBehaviour` 建立，`Awake()` 中建構 `EventBus` 實例（`IEventBus`）並存於**實例欄位**（非 static）
- [ ] `AppBootstrap.Awake()` 呼叫 `DontDestroyOnLoad(gameObject)` 確保 Bootstrap 場景常駐
- [ ] 組合根以**方法/建構子注入**傳遞依賴；無任何 `static` 欄位持有 `IEventBus` 或查詢介面實例
- [ ] 場景骨架：`Assets/_Project/Scenes/Bootstrap.unity` 存在，掛載 `AppBootstrap`，並被設為 `BuildSettings` 的 Scene Index 0
- [ ] 任何系統（如 Economy stub）可在 EditMode 測試中接受 `new EventBus()` 直接注入，完全不依賴 `AppBootstrap` 或 Bootstrap 場景——驗證 DI 設計使各系統可獨立測試

---

## Implementation Notes

*Derived from ADR-0005 §3 + architecture.md §4.1/§5.4:*

**`AppBootstrap.Awake()` 建構順序**：
1. `var eventBus = new EventBus();` — 建構匯流排實例
2. （各系統具體實作待各 Epic 完成後填入）`// var partStateQuery = new KaijuPartStateQuery(...); `
3. 注入：`// _weaponsSystem.Initialize(eventBus, partStateQuery);`
4. `DontDestroyOnLoad(gameObject);`

此 Story 只建立 `AppBootstrap` **骨架**——步驟 2/3 以 `// TODO: inject [SystemName] after [EpicName] Epic` 占位。後續各系統 Epic 的最後一個故事負責填入注入邏輯並更新 `App.asmdef` references。

**服務定位模式禁止**：禁止 `ServiceLocator.Register(eventBus)` 等 static registry 模式——與 ADR-0005 §3 DI over singletons 原則衝突。

**`IBulletSimBridge` 每幀呼叫**：`AppBootstrap` 應持有 `IBulletSimBridge` 實例，在 `Update()` 或 `LateUpdate()` 中呼叫 `_bridge.DrainAndPublish(_eventBus)`（BulletSim Epic 完成後填入具體實作）。此 Story 在骨架中加入方法殼與占位注釋。

**Bootstrap 場景建立**：在 `Assets/_Project/Scenes/` 建立空場景 `Bootstrap.unity`，建立空 `GameObject` 命名為 `[AppBootstrap]`，掛載 `AppBootstrap` 組件，加入 `BuildSettings` Index 0。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- 各系統具體實作的注入填充（各系統 Epic 各自 Story 負責更新 AppBootstrap）
- MetaHub / Run 場景載入邏輯（Stage Epic）
- `on_app_suspend` 同步存檔安全網（Meta Epic）

---

## QA Test Cases

*Authored at story creation (lean mode — qa-lead gate skipped). Integration type.*

- **AC-1**: `AppBootstrap.Awake()` 建構後 IEventBus 非 null
  - Given: 在 EditMode 測試中建立 `GameObject` 並 `AddComponent<AppBootstrap>()`
  - When: 呼叫 `bootstrap.Awake()`（或讓 Unity Test Framework 執行 Awake）
  - Then: 透過 internal/public accessor 或 reflection 確認 `_eventBus` 欄位非 null；型別為 `EventBus`
  - Edge cases: `Awake` 拋例外 → test fail，例外不被 swallow（AppBootstrap 不 try-catch Awake）

- **AC-2**: 無 static 欄位持有遊戲狀態
  - Given: `AppBootstrap` 類別已定義
  - When: `typeof(AppBootstrap).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)` 枚舉靜態欄位
  - Then: 零個靜態欄位（或僅有常數 / readonly 靜態工具，不含 IEventBus、IPartStateQuery 等服務型別）
  - Edge cases: 若任一 static 欄位型別實作 `IGameEvent` 或查詢介面 → 測試失敗

- **AC-3**: 系統可脫離 App 以假依賴獨立測試
  - Given: 假設有 Economy stub class；`var bus = new EventBus(); var economy = new EconomyStub(bus);`（stub 直接接受 IEventBus 建構子）
  - When: `bus.Publish(new PartBroke { BreakQuality = BreakQuality.SOFTENED, ... })`
  - Then: `economy` 收到事件並執行其 Subscribe handler——全程不啟動 Bootstrap 場景，不涉及 `AppBootstrap`
  - Edge cases: 此 AC 在各系統 Epic 的整合測試中逐步驗證；本 Story 以 Core + EventBus 做最小版 smoke check

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/EditMode/App/app_composition_root_test.cs` — 必須存在且通過（BLOCKING）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003（查詢介面），Story 004（EventBus 實作），Story 005（Bridge 合約）must be DONE
- Unlocks: 各系統 Feature Epic（可開始接入 App 佈線）
