using KaijuBreaker.Core;
using KaijuBreaker.Meta;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Meta
{
    /// <summary>
    /// UtilityUpgrades — the meta layer buys UTILITY (faster fire / higher drop rate) with ShardCommon,
    /// persisted via ISaveService flags. These assert the buy/spend/level/mult behaviour deterministically
    /// against the in-memory RecordingSaveService (no killing-power effect — that is the in-run system).
    /// </summary>
    public sealed class UtilityUpgradesTest
    {
        private static UtilityUpgrades Make(int shards, out RecordingSaveService save)
        {
            save = new RecordingSaveService();
            save.Seed(MaterialId.ShardCommon, shards);
            return new UtilityUpgrades(save);
        }

        [Test]
        public void test_level0_gives_identity_multipliers()
        {
            var u = Make(0, out _);
            Assert.AreEqual(0, u.FireRateLevel);
            Assert.AreEqual(0, u.DropRateLevel);
            Assert.AreEqual(1f, u.FireIntervalMult, 1e-4f);
            Assert.AreEqual(1f, u.DropRateMult, 1e-4f);
        }

        [Test]
        public void test_buy_fire_rate_spends_shards_and_raises_level_and_speeds_fire()
        {
            var u = Make(100, out var save);
            int cost = u.CostFor(0);

            Assert.IsTrue(u.BuyFireRate());
            Assert.AreEqual(1, u.FireRateLevel);
            Assert.AreEqual(100 - cost, save.GetMaterialCount(MaterialId.ShardCommon));
            Assert.Less(u.FireIntervalMult, 1f); // faster fire = lower interval
        }

        [Test]
        public void test_buy_fails_when_too_few_shards()
        {
            var u = Make(0, out var save);
            Assert.IsFalse(u.BuyFireRate());
            Assert.AreEqual(0, u.FireRateLevel);
            Assert.AreEqual(0, save.GetMaterialCount(MaterialId.ShardCommon));
        }

        [Test]
        public void test_drop_rate_raises_multiplier_above_one()
        {
            var u = Make(1000, out _);
            Assert.IsTrue(u.BuyDropRate());
            Assert.AreEqual(1, u.DropRateLevel);
            Assert.Greater(u.DropRateMult, 1f);
        }

        [Test]
        public void test_cannot_buy_past_max_level()
        {
            var u = Make(100000, out _);
            for (int i = 0; i < UtilityUpgrades.MaxLevel; i++)
                Assert.IsTrue(u.BuyFireRate(), "buy level " + (i + 1));

            Assert.AreEqual(UtilityUpgrades.MaxLevel, u.FireRateLevel);
            Assert.IsFalse(u.BuyFireRate(), "should not buy past max");
        }

        [Test]
        public void test_fire_and_drop_tracks_are_independent()
        {
            var u = Make(1000, out _);
            u.BuyFireRate();
            Assert.AreEqual(1, u.FireRateLevel);
            Assert.AreEqual(0, u.DropRateLevel); // buying fire did not touch drop
        }
    }
}
