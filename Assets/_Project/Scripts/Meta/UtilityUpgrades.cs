using KaijuBreaker.Core;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// The out-of-run (meta) upgrade layer: permanent UTILITY boosts bought with materials between runs —
    /// deliberately NOT killing power (that is the in-run Raiden firepower). Two tracks: faster fire
    /// (open-fire interval) and a higher power-up drop rate. Levels persist through <see cref="ISaveService"/>
    /// as flags (<c>util_fire_1..N</c> / <c>util_drop_1..N</c>) so no save-schema change is needed for the
    /// placeholder; ShardCommon is the currency. Pure C# over the save interface — EditMode-testable.
    /// </summary>
    public sealed class UtilityUpgrades
    {
        public const int MaxLevel = 5;
        private const MaterialId Currency = MaterialId.ShardCommon;

        private readonly ISaveService _save;

        public UtilityUpgrades(ISaveService save)
        {
            _save = save ?? throw new System.ArgumentNullException(nameof(save));
        }

        /// <summary>Faster-fire track level (0..MaxLevel).</summary>
        public int FireRateLevel => LevelOf("util_fire");
        /// <summary>Drop-rate track level (0..MaxLevel).</summary>
        public int DropRateLevel => LevelOf("util_drop");

        /// <summary>Multiplier applied to the primary fire interval (lower = faster). 1.0 at level 0.</summary>
        public float FireIntervalMult => 1f / (1f + FireRateLevel * 0.08f);
        /// <summary>Multiplier applied to power-up drop chances. 1.0 at level 0.</summary>
        public float DropRateMult => 1f + DropRateLevel * 0.22f;

        /// <summary>Current spendable ShardCommon.</summary>
        public int Shards => _save.GetMaterialCount(Currency);

        /// <summary>Shard cost to buy the level after <paramref name="currentLevel"/>.</summary>
        public int CostFor(int currentLevel) => (currentLevel + 1) * 8;

        /// <summary>Buy the next faster-fire level. Returns false if maxed or too few shards.</summary>
        public bool BuyFireRate() => Buy("util_fire", FireRateLevel);
        /// <summary>Buy the next drop-rate level. Returns false if maxed or too few shards.</summary>
        public bool BuyDropRate() => Buy("util_drop", DropRateLevel);

        private bool Buy(string key, int level)
        {
            if (level >= MaxLevel) return false;
            int cost = CostFor(level);
            if (_save.GetMaterialCount(Currency) < cost) return false;
            _save.SpendMaterials(Currency, cost);
            _save.SetFlag(key + "_" + (level + 1), true);
            _save.EnqueueAutosave();
            return true;
        }

        private int LevelOf(string key)
        {
            int n = 0;
            for (int i = 1; i <= MaxLevel; i++)
            {
                if (_save.GetFlag(key + "_" + i)) n = i;
                else break;
            }
            return n;
        }
    }
}
