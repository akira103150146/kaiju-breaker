# Active Session State

*Last updated: 2026-07-01*

## Current Task
`/prototype` **IN PROGRESS — Phase 5 (Implement)**. Concept prototype `break-part-feel`, HTML path.
- **Hypothesis**: 玩家集中火力擊破巨獸部位、得到強烈視聽回饋後，會*主動*去破第二、第三個部位（而非直接殺核心衝過關），2 分鐘內至少嘗試破 2 個。
- **Riskiest assumption (tested first)**: 破壞那一下的「爽度」本身（juice）。
- **Path**: HTML（單一 prototype.html，滑鼠跟隨＋自動射擊；瀏覽器能還原破壞 juice；閃避手感刻意不在此驗證範圍）。
- **Scope**: 1 巨獸（1 核心可直接擊殺＝贏 ＋ 3 可選破壞部位）；破部位＝全力 juice（hitstop／震動／碎屑／爆音／素材彈出）；簡單彈幕當情境；右上材料計數器。
- **Cut**: 武器池、升級經濟、多關卡、四階難度、觸控、選單／GameOver／音樂、真實像素美術。
- **File**: `prototypes/break-part-feel-concept/prototype.html`
- **Next after build**: Phase 6 playtest debrief（Phase 8 CD-PLAYTEST 因 lean 模式略過）。

## Project Snapshot
- **Game**: 殲獸戰機 / KAIJU BREAKER — 科幻縱向彈幕射擊 ＋ 破壞部位狩獵養成
- **Stage**: Concept (`production/stage.txt`)
- **Review mode**: lean (`production/review-mode.txt`)
- **Engine**: Unity 6.3 LTS — configured (C#, URP 2D + Pixel Perfect, Box2D 2D physics)
- **Platform**: 多平台（PC + 手機）
- **Visual direction**: 像素街機懷舊 (Retro Pixel Arcade)

## Progress Checklist
- [x] /start onboarding (stage=Concept, review=lean)
- [x] Obsidian live-sync via directory junctions (`C:\我的筆記\飛機彈幕遊戲\`)
- [x] /brainstorm Phases 1–6 complete
- [x] Game concept written → `design/gdd/game-concept.md`
- [x] `/setup-engine` (Unity 6.3 LTS) — CLAUDE.md + technical-preferences.md + reference docs updated, 5 agents version-aware
- [ ] `/art-bible` (seed = Visual Identity Anchor in concept doc)
- [ ] `/prototype` — validate break-part feel + touch controls + weapon balance (recommended next, flagged open questions)
- [ ] `/map-systems`

## Key Decisions
- 5 pillars locked: 科技對巨獸 / 頭目是靈魂 / 橫向選擇 / 破壞即獎勵 / 難度是門不是牆
- Meta-structure: 街機 ＋ 永久養成（素材 → 永久升級 8 武器）
- Weapons: 2 separate pools (主=雷射系×4, 副=飛彈系×4), field-drop acquisition, equal-power sidegrades
- Difficulty: 4 tiers scaling bullet density
- Random stages: embraced (lightweight — handcrafted segment pool, not full procgen)

## Open Questions (for /prototype)
- 武器先做 4 種還是直接 8 種驗證？
- 觸控彈幕操作手感？
- 破部位視聽回饋要多強才「爽」？

## Biggest Risk
武器平衡（等價而異性格）— 全專案最硬的設計題，繫於〔橫向選擇〕支柱。
