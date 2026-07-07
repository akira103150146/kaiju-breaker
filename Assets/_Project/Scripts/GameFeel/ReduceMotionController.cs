using System;
using KaijuBreaker.Core;

namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Owns the reduce-motion accessibility toggle (game-feel.md §I.7): it drives the shared
    /// <see cref="ReduceMotionSettings"/> the feel systems read, and persists the choice through
    /// <see cref="ISaveService"/> flags (ADR-0004) so it survives a restart. Applies immediately — no restart
    /// needed. On construction it restores the persisted state.
    /// </summary>
    public sealed class ReduceMotionController
    {
        /// <summary>Save flag key for the persisted reduce-motion toggle.</summary>
        public const string ReduceMotionFlag = "reduce_motion";

        private readonly ReduceMotionSettings _settings;
        private readonly ISaveService _save;

        public ReduceMotionController(ReduceMotionSettings settings, ISaveService save)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _settings.SetReduceMotion(_save.GetFlag(ReduceMotionFlag)); // restore persisted state
        }

        /// <summary>Whether reduce-motion is on.</summary>
        public bool IsEnabled => _settings.ReduceMotion;

        /// <summary>Turn reduce-motion on/off: apply the multiplier profile immediately and persist the flag.</summary>
        public void SetEnabled(bool enabled)
        {
            _settings.SetReduceMotion(enabled);
            _save.SetFlag(ReduceMotionFlag, enabled);
        }

        /// <summary>Flip the current state.</summary>
        public void Toggle() => SetEnabled(!IsEnabled);
    }
}
