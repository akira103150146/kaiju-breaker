using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 002 H.3 — M3 heat-shock armor gate. The whole dual-track design hinges on the
    /// soften-first path being meaningfully faster than brute-forcing an unsoftened part with the
    /// AP torpedo: TTB(skip-heat) MUST be ≥ 1.5× TTB(normal soften→heat-shock) at the shipped
    /// balance defaults (weapon-system.md H.3 / D.4).
    ///
    /// Uses the GDD's continuous-rate simplification (D.4) so no per-torpedo shot-cadence field is
    /// needed — this validates the RATE gate, which is the part that is cleanly computable from the
    /// current SO data. Values are read from injected fixtures (ADR-0003), never hardcoded.
    ///
    /// SCOPE NOTE: the gate holds at the default B_unsoftened_mult (0.35, ratio ≈ 1.53) but weakens
    /// below 1.5× toward the safe-range upper bound (0.50, ratio ≈ 1.07). That range-boundary
    /// tightening, plus H.1/H.2 (equal-power) and H.7 (Tier-3 identity), are folded into the weapon
    /// balance pass tracked in design/balance/weapon-d0-equal-power-analysis.md — not asserted here.
    /// </summary>
    public sealed class WeaponsM3GateValidationTests
    {
        // Continuous-rate TTB model (weapon-system.md D.2/D.3/D.4), unit-consistent in BU.
        private static float TtbUnsoftened(WeaponDef m3, WeaponBalanceConfig b)
        {
            // T_soften = 0 (brute force). Unsoftened break rate = mult × BuPerD0 × B_unsoftened_mult.
            float rate = m3.M3DmgUnsoftenedMult * b.BuPerD0 * b.BUnsoftenedMult;
            return b.RequiredBreakThresholdNormal / rate;
        }

        private static float TtbNormal(WeaponDef m3, WeaponBalanceConfig b, float laserHRate)
        {
            // Soften with a laser, then heat-shock detonate (softened rate = mult × heatShock × BuPerD0).
            float netHeatRate = laserHRate - b.HDecayRate;
            if (netHeatRate < 0.01f) netHeatRate = 0.01f; // GDD max(H_rate - H_decay, ε) guard
            float tSoften = b.ThetaS / netHeatRate;
            float shockRate = m3.M3DmgUnsoftenedMult * m3.M3HeatShockFillMult * b.BuPerD0;
            float tBreak = b.RequiredBreakThresholdNormal / shockRate;
            return tSoften + tBreak;
        }

        private static WeaponDef M3Def() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M3), ("_type", WeaponType.Missile),
            ("_m3DmgUnsoftenedMult", 3.0f), ("_m3HeatShockFillMult", 2.0f));

        private static WeaponBalanceConfig Balance(float bUnsoftened) =>
            ContentTestFactory.Create<WeaponBalanceConfig>(("_bUnsoftenedMult", bUnsoftened));

        [Test]
        public void test_m3_skip_heat_path_is_at_least_1_5x_slower_at_default_unsoftened_mult()
        {
            // Arrange — shipped default B_unsoftened_mult = 0.35; soften via L1 full spread (25 HU/s).
            var m3 = M3Def();
            var balance = Balance(0.35f);
            const float l1FullHRate = 25f;

            // Act
            float ttbUnsoftened = TtbUnsoftened(m3, balance);
            float ttbNormal = TtbNormal(m3, balance, l1FullHRate);

            // Assert — the heat-shock gate makes brute-forcing ≥ 1.5× slower (weapon-system.md H.3).
            Assert.That(ttbUnsoftened, Is.GreaterThanOrEqualTo(ttbNormal * 1.5f),
                $"heat-shock gate too weak: unsoftened {ttbUnsoftened:F2}s vs normal {ttbNormal:F2}s " +
                $"(ratio {ttbUnsoftened / ttbNormal:F2}, need ≥ 1.5).");
        }

        [Test]
        public void test_m3_gate_holds_at_lower_unsoftened_bound()
        {
            // At the safe-range lower bound (0.20) the gate is even stronger — regression guard.
            var m3 = M3Def();
            var balance = Balance(0.20f);
            float ratio = TtbUnsoftened(m3, balance) / TtbNormal(m3, balance, 25f);
            Assert.That(ratio, Is.GreaterThanOrEqualTo(1.5f));
        }
    }
}
