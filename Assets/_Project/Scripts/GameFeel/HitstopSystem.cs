using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Hitstop — the brief full freeze that sells an impact (game-feel.md §I.2). Subscribes to
    /// <see cref="PartBroke"/> and <see cref="BossCoreBroke"/> and drops the time scale to 0 for a
    /// config-driven duration, counting the freeze down on UNSCALED time (so it can end itself while frozen).
    /// A boss-death freeze overrides and is not overridden by a part-break; consecutive part-breaks RESET the
    /// timer (never accumulate), so a break chain can never freeze longer than one part-break window.
    ///
    /// <para>Pure C# + injected <see cref="ITimeScaleControl"/> — no Unity Time API — so timing is fully
    /// EditMode-testable via <see cref="Tick"/>. A pure-presentation consumer: it never mutates game state
    /// (control-manifest §3 GameFeel). The accessibility multiplier (<see cref="GameFeelConfig.HitstopAccessibilityMult"/>,
    /// Story 007) is applied at trigger — a value of 0 disables hitstop for reduce-motion.</para>
    /// </summary>
    public sealed class HitstopSystem
    {
        private readonly IEventBus _bus;
        private readonly GameFeelConfig _config;
        private readonly ITimeScaleControl _time;
        private readonly ReduceMotionSettings _motion; // optional runtime a11y scale (may be null)
        private readonly Action<PartBroke> _onPartBroke;
        private readonly Action<BossCoreBroke> _onBossCoreBroke;

        private float _timer;
        private bool _active;
        private bool _isBossHitstop;

        /// <summary>True while the freeze is in effect.</summary>
        public bool IsActive => _active;

        /// <summary>Seconds of freeze remaining.</summary>
        public float RemainingSeconds => _timer;

        private readonly bool _subscribed;

        /// <summary>Fired the frame the freeze ends (used by the break-payoff sequencer to hand off to slow-mo).</summary>
        public event System.Action HitstopEnded;

        /// <param name="subscribeToBus">
        /// When true (default, standalone use) the system self-subscribes to break events. Pass false when a
        /// payoff sequencer drives it via <see cref="TriggerPartBreak"/>/<see cref="TriggerBossDeath"/> so it
        /// can sequence hitstop → slow-mo without two systems fighting over the time scale.
        /// </param>
        public HitstopSystem(IEventBus bus, GameFeelConfig config, ITimeScaleControl time,
                             ReduceMotionSettings motion = null, bool subscribeToBus = true)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _motion = motion;
            _onPartBroke = OnPartBroke;
            _onBossCoreBroke = OnBossCoreBroke;
            _subscribed = subscribeToBus;
            if (_subscribed)
            {
                _bus.Subscribe(_onPartBroke);
                _bus.Subscribe(_onBossCoreBroke);
            }
        }

        /// <summary>Unsubscribe on teardown.</summary>
        public void Dispose()
        {
            if (!_subscribed) return;
            _bus.Unsubscribe(_onPartBroke);
            _bus.Unsubscribe(_onBossCoreBroke);
        }

        /// <summary>Freeze for the part-break window (a boss freeze in progress is not overridden, §I.2).</summary>
        public void TriggerPartBreak()
        {
            if (_active && _isBossHitstop) return;
            Freeze(_config.HitstopPartBreakMs, isBoss: false);
        }

        /// <summary>Freeze for the boss-death window (overrides any in-progress part-break freeze).</summary>
        public void TriggerBossDeath() => Freeze(_config.HitstopBossDeathMs, isBoss: true);

        private void OnPartBroke(PartBroke evt) => TriggerPartBreak();
        private void OnBossCoreBroke(BossCoreBroke evt) => TriggerBossDeath();

        private void Freeze(float milliseconds, bool isBoss)
        {
            float motionMult = _motion?.HitstopMult ?? 1f;
            float seconds = milliseconds * 0.001f * _config.HitstopAccessibilityMult * motionMult;
            if (seconds <= 0f) return; // reduce-motion (a11y mult 0) → no freeze
            _timer = seconds;          // reset, never accumulate
            _isBossHitstop = isBoss;
            _active = true;
            _time.TimeScale = 0f;
        }

        /// <summary>
        /// Advance the freeze on <paramref name="unscaledDeltaSeconds"/> (real time). Restores the time scale
        /// once the timer elapses. Driven by the GameFeel orchestrator's unscaled update loop.
        /// </summary>
        public void Tick(float unscaledDeltaSeconds)
        {
            if (!_active) return;
            _timer -= unscaledDeltaSeconds;
            if (_timer <= 0f)
            {
                _timer = 0f;
                _active = false;
                _isBossHitstop = false;
                _time.TimeScale = 1f;
                HitstopEnded?.Invoke(); // sequencer hands off to slow-mo here (same frame → no gap)
            }
        }
    }
}
