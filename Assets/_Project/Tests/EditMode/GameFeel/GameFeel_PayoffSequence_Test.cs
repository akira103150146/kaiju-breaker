using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.GameFeel;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.GameFeel
{
    /// <summary>
    /// GameFeel Story 006 (sequencing) — <see cref="BreakPayoffSequencer"/> orders the payoff so hitstop
    /// freezes first, then hands off to slow-mo when the freeze ends (game-feel.md §D.4). Hitstop/Slowmo are
    /// driven (not self-subscribed), so there is one clean owner of the time scale. Boss death overrides a
    /// same-frame part break (§E.2). Pure C#; time driven by ticking both systems.
    /// </summary>
    [TestFixture]
    public sealed class GameFeelPayoffSequenceTests
    {
        private sealed class FakeTime : ITimeScaleControl { public float TimeScale { get; set; } = 1f; }

        private RecordingEventBus _bus;
        private GameFeelConfig _config;
        private FakeTime _time;
        private HitstopSystem _hitstop;
        private SlowmoSystem _slowmo;
        private FlashSystem _flash;
        private BreakPayoffSequencer _seq;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _config = ContentTestFactory.Create<GameFeelConfig>(
                ("_hitstopPartBreakMs", 115f), ("_hitstopBossDeathMs", 220f), ("_hitstopAccessibilityMult", 1f),
                ("_slowmoPartBreakTimescale", 0.12f), ("_slowmoPartBreakHoldSeconds", 0.65f),
                ("_slowmoBossDeathTimescale", 0.05f), ("_slowmoBossDeathHoldSeconds", 1.2f),
                ("_slowmoRampRate", 3.8f), ("_slowmoAccessibilityMult", 1f),
                ("_flashDecayRate", 2.6f), ("_flashMaxAlpha", 0.85f), ("_flashAccessibilityMult", 1f));
            _time = new FakeTime();
            _hitstop = new HitstopSystem(_bus, _config, _time, null, subscribeToBus: false);
            _slowmo = new SlowmoSystem(_bus, _config, _time, null, subscribeToBus: false);
            _flash = new FlashSystem(_config);
            _seq = new BreakPayoffSequencer(_bus, _hitstop, _slowmo, _flash);
        }

        [TearDown]
        public void TearDown()
        {
            _seq.Dispose();
            _slowmo.Dispose();
            _hitstop.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private void Tick(float dt) { _hitstop.Tick(dt); _slowmo.Tick(dt); }
        private void PartBroke() => _bus.Publish(new PartBroke(1, 1, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false));
        private void BossCoreBroke() => _bus.Publish(new BossCoreBroke(1, Vector2.zero));

        [Test]
        public void test_part_break_flashes_and_freezes_then_hands_off_to_slowmo()
        {
            PartBroke();
            Assert.AreEqual(0f, _time.TimeScale, "hitstop first");
            Assert.AreEqual(0.92f, _flash.Intensity, 0.0001f, "part-break flash");

            Tick(0.116f); // end the 115ms freeze → hand off to slow-mo this frame
            Assert.AreEqual(0.12f, _time.TimeScale, 0.0001f, "slow-mo took over at its min");

            for (int i = 0; i < 200 && _time.TimeScale < 1f; i++) Tick(0.02f); // ride the hold + ramp
            Assert.AreEqual(1f, _time.TimeScale, 0.0001f, "eventually back to full speed");
        }

        [Test]
        public void test_no_slowmo_before_hitstop_ends()
        {
            PartBroke();
            Tick(0.05f); // still inside the freeze
            Assert.AreEqual(0f, _time.TimeScale, "slow-mo must not start until the freeze ends");
        }

        [Test]
        public void test_boss_death_overrides_same_frame_part_break()
        {
            BossCoreBroke();
            PartBroke(); // same-frame part break must not downgrade the boss payoff (§E.2)
            Assert.AreEqual(1.0f, _flash.Intensity, 0.0001f, "boss flash, not part-break flash");

            Tick(0.221f); // end the 220ms boss freeze → boss slow-mo
            Assert.AreEqual(0.05f, _time.TimeScale, 0.0001f, "boss slow-mo min");
        }
    }
}
