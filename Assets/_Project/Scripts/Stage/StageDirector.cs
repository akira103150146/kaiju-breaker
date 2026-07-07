using System;
using System.Collections.Generic;
using System.Linq;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Assembles the per-run stage systems when a run starts (stage-system.md §D/§F/§G). On
    /// <see cref="LoadoutConfirmed"/> it recombines the segment pool into this run's
    /// <see cref="CurrentSequence"/>, and builds the run-scoped <see cref="PodDropTracker"/>,
    /// <see cref="PreBossLullController"/> and <see cref="OnboardingController"/> — then lets the onboarding
    /// review the first segment. It tracks the drawn segment ids so the next run's no-repeat window works,
    /// and tears the run-scoped systems down on <see cref="HuntEnded"/>.
    ///
    /// <para>Pure C# and driven by hooks (<see cref="NotifyLastSegmentEnded"/>, <see cref="Tick"/>) the scene's
    /// wave scheduler calls — so the whole per-run assembly is EditMode-testable. The <see cref="WaveSpawner"/>
    /// MonoBehaviour that instantiates enemies from <see cref="CurrentSequence"/> is the scene-level consumer
    /// (a thin follow-up). Session-lifetime; owned by the composition root.</para>
    /// </summary>
    public sealed class StageDirector
    {
        private readonly IEventBus _bus;
        private readonly ISaveService _save;
        private readonly IDifficultyProvider _difficulty;
        private readonly ISceneLoader _sceneLoader;
        private readonly RunController _run;
        private readonly StageDef _stage;
        private readonly PodDropConfig _podDrop;
        private readonly OnboardingConfig _onboardingConfig;
        private readonly Func<System.Random> _rngFactory;
        private readonly int _bossBreakablePartCount;
        private readonly Action<LoadoutConfirmed> _onLoadoutConfirmed;
        private readonly Action<HuntEnded> _onHuntEnded;

        private readonly List<string> _lastRunSegmentIds = new List<string>();

        private PodDropTracker _podTracker;
        private PreBossLullController _lull;
        private OnboardingController _onboarding;

        /// <summary>This run's drawn stage layout (null before the first run starts).</summary>
        public SegmentSequence CurrentSequence { get; private set; }

        /// <summary>The run-scoped pod-guarantee tracker (null between runs).</summary>
        public PodDropTracker PodTracker => _podTracker;

        public StageDirector(IEventBus bus, ISaveService save, IDifficultyProvider difficulty,
                             ISceneLoader sceneLoader, RunController run, StageDef stage, PodDropConfig podDrop,
                             OnboardingConfig onboarding, Func<System.Random> rngFactory, int bossBreakablePartCount)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _stage = stage ?? throw new ArgumentNullException(nameof(stage));
            _podDrop = podDrop ?? throw new ArgumentNullException(nameof(podDrop));
            _onboardingConfig = onboarding ?? throw new ArgumentNullException(nameof(onboarding));
            _rngFactory = rngFactory ?? (() => new System.Random());
            _bossBreakablePartCount = bossBreakablePartCount;

            _onLoadoutConfirmed = OnLoadoutConfirmed;
            _onHuntEnded = _ => EndRun();
            _bus.Subscribe(_onLoadoutConfirmed);
            _bus.Subscribe(_onHuntEnded);
        }

        /// <summary>Unsubscribe + tear down any live run (App teardown).</summary>
        public void Dispose()
        {
            _bus.Unsubscribe(_onLoadoutConfirmed);
            _bus.Unsubscribe(_onHuntEnded);
            EndRun();
        }

        private void OnLoadoutConfirmed(LoadoutConfirmed evt)
        {
            EndRun(); // clean up a prior run's scoped systems first

            var recombinator = new SegmentRecombinator(_stage, _difficulty.CurrentTier, _rngFactory());
            CurrentSequence = recombinator.Recombine(_lastRunSegmentIds);

            _podTracker = new PodDropTracker(_bus, _podDrop);
            _lull = new PreBossLullController(_bus, _sceneLoader, _stage.PreBossLullDurationSeconds,
                                              _podTracker, EnterBoss);
            _onboarding = new OnboardingController(_bus, _save, _onboardingConfig, _difficulty, _stage.StageId);
            _onboarding.ReviewFirstSegment(CurrentSequence);

            // Remember this run's segments for the next run's no-repeat window.
            _lastRunSegmentIds.Clear();
            _lastRunSegmentIds.AddRange(CurrentSequence.EscalatingSegments.Select(s => s.SegmentId));
        }

        /// <summary>
        /// Called by the wave scheduler when the last escalating segment finishes: end enemy spawning and
        /// start the pre-boss lull (which preloads the boss arena and, when ready, enters the boss).
        /// </summary>
        public void NotifyLastSegmentEnded()
        {
            _lull?.StartLull(_stage.BossKaijuId, _stage.BossKaijuId);
        }

        /// <summary>Advance the pre-boss lull timer (call each frame during STAGE).</summary>
        public void Tick(float deltaSeconds) => _lull?.Tick(deltaSeconds);

        private void EnterBoss()
        {
            if (_run.CurrentState == RunState.Stage) _run.EnterBoss(_bossBreakablePartCount);
        }

        private void EndRun()
        {
            _lull = null; // PreBossLull holds no subscriptions
            _podTracker?.Dispose();
            _podTracker = null;
            _onboarding?.Dispose();
            _onboarding = null;
        }
    }
}
