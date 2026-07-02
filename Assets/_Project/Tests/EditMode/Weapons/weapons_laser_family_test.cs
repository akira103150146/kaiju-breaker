using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Weapons;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 004 — Laser family base firing: L1 Spread, L2 Focus Beam, L4 Pierce Beam
    /// (Tier 0-2 behavior; Tier-3 unique mechanics are covered separately in
    /// weapons_laser_tier3_test.cs per Story 008). Verifies AC-1..AC-4 from
    /// production/epics/weapons/story-004-laser-family-firing.md against
    /// design/gdd/weapon-system.md §C.4 / §D.2.
    /// </summary>
    public sealed class WeaponsLaserFamilyTests
    {
        // ── Fixtures ─────────────────────────────────────────────────────────────

        private static WeaponBalanceConfig Balance() =>
            ContentTestFactory.Create<WeaponBalanceConfig>();

        private static PartSystemConfig PartSystem() =>
            ContentTestFactory.Create<PartSystemConfig>();

        private static WeaponDef L1Def() =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.L1), ("_type", WeaponType.Laser),
                ("_l1HRateFull", 25.0f), ("_l1HRateCenter", 8.3f));

        private static WeaponDef L2Def() =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.L2), ("_type", WeaponType.Laser),
                ("_l2HRate", 37.5f));

        private static WeaponDef L4Def(float fireInterval = 0.4f, float hRate = 25.0f) =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.L4), ("_type", WeaponType.Laser),
                ("_l4FireInterval", fireInterval), ("_l4HRate", hRate));

        private static L1SpreadLaser MakeL1(IEventBus bus) =>
            new L1SpreadLaser(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), L1Def(),
                new ResidualHeatTracker(bus));

        private static L2FocusBeam MakeL2(IEventBus bus) =>
            new L2FocusBeam(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), L2Def(), PartSystem());

        private static L4PierceBeam MakeL4(IEventBus bus, float fireInterval = 0.4f, float hRate = 25.0f) =>
            new L4PierceBeam(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(),
                L4Def(fireInterval, hRate), new ResidualHeatTracker(bus));

        // ── AC-1: L1 三束全中 heat_delta 正確 ─────────────────────────────────────

        [Test]
        public void test_l1_all_three_beams_hit_same_part_splits_full_rate_across_beams()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l1 = MakeL1(bus);

            // Act — all 3 beam slots resolve to partId=1
            l1.FireFrame(deltaTime: 0.016f, kaijuId: 0, beamHitPartIds: new[] { 1, 1, 1 });

            // Assert
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(3));
            float sum = 0f;
            foreach (var hit in bus.Events<LaserHit>())
            {
                Assert.That(hit.PartId, Is.EqualTo(1));
                Assert.That(hit.HeatDelta, Is.EqualTo(25.0f * 0.016f / 3f).Within(1e-4f));
                sum += hit.HeatDelta;
            }
            // 3 beams on the same part sum back to the full rate over the frame (25 HU/s × 0.016 s).
            Assert.That(sum, Is.EqualTo(25.0f * 0.016f).Within(1e-3f));
        }

        [Test]
        public void test_l1_center_beam_only_hit_uses_center_rate()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l1 = MakeL1(bus);

            // Act — center hits, both side beams miss (-1)
            l1.FireFrame(deltaTime: 0.016f, kaijuId: 0, beamHitPartIds: new[] { 1, -1, -1 });

            // Assert
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
            var hit = bus.Events<LaserHit>()[0];
            Assert.That(hit.PartId, Is.EqualTo(1));
            Assert.That(hit.HeatDelta, Is.EqualTo(8.3f * 0.016f).Within(1e-4f));
        }

        [Test]
        public void test_l1_all_beams_miss_emits_nothing()
        {
            var bus = new RecordingEventBus();
            var l1 = MakeL1(bus);

            l1.FireFrame(deltaTime: 0.016f, kaijuId: 0, beamHitPartIds: new[] { -1, -1, -1 });

            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(0));
        }

        // ── AC-2: L2 僅命中時發 LaserHit（邊緣硬截止）──────────────────────────────

        [Test]
        public void test_l2_hold_with_valid_target_emits_single_laserhit()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l2 = MakeL2(bus);

            // Act
            l2.FireFrame(deltaTime: 0.016f, kaijuId: 0, hold: true, targetPartId: 1);

            // Assert
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
            var hit = bus.Events<LaserHit>()[0];
            Assert.That(hit.PartId, Is.EqualTo(1));
            Assert.That(hit.HeatDelta, Is.EqualTo(37.5f * 0.016f).Within(1e-4f));
        }

        [Test]
        public void test_l2_hold_false_emits_nothing_even_with_valid_target()
        {
            var bus = new RecordingEventBus();
            var l2 = MakeL2(bus);

            l2.FireFrame(deltaTime: 0.016f, kaijuId: 0, hold: false, targetPartId: 1);

            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(0));
        }

        [Test]
        public void test_l2_hold_true_but_no_resolved_target_emits_nothing()
        {
            // Edge-target-outside-narrow-collider is represented by the caller never resolving a hit (-1).
            var bus = new RecordingEventBus();
            var l2 = MakeL2(bus);

            l2.FireFrame(deltaTime: 0.016f, kaijuId: 0, hold: true, targetPartId: -1);

            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(0));
        }

        // ── AC-3: L4 穿透雷射對路徑上 N 個部位各自發 LaserHit ─────────────────────

        [Test]
        public void test_l4_pierce_fires_laserhit_per_pierced_part_at_interval()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l4 = MakeL4(bus, fireInterval: 0.4f, hRate: 25.0f);

            // Act — a single UpdateFrame call whose deltaTime exactly reaches the fire interval
            bool fired = l4.UpdateFrame(deltaTime: 0.4f, kaijuId: 0, piercedPartIds: new List<int> { 1, 2 });

            // Assert
            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(2));
            foreach (var hit in bus.Events<LaserHit>())
                Assert.That(hit.HeatDelta, Is.EqualTo(25.0f * 0.4f).Within(1e-4f));
        }

        [Test]
        public void test_l4_pierce_truncates_to_eight_parts_when_path_has_more()
        {
            var bus = new RecordingEventBus();
            var l4 = MakeL4(bus, fireInterval: 0.4f, hRate: 25.0f);

            var nineParts = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            l4.UpdateFrame(deltaTime: 0.4f, kaijuId: 0, piercedPartIds: nineParts);

            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(8));
            var hitPartIds = new List<int>();
            foreach (var hit in bus.Events<LaserHit>()) hitPartIds.Add(hit.PartId);
            Assert.That(hitPartIds, Is.EqualTo(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 }));
        }

        // ── AC-4: 幀率無關（frame-rate independence）──────────────────────────────

        [Test]
        public void test_l4_accumulates_deltatime_across_frames_and_fires_once_at_interval()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l4 = MakeL4(bus, fireInterval: 0.4f, hRate: 25.0f);
            var parts = new List<int> { 1, 2 };

            // Act — 4 frames of 0.1s; only the 4th (total 0.4s) should fire
            Assert.That(l4.UpdateFrame(0.1f, 0, parts), Is.False);
            Assert.That(l4.UpdateFrame(0.1f, 0, parts), Is.False);
            Assert.That(l4.UpdateFrame(0.1f, 0, parts), Is.False);
            bool firedOnFourth = l4.UpdateFrame(0.1f, 0, parts);

            // Assert
            Assert.That(firedOnFourth, Is.True);
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(2));
            foreach (var hit in bus.Events<LaserHit>())
                Assert.That(hit.HeatDelta, Is.EqualTo(25.0f * 0.4f).Within(1e-4f));
        }

        [Test]
        public void test_l4_large_overshoot_deltatime_fires_only_once_no_multifire()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l4 = MakeL4(bus, fireInterval: 0.4f, hRate: 25.0f);
            var parts = new List<int> { 1 };

            // Act — a single deltaTime bigger than the interval (0.5s > 0.4s)
            bool fired = l4.UpdateFrame(deltaTime: 0.5f, kaijuId: 0, piercedPartIds: parts);

            // Assert — exactly one volley, not two
            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
        }
    }
}
