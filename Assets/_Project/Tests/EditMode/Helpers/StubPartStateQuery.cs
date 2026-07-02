using System.Collections.Generic;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Configurable <see cref="IPartStateQuery"/> test double for Weapons tests. Each part is
    /// registered via <see cref="Configure"/> with fixed heat/armor/heat-values/position/alive.
    /// Replicates the real <c>PartStateSystem</c> contract: the value getters throw
    /// <see cref="KeyNotFoundException"/> for an unregistered id, <see cref="IsPartAlive"/> returns
    /// false, and <see cref="GetHottestAlivePartId"/> returns −1 when no registered part is alive
    /// (ties broken to the lowest id).
    /// </summary>
    public sealed class StubPartStateQuery : IPartStateQuery
    {
        /// <summary>Mutable per-part record. Fields are public so tests can tweak in place.</summary>
        public sealed class Record
        {
            public HeatState Heat = HeatState.Intact;
            public ArmorState Armor = ArmorState.Intact;
            public float CurrentHeat;
            public float MaxHeat = 100f;
            public Vector2 WorldPosition;
            public bool Alive = true;
        }

        private readonly Dictionary<int, Record> _parts = new Dictionary<int, Record>();

        /// <summary>Register (or update) a part and return its record for further tweaking.</summary>
        public Record Configure(int partId, HeatState heat = HeatState.Intact,
            ArmorState armor = ArmorState.Intact, float currentHeat = 0f, float maxHeat = 100f,
            bool alive = true, Vector2 worldPosition = default)
        {
            var r = new Record
            {
                Heat = heat,
                Armor = armor,
                CurrentHeat = currentHeat,
                MaxHeat = maxHeat,
                Alive = alive,
                WorldPosition = worldPosition
            };
            _parts[partId] = r;
            return r;
        }

        /// <summary>Direct record access (throws if unregistered) — for in-place mutation between fire calls.</summary>
        public Record this[int partId] => Require(partId);

        private Record Require(int partId)
        {
            if (_parts.TryGetValue(partId, out var r)) return r;
            throw new KeyNotFoundException($"[StubPartStateQuery] No part with id {partId} configured.");
        }

        public HeatState GetHeatState(int partId) => Require(partId).Heat;
        public ArmorState GetArmorState(int partId) => Require(partId).Armor;
        public float GetCurrentHeat(int partId) => Require(partId).CurrentHeat;
        public float GetMaxHeat(int partId) => Require(partId).MaxHeat;
        public Vector2 GetWorldPosition(int partId) => Require(partId).WorldPosition;

        public bool IsPartAlive(int partId) => _parts.TryGetValue(partId, out var r) && r.Alive;

        public int GetHottestAlivePartId()
        {
            int bestId = -1;
            float bestHeat = float.NegativeInfinity;
            foreach (var kvp in _parts)
            {
                var r = kvp.Value;
                if (!r.Alive) continue;
                if (r.CurrentHeat > bestHeat || (r.CurrentHeat == bestHeat && kvp.Key < bestId))
                {
                    bestHeat = r.CurrentHeat;
                    bestId = kvp.Key;
                }
            }
            return bestId;
        }
    }
}
