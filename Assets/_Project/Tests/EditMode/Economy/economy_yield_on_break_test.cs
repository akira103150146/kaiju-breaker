using System;
using System.Collections.Generic;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Economy;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Economy
{
    /// <summary>
    /// Economy Story 001 — Part-Break Material Yield Computation (material-economy.md §H.2/§H.4).
    /// Verifies Economy independently computes shard + kaiju-theme core yield from a <see cref="PartBroke"/>
    /// event: shard scales by break quality (floored), core type is fixed by kaiju THEME (not part type),
    /// and core count is 1 (or 2 on a Perfect break when double-drop is enabled). No zero-drop path.
    /// </summary>
    [TestFixture]
    public sealed class EconomyYieldOnBreakTests
    {
        // Runtime kaiju ids used across tests; each maps to a distinct theme via the stub query.
        private const int KaijuCarapace = 10;
        private const int KaijuLimb = 20;
        private const int KaijuEnergy = 30;

        // Fixture defaults matching the story's config (shard_base=2, precision=1.5, perfect=2.0, double=TRUE).
        private const int ShardBase = 2;
        private const float PrecisionMult = 1.5f;
        private const float PerfectMult = 2.0f;

        private static EconomyConfig MakeConfig(
            int shardBase = ShardBase,
            float precisionMult = PrecisionMult,
            float perfectMult = PerfectMult,
            bool perfectDoubleDrop = true)
        {
            return ContentTestFactory.Create<EconomyConfig>(
                ("_shardYieldBase", shardBase),
                ("_shardYieldSoftenedMult", precisionMult),
                ("_shardYieldSoftenedStaggeredMult", perfectMult),
                ("_corePerfectDoubleDrop", perfectDoubleDrop));
        }

        private static StubKaijuThemeQuery MakeThemeQuery()
        {
            return new StubKaijuThemeQuery()
                .Register(KaijuCarapace, KaijuTheme.Carapace)
                .Register(KaijuLimb, KaijuTheme.Limb)
                .Register(KaijuEnergy, KaijuTheme.Energy);
        }

        /// <summary>Wire a live Economy over a real bus, fire one PartBroke, return the recorded credits.</summary>
        private static RecordingSaveService FireBreak(
            EconomyConfig config, IKaijuThemeQuery themeQuery,
            int kaijuId, BreakQuality quality, PartType partType)
        {
            var bus = new TypedEventBus();
            var rec = new RecordingSaveService();
            // Service subscribes in its ctor; the bus keeps it alive via the subscription delegate.
            var _ = new EconomyService(config, bus, rec, themeQuery);

            var evt = new PartBroke(
                partId: 1, kaijuId: kaijuId, type: partType, worldPosition: Vector2.zero,
                dropTableId: 0, quality: quality, adjacencyIds: Array.Empty<int>(), isChainBreak: false);
            bus.Publish(evt);
            return rec;
        }

        private static MaterialId ExpectedCore(int kaijuId) => kaijuId switch
        {
            KaijuCarapace => MaterialId.CoreCarapace,
            KaijuLimb => MaterialId.CoreLimb,
            KaijuEnergy => MaterialId.CoreEnergy,
            _ => throw new ArgumentException("unmapped test kaiju id")
        };

        private static int ExpectedShard(BreakQuality quality) => quality switch
        {
            BreakQuality.Normal => Mathf.FloorToInt(ShardBase * 1.0f),          // 2
            BreakQuality.Softened => Mathf.FloorToInt(ShardBase * PrecisionMult),  // 3
            BreakQuality.SoftenedStaggered => Mathf.FloorToInt(ShardBase * PerfectMult), // 4
            _ => throw new ArgumentOutOfRangeException(nameof(quality))
        };

        // ── AC-1/AC-2/AC-3 + the 27-scenario matrix (3 quality × 3 part_type × 3 theme) ──────────

        private static readonly BreakQuality[] AllQualities =
            { BreakQuality.Normal, BreakQuality.Softened, BreakQuality.SoftenedStaggered };
        private static readonly PartType[] YieldPartTypes =
            { PartType.Normal, PartType.Armored, PartType.BossCore };
        private static readonly int[] AllKaiju = { KaijuCarapace, KaijuLimb, KaijuEnergy };

        private static IEnumerable<TestCaseData> YieldMatrix()
        {
            foreach (var q in AllQualities)
                foreach (var pt in YieldPartTypes)
                    foreach (var k in AllKaiju)
                        yield return new TestCaseData(q, pt, k)
                            .SetName($"Yield_{q}_{pt}_kaiju{k}");
        }

        [TestCaseSource(nameof(YieldMatrix))]
        public void test_economy_break_yields_correct_shard_and_core(BreakQuality quality, PartType partType, int kaijuId)
        {
            // Arrange
            var config = MakeConfig();
            var themeQuery = MakeThemeQuery();
            int expectedShard = ExpectedShard(quality);
            MaterialId expectedCore = ExpectedCore(kaijuId);
            int expectedCoreCount = quality == BreakQuality.SoftenedStaggered ? 2 : 1;

            // Act
            var rec = FireBreak(config, themeQuery, kaijuId, quality, partType);

            // Assert — exactly two credits: shard then core; part_type never changes either.
            Assert.AreEqual(2, rec.CallCount, "Each break credits exactly shard + core.");
            Assert.AreEqual(expectedShard, rec.TotalFor(MaterialId.ShardCommon), "Shard yield");
            Assert.AreEqual(expectedCoreCount, rec.TotalFor(expectedCore), "Core count for the kaiju theme");
            // No other core type is ever credited.
            foreach (MaterialId core in new[] { MaterialId.CoreCarapace, MaterialId.CoreLimb, MaterialId.CoreEnergy })
                if (core != expectedCore)
                    Assert.AreEqual(0, rec.TotalFor(core), $"No {core} for this kaiju theme");
        }

        // ── AC-2 edge: floor, not round/ceil ────────────────────────────────────────────────────

        [Test]
        public void test_economy_shard_yield_uses_floor_not_round()
        {
            // base=3, precision=1.5 → 4.5 → floor = 4 (NOT 5).
            var config = MakeConfig(shardBase: 3, precisionMult: 1.5f);
            var rec = FireBreak(config, MakeThemeQuery(), KaijuLimb, BreakQuality.Softened, PartType.Armored);
            Assert.AreEqual(4, rec.TotalFor(MaterialId.ShardCommon), "floor(3 × 1.5) = 4");
        }

        // ── AC-3 edge: core_perfect_double_drop = FALSE → 1 core on Perfect ─────────────────────

        [Test]
        public void test_economy_perfect_break_yields_single_core_when_double_drop_disabled()
        {
            var config = MakeConfig(perfectDoubleDrop: false);
            var rec = FireBreak(config, MakeThemeQuery(), KaijuEnergy, BreakQuality.SoftenedStaggered, PartType.BossCore);
            Assert.AreEqual(1, rec.TotalFor(MaterialId.CoreEnergy), "Perfect yields 1 core when double-drop is off");
            Assert.AreEqual(Mathf.FloorToInt(ShardBase * PerfectMult), rec.TotalFor(MaterialId.ShardCommon),
                "Shard multiplier is unaffected by the core double-drop flag");
        }

        [Test]
        public void test_economy_standard_and_precision_always_yield_single_core_regardless_of_flag()
        {
            var config = MakeConfig(perfectDoubleDrop: true);
            var recStd = FireBreak(config, MakeThemeQuery(), KaijuCarapace, BreakQuality.Normal, PartType.Normal);
            var recPrec = FireBreak(config, MakeThemeQuery(), KaijuCarapace, BreakQuality.Softened, PartType.Normal);
            Assert.AreEqual(1, recStd.TotalFor(MaterialId.CoreCarapace), "Standard → 1 core even with double-drop on");
            Assert.AreEqual(1, recPrec.TotalFor(MaterialId.CoreCarapace), "Precision → 1 core even with double-drop on");
        }

        // ── AC-4 edge: unknown kaiju id fails loud ──────────────────────────────────────────────

        [Test]
        public void test_economy_unknown_kaiju_id_throws_and_credits_nothing()
        {
            var config = MakeConfig();
            var themeQuery = MakeThemeQuery();
            var bus = new TypedEventBus();
            var rec = new RecordingSaveService();
            var _ = new EconomyService(config, bus, rec, themeQuery);

            var evt = new PartBroke(
                partId: 1, kaijuId: 999 /* unregistered */, type: PartType.Normal, worldPosition: Vector2.zero,
                dropTableId: 0, quality: BreakQuality.Softened, adjacencyIds: Array.Empty<int>(), isChainBreak: false);

            Assert.Throws<ArgumentException>(() => bus.Publish(evt),
                "An unregistered kaiju must fail loud rather than award a wrong core.");
            Assert.AreEqual(0, rec.CallCount, "No materials are banked when theme resolution fails.");
        }

        // ── AC-5: the PartBroke struct carries no pre-computed yield fields ──────────────────────

        [Test]
        public void test_partbroke_struct_carries_no_precomputed_yield_fields()
        {
            FieldInfo[] fields = typeof(PartBroke).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo f in fields)
            {
                string n = f.Name.ToLowerInvariant();
                Assert.IsFalse(n.Contains("yield"), $"PartBroke must not carry a yield field ('{f.Name}').");
                Assert.IsFalse(n.Contains("shard"), $"PartBroke must not carry a shard field ('{f.Name}').");
                Assert.IsFalse(n.Contains("essence"), $"PartBroke must not carry a material field ('{f.Name}').");
            }
        }

        // ── AC-1 explicit spot checks (readable, non-parametrised) ──────────────────────────────

        [Test]
        public void test_economy_standard_break_yields_base_shard_and_one_theme_core()
        {
            var rec = FireBreak(MakeConfig(), MakeThemeQuery(), KaijuCarapace, BreakQuality.Normal, PartType.Normal);
            Assert.AreEqual(2, rec.CallCount);
            Assert.AreEqual(2, rec.TotalFor(MaterialId.ShardCommon), "floor(2 × 1.0) = 2");
            Assert.AreEqual(1, rec.TotalFor(MaterialId.CoreCarapace));
        }

        [Test]
        public void test_economy_perfect_carapace_break_doubles_core()
        {
            var rec = FireBreak(MakeConfig(), MakeThemeQuery(), KaijuCarapace, BreakQuality.SoftenedStaggered, PartType.Armored);
            Assert.AreEqual(4, rec.TotalFor(MaterialId.ShardCommon), "floor(2 × 2.0) = 4");
            Assert.AreEqual(2, rec.TotalFor(MaterialId.CoreCarapace), "Perfect double-drop");
        }
    }
}
