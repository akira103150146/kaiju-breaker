# Story 005: Anti-Degenerate Loadout Guard (TTB Matrix Assertion)

> **Epic**: 素材經濟與永久升級
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M (3–4 h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/material-economy.md`
**Requirement**: `TR-economy-003`, `TR-economy-006`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003 (primary): `WeaponDef` Tier effects and `EconomyConfig.MaxTtbImprovementPct` as SO-injected test inputs; ADR-0002 (secondary): `IWeaponTierQuery` to confirm Tier 3 is the state under test
**ADR Decision Summary**: The guard is an automated EditMode test suite — not runtime enforcement — that consumes `WeaponDef` SO fixture data (Tier 1/2/3 effect modifiers per weapon) as inputs and asserts two invariants: (1) no weapon's TTB improvement Tier 0→3 exceeds `max_ttb_improvement_pct` (default 0.15); (2) no single loadout dominates all part type scenarios after maxing progression. The test shares the 8×8 TTB matrix fixture with `weapon_loadout_matrix_test` from `weapon-system.md` H.7. Changing `WeaponDef` Tier effects reruns the test in CI — if constraints are violated, balancers adjust `WeaponDef`, not the test thresholds.

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: Pure C# math assertions in an EditMode test suite. `WeaponDef` and `EconomyConfig` fixtures injected as SO assets or in-memory objects. No Unity engine runtime APIs required during test execution — deterministic, isolated, no I/O.

**Control Manifest Rules (Core layer)**:
- Required: `max_ttb_improvement_pct` MUST come from `EconomyConfig` SO (default 0.15, safe range 0.10–0.20) — zero hard-coded threshold in test code (control-manifest §1.2, GDD §G.3).
- Required: The guard MUST use `WeaponDef` Tier effect SO fixtures as test inputs — changing SO values re-runs CI and may fail/pass the guard without code changes (ADR-0003 §4).
- Required: Guard MUST cover all 8 weapons × all 4 Tiers × all 3 part types (NORMAL / ARMORED / BOSS_CORE) — minimum 24 per-weapon TTB assertions, plus the 64-entry matrix spread assertion, plus the no-dominance assertion (GDD §H.3).
- Required: Economy READS `break_quality` from events and MUST NOT recompute it — this story verifies the downstream effect of Tier upgrades, not the yield computation path (control-manifest §4.2).
- Forbidden: MUST NOT hard-code any TTB value or threshold in the test — all sourced from SO fixtures (ADR-0003, control-manifest §1.2, §5).
- Guardrail: This test file is BLOCKING for Full Vision milestone gate per GDD §H.3. If CI fails on this test, balancers adjust `WeaponDef` Tier effects — do not adjust the assertion thresholds to make the test pass.

---

## Acceptance Criteria

*From GDD `design/gdd/material-economy.md` §D.4 (TTB ceiling formula), §H.3 (no-dominant-loadout assertion), §H.6 (full-Tier-3 playtest context), §C.3 (TTB improvement caps per tier):*

- [ ] For every weapon W and every part type P: `TTB_tier3(W, P) >= TTB_tier0(W, P) × (1 - max_ttb_improvement_pct)` — Tier 3 TTB improvement ≤ 15% over Tier 0 baseline (GDD §D.4).
- [ ] Tier 0→2 intermediate check: cumulative TTB improvement ≤ 10% for each weapon across all part types (GDD §C.3 Tier 1 and Tier 2 combined cap).
- [ ] Full 8×8 loadout matrix at Tier 3 (8 weapons × contexts per `weapon-system.md` §H.2): `max(TTB_tier3_matrix) / min(TTB_tier3_matrix) <= 2.0` (GDD §H.3).
- [ ] No-dominance assertion: no single loadout ranks in the top 3 TTB across ALL 3 part types simultaneously (NORMAL + ARMORED + BOSS_CORE) — confirms final progression does not produce a dominant loadout (GDD §H.3, §H.6).
- [ ] All assertions use `WeaponDef` SO fixture data as input — no TTB values or improvement percentages embedded in test code.
- [ ] `max_ttb_improvement_pct` used in per-weapon cap assertion sourced from `EconomyConfig` fixture (confirms config-driven threshold).
- [ ] Test file `Assets/_Project/Tests/Economy/economy_dominant_loadout_guard_test.cs` is part of the EditMode CI suite and passes.

---

## Implementation Notes

*Derived from ADR-0003 §4 (SO as test input, fixture injection) and GDD §D.4 (TTB ceiling verification formula):*

Create EditMode test class `EconomyDominantLoadoutGuardTest` in `Assets/_Project/Tests/Economy/`. Import the same `WeaponDef[]` fixture assets used by `weapon_loadout_matrix_test` in the `Weapons.Tests` project.

Test structure:
1. Load fixture `WeaponDef[]` (8 weapons) and `EconomyConfig` fixture (for `MaxTtbImprovementPct`).
2. For each weapon W, Tier T in {0, 1, 2, 3}, and part type P in {NORMAL, ARMORED, BOSS_CORE}: compute `TTB(W, P, T)` by applying the Tier T effect modifiers from `WeaponDef` to the Tier 0 baseline.
3. **Per-weapon Tier 0→3 cap assertion**: `Assert.That(TTB(W, P, Tier3) / TTB(W, P, Tier0), Is.GreaterThanOrEqualTo(1f - config.MaxTtbImprovementPct))` for all 8 × 3 = 24 combinations.
4. **Per-weapon Tier 0→2 intermediate cap**: same assertion with `Tier2` and threshold `0.10` (from `WeaponDef` or a constant field in `EconomyConfig` — confirm field name before implementing).
5. **Matrix spread assertion**: build 64-entry matrix of `TTB(W, context, Tier3)` for all loadout/context combinations; `Assert.That(matrixMax / matrixMin, Is.LessThanOrEqualTo(2.0f))`.
6. **No-dominance assertion**: for each weapon W, count how many part types it ranks in the top 3 fastest TTB; `Assert.That(countAllThreePartTypes, Is.LessThan(3))` — no weapon should rank top-3 in all 3 simultaneously.

Extract the TTB calculation function as a pure static helper shared with `weapon_loadout_matrix_test` to avoid duplication. If the shared helper does not yet exist in a `Core`-accessible test utility class, create it as part of this story.

This story implements a CI gate, not runtime behaviour. Balancers adjust `WeaponDef` SO assets when the gate fails — they do not adjust the assertion code.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: Upgrade transaction mechanics — must be complete so Tier effects are stable before this guard is meaningful.
- Weapons epic (`weapon_loadout_matrix_test`): The 8×8 matrix test from the Weapon system side — this story's guard cross-references the same fixture but focuses on the Economy's Tier-effect contribution to the constraint.
- H.6 playtest validation: Designer trial with full Tier-3 loadout is an advisory playtest check coordinated by QA lead separately from this automated guard.
- Runtime enforcement: No in-game dominance warning or UI alert — this is a design-time CI gate only.

---

## QA Test Cases

*Written by qa-lead at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Per-weapon Tier 0→3 TTB improvement ≤ 15% for all 8 weapons and all 3 part types
  - Given: `WeaponDef` fixtures for all 8 weapons with default Tier effects per GDD §C.3; `EconomyConfig` fixture with `MaxTtbImprovementPct=0.15`; part types NORMAL, ARMORED, BOSS_CORE
  - When: `AssertPerWeaponTTBCap()` runs for all 24 combinations
  - Then: All 24 assertions pass with the default GDD Tier effects
  - Edge cases: Fixture with exactly 15% improvement (boundary) → passes (boundary is inclusive); fixture with 15.1% improvement → test FAILS — designer must reduce the Tier effect value in `WeaponDef`

- **AC-2**: Per-weapon Tier 0→2 cumulative TTB improvement ≤ 10%
  - Given: Same `WeaponDef` fixtures; Tier 2 effect modifiers applied cumulatively (Tier 1 + Tier 2 stacked)
  - When: Intermediate cap assertion runs for all 8 × 3 combinations
  - Then: All 24 Tier-2 assertions pass; Tier 1 alone and Tier 1+2 combined do not exceed 10%
  - Edge cases: Tier 2 at exactly 10% → passes; 10.1% → test FAILS; verify that Tier 1 individual effect alone (per GDD §C.3 ≤10% for both Tier 1 and Tier 2 separately) is also checked

- **AC-3**: 8×8 matrix max/min spread ≤ 2.0× at Tier 3
  - Given: Full 64-entry TTB matrix at Tier 3 built from `WeaponDef` fixtures and all loadout/part-type contexts
  - When: Spread assertion runs
  - Then: `max(matrix) / min(matrix) <= 2.0`
  - Edge cases: Matrix entry with value ≤ 0 (invalid — no weapon should have zero or negative TTB) → test detects and fails with descriptive error before division; spread at exactly 2.0 → passes

- **AC-4**: No single weapon dominates all 3 part types in top-3 TTB at Tier 3
  - Given: TTB matrix at Tier 3; each weapon ranked by TTB per part type (NORMAL, ARMORED, BOSS_CORE) — lower TTB = faster = better rank
  - When: No-dominance assertion runs
  - Then: No weapon appears in top-3 fastest TTB for ALL three part types simultaneously; at least one part type exists where each weapon is NOT in top-3
  - Edge cases: Weapon ranking 3rd in only 2 of 3 part types → passes (dominance requires all 3); weapon ranking 1st in 2 and 3rd in 1 → FAILS (top-3 in all 3); weapon ranking 4th in any single part type → passes that part type check

- **AC-5**: All thresholds sourced from SO fixtures — no hard-coded values in test
  - Given: `EconomyConfig` fixture with `MaxTtbImprovementPct=0.12` (non-default); same `WeaponDef` fixtures as AC-1
  - When: Per-weapon cap assertion runs
  - Then: Test uses 0.12 as the cap threshold (not hard-coded 0.15); any weapon with 13% improvement (which would pass at 0.15) now FAILS — confirms threshold is fully config-driven

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Economy/economy_dominant_loadout_guard_test.cs` — must exist and all cases must pass
- This test is **BLOCKING for Full Vision milestone** per GDD §H.3. CI must report this test as a gate check.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 004 DONE (Tier effects are stable and reflected in `IWeaponTierQuery`; `WeaponDef` Tier effect fields populated for all 8 weapons); `EconomyConfig` has `MaxTtbImprovementPct` field; `weapon_loadout_matrix_test` fixture (or its TTB calculation helper) available for reuse from `Weapons.Tests`
- Unlocks: Full Vision milestone gate (H.3 automated assertion is BLOCKING); H.6 playtest verification step (advisory, coordinated by QA lead after this gate passes)
