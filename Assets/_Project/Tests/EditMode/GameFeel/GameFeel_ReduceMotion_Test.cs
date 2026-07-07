using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.GameFeel;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.GameFeel
{
    /// <summary>
    /// GameFeel Story 007 — reduce-motion accessibility toggle (game-feel.md §I.7). The
    /// <see cref="ReduceMotionController"/> drives a mutable <see cref="ReduceMotionSettings"/> the feel
    /// systems read (a runtime layer over the read-only config) and persists the choice via an
    /// <see cref="ISaveService"/> flag. Verifies the four multiplier values, immediate + persisted effect, and
    /// the real impact on hitstop/slow-mo/shake.
    ///
    /// <para><b>Reconciliation:</b> the reduced multipliers live in <see cref="ReduceMotionSettings"/>
    /// (settings/save layer), NOT on the read-only GameFeelConfig SO; systems multiply
    /// <c>config.*AccessibilityMult × settings.*Mult</c> (settings default 1.0 → no behaviour change until the
    /// toggle is on). Persistence uses the new <c>ISaveService</c> flags (meta-save story-007).</para>
    /// </summary>
    [TestFixture]
    public sealed class GameFeelReduceMotionTests
    {
        private sealed class FakeTime : ITimeScaleControl { public float TimeScale { get; set; } = 1f; }

        private RecordingEventBus _bus;
        private RecordingSaveService _save;
        private ReduceMotionSettings _settings;
        private ReduceMotionController _controller;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _save = new RecordingSaveService();
            _settings = new ReduceMotionSettings();
            _controller = new ReduceMotionController(_settings, _save);
        }

        private static GameFeelConfig FullConfig() => ContentTestFactory.Create<GameFeelConfig>(
            ("_hitstopPartBreakMs", 115f), ("_hitstopAccessibilityMult", 1f),
            ("_slowmoPartBreakTimescale", 0.12f), ("_slowmoPartBreakHoldSeconds", 0.65f), ("_slowmoAccessibilityMult", 1f),
            ("_shakeMagnitudeCap", 24f), ("_shakeThreshold", 0.3f), ("_shakeAccessibilityMult", 1f),
            ("_slowmoRampRate", 3.8f), ("_shakeDecayRate", 42f));

        // ── The four multipliers + immediate toggle ───────────────────────────

        [Test]
        public void test_enable_sets_the_four_reduced_multipliers()
        {
            _controller.SetEnabled(true);
            Assert.AreEqual(0.25f, _settings.ShakeMult, 0.0001f, "shake 25%");
            Assert.AreEqual(0f, _settings.FlashMult, 0.0001f, "flash off");
            Assert.AreEqual(0f, _settings.SlowmoMult, 0.0001f, "slow-mo off");
            Assert.AreEqual(0.5f, _settings.HitstopMult, 0.0001f, "hitstop 50%");
            Assert.IsTrue(_controller.IsEnabled);
        }

        [Test]
        public void test_disable_restores_full_multipliers()
        {
            _controller.SetEnabled(true);
            _controller.SetEnabled(false);
            Assert.AreEqual(1f, _settings.ShakeMult, 0.0001f);
            Assert.AreEqual(1f, _settings.FlashMult, 0.0001f);
            Assert.AreEqual(1f, _settings.SlowmoMult, 0.0001f);
            Assert.AreEqual(1f, _settings.HitstopMult, 0.0001f);
        }

        // ── Persistence ───────────────────────────────────────────────────────

        [Test]
        public void test_toggle_is_persisted_and_restored()
        {
            _controller.SetEnabled(true);
            Assert.IsTrue(_save.GetFlag(ReduceMotionController.ReduceMotionFlag), "flag persisted");

            // A fresh controller on the same save restores the enabled state.
            var settings2 = new ReduceMotionSettings();
            var controller2 = new ReduceMotionController(settings2, _save);
            Assert.IsTrue(controller2.IsEnabled);
            Assert.AreEqual(0.25f, settings2.ShakeMult, 0.0001f, "reduced profile restored from save");
        }

        // ── Real effect on the feel systems ───────────────────────────────────

        [Test]
        public void test_reduce_motion_shrinks_shake_to_25_percent()
        {
            _controller.SetEnabled(true);
            var cfg = FullConfig();
            var shake = new ShakeSystem(_bus, cfg, () => 0.5f, _settings);
            shake.AddShake(20f);
            Assert.AreEqual(5f, shake.CurrentMagnitude, 0.0001f, "20 × 0.25 = 5");
            shake.Dispose();
            UnityEngine.Object.DestroyImmediate(cfg);
        }

        [Test]
        public void test_reduce_motion_disables_slowmo()
        {
            _controller.SetEnabled(true);
            var cfg = FullConfig();
            var time = new FakeTime();
            var slow = new SlowmoSystem(_bus, cfg, time, _settings);
            _bus.Publish(new PartBroke(1, 1, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false));
            Assert.AreEqual(1f, time.TimeScale, 0.0001f, "slow-mo off under reduce-motion");
            Assert.IsFalse(slow.IsActive);
            slow.Dispose();
            UnityEngine.Object.DestroyImmediate(cfg);
        }

        [Test]
        public void test_reduce_motion_halves_hitstop_duration()
        {
            _controller.SetEnabled(true);
            var cfg = FullConfig();
            var time = new FakeTime();
            var hit = new HitstopSystem(_bus, cfg, time, _settings);
            _bus.Publish(new PartBroke(1, 1, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false));
            // 115ms × 0.5 = 57.5ms
            Assert.AreEqual(0.0575f, hit.RemainingSeconds, 0.0001f, "hitstop at 50% duration");
            hit.Dispose();
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }
}
