using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Configurable <see cref="IWeaponTierQuery"/> test double. Every weapon defaults to tier 0;
    /// call <see cref="SetTier"/> to override (e.g. tier 3 to exercise a unique-mechanic path).
    /// </summary>
    public sealed class StubWeaponTierQuery : IWeaponTierQuery
    {
        private readonly Dictionary<WeaponId, int> _tiers = new Dictionary<WeaponId, int>();

        /// <summary>Fluent tier override; returns this for chaining.</summary>
        public StubWeaponTierQuery SetTier(WeaponId weapon, int tier)
        {
            _tiers[weapon] = tier;
            return this;
        }

        public int GetTier(WeaponId weapon) => _tiers.TryGetValue(weapon, out int t) ? t : 0;
    }
}
