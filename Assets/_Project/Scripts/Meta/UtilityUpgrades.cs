using KaijuBreaker.Core;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// The out-of-run (meta) upgrade layer: permanent UTILITY boosts bought with materials between runs —
    /// deliberately NOT killing power (that is the in-run Raiden firepower). Two shard-funded tracks (faster
    /// fire / higher drop rate) plus five theme-core-funded tracks (the MECHA/utility axis for the new cores,
    /// session-9 decision). Levels persist through <see cref="ISaveService"/> as flags (<c>util_*_N</c>) so no
    /// save-schema change is needed for the placeholder. Pure C# over the save interface — EditMode-testable.
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

        // ── Core-funded utility tracks (session-9 sink decision) ─────────────────────────
        // The five new theme cores fund a MECHA/utility axis, separate from in-run firepower. Each track spends
        // its OWN theme core (Swarm/Crystal/Abyss/Ember/Void), not ShardCommon. Levels persist as util_<name>_N
        // flags, same as the shard tracks. Every effect is non-killpower QoL / survivability / convenience.
        public const int MaxCoreLevel = 5;

        /// <summary>Secondary-ammo track level, 0..MaxCoreLevel (Swarm core).</summary>
        public int AmmoLevel => LevelOf("util_ammo");
        /// <summary>Material-magnet track level (Crystal core).</summary>
        public int MagnetLevel => LevelOf("util_magnet");
        /// <summary>Post-hit i-frame track level (Abyss core).</summary>
        public int IFrameLevel => LevelOf("util_iframe");
        /// <summary>Move-speed track level (Ember core).</summary>
        public int SpeedLevel => LevelOf("util_speed");
        /// <summary>Run head-start firepower track level (Void core).</summary>
        public int HeadStartLevel => LevelOf("util_headstart");

        /// <summary>Extra secondary-weapon rounds per magazine (+1 per level) — sustain, not per-hit power.</summary>
        public int SecondaryAmmoBonus => AmmoLevel;
        /// <summary>Secondary-fire cooldown multiplier (Swarm core; lower = faster missiles, does NOT change missile count). 1.0 at level 0.</summary>
        public float SecondaryCooldownMult => 1f / (1f + AmmoLevel * 0.1f);
        /// <summary>Material auto-collect radius multiplier (1.0 at level 0).</summary>
        public float MagnetRadiusMult => 1f + MagnetLevel * 0.25f;
        /// <summary>Post-hit invulnerability duration multiplier (1.0 at level 0).</summary>
        public float IFrameMult => 1f + IFrameLevel * 0.15f;
        /// <summary>Move-speed multiplier (1.0 at level 0).</summary>
        public float MoveSpeedMult => 1f + SpeedLevel * 0.06f;
        /// <summary>Firepower level each run starts at (0 at level 0). The in-run ceiling is unchanged.</summary>
        public int StartPowerLevel => HeadStartLevel;

        /// <summary>Core cost to buy the level after <paramref name="currentLevel"/> (cores are rarer than shards).</summary>
        public int CoreCostFor(int currentLevel) => (currentLevel + 1) * 4;

        /// <summary>Current spendable count of a theme core.</summary>
        public int CoreBalance(MaterialId core) => _save.GetMaterialCount(core);

        /// <summary>Buy the next secondary-ammo level (Swarm core). False if maxed or too few cores.</summary>
        public bool BuyAmmo() => BuyCore("util_ammo", AmmoLevel, MaterialId.CoreSwarm);
        /// <summary>Buy the next material-magnet level (Crystal core).</summary>
        public bool BuyMagnet() => BuyCore("util_magnet", MagnetLevel, MaterialId.CoreCrystal);
        /// <summary>Buy the next i-frame level (Abyss core).</summary>
        public bool BuyIFrame() => BuyCore("util_iframe", IFrameLevel, MaterialId.CoreAbyss);
        /// <summary>Buy the next move-speed level (Ember core).</summary>
        public bool BuySpeed() => BuyCore("util_speed", SpeedLevel, MaterialId.CoreEmber);
        /// <summary>Buy the next run head-start level (Void core).</summary>
        public bool BuyHeadStart() => BuyCore("util_headstart", HeadStartLevel, MaterialId.CoreVoid);

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

        private bool BuyCore(string key, int level, MaterialId currency)
        {
            if (level >= MaxCoreLevel) return false;
            int cost = CoreCostFor(level);
            if (_save.GetMaterialCount(currency) < cost) return false;
            _save.SpendMaterials(currency, cost);
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
