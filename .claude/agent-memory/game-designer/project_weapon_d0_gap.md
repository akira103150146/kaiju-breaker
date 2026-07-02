---
name: project-weapon-d0-gap
description: weapon-system.md's D0 equal-power constraint is unimplementable as-is — analysis doc + fix proposals live in design/balance/
metadata:
  type: project
---

`design/gdd/weapon-system.md` (LOCKED) §D.1 claims all 8 weapons hit
Sustained_Output = D0 ±10%, but computing this from the GDD's own §C.4/C.5/D.2/D.3
values shows only 2 of 8 weapons (L1 full-spread, L4 single-part) actually land in
band — and those two are the weapons whose numbers define the `HuPerD0=25`
conversion constant itself, so it's circular. The other 6 (L2, L3 both modes, M1,
M2, M3, M4) are out of band, from -78% (M2) to +50% (L2).

Root cause: `Assets/_Project/Scripts/Content/WeaponDef.cs` has no per-weapon
`EffectiveHitRate` field and no `ShotInterval`/cadence field for M1/M2/M4 (only
L4 has `L4FireInterval`). The GDD's D.1 promises a "hit-rate correction" but never
gives numbers, and the SO data model has nowhere to put them.

Full breakdown, proposed `EffectiveHitRate` values (L2=0.65, M3=0.80), and
base-retune proposals for the structurally-low weapons (M1/M4 need a
`ShotInterval` field before anything else can be decided; M2 needs a bigger
structural fix — a `m2_dmg_per_missile_mult` field doesn't even exist yet) are
written up in `design/balance/weapon-d0-equal-power-analysis.md` (status: draft,
awaiting director review as of 2026-07-02).

**Why:** H.1/H.2 automated equal-power tests (`tests/unit/weapon/...` per
weapon-system.md D.5/H.1/H.2) cannot be written until the director picks values
from that doc's §3 (correction factors) and §4 (new SO fields).

**How to apply:** Before writing weapon balance tests or touching `WeaponDef.cs`/
`WeaponBalanceConfig.cs` for equal-power purposes, check whether
`weapon-d0-equal-power-analysis.md` has been reviewed/updated — its open
questions (§6) are unresolved director decisions, especially: M1/M4 real
ShotInterval value, which L3 mode (tap vs charge) is the equal-power baseline,
and the M2 fix path (Option A: 4x per-missile mult, changes weapon identity, vs
Option B: structural multi-salvo redesign).
