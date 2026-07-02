using NUnit.Framework;
using KaijuBreaker.Content;
using KaijuBreaker.Tests.EditMode.Helpers;

namespace KaijuBreaker.Tests.EditMode.Content
{
    /// <summary>
    /// content-config — verifies SO defaults match the GDDs and that the test-fixture
    /// factory (story 009) can create + override config in-memory.
    /// </summary>
    public sealed class ContentConfigTests
    {
        [Test]
        public void EconomyConfig_DefaultShardBase_MatchesGdd()
        {
            // material-economy.md §G.1/D.3 shard_base = 2 (CreateInstance runs field initializers).
            var econ = ContentTestFactory.Create<EconomyConfig>();
            Assert.AreEqual(2, econ.ShardYieldBase);
            Assert.AreEqual(1.5f, econ.ShardYieldSoftenedMult, 0.0001f);
            Assert.AreEqual(2.0f, econ.ShardYieldSoftenedStaggeredMult, 0.0001f);
        }

        [Test]
        public void ContentTestFactory_AppliesFieldOverrides()
        {
            var econ = ContentTestFactory.Create<EconomyConfig>(
                ("_shardYieldBase", 5),
                ("_eliteShardBonus", 9));

            Assert.AreEqual(5, econ.ShardYieldBase);
            Assert.AreEqual(9, econ.EliteShardBonus);
        }

        [Test]
        public void ContentTestFactory_UnknownField_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => ContentTestFactory.Create<EconomyConfig>(("_notAField", 1)));
        }

        [Test]
        public void GameFeelConfig_CanBeInstantiated_WithDefaults()
        {
            // Smoke: the SO constructs and its initializers run (no missing-type / compile issues).
            var feel = ContentTestFactory.Create<GameFeelConfig>();
            Assert.IsNotNull(feel);
        }
    }
}
