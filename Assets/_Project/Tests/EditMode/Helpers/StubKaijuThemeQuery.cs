using System;
using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Test double for <see cref="IKaijuThemeQuery"/>. Maps runtime kaiju ids to themes from a
    /// dictionary and throws <see cref="ArgumentException"/> for any unregistered id — mirroring the
    /// production "fail loud, fail fast" contract so a wrong core is never silently awarded
    /// (material-economy.md §H.4 edge case).
    /// </summary>
    public sealed class StubKaijuThemeQuery : IKaijuThemeQuery
    {
        private readonly Dictionary<int, KaijuTheme> _themes;

        /// <summary>Create with an initial id→theme map (may be empty; add later via <see cref="Register"/>).</summary>
        public StubKaijuThemeQuery(IDictionary<int, KaijuTheme> themes = null)
        {
            _themes = themes != null ? new Dictionary<int, KaijuTheme>(themes) : new Dictionary<int, KaijuTheme>();
        }

        /// <summary>Register or overwrite a kaiju id → theme mapping. Returns this for fluent setup.</summary>
        public StubKaijuThemeQuery Register(int kaijuId, KaijuTheme theme)
        {
            _themes[kaijuId] = theme;
            return this;
        }

        public KaijuTheme GetTheme(int kaijuId)
        {
            if (_themes.TryGetValue(kaijuId, out KaijuTheme theme))
                return theme;

            throw new ArgumentException(
                $"No theme registered for kaiju id {kaijuId}. Economy must not award a core for an " +
                "unregistered kaiju (material-economy.md §H.4).", nameof(kaijuId));
        }
    }
}
