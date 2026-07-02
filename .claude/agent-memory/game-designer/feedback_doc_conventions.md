---
name: feedback-doc-conventions
description: kaiju-breaker design docs (GDDs, balance analyses) are written in Traditional Chinese with English technical terms/identifiers inline
metadata:
  type: feedback
---

`design/gdd/*.md` (e.g. weapon-system.md) and director-facing analysis docs are
written in Traditional Chinese prose, with English used for: section header
English glosses in parentheses, formula variable names, code/field identifiers
(e.g. `EffectiveHitRate`, `B_unsoftened_mult`), and file paths. Tables mix both
freely (Chinese labels, English data/identifiers).

**Why:** Confirmed by reading the existing LOCKED weapon-system.md and the
2026-07-02 feedback doc in design/feedback/ — both are full Traditional Chinese
with embedded English technical vocabulary. No explicit instruction was given
for this task's balance-analysis doc, but matching the established convention
was the safer default for a director-facing artifact in the same design/ tree.

**How to apply:** Default to this bilingual convention for any new file under
`design/` (GDDs, balance docs, quick-specs) unless the user explicitly asks for
English-only. Keep formulas, SO field names, and file paths in English regardless
of prose language — matches [[project-weapon-d0-gap]] doc's formatting too.
