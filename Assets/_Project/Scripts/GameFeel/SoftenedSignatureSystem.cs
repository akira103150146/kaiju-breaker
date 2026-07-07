using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Tracks which parts are currently SOFTENED so the presentation layer can draw the softened-weak-point
    /// signature — the pulsing glow ring that reads as "hit this now" (game-feel.md §I.1, the P0 Alpha
    /// readability blocker). A part is registered on <see cref="PartSoftened"/> and cleared on
    /// <see cref="PartSoftenedExit"/> or <see cref="PartBroke"/>. Every softened part gets its glow, but the
    /// accompanying SFX is rate-limited to <see cref="GameFeelConfig.SoftenedSfxMaxPerFrame"/> per frame so a
    /// mass-soften does not machine-gun the audio.
    ///
    /// <para>Pure C# — the renderer/audio adapter reads <see cref="IsSoftened"/> and consumes the per-frame
    /// SFX budget via <see cref="TryConsumeSfxBudget"/>; call <see cref="ResetFrame"/> once per frame. Glow
    /// geometry/pulse (2 Hz, radius %) is the renderer's job; this system owns the state + SFX budget only.</para>
    /// </summary>
    public sealed class SoftenedSignatureSystem
    {
        private readonly IEventBus _bus;
        private readonly GameFeelConfig _config;
        private readonly HashSet<int> _softened = new HashSet<int>();
        private readonly Action<PartSoftened> _onSoftened;
        private readonly Action<PartSoftenedExit> _onSoftenedExit;
        private readonly Action<PartBroke> _onPartBroke;

        private int _sfxThisFrame;

        /// <summary>Number of parts currently softened (each gets a glow ring).</summary>
        public int SoftenedCount => _softened.Count;

        /// <summary>SFX calls already spent this frame (≤ <see cref="GameFeelConfig.SoftenedSfxMaxPerFrame"/>).</summary>
        public int SfxThisFrame => _sfxThisFrame;

        public SoftenedSignatureSystem(IEventBus bus, GameFeelConfig config)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _onSoftened = OnSoftened;
            _onSoftenedExit = e => _softened.Remove(e.PartId);
            _onPartBroke = e => _softened.Remove(e.PartId);
            _bus.Subscribe(_onSoftened);
            _bus.Subscribe(_onSoftenedExit);
            _bus.Subscribe(_onPartBroke);
        }

        /// <summary>Unsubscribe on teardown.</summary>
        public void Dispose()
        {
            _bus.Unsubscribe(_onSoftened);
            _bus.Unsubscribe(_onSoftenedExit);
            _bus.Unsubscribe(_onPartBroke);
        }

        /// <summary>Whether a part is currently softened (renderer draws its glow).</summary>
        public bool IsSoftened(int partId) => _softened.Contains(partId);

        /// <summary>Reset the per-frame SFX budget. Call once per frame (unscaled update).</summary>
        public void ResetFrame() => _sfxThisFrame = 0;

        /// <summary>
        /// Try to spend one SFX from this frame's budget; true if the soften SFX may play. The glow always
        /// shows regardless — only the audio is capped (game-feel.md §I.1).
        /// </summary>
        public bool TryConsumeSfxBudget()
        {
            if (_sfxThisFrame >= _config.SoftenedSfxMaxPerFrame) return false;
            _sfxThisFrame++;
            return true;
        }

        private void OnSoftened(PartSoftened evt)
        {
            _softened.Add(evt.PartId);   // glow always registers
            TryConsumeSfxBudget();       // SFX only if budget remains
        }
    }
}
