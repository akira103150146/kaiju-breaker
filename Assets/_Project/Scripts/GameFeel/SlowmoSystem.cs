using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Slow-motion — the brief speed-drop that lets a break/boss-death breathe (game-feel.md §I.3). On
    /// <see cref="PartBroke"/> the time scale drops instantly to a config minimum, holds for a config window
    /// (unscaled), then ramps linearly back to 1.0 at <see cref="GameFeelConfig.SlowmoRampRate"/>. A
    /// <see cref="BossCoreBroke"/> uses a deeper minimum + longer hold and overrides an in-progress part-break
    /// slow-mo; a fresh part-break during the hold resets (never adds to) the hold timer.
    ///
    /// <para>Pure C# + injected <see cref="ITimeScaleControl"/>, driven by <see cref="Tick"/> on unscaled
    /// time — fully EditMode-testable, no game-state mutation (pure presentation). Sequencing with hitstop
    /// (freeze first, then slow-mo) is the payoff orchestrator's job (Story 006). The accessibility multiplier
    /// (Story 007) lerps the minimum toward 1 and scales the hold — a value of 0 disables slow-mo entirely.</para>
    /// </summary>
    public sealed class SlowmoSystem
    {
        private enum Phase { Idle, Hold, Ramp }

        private readonly IEventBus _bus;
        private readonly GameFeelConfig _config;
        private readonly ITimeScaleControl _time;
        private readonly ReduceMotionSettings _motion; // optional runtime a11y scale (may be null)
        private readonly Action<PartBroke> _onPartBroke;
        private readonly Action<BossCoreBroke> _onBossCoreBroke;

        private Phase _phase = Phase.Idle;
        private float _min;
        private float _holdTimer;
        private float _rampElapsed;
        private bool _isBoss;

        /// <summary>True while slow-mo is holding or ramping.</summary>
        public bool IsActive => _phase != Phase.Idle;

        private readonly bool _subscribed;

        /// <param name="subscribeToBus">
        /// True (default) → self-subscribe to break events (standalone). False → a payoff sequencer triggers
        /// it via <see cref="TriggerPartBreak"/>/<see cref="TriggerBossDeath"/> after hitstop ends, so the two
        /// don't both drive the time scale on the same event.
        /// </param>
        public SlowmoSystem(IEventBus bus, GameFeelConfig config, ITimeScaleControl time,
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

        /// <summary>Begin part-break slow-mo (a boss slow-mo in progress is not overridden).</summary>
        public void TriggerPartBreak()
        {
            if (IsActive && _isBoss) return;
            Begin(_config.SlowmoPartBreakTimescale, _config.SlowmoPartBreakHoldSeconds, isBoss: false);
        }

        /// <summary>Begin boss-death slow-mo (deeper + longer; overrides a part-break slow-mo).</summary>
        public void TriggerBossDeath() => Begin(_config.SlowmoBossDeathTimescale, _config.SlowmoBossDeathHoldSeconds, isBoss: true);

        private void OnPartBroke(PartBroke evt) => TriggerPartBreak();
        private void OnBossCoreBroke(BossCoreBroke evt) => TriggerBossDeath();

        private void Begin(float rawMin, float rawHold, bool isBoss)
        {
            float mult = Mathf.Clamp01(_config.SlowmoAccessibilityMult * (_motion?.SlowmoMult ?? 1f));
            if (mult <= 0f) return; // reduce-motion → no slow-mo

            _min = Mathf.Lerp(1f, rawMin, mult);
            _holdTimer = rawHold * mult; // reset, never accumulate
            _isBoss = isBoss;
            _phase = Phase.Hold;
            _time.TimeScale = _min;
        }

        /// <summary>Advance slow-mo on <paramref name="unscaledDeltaSeconds"/>; ramps the time scale back to 1.0.</summary>
        public void Tick(float unscaledDeltaSeconds)
        {
            switch (_phase)
            {
                case Phase.Hold:
                    _time.TimeScale = _min;
                    _holdTimer -= unscaledDeltaSeconds;
                    if (_holdTimer <= 0f) { _phase = Phase.Ramp; _rampElapsed = 0f; }
                    break;

                case Phase.Ramp:
                    _rampElapsed += unscaledDeltaSeconds;
                    float ts = Mathf.Min(1f, _min + _config.SlowmoRampRate * _rampElapsed); // §D.2 ramp
                    _time.TimeScale = ts;
                    if (ts >= 1f) { _time.TimeScale = 1f; _phase = Phase.Idle; _isBoss = false; }
                    break;
            }
        }
    }
}
