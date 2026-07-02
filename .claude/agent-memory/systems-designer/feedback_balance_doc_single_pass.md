---
name: feedback-balance-doc-single-pass
description: For "Draft for Director Review" balance/tiering analysis docs in kaiju-breaker, write the full doc in one pass rather than per-section approval
metadata:
  type: feedback
---

When a task explicitly frames the deliverable as a "Draft for Director
Review" balance/formula spec (not a fresh player-facing GDD), and the
director's key creative decisions are already stated up front in the task
brief, write the complete document in a single pass rather than following
the incremental skeleton-then-section-by-section-approval flow described in
`.claude/docs/context-management.md` / `design/CLAUDE.md`.

**Why**: this matches the established precedent set by
`design/balance/weapon-d0-equal-power-analysis.md` (written single-pass by
the game-designer agent) which `design/gdd/weapon-tiering-and-equal-power.md`
directly extends. These docs are themselves pre-approval review artifacts —
the director bulk-reviews them and either approves or sends back specific
open questions (tracked in each doc's own "Open Questions" section), so
per-section chat approval would just duplicate that review step. The doc
must still include the mandatory 8 GDD sections and every ambiguity /
judgment call must be surfaced explicitly as a tagged, numbered open
question for the director rather than silently decided.

**How to apply**: still holds even under the general collaboration protocol
requiring "May I write this to [filepath]?" — for this specific document
type (balance/tiering spec continuing an existing analysis-doc chain), skip
straight to a complete draft and return it for review, because that's the
pattern the human director has already established for this doc pair.
Doesn't necessarily generalize to brand-new player-facing mechanic GDDs
being authored from scratch with no prior analysis doc — those should still
follow the incremental question-first flow.
