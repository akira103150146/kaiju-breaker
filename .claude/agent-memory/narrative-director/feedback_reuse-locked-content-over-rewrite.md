---
name: feedback-reuse-locked-content-over-rewrite
description: When a new narrative direction conflicts with already-authored system/level content, wrap with narrative framing instead of discarding or forcing a rename
metadata:
  type: feedback
---

When a narrative brief (e.g. director-approved story direction) implies restructuring that
would appear to invalidate already-drafted system GDDs (e.g. `stage-system.md` locking in
3 stages/3 kaiju while a new story brief calls for 5 zones), do not silently discard the
existing content or force-fit new fictional dressing onto it if that dressing contradicts
already-written specifics (art direction, location names, mechanical themes already in the
doc).

**Approach that worked (self-applied 2026-07-03, not yet director-confirmed)**: treat the
existing 3 fully-designed kaiju/stages as reusable building blocks, insert new zones only
where the new brief's ideas don't already have a good match, and reorder/reframe existing
content (e.g. moving a boss from "final" to "zone 4" slot, reframing its lore without
touching its mechanics) rather than rewriting mechanical specs that belong to another
agent's domain (game-designer/level-designer/kaiju-content-designer own those; narrative
work should stay in the "wrapper" layer).

**Why**: (1) respects "No Unilateral Cross-Domain Changes" (coordination-rules.md) — I
don't have authority to redefine kaiju part tables or stage pool structures; (2) avoids
wasted work — those docs represent real design/authoring hours already spent; (3) keeps
contradictions-check honest — forcing new fiction onto locked visual/mechanical specifics
(e.g. renaming an already-described "lava rift" biome to "rusted city streets") creates
exactly the kind of lore contradiction the narrative-director role is supposed to catch,
not introduce.

**How to apply**: before expanding/restructuring a narrative doc that touches level/zone
counts, always read the relevant systems-index.md and the specific system GDDs first to
check what's already locked or drafted, then explicitly flag in the new doc where the
brief's proposed content maps cleanly onto existing work vs. where it genuinely requires
new content — and route the "new content" parts to the owning agent via a Cross-Domain
Dependencies section rather than speccing it yourself.

See also [[project-kaiju-breaker-narrative-structure]] for the concrete case this came
from (5-zone story restructure reusing CARAPEX/LACERA/VOLTWYRM).
