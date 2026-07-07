using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.GameFeel;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.GameFeel
{
    /// <summary>
    /// GameFeel Story 002 — hitstop (game-feel.md §I.2; TR-gamefeel-002). The <see cref="HitstopSystem"/> is
    /// pure C# with an injected <see cref="ITimeScaleControl"/>, so the freeze/restore timing is verified
    /// arithmetically via <see cref="HitstopSystem.Tick"/> — no Play Mode, no real Time API.
    ///
    /// <para><b>Reconciliation:</b> the committed core-break event is <see cref="BossCoreBroke"/> (not the
    /// story's <c>BossCoreBreak</c>); time scale goes through <see cref="ITimeScaleControl"/> (App maps it to
    /// Time.timeScale). AC-5 input-buffering is the Input system's (out of scope here).</para>
    /// </summary>
    [TestFixture]
    public sealed class GameFeelHitstopTests
    {
        private sealed class FakeTime : ITimeScaleControl { public float TimeScale { get; set; } = 1f; }

        private RecordingEventBus _bus;
        private GameFeelConfig _config;
        private FakeTime _time;
        private HitstopSystem _sys;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _config = ContentTestFactory.Create<GameFeelConfig>(
                ("_hitstopPartBreakMs", 115f), ("_hitstopBossDeathMs", 220f), ("_hitstopAccessibilityMult", 1f));
            _time = new FakeTime();
            _sys = new HitstopSystem(_bus, _config, _time);
        }

        [TearDown]
        public void TearDown()
        {
            _sys.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private void PublishPartBroke() =>
            _bus.Publish(new PartBroke(1, 1, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false));

        private void PublishBossCoreBroke() => _bus.Publish(new BossCoreBroke(1, Vector2.zero));

        // ── AC-1: part-break freeze duration ──────────────────────────────────

        [Test]
        public void test_part_break_freezes_then_restores_after_duration()
        {
            PublishPartBroke();
            Assert.AreEqual(0f, _time.TimeScale, "frozen immediately");
            Assert.IsTrue(_sys.IsActive);

            _sys.Tick(0.114f);
            Assert.AreEqual(0f, _time.TimeScale, "still frozen just before the window ends");

            _sys.Tick(0.002f); // total 0.116 > 0.115
            Assert.AreEqual(1f, _time.TimeScale, "restored after 115ms");
            Assert.IsFalse(_sys.IsActive);
        }

        // ── AC-4: countdown on unscaled time ──────────────────────────────────

        [Test]
        public void test_timer_counts_down_on_unscaled_delta()
        {
            PublishPartBroke();
            _sys.Tick(0.050f);
            Assert.AreEqual(0.065f, _sys.RemainingSeconds, 0.0001f, "115ms − 50ms = 65ms remaining");
        }

        // ── AC-3: consecutive part-breaks reset (not accumulate) ──────────────

        [Test]
        public void test_consecutive_part_breaks_reset_timer()
        {
            PublishPartBroke();
            _sys.Tick(0.085f); // 30ms remaining
            Assert.AreEqual(0.030f, _sys.RemainingSeconds, 0.0001f);

            PublishPartBroke(); // reset, not add
            Assert.AreEqual(0.115f, _sys.RemainingSeconds, 0.0001f, "reset to 115ms, not 145ms");
        }

        // ── AC-2: boss death overrides + is not overridden ────────────────────

        [Test]
        public void test_boss_death_overrides_part_break_hitstop()
        {
            PublishPartBroke();
            _sys.Tick(0.065f); // 50ms remaining
            PublishBossCoreBroke();
            Assert.AreEqual(0.220f, _sys.RemainingSeconds, 0.0001f, "reset to boss window 220ms");
        }

        [Test]
        public void test_part_break_does_not_override_boss_hitstop()
        {
            PublishBossCoreBroke();
            _sys.Tick(0.020f); // 200ms remaining
            PublishPartBroke(); // must NOT reset to 115ms
            Assert.AreEqual(0.200f, _sys.RemainingSeconds, 0.0001f, "boss freeze unaffected by a part-break");
        }

        // ── overshoot + reduce-motion ─────────────────────────────────────────

        [Test]
        public void test_large_delta_overshoot_restores_time_scale()
        {
            PublishPartBroke();
            _sys.Tick(5f); // huge frame spike
            Assert.AreEqual(1f, _time.TimeScale);
            Assert.IsFalse(_sys.IsActive);
            Assert.AreEqual(0f, _sys.RemainingSeconds);
        }

        [Test]
        public void test_accessibility_mult_zero_disables_hitstop()
        {
            var cfg = ContentTestFactory.Create<GameFeelConfig>(
                ("_hitstopPartBreakMs", 115f), ("_hitstopAccessibilityMult", 0f));
            var time = new FakeTime();
            var sys = new HitstopSystem(_bus, cfg, time);

            _bus.Publish(new PartBroke(1, 1, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false));

            Assert.AreEqual(1f, time.TimeScale, "reduce-motion (a11y 0) → no freeze");
            Assert.IsFalse(sys.IsActive);
            sys.Dispose();
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }
}
