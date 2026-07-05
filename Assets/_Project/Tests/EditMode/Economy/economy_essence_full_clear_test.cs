using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Economy;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Economy
{
    /// <summary>
    /// Economy Story 002 — Full-Clear Essence Award (material-economy.md §C.2, §D.1, §H.4).
    /// Verifies Economy awards <c>essence_per_full_clear</c> essence + <c>shard_completeness_bonus</c>
    /// shards on a <see cref="HuntEnded"/> with IsAllPartsBroken=true, nothing on a non-full-clear,
    /// and that essence never originates from the per-break (<see cref="PartBroke"/>) path.
    /// </summary>
    [TestFixture]
    public sealed class EconomyEssenceFullClearTests
    {
        private const int EssencePerFullClear = 1;
        private const int CompletenessBonus = 5;
        private const int KaijuCarapace = 10;

        private static EconomyConfig MakeConfig(int essence = EssencePerFullClear, int bonus = CompletenessBonus)
        {
            return ContentTestFactory.Create<EconomyConfig>(
                ("_essencePerFullClear", essence),
                ("_shardCompletenessBonus", bonus));
        }

        private static StubKaijuThemeQuery MakeThemeQuery() =>
            new StubKaijuThemeQuery().Register(KaijuCarapace, KaijuTheme.Carapace);

        /// <summary>Wire a live Economy over a real bus; returns (bus, recorder) so the test can publish.</summary>
        private static (TypedEventBus bus, RecordingSaveService rec) Rig(EconomyConfig config)
        {
            var bus = new TypedEventBus();
            var rec = new RecordingSaveService();
            var _ = new EconomyService(config, bus, rec, MakeThemeQuery(), rec);
            return (bus, rec);
        }

        // ── AC-1: full clear awards essence + completeness shards, once each ─────────────────────

        [Test]
        public void test_economy_full_clear_awards_essence_and_completeness_shards()
        {
            var (bus, rec) = Rig(MakeConfig());

            bus.Publish(new HuntEnded(isAllPartsBroken: true));

            Assert.AreEqual(2, rec.CallCount, "Full clear credits exactly essence + completeness shards.");
            Assert.AreEqual(EssencePerFullClear, rec.TotalFor(MaterialId.EssenceKaiju));
            Assert.AreEqual(CompletenessBonus, rec.TotalFor(MaterialId.ShardCommon));
        }

        [Test]
        public void test_economy_full_clear_essence_amount_is_data_driven()
        {
            // essence_per_full_clear = 2 (G.1 safe-range max) → 2 essence, no code change.
            var (bus, rec) = Rig(MakeConfig(essence: 2, bonus: 7));

            bus.Publish(new HuntEnded(isAllPartsBroken: true));

            Assert.AreEqual(2, rec.TotalFor(MaterialId.EssenceKaiju), "Essence award follows the SO value.");
            Assert.AreEqual(7, rec.TotalFor(MaterialId.ShardCommon), "Completeness bonus follows the SO value.");
        }

        // ── AC-2: non-full-clear awards nothing from the hunt-end handler ────────────────────────

        [Test]
        public void test_economy_non_full_clear_awards_nothing()
        {
            var (bus, rec) = Rig(MakeConfig());

            bus.Publish(new HuntEnded(isAllPartsBroken: false));

            Assert.AreEqual(0, rec.CallCount, "A non-full-clear hunt end grants no settlement reward.");
        }

        [Test]
        public void test_economy_hunt_end_has_no_cross_call_accumulation()
        {
            // Firing twice (e.g. restart edge) — each call is evaluated independently.
            var (bus, rec) = Rig(MakeConfig());

            bus.Publish(new HuntEnded(isAllPartsBroken: false)); // nothing
            bus.Publish(new HuntEnded(isAllPartsBroken: true));  // one full award
            bus.Publish(new HuntEnded(isAllPartsBroken: false)); // nothing

            Assert.AreEqual(EssencePerFullClear, rec.TotalFor(MaterialId.EssenceKaiju),
                "Only the single full-clear invocation awards essence.");
            Assert.AreEqual(CompletenessBonus, rec.TotalFor(MaterialId.ShardCommon));
        }

        // ── AC-3: essence never originates from the per-break handler ───────────────────────────

        [Test]
        public void test_economy_part_break_never_awards_essence()
        {
            var (bus, rec) = Rig(MakeConfig());

            // Fire a break at every quality — none may ever credit essence.
            foreach (BreakQuality q in new[] { BreakQuality.Normal, BreakQuality.Softened, BreakQuality.SoftenedStaggered })
            {
                bus.Publish(new PartBroke(
                    partId: 1, kaijuId: KaijuCarapace, type: PartType.BossCore, worldPosition: Vector2.zero,
                    dropTableId: 0, quality: q, adjacencyIds: Array.Empty<int>(), isChainBreak: false));
            }

            Assert.AreEqual(0, rec.TotalFor(MaterialId.EssenceKaiju),
                "Essence is a hunt-end reward only — never dropped per part break.");
        }
    }
}
