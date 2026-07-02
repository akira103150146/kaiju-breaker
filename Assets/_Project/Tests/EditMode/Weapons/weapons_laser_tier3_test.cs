using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Weapons;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 008 — Laser Tier-3 unique mechanics (L1 residual flame, L2 break ripple, L3
    /// resonance diffusion, L4 afterimage) + the E.6 "take the max, don't sum" overlap
    /// guardrail. Verifies AC-1..AC-4 from
    /// production/epics/weapons/story-008-laser-tier3-mechanics.md against
    /// design/gdd/weapon-system.md §C.4 Tier-3 rows / §E.6.
    /// </summary>
    public sealed class WeaponsLaserTier3Tests
    {
        private const int KaijuId = 0;

        // ── Fixtures ─────────────────────────────────────────────────────────────

        private static WeaponBalanceConfig Balance() =>
            ContentTestFactory.Create<WeaponBalanceConfig>(("_huPerD0", 25.0f));

        private static WeaponDef L1Def() =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.L1), ("_type", WeaponType.Laser),
                ("_l1HRateFull", 25.0f), ("_l1HRateCenter", 8.3f),
                ("_l1T3ResidualRateMult", 0.40f), ("_l1T3ResidualDuration", 1.5f));

        private static WeaponDef L2Def() =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.L2), ("_type", WeaponType.Laser),
                ("_l2HRate", 37.5f));

        private static WeaponDef L3Def() =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.L3), ("_type", WeaponType.Laser),
                ("_l3TapOutputMult", 0.60f), ("_l3ChargeTime", 1.5f),
                ("_l3ChargeOutputMult", 2.50f), ("_l3ChargeCooldown", 2.0f),
                ("_l3T3HeatInjectPct", 0.50f));

        private static WeaponDef L4Def() =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.L4), ("_type", WeaponType.Laser),
                ("_l4FireInterval", 0.4f), ("_l4HRate", 25.0f),
                ("_l4T3AfterimageRateMult", 0.40f), ("_l4T3AfterimageRateMultDuration", 2.0f));

        private static PartSystemConfig PartSystem(float adjacentHeatPct = 0.30f) =>
            ContentTestFactory.Create<PartSystemConfig>(("_l2T3AdjacentHeatPct", adjacentHeatPct));

        // ── AC-1: L1 T3 殘熱焰在 1.5s 內持續發 LaserHit ────────────────────────────

        [Test]
        public void test_l1_t3_residual_flame_ticks_for_full_duration_then_stops()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var tracker = new ResidualHeatTracker(bus);
            var tierQuery = new StubWeaponTierQuery().SetTier(WeaponId.L1, 3);
            var l1 = new L1SpreadLaser(bus, tierQuery, new StubPartStateQuery(), Balance(), L1Def(), tracker);

            // Act — a beam lands on part 1 (registers the residual timer as a side effect)
            l1.FireFrame(deltaTime: 0.016f, kaijuId: KaijuId, beamHitPartIds: new[] { 1, 1, 1, 1 });
            bus.Clear(); // only interested in the residual ticks from here on

            // 30 ticks of 0.05s = 1.5s, matching l1_t3_residual_duration exactly.
            for (int i = 0; i < 30; i++)
                tracker.Tick(0.05f);

            // Assert — every one of the 30 ticks emitted exactly one LaserHit at the residual rate
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(30));
            foreach (var hit in bus.Events<LaserHit>())
            {
                Assert.That(hit.PartId, Is.EqualTo(1));
                Assert.That(hit.HeatDelta, Is.EqualTo(0.40f * 25.0f * 0.05f).Within(1e-4f));
            }

            // Timer fully depleted — one more tick emits nothing.
            bus.Clear();
            tracker.Tick(0.05f);
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(0));
        }

        [Test]
        public void test_l1_t3_residual_re_hit_resets_timer_not_additive()
        {
            var bus = new RecordingEventBus();
            var tracker = new ResidualHeatTracker(bus);
            var tierQuery = new StubWeaponTierQuery().SetTier(WeaponId.L1, 3);
            var l1 = new L1SpreadLaser(bus, tierQuery, new StubPartStateQuery(), Balance(), L1Def(), tracker);

            l1.FireFrame(0.016f, KaijuId, new[] { 1, 1, 1, 1 });
            tracker.Tick(1.0f); // 1.0s elapsed of the 1.5s window — 0.5s would remain if NOT reset

            l1.FireFrame(0.016f, KaijuId, new[] { 1, 1, 1, 1 }); // re-hit: resets remaining back to the full 1.5s
            bus.Clear();

            // Exactly the reset duration — still active (and emits) at the start of this tick.
            tracker.Tick(1.5f);
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1), "still active for the full reset duration");

            // If Register() had been additive (0.5s pre-reset remainder + 1.5s new = 2.0s), the
            // window would still have 0.5s left here and this tick would ALSO emit. It must not.
            bus.Clear();
            tracker.Tick(0.05f);
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(0), "window did not extend past the reset duration (proves reset, not additive stacking)");
        }

        // ── AC-2: L2 T3 漣漪在 PartBroke 後發 LaserHit 至相鄰部位 ───────────────────

        [Test]
        public void test_l2_t3_break_ripple_emits_laserhit_to_each_adjacent_part()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var partQuery = new StubPartStateQuery();
            partQuery.Configure(2, maxHeat: 100f);
            partQuery.Configure(3, maxHeat: 100f);
            var tierQuery = new StubWeaponTierQuery().SetTier(WeaponId.L2, 3);
            var l2 = new L2FocusBeam(bus, tierQuery, partQuery, Balance(), L2Def(), PartSystem(0.30f));
            l2.Enable();

            // Act
            bus.Publish(new PartBroke(1, KaijuId, PartType.Normal, default, dropTableId: 1,
                quality: BreakQuality.Softened, adjacencyIds: new[] { 2, 3 }, isChainBreak: false));

            // Assert
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(2));
            var hits = bus.Events<LaserHit>();
            Assert.That(hits[0].PartId, Is.EqualTo(2));
            Assert.That(hits[0].HeatDelta, Is.EqualTo(30.0f).Within(1e-3f));
            Assert.That(hits[1].PartId, Is.EqualTo(3));
            Assert.That(hits[1].HeatDelta, Is.EqualTo(30.0f).Within(1e-3f));
        }

        [Test]
        public void test_l2_below_tier3_ignores_partbroke_ripple()
        {
            var bus = new RecordingEventBus();
            var partQuery = new StubPartStateQuery();
            partQuery.Configure(2, maxHeat: 100f);
            var tierQuery = new StubWeaponTierQuery(); // default tier 0
            var l2 = new L2FocusBeam(bus, tierQuery, partQuery, Balance(), L2Def(), PartSystem(0.30f));
            l2.Enable();

            bus.Publish(new PartBroke(1, KaijuId, PartType.Normal, default, 1, BreakQuality.Normal,
                new[] { 2 }, false));

            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(0));
        }

        // ── AC-3: L3 T3 共鳴擴散在蓄力釋放同幀發兩次 LaserHit ───────────────────────

        [Test]
        public void test_l3_t3_resonance_diffusion_emits_second_laserhit_alongside_charge()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var partQuery = new StubPartStateQuery();
            partQuery.Configure(1, maxHeat: 100f);
            var tierQuery = new StubWeaponTierQuery().SetTier(WeaponId.L3, 3);
            var l3 = new L3WaveCannon(bus, tierQuery, partQuery, Balance(), L3Def());

            // Act
            l3.UpdateFrame(1.5f, true, 1, KaijuId);
            l3.UpdateFrame(0.01f, false, 1, KaijuId);

            // Assert
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(2));
            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(1));

            var hits = bus.Events<LaserHit>();
            Assert.That(hits[0].HeatDelta, Is.EqualTo(2.50f * 25.0f).Within(1e-3f), "charge hit, unchanged by T3");
            Assert.That(hits[1].HeatDelta, Is.EqualTo(0.50f * 100.0f).Within(1e-3f), "T3 resonance inject = l3_t3_heat_inject_pct * part MaxHeat");
        }

        [Test]
        public void test_l3_below_tier3_charge_emits_only_one_laserhit()
        {
            var bus = new RecordingEventBus();
            var partQuery = new StubPartStateQuery();
            partQuery.Configure(1, maxHeat: 100f);
            var tierQuery = new StubWeaponTierQuery(); // default tier 0
            var l3 = new L3WaveCannon(bus, tierQuery, partQuery, Balance(), L3Def());

            l3.UpdateFrame(1.5f, true, 1, KaijuId);
            l3.UpdateFrame(0.01f, false, 1, KaijuId);

            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(1));
        }

        // ── AC-4: E.6 L1 殘熱 + L4 熱殘影同時取最大，不疊加 ─────────────────────────

        [Test]
        public void test_e6_overlapping_residual_channels_on_same_part_take_max_not_sum()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var tracker = new ResidualHeatTracker(bus);

            // L1 residual rate = 0.40 * 25 = 10 HU/s; L4 afterimage rate = 0.40 * 25 = 10 HU/s (equal).
            tracker.Register(partId: 1, kaijuId: KaijuId, ResidualChannel.L1Residual, rate: 10f, duration: 1.5f);
            tracker.Register(partId: 1, kaijuId: KaijuId, ResidualChannel.L4Afterimage, rate: 10f, duration: 2.0f);

            // Act
            tracker.Tick(deltaTime: 0.016f);

            // Assert — exactly one LaserHit, at max(10,10)*dt, not 20*dt
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
            Assert.That(bus.Events<LaserHit>()[0].HeatDelta, Is.EqualTo(10f * 0.016f).Within(1e-5f));
        }

        [Test]
        public void test_e6_overlapping_residual_channels_with_different_rates_takes_the_larger()
        {
            var bus = new RecordingEventBus();
            var tracker = new ResidualHeatTracker(bus);

            tracker.Register(1, KaijuId, ResidualChannel.L1Residual, rate: 10f, duration: 1.5f);
            tracker.Register(1, KaijuId, ResidualChannel.L4Afterimage, rate: 15f, duration: 2.0f);

            tracker.Tick(0.016f);

            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
            Assert.That(bus.Events<LaserHit>()[0].HeatDelta, Is.EqualTo(15f * 0.016f).Within(1e-5f));
        }
    }
}
