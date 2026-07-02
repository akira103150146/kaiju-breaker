---
name: hit-feel-tiering-design-approach
description: Design approach and key findings behind design/gdd/hit-feel-tiering.md — how it layers on the LOCKED game-feel.md and reconciles with the sibling enemy-tier-system.md.
metadata:
  type: project
---

`design/gdd/hit-feel-tiering.md` (2026-07-03, v1.1.0) implements feedback point 6 (hit-feel
scaling by enemy tier). It was written in the same session as the sibling spec
`design/gdd/enemy-tier-system.md` (point 7) — both are single-shot subagent
invocations with no interactive back-and-forth, so **check whether the director has
since responded to either doc's Open Questions before treating any tier decision
as final.**

**Canonical tier taxonomy — use `enemy-tier-system.md`'s `EnemyTier` enum, not an
invented one**: `Trash` / `Elite` / `Mid` / `Boss`. An earlier draft of
hit-feel-tiering.md used ad-hoc labels (`NORMAL`/`ELITE`/`MID`/`FINAL_BOSS`) and
wrongly assumed the existing 3 kaiju (Carapex/Lacera/Voltwyrm) were "Mid" tier —
this was corrected once `enemy-tier-system.md` was read: those 3 kaiju default to
**`Boss`** tier (`enemy-tier-system.md` C.5, explicit "keep existing behaviour
unchanged"). `Mid` is reserved for brand-new, not-yet-built 1–2 part mini-encounters.
If touching either doc again, grep for `NORMAL`/`FINAL_BOSS` first — any hit means a
stale reference survived a reconciliation pass.

**Key structural facts driving the design**:
- Trash mobs (`EnemyDef`) had ZERO death-feel spec — no VFX/SFX/shake/hitstop existed
  anywhere for mob kills. This was the actual gap feedback point 6 pointed at.
- Every existing kaiju already gets identical `on_boss_core_break` treatment — no
  code-level distinction between "early boss" and "final boss" exists.
- **`Mid` tier CANNOT use `on_boss_core_break`** — `enemy-tier-system.md` C.4 forbids
  `PartType.BossCore` on Mid `KaijuDef`s (breaking it is the ONLY global run-victory
  signal; a mid-stage mini-boss must not accidentally end the run). So hit-feel-tiering.md
  had to invent a NEW event, `on_mid_encounter_cleared` (Stage-System-owned, fires only
  after all a Mid kaiju's parts are BROKEN), with its own lightweight feel profile
  (shake ~15px + a second flash pulse + bonus particles, deliberately NO hitstop/slowmo
  since the triggering part's own PartBreak already gave those — sequenced to fire only
  AFTER that part's hitstop timer ends, not simultaneously).
- `Boss` tier (both current 3 kaiju AND any future final boss) is left **completely
  unchanged** from `game-feel.md`'s existing numbers — no bonus multiplier. The
  original idea of a `FINAL_BOSS` sub-tier with a hitstop/slowmo/particle bonus
  (clamped inside game-feel.md's existing safe ranges) was dropped because
  `enemy-tier-system.md`'s enum has no such sub-distinction; it's now Open Question #1
  in hit-feel-tiering.md (would need a new `KaijuDef.IsRunFinale`-style flag, owned by
  enemy-tier-system.md, not something this doc can add unilaterally).
- Juice-scarcity ladder still holds: Trash(0 juice) < Elite(mild) < Mid ordinary parts
  (= full PartBreak, same as Boss's ordinary parts) < Mid-cleared bonus (shake+flash+
  sfx, no hitstop stacking) < Boss Core Break (full treatment, only true climax gets
  slow-mo).

**How to apply**: if `enemy-tier-system.md`'s Open Questions (e.g. #1 `PartType.MidCore`,
#4 Mid/Boss part-count boundary) get resolved by the director, re-check whether
hit-feel-tiering.md's C.1 table and D.7 formula still hold. The two docs cross-reference
each other bidirectionally (enemy-tier-system.md F.6 points back at hit-feel-tiering.md).
