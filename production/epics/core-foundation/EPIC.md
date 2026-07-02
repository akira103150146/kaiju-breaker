# Epic: Core 基礎設施（事件匯流排 + DI）

> **Layer**: Foundation
> **GDD**: docs/architecture/architecture.md §2/§5（橫切基礎設施，無專屬 GDD）
> **Architecture Module**: `KaijuBreaker.Core` + `KaijuBreaker.App`（組合根）
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories core-foundation`

## Overview

本 Epic 建立全專案的橫切技術骨幹：`KaijuBreaker.Core` 定義型別化事件匯流排 (`IEventBus`)、所有系統間共享的強型別事件 struct（`PartBroke`/`LaserHit`/`MissileHit`/`BossCoreBroke`…）、唯讀查詢介面（`IPartStateQuery`/`IDifficultyProvider`/`ISaveService`/`IWeaponTierQuery`）、共用型別（`WeaponId`/`PartType`/`BreakQuality`/`DifficultyTier`）與 Run 狀態列舉；`KaijuBreaker.App` 作為唯一組合根，於啟動時佈線 DI、事件匯流排與場景載入。此模組是 ADR-0002 事件架構與 ADR-0005 組件邊界的落地基座，所有其他系統僅依賴它通訊，達成「系統零直接互引用、可獨立單元測試」。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 ADR-0002/0005 決策條目推導，待 `/architecture-review` 建立正式登記表。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: 事件架構與系統間通訊 | Core 型別化事件匯流排 + 唯讀查詢介面；readonly struct 事件、同步同幀分發、零 GC | LOW |
| ADR-0005: 專案結構與組件邊界 | 一系統一 `.asmdef`；系統零直接互引用；`App` 唯一組合根；DI over singletons | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-core-001 | 型別化事件匯流排 `Publish<T>(in T)`/`Subscribe<T>`；事件為 readonly struct、`in` 傳遞、穩態零 GC | ADR-0002 ✅ |
| TR-core-002 | 所有 GDD 事件契約以 Core struct 表達，同步同幀分發（維持破壞入帳/漣漪/Boss 核心事件順序語義） | ADR-0002 ✅ |
| TR-core-003 | 唯讀查詢介面於 Core，`App` 建構具體實作並注入；系統測試可注入假實作 | ADR-0002 ✅ |
| TR-core-004 | 一系統一 `.asmdef`，references 編譯期強制系統零直接互引用 | ADR-0005 ✅ |
| TR-core-005 | `App` 為唯一組合根；禁止持有遊戲狀態的 static 單例；每系統可脫離其他系統以假依賴 EditMode 測試 | ADR-0005 ✅ |
| TR-core-006 | 共用型別與 Run 狀態列舉集中於 Core（僅放真正共享的抽象，不放實作） | ADR-0005 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `docs/architecture/architecture.md` §2/§5 and ADR-0002/ADR-0005 are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories core-foundation` to break this epic into implementable stories.
