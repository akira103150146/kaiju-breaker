---
name: kaiju-breaker-feedback-backlog
description: Kaiju-Breaker's 7-point director feedback backlog (2026-07-02) and which points systems-designer has converted to formal GDDs.
metadata:
  type: project
---

The director gave a 7-point gameplay-feedback backlog on 2026-07-02, logged at
`design/feedback/2026-07-02-改進意見與劇情草案.md` §A. Each point gets converted to a
formal spec once direction is confirmed (backlog status per that file's §C):

1. Bullet-pattern + movement diversity — gated on ADR-0001 phone perf spike (parked).
2. Charge-shot direction (horizontal → vertical) — small, `/quick-design` scope.
3. Bullet size tiers + weapon upgrade ladder (shotgun 2→3→4→5 pellets) — absorbs the
   weapon equal-power retune (`design/balance/weapon-d0-equal-power-analysis.md`).
4. Scrolling background / parallax (forward-progress illusion).
5. Story + level draft — direction greenlit for expansion (world: 方舟/Ark evacuation,
   cute-sci-fi protagonist, Stellar Blade-inspired desolate aesthetic, 5 regions + final boss).
6. **Hit-feel tiering by enemy class — DONE.** Spec: `design/gdd/hit-feel-tiering.md`
   (registered in `design/systems-index.md` as S8b, additive layer on top of the
   LOCKED `design/gdd/game-feel.md`). See [[hit-feel-tiering-design-approach]].
7. Enemy tier system (Trash/Elite/Mid/Boss × HP/armor/mechanics) — **DONE** (written
   concurrently with point 6, same 2026-07-03 session). Spec: `design/gdd/enemy-tier-system.md`.
   Defines the canonical `EnemyTier` enum that hit-feel-tiering.md consumes (does not own).
   Neither doc has director sign-off yet — both flag multiple Open Questions (§I in each).

**Why this matters**: when asked to pick up the next feedback point, check this list
and `design/feedback/2026-07-02-改進意見與劇情草案.md` §C for routing (`/design-system` for
big items, `/quick-design` for small, `/balance-check` for anything touching numbers).

**How to apply**: before starting point 7 (enemy-tier-system.md) or any GDD that
references enemy tiers, re-check whether it now exists and reconcile any tier-taxonomy
assumptions hit-feel-tiering.md made in its §C.1 (it assumed NORMAL/ELITE/MID/FINAL_BOSS
as placeholder labels, explicitly deferring ownership of the exact ID→tier mapping).
