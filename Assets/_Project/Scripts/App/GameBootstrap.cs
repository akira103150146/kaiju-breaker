using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.App
{
    /// <summary>
    /// Composition root (ADR-0005 §3). Lives on a persistent GameObject in the
    /// Bootstrap scene and runs once at startup before any other scene loads.
    ///
    /// Responsibilities:
    ///   1. Construct the single <see cref="IEventBus"/>.
    ///   2. Construct each system and inject its dependencies (event bus + query interfaces).
    ///   3. Initialise Addressables and transition to the MetaHub scene.
    ///
    /// This object is PURE WIRING — it holds no game state. Per ADR-0005 / coding-standards:
    /// no static singletons holding state, no FindObjectOfType, DI over singletons.
    /// Systems receive their dependencies here; they never reference each other directly.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        // Inspector-assigned ScriptableObject config references (populated in the Bootstrap
        // scene — no runtime Find() calls). Added as each Content SO type lands (content-config epic).
        // [SerializeField] private GameFeelConfig _gameFeelConfig;
        // [SerializeField] private DifficultyConfig _difficultyConfig;
        // [SerializeField] private SaveConfig _saveConfig;
        // ... etc.

        /// <summary>The application-wide event bus. Injected into systems; not a global access point.</summary>
        private IEventBus _bus;

        private void Awake()
        {
            // Survive scene transitions — the composition root persists for the whole session.
            DontDestroyOnLoad(gameObject);

            // 1. The one event bus (ADR-0002). Everything else is injected with it.
            _bus = new TypedEventBus();

            WireSystems();

            Debug.Log("[GameBootstrap] Core composition root initialised (event bus ready).");

            // TODO (content-config + system epics): once systems exist, load the core_boot
            // Addressables group and transition to the MetaHub scene.
        }

        /// <summary>
        /// Construct systems and inject dependencies. Each line is added as its epic lands;
        /// order follows the dependency graph (Difficulty/Save → Parts → Weapons/Economy → …).
        /// </summary>
        private void WireSystems()
        {
            // Foundation infrastructure is ready (_bus). System wiring is filled in per epic, e.g.:
            //   var difficulty = new DifficultySystem(_difficultyConfig);                 // IDifficultyProvider
            //   var meta       = new MetaSystem(_saveConfig, _bus);                        // ISaveService, IWeaponTierQuery
            //   var kaijuParts = new KaijuPartsSystem(_partSystemConfig, _bus);            // IPartStateQuery
            //   var weapons    = new WeaponsSystem(_weaponBalance, _bus, kaijuParts, meta);// injects IPartStateQuery + IWeaponTierQuery
            //   var economy    = new EconomySystem(_economyConfig, _bus, meta);
            //   var gameFeel   = new GameFeelSystem(_gameFeelConfig, _bus, kaijuParts);
            //   var bridge     = new BulletSimBridge(_bus);                                // IBulletSimBridge
            //   ...
            // No system references another directly — only the bus + Core query interfaces.
        }
    }
}
