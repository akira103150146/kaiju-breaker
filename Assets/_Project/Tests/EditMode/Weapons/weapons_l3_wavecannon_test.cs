using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Weapons;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 005 — L3 Wave Cannon dual-mode tap/charge + L3WaveHit event. Verifies AC-1..AC-4
    /// from production/epics/weapons/story-005-l3-wave-cannon.md against
    /// design/gdd/weapon-system.md §C.4 L3 / §E.4.
    ///
    /// DEVIATION from story-005 AC-2's original text: <see cref="WaveHit"/> carries only
    /// (PartId, KaijuId) — no StaggerDuration payload field (per the story-004/005 handoff
    /// decision; the stagger window length is WeaponBalanceConfig.StaggerDuration, applied by
    /// KaijuParts, not carried on this event). The "payload StaggerDuration ≈ 2.0f" assertion
    /// from the original AC-2 text is dropped accordingly.
    /// </summary>
    public sealed class WeaponsL3WaveCannonTests
    {
        private const int TargetPart = 1;
        private const int KaijuId = 0;

        // ── Fixtures ─────────────────────────────────────────────────────────────

        private static WeaponBalanceConfig Balance() =>
            ContentTestFactory.Create<WeaponBalanceConfig>(("_huPerD0", 25.0f));

        private static WeaponDef L3Def() =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.L3), ("_type", WeaponType.Laser),
                ("_l3TapOutputMult", 0.60f), ("_l3ChargeTime", 1.5f),
                ("_l3ChargeOutputMult", 2.50f), ("_l3ChargeCooldown", 2.0f),
                ("_l3T3HeatInjectPct", 0.50f));

        private static L3WaveCannon MakeL3(IEventBus bus, IWeaponTierQuery tierQuery = null) =>
            new L3WaveCannon(bus, tierQuery ?? new StubWeaponTierQuery(), new StubPartStateQuery(),
                Balance(), L3Def());

        // ── AC-1: Tap 模式發 LaserHit（短按）──────────────────────────────────────

        [Test]
        public void test_l3_short_hold_below_charge_time_fires_tap_laserhit_no_wavehit()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l3 = MakeL3(bus);

            // Act — held for 0.3s (< 1.5s charge_time), then released
            l3.UpdateFrame(deltaTime: 0.3f, isHeld: true, targetPartId: TargetPart, kaijuId: KaijuId);
            l3.UpdateFrame(deltaTime: 0.016f, isHeld: false, targetPartId: TargetPart, kaijuId: KaijuId);

            // Assert
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(0));
            var hit = bus.Events<LaserHit>()[0];
            Assert.That(hit.HeatDelta, Is.EqualTo(0.60f * 25.0f * 0.3f).Within(1e-3f), "0.6 * D0-per-HU * heldTime = 4.5 HU");
        }

        [Test]
        public void test_l3_held_exactly_at_charge_time_boundary_resolves_as_charge()
        {
            // Boundary: heldTime == l3_charge_time (1.5f) must resolve as CHARGE, not tap (>=).
            var bus = new RecordingEventBus();
            var l3 = MakeL3(bus);

            l3.UpdateFrame(deltaTime: 1.5f, isHeld: true, targetPartId: TargetPart, kaijuId: KaijuId);
            l3.UpdateFrame(deltaTime: 0.001f, isHeld: false, targetPartId: TargetPart, kaijuId: KaijuId);

            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(1), "boundary hold (== charge_time) must count as a full charge");
        }

        // ── AC-2: Charge 模式發 LaserHit + L3WaveHit（長按）─────────────────────────

        [Test]
        public void test_l3_hold_at_or_above_charge_time_fires_charge_laserhit_and_wavehit_same_update()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l3 = MakeL3(bus);

            // Act — held 1.5s then released
            l3.UpdateFrame(deltaTime: 1.5f, isHeld: true, targetPartId: TargetPart, kaijuId: KaijuId);
            l3.UpdateFrame(deltaTime: 0.016f, isHeld: false, targetPartId: TargetPart, kaijuId: KaijuId);

            // Assert — both events published, LaserHit first, in the same (release) call
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(1));

            var laserHit = bus.Events<LaserHit>()[0];
            Assert.That(laserHit.HeatDelta, Is.EqualTo(2.50f * 25.0f).Within(1e-3f), "charge_output_mult * HuPerD0 (flat, not held-time scaled)");

            var waveHit = bus.Events<WaveHit>()[0];
            Assert.That(waveHit.PartId, Is.EqualTo(TargetPart));
            Assert.That(waveHit.KaijuId, Is.EqualTo(KaijuId));

            Assert.That(bus.Recorded[bus.Recorded.Count - 2], Is.TypeOf<LaserHit>(), "charge LaserHit publishes before WaveHit, same frame");
            Assert.That(bus.Recorded[bus.Recorded.Count - 1], Is.TypeOf<WaveHit>());
        }

        // ── AC-3: 蓄力冷卻期間不觸發第二次蓄力 ──────────────────────────────────────

        [Test]
        public void test_l3_charge_cooldown_blocks_second_charge_but_allows_tap()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l3 = MakeL3(bus);

            // First charge — starts the 2.0s cooldown.
            l3.UpdateFrame(1.5f, true, TargetPart, KaijuId);
            l3.UpdateFrame(0.01f, false, TargetPart, KaijuId);
            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(1));

            // Act — immediately attempt a second long hold while still cooling down (0.5s left).
            l3.UpdateFrame(1.5f, true, TargetPart, KaijuId);   // cooldown ticks 0.5 -> 0 during this hold... (0.5 - 1.5 clamped at 0)
            l3.UpdateFrame(0.01f, false, TargetPart, KaijuId); // release: still resolves as TAP, not a second charge

            // Assert — no second WaveHit; tap still emitted a LaserHit (cooldown does not block tap)
            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(1), "cooldown blocks a second charge");
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(2), "1st LaserHit from the initial charge + a 2nd from the cooldown-downgraded tap");

            // Act — let the cooldown fully elapse, then hold/release >= charge_time again.
            l3.UpdateFrame(2.0f, false, TargetPart, KaijuId); // idle tick, drains any residual cooldown
            Assert.That(l3.IsOnCooldown, Is.False);

            l3.UpdateFrame(1.5f, true, TargetPart, KaijuId);
            l3.UpdateFrame(0.01f, false, TargetPart, KaijuId);

            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(2), "cooldown elapsed -> a fresh charge succeeds");
        }

        // ── AC-4: timeScale=0 時蓄力暫停 ─────────────────────────────────────────

        [Test]
        public void test_l3_zero_deltatime_pauses_charge_accumulation()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var l3 = MakeL3(bus);

            // Enter Charging with a small nonzero step.
            l3.UpdateFrame(0.01f, true, TargetPart, KaijuId);
            Assert.That(l3.IsCharging, Is.True);
            Assert.That(l3.HeldTime, Is.EqualTo(0.01f).Within(1e-6f));

            // Act — simulate timeScale = 0: Time.deltaTime reads 0 for 50 Update calls.
            for (int i = 0; i < 50; i++)
                l3.UpdateFrame(0f, true, TargetPart, KaijuId);

            // Assert — charge timer did not move; state unchanged; nothing published.
            Assert.That(l3.HeldTime, Is.EqualTo(0.01f).Within(1e-6f));
            Assert.That(l3.IsCharging, Is.True);
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(0));
            Assert.That(bus.CountOf<WaveHit>(), Is.EqualTo(0));
        }
    }
}
