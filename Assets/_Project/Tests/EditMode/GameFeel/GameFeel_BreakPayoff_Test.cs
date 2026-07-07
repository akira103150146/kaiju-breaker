using KaijuBreaker.Content;
using KaijuBreaker.GameFeel;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.GameFeel
{
    /// <summary>
    /// GameFeel Story 006 (flash model) — the full-screen flash of the break payoff (game-feel.md §D.3/§D.4;
    /// §I.5 readability guardrail). Pure C#; verifies max-not-add, linear decay, alpha ceiling, the 0.4s
    /// fade-below-20% guardrail, and the reduce-motion off state.
    ///
    /// <para><b>Scope:</b> the FLASH model is the cleanly-testable piece of the payoff. The rest of the §D.4
    /// sequence — debris/smoke particles, homing material orbs, boss-death detonation, and the ordered
    /// hitstop→slow-mo handoff — is visual orchestration that composes the already-tested Hitstop/Slowmo/Shake
    /// systems and is an App-layer follow-up.</para>
    /// </summary>
    [TestFixture]
    public sealed class GameFeelBreakPayoffTests
    {
        private GameFeelConfig _config;
        private FlashSystem _flash;

        [SetUp]
        public void SetUp()
        {
            _config = ContentTestFactory.Create<GameFeelConfig>(
                ("_flashDecayRate", 2.6f), ("_flashMaxAlpha", 0.85f), ("_flashAccessibilityMult", 1f));
            _flash = new FlashSystem(_config);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        [Test]
        public void test_trigger_sets_intensity_and_alpha_ceiling()
        {
            _flash.Trigger(1f);
            Assert.AreEqual(1f, _flash.Intensity, 0.0001f);
            Assert.AreEqual(0.85f, _flash.Alpha, 0.0001f, "alpha = intensity × FlashMaxAlpha (never full white-out)");
        }

        [Test]
        public void test_flash_is_max_not_additive()
        {
            _flash.Trigger(0.9f);
            _flash.Trigger(0.5f);
            Assert.AreEqual(0.9f, _flash.Intensity, 0.0001f, "max(0.9, 0.5), not 1.4");
        }

        [Test]
        public void test_flash_decays_linearly()
        {
            _flash.Trigger(1f);
            _flash.Tick(0.1f); // 1 − 2.6*0.1 = 0.74
            Assert.AreEqual(0.74f, _flash.Intensity, 0.0001f);
        }

        [Test]
        public void test_flash_fades_below_20pct_alpha_within_04s()
        {
            _flash.Trigger(1f);
            _flash.Tick(0.4f); // 1 − 2.6*0.4 = −0.04 → 0
            Assert.Less(_flash.Alpha, 0.2f, "boss-death flash clears the readability guardrail within 0.4s");
        }

        [Test]
        public void test_reduce_motion_disables_flash()
        {
            var settings = new ReduceMotionSettings();
            settings.SetReduceMotion(true); // FlashMult → 0
            var flash = new FlashSystem(_config, settings);
            flash.Trigger(1f);
            Assert.AreEqual(0f, flash.Intensity, 0.0001f, "no flash overlay under reduce-motion");
        }
    }
}
