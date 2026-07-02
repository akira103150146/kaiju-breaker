---
name: project-enemy-tier-system
description: Enemy Tier System GDD (design/gdd/enemy-tier-system.md) authored 2026-07-03 from feedback backlog §A.7 — taxonomy, data model split, and key open decisions
metadata:
  type: project
---

`design/gdd/enemy-tier-system.md` (v1.0.0, Draft) defines four enemy tiers — Trash /
Elite / Mid / Boss — answering feedback backlog item §A.7 (mechanics should not be
BOSS-only).

**Why**: Director feedback captured in `design/feedback/2026-07-02-改進意見與劇情草案.md`
§A.7 explicitly asked for per-tier HP/armor differentiation and for mobs/elites to
carry small mechanics (not just bosses), tying into the existing kaiju-parts break
system where sensible.

**Key structural decisions made (director has not yet confirmed — see doc §I):**
- Trash/Elite reuse `EnemyDef` (existing SO) + a NEW lightweight single-scalar "Gate"
  state machine (`MechanicPatternSO`, parallel to existing `MovementPatternSO`/
  `EmitterPatternSO`) — NOT the full kaiju-parts HU/BU dual-track (too heavy for
  mobs that die in seconds). `shield_flier`'s existing 3-hit frontal shield was
  retroactively formalized as the `DirectionalShield` Gate type precedent.
- Mid/Boss reuse `KaijuDef`/`PartDef` (existing, unmodified formulas) — differentiated
  by a NEW `KaijuDef.Tier` field, NOT by part count. The authoritative Mid-vs-Boss
  rule is whether a part is wired to `PartType.BossCore` → `on_boss_core_break`
  (global run-victory event). Mid tier is explicitly FORBIDDEN from using
  `PartType.BossCore` to avoid a mid-stage mini-boss accidentally ending the whole
  run — flagged as a blocking regression-test requirement (doc §H.6).
- Deliberately did NOT extend `PartType` enum (e.g. no `PartType.MidCore` added) —
  that's a cross-system contract change owned by `kaiju-part-system.md`, out of this
  agent's unilateral authority. Flagged as open question §I.1 for technical-director
  sign-off if a "mid-core" semantic is later needed.
- Tier stats (HP, Gate HP, part count) are difficulty-invariant by design, mirroring
  `kaiju-part-system.md` C.8 / `difficulty-system.md` C.3. Bullet density composes
  three independent multiplicative layers: base_bullets × TierDensityMult(tier) ×
  bullet_density_mult(difficulty) — this extends `difficulty-system.md` D.2, which
  did not previously mention the tier/elite factor explicitly (flagged as a doc-sync
  gap in §F.3).

**How to apply**: Before extending or implementing this system, read doc §I (5 open
questions) first — none of the structural decisions above are director-approved yet,
they were made autonomously (single-shot subagent invocation, no interactive
question loop available) and explicitly surfaced as open questions per the doc's
own instructions. If picking this back up, check whether the director has since
responded to `design/feedback/2026-07-02-改進意見與劇情草案.md` before treating any
Tier taxonomy decision as final.
