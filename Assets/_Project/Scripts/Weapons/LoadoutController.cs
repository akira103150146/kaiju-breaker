using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Weapons
{
    /// <summary>Which pool slot a weapon occupies. Primary = laser pool, Secondary = missile pool.</summary>
    public enum WeaponSlot
    {
        /// <summary>Primary pool — accepts only <see cref="WeaponType.Laser"/> (L1–L4).</summary>
        Primary = 0,

        /// <summary>Secondary pool — accepts only <see cref="WeaponType.Missile"/> (M1–M4).</summary>
        Secondary = 1
    }

    /// <summary>
    /// Owns the player's 1 primary + 1 secondary loadout (weapon-system.md C.1 / F.3, Story 010).
    /// There is NO inventory — a weapon-pod pickup REPLACES the current weapon in its pool and the
    /// old one is dropped (control-manifest §3 Weapons). Pool slots never cross: a Laser pod is
    /// silently ignored by the Secondary slot and vice-versa.
    ///
    /// Pure C# (ADR-0005 testability): the scene shell's <c>WeaponPodPickup</c> MonoBehaviour calls
    /// <see cref="EquipWeapon"/> from <c>OnTriggerEnter2D</c>. The controller raises
    /// <see cref="WeaponActivated"/>/<see cref="WeaponDeactivated"/> so App can Enable/Disable the
    /// concrete weapon behaviour, and publishes <see cref="WeaponEquipped"/> on the bus so Stage can
    /// enqueue an autosave (Weapons never calls <see cref="ISaveService"/> directly — ADR-0002 §3).
    /// Upgrade tier is NOT reset on pickup; it is owned by <see cref="IWeaponTierQuery"/>.
    /// </summary>
    public sealed class LoadoutController
    {
        private readonly IEventBus _bus;
        private readonly WeaponBalanceConfig _balance;
        private readonly Func<WeaponId, WeaponDef> _resolve;
        private readonly WeaponDef[] _slots = new WeaponDef[2];

        /// <summary>Raised with the newly-activated <see cref="WeaponDef"/> after an equip (App enables its behaviour).</summary>
        public event Action<WeaponDef> WeaponActivated;

        /// <summary>Raised with the replaced <see cref="WeaponDef"/> when a pickup swaps it out (App disables its behaviour).</summary>
        public event Action<WeaponDef> WeaponDeactivated;

        /// <param name="bus">Typed event bus (required).</param>
        /// <param name="balance">Balance config — source of the fresh-save default loadout (required).</param>
        /// <param name="weaponResolver">Maps a <see cref="WeaponId"/> to its authored <see cref="WeaponDef"/>
        /// (App composition root provides this from the content registry). Required.</param>
        public LoadoutController(IEventBus bus, WeaponBalanceConfig balance, Func<WeaponId, WeaponDef> weaponResolver)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _balance = balance ? balance : throw new ArgumentNullException(nameof(balance));
            _resolve = weaponResolver ?? throw new ArgumentNullException(nameof(weaponResolver));
        }

        /// <summary>The pool a weapon id belongs to (laser ids &lt; 4 → Primary, missile ids ≥ 4 → Secondary).</summary>
        public static WeaponSlot SlotOf(WeaponId id) => (int)id < 4 ? WeaponSlot.Primary : WeaponSlot.Secondary;

        /// <summary>The <see cref="WeaponType"/> a slot accepts.</summary>
        public static WeaponType RequiredType(WeaponSlot slot) =>
            slot == WeaponSlot.Primary ? WeaponType.Laser : WeaponType.Missile;

        /// <summary>Currently-equipped weapon in a slot (null before <see cref="Initialize"/>).</summary>
        public WeaponDef GetActiveWeapon(WeaponSlot slot) => _slots[(int)slot];

        /// <summary>
        /// Seed the loadout at run start from persisted choice. Reads
        /// <see cref="ISaveService.GetInitialLoadout"/>; on a fresh save (null) falls back to the
        /// data-driven defaults in <see cref="WeaponBalanceConfig"/> (never a hardcoded weapon id).
        /// Activates both slots but does NOT publish <see cref="WeaponEquipped"/> (that signals a
        /// mid-run pickup, not the initial seed).
        /// </summary>
        public void Initialize(ISaveService saveService)
        {
            WeaponId primary, secondary;
            var stored = saveService?.GetInitialLoadout();
            if (stored.HasValue)
            {
                primary = stored.Value.Primary;
                secondary = stored.Value.Secondary;
            }
            else
            {
                primary = _balance.DefaultPrimary;
                secondary = _balance.DefaultSecondary;
            }

            SeedSlot(WeaponSlot.Primary, _resolve(primary));
            SeedSlot(WeaponSlot.Secondary, _resolve(secondary));
        }

        /// <summary>
        /// Equip <paramref name="def"/> into <paramref name="slot"/> (weapon-pod pickup). Silently
        /// ignored (returns false, no state change) if the weapon's pool does not match the slot.
        /// On success: deactivates the replaced weapon, activates the new one, publishes
        /// <see cref="WeaponEquipped"/>, and returns the replaced <see cref="WeaponDef"/> (null if the
        /// slot was empty) so GameFeel can play a pickup cue.
        /// </summary>
        public WeaponDef EquipWeapon(WeaponDef def, WeaponSlot slot)
        {
            if (def == null) return null;
            if (def.Type != RequiredType(slot)) return null; // wrong pool — silent ignore (C.1)

            WeaponDef replaced = _slots[(int)slot];
            if (!ReferenceEquals(replaced, null) && !ReferenceEquals(replaced, def))
                WeaponDeactivated?.Invoke(replaced);

            _slots[(int)slot] = def;
            WeaponActivated?.Invoke(def);
            _bus.Publish(new WeaponEquipped(def.Id));
            return replaced;
        }

        private void SeedSlot(WeaponSlot slot, WeaponDef def)
        {
            _slots[(int)slot] = def;
            if (!ReferenceEquals(def, null)) WeaponActivated?.Invoke(def);
        }
    }
}
