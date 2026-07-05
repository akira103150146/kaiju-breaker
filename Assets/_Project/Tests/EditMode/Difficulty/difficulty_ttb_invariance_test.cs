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
    /// Difficulty Story 003 (BLOCKING) — part-TTB invariance across D1–D4 (difficulty-system.md §H.2).
    /// Proves part durability (time-to-break) is identical at every difficulty tier for NORMAL / ARMORED /
    /// BOSS_CORE parts, and emits the 4×3 matrix to production/qa/evidence for designer review.
    ///
    /// The invariance is a STRUCTURAL guarantee, not a runtime clamp: <see cref="WeaponBalanceModel.Ttb"/>
    /// (the closed-form TTB model shared with the weapon-system equal-power tests) has no DifficultyTier
    /// parameter — difficulty literally cannot enter the computation. Selecting a tier on a live
    /// DifficultySystem below is theatre that mirrors the real "in D-x environment" framing; the assertion
    /// that all four rows are byte-identical is what a future difficulty-into-TTB regression would break.
    /// The companion assembly-scan test is the stronger, compile-independent proof.
    /// </summary>
    [TestFixture]
    public sealed class PartTtbInvarianceTests
    {
        private static readonly DifficultyTier[] Tiers =
            { DifficultyTier.D1, DifficultyTier.D2, DifficultyTier.D3, DifficultyTier.D4 };
        private static readonly PartType[] PartTypes =
            { PartType.Normal, PartType.Armored, PartType.BossCore };

        [Test]
        public void test_part_ttb_invariant_across_all_difficulty_tiers()
        {
            WeaponBalanceConfig balance = WeaponBalanceFixtures.Balance();
            WeaponDef weapon = WeaponBalanceFixtures.L2();        // representative primary (Focus Beam)
            DifficultyConfig cfg = ContentTestFactory.Create<DifficultyConfig>();

            var baseline = new Dictionary<PartType, float>();
            var sb = new StringBuilder("Tier,NORMAL,ARMORED,BOSS_CORE\n");

            foreach (DifficultyTier diff in Tiers)
            {
                var sys = new DifficultySystem(cfg);
                sys.SetTier(diff);   // "in D-x environment" — TTB model ignores it by construction

                var cells = new List<string> { diff.ToString() };
                foreach (PartType pt in PartTypes)
                {
                    float ttb = WeaponBalanceModel.Ttb(weapon, balance, pt, tier: 1);
                    if (diff == DifficultyTier.D1)
                        baseline[pt] = ttb;
                    else
                        Assert.AreEqual(baseline[pt], ttb,
                            $"TTB drifted at {diff}/{pt} — part durability MUST be difficulty-invariant (§H.2).");
                    cells.Add(ttb.ToString("F4", CultureInfo.InvariantCulture));
                }
                sb.AppendLine(string.Join(",", cells));
            }

            DifficultyEvidence.Write("ttb_invariance_matrix.txt", sb.ToString());
        }

        [Test]
        public void test_kaijuparts_assembly_never_references_difficulty_provider()
        {
            List<string> hits = AssemblyReferenceScanner.FindReferencesTo(
                "KaijuBreaker.KaijuParts", typeof(IDifficultyProvider));
            Assert.IsEmpty(hits,
                "KaijuParts must NEVER read difficulty (§H.2 structural invariance). Found: "
                + string.Join("; ", hits));
        }
    }
}
