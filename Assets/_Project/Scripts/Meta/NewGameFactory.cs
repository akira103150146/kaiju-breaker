using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Builds a fresh-save <see cref="SaveData"/> from <see cref="SaveConfig"/> defaults
    /// (meta-progression-system.md §C.7, §E.3): every tracked weapon present (owned only if in the starting
    /// set), all materials at 0, one record per tracked kaiju, version = current, first-launch not complete.
    /// Story 003 uses it for the first-launch <c>NewGame</c> result; Story 005 owns the surrounding
    /// init/flow. Pure data — no I/O.
    /// </summary>
    public static class NewGameFactory
    {
        public static SaveData Create(SaveConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var data = new SaveData { Version = SaveData.CurrentVersion, IntegrityHash = string.Empty };

            var startingOwned = new System.Collections.Generic.HashSet<WeaponId>(config.StartingOwnedWeapons ?? Array.Empty<WeaponId>());
            foreach (var w in config.ActiveWeaponIds ?? Array.Empty<WeaponId>())
                data.Weapons[w.ToString()] = new WeaponSaveData(tier: 0, owned: startingOwned.Contains(w));

            foreach (var key in MaterialKeys.AllKeys)
                data.Materials[key] = 0;

            foreach (var kaijuId in config.ActiveKaijuIds ?? Array.Empty<string>())
                data.KaijuRecords[kaijuId] = NewKaijuRecord();

            data.Meta.FirstLaunchComplete = false;
            data.Meta.LastSelectedDifficulty = DifficultyTier.D1.ToString();
            data.Meta.LastLoadout = DefaultLoadout(config);

            return data;
        }

        private static KaijuRecordData NewKaijuRecord()
        {
            var rec = new KaijuRecordData { FullClearCount = 0 };
            foreach (DifficultyTier t in Enum.GetValues(typeof(DifficultyTier)))
            {
                rec.HuntCountPerDifficulty[t.ToString()] = 0;
                rec.BestTimePerDifficulty[t.ToString()] = null;
            }
            return rec;
        }

        /// <summary>Default loadout = the first owned primary (laser L*) + first owned secondary (missile M*).</summary>
        private static LoadoutData DefaultLoadout(SaveConfig config)
        {
            string primary = "L1", secondary = "M1";
            foreach (var w in config.StartingOwnedWeapons ?? Array.Empty<WeaponId>())
            {
                string s = w.ToString();
                if (s.StartsWith("L", StringComparison.Ordinal)) { primary = s; break; }
            }
            foreach (var w in config.StartingOwnedWeapons ?? Array.Empty<WeaponId>())
            {
                string s = w.ToString();
                if (s.StartsWith("M", StringComparison.Ordinal)) { secondary = s; break; }
            }
            return new LoadoutData(primary, secondary);
        }
    }
}
