using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.GameFeel;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.GameFeel
{
    /// <summary>
    /// GameFeel Story 005 (state + SFX budget) — the SOFTENED signature registry (game-feel.md §I.1). Tracks
    /// which parts are softened so the renderer can draw each one's glow ring, while capping the soften SFX to
    /// <see cref="GameFeelConfig.SoftenedSfxMaxPerFrame"/> per frame. Pure C#; the glow geometry/pulse and
    /// debris/weakness-frame particles are the renderer's visual follow-up.
    /// </summary>
    [TestFixture]
    public sealed class GameFeelSoftenedSignatureTests
    {
        private RecordingEventBus _bus;
        private GameFeelConfig _config;
        private SoftenedSignatureSystem _sys;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _config = ContentTestFactory.Create<GameFeelConfig>(("_softenedSfxMaxPerFrame", 2));
            _sys = new SoftenedSignatureSystem(_bus, _config);
        }

        [TearDown]
        public void TearDown()
        {
            _sys.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private void Soften(int partId) => _bus.Publish(new PartSoftened(partId, 1, 50f, 100f));

        [Test]
        public void test_softened_parts_all_register_but_sfx_is_capped_per_frame()
        {
            Soften(10);
            Soften(11);
            Soften(12); // third soften in the same frame

            Assert.AreEqual(3, _sys.SoftenedCount, "every softened part gets a glow ring");
            Assert.AreEqual(2, _sys.SfxThisFrame, "SFX capped at 2 per frame");
            Assert.IsTrue(_sys.IsSoftened(12));
        }

        [Test]
        public void test_reset_frame_replenishes_sfx_budget()
        {
            Soften(10); Soften(11); // budget spent
            Assert.AreEqual(2, _sys.SfxThisFrame);

            _sys.ResetFrame();
            Assert.AreEqual(0, _sys.SfxThisFrame);
            Soften(12);
            Assert.AreEqual(1, _sys.SfxThisFrame, "budget available again next frame");
        }

        [Test]
        public void test_softened_exit_clears_the_glow()
        {
            Soften(10);
            Assert.IsTrue(_sys.IsSoftened(10));

            _bus.Publish(new PartSoftenedExit(10, 1));
            Assert.IsFalse(_sys.IsSoftened(10));
            Assert.AreEqual(0, _sys.SoftenedCount);
        }

        [Test]
        public void test_part_break_clears_the_glow()
        {
            Soften(10);
            _bus.Publish(new PartBroke(10, 1, PartType.Normal, UnityEngine.Vector2.zero, 0, BreakQuality.Normal, null, false));
            Assert.IsFalse(_sys.IsSoftened(10), "a broken part no longer glows");
        }
    }
}
