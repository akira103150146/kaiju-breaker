using System.Collections.Generic;
using System.IO;
using KaijuBreaker.App;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.GameFeel;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.App
{
    /// <summary>
    /// App composition-root integration (ADR-0005 §3). Builds the whole always-on system graph via
    /// <see cref="GameComposition"/> from a <see cref="ContentRegistry"/> and drives a mini run through the
    /// event bus, proving the systems are wired and cross-talk correctly end-to-end — no per-system mocks,
    /// no scene. Verifies the run state machine advances, and a part break flows through Meta (stats +
    /// persistence backend), GameFeel (shake + hitstop), and Economy (no fail-loud throw with a registered
    /// theme) via a single publish.
    /// </summary>
    [TestFixture]
    public sealed class GameCompositionTests
    {
        private sealed class FakeTime : ITimeScaleControl { public float TimeScale { get; set; } = 1f; }

        private string _dir;
        private readonly List<Object> _assets = new List<Object>();
        private ContentRegistry _content;
        private FakeTime _time;
        private GameComposition _comp;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "kaiju_composition_test");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);

            _content = BuildRegistry();
            _time = new FakeTime();
            _comp = new GameComposition(_content, _dir, _time);
            _comp.Themes.Register(1, KaijuTheme.Carapace); // so Economy can award a core (fail-loud otherwise)
        }

        [TearDown]
        public void TearDown()
        {
            _comp.Dispose();
            foreach (var a in _assets) if (a != null) Object.DestroyImmediate(a);
            _assets.Clear();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private T Asset<T>(params (string, object)[] overrides) where T : ScriptableObject
        {
            var so = ContentTestFactory.Create<T>(overrides);
            _assets.Add(so);
            return so;
        }

        private ContentRegistry BuildRegistry()
        {
            return Asset<ContentRegistry>(
                ("_weaponBalance", Asset<WeaponBalanceConfig>()),
                ("_partSystem", Asset<PartSystemConfig>()),
                ("_difficulty", Asset<DifficultyConfig>()),
                ("_gameFeel", Asset<GameFeelConfig>(("_hitstopPartBreakMs", 115f), ("_hitstopAccessibilityMult", 1f),
                                                    ("_shakeMagPartBreakBase", 11f), ("_shakeAccessibilityMult", 1f),
                                                    ("_shakeMagnitudeCap", 24f), ("_shakeThreshold", 0.3f))),
                ("_economy", Asset<EconomyConfig>()),
                ("_save", Asset<SaveConfig>()));
        }

        private static PartBroke Break(int kaijuId) =>
            new PartBroke(10, kaijuId, PartType.Normal, Vector2.zero, 0, BreakQuality.Normal, null, false);

        // ── The graph is built and every system present ───────────────────────

        [Test]
        public void test_composition_builds_all_always_on_systems()
        {
            Assert.IsNotNull(_comp.Bus);
            Assert.IsNotNull(_comp.Meta);
            Assert.IsTrue(_comp.Meta.IsInitialized, "Meta initialised from a fresh save");
            Assert.IsNotNull(_comp.Difficulty);
            Assert.IsNotNull(_comp.Parts);
            Assert.IsNotNull(_comp.Economy);
            Assert.IsNotNull(_comp.Run);
            Assert.IsNotNull(_comp.Hitstop);
            Assert.IsNotNull(_comp.Shake);
        }

        // ── Run state machine drives off the shared bus ───────────────────────

        [Test]
        public void test_loadout_confirmed_advances_the_run_controller()
        {
            Assert.AreEqual(RunState.Loadout, _comp.Run.CurrentState);
            _comp.Bus.Publish(new LoadoutConfirmed(WeaponId.L1, WeaponId.M1, DifficultyTier.D1));
            Assert.AreEqual(RunState.Stage, _comp.Run.CurrentState, "RunController transitioned via the composed bus");
        }

        // ── One PartBroke fans out across Meta + GameFeel (+ Economy no-throw) ─

        [Test]
        public void test_part_break_fans_out_through_meta_and_gamefeel()
        {
            long brokenBefore = _comp.Meta.State.Stats.TotalPartsBroken;

            _comp.Bus.Publish(Break(kaijuId: 1)); // Economy has a theme for id 1, so it won't fail-loud

            Assert.AreEqual(brokenBefore + 1, _comp.Meta.State.Stats.TotalPartsBroken, "Meta banked the break stat");
            Assert.Greater(_comp.Shake.CurrentMagnitude, 0f, "shake fired");
            Assert.AreEqual(0f, _time.TimeScale, "hitstop froze the time scale");
        }

        [Test]
        public void test_break_payoff_hands_hitstop_off_to_slowmo()
        {
            _comp.Bus.Publish(Break(kaijuId: 1));
            Assert.AreEqual(0f, _time.TimeScale, "hitstop froze first");

            _comp.TickGameFeel(0.2f); // past the 115ms hitstop window → sequencer hands off to slow-mo
            Assert.Greater(_time.TimeScale, 0f, "no longer frozen");
            Assert.Less(_time.TimeScale, 1f, "now in slow-mo, not back to full speed");
        }

        // ── Reduce-motion flows through the shared settings + Meta persistence ─

        [Test]
        public void test_reduce_motion_toggle_affects_shake_and_persists()
        {
            _comp.ReduceMotion.SetEnabled(true);
            Assert.AreEqual(0.25f, _comp.Motion.ShakeMult, 0.0001f);
            Assert.IsTrue(_comp.Meta.GetFlag(ReduceMotionController.ReduceMotionFlag), "persisted to the save backend");

            _comp.Bus.Publish(Break(kaijuId: 1)); // shake now scaled to 25%
            Assert.AreEqual(11f * 0.25f, _comp.Shake.CurrentMagnitude, 0.001f, "shake at reduced intensity");
        }
    }
}
