using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 002 H.7 — Tier-3 deepens identity, it does not just add power: for every weapon,
    /// <c>Ttb(tier3) &gt;= Ttb(tier1) * 0.85</c> on a Normal part (Tier-3 may shrink TTB by at most
    /// 15%, extending weapon-system.md H.7 with the tiering/equal-power growth added by
    /// design/gdd/weapon-tiering-and-equal-power.md D.6). Test data mirrors the D.6 "health-check"
    /// table 1:1 — each assertion below cites the D.6 row it verifies.
    ///
    /// L3's Tier-3 "resonance diffusion" heat-inject is EXCLUDED from this comparison per the
    /// director's I#1 "(c) situational burst value" ruling (weapon-tiering-and-equal-power.md
    /// D.6/I#1) — <see cref="WeaponBalanceModel"/> never models that value, so L3's Ttb below is
    /// the Charge-basis figure only, which is tier-invariant by construction (trivial pass).
    ///
    /// M2 (Chain Hive: identical magazine/output at every tier, D.5) and L1 (total heat is
    /// tier-invariant by the C.3 beam-split invariant) also pass trivially — see their test bodies
    /// for why, per the task brief.
    /// </summary>
    public sealed class WeaponsTier3IdentityDepthTests
    {
        private const int BaseTier = 1;   // "Tier-1 baseline" per weapon-system.md H.7 wording.
        private const int Tier3 = 3;
        private const float MinRatio = 0.85f; // WeaponBalanceConfig-independent gate constant per H.7's own "<=15%" wording, not a magic balance number.

        // ── Fixtures — same spec-default knob values as the H.1/H.2 suites ───────────────────────

        private static WeaponBalanceConfig Balance() => ContentTestFactory.Create<WeaponBalanceConfig>(
            ("_d0Reference", 100f), ("_buPerD0", 10f), ("_huPerD0", 25f),
            ("_hDecayRate", 3f), ("_thetaS", 100f),
            ("_requiredBreakThresholdNormal", 100f), ("_requiredBreakThresholdArmored", 150f),
            ("_requiredBreakThresholdBossCore", 200f),
            ("_equalPowerBandTolerance", 0.10f));

        private static WeaponDef L1Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.L1), ("_type", WeaponType.Laser),
            ("_l1HRateFull", 25f), ("_l1BaseBeamCount", 2), ("_effectiveHitRate", 1.0f));

        private static WeaponDef L2Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.L2), ("_type", WeaponType.Laser),
            ("_l2HRate", 37.5f), ("_effectiveHitRate", 0.65f));

        private static WeaponDef L3Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.L3), ("_type", WeaponType.Laser),
            ("_l3TapOutputMult", 0.60f),
            ("_l3ChargeTime", 1.2f), ("_l3ChargeOutputMult", 2.50f), ("_l3ChargeCooldown", 1.5f),
            ("_effectiveHitRate", 1.0f));

        private static WeaponDef L4Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.L4), ("_type", WeaponType.Laser),
            ("_l4HRate", 25f), ("_effectiveHitRate", 1.0f));

        private static WeaponDef M1Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M1), ("_type", WeaponType.Missile),
            ("_m1MissilesPerShot", 2), ("_m1DmgPerMissileMult", 0.50f),
            ("_m1MagSize", 6), ("_m1ReloadTime", 3.0f), ("_m1T3MissilesPerShot", 3),
            ("_m1ShotInterval", 0.10f), ("_effectiveHitRate", 1.0f));

        private static WeaponDef M2Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M2), ("_type", WeaponType.Missile),
            ("_m2MicroCount", 8), ("_m2SalvoCount", 3), ("_m2DmgPerMissileMult", 0.25f),
            ("_m2InterSalvoInterval", 0.4f), ("_m2ReloadTime", 5.0f), ("_effectiveHitRate", 1.0f));

        private static WeaponDef M3Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M3), ("_type", WeaponType.Missile),
            ("_m3DmgUnsoftenedMult", 3.0f), ("_m3MagSize", 3), ("_m3ReloadTime", 4.0f),
            ("_m3ShotInterval", 1.0f), ("_effectiveHitRate", 0.80f));

        private static WeaponDef M4Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M4), ("_type", WeaponType.Missile),
            ("_m4TotalOutputCapMult", 1.0f), ("_m4MagSize", 4), ("_m4ReloadTime", 3.5f),
            ("_m4ShotInterval", 0.20f), ("_m4T3ChildCount", 6),
            ("_m4T3ChildDmgPct", 0.18f), ("_effectiveHitRate", 1.0f));

        // ── Shared assertion ──────────────────────────────────────────────────────

        private static void AssertTier3DoesNotShrinkTtbBeyond15Percent(WeaponDef weapon, string d6RowNote)
        {
            // Arrange
            var balance = Balance();

            // Act
            float ttbBase = WeaponBalanceModel.Ttb(weapon, balance, PartType.Normal, BaseTier);
            float ttb3 = WeaponBalanceModel.Ttb(weapon, balance, PartType.Normal, Tier3);

            // Assert
            Assert.That(ttb3, Is.GreaterThanOrEqualTo(ttbBase * MinRatio),
                $"{weapon.Id}: Tier-3 TTB ({ttb3:F3}s) shrank more than 15% below Tier-1 TTB " +
                $"({ttbBase:F3}s, floor {ttbBase * MinRatio:F3}s) — {d6RowNote}");
        }

        // ── D.6 table, one test per weapon ────────────────────────────────────────

        [Test]
        public void test_l1_tier3_residual_flame_does_not_shrink_ttb()
        {
            // D.6: residual flame only fires after the laser leaves the part — 0% steady-state
            // contribution; total heat is tier-invariant by the C.3 beam-split rule, so T_soften
            // (and therefore Ttb) is IDENTICAL at every tier — trivially passes.
            AssertTier3DoesNotShrinkTtbBeyond15Percent(L1Def(), "D.6: residual flame, 0% steady-state contribution");
        }

        [Test]
        public void test_l2_tier3_autotrack_and_ripple_do_not_shrink_ttb()
        {
            // D.6: micro-track aids hit-rate (not modelled as a rate change here) and the heat
            // ripple is a one-time PartBroke event, not steady-state — Ttb is tier-invariant.
            AssertTier3DoesNotShrinkTtbBeyond15Percent(L2Def(), "D.6: autotrack/ripple, 0% steady-state contribution");
        }

        [Test]
        public void test_l3_tier3_resonance_diffusion_excluded_charge_basis_does_not_shrink_ttb()
        {
            // D.6/I#1: resonance diffusion's heat-inject is a burst value, excluded from this
            // model entirely (director ruling) — only the Charge basis is compared, which is
            // tier-invariant by construction.
            AssertTier3DoesNotShrinkTtbBeyond15Percent(L3Def(), "D.6/I#1: heat-inject excluded as a burst value");
        }

        [Test]
        public void test_l4_tier3_afterimage_does_not_shrink_ttb()
        {
            // D.6: afterimage only fires after the laser leaves the part, same as L1 residual — 0%.
            AssertTier3DoesNotShrinkTtbBeyond15Percent(L4Def(), "D.6: afterimage, 0% steady-state contribution");
        }

        [Test]
        public void test_m1_tier3_heat_seeking_third_missile_stays_within_15_percent()
        {
            // D.6: magazine total is tier-invariant (6 missiles); Tier-3 only redistributes into
            // fewer, bigger shots, so Total_Output_per_Mag is conserved and Mag_Duration shrinks
            // slightly (~+3.2% Sustained_Output) — comfortably inside the 15% floor.
            AssertTier3DoesNotShrinkTtbBeyond15Percent(M1Def(), "D.6: +3.2% Sustained_Output drift (mag total conserved)");
        }

        [Test]
        public void test_m2_tier3_saturation_callout_ttb_is_identical_to_base()
        {
            // D.5/D.6: Chain Hive Tier-3 "saturation callout" changes TARGETING only — SalvoCount /
            // MicroCount / DmgPerMissileMult / InterSalvoInterval / ReloadTime are IDENTICAL at
            // every tier by design (Option B), so Ttb(tier3) == Ttb(base) exactly — trivial pass.
            var balance = Balance();
            var m2 = M2Def();

            float ttbBase = WeaponBalanceModel.Ttb(m2, balance, PartType.Normal, BaseTier);
            float ttb3 = WeaponBalanceModel.Ttb(m2, balance, PartType.Normal, Tier3);

            Assert.That(ttb3, Is.EqualTo(ttbBase).Within(1e-6f),
                "Chain Hive is tier-invariant by construction — the equal-power design guarantee itself");
        }

        [Test]
        public void test_m3_tier3_ap_chain_does_not_shrink_ttb()
        {
            // D.6: the AP chain only fires on a PartBroke event (a one-time, non-steady-state
            // trigger owned by KaijuParts, per weapons_missile_tier3_test.cs's negative assertion)
            // — the base torpedo cadence used for Ttb is unaffected by tier.
            AssertTier3DoesNotShrinkTtbBeyond15Percent(M3Def(), "D.6: AP chain is a one-time break-event trigger, 0% steady-state");
        }

        [Test]
        public void test_m4_tier3_cluster_split_stays_within_15_percent_after_g6_retune()
        {
            // D.6: BEFORE the G.6 retune, the Tier-3 child split overshoots to +20% (fails this
            // gate); AFTER retuning m4_t3_child_dmg_pct 0.20 -> 0.18 it lands at +8.1%, comfortably
            // inside the 15% floor. This fixture uses the retuned value.
            AssertTier3DoesNotShrinkTtbBeyond15Percent(M4Def(), "D.6: +8.1% Sustained_Output drift after G.6 retune (0.20 -> 0.18)");
        }
    }
}
