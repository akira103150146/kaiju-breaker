using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Difficulty;
using KaijuBreaker.Economy;
using KaijuBreaker.GameFeel;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Meta;
using KaijuBreaker.Stage;

namespace KaijuBreaker.App
{
    /// <summary>
    /// The composition root (ADR-0005 §3) as pure C#: constructs the single event bus and every always-on
    /// system, wiring each with constructor DI — no system references another directly, they cross-talk only
    /// through <see cref="Bus"/> and the Core query interfaces. Config comes entirely from a
    /// <see cref="ContentRegistry"/> (ADR-0003); Unity subsystems arrive as injected adapters
    /// (<see cref="ITimeScaleControl"/>), so this whole graph is EditMode-testable end-to-end.
    ///
    /// <para><b>Wired now:</b> Difficulty → Meta (the real ISaveService/IWeaponTierQuery backend) → KaijuParts
    /// → Economy → GameFeel (hitstop/shake/flash/softened + reduce-motion) → Stage RunController. <b>Deferred:</b>
    /// SlowmoSystem is NOT wired alongside Hitstop yet — both drive the time scale, so they need the break-
    /// payoff sequencer (game-feel story-006 orchestration follow-up) to hand off cleanly. Per-run stage
    /// assembly (WaveSpawner / PodDropTracker / PreBossLull / Onboarding) is built at run start (a later
    /// wiring step); this root owns the session-lifetime systems.</para>
    /// </summary>
    public sealed class GameComposition
    {
        public IEventBus Bus { get; }
        public DifficultySystem Difficulty { get; }
        public MetaSaveService Meta { get; }
        public PartStateSystem Parts { get; }
        public KaijuThemeRegistry Themes { get; }
        public EconomyService Economy { get; }
        public ReduceMotionSettings Motion { get; }
        public ReduceMotionController ReduceMotion { get; }
        public HitstopSystem Hitstop { get; }
        public SlowmoSystem Slowmo { get; }
        public ShakeSystem Shake { get; }
        public FlashSystem Flash { get; }
        public SoftenedSignatureSystem Softened { get; }
        public BreakPayoffSequencer Payoff { get; }
        public RunController Run { get; }

        /// <summary>Per-run stage assembly (null if the ContentRegistry has no stage config wired).</summary>
        public StageDirector Stage { get; }

        public GameComposition(ContentRegistry content, string saveDirectory, ITimeScaleControl timeScale,
                               ISceneLoader sceneLoader = null, Func<float> shakeRandom = null)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (timeScale == null) throw new ArgumentNullException(nameof(timeScale));
            sceneLoader = sceneLoader ?? new ImmediateSceneLoader();

            Bus = new TypedEventBus();

            Difficulty = new DifficultySystem(content.Difficulty);

            // Meta — the real persistence backend (implements ISaveService + IWeaponTierQuery).
            var serializer = new CanonicalJsonSerializer();
            var writer = new AtomicSaveWriter(content.Save, saveDirectory, serializer);
            var worker = new SaveWorker(writer, content.Save.SaveWorkerIdleMs);
            var loader = new SaveLoader(content.Save, serializer, Bus, writer);
            Meta = new MetaSaveService(content.Save, Bus, loader, SaveMigrator.Default(), worker);
            Meta.Initialize();

            Parts = new PartStateSystem(Bus, content.WeaponBalance, content.PartSystem); // IPartStateQuery

            Themes = new KaijuThemeRegistry();
            Economy = new EconomyService(content.Economy, Bus, Meta, Themes, Meta);

            // GameFeel — reduce-motion multipliers persist through Meta's flags.
            Motion = new ReduceMotionSettings();
            ReduceMotion = new ReduceMotionController(Motion, Meta);
            // Hitstop + Slowmo are driven by the payoff sequencer (not self-subscribed) so the freeze hands
            // off cleanly to slow-mo instead of both fighting over the time scale.
            Hitstop = new HitstopSystem(Bus, content.GameFeel, timeScale, Motion, subscribeToBus: false);
            Slowmo = new SlowmoSystem(Bus, content.GameFeel, timeScale, Motion, subscribeToBus: false);
            Shake = new ShakeSystem(Bus, content.GameFeel, shakeRandom, Motion);
            Flash = new FlashSystem(content.GameFeel, Motion);
            Softened = new SoftenedSignatureSystem(Bus, content.GameFeel);
            Payoff = new BreakPayoffSequencer(Bus, Hitstop, Slowmo, Flash);

            Run = new RunController(Bus, Meta);

            // Per-run stage assembly — only when the stage config is wired (MVP stage_01).
            if (content.Stage != null && content.PodDrop != null && content.Onboarding != null)
            {
                Stage = new StageDirector(Bus, Meta, Difficulty, sceneLoader, Run,
                                          content.Stage, content.PodDrop, content.Onboarding,
                                          () => new System.Random(), bossBreakablePartCount: 0);
            }
        }

        /// <summary>Advance the frame-driven feel systems on unscaled time (call every frame from App).</summary>
        public void TickGameFeel(float unscaledDeltaSeconds)
        {
            Hitstop.Tick(unscaledDeltaSeconds);
            Slowmo.Tick(unscaledDeltaSeconds);
            Shake.Tick(unscaledDeltaSeconds);
            Flash.Tick(unscaledDeltaSeconds);
            Softened.ResetFrame();
        }

        /// <summary>Tear down every subscribing system (App teardown). DifficultySystem/Flash hold no subscriptions.</summary>
        public void Dispose()
        {
            Stage?.Dispose();
            Run.Dispose();
            Payoff.Dispose();
            Softened.Dispose();
            Shake.Dispose();
            Slowmo.Dispose();
            Hitstop.Dispose();
            Economy.Dispose();
            Parts.Dispose();
            Meta.Dispose();
        }
    }
}
