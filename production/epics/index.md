# Epics Index

Last Updated: 2026-07-02
Engine: Unity 6.3 LTS (C#)

> One epic per architectural module (`.asmdef`), per `docs/architecture/architecture.md` §2/§8 traceability matrix.
> **tr-registry.yaml not yet formalized** — TR-IDs in each EPIC.md are derived from GDD Acceptance Criteria; to be formalized via `/architecture-review`.

| Epic | Layer | System | GDD | Stories | Status |
|------|-------|--------|-----|---------|--------|
| Core 基礎設施（事件匯流排 + DI） | Foundation | 橫切 (`Core`/`App`) | docs/architecture/architecture.md §2/§5 | 6 stories | Ready |
| Content 調校資料框架 | Foundation | 全系統調校資料 (`Content`) | docs/architecture/architecture.md §6 | 9 stories | Ready |
| 元進度與存檔系統 | Foundation | S10 Meta/Save | design/gdd/meta-progression-system.md | 7 stories | Ready |
| 子彈/彈幕引擎（DOTS） | Foundation | S9 Bullet Sim | design/gdd/bullet-system.md | 9 stories（1 Ready spike, 8 Blocked on ADR-0001） | Ready |
| 武器系統 | Core | S1 Weapons | design/gdd/weapon-system.md | Not yet created | Ready |
| 可破壞部位系統 | Core | S2 Kaiju Parts | design/gdd/kaiju-part-system.md | Not yet created | Ready |
| 素材經濟與永久升級 | Core | S3 Economy | design/gdd/material-economy.md | Not yet created | Ready |
| 難度系統 | Core | S5 Difficulty | design/gdd/difficulty-system.md | Not yet created | Ready |
| 關卡系統與 Run 流程 | Core | S4 Stage | design/gdd/stage-system.md | Not yet created | Ready |
| 巨獸內容（CARAPEX/LACERA/VOLTWYRM） | Feature | C1 Kaiju Roster | design/gdd/kaiju/01-carapex.md · 02-lacera.md · 03-voltwyrm.md | Not yet created | Ready |
| 輸入系統 | Feature | S6 Input | design/gdd/input-system.md | Not yet created | Ready |
| HUD / UI 系統 | Presentation | S7 HUD/UI | design/gdd/hud-ui-system.md | Not yet created | Ready |
| 打擊感（Game Feel） | Presentation | S8 Game Feel | design/gdd/game-feel.md | Not yet created | Ready |

## Engine Risk Summary

| Epic | Engine Risk | Note |
|------|-------------|------|
| 子彈/彈幕引擎（DOTS） | **HIGH** | ADR-0001 Proposed — 待效能原型於基準機達 1,000@60fps + 零 GC 才 LOCK；Entities 1.3 API [需查證] |
| 元進度與存檔系統 | MEDIUM | ADR-0004 — `JsonUtility` canonical/dictionary、fsync/rename、`OnApplicationPause/Quit` 均 [需查證 6.3 API] |
| 輸入系統 | MEDIUM | Unity 6 Input System package API 對 2022.3 有重大變更 [需查證]；觸控手感原型阻斷 pre-MVP |
| HUD / UI 系統 | MEDIUM | UI 框架 ADR（UGUI vs UI Toolkit）待補 |
| 打擊感（Game Feel） | MEDIUM | `time_scale=0` 凍結敵彈的 ECS 時間注入 [需查證 6.3 API] |
| 其餘 8 個 Epic | LOW | ADR-0002/0003/0005 均 Accepted，標準 Unity/C# 模式 |
