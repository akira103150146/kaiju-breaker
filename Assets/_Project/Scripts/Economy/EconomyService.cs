using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Economy
{
    /// <summary>
    /// Material-economy runtime (material-economy.md §A, §D.1). Subscribes to <see cref="PartBroke"/>
    /// and, for each break, INDEPENDENTLY computes the Common-Shard and kaiju-theme core yields from
    /// <see cref="PartBroke.Quality"/> + <see cref="PartBroke.KaijuId"/>, then banks them through the
    /// injected <see cref="ISaveService"/>. The <see cref="PartBroke"/> payload carries NO pre-computed
    /// yields (ADR-0002 §3; control-manifest §3 Economy) — Economy owns the yield formula, KaijuParts owns
    /// break-quality. All multipliers and the theme→core map come from <see cref="EconomyConfig"/> (ADR-0003).
    ///
    /// <para>
    /// Story 001 scope: per-break yield only. Full-clear essence (Story 002), inventory persistence
    /// (Story 003), upgrade transactions (Story 004), and the TTB anti-dominant guard (Story 005) live
    /// in sibling stories and are NOT handled here.
    /// </para>
    ///
    /// <para>Constructor-injected; no singletons, no direct KaijuParts/Meta references (ADR-0005).
    /// Call <see cref="Dispose"/> at teardown to unsubscribe.</para>
    /// </summary>
    public sealed class EconomyService : IDisposable
    {
        private readonly EconomyConfig _config;
        private readonly IEventBus _bus;
        private readonly ISaveService _saveService;
        private readonly IKaijuThemeQuery _themeQuery;
        private readonly IWeaponTierQuery _tierQuery;

        private bool _subscribed;

        /// <summary>
        /// Wire the economy runtime. Subscribes to <see cref="PartBroke"/> immediately.
        /// </summary>
        /// <param name="config">Economy tuning (shard base/multipliers, theme→core map, double-drop flag). Required.</param>
        /// <param name="bus">The application event bus. Required.</param>
        /// <param name="saveService">Persistence sink; Economy banks/spends materials + writes tiers here. Required.</param>
        /// <param name="themeQuery">Resolves the incoming runtime kaiju id → <see cref="KaijuTheme"/>. Required.</param>
        /// <param name="tierQuery">Reads a weapon's current tier for the upgrade affordability/from-tier check. Required.</param>
        public EconomyService(EconomyConfig config, IEventBus bus, ISaveService saveService,
                              IKaijuThemeQuery themeQuery, IWeaponTierQuery tierQuery)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _themeQuery = themeQuery ?? throw new ArgumentNullException(nameof(themeQuery));
            _tierQuery = tierQuery ?? throw new ArgumentNullException(nameof(tierQuery));

            _bus.Subscribe<PartBroke>(OnPartBroke);
            _bus.Subscribe<HuntEnded>(OnHuntEnded);
            _subscribed = true;
        }

        /// <summary>
        /// Compute and bank the material yield for one part break (material-economy.md §D.1).
        /// Shard yield scales by break quality; core type is fixed by the kaiju's theme (NOT part type);
        /// core count is 1, or 2 on a Perfect break when <see cref="EconomyConfig.CorePerfectDoubleDrop"/>.
        /// No zero-drop path exists — every break yields ≥1 shard and exactly 1 (or 2) core.
        /// </summary>
        private void OnPartBroke(PartBroke evt)
        {
            // 1. Common Shard — quality-scaled, floored (§D.1). part_type does NOT affect yield.
            int shardYield = Mathf.FloorToInt(_config.ShardYieldBase * _config.QualityShardMult(evt.Quality));

            // 2. Kaiju-theme core — theme resolved from kaiju id (fail-loud on an unregistered kaiju),
            //    then mapped theme→core via the config. part_type is irrelevant to which core drops.
            KaijuTheme theme = _themeQuery.GetTheme(evt.KaijuId);
            MaterialId coreType = _config.GetCoreForTheme(theme);

            // 3. Core count — 1 by default; 2 only on a Perfect break when double-drop is enabled (§D.1).
            int coreYield = (evt.Quality == BreakQuality.SoftenedStaggered && _config.CorePerfectDoubleDrop) ? 2 : 1;

            // 4. Bank both in the same frame (same-frame reward requirement, ADR-0002).
            _saveService.CreditMaterials(MaterialId.ShardCommon, shardYield);
            _saveService.CreditMaterials(coreType, coreYield);
        }

        /// <summary>
        /// Award the full-clear settlement bonus (material-economy.md §C.2, §D.1). Essence and the
        /// completeness shard bonus are granted ONLY when every breakable part was destroyed; a
        /// non-full-clear hunt yields nothing here (the per-break shards/cores from <see cref="OnPartBroke"/>
        /// are already banked and unaffected). Essence has no other source — it never drops per-break.
        /// </summary>
        private void OnHuntEnded(HuntEnded evt)
        {
            if (!evt.IsAllPartsBroken) return;

            _saveService.CreditMaterials(MaterialId.EssenceKaiju, _config.EssencePerFullClear);
            _saveService.CreditMaterials(MaterialId.ShardCommon, _config.ShardCompletenessBonus);
        }

        /// <summary>
        /// Attempt a permanent weapon upgrade (material-economy.md §C.3, §C.4, §D.2). ATOMIC and one-way:
        /// succeeds only when the weapon is exactly at the transition's from-tier AND the player can afford
        /// every cost (shards + weapon-theme core + essence); on success it deducts all three and advances
        /// the tier by one (immediately visible via <see cref="IWeaponTierQuery.GetTier"/>). On any failed
        /// check NOTHING is deducted and the tier is unchanged. All costs come from <see cref="EconomyConfig"/>.
        /// </summary>
        /// <returns>True if the upgrade was applied; false if the from-tier mismatched or a cost was unmet.</returns>
        public bool TryUpgrade(WeaponId weapon, TierTransition transition)
        {
            // Sequential/one-way gate — must be exactly at the from-tier (no skipping, no re-upgrade).
            if (_tierQuery.GetTier(weapon) != transition.FromTier())
                return false;

            int shardCost = _config.UpgradeShardCost(transition);
            int coreCost = _config.UpgradeCoreCost(transition);
            int essenceCost = _config.UpgradeEssenceCost(transition);
            MaterialId coreType = _config.GetCoreForWeapon(weapon);

            // Affordability — check all BEFORE spending anything (atomic all-or-nothing).
            if (_saveService.GetMaterialCount(MaterialId.ShardCommon) < shardCost) return false;
            if (_saveService.GetMaterialCount(coreType) < coreCost) return false;
            if (_saveService.GetMaterialCount(MaterialId.EssenceKaiju) < essenceCost) return false;

            _saveService.SpendMaterials(MaterialId.ShardCommon, shardCost);
            _saveService.SpendMaterials(coreType, coreCost);
            _saveService.SpendMaterials(MaterialId.EssenceKaiju, essenceCost);
            _saveService.SetWeaponTier(weapon, transition.ToTier());
            return true;
        }

        /// <summary>Unsubscribe from the bus. Idempotent.</summary>
        public void Dispose()
        {
            if (!_subscribed) return;
            _bus.Unsubscribe<PartBroke>(OnPartBroke);
            _bus.Unsubscribe<HuntEnded>(OnHuntEnded);
            _subscribed = false;
        }
    }
}
