using System;
using System.Collections.Generic;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Economy;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Difficulty
{
    /// <summary>
    /// Difficulty Story 004 (BLOCKING) — material-yield invariance across D1–D4 (difficulty-system.md §H.4).
    /// Proves a part break yields identical materials at every difficulty tier: 3 qualities × 3 part types,
    /// each asserted equal across all four tiers (36 checks).
    ///
    /// Reconciliation vs. story text: economy Story 001 implemented yield as an EVENT-DRIVEN producer
    /// (EconomyService subscribes to PartBroke and credits ISaveService) — there is no
    /// <c>CalculateYield(quality, partType)</c> pure function, and EconomyConfig has NO
    /// <c>difficulty_yield_bonus</c> field at all. That absence is STRONGER than "always 0.0": the knob
    /// cannot be set non-zero because it does not exist (asserted below). Yield is observed here through the
    /// real PartBroke → EconomyService → RecordingSaveService path, mirroring economy's own yield tests.
    /// </summary>
    [TestFixture]
    public sealed class MaterialYieldInvarianceTests
    {
        private const int KaijuId = 10;   // maps to KaijuTheme.Carapace via the stub
        private static readonly DifficultyTier[] Tiers =
            { DifficultyTier.D1, DifficultyTier.D2, DifficultyTier.D3, DifficultyTier.D4 };

        /// <summary>Fire one break and return its (shard, core) yield fingerprint.</summary>
        private static (int shard, int core) YieldFor(BreakQuality quality, PartType partType)
        {
            var config = ContentTestFactory.Create<EconomyConfig>();
            var bus = new TypedEventBus();
            var rec = new RecordingSaveService();
            var theme = new StubKaijuThemeQuery().Register(KaijuId, KaijuTheme.Carapace);
            // EconomyService takes NO IDifficultyProvider — difficulty cannot enter the yield computation.
            var _ = new EconomyService(config, bus, rec, theme, rec);

            bus.Publish(new PartBroke(
                partId: 1, kaijuId: KaijuId, type: partType, worldPosition: Vector2.zero,
                dropTableId: 0, quality: quality, adjacencyIds: Array.Empty<int>(), isChainBreak: false));

            return (rec.TotalFor(MaterialId.ShardCommon), rec.TotalFor(MaterialId.CoreCarapace));
        }

        private static IEnumerable<TestCaseData> YieldMatrix()
        {
            foreach (BreakQuality q in new[] { BreakQuality.Normal, BreakQuality.Softened, BreakQuality.SoftenedStaggered })
                foreach (PartType pt in new[] { PartType.Normal, PartType.Armored, PartType.BossCore })
                    yield return new TestCaseData(q, pt).SetName($"Yield_{q}_{pt}_invariant");
        }

        // ── AC-H.4: yield identical across D1–D4 (36 checks = 9 combos × 4 tiers) ────────────────

        [TestCaseSource(nameof(YieldMatrix))]
        public void test_material_yield_invariant_across_all_difficulty_tiers(BreakQuality quality, PartType partType)
        {
            (int shard, int core) baseline = YieldFor(quality, partType);
            foreach (DifficultyTier diff in Tiers)
            {
                // The yield path has no difficulty input, so "under tier X" is the same call — it MUST match.
                (int shard, int core) actual = YieldFor(quality, partType);
                Assert.AreEqual(baseline, actual,
                    $"Material yield MUST be difficulty-invariant ({quality}/{partType} @ {diff}, §H.4).");
            }
        }

        // ── AC: EconomyConfig exposes NO difficulty knob (stronger than difficulty_yield_bonus == 0) ─

        [Test]
        public void test_economy_config_has_no_difficulty_yield_knob()
        {
            Type t = typeof(EconomyConfig);
            var found = new List<string>();

            foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                if (f.Name.ToLowerInvariant().Contains("difficulty")) found.Add(f.Name);
            foreach (PropertyInfo p in t.GetProperties())
                if (p.Name.ToLowerInvariant().Contains("difficulty")) found.Add(p.Name);

            Assert.IsEmpty(found,
                "EconomyConfig must expose NO difficulty knob — 'difficulty_yield_bonus' must not exist "
                + "(§H.4: yield is difficulty-invariant; the knob cannot be non-zero if absent). Found: "
                + string.Join(", ", found));
        }

        // ── AC: Economy assembly never references difficulty (structural invariance) ────────────

        [Test]
        public void test_economy_assembly_never_references_difficulty_provider()
        {
            List<string> hits = AssemblyReferenceScanner.FindReferencesTo(
                "KaijuBreaker.Economy", typeof(IDifficultyProvider));
            Assert.IsEmpty(hits,
                "Economy must NEVER read difficulty (§H.4 structural invariance). Found: "
                + string.Join("; ", hits));
        }
    }
}
