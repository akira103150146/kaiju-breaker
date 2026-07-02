using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Weapons;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 010 — Loadout system (1 primary + 1 secondary, pod pickup replaces, pool-typed slots,
    /// initial loadout from ISaveService with a data-driven fresh-save fallback).
    /// </summary>
    public sealed class WeaponsLoadoutSystemTests
    {
        // Minimal ISaveService double: configurable initial loadout + autosave call count.
        private sealed class StubSaveService : ISaveService
        {
            private readonly (WeaponId, WeaponId)? _loadout;
            public int EnqueueCalls;
            public StubSaveService((WeaponId, WeaponId)? loadout) { _loadout = loadout; }
            public void EnqueueAutosave() => EnqueueCalls++;
            public void FlushSync() { }
            public (WeaponId Primary, WeaponId Secondary)? GetInitialLoadout() => _loadout;
        }

        private static WeaponDef Def(WeaponId id) =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", id), ("_type", (int)id < 4 ? WeaponType.Laser : WeaponType.Missile));

        private readonly Dictionary<WeaponId, WeaponDef> _catalog = new Dictionary<WeaponId, WeaponDef>();

        private LoadoutController MakeController(RecordingEventBus bus, WeaponBalanceConfig balance = null)
        {
            _catalog.Clear();
            foreach (WeaponId id in System.Enum.GetValues(typeof(WeaponId)))
                _catalog[id] = Def(id);
            balance = balance ?? ContentTestFactory.Create<WeaponBalanceConfig>();
            return new LoadoutController(bus, balance, id => _catalog[id]);
        }

        // ── AC-1: pickup replaces correct-type weapon; wrong type ignored ────────

        [Test]
        public void test_equip_primary_pod_replaces_laser_and_publishes_event()
        {
            var bus = new RecordingEventBus();
            var loadout = MakeController(bus);
            loadout.Initialize(new StubSaveService((WeaponId.L1, WeaponId.M1)));

            WeaponDef deactivated = null;
            loadout.WeaponDeactivated += d => deactivated = d;

            var replaced = loadout.EquipWeapon(_catalog[WeaponId.L2], WeaponSlot.Primary);

            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Primary).Id, Is.EqualTo(WeaponId.L2));
            Assert.That(replaced.Id, Is.EqualTo(WeaponId.L1));
            Assert.That(deactivated.Id, Is.EqualTo(WeaponId.L1), "old primary deactivated");
            Assert.That(bus.CountOf<WeaponEquipped>(), Is.EqualTo(1));
            Assert.That(bus.Events<WeaponEquipped>()[0].Weapon, Is.EqualTo(WeaponId.L2));
        }

        [Test]
        public void test_missile_pod_into_primary_slot_is_ignored()
        {
            var bus = new RecordingEventBus();
            var loadout = MakeController(bus);
            loadout.Initialize(new StubSaveService((WeaponId.L1, WeaponId.M1)));

            var replaced = loadout.EquipWeapon(_catalog[WeaponId.M3], WeaponSlot.Primary);

            Assert.That(replaced, Is.Null);
            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Primary).Id, Is.EqualTo(WeaponId.L1));
            Assert.That(bus.CountOf<WeaponEquipped>(), Is.EqualTo(0));
        }

        // ── AC-2: secondary equip does not touch primary; no inventory ───────────

        [Test]
        public void test_secondary_equip_leaves_primary_untouched()
        {
            var bus = new RecordingEventBus();
            var loadout = MakeController(bus);
            loadout.Initialize(new StubSaveService((WeaponId.L2, WeaponId.M1)));

            loadout.EquipWeapon(_catalog[WeaponId.M3], WeaponSlot.Secondary);

            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Secondary).Id, Is.EqualTo(WeaponId.M3));
            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Primary).Id, Is.EqualTo(WeaponId.L2));
        }

        [Test]
        public void test_two_consecutive_pods_keep_only_last_no_inventory()
        {
            var bus = new RecordingEventBus();
            var loadout = MakeController(bus);
            loadout.Initialize(new StubSaveService((WeaponId.L1, WeaponId.M1)));

            loadout.EquipWeapon(_catalog[WeaponId.M2], WeaponSlot.Secondary);
            loadout.EquipWeapon(_catalog[WeaponId.M4], WeaponSlot.Secondary);

            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Secondary).Id, Is.EqualTo(WeaponId.M4));
        }

        // ── AC-3: initial loadout from ISaveService + fallback ───────────────────

        [Test]
        public void test_initialize_reads_loadout_from_save_service()
        {
            var bus = new RecordingEventBus();
            var loadout = MakeController(bus);

            loadout.Initialize(new StubSaveService((WeaponId.L1, WeaponId.M2)));

            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Primary).Id, Is.EqualTo(WeaponId.L1));
            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Secondary).Id, Is.EqualTo(WeaponId.M2));
        }

        [Test]
        public void test_initialize_null_loadout_uses_config_default_fallback()
        {
            var bus = new RecordingEventBus();
            var balance = ContentTestFactory.Create<WeaponBalanceConfig>(
                ("_defaultPrimary", WeaponId.L3), ("_defaultSecondary", WeaponId.M4));
            var loadout = MakeController(bus, balance);

            loadout.Initialize(new StubSaveService(null)); // fresh save

            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Primary).Id, Is.EqualTo(WeaponId.L3));
            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Secondary).Id, Is.EqualTo(WeaponId.M4));
        }

        // ── AC-4: pool validation — wrong-type equip is a no-op, no event ────────

        [Test]
        public void test_wrong_pool_equip_emits_no_event_and_keeps_current()
        {
            var bus = new RecordingEventBus();
            var loadout = MakeController(bus);
            loadout.Initialize(new StubSaveService((WeaponId.L1, WeaponId.M1)));

            bool activatedFired = false;
            loadout.WeaponActivated += _ => activatedFired = true;

            loadout.EquipWeapon(_catalog[WeaponId.M3], WeaponSlot.Primary); // missile into laser slot

            Assert.That(loadout.GetActiveWeapon(WeaponSlot.Primary).Id, Is.EqualTo(WeaponId.L1));
            Assert.That(bus.CountOf<WeaponEquipped>(), Is.EqualTo(0));
            Assert.That(activatedFired, Is.False);
        }

        [Test]
        public void test_slot_of_maps_pools_by_id_ordinal()
        {
            Assert.That(LoadoutController.SlotOf(WeaponId.L4), Is.EqualTo(WeaponSlot.Primary));
            Assert.That(LoadoutController.SlotOf(WeaponId.M1), Is.EqualTo(WeaponSlot.Secondary));
        }
    }
}
