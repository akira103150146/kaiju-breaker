using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 002 H.1 — D₀ equal-power equivalence. All 8 weapons must sustain
    /// <c>Sustained_Output ∈ D0Reference × [1-EqualPowerBandTolerance, 1+EqualPowerBandTolerance]</c>
    /// at the shipped balance defaults (design/gdd/weapon-tiering-and-equal-power.md §D/§H.1,
    /// extending weapon-system.md H.1). Fixture values below are the spec's DEFAULT knob values
    /// (G.2-G.6) — where those differ from the WeaponDef/WeaponBalanceConfig SO literal defaults
    /// (M2's DmgPerMissileMult/InterSalvoInterval, L3's ChargeTime/ChargeCooldown,
    /// M4's T3ChildDmgPct — all still pending an SO-default sync per the doc's "re-tunable
    /// placeholder" note), they are set explicitly here rather than relied upon implicitly.
    ///
    /// Uses <see cref="WeaponBalanceModel"/> — the closed-form D.1-D.5 formula model — NOT the
    /// runtime FireFrame/TryFire behaviour objects (those are covered by weapons_*_family_test.cs /
    /// weapons_*_tier3_test.cs).
    /// </summary>
    public sealed class WeaponsDpsEquivalenceTests
    {
        // ── Fixtures — spec default knob values (weapon-tiering-and-equal-power.md G.2-G.6) ──────

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
            ("_m4T3ChildDmgPct", 0.18f), // G.6 retune: 0.20 -> 0.18 fixes the D.6 M4-T3 +20% overshoot
            ("_effectiveHitRate", 1.0f));

        // ── Band helper — Sustained_Output is expressed in ×D₀ units (D.1-D.5), so the reference
        // point is 1.0, not the literal D0Reference PU/s value (D0Reference cancels — see D.1). ──

        private static void AssertInBand(float sustainedOutput, WeaponBalanceConfig balance, string message)
        {
            float tol = balance.EqualPowerBandTolerance;
            Assert.That(sustainedOutput, Is.InRange(1f - tol, 1f + tol), message);
        }

        // ── L1 — beam ladder invariant (C.3) across all 4 tiers ───────────────────

        [Test]
        public void test_l1_sustained_output_is_tier_invariant_across_beam_ladder_and_in_band()
        {
            // Arrange
            var l1 = L1Def();
            var balance = Balance();
            var expectedBeamCounts = new[] { 2, 3, 4, 5 };

            for (int tier = 0; tier <= 3; tier++)
            {
                // Act
                int beamCount = WeaponBalanceModel.L1BeamCount(l1, tier);
                float perBeam = WeaponBalanceModel.L1PerBeamHeatRate(l1, tier);
                float so = WeaponBalanceModel.SustainedOutput(l1, balance, tier);

                // Assert
                Assert.That(beamCount, Is.EqualTo(expectedBeamCounts[tier]), $"tier {tier} beam count");
                Assert.That(perBeam * beamCount, Is.EqualTo(l1.L1HRateFull).Within(1e-4f),
                    $"tier {tier}: per-beam split must sum back to the full rate (C.3 invariant)");
                AssertInBand(so, balance, $"L1 tier {tier} Sustained_Output out of band");
            }
        }

        // ── L2 — single tier-independent assertion ────────────────────────────────

        [Test]
        public void test_l2_sustained_output_is_in_band()
        {
            var l2 = L2Def();
            var balance = Balance();

            float so = WeaponBalanceModel.SustainedOutput(l2, balance, tier: 0);

            Assert.That(so, Is.EqualTo(0.975f).Within(1e-3f), "matches D.2 worked example (37.5*0.65/25)");
            AssertInBand(so, balance, "L2 Sustained_Output out of band");
        }

        // ── L3 — Charge basis is the equal-power reference; Tap is recorded but not asserted ────

        [Test]
        public void test_l3_charge_sustained_output_is_in_band()
        {
            var l3 = L3Def();
            var balance = Balance();

            float so = WeaponBalanceModel.SustainedOutput(l3, balance, tier: 0);

            Assert.That(so, Is.EqualTo(2.50f / 2.7f).Within(1e-3f), "matches D.3 worked example (2.5/(1.2+1.5))");
            AssertInBand(so, balance, "L3 charge Sustained_Output out of band");
        }

        [Test]
        public void test_l3_tap_sustained_output_is_a_fixed_burst_value_not_asserted_in_band()
        {
            // D.3: Tap is a deliberately weakened filler mode — recorded so it doesn't drift, but
            // NOT looped into the H.1 in-band assertions (director I#1 burst-value ruling).
            var l3 = L3Def();

            float tapOutput = WeaponBalanceModel.L3TapSustainedOutput(l3);

            Assert.That(tapOutput, Is.EqualTo(0.60f).Within(1e-6f));
        }

        // ── L4 — single tier-independent assertion ────────────────────────────────

        [Test]
        public void test_l4_sustained_output_is_in_band()
        {
            var l4 = L4Def();
            var balance = Balance();

            float so = WeaponBalanceModel.SustainedOutput(l4, balance, tier: 0);

            AssertInBand(so, balance, "L4 Sustained_Output out of band");
        }

        // ── M1 — Tier 0-2 and Tier-3 both in band (D.6: +3.2% drift) ──────────────

        [Test]
        public void test_m1_sustained_output_in_band_at_base_and_tier3()
        {
            var m1 = M1Def();
            var balance = Balance();

            float soBase = WeaponBalanceModel.SustainedOutput(m1, balance, tier: 1);
            float soTier3 = WeaponBalanceModel.SustainedOutput(m1, balance, tier: 3);

            Assert.That(soBase, Is.EqualTo(0.909f).Within(2e-3f), "matches D.4 worked example (3.0/3.3)");
            AssertInBand(soBase, balance, "M1 base Sustained_Output out of band");
            AssertInBand(soTier3, balance, "M1 tier-3 Sustained_Output out of band");
        }

        // ── M2 — Chain Hive: Tier 0-2 and Tier-3 numbers are IDENTICAL (D.5) ──────

        [Test]
        public void test_m2_sustained_output_identical_at_base_and_tier3_and_in_band()
        {
            var m2 = M2Def();
            var balance = Balance();

            float soBase = WeaponBalanceModel.SustainedOutput(m2, balance, tier: 1);
            float soTier3 = WeaponBalanceModel.SustainedOutput(m2, balance, tier: 3);

            Assert.That(soTier3, Is.EqualTo(soBase).Within(1e-6f),
                "Chain Hive: T3 'saturation callout' only changes targeting, never the D.5 formula inputs");
            Assert.That(soBase, Is.EqualTo(1.034f).Within(2e-3f), "matches D.5 worked example (6.0/5.8)");
            AssertInBand(soBase, balance, "M2 Sustained_Output out of band");
        }

        // ── M3 — single assertion, EffectiveHitRate=0.80 applied (unsoftened basis) ──

        [Test]
        public void test_m3_sustained_output_in_band_with_effective_hit_rate_applied()
        {
            var m3 = M3Def();
            var balance = Balance();

            float so = WeaponBalanceModel.SustainedOutput(m3, balance, tier: 0);

            Assert.That(so, Is.EqualTo(7.2f / 7.0f).Within(1e-3f), "matches D.1 worked example ((9*0.8)/(3+4))");
            AssertInBand(so, balance, "M3 Sustained_Output out of band");
        }

        // ── M4 — Tier 0-2 and Tier-3 both in band after the G.6 retune (0.20 -> 0.18) ────

        [Test]
        public void test_m4_sustained_output_in_band_at_base_and_tier3_after_g6_retune()
        {
            var m4 = M4Def();
            var balance = Balance();

            float soBase = WeaponBalanceModel.SustainedOutput(m4, balance, tier: 1);
            float soTier3 = WeaponBalanceModel.SustainedOutput(m4, balance, tier: 3);

            Assert.That(soBase, Is.EqualTo(0.930f).Within(2e-3f), "matches D.4 worked example (4.0/4.3)");
            Assert.That(soTier3, Is.EqualTo(1.005f).Within(3e-3f), "matches D.6 retuned worked example (4.32/4.3)");
            AssertInBand(soBase, balance, "M4 base Sustained_Output out of band");
            AssertInBand(soTier3, balance, "M4 tier-3 Sustained_Output out of band");
        }
    }
}
