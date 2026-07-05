using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Test double for <see cref="ISaveService"/> that records every <see cref="CreditMaterials"/>
    /// call so economy tests can assert exact material yields (id + amount + call order) without a
    /// real Meta/save backend. Autosave/flush/loadout are no-ops.
    /// </summary>
    public sealed class RecordingSaveService : ISaveService
    {
        /// <summary>One recorded <see cref="CreditMaterials"/> call, in order.</summary>
        public readonly struct Credit
        {
            public readonly MaterialId Id;
            public readonly int Amount;
            public Credit(MaterialId id, int amount) { Id = id; Amount = amount; }
            public override string ToString() => $"{Id} x{Amount}";
        }

        /// <summary>All credits in the order they were requested.</summary>
        public readonly List<Credit> Credits = new List<Credit>();

        /// <summary>Loadout returned by <see cref="GetInitialLoadout"/> (null = fresh save).</summary>
        public (WeaponId Primary, WeaponId Secondary)? InitialLoadout;

        /// <summary>Number of <see cref="EnqueueAutosave"/> calls.</summary>
        public int EnqueueCalls { get; private set; }

        public void CreditMaterials(MaterialId id, int amount) => Credits.Add(new Credit(id, amount));

        public void EnqueueAutosave() => EnqueueCalls++;

        public void FlushSync() { }

        public (WeaponId Primary, WeaponId Secondary)? GetInitialLoadout() => InitialLoadout;

        // ── Assertion conveniences ────────────────────────────────────────────

        /// <summary>Total amount credited for a given material across all calls.</summary>
        public int TotalFor(MaterialId id)
        {
            int sum = 0;
            for (int i = 0; i < Credits.Count; i++)
                if (Credits[i].Id == id) sum += Credits[i].Amount;
            return sum;
        }

        /// <summary>Number of distinct <see cref="CreditMaterials"/> calls recorded.</summary>
        public int CallCount => Credits.Count;

        /// <summary>Forget all recorded credits (reuse the double across scenarios).</summary>
        public void Reset() { Credits.Clear(); EnqueueCalls = 0; }
    }
}
