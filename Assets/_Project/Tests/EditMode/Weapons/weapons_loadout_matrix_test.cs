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
        // ── Fixtures — shared spec-default knobs (WeaponBalanceFixtures) ────────────────────────
        // Extracted to Helpers so the Economy anti-dominant guard (Story 005) asserts against the
        // exact same inputs (weapon-tiering-and-equal-power.md §D).

        private static WeaponBalanceConfig Balance() => WeaponBalanceFixtures.Balance();
        private static List<WeaponDef> Primaries() => WeaponBalanceFixtures.Primaries();
        private static List<WeaponDef> Secondaries() => WeaponBalanceFixtures.Secondaries();

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
