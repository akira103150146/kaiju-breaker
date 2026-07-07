using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Stage Story 006 — pre-boss lull + boss-arena preload gate (stage-system.md §G.1; TR-stage-007). The
    /// <see cref="PreBossLullController"/> is pure tick-driven C#, so this is an EditMode Logic test with a
    /// fake <see cref="ISceneLoader"/> (immediate or delayed). AC-4 (BossCoreBreak → RESULTS) is RunController's
    /// (story-001) and already covered.
    ///
    /// <para><b>Reconciliation:</b> the lull is a companion controller (not folded into the committed
    /// RunController) that invokes an <c>onLullComplete</c> callback wired to <c>RunController.EnterBoss</c>;
    /// the real additive scene load lives in App (ISceneLoader impl) — a thin follow-up.</para>
    /// </summary>
    [TestFixture]
    public sealed class PreBossLullTests
    {
        private sealed class FakeSceneLoader : ISceneLoader
        {
            public bool Immediate = true;
            public string LoadedScene;
            private Action _pending;
            public void LoadAdditiveAsync(string sceneName, Action onComplete)
            {
                LoadedScene = sceneName;
                if (Immediate) onComplete?.Invoke(); else _pending = onComplete;
            }
            public void CompletePending() { _pending?.Invoke(); _pending = null; }
            public void UnloadAsync(string sceneName, Action onComplete) => onComplete?.Invoke();
        }

        private RecordingEventBus _bus;
        private PodDropConfig _podConfig;
        private PodDropTracker _podTracker;
        private FakeSceneLoader _loader;
        private bool _enterBossCalled;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _podConfig = ContentTestFactory.Create<PodDropConfig>(("_preBossLullPodCount", 1));
            _podTracker = new PodDropTracker(_bus, _podConfig);
            _loader = new FakeSceneLoader();
            _enterBossCalled = false;
        }

        [TearDown]
        public void TearDown()
        {
            _podTracker.Dispose();
            UnityEngine.Object.DestroyImmediate(_podConfig);
        }

        private PreBossLullController Make(float duration = 20f) =>
            new PreBossLullController(_bus, _loader, duration, _podTracker, () => _enterBossCalled = true);

        // ── AC-1: lull start publishes event + spawns pods + idempotent ───────

        [Test]
        public void test_start_lull_publishes_event_preloads_scene_and_tops_up_pods()
        {
            var lull = Make();
            lull.StartLull("carapex", "kaiju_carapex");

            Assert.IsTrue(lull.IsLullActive);
            Assert.AreEqual(1, _bus.CountOf<PreBossLullStarted>());
            Assert.AreEqual("carapex", _bus.Events<PreBossLullStarted>()[0].KaijuId);
            Assert.AreEqual("kaiju_carapex", _loader.LoadedScene, "boss arena preload started");
            Assert.GreaterOrEqual(_bus.CountOf<PodSpawnRequested>(), 1, "pre-boss lull tops up a pod");
        }

        [Test]
        public void test_start_lull_is_idempotent()
        {
            var lull = Make();
            lull.StartLull("carapex", "kaiju_carapex");
            lull.StartLull("carapex", "kaiju_carapex"); // duplicate
            Assert.AreEqual(1, _bus.CountOf<PreBossLullStarted>(), "second StartLull is a no-op");
        }

        // ── AC-2: timer elapses → EnterBoss (scene ready) ─────────────────────

        [Test]
        public void test_lull_completes_and_enters_boss_when_timer_elapses()
        {
            _loader.Immediate = true; // scene ready during StartLull
            var lull = Make(20f);
            lull.StartLull("carapex", "kaiju_carapex");

            lull.Tick(10f);
            Assert.IsFalse(lull.IsComplete, "not yet — halfway through the lull");
            lull.Tick(10f);

            Assert.IsTrue(lull.IsComplete);
            Assert.IsTrue(_enterBossCalled, "EnterBoss callback fired");
        }

        // ── AC-2 edge: slow scene load → wait for readiness ───────────────────

        [Test]
        public void test_lull_waits_for_scene_when_load_is_slow()
        {
            _loader.Immediate = false; // scene NOT ready yet
            var lull = Make(20f);
            lull.StartLull("carapex", "kaiju_carapex");

            lull.Tick(25f); // timer elapsed, but scene still loading
            Assert.IsFalse(lull.IsComplete, "must wait for the arena to finish loading");
            Assert.IsFalse(_enterBossCalled);

            _loader.CompletePending(); // scene finished
            lull.Tick(0.1f);
            Assert.IsTrue(lull.IsComplete, "completes once the scene is ready");
            Assert.IsTrue(_enterBossCalled);
        }

        // ── AC-3: BossArenaEntered event ──────────────────────────────────────

        [Test]
        public void test_boss_arena_entered_event_published_on_completion()
        {
            var lull = Make(1f);
            lull.StartLull("carapex", "kaiju_carapex");
            lull.Tick(1f);

            Assert.AreEqual(1, _bus.CountOf<BossArenaEntered>());
            Assert.AreEqual("carapex", _bus.Events<BossArenaEntered>()[0].KaijuId);
        }

        [Test]
        public void test_no_completion_before_timer_even_if_scene_ready()
        {
            _loader.Immediate = true;
            var lull = Make(20f);
            lull.StartLull("carapex", "kaiju_carapex");
            lull.Tick(5f);
            Assert.IsFalse(lull.IsComplete);
            Assert.AreEqual(0, _bus.CountOf<BossArenaEntered>());
        }
    }
}
