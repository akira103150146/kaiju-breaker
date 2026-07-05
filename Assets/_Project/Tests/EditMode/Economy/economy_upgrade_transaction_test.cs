using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Economy;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Economy
{
    /// <summary>
    /// Economy Story 004 — Tier 0→3 Upgrade Transaction (material-economy.md §C.3/§C.4/§D.2).
    /// Verifies <see cref="EconomyService.TryUpgrade"/> is atomic + one-way: it succeeds only at the exact
    /// from-tier with all costs affordable, deducts shards + weapon-theme core + essence, advances the tier
    /// (visible same-frame via IWeaponTierQuery), and on any failed check deducts nothing and leaves the tier.
    /// All costs are read from <see cref="EconomyConfig"/>.
    /// </summary>
    [TestFixture]
    public sealed class EconomyUpgradeTransactionTests
    {
        private static EconomyConfig MakeConfig(params (string field, object value)[] overrides)
            => ContentTestFactory.Create<EconomyConfig>(overrides);

        /// <summary>Economy over a real bus with an in-memory player store (materials + tiers).</summary>
        private static (EconomyService svc, RecordingSaveService store) Rig(EconomyConfig config)
        {
            var bus = new TypedEventBus();
            var store = new RecordingSaveService();
            var svc = new EconomyService(config, bus, store, new StubKaijuThemeQuery(), store);
            return (svc, store);
        }

        // Weapon → theme core identity (material-economy.md §C.1).
        private static readonly Dictionary<WeaponId, MaterialId> ExpectedCore = new Dictionary<WeaponId, MaterialId>
        {
            { WeaponId.L1, MaterialId.CoreCarapace }, { WeaponId.M2, MaterialId.CoreCarapace }, { WeaponId.M4, MaterialId.CoreCarapace },
            { WeaponId.L2, MaterialId.CoreLimb },     { WeaponId.L4, MaterialId.CoreLimb },     { WeaponId.M1, MaterialId.CoreLimb },
            { WeaponId.L3, MaterialId.CoreEnergy },   { WeaponId.M3, MaterialId.CoreEnergy },
        };

        // ── AC-1: T0→1 succeeds with exactly enough shards, fails with one fewer ────────────────

        [Test]
        public void test_upgrade_tier0to1_succeeds_with_exact_shards()
        {
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L1, 0).Seed(MaterialId.ShardCommon, 8);

            bool ok = svc.TryUpgrade(WeaponId.L1, TierTransition.Tier0To1);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, store.GetTier(WeaponId.L1), "Tier advances to 1 in the same frame.");
            Assert.AreEqual(8, store.SpentFor(MaterialId.ShardCommon));
            Assert.AreEqual(0, store.SpentFor(MaterialId.CoreCarapace), "No core at Tier 0→1.");
            Assert.AreEqual(0, store.SpentFor(MaterialId.EssenceKaiju), "No essence at Tier 0→1.");
            Assert.AreEqual(0, store.GetMaterialCount(MaterialId.ShardCommon), "Shards fully deducted.");
        }

        [Test]
        public void test_upgrade_tier0to1_fails_with_one_fewer_shard()
        {
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L1, 0).Seed(MaterialId.ShardCommon, 7);

            bool ok = svc.TryUpgrade(WeaponId.L1, TierTransition.Tier0To1);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, store.SpendCount, "No deduction on failure.");
            Assert.AreEqual(0, store.GetTier(WeaponId.L1), "Tier unchanged.");
            Assert.AreEqual(7, store.GetMaterialCount(MaterialId.ShardCommon));
        }

        // ── AC-2: T1→2 needs the correct weapon-theme core; all 8 weapon→core mappings ──────────

        private static IEnumerable<TestCaseData> AllWeapons()
        {
            foreach (WeaponId w in ExpectedCore.Keys)
                yield return new TestCaseData(w).SetName($"Weapon_{w}_core_binding");
        }

        [TestCaseSource(nameof(AllWeapons))]
        public void test_upgrade_tier1to2_uses_correct_core_per_weapon(WeaponId weapon)
        {
            MaterialId core = ExpectedCore[weapon];
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(weapon, 1).Seed(MaterialId.ShardCommon, 12).Seed(core, 5);

            bool ok = svc.TryUpgrade(weapon, TierTransition.Tier1To2);

            Assert.IsTrue(ok, $"{weapon} T1→2 should succeed with 12 shards + 5 {core}.");
            Assert.AreEqual(2, store.GetTier(weapon));
            Assert.AreEqual(12, store.SpentFor(MaterialId.ShardCommon));
            Assert.AreEqual(5, store.SpentFor(core), $"{weapon} must consume {core}.");
        }

        [Test]
        public void test_upgrade_tier1to2_fails_when_only_wrong_core_owned()
        {
            // L2 needs core_limb; player has carapace instead.
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L2, 1).Seed(MaterialId.ShardCommon, 12)
                 .Seed(MaterialId.CoreCarapace, 5).Seed(MaterialId.CoreLimb, 0);

            bool ok = svc.TryUpgrade(WeaponId.L2, TierTransition.Tier1To2);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, store.SpendCount, "Atomic failure — no shard nor core deducted.");
            Assert.AreEqual(1, store.GetTier(WeaponId.L2));
        }

        // ── AC-3: T2→3 needs shards + core + essence ────────────────────────────────────────────

        [Test]
        public void test_upgrade_tier2to3_consumes_shards_core_and_essence()
        {
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L3, 2)
                 .Seed(MaterialId.ShardCommon, 25).Seed(MaterialId.CoreEnergy, 8).Seed(MaterialId.EssenceKaiju, 1);

            bool ok = svc.TryUpgrade(WeaponId.L3, TierTransition.Tier2To3);

            Assert.IsTrue(ok);
            Assert.AreEqual(3, store.GetTier(WeaponId.L3));
            Assert.AreEqual(25, store.SpentFor(MaterialId.ShardCommon));
            Assert.AreEqual(8, store.SpentFor(MaterialId.CoreEnergy));
            Assert.AreEqual(1, store.SpentFor(MaterialId.EssenceKaiju));
        }

        [Test]
        public void test_upgrade_tier2to3_fails_without_essence()
        {
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L3, 2)
                 .Seed(MaterialId.ShardCommon, 25).Seed(MaterialId.CoreEnergy, 8).Seed(MaterialId.EssenceKaiju, 0);

            bool ok = svc.TryUpgrade(WeaponId.L3, TierTransition.Tier2To3);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, store.SpendCount, "No deduction when essence is missing.");
            Assert.AreEqual(2, store.GetTier(WeaponId.L3));
        }

        [Test]
        public void test_upgrade_tier2to3_spends_exactly_one_of_excess_essence()
        {
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.M3, 2)
                 .Seed(MaterialId.ShardCommon, 25).Seed(MaterialId.CoreEnergy, 8).Seed(MaterialId.EssenceKaiju, 2);

            bool ok = svc.TryUpgrade(WeaponId.M3, TierTransition.Tier2To3);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, store.SpentFor(MaterialId.EssenceKaiju), "Exactly one essence spent.");
            Assert.AreEqual(1, store.GetMaterialCount(MaterialId.EssenceKaiju), "Remaining essence preserved.");
        }

        // ── AC-4: cannot skip tiers or re-upgrade ───────────────────────────────────────────────

        [Test]
        public void test_upgrade_cannot_skip_tier()
        {
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L1, 0) // at tier 0, attempt T1→2
                 .Seed(MaterialId.ShardCommon, 999).Seed(MaterialId.CoreCarapace, 999).Seed(MaterialId.EssenceKaiju, 999);

            bool ok = svc.TryUpgrade(WeaponId.L1, TierTransition.Tier1To2);

            Assert.IsFalse(ok, "Cannot take T1→2 while at tier 0.");
            Assert.AreEqual(0, store.SpendCount);
            Assert.AreEqual(0, store.GetTier(WeaponId.L1));
        }

        [Test]
        public void test_upgrade_cannot_re_upgrade_past_from_tier()
        {
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L1, 1) // already at tier 1, attempt T0→1 again
                 .Seed(MaterialId.ShardCommon, 999);

            bool ok = svc.TryUpgrade(WeaponId.L1, TierTransition.Tier0To1);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, store.SpendCount);
            Assert.AreEqual(1, store.GetTier(WeaponId.L1));
        }

        [Test]
        public void test_upgrade_at_max_tier_returns_false()
        {
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L1, 3) // maxed; no T3→4 exists, T2→3 from-tier mismatches
                 .Seed(MaterialId.ShardCommon, 999).Seed(MaterialId.CoreCarapace, 999).Seed(MaterialId.EssenceKaiju, 999);

            Assert.IsFalse(svc.TryUpgrade(WeaponId.L1, TierTransition.Tier2To3));
            Assert.AreEqual(0, store.SpendCount);
            Assert.AreEqual(3, store.GetTier(WeaponId.L1));
        }

        // ── AC-5: partial affordability is atomic — no partial deduction ────────────────────────

        [Test]
        public void test_upgrade_partial_affordability_is_atomic()
        {
            // L2 T1→2: shards sufficient (12), core_limb insufficient (3 of 5).
            var (svc, store) = Rig(MakeConfig());
            store.SeedTier(WeaponId.L2, 1).Seed(MaterialId.ShardCommon, 12).Seed(MaterialId.CoreLimb, 3);

            bool ok = svc.TryUpgrade(WeaponId.L2, TierTransition.Tier1To2);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, store.SpendCount, "Sufficient shards must NOT be spent when the core is short.");
            Assert.AreEqual(12, store.GetMaterialCount(MaterialId.ShardCommon));
            Assert.AreEqual(3, store.GetMaterialCount(MaterialId.CoreLimb));
            Assert.AreEqual(1, store.GetTier(WeaponId.L2));
        }

        // ── AC-7: cost values are data-driven from EconomyConfig ─────────────────────────────────

        [Test]
        public void test_upgrade_cost_is_config_driven()
        {
            // Non-default T0→1 cost of 4 (not the GDD default 8).
            var (svc, store) = Rig(MakeConfig(("_weaponUpgradeCostT0ToT1", 4)));
            store.SeedTier(WeaponId.L1, 0).Seed(MaterialId.ShardCommon, 4);

            bool ok = svc.TryUpgrade(WeaponId.L1, TierTransition.Tier0To1);

            Assert.IsTrue(ok, "Upgrade uses the config cost (4), not the hard-coded GDD default (8).");
            Assert.AreEqual(4, store.SpentFor(MaterialId.ShardCommon));
        }
    }
}
