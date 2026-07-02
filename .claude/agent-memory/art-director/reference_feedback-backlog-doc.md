---
name: feedback-backlog-doc
description: Location and status conventions for the design feedback/backlog folder that seeds new design/art work items.
metadata:
  type: reference
---

`design/feedback/` holds dated backlog docs (e.g.
`2026-07-02-改進意見與劇情草案.md`) capturing the user's raw verbal/written design
notes before they become formal specs. These docs explicitly mark themselves
`待確認 / 待轉成正式 GDD` and list a "下一步" section describing which skill
(`/design-system`, `/quick-design`, `/brainstorm`, `/team-narrative`) should be
used to formalize each item once direction is confirmed.

**How to apply:** When asked to spec out a numbered point from one of these
docs, treat the doc's own item as the seed but do NOT treat adjacent unconfirmed
items (e.g. the story/zone draft in §B of the 2026-07-02 doc) as settled canon —
cross-check against the actual confirmed GDDs (`design/gdd/*.md`) first. See
[[five-zone-vs-three-stage-canon]] for a concrete instance of this tension.
