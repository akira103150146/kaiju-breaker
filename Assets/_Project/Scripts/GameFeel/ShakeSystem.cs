using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Screen shake — a decaying trauma value that offsets the camera (game-feel.md §I.4/§D.1). Each impact
    /// raises the current magnitude with <c>max</c> (never additive) and clamps to
    /// <see cref="GameFeelConfig.ShakeMagnitudeCap"/>, so even a torpedo spam can't stack into an unreadable
    /// quake. It decays linearly on UNSCALED time (so slow-mo/hitstop don't freeze the decay), and each frame
    /// produces an integer-pixel random offset that zeroes out below <see cref="GameFeelConfig.ShakeThreshold"/>
    /// (no micro-jitter).
    ///
    /// <para>Pure C# — the random source is injected (defaults to <c>UnityEngine.Random</c>) so the offset
    /// math is deterministically testable; the camera adapter (App/Presentation) applies
    /// <see cref="ComputeOffset"/>. A pure-presentation consumer (no game-state mutation). Accessibility
    /// multiplier scales every magnitude (0 = no shake for reduce-motion, Story 007).</para>
    /// </summary>
    public sealed class ShakeSystem
    {
        private readonly IEventBus _bus;
        private readonly GameFeelConfig _config;
        private readonly Func<float> _nextSignedUnit; // returns [-1, 1]

        private readonly Action<PartBroke> _onPartBroke;
        private readonly Action<BossCoreBroke> _onBossCoreBroke;
        private readonly Action<PartSoftened> _onPartSoftened;
        private readonly Action<PartStaggered> _onPartStaggered;
        private readonly Action<WaveHit> _onWaveHit;
        private readonly Action<MissileHit> _onMissileHit;

        private float _current;

        /// <summary>Current shake magnitude (px); decays toward 0.</summary>
        public float CurrentMagnitude => _current;

        public ShakeSystem(IEventBus bus, GameFeelConfig config, Func<float> nextSignedUnit = null)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _nextSignedUnit = nextSignedUnit ?? (() => UnityEngine.Random.Range(-1f, 1f));

            _onPartBroke = e => AddShake(_config.ShakeMagPartBreakBase);
            _onBossCoreBroke = e => AddShake(_config.ShakeMagBossDeath);
            _onPartSoftened = e => AddShake(_config.ShakeMagSoften);
            _onPartStaggered = e => AddShake(_config.ShakeMagArmorStrip);
            _onWaveHit = e => AddShake(_config.ShakeMagL3Shockwave);
            _onMissileHit = OnMissileHit;

            _bus.Subscribe(_onPartBroke);
            _bus.Subscribe(_onBossCoreBroke);
            _bus.Subscribe(_onPartSoftened);
            _bus.Subscribe(_onPartStaggered);
            _bus.Subscribe(_onWaveHit);
            _bus.Subscribe(_onMissileHit);
        }

        /// <summary>Unsubscribe on teardown.</summary>
        public void Dispose()
        {
            _bus.Unsubscribe(_onPartBroke);
            _bus.Unsubscribe(_onBossCoreBroke);
            _bus.Unsubscribe(_onPartSoftened);
            _bus.Unsubscribe(_onPartStaggered);
            _bus.Unsubscribe(_onWaveHit);
            _bus.Unsubscribe(_onMissileHit);
        }

        private void OnMissileHit(MissileHit evt)
        {
            // Only the heavy missiles carry a shake signature (game-feel.md §D.1).
            if (evt.Weapon == WeaponId.M3) AddShake(_config.ShakeMagM3TorpedoHit);
            else if (evt.Weapon == WeaponId.M4) AddShake(_config.ShakeMagM4Cluster);
        }

        /// <summary>
        /// Raise the shake with <paramref name="magnitude"/> (max, not add) and clamp to the cap. Applies the
        /// accessibility multiplier — 0 leaves the shake untouched.
        /// </summary>
        public void AddShake(float magnitude)
        {
            float m = Mathf.Max(0f, magnitude) * _config.ShakeAccessibilityMult;
            _current = Mathf.Min(Mathf.Max(_current, m), _config.ShakeMagnitudeCap);
        }

        /// <summary>Decay the shake on <paramref name="unscaledDeltaSeconds"/> (linear, floored at 0).</summary>
        public void Tick(float unscaledDeltaSeconds)
        {
            _current = Mathf.Max(0f, _current - _config.ShakeDecayRate * unscaledDeltaSeconds);
        }

        /// <summary>
        /// The camera offset for this frame: an integer-pixel random displacement scaled by the current
        /// magnitude, or zero when below the threshold (no micro-jitter). Random direction each call.
        /// </summary>
        public Vector2 ComputeOffset()
        {
            if (_current < _config.ShakeThreshold) return Vector2.zero;
            float ox = Mathf.Floor(_nextSignedUnit() * _current);
            float oy = Mathf.Floor(_nextSignedUnit() * _current);
            return new Vector2(ox, oy);
        }
    }
}
