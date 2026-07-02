using System.Linq;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Weapons;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 003 — WeaponController base &amp; dual-track event-bus wiring.
    /// Verifies the shared laser/missile core: LaserHit/MissileHit publish path, the missile
    /// magazine→reload state machine, PartBroke→ClearCollider hook (idempotent, no state mutation),
    /// and the assembly-isolation guardrail (Weapons never references KaijuParts).
    /// </summary>
    public sealed class WeaponsControllerBaseTests
    {
        // ── Minimal concrete doubles over the abstract bases ─────────────────────

        private sealed class TestLaser : LaserWeaponBase
        {
            public int ClearColliderCalls;
            public int LastClearedPart = -999;

            public TestLaser(IEventBus bus, IWeaponTierQuery tier, IPartStateQuery parts,
                WeaponBalanceConfig balance, WeaponDef def) : base(bus, tier, parts, balance, def) { }

            public void FireTick(int partId, int kaijuId, float heatDelta) => EmitLaserHit(partId, kaijuId, heatDelta);
            public int Tier => CurrentTier;

            protected override void ClearCollider(int partId)
            {
                ClearColliderCalls++;
                LastClearedPart = partId;
            }
        }

        private sealed class TestMissile : MissileWeaponBase
        {
            public TestMissile(IEventBus bus, IWeaponTierQuery tier, IPartStateQuery parts,
                WeaponBalanceConfig balance, WeaponDef def) : base(bus, tier, parts, balance, def) { }

            protected override int MagCapacity => Def.M1MagSize;
            protected override float ReloadTime => Def.M1ReloadTime;

            public bool FireShot(int missileCount) => TryConsumeShot(missileCount);
            public void Emit(int partId, int kaijuId, float bdb) => EmitMissileHit(partId, kaijuId, bdb);
        }

        // ── Fixtures ─────────────────────────────────────────────────────────────

        private static WeaponBalanceConfig Balance() =>
            ContentTestFactory.Create<WeaponBalanceConfig>();

        private static WeaponDef LaserDef() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.L1), ("_type", WeaponType.Laser));

        private static WeaponDef MissileDef(int mag = 6, float reload = 3f) =>
            ContentTestFactory.Create<WeaponDef>(
                ("_id", WeaponId.M1), ("_type", WeaponType.Missile),
                ("_m1MagSize", mag), ("_m1ReloadTime", reload));

        private static TestLaser MakeLaser(IEventBus bus) =>
            new TestLaser(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), LaserDef());

        private static TestMissile MakeMissile(IEventBus bus, int mag = 6, float reload = 3f) =>
            new TestMissile(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), MissileDef(mag, reload));

        // ── AC-1: LaserHit publish path ──────────────────────────────────────────

        [Test]
        public void test_emit_laser_hit_publishes_single_laserhit_with_delta()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var laser = MakeLaser(bus);

            // Act — 37.5 HU/s over a 16 ms frame = 0.6 HU
            laser.FireTick(partId: 1, kaijuId: 0, heatDelta: 37.5f * 0.016f);

            // Assert
            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(1));
            var hit = bus.Events<LaserHit>()[0];
            Assert.That(hit.PartId, Is.EqualTo(1));
            Assert.That(hit.KaijuId, Is.EqualTo(0));
            Assert.That(hit.HeatDelta, Is.EqualTo(0.6f).Within(1e-4f));
        }

        [Test]
        public void test_emit_laser_hit_drops_nonpositive_delta()
        {
            var bus = new RecordingEventBus();
            var laser = MakeLaser(bus);

            laser.FireTick(1, 0, 0f);
            laser.FireTick(1, 0, -5f);

            Assert.That(bus.CountOf<LaserHit>(), Is.EqualTo(0));
        }

        // ── AC-2: missile magazine → reload state machine ────────────────────────

        [Test]
        public void test_missile_magazine_exhausts_after_three_two_missile_shots()
        {
            var bus = new RecordingEventBus();
            var missile = MakeMissile(bus, mag: 6, reload: 3f);

            Assert.That(missile.FireShot(2), Is.True, "shot 1");
            Assert.That(missile.FireShot(2), Is.True, "shot 2");
            Assert.That(missile.FireShot(2), Is.True, "shot 3");
            Assert.That(missile.Ammo, Is.EqualTo(0));
            Assert.That(missile.IsReloading, Is.True, "auto-reload starts on empty magazine");

            // 4th shot blocked while reloading
            Assert.That(missile.FireShot(2), Is.False, "cannot fire during reload");
        }

        [Test]
        public void test_missile_reload_refills_magazine_after_reload_time()
        {
            var bus = new RecordingEventBus();
            var missile = MakeMissile(bus, mag: 6, reload: 3f);

            missile.FireShot(2);
            missile.FireShot(2);
            missile.FireShot(2); // empty → reloading

            missile.Tick(1.5f);
            Assert.That(missile.IsReloading, Is.True, "still reloading at 1.5s");
            Assert.That(missile.FireShot(2), Is.False);

            missile.Tick(1.5f); // total 3.0s
            Assert.That(missile.IsReloading, Is.False, "reload complete at 3.0s");
            Assert.That(missile.Ammo, Is.EqualTo(6));
            Assert.That(missile.CanFire, Is.True);
            Assert.That(missile.FireShot(2), Is.True);
        }

        [Test]
        public void test_missile_emit_publishes_missilehit_tagged_with_weapon()
        {
            var bus = new RecordingEventBus();
            var missile = MakeMissile(bus);

            missile.Emit(partId: 4, kaijuId: 2, bdb: 500f);

            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(1));
            var hit = bus.Events<MissileHit>()[0];
            Assert.That(hit.PartId, Is.EqualTo(4));
            Assert.That(hit.KaijuId, Is.EqualTo(2));
            Assert.That(hit.BreakDeltaBase, Is.EqualTo(500f).Within(1e-4f));
            Assert.That(hit.Weapon, Is.EqualTo(WeaponId.M1));
        }

        // ── AC-3: PartBroke → ClearCollider (idempotent, no state mutation) ───────

        [Test]
        public void test_partbroke_invokes_clearcollider_once_while_enabled()
        {
            var bus = new RecordingEventBus();
            var laser = MakeLaser(bus);
            laser.Enable();

            bus.Publish(new PartBroke(3, 0, PartType.Normal, default, 1, BreakQuality.Softened,
                System.Array.Empty<int>(), false));

            Assert.That(laser.ClearColliderCalls, Is.EqualTo(1));
            Assert.That(laser.LastClearedPart, Is.EqualTo(3));
        }

        [Test]
        public void test_disabled_weapon_ignores_partbroke()
        {
            var bus = new RecordingEventBus();
            var laser = MakeLaser(bus);
            // never Enable(), or Enable then Disable
            laser.Enable();
            laser.Disable();

            bus.Publish(new PartBroke(3, 0, PartType.Normal, default, 1, BreakQuality.Softened,
                System.Array.Empty<int>(), false));

            Assert.That(laser.ClearColliderCalls, Is.EqualTo(0));
        }

        [Test]
        public void test_enable_is_idempotent_no_double_clearcollider()
        {
            var bus = new RecordingEventBus();
            var laser = MakeLaser(bus);
            laser.Enable();
            laser.Enable(); // second Enable must not double-subscribe

            bus.Publish(new PartBroke(7, 0, PartType.Normal, default, 1, BreakQuality.Normal,
                System.Array.Empty<int>(), false));

            Assert.That(laser.ClearColliderCalls, Is.EqualTo(1));
        }

        // ── AC-4: assembly isolation guardrail (ADR-0005) ────────────────────────

        [Test]
        public void test_weapons_assembly_does_not_reference_kaijuparts()
        {
            var weaponsAsm = typeof(LaserWeaponBase).Assembly;
            bool referencesKaijuParts = weaponsAsm
                .GetReferencedAssemblies()
                .Any(a => a.Name == "KaijuBreaker.KaijuParts");

            Assert.That(referencesKaijuParts, Is.False,
                "Weapons must reach part state only through IPartStateQuery/events, never a direct KaijuParts reference (ADR-0005).");
        }
    }
}
