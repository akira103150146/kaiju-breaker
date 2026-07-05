using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// In-memory <see cref="ISaveService"/> + <see cref="IWeaponTierQuery"/> test double for the Economy
    /// stories. Records every credit/spend (for exact yield assertions) AND maintains real material counts
    /// and weapon tiers over shared state, so upgrade-transaction tests (Story 004) can seed inventory,
    /// run <c>TryUpgrade</c>, and read back both the deducted counts and the new tier in the same frame.
    /// Autosave/flush are counted no-ops.
    /// </summary>
    public sealed class RecordingSaveService : ISaveService, IWeaponTierQuery
    {
        /// <summary>One recorded material movement (credit or spend), in order.</summary>
        public readonly struct Credit
        {
            public readonly MaterialId Id;
            public readonly int Amount;
            public Credit(MaterialId id, int amount) { Id = id; Amount = amount; }
            public override string ToString() => $"{Id} x{Amount}";
        }

        /// <summary>All CREDIT calls in order (spends are in <see cref="Spends"/>).</summary>
        public readonly List<Credit> Credits = new List<Credit>();

        /// <summary>All SPEND calls in order.</summary>
        public readonly List<Credit> Spends = new List<Credit>();

        private readonly Dictionary<MaterialId, int> _counts = new Dictionary<MaterialId, int>();
        private readonly Dictionary<WeaponId, int> _tiers = new Dictionary<WeaponId, int>();

        /// <summary>Loadout returned by <see cref="GetInitialLoadout"/> (null = fresh save).</summary>
        public (WeaponId Primary, WeaponId Secondary)? InitialLoadout;

        /// <summary>Number of <see cref="EnqueueAutosave"/> calls.</summary>
        public int EnqueueCalls { get; private set; }

        // ── ISaveService ──────────────────────────────────────────────────────

        public void CreditMaterials(MaterialId id, int amount)
        {
            Credits.Add(new Credit(id, amount));
            _counts[id] = GetMaterialCount(id) + amount;
        }

        public int GetMaterialCount(MaterialId id) => _counts.TryGetValue(id, out int c) ? c : 0;

        public void SpendMaterials(MaterialId id, int amount)
        {
            Spends.Add(new Credit(id, amount));
            _counts[id] = GetMaterialCount(id) - amount;
        }

        public void SetWeaponTier(WeaponId weapon, int tier) => _tiers[weapon] = tier;

        public void EnqueueAutosave() => EnqueueCalls++;

        public void FlushSync() { }

        public (WeaponId Primary, WeaponId Secondary)? GetInitialLoadout() => InitialLoadout;

        // ── IWeaponTierQuery ──────────────────────────────────────────────────

        public int GetTier(WeaponId weapon) => _tiers.TryGetValue(weapon, out int t) ? t : 0;

        // ── Seeding + assertion conveniences ──────────────────────────────────

        /// <summary>Set a material's starting count (fluent).</summary>
        public RecordingSaveService Seed(MaterialId id, int amount) { _counts[id] = amount; return this; }

        /// <summary>Set a weapon's starting tier (fluent).</summary>
        public RecordingSaveService SeedTier(WeaponId weapon, int tier) { _tiers[weapon] = tier; return this; }

        /// <summary>Total amount CREDITED for a material across all credit calls (ignores spends).</summary>
        public int TotalFor(MaterialId id)
        {
            int sum = 0;
            for (int i = 0; i < Credits.Count; i++)
                if (Credits[i].Id == id) sum += Credits[i].Amount;
            return sum;
        }

        /// <summary>Total amount SPENT for a material across all spend calls.</summary>
        public int SpentFor(MaterialId id)
        {
            int sum = 0;
            for (int i = 0; i < Spends.Count; i++)
                if (Spends[i].Id == id) sum += Spends[i].Amount;
            return sum;
        }

        /// <summary>Number of distinct credit calls recorded.</summary>
        public int CallCount => Credits.Count;

        /// <summary>Number of distinct spend calls recorded.</summary>
        public int SpendCount => Spends.Count;
    }
}
