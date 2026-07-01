// SCAFFOLD STUB — structural placeholder only, not functional.
// See docs/architecture/architecture.md §4 (Scene Architecture) and
// ADR-0005 §3 (App as composition root) for design intent.
using UnityEngine;

namespace KaijuBreaker.App
{
    /// <summary>
    /// Composition root (ADR-0005 §3). Lives on a persistent GameObject in the
    /// Bootstrap scene. Runs once at application startup before any other scene loads.
    ///
    /// Responsibilities:
    ///   1. Construct the IEventBus implementation.
    ///   2. Construct concrete system objects and inject their dependencies.
    ///   3. Wire query interfaces (IPartStateQuery, IDifficultyProvider, ISaveService)
    ///      into systems that need them via constructor/method injection.
    ///   4. Initialise Addressables and load the core_boot group.
    ///   5. Transition to MetaHub scene via Addressables async load.
    ///
    /// No game state lives here — this object is pure wiring.
    /// Do NOT add FindObjectOfType or static singletons holding game state.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        // Inspector-assigned ScriptableObject config references.
        // Populated from the Bootstrap scene — no runtime Find() calls.
        // TODO: add [SerializeField] fields for GlobalConfig, SaveConfig, etc. once Content SOs are defined.

        private void Awake()
        {
            // TODO: Construct IEventBus (e.g. new TypedEventBus())

            // TODO: Construct system implementations:
            //   var difficulty = new DifficultySystem(difficultyConfig);
            //   var meta       = new MetaSystem(saveConfig, eventBus);
            //   var kaijuParts = new KaijuPartsSystem(partSystemConfig, eventBus);
            //   var weapons    = new WeaponsSystem(weaponBalance, eventBus, kaijuParts /* IPartStateQuery */);
            //   var economy    = new EconomySystem(economyConfig, eventBus);
            //   var gameFeel   = new GameFeelSystem(gameFeelConfig, eventBus);
            //   var stage      = new StageSystem(stageDef, difficulty, eventBus);
            //   var input      = new InputSystem(inputSettings, eventBus);
            //   var ui         = new UISystem(eventBus, kaijuParts /* IPartStateQuery */);

            // TODO: Initialise Addressables
            // TODO: Load Bootstrap Addressable group (core_boot) async
            // TODO: On load complete, transition to MetaHub scene
        }
    }
}
