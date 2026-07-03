using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 002 H.2 — no dominant loadout. TTB = T_soften(primary) + T_break(secondary)
    /// (weapon-system.md D.4) for every Primary(laser) × Secondary(missile) pair, on a Normal
    /// part. design/gdd/weapon-tiering-and-equal-power.md H.2 extends the original H.2 to run the
    /// full matrix at BOTH Tier 0 (base) and Tier 3 (maxed) — the tiering/equal-power growth added
    /// by this doc must not create a new dominant combo once weapons are fully upgraded.
    ///
    /// Only 4×4 = 16 loadouts exist (Primary MUST be a laser L1-L4, Secondary MUST be a missile
    /// M1-M4 — see LoadoutController.SlotOf / weapons_loadout_system_test.cs); the "8×8=64" figure
    /// in weapon-system.md H.2's prose is loose phrasing for "all pairs across the two 4-weapon
    /// pools," not a literal 8×8 grid.
    ///
    /// Uses <see cref="WeaponBalanceModel"/> — the closed-form D.4 formula model — NOT the runtime
    /// weapon behaviour objects.
    /// </summary>
    public sealed class WeaponsLoadoutMatrixTests
    {
        // ── Fixtures — same spec-default knob values as weapons_dps_equivalence_test.cs ─────────

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

        private static List<WeaponDef> Primaries() => new List<WeaponDef> { L1Def(), L2Def(), L3Def(), L4Def() };
        private static List<WeaponDef> Secondaries() => new List<WeaponDef> { M1Def(), M2Def(), M3Def(), M4Def() };

        // ── Shared matrix walk ────────────────────────────────────────────────────

        private static void AssertNoDominantLoadout(int tier, string label)
        {
            // Arrange
            var balance = Balance();
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            string minPair = null, maxPair = null;

            // Act — walk all 4×4 = 16 Primary(laser) × Secondary(missile) pairs.
            foreach (var primary in Primaries())
            {
                foreach (var secondary in Secondaries())
                {
                    float ttb = WeaponBalanceModel.LoadoutTtb(primary, secondary, balance, PartType.Normal, tier);
                    Assert.That(ttb, Is.GreaterThan(0f), $"{label}: {primary.Id}+{secondary.Id} TTB must be positive");

                    if (ttb < min) { min = ttb; minPair = $"{primary.Id}+{secondary.Id}"; }
                    if (ttb > max) { max = ttb; maxPair = $"{primary.Id}+{secondary.Id}"; }
                }
            }

            // Assert — weapon-system.md H.2 / weapon-tiering-and-equal-power.md H.2: no loadout's
            // TTB exceeds 2.0x the fastest loadout's TTB.
            Assert.That(max / min, Is.LessThanOrEqualTo(2.0f),
                $"{label}: dominant loadout detected — slowest {maxPair} ({max:F2}s) vs fastest {minPair} ({min:F2}s), " +
                $"ratio {max / min:F2} exceeds the 2.0x H.2 ceiling");
        }

        // ── H.2: Tier 0 (base) matrix ──────────────────────────────────────────────

        [Test]
        public void test_loadout_matrix_tier0_ttb_ratio_within_2x_ceiling()
        {
            AssertNoDominantLoadout(tier: 0, label: "Tier 0");
        }

        // ── H.2 extension (weapon-tiering-and-equal-power.md): Tier 3 (maxed) matrix ─────────────

        [Test]
        public void test_loadout_matrix_tier3_ttb_ratio_within_2x_ceiling()
        {
            AssertNoDominantLoadout(tier: 3, label: "Tier 3");
        }
    }
}
