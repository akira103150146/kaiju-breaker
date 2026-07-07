using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Stage per-run assembly — <see cref="StageDirector"/> (stage-system.md §D/§F/§G). On LoadoutConfirmed it
    /// recombines the pool into a run sequence and stands up the run-scoped pod-guarantee tracker, pre-boss
    /// lull, and onboarding; NotifyLastSegmentEnded → lull → EnterBoss; HuntEnded tears the run down. Pure C#,
    /// seeded RNG + fake scene loader → EditMode-testable.
    /// </summary>
    [TestFixture]
    public sealed class StageDirectorTests
    {
        private sealed class FakeDifficulty : IDifficultyProvider
        {
            public DifficultyTier CurrentTier { get; set; } = DifficultyTier.D1;
            public float BulletDensityMult => 1f;
            public float EnemyCountMult => 1f;
        }

        private sealed class ImmediateScenes : ISceneLoader
        {
            public void LoadAdditiveAsync(string sceneName, Action onComplete) => onComplete?.Invoke();
            public void UnloadAsync(string sceneName, Action onComplete) => onComplete?.Invoke();
        }

        private RecordingEventBus _bus;
        private RecordingSaveService _save;
        private RunController _run;
        private StageDef _stage;
        private PodDropConfig _podDrop;
        private OnboardingConfig _onboarding;
        private StageDirector _director;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _save = new RecordingSaveService();
            _run = new RunController(_bus, _save);

            var pool = new[]
            {
                Seg("s1_01", 1, eliteWave: -1), Seg("s1_02", 2, eliteWave: -1), Seg("s1_03", 3, eliteWave: -1),
            };
            _stage = ContentTestFactory.Create<StageDef>(
                ("_stageId", "stage_01"), ("_bossKaijuId", "carapex"),
                ("_segmentDrawCount", 2), ("_segmentPool", pool), ("_preBossLullDurationSeconds", 1f));
            _podDrop = ContentTestFactory.Create<PodDropConfig>(("_preBossLullPodCount", 1));
            _onboarding = ContentTestFactory.Create<OnboardingConfig>();

            _director = new StageDirector(_bus, _save, new FakeDifficulty(), new ImmediateScenes(), _run,
                                          _stage, _podDrop, _onboarding, () => new System.Random(42), bossBreakablePartCount: 3);
        }

        [TearDown]
        public void TearDown()
        {
            _director.Dispose();
            _run.Dispose();
            UnityEngine.Object.DestroyImmediate(_stage);
            UnityEngine.Object.DestroyImmediate(_podDrop);
            UnityEngine.Object.DestroyImmediate(_onboarding);
        }

        private static SegmentDef Seg(string id, int weight, int eliteWave) => ContentTestFactory.Create<SegmentDef>(
            ("_segmentId", id), ("_difficultyWeight", weight), ("_eliteWaveIndex", eliteWave));

        private void ConfirmLoadout() => _bus.Publish(new LoadoutConfirmed(WeaponId.L1, WeaponId.M1, DifficultyTier.D1));

        [Test]
        public void test_loadout_confirmed_assembles_the_run_sequence_and_pod_tracker()
        {
            ConfirmLoadout();

            Assert.IsNotNull(_director.CurrentSequence);
            Assert.AreEqual(2, _director.CurrentSequence.EscalatingSegments.Count, "drew SegmentDrawCount segments");
            Assert.IsNotNull(_director.PodTracker, "run-scoped pod tracker built");
        }

        [Test]
        public void test_onboarding_forces_first_segment_pod_on_stage_01()
        {
            ConfirmLoadout(); // first segment has no elite → onboarding forces a primary pod
            Assert.AreEqual(1, _bus.CountOf<ForceFirstSegmentPodCarrier>());
        }

        [Test]
        public void test_run_scoped_pod_tracker_responds_to_elite_kills()
        {
            ConfirmLoadout();
            _bus.Clear();
            _bus.Publish(new EliteKilled(PodPoolPreference.Primary, UnityEngine.Vector2.zero));
            Assert.GreaterOrEqual(_bus.CountOf<PodSpawnRequested>(), 1, "elite kill drove the run's pod tracker");
        }

        [Test]
        public void test_last_segment_then_lull_enters_boss()
        {
            ConfirmLoadout(); // Run → STAGE
            Assert.AreEqual(RunState.Stage, _run.CurrentState);

            _director.NotifyLastSegmentEnded();
            Assert.AreEqual(1, _bus.CountOf<PreBossLullStarted>(), "lull started, boss scene preloaded");

            _director.Tick(1.0f); // elapse the lull; immediate scene → ready
            Assert.AreEqual(RunState.Boss, _run.CurrentState, "lull completion entered the boss");
        }

        [Test]
        public void test_hunt_ended_tears_down_the_run_scope()
        {
            ConfirmLoadout();
            Assert.IsNotNull(_director.PodTracker);
            _bus.Publish(new HuntEnded(isAllPartsBroken: true));
            Assert.IsNull(_director.PodTracker, "run-scoped systems disposed on hunt end");
        }
    }
}
