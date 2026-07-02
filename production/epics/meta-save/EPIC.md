# Epic: 元進度與存檔系統

> **Layer**: Foundation
> **GDD**: design/gdd/meta-progression-system.md
> **Architecture Module**: `KaijuBreaker.Meta`
> **Status**: Ready
> **Stories**: 7 stories — see table below

## Overview

本 Epic 實作永久養成資料的持久化：`KaijuBreaker.Meta` 以單槽 JSON（`Application.persistentDataPath`）+ 暫存改名原子寫入 + 備份 + CRC32 完整性 + 純函數版本遷移，經 `ISaveService` 供其他系統使用。核心承諾為「永久養成進度永不丟失」——`on_part_break` 同幀寫記憶體永久庫存並 enqueue 背景非同步存檔，`on_app_suspend/quit` 走同步寫作安全網。亦擁有武器所有權模型（拾取=永久解鎖）與 last_loadout/last_difficulty 預填。此模組屬 Foundation，因所有戰鬥/經濟系統的入帳都經它落地。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 `meta-progression-system.md` §H 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0004: 存檔系統（JSON + 原子寫入 + CRC32） | 單槽 JSON、暫存改名原子寫、備份、CRC32、版本遷移、背景非同步寫 + 事件即時入帳 | MEDIUM（`JsonUtility` dictionary/canonical、fsync/rename 原子性、`OnApplicationPause/Quit` 各平台保證均標 [需查證 6.3 API]）|
| ADR-0002: 事件架構 | 訂閱 `on_part_break` 等事件即時入帳；經 `ISaveService` 查詢介面對外 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-meta-001 | H.1 進程於 `on_part_break` 後 100ms 內被殺，重啟後該素材完整（誤差 0）；suspend 後強殺存檔完整 | ADR-0004 ✅ |
| TR-meta-002 | H.2 材料入帳公式正確：27 情境 + hunt-end 精魄/碎片 + 連續破壞累加無溢位 | ADR-0004 ✅ |
| TR-meta-003 | H.3 武器所有權狀態機：新遊戲 L1/M1 owned；首次拾取→永久 owned；二次拾取不重複解鎖 | ADR-0004 ✅ |
| TR-meta-004 | H.4 原子寫入：write 後 rename 前被殺，磁碟 save.json 恆為舊/新完整之一，無損毀 JSON | ADR-0004 ✅ |
| TR-meta-005 | H.5 CRC32 完整性校驗；失敗讀備份；皆損毀顯示重置畫面不崩潰 | ADR-0004 ✅ |
| TR-meta-006 | H.6 版本遷移純函數鏈；未來版本拒載；缺欄位以新遊戲預設填充 | ADR-0004 ✅ |
| TR-meta-007 | H.7/H.8 last_difficulty/last_loadout 預填；loadout 指向未擁有武器時 fallback 至 L1 | ADR-0004 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/meta-progression-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | SaveData Schema & Canonical JSON Serializer | Logic | Ready | ADR-0004 |
| 002 | Atomic Temp-Then-Rename Write & Backup | Logic | Ready | ADR-0004 |
| 003 | CRC32 Integrity Check, Load & Corruption Repair | Logic | Ready | ADR-0004 |
| 004 | Save Versioning & Migration Chain | Logic | Ready | ADR-0004 |
| 005 | Persistent vs Per-Run State Boundary & New Game Init | Logic | Ready | ADR-0004 |
| 006 | Autosave-on-Bank: on_part_break Instant Credit & Suspend Sync | Integration | Ready | ADR-0004, ADR-0002 |
| 007 | Weapon Ownership & Unlock Persistence | Logic | Ready | ADR-0004 |

## Next Step

Run `/story-readiness production/epics/meta-save/story-001-save-schema-serializer.md` to begin implementation. Work through stories in order — each story's `Depends on:` field tells you what must be DONE before starting it.
