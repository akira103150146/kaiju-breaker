using System;
using KaijuBreaker.Core;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Drives the pre-boss lull — the fixed breather between the last escalating wave and the boss
    /// (stage-system.md §G.1). On <see cref="StartLull"/> it stops enemy spawning (the caller does that),
    /// publishes <see cref="PreBossLullStarted"/>, tops up weapon pods via the tracker, and begins an additive
    /// async preload of the boss arena. It then waits BOTH for the lull timer to elapse AND the scene to be
    /// ready before completing — publishing <see cref="BossArenaEntered"/> and invoking the
    /// <c>onLullComplete</c> callback (wired to <see cref="RunController.EnterBoss"/>). Requiring both gates
    /// guarantees a stall-free STAGE→BOSS transition (§L.1).
    ///
    /// <para>Pure C# (tick-driven, no Unity API), so timing + the scene-ready gate are EditMode-testable with
    /// a fake <see cref="ISceneLoader"/>. Scene lifecycle itself lives in <c>App</c> (ADR-0005).</para>
    /// </summary>
    public sealed class PreBossLullController
    {
        private readonly IEventBus _bus;
        private readonly ISceneLoader _sceneLoader;
        private readonly float _lullDuration;
        private readonly PodDropTracker _podTracker; // optional; may be null
        private readonly Action _onLullComplete;     // wired to RunController.EnterBoss

        private float _timer;
        private bool _sceneReady;
        private string _kaijuId;

        /// <summary>True while the lull is running (started, not yet completed).</summary>
        public bool IsLullActive { get; private set; }

        /// <summary>True once the lull has completed and the boss was entered.</summary>
        public bool IsComplete { get; private set; }

        /// <summary>True once the boss arena scene finished its async load.</summary>
        public bool SceneReady => _sceneReady;

        public PreBossLullController(IEventBus bus, ISceneLoader sceneLoader, float lullDuration,
                                     PodDropTracker podTracker, Action onLullComplete)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _lullDuration = Math.Max(0f, lullDuration);
            _podTracker = podTracker;
            _onLullComplete = onLullComplete;
        }

        /// <summary>
        /// Begin the lull for <paramref name="kaijuId"/>, preloading <paramref name="bossSceneName"/>. Idempotent:
        /// a second call while active (or after completion) is a no-op (stage-system.md AC-1 edge).
        /// </summary>
        public void StartLull(string kaijuId, string bossSceneName)
        {
            if (IsLullActive || IsComplete) return;

            IsLullActive = true;
            _kaijuId = kaijuId;
            _timer = _lullDuration;
            _sceneReady = false;

            _bus.Publish(new PreBossLullStarted(kaijuId));
            _podTracker?.SpawnPreBossLullPods();
            _sceneLoader.LoadAdditiveAsync(bossSceneName, () => _sceneReady = true);
        }

        /// <summary>
        /// Advance the lull timer. Once the timer has elapsed AND the arena is ready, complete the lull:
        /// publish <see cref="BossArenaEntered"/> and invoke the EnterBoss callback. If the scene is still
        /// loading when the timer runs out, the controller keeps waiting (the timer clamps at 0).
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsLullActive || IsComplete) return;

            if (_timer > 0f) _timer = Math.Max(0f, _timer - deltaSeconds);

            if (_timer <= 0f && _sceneReady)
                Complete();
        }

        private void Complete()
        {
            IsLullActive = false;
            IsComplete = true;
            _bus.Publish(new BossArenaEntered(_kaijuId));
            _onLullComplete?.Invoke();
        }
    }
}
