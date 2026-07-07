using System;
using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.App
{
    /// <summary>
    /// Runtime <see cref="IKaijuThemeQuery"/> — maps the integer kaiju id the events carry (assigned when a
    /// kaiju's parts are initialised) to its <see cref="KaijuTheme"/>, so Economy can award the right theme
    /// core (material-economy.md §C.1). The run start registers the boss kaiju's theme. Unregistered ids
    /// throw (fail-loud, §H.2/§H.4) — a wrong core must never be silently awarded.
    /// </summary>
    public sealed class KaijuThemeRegistry : IKaijuThemeQuery
    {
        private readonly Dictionary<int, KaijuTheme> _themes = new Dictionary<int, KaijuTheme>();

        /// <summary>Register (or overwrite) the theme for a runtime kaiju id (called at run start).</summary>
        public void Register(int kaijuId, KaijuTheme theme) => _themes[kaijuId] = theme;

        /// <summary>Clear all registrations (e.g. between runs).</summary>
        public void Clear() => _themes.Clear();

        /// <inheritdoc/>
        public KaijuTheme GetTheme(int kaijuId)
        {
            if (_themes.TryGetValue(kaijuId, out var theme)) return theme;
            throw new ArgumentException($"No theme registered for kaiju id {kaijuId}.", nameof(kaijuId));
        }
    }
}
