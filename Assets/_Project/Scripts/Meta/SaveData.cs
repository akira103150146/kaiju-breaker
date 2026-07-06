using System.Collections.Generic;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// In-memory model of the single-slot JSON save (meta-progression-system.md §C.3). Plain data —
    /// no I/O, no serialization logic (that is <see cref="CanonicalJsonSerializer"/>). Maps are held as
    /// dictionaries and canonicalised (key-sorted) only at serialize time, so insertion order never
    /// affects the on-disk bytes — a hard requirement for the CRC32 integrity hash (§D.2).
    ///
    /// <para>Material counts and stat totals use <c>long</c> (not the schema's nominal int) to guarantee no
    /// overflow under sustained accumulation (§H.2 連續破壞累加無溢位); they serialize as plain integers.</para>
    /// </summary>
    public sealed class SaveData
    {
        /// <summary>The current save schema version this build reads/writes (§C.4). Bump when the schema changes.</summary>
        public const int CurrentVersion = 1;

        /// <summary>Save schema version (§C.4 CURRENT_VERSION = 1). Range [1, ∞).</summary>
        public int Version = CurrentVersion;

        /// <summary>CRC32 hex of the canonical JSON of everything except this field (§D.2). 8 uppercase hex chars.</summary>
        public string IntegrityHash = string.Empty;

        /// <summary>Per-weapon permanent tier + ownership, keyed by weapon id ("L1".."M4").</summary>
        public Dictionary<string, WeaponSaveData> Weapons = new Dictionary<string, WeaponSaveData>();

        /// <summary>Material inventory keyed by material id ("shard_common", "core_carapace", …). Values [0, ∞).</summary>
        public Dictionary<string, long> Materials = new Dictionary<string, long>();

        /// <summary>Per-kaiju lifetime records keyed by kaiju id ("CARAPEX", …).</summary>
        public Dictionary<string, KaijuRecordData> KaijuRecords = new Dictionary<string, KaijuRecordData>();

        /// <summary>Cross-run meta prefs (last difficulty / loadout / first-launch flag).</summary>
        public MetaBlock Meta = new MetaBlock();

        /// <summary>Accessibility + audio settings.</summary>
        public SettingsData Settings = new SettingsData();

        /// <summary>Lifetime statistics (non-gameplay; achievements-facing).</summary>
        public StatsData Stats = new StatsData();

        /// <summary>
        /// Full deep clone — every nested dictionary, list, and object is copied, so the returned snapshot
        /// is completely isolated from later mutation of the original. Required before handing a snapshot to
        /// the background save worker (meta-progression-system.md §C.5.3; Story 002): the main thread keeps
        /// crediting materials while a write is in flight.
        /// </summary>
        public SaveData DeepCopy()
        {
            var clone = new SaveData
            {
                Version = Version,
                IntegrityHash = IntegrityHash,
                Meta = new MetaBlock
                {
                    LastSelectedDifficulty = Meta.LastSelectedDifficulty,
                    FirstLaunchComplete = Meta.FirstLaunchComplete,
                    LastLoadout = new LoadoutData(Meta.LastLoadout.Primary, Meta.LastLoadout.Secondary),
                },
                Settings = new SettingsData
                {
                    ReduceMotion = Settings.ReduceMotion,
                    ColorblindMode = Settings.ColorblindMode,
                    TextScale = Settings.TextScale,
                    BgmVolume = Settings.BgmVolume,
                    SfxVolume = Settings.SfxVolume,
                },
                Stats = new StatsData
                {
                    TotalRunsStarted = Stats.TotalRunsStarted,
                    TotalRunsCompleted = Stats.TotalRunsCompleted,
                    TotalPartsBroken = Stats.TotalPartsBroken,
                    TotalFullClears = Stats.TotalFullClears,
                    TotalPlayTimeSeconds = Stats.TotalPlayTimeSeconds,
                },
            };

            foreach (var kv in Weapons)
                clone.Weapons[kv.Key] = new WeaponSaveData(kv.Value.Tier, kv.Value.Owned);

            foreach (var kv in Materials)
                clone.Materials[kv.Key] = kv.Value;

            foreach (var kv in KaijuRecords)
            {
                var src = kv.Value;
                var rec = new KaijuRecordData
                {
                    FullClearCount = src.FullClearCount,
                    PartsEverBroken = new List<string>(src.PartsEverBroken),
                    HuntCountPerDifficulty = new Dictionary<string, int>(src.HuntCountPerDifficulty),
                    BestTimePerDifficulty = new Dictionary<string, float?>(src.BestTimePerDifficulty),
                };
                clone.KaijuRecords[kv.Key] = rec;
            }

            return clone;
        }
    }

    /// <summary>One weapon's persisted state (meta-progression-system.md §C.3 weapons[id]).</summary>
    public sealed class WeaponSaveData
    {
        /// <summary>Permanent upgrade tier {0,1,2,3}; one-way (only increases).</summary>
        public int Tier;

        /// <summary>Whether the weapon is selectable in the loadout; irreversibly true after first pickup.</summary>
        public bool Owned;

        public WeaponSaveData() { }
        public WeaponSaveData(int tier, bool owned) { Tier = tier; Owned = owned; }
    }

    /// <summary>One kaiju's lifetime record (meta-progression-system.md §C.3 kaiju_records[id]).</summary>
    public sealed class KaijuRecordData
    {
        /// <summary>Set of part ids ever broken across all runs (set semantics; duplicates not re-counted).</summary>
        public List<string> PartsEverBroken = new List<string>();

        /// <summary>Number of full-clear (all parts broken) victories on this kaiju. Range [0, ∞).</summary>
        public int FullClearCount;

        /// <summary>Successful hunt count per difficulty ("D1".."D4"). Values [0, ∞).</summary>
        public Dictionary<string, int> HuntCountPerDifficulty = new Dictionary<string, int>();

        /// <summary>Best victory time (seconds) per difficulty ("D1".."D4"); null = never completed.</summary>
        public Dictionary<string, float?> BestTimePerDifficulty = new Dictionary<string, float?>();
    }

    /// <summary>Cross-run meta preferences (meta-progression-system.md §C.3 meta{}).</summary>
    public sealed class MetaBlock
    {
        /// <summary>Last selected difficulty ("D1".."D4"); prefilled on the next loadout screen.</summary>
        public string LastSelectedDifficulty = "D1";

        /// <summary>Last confirmed loadout; prefilled next run (with owned-weapon fallback at load — Story 005).</summary>
        public LoadoutData LastLoadout = new LoadoutData();

        /// <summary>Whether first-launch onboarding has completed.</summary>
        public bool FirstLaunchComplete;
    }

    /// <summary>A primary+secondary weapon pair (meta-progression-system.md §C.3 meta.last_loadout).</summary>
    public sealed class LoadoutData
    {
        /// <summary>Primary (laser-family) weapon id.</summary>
        public string Primary = "L1";

        /// <summary>Secondary (missile-family) weapon id.</summary>
        public string Secondary = "M1";

        public LoadoutData() { }
        public LoadoutData(string primary, string secondary) { Primary = primary; Secondary = secondary; }
    }

    /// <summary>Accessibility + audio settings (meta-progression-system.md §C.3 settings{}).</summary>
    public sealed class SettingsData
    {
        /// <summary>Reduce-motion accessibility flag (hud-ui-system.md I.3).</summary>
        public bool ReduceMotion;

        /// <summary>Colorblind mode: "default", "blue_yellow", or "shape_priority" (hud-ui-system.md I.2).</summary>
        public string ColorblindMode = "default";

        /// <summary>UI text scale {1.0, 1.25, 1.5} (hud-ui-system.md I.1).</summary>
        public float TextScale = 1.0f;

        /// <summary>Background-music volume [0.0, 1.0].</summary>
        public float BgmVolume = 1.0f;

        /// <summary>Sound-effects volume [0.0, 1.0].</summary>
        public float SfxVolume = 1.0f;
    }

    /// <summary>Lifetime statistics (meta-progression-system.md §C.3 stats{}). Non-gameplay.</summary>
    public sealed class StatsData
    {
        public long TotalRunsStarted;
        public long TotalRunsCompleted;
        public long TotalPartsBroken;
        public long TotalFullClears;
        public long TotalPlayTimeSeconds;
    }
}
