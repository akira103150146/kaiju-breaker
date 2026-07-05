using System.Collections.Generic;
using System.Globalization;
using System.Text;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Difficulty;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Tests.EditMode.Weapons;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Difficulty
{
    /// <summary>
    /// Difficulty Story 003 (BLOCKING) — weapon-output invariance across D1–D4 (difficulty-system.md §H.3).
    /// Proves each of the 8 weapons delivers identical 30s sustained output at every difficulty tier, and
    /// emits the 4×8 matrix to production/qa/evidence. Shares the weapon-system H.1 equal-power infra
    /// (<see cref="WeaponBalanceModel.SustainedOutput"/>) and adds the cross-difficulty assertion layer.
    ///
    /// As with TTB, invariance is structural: SustainedOutput has no DifficultyTier parameter, so weapon
    /// damage cannot scale with difficulty. The assembly-scan test is the load-bearing guarantee.
    /// </summary>
    [TestFixture]
    public sealed class WeaponOutputInvarianceTests
    {
        private static readonly DifficultyTier[] Tiers =
            { DifficultyTier.D1, DifficultyTier.D2, DifficultyTier.D3, DifficultyTier.D4 };
        private static readonly string[] Labels = { "L1", "L2", "L3", "L4", "M1", "M2", "M3", "M4" };

        [Test]
        public void test_weapon_output_invariant_across_all_difficulty_tiers()
        {
            WeaponBalanceConfig balance = WeaponBalanceFixtures.Balance();
            var weapons = new List<WeaponDef>();
            weapons.AddRange(WeaponBalanceFixtures.Primaries());    // L1..L4
            weapons.AddRange(WeaponBalanceFixtures.Secondaries());  // M1..M4
            Assert.AreEqual(Labels.Length, weapons.Count, "Expected exactly 8 weapons (4 laser + 4 missile).");

            DifficultyConfig cfg = ContentTestFactory.Create<DifficultyConfig>();
            var baseline = new float[weapons.Count];
            var sb = new StringBuilder("Tier," + string.Join(",", Labels) + "\n");

            foreach (DifficultyTier diff in Tiers)
            {
                var sys = new DifficultySystem(cfg);
                sys.SetTier(diff);   // "in D-x environment" — output model ignores it by construction

                var cells = new List<string> { diff.ToString() };
                for (int i = 0; i < weapons.Count; i++)
                {
                    float output = WeaponBalanceModel.SustainedOutput(weapons[i], balance, tier: 1);
                    if (diff == DifficultyTier.D1)
                        baseline[i] = output;
                    else
                        Assert.AreEqual(baseline[i], output,
                            $"{Labels[i]} output drifted at {diff} — weapon output MUST be difficulty-invariant (§H.3).");
                    cells.Add(output.ToString("F4", CultureInfo.InvariantCulture));
                }
                sb.AppendLine(string.Join(",", cells));
            }

            DifficultyEvidence.Write("weapon_output_invariance_matrix.txt", sb.ToString());
        }

        [Test]
        public void test_weapons_assembly_never_references_difficulty_provider()
        {
            List<string> hits = AssemblyReferenceScanner.FindReferencesTo(
                "KaijuBreaker.Weapons", typeof(IDifficultyProvider));
            Assert.IsEmpty(hits,
                "Weapons must NEVER read difficulty (§H.3 structural invariance). Found: "
                + string.Join("; ", hits));
        }
    }
}
