using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Weapons;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 007 — M3 AP Torpedo heat-shock gate. Verifies the same-frame (never cached)
    /// <see cref="IPartStateQuery.GetHeatState"/> query drives the detonation multiplier, the
    /// 3-round magazine / 4s reload cycle, and that state is re-queried fresh on every shot
    /// (weapon-system.md C.5 M3, E.1, G.3).
    /// </summary>
    public sealed class WeaponsM3HeatShockTests
    {
        // ── Fixtures ─────────────────────────────────────────────────────────────

        private static WeaponBalanceConfig Balance() =>
            ContentTestFactory.Create<WeaponBalanceConfig>();

        private static WeaponDef M3Def() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.M3), ("_type", WeaponType.Missile));

        // ── AC-1: SOFTENED target triggers heat-shock detonation (6000) ──────────

        [Test]
        public void test_m3_tryfire_softened_target_triggers_heat_shock_detonation()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, heat: HeatState.Softened);
            var m3 = new M3ApTorpedo(bus, new StubWeaponTierQuery(), parts, Balance(), M3Def());

            // Act
            bool fired = m3.TryFire(targetPartId: 1, kaijuId: 0);

            // Assert
            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(1));
            var hit = bus.Events<MissileHit>()[0];
            Assert.That(hit.BreakDeltaBase, Is.EqualTo(6000f).Within(1e-2f));
            Assert.That(hit.Weapon, Is.EqualTo(WeaponId.M3));
        }

        // ── AC-2: unsoftened target deals base break only (3000, no state mult applied here) ─

        [Test]
        public void test_m3_tryfire_unsoftened_target_deals_base_break_only()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, heat: HeatState.Intact);
            var m3 = new M3ApTorpedo(bus, new StubWeaponTierQuery(), parts, Balance(), M3Def());

            m3.TryFire(targetPartId: 1, kaijuId: 0);

            var hit = bus.Events<MissileHit>()[0];
            Assert.That(hit.BreakDeltaBase, Is.EqualTo(3000f).Within(1e-2f),
                "unsoftened base value; KaijuParts applies B_unsoftened_mult downstream, not tested here");
        }

        // ── AC-3: 3-round magazine exhausts, reload at 4.0s ───────────────────────

        [Test]
        public void test_m3_magazine_exhausts_after_three_shots_then_reloads()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, heat: HeatState.Intact);
            var m3 = new M3ApTorpedo(bus, new StubWeaponTierQuery(), parts, Balance(), M3Def());

            Assert.That(m3.TryFire(1, 0), Is.True, "shot 1");
            Assert.That(m3.TryFire(1, 0), Is.True, "shot 2");
            Assert.That(m3.TryFire(1, 0), Is.True, "shot 3");
            Assert.That(m3.Ammo, Is.EqualTo(0));
            Assert.That(m3.IsReloading, Is.True);
            Assert.That(m3.TryFire(1, 0), Is.False, "4th shot blocked during reload");

            m3.Tick(4.0f);
            Assert.That(m3.IsReloading, Is.False);
            Assert.That(m3.Ammo, Is.EqualTo(3));
            Assert.That(m3.TryFire(1, 0), Is.True);
        }

        // ── AC-4: heat state re-queried fresh every shot, no caching ──────────────

        [Test]
        public void test_m3_requeries_heat_state_fresh_every_shot_no_caching()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, heat: HeatState.Intact);
            var m3 = new M3ApTorpedo(bus, new StubWeaponTierQuery(), parts, Balance(), M3Def());

            m3.TryFire(1, 0); // NORMAL -> 3000

            parts[1].Heat = HeatState.Softened; // state changes between shots — no cache to invalidate
            m3.TryFire(1, 0); // SOFTENED -> 6000

            var hits = bus.Events<MissileHit>();
            Assert.That(hits.Count, Is.EqualTo(2));
            Assert.That(hits[0].BreakDeltaBase, Is.EqualTo(3000f).Within(1e-2f));
            Assert.That(hits[1].BreakDeltaBase, Is.EqualTo(6000f).Within(1e-2f));
        }
    }
}
