using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Tests.EditMode.Weapons; // WeaponBalanceModel (same EditMode assembly)
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Economy
{
    /// <summary>
    /// Economy Story 005 — Anti-Degenerate Loadout Guard (material-economy.md §D.4, §H.3; TR-economy-003/006).
    /// A design-time CI gate (NOT runtime) asserting the permanent-upgrade progression never produces a
    /// dominant loadout. Consumes the SHARED <see cref="WeaponBalanceFixtures"/> + <see cref="WeaponBalanceModel"/>
    /// closed-form TTB model and the Economy-owned <see cref="EconomyConfig.MaxTtbImprovementPct"/> threshold.
    ///
    /// <para><b>Scope reconciliation (surfaced for review).</b> The GDD's automatable anti-dominance rule is the
    /// §D.4/§H.3 <b>≤2.0× TTB spread</b> plus the per-weapon <b>≤15% Tier 0→3 improvement</b> cap — both asserted
    /// here across all 3 part types. Story 005 AC-4's literal "no loadout ranks top-3 in ALL 3 part types" is
    /// <b>unsatisfiable by construction</b> for this closed-form model: part type scales only the T_break term
    /// uniformly, so the fastest-softening laser + fastest-breaking missile is necessarily rank-1 in Normal,
    /// Armored and BossCore alike. The qualitative "every weapon is best-or-near-best somewhere" guarantee
    /// (§H.6 — driven by identity traits like spread / pierce / homing that a pure-TTB model does not capture)
    /// is the QA playtest's job, which the story's own Out-of-Scope defers to the QA lead. This suite therefore
    /// asserts the two TTB invariants the model CAN verify; it does not encode the top-3 metric.</para>
    /// </summary>
    [TestFixture]
    public sealed class EconomyDominantLoadoutGuardTests
    {
        private static readonly PartType[] PartTypes = { PartType.Normal, PartType.Armored, PartType.BossCore };
        private const int Tier0 = 0, Tier2 = 2, Tier3 = 3;

        /// <summary>Fractional TTB improvement (0..1) a weapon gains going from one tier to a higher one on a
        /// part type: <c>1 − TTB_high / TTB_low</c>. Positive = faster (better) at the higher tier.</summary>
        private static float Improvement(WeaponDef w, WeaponBalanceConfig b, PartType pt, int lowTier, int highTier)
        {
            float low = WeaponBalanceModel.Ttb(w, b, pt, lowTier);
            float high = WeaponBalanceModel.Ttb(w, b, pt, highTier);
            return 1f - high / low;
        }

        // ── AC-1: per-weapon Tier 0→3 improvement ≤ MaxTtbImprovementPct (config-driven), all 3 part types ──

        [Test]
        public void test_per_weapon_tier0to3_improvement_within_config_cap()
        {
            var b = WeaponBalanceFixtures.Balance();
            var config = ContentTestFactory.Create<EconomyConfig>();
            float cap = config.MaxTtbImprovementPct;

            foreach (WeaponDef w in WeaponBalanceFixtures.All())
                foreach (PartType pt in PartTypes)
                {
                    float imp = Improvement(w, b, pt, Tier0, Tier3);
                    Assert.LessOrEqual(imp, cap + 1e-4f,
                        $"{w.Id} on {pt}: Tier 0→3 TTB improvement {imp:P1} exceeds the {cap:P0} cap " +
                        "(material-economy.md §D.4). Balancers must reduce the WeaponDef Tier-3 effect, not this threshold.");
                }
        }

        // ── AC-2: per-weapon Tier 0→2 cumulative improvement ≤ Tier0To2CapPct ───────────────────

        [Test]
        public void test_per_weapon_tier0to2_improvement_within_intermediate_cap()
        {
            var b = WeaponBalanceFixtures.Balance();
            var config = ContentTestFactory.Create<EconomyConfig>();
            float cap = config.Tier0To2CapPct;

            foreach (WeaponDef w in WeaponBalanceFixtures.All())
                foreach (PartType pt in PartTypes)
                {
                    float imp = Improvement(w, b, pt, Tier0, Tier2);
                    Assert.LessOrEqual(imp, cap + 1e-4f,
                        $"{w.Id} on {pt}: Tier 0→2 TTB improvement {imp:P1} exceeds the {cap:P0} intermediate cap " +
                        "(material-economy.md §C.3).");
                }
        }

        // ── AC-3: 4×4 loadout matrix spread ≤ 2.0× at Tier 3, across ALL 3 part types ────────────

        [Test]
        public void test_loadout_matrix_spread_within_2x_all_part_types()
        {
            var b = WeaponBalanceFixtures.Balance();
            var primaries = WeaponBalanceFixtures.Primaries();
            var secondaries = WeaponBalanceFixtures.Secondaries();

            foreach (PartType pt in PartTypes)
            {
                float min = float.PositiveInfinity, max = float.NegativeInfinity;
                string minPair = null, maxPair = null;

                foreach (WeaponDef primary in primaries)
                    foreach (WeaponDef secondary in secondaries)
                    {
                        float ttb = WeaponBalanceModel.LoadoutTtb(primary, secondary, b, pt, Tier3);
                        Assert.Greater(ttb, 0f, $"{pt}: {primary.Id}+{secondary.Id} TTB must be > 0 (guards the ratio divisor).");
                        if (ttb < min) { min = ttb; minPair = $"{primary.Id}+{secondary.Id}"; }
                        if (ttb > max) { max = ttb; maxPair = $"{primary.Id}+{secondary.Id}"; }
                    }

                Assert.LessOrEqual(max / min, 2.0f,
                    $"{pt}: dominant loadout — slowest {maxPair} ({max:F2}s) vs fastest {minPair} ({min:F2}s), " +
                    $"ratio {max / min:F2} exceeds the 2.0× ceiling (material-economy.md §H.3).");
            }
        }

        // ── AC-5: the per-weapon cap threshold is sourced from EconomyConfig, not hard-coded ─────

        [Test]
        public void test_ttb_cap_threshold_is_config_driven()
        {
            var b = WeaponBalanceFixtures.Balance();
            var defaultConfig = ContentTestFactory.Create<EconomyConfig>();                       // 0.15
            var strictConfig = ContentTestFactory.Create<EconomyConfig>(("_maxTtbImprovementPct", 0.02f));

            // Find the weapon with the largest real Tier 0→3 improvement (M4 child-split, ~7%).
            float maxImp = 0f; WeaponId worst = WeaponId.L1;
            foreach (WeaponDef w in WeaponBalanceFixtures.All())
            {
                float imp = Improvement(w, b, PartType.Normal, Tier0, Tier3);
                if (imp > maxImp) { maxImp = imp; worst = w.Id; }
            }

            // That weapon must PASS the default 0.15 cap but FAIL the strict 0.02 cap — proving the
            // assertion compares against the config value, not a literal.
            Assert.LessOrEqual(maxImp, defaultConfig.MaxTtbImprovementPct + 1e-4f,
                $"{worst} ({maxImp:P1}) should pass the default {defaultConfig.MaxTtbImprovementPct:P0} cap.");
            Assert.Greater(maxImp, strictConfig.MaxTtbImprovementPct,
                $"{worst} ({maxImp:P1}) should breach a strict {strictConfig.MaxTtbImprovementPct:P0} cap — confirms config-driven threshold.");
        }
    }
}
