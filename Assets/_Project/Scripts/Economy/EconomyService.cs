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

        private bool _subscribed;

        /// <summary>
        /// Wire the economy runtime. Subscribes to <see cref="PartBroke"/> immediately.
        /// </summary>
        /// <param name="config">Economy tuning (shard base/multipliers, theme→core map, double-drop flag). Required.</param>
        /// <param name="bus">The application event bus. Required.</param>
        /// <param name="saveService">Persistence sink; Economy banks materials here. Required.</param>
        /// <param name="themeQuery">Resolves the incoming runtime kaiju id → <see cref="KaijuTheme"/>. Required.</param>
        public EconomyService(EconomyConfig config, IEventBus bus, ISaveService saveService, IKaijuThemeQuery themeQuery)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _themeQuery = themeQuery ?? throw new ArgumentNullException(nameof(themeQuery));

            _bus.Subscribe<PartBroke>(OnPartBroke);
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

        /// <summary>Unsubscribe from the bus. Idempotent.</summary>
        public void Dispose()
        {
            if (!_subscribed) return;
            _bus.Unsubscribe<PartBroke>(OnPartBroke);
            _subscribed = false;
        }
    }
}
