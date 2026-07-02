# Story 001: Core .asmdef 建立與共用型別定義

> **Epic**: Core 基礎設施（事件匯流排 + DI）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: ~2h
> **Manifest Version**: 2026-07-02
> **Last Updated**: (set by /dev-story when implementation begins)

## Context

**GDD**: `docs/architecture/architecture.md` §2/§5（橫切基礎設施，無專屬 GDD）
**Requirement**: `TR-core-004`, `TR-core-006`
*(tr-registry.yaml 尚未正式化 — TR-ID 由 ADR-0002/0005 推導；讀 EPIC.md GDD Requirements 表獲取規格)*

**ADR Governing Implementation**: ADR-0005（主）, ADR-0002（次）
**ADR Decision Summary**: 一系統一 `.asmdef`，依賴邊界由編譯器強制——`Core` 只依賴 `UnityEngine`，不引用任何系統或 `Content`；共用型別（列舉、值型別）集中於 `Core`，僅放真正跨系統共享的抽象，不放實作。

**Engine**: Unity 6.3 LTS (C#) | **Risk**: LOW
**Engine Notes**: 純 C# 列舉與 struct 定義，無 Unity 6.3 post-cutoff API 依賴。`.asmdef` 格式在 Unity 6.x 穩定，無需查證。

**Control Manifest Rules (Foundation layer)**:
- Required: `Core` MUST 僅依賴 `UnityEngine`；一組件一 `.asmdef`（ADR-0005 §1/§2）
- Required: `Core` MUST 擁有共用列舉、共用值型別、狀態機列舉（control-manifest §3 Core）
- Forbidden: `Core` MUST NOT 依賴任何系統組件、`Content`、DOTS 套件或任何實作（control-manifest §2 Foundation 層）
- Forbidden: MUST NOT 在 `Core` 放任何執行期行為邏輯（control-manifest §3 Core）
- Guardrail: 所有型別 PascalCase 命名；public 定義有 doc comment（control-manifest §1.1 / §1.8）

---

## Acceptance Criteria

*From `docs/architecture/architecture.md` §2 + ADR-0005, scoped to TR-core-004 / TR-core-006:*

- [ ] `KaijuBreaker.Core.asmdef` 建立於 `Assets/_Project/Scripts/Core/`，`references` 僅含 `UnityEngine`（無任何系統、`Content`、DOTS 套件引用）
- [ ] 下列共用列舉定義完成，各自獨立 `.cs` 檔，均在 `KaijuBreaker.Core` namespace：
  - `WeaponId`（L1..L4, M1..M4 標示符）
  - `PartType`（巨獸部位分類：至少 LIMB, CARAPACE, BOSS_CORE；參照 `kaiju-part-system.md` payload 契約）
  - `BreakQuality`（NORMAL, SOFTENED, SOFTENED_STAGGERED）
  - `DifficultyTier`（D0..D4 或等價值）
  - `HeatState`（NORMAL, SOFTENED — 用於 `IPartStateQuery` 回傳 + GameFeel 事件消費）
  - `ArmorState`（INTACT, STAGGERED — 用於 `IPartStateQuery` 回傳 + KaijuParts 狀態機）
- [ ] Run 狀態列舉 `RunState`（LOADOUT, STAGE, BOSS, RESULTS）定義完成
- [ ] 所有公開型別有 doc comment；命名全部 PascalCase
- [ ] 任何系統 `.asmdef` 若加入 `KaijuBreaker.Core` 引用，即可編譯使用上述型別（無隱性依賴）

---

## Implementation Notes

*Derived from ADR-0005 §1/§6 + control-manifest §3 Core:*

建立目錄 `Assets/_Project/Scripts/Core/`，放置 `KaijuBreaker.Core.asmdef`（`autoReferenced: false`，platforms: Any）。

型別依功能分子目錄（推薦）：
- `Assets/_Project/Scripts/Core/Types/` → `WeaponId.cs`, `PartType.cs`, `BreakQuality.cs`, `DifficultyTier.cs`, `HeatState.cs`, `ArmorState.cs`, `RunState.cs`

每個型別一個 `.cs` 檔；namespace 統一為 `KaijuBreaker.Core`。

**`BreakQuality` 語義**（對齊 ADR-0002 §3 + control-manifest §4.2）：
- `NORMAL` — 部位自然破壞（無蓄熱加成、無暈眩）
- `SOFTENED` — 破壞時部位處於 SOFTENED 狀態（有熱加成）
- `SOFTENED_STAGGERED` — 破壞時部位處於 SOFTENED + STAGGERED 雙狀態（最高倍率）

**`DifficultyTier` 語義**（對齊 `difficulty-system.md` D0–D4 結構）：若設計 GDD 確認具體值前，定義為 `D0, D1, D2, D3, D4` 五個值。

此 Story **只定義型別**，不包含任何介面（IEventBus 等）或實作邏輯。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: `IEventBus`、`IGameEvent` 介面與所有事件 `readonly struct`
- Story 003: `IPartStateQuery`、`IDifficultyProvider`、`ISaveService`、`IWeaponTierQuery` 查詢介面
- Story 004: `EventBus` 具體實作
- Story 005: `HitEvent` struct 與 `IBulletSimBridge` 介面

---

## QA Test Cases

*Authored at story creation (lean mode — qa-lead gate skipped). Developer implements against these.*

- **AC-1**: `KaijuBreaker.Core.asmdef` 存在且引用清單乾淨
  - Given: `KaijuBreaker.Core.asmdef` 已寫入 `Assets/_Project/Scripts/Core/`
  - When: Unity Editor 重新編譯（或 CI `asmdef` 解析）
  - Then: Assembly Browser 顯示 `KaijuBreaker.Core`；`references` 清單不含任何非 `UnityEngine` 條目
  - Edge cases: 若 references 清單空白亦為合法（`.asmdef` 預設僅 UnityEngine）

- **AC-2**: `BreakQuality` 具備且僅有三個預期值
  - Given: `BreakQuality` 列舉已定義
  - When: `System.Enum.GetValues(typeof(BreakQuality))` 枚舉
  - Then: 結果集合 = { NORMAL, SOFTENED, SOFTENED_STAGGERED }，共 3 個，無其他值
  - Edge cases: 若日後新增值，此測試應失敗並提示更新下游系統

- **AC-3**: `RunState` 具備且僅有四個預期轉換狀態
  - Given: `RunState` 列舉已定義
  - When: `System.Enum.GetValues(typeof(RunState))` 枚舉
  - Then: 結果集合 = { LOADOUT, STAGE, BOSS, RESULTS }，共 4 個
  - Edge cases: 值的整數順序應從 0 起連續（若狀態機以 int 比較）

- **AC-4**: 所有共用型別可從外部系統組件引用
  - Given: 測試組件 `KaijuBreaker.Core.Tests.asmdef` 引用 `KaijuBreaker.Core`
  - When: 測試檔中 `using KaijuBreaker.Core;` 並使用 `BreakQuality.NORMAL`, `RunState.BOSS` 等
  - Then: 編譯成功，無「型別未找到」錯誤

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/EditMode/Core/core_shared_types_test.cs` — 必須存在且通過（BLOCKING）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None（此為 core-foundation 首個 Story）
- Unlocks: Story 002（IEventBus + 事件 struct 需要共用型別）、Story 003（查詢介面需要 `PartType`、`HeatState`、`ArmorState`、`WeaponId`）
