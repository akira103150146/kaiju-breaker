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

        // ── AC-2: M2 Tier-3 fires two 6-bursts, 1s apart, cooldown ignores TryFire ─

        [Test]
        public void test_m2_tier3_fires_first_burst_of_six_immediately()
        {
            var bus = new RecordingEventBus();
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M2, 3);
            var m2 = new M2SwarmLauncher(bus, tier, new StubPartStateQuery(), Balance(), M2Def());
            var hits = new List<int> { 1, 1, 1, 1, 1, 1 };

            bool fired = m2.TryFire(hits, kaijuId: 0);

            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(6));
            Assert.That(m2.Ammo, Is.EqualTo(6), "half of the 12-round Tier-3 magazine remains for burst B");
            Assert.That(m2.IsBurstCoolingDown, Is.True);
        }

        [Test]
        public void test_m2_tier3_second_burst_fires_automatically_after_cooldown()
        {
            var bus = new RecordingEventBus();
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M2, 3);
            var m2 = new M2SwarmLauncher(bus, tier, new StubPartStateQuery(), Balance(), M2Def());
            m2.TryFire(new List<int> { 1, 1, 1, 1, 1, 1 }, kaijuId: 0);

            m2.Tick(1.0f);

            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(12));
            Assert.That(m2.IsBurstCoolingDown, Is.False);
            Assert.That(m2.IsReloading, Is.True, "magazine empty after both bursts, reload begins");
        }

        [Test]
        public void test_m2_tier3_tryfire_during_burst_cooldown_is_ignored()
        {
            var bus = new RecordingEventBus();
            var tier = new StubWeaponTierQuery().SetTier(WeaponId.M2, 3);
            var m2 = new M2SwarmLauncher(bus, tier, new StubPartStateQuery(), Balance(), M2Def());
            m2.TryFire(new List<int> { 1, 1, 1, 1, 1, 1 }, kaijuId: 0);

            bool secondCallDuringCooldown = m2.TryFire(new List<int> { 2, 2, 2, 2, 2, 2 }, kaijuId: 0);

            Assert.That(secondCallDuringCooldown, Is.False, "TryFire during the inter-burst cooldown is a no-op");
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(6), "the ignored call must not add hits or interrupt the pending burst");

            m2.Tick(1.0f);

            var allHits = bus.Events<MissileHit>();
            Assert.That(allHits.Count, Is.EqualTo(12));
            for (int i = 6; i < 12; i++)
                Assert.That(allHits[i].PartId, Is.EqualTo(1), "burst B replays the ORIGINAL cached hit list, not the ignored call's");
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
