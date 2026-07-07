using System;
using KaijuBreaker.Core;

namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Orchestrates the break payoff so the time-scale effects fire in the right ORDER (game-feel.md §D.4):
    /// on a break it flashes and freezes (hitstop), then — the frame the freeze ends — hands off to slow-mo.
    /// Without this, <see cref="HitstopSystem"/> and <see cref="SlowmoSystem"/> would both drive the time
    /// scale on the same event and fight; here they are driven (not self-subscribed) so the handoff is clean.
    /// A boss death overrides a same-frame part break (§E.2): boss flash/hitstop/slow-mo win.
    ///
    /// <para>Shake and the softened glow self-subscribe (no time-scale conflict) and are unaffected. Particles/
    /// orbs/detonation remain the renderer's visual follow-up. Pure C#, so the ordering is EditMode-testable.</para>
    /// </summary>
    public sealed class BreakPayoffSequencer
    {
        // Flash intensities for the payoff (game-feel.md §D.4). Not balance knobs — the payoff's signature.
        private const float PartBreakFlash = 0.92f;
        private const float BossDeathFlash = 1.0f;

        private readonly IEventBus _bus;
        private readonly HitstopSystem _hitstop;
        private readonly SlowmoSystem _slowmo;
        private readonly FlashSystem _flash;
        private readonly Action<PartBroke> _onPartBroke;
        private readonly Action<BossCoreBroke> _onBossCoreBroke;

        private bool _pendingBoss;

        public BreakPayoffSequencer(IEventBus bus, HitstopSystem hitstop, SlowmoSystem slowmo, FlashSystem flash)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _hitstop = hitstop ?? throw new ArgumentNullException(nameof(hitstop));
            _slowmo = slowmo ?? throw new ArgumentNullException(nameof(slowmo));
            _flash = flash ?? throw new ArgumentNullException(nameof(flash));

            _onPartBroke = OnPartBroke;
            _onBossCoreBroke = OnBossCoreBroke;
            _bus.Subscribe(_onPartBroke);
            _bus.Subscribe(_onBossCoreBroke);
            _hitstop.HitstopEnded += OnHitstopEnded;
        }

        /// <summary>Unsubscribe on teardown.</summary>
        public void Dispose()
        {
            _bus.Unsubscribe(_onPartBroke);
            _bus.Unsubscribe(_onBossCoreBroke);
            _hitstop.HitstopEnded -= OnHitstopEnded;
        }

        private void OnPartBroke(PartBroke evt)
        {
            if (_pendingBoss && _hitstop.IsActive) return; // boss death in progress overrides a part break (§E.2)
            _pendingBoss = false;
            _flash.Trigger(PartBreakFlash);
            _hitstop.TriggerPartBreak();
        }

        private void OnBossCoreBroke(BossCoreBroke evt)
        {
            _pendingBoss = true;
            _flash.Trigger(BossDeathFlash);
            _hitstop.TriggerBossDeath();
        }

        private void OnHitstopEnded()
        {
            // Freeze just ended this frame → hand off to slow-mo (no visible gap).
            if (_pendingBoss) _slowmo.TriggerBossDeath();
            else _slowmo.TriggerPartBreak();
            _pendingBoss = false;
        }
    }
}
