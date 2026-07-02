---
name: project-weapon-d0-tiering
description: Status of the weapon-system D0 equal-power + tiering spec chain (analysis doc -> this new spec -> pending SO/test implementation)
metadata:
  type: project
---

Kaiju Breaker's `weapon-system.md` (LOCKED) asserts an equal-power constraint
(Sustained_Output = D0 +/-10%) that was never actually verifiable — most
weapons were missing `EffectiveHitRate` / `ShotInterval` fields entirely.

Two documents now form the review chain, both **Draft for Director Review,
not yet approved** as of 2026-07-03:
1. `design/balance/weapon-d0-equal-power-analysis.md` — gap analysis, computes
   raw Sustained_Output for all 8 weapons from GDD numbers, proposes
   correction-factor defaults + a data-model gap table (§4).
2. `design/gdd/weapon-tiering-and-equal-power.md` — extends (1) with two
   director-approved 2026-07-03 decisions: M2 Swarm Launcher gets an
   "Option B" redesign (burst-salvo becomes base-tier behavior, not just
   Tier-3 — "Chain Hive": 3 salvos x 8 missiles, tier-universal, Tier-3 only
   changes *targeting*, not raw numbers) and L1 Spread Laser beam count is
   bound to a continuous Tier 0-3 ladder (2/3/4/5 beams; total heat rate
   stays constant across beams — the existing code's `perBeamDelta = rate/
   beamCount` already makes this equal-power-safe with no extra work).

**Non-obvious finding from writing doc (2)**: sweeping all 8 weapons' Tier-3
mechanics through the equal-power formula surfaced two *new* problems the
analysis doc didn't catch: L3 Wave Cannon's "共鳴擴散" (resonance heat
inject) Tier-3 mechanic overshoots by +80% once quantified in D0-equivalent
units (needs director pick of 3 remediation options — recommended: treat as
a situational burst value like M3's heat-shock detonation, not a sustained
value); M4 Cluster Bomb's Tier-3 star-split overshoots by +20% (recommended
fix: tighten `m4_t3_child_dmg_pct` 0.20 -> 0.18, existing safe range covers
it). Both are open questions in doc (2) §I, not yet resolved.

**Why this matters**: the director reviews both docs together tomorrow
(2026-07-04 target). Neither doc's numbers should be treated as final —
they're placeholder defaults with safe ranges, formula-correctness is the
priority per repo convention (data-driven SO fields, ADR-0003). Next step
after approval: implement the SO fields + write
`weapon_dps_equivalence_test` / `weapon_loadout_matrix_test` /
`tier3_identity_depth_test` in `Assets/_Project/Tests/EditMode/`.

See also [[feedback-balance-doc-single-pass]].
