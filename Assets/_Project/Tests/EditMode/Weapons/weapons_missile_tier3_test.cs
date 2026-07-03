using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Weapons;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 009 — Missile Tier-3 unique mechanics (M1/M2/M4). M3's Tier-3 "AP Chain" case is
    /// REPLACED here with a negative assertion per the orchestrator's decision: the chain mechanic
    /// is fully owned by KaijuParts (<c>PartStateSystem.OnPartBroke → ApplyM3Chain</c>) to avoid
    /// double-counting break fill — <see cref="M3ApTorpedo"/> must never subscribe to
    /// <see cref="PartBroke"/> for chaining and must emit zero extra <see cref="MissileHit"/>s when
    /// a neighbour part breaks (weapon-system.md C.5 M1/M2/M4 Tier-3, G.3).
    /// </summary>
    public sealed class WeaponsMissileTier3Tests
    {
        // ── Fixtures ─────────────────────────────────────────────────────────────

        private static WeaponBalanceConfig Balance() =>
            ContentTestFactory.Create<WeaponBalanceConfig>();

        private static WeaponDef M1Def() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.M1), ("_type", WeaponType.Missile));

        private static WeaponDef M2Def() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.M2), ("_type", WeaponType.Missile));

        private static WeaponDef M3Def() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.M3), ("_type", WeaponType.Missile));

        private static WeaponDef M4Def() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.M4), ("_type", WeaponType.Missile));

        // ── AC-1: M1 Tier-3 third missile locks the hottest alive part ───────────

        [Test]
        public void test_m1_tier3_third_missile_locks_hottest_alive_part()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, currentHeat: 80f);
            parts.Configure(2, currentHeat: 40f);
            parts.Configure(3, currentHeat: 90f);
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M1, 3);
            var m1 = new M1HomingMissile(bus, tier, parts, Balance(), M1Def());

            // Act
            bool fired = m1.TryFire(targetPartId: 1, kaijuId: 0);

            // Assert
            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(3));
            var hits = bus.Events<MissileHit>();
            Assert.That(hits[0].PartId, Is.EqualTo(1), "first missile tracks the passed-in target");
            Assert.That(hits[1].PartId, Is.EqualTo(1), "second missile tracks the passed-in target");
            Assert.That(hits[2].PartId, Is.EqualTo(3), "third missile locks the hottest alive part");
        }

        [Test]
        public void test_m1_tier3_consumes_three_missiles_from_magazine()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, currentHeat: 10f);
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M1, 3);
            var m1 = new M1HomingMissile(bus, tier, parts, Balance(), M1Def());

            m1.TryFire(1, 0);

            Assert.That(m1.Ammo, Is.EqualTo(3), "6-round magazine minus a 3-missile Tier-3 shot");
        }

        [Test]
        public void test_m1_tier3_skips_third_missile_when_no_part_alive()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery(); // no parts registered -> GetHottestAlivePartId() = -1
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M1, 3);
            var m1 = new M1HomingMissile(bus, tier, parts, Balance(), M1Def());

            bool fired = m1.TryFire(targetPartId: 1, kaijuId: 0);

            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(2), "third missile is skipped when no part is alive to lock");
        }

        // ── AC-2: M2 Tier-3 "飽和點名" — same 3×8 numbers, redirect onto hottest softened part ─

        [Test]
        public void test_m2_tier3_saturation_redirects_salvo_onto_hottest_softened_part()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, heat: HeatState.Intact, currentHeat: 10f);          // the passed target
            parts.Configure(2, heat: HeatState.Softened, currentHeat: 90f);        // hottest softened
            parts.Configure(3, heat: HeatState.Softened, currentHeat: 50f);
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M2, 3);
            var m2 = new M2SwarmLauncher(bus, tier, parts, Balance(), M2Def());

            // Scene shell resolved the salvo onto part 1, but saturation callout redirects to part 2.
            bool fired = m2.TryFire(new List<int> { 1, 1, 1, 1, 1, 1, 1, 1 }, kaijuId: 0);

            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(8), "same salvo size — Tier-3 changes targeting, not numbers");
            foreach (var hit in bus.Events<MissileHit>())
            {
                Assert.That(hit.PartId, Is.EqualTo(2), "all micros saturate the hottest softened part");
                Assert.That(hit.BreakDeltaBase, Is.EqualTo(1.25f).Within(1e-3f), "per-missile break identical to base tier");
            }
        }

        [Test]
        public void test_m2_tier3_falls_back_to_passed_targets_when_nothing_softened()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, heat: HeatState.Intact, currentHeat: 10f); // nothing softened
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M2, 3);
            var m2 = new M2SwarmLauncher(bus, tier, parts, Balance(), M2Def());

            m2.TryFire(new List<int> { 1, 1, 1, 1, 1, 1, 1, 1 }, kaijuId: 0);

            foreach (var hit in bus.Events<MissileHit>())
                Assert.That(hit.PartId, Is.EqualTo(1), "no softened part → use the shell-resolved targets");
        }

        // Chain Hive mag (salvoCount×microCount = 24) and per-missile break are identical at Tier 0
        // and Tier 3 → Sustained_Output is tier-invariant by construction (equal power / H.7).
        private static int DrainFullBurstHitCount(int tier)
        {
            var bus = new RecordingEventBus();
            var tierQuery = new StubWeaponTierQuery().SetTier(WeaponId.M2, tier);
            var m2 = new M2SwarmLauncher(bus, tierQuery, new StubPartStateQuery(), Balance(), M2Def());
            m2.TryFire(new List<int> { 9, 9, 9, 9, 9, 9, 9, 9 }, kaijuId: 0);
            m2.Tick(0.8f);
            m2.Tick(0.8f);
            return bus.CountOf<MissileHit>();
        }

        [Test]
        public void test_m2_tier3_fires_same_missile_count_as_tier0_equal_power()
        {
            Assert.That(DrainFullBurstHitCount(0), Is.EqualTo(24));
            Assert.That(DrainFullBurstHitCount(3), Is.EqualTo(24),
                "Tier-3 saturation changes targeting only — same missile count = equal power");
        }

        // ── AC-3: M3 negative assertion — no chaining in Weapons (owned by KaijuParts) ─

        [Test]
        public void test_m3_tier3_does_not_subscribe_to_partbroke_for_chaining()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M3, 3);
            var m3 = new M3ApTorpedo(bus, tier, parts, Balance(), M3Def());
            m3.Enable(); // even enabled (subscribes only to the inherited ClearCollider hook)

            bus.Publish(new PartBroke(1, 0, PartType.Normal, default, 1, BreakQuality.Softened,
                new[] { 2, 3, 4 }, false));

            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(0),
                "M3ApTorpedo must not emit chain MissileHits on a neighbour break — that mechanic is " +
                "owned entirely by KaijuParts.PartStateSystem.ApplyM3Chain (story-009 out-of-scope note)");
        }

        // ── AC-4: M4 Tier-3 splits into 6 children at 2 BU each ───────────────────

        [Test]
        public void test_m4_tier3_cluster_split_emits_six_child_hits()
        {
            var bus = new RecordingEventBus();
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M4, 3);
            var m4 = new M4ClusterBomb(bus, tier, new StubPartStateQuery(), Balance(), M4Def());
            var children = new List<int> { 1, 2, 3, 4, 5, 6 };

            bool fired = m4.TryFire(children, kaijuId: 0);

            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(6));
            // Each child in BU: M4T3ChildDmgPct(0.2) × BuPerD0(10) = 2 BU (6 children = 1.2×D₀).
            foreach (var hit in bus.Events<MissileHit>())
                Assert.That(hit.BreakDeltaBase, Is.EqualTo(2f).Within(1e-3f));
        }

        [Test]
        public void test_m4_tier_below_three_still_uses_aoe_formula_not_children()
        {
            var bus = new RecordingEventBus();
            var m4 = new M4ClusterBomb(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M4Def());

            m4.TryFire(new List<int> { 1 }, kaijuId: 0);

            var hit = bus.Events<MissileHit>()[0];
            Assert.That(hit.BreakDeltaBase, Is.EqualTo(20f).Within(1e-3f), // N=1: 2×D₀ × BuPerD0
                "Tier < 3 must keep using the N=1 AoE formula, not the Tier-3 child split");
        }
    }
}
