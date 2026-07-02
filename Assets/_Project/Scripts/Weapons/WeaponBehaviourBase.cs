using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>
    /// Shared core for every weapon (laser + missile families). Pure C# — no MonoBehaviour, no
    /// Physics2D — so hit resolution is EditMode-testable: the scene shell runs the raycast /
    /// overlap query and passes the already-resolved target part ids into the weapon's fire
    /// methods (Story 003, ADR-0002/0005).
    ///
    /// Constructor-injects the event bus, the two query interfaces, and the two config SOs
    /// (ADR-0003 — all tuning is data-driven, no magic numbers). While <see cref="Enable"/>d the
    /// weapon subscribes to <see cref="PartBroke"/> so a subclass can drop a stale target on the
    /// now-broken part via <see cref="ClearCollider"/>; it MUST NOT mutate part state — KaijuParts
    /// is the sole owner of that (§5 manifest). Tier gating goes through <see cref="CurrentTier"/>.
    /// </summary>
    public abstract class WeaponBehaviourBase : IDisposable
    {
        /// <summary>Typed event bus — weapons publish hit events onto it (never call KaijuParts directly).</summary>
        protected readonly IEventBus Bus;

        /// <summary>Current upgrade tier lookup (0..3) for this weapon.</summary>
        protected readonly IWeaponTierQuery TierQuery;

        /// <summary>Read-only part state (heat / armor / position / alive) for targeting and gates.</summary>
        protected readonly IPartStateQuery PartQuery;

        /// <summary>Global balance knobs (D₀, conversions, capacities, stagger).</summary>
        protected readonly WeaponBalanceConfig Balance;

        /// <summary>This weapon's static tuning asset.</summary>
        protected readonly WeaponDef Def;

        /// <summary>Stable id of this weapon (from <see cref="WeaponDef.Id"/>).</summary>
        protected readonly WeaponId WeaponId;

        private readonly Action<PartBroke> _onPartBroke;
        private bool _enabled;

        /// <param name="bus">Typed event bus (required).</param>
        /// <param name="tierQuery">Upgrade-tier lookup (required).</param>
        /// <param name="partQuery">Part-state read interface (required).</param>
        /// <param name="balance">Global weapon balance config SO (required).</param>
        /// <param name="def">This weapon's tuning SO (required).</param>
        protected WeaponBehaviourBase(IEventBus bus, IWeaponTierQuery tierQuery, IPartStateQuery partQuery,
            WeaponBalanceConfig balance, WeaponDef def)
        {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            TierQuery = tierQuery ?? throw new ArgumentNullException(nameof(tierQuery));
            PartQuery = partQuery ?? throw new ArgumentNullException(nameof(partQuery));
            Balance = balance ? balance : throw new ArgumentNullException(nameof(balance));
            Def = def ? def : throw new ArgumentNullException(nameof(def));
            WeaponId = def.Id;
            _onPartBroke = OnPartBroke;
        }

        /// <summary>Current permanent upgrade tier (0 = base … 3 = unique mechanic) for this weapon.</summary>
        protected int CurrentTier => TierQuery.GetTier(WeaponId);

        /// <summary>True once <see cref="Enable"/> has been called (and not yet <see cref="Disable"/>d).</summary>
        public bool IsEnabled => _enabled;

        /// <summary>Begin listening for part breaks. Call when the weapon becomes the active/equipped weapon. Idempotent.</summary>
        public void Enable()
        {
            if (_enabled) return;
            Bus.Subscribe(_onPartBroke);
            _enabled = true;
        }

        /// <summary>Stop listening. Call when the weapon is unequipped/deactivated. Idempotent.</summary>
        public void Disable()
        {
            if (!_enabled) return;
            Bus.Unsubscribe(_onPartBroke);
            _enabled = false;
        }

        /// <summary>Releases the PartBroke subscription.</summary>
        public void Dispose() => Disable();

        private void OnPartBroke(PartBroke evt) => ClearCollider(evt.PartId);

        /// <summary>
        /// Hook for the scene shell to release any cached collider/target reference for a part that
        /// just broke. Base implementation is a no-op — pure-logic subclasses that hold no colliders
        /// do not need to override it. MUST NOT touch part state.
        /// </summary>
        protected virtual void ClearCollider(int partId) { }
    }
}
