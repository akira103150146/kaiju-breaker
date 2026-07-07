using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.GameFeel;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.GameFeel
{
    /// <summary>
    /// GameFeel Story 003 — slow-motion (game-feel.md §I.3/§D.2). Pure C# via injected
    /// <see cref="ITimeScaleControl"/>, so drop→hold→ramp is verified arithmetically with
    /// <see cref="SlowmoSystem.Tick"/>. Committed event is <see cref="BossCoreBroke"/>.
    /// </summary>
    [TestFixture]
    public sealed class GameFeelSlowMoTests
    {
        private sealed class FakeTime : ITimeScaleControl { public float TimeScale { get; set; } = 1f; }

        private RecordingEventBus _bus;
        private GameFeelConfig _config;
        private FakeTime _time;
        private SlowmoSystem _sys;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _config = Config(1f);
            _time = new FakeTime();
            _sys = new SlowmoSystem(_bus, _config, _time);
        }

        [TearDown]
        public void TearDown()
        {
            _sys.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private static GameFeelConfig Config(float a11y) => ContentTestFactory.Create<GameFeelConfig>(
            ("_slowmoPartBreakTimescale", 0.12f), ("_slowmoPartBreakHoldSeconds", 0.65f),
            ("_slowmoBossDeathTimescale", 0.05f), ("_slowmoBossDeathHoldSeconds", 1.20f),
            ("_slowmoRampRate", 3.8f), ("_slowmoAccessibilityMult", a11y));

        private void PartBroke() =>
            _bus.Publish(new PartBroke(1, 1, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false));
        private void BossCoreBroke() => _bus.Publish(new BossCoreBroke(1, Vector2.zero));

        // ── AC-1: drop → hold → ramp back to 1 ────────────────────────────────

        [Test]
        public void test_part_break_drops_holds_then_ramps_back()
        {
            PartBroke();
            Assert.AreEqual(0.12f, _time.TimeScale, 0.0001f, "instant drop to slowmo min");

            _sys.Tick(0.65f); // finish the hold; still at min
            Assert.AreEqual(0.12f, _time.TimeScale, 0.0001f, "held at min through the hold window");
            Assert.IsTrue(_sys.IsActive);

            _sys.Tick(0.30f); // ramp: 0.12 + 3.8*0.30 = 1.26 → clamps to 1
            Assert.AreEqual(1f, _time.TimeScale, 0.0001f, "ramped back to normal");
            Assert.IsFalse(_sys.IsActive);
        }

        [Test]
        public void test_ramp_math_matches_gdd_formula()
        {
            PartBroke();
            _sys.Tick(0.65f);            // end hold
            _sys.Tick(0.10f);            // ramp for 0.10s
            Assert.AreEqual(0.12f + 3.8f * 0.10f, _time.TimeScale, 0.0001f, "min + rampRate*elapsed");
        }

        // ── Boss override + not-overridden ────────────────────────────────────

        [Test]
        public void test_boss_death_overrides_part_break_slowmo()
        {
            PartBroke();
            _sys.Tick(0.10f);
            BossCoreBroke();
            Assert.AreEqual(0.05f, _time.TimeScale, 0.0001f, "boss uses the deeper min");
        }

        [Test]
        public void test_part_break_does_not_override_boss_slowmo()
        {
            BossCoreBroke();
            PartBroke();
            Assert.AreEqual(0.05f, _time.TimeScale, 0.0001f, "boss slowmo unaffected by a part-break");
        }

        // ── Reset (not add) the hold ──────────────────────────────────────────

        [Test]
        public void test_part_break_during_hold_resets_hold_timer()
        {
            PartBroke();
            _sys.Tick(0.30f); // 0.35s of hold remaining
            PartBroke();      // reset hold back to 0.65s

            _sys.Tick(0.50f); // if NOT reset the hold would have ended and be ramping; reset keeps it holding
            Assert.AreEqual(0.12f, _time.TimeScale, 0.0001f, "still holding at min after reset");
        }

        // ── reduce-motion ─────────────────────────────────────────────────────

        [Test]
        public void test_accessibility_mult_zero_disables_slowmo()
        {
            var cfg = Config(0f);
            var time = new FakeTime();
            var sys = new SlowmoSystem(_bus, cfg, time);

            _bus.Publish(new PartBroke(1, 1, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false));

            Assert.AreEqual(1f, time.TimeScale, "reduce-motion → no slowmo");
            Assert.IsFalse(sys.IsActive);
            sys.Dispose();
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }
}
