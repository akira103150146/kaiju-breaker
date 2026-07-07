using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.GameFeel;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.GameFeel
{
    /// <summary>
    /// GameFeel Story 004 — screen shake trauma model (game-feel.md §I.4/§D.1). Pure C#; the random source is
    /// injected so the integer-pixel offset + threshold zeroing are deterministic. Verifies cap, max-not-add,
    /// linear decay, offset floor, threshold, and event→magnitude wiring.
    /// </summary>
    [TestFixture]
    public sealed class GameFeelShakeTests
    {
        private RecordingEventBus _bus;
        private GameFeelConfig _config;
        private ShakeSystem _sys;
        private float _rand = 0.5f; // deterministic signed-unit source

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _config = Config(1f);
            _sys = new ShakeSystem(_bus, _config, () => _rand);
        }

        [TearDown]
        public void TearDown()
        {
            _sys.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private static GameFeelConfig Config(float a11y) => ContentTestFactory.Create<GameFeelConfig>(
            ("_shakeMagnitudeCap", 24f), ("_shakeDecayRate", 42f), ("_shakeThreshold", 0.3f),
            ("_shakeAccessibilityMult", a11y), ("_shakeMagPartBreakBase", 11f),
            ("_shakeMagBossDeath", 24f), ("_shakeMagM3TorpedoHit", 9f));

        // ── AC-1: cap ─────────────────────────────────────────────────────────

        [Test]
        public void test_shake_is_capped()
        {
            _sys.AddShake(30f);
            Assert.AreEqual(24f, _sys.CurrentMagnitude, 0.0001f, "clamped to the magnitude cap");
        }

        // ── AC-2: max, not additive ───────────────────────────────────────────

        [Test]
        public void test_consecutive_shakes_take_max_not_sum()
        {
            _sys.AddShake(10f);
            _sys.AddShake(15f);
            Assert.AreEqual(15f, _sys.CurrentMagnitude, 0.0001f, "max(10,15)=15, not 25");
            _sys.AddShake(5f);
            Assert.AreEqual(15f, _sys.CurrentMagnitude, 0.0001f, "a smaller shake does not lower it");
        }

        // ── AC-3: linear decay ────────────────────────────────────────────────

        [Test]
        public void test_shake_decays_linearly()
        {
            _sys.AddShake(15f);
            _sys.Tick(0.1f); // 15 − 42*0.1 = 10.8
            Assert.AreEqual(10.8f, _sys.CurrentMagnitude, 0.0001f);
        }

        [Test]
        public void test_shake_decay_floors_at_zero()
        {
            _sys.AddShake(5f);
            _sys.Tick(1f); // way past 0
            Assert.AreEqual(0f, _sys.CurrentMagnitude, 0.0001f);
        }

        // ── AC-4: integer offset ──────────────────────────────────────────────

        [Test]
        public void test_offset_is_floored_random_scaled_by_magnitude()
        {
            _sys.AddShake(20f);
            _rand = 0.5f;
            var off = _sys.ComputeOffset(); // floor(0.5 * 20) = 10
            Assert.AreEqual(10f, off.x, 0.0001f);
            Assert.AreEqual(10f, off.y, 0.0001f);
        }

        // ── AC-5: threshold zeroes micro-jitter ───────────────────────────────

        [Test]
        public void test_offset_is_zero_below_threshold()
        {
            _sys.AddShake(0.2f); // below 0.3 threshold
            Assert.AreEqual(Vector2.zero, _sys.ComputeOffset());
        }

        // ── Event wiring ──────────────────────────────────────────────────────

        [Test]
        public void test_part_broke_and_boss_death_map_to_magnitudes()
        {
            _bus.Publish(new PartBroke(1, 1, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false));
            Assert.AreEqual(11f, _sys.CurrentMagnitude, 0.0001f, "part-break base");

            _bus.Publish(new BossCoreBroke(1, Vector2.zero));
            Assert.AreEqual(24f, _sys.CurrentMagnitude, 0.0001f, "boss death (max over part-break)");
        }

        [Test]
        public void test_m3_missile_hit_adds_torpedo_shake()
        {
            _bus.Publish(new MissileHit(1, 1, 10f, WeaponId.M3));
            Assert.AreEqual(9f, _sys.CurrentMagnitude, 0.0001f);
        }

        // ── reduce-motion ─────────────────────────────────────────────────────

        [Test]
        public void test_accessibility_mult_zero_disables_shake()
        {
            var cfg = Config(0f);
            var sys = new ShakeSystem(_bus, cfg, () => _rand);
            sys.AddShake(30f);
            Assert.AreEqual(0f, sys.CurrentMagnitude, 0.0001f, "reduce-motion → no shake");
            sys.Dispose();
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }
}
