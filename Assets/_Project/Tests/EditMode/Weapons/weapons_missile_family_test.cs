using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using KaijuBreaker.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 006 — Missile family firing: M1 Homing Missile, M2 Swarm Launcher, M4 Cluster Bomb
    /// base (Tier 0–2) fire behaviour. Verifies MissileHit publish counts/values, magazine→reload
    /// state machines, and the AoE piecewise formula (weapon-system.md C.5 M1/M2/M4, G.3).
    /// M3 AP Torpedo is covered separately in weapons_m3_heat_shock_test.cs (Story 007).
    /// </summary>
    public sealed class WeaponsMissileFamilyTests
    {
        // ── Fixtures ─────────────────────────────────────────────────────────────

        private static WeaponBalanceConfig Balance() =>
            ContentTestFactory.Create<WeaponBalanceConfig>();

        private static WeaponDef M1Def() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.M1), ("_type", WeaponType.Missile));

        private static WeaponDef M2Def() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.M2), ("_type", WeaponType.Missile));

        private static WeaponDef M4Def() =>
            ContentTestFactory.Create<WeaponDef>(("_id", WeaponId.M4), ("_type", WeaponType.Missile));

        // ── AC-1: M1 shot emits 2 MissileHits at 0.5 x D0 x buPerD0 each ─────────

        [Test]
        public void test_m1_tryfire_emits_two_missile_hits_with_correct_delta()
        {
            // Arrange
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, worldPosition: new Vector2(5f, 5f));
            var m1 = new M1HomingMissile(bus, new StubWeaponTierQuery(), parts, Balance(), M1Def());

            // Act
            bool fired = m1.TryFire(targetPartId: 1, kaijuId: 0);

            // Assert
            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(2));
            foreach (var hit in bus.Events<MissileHit>())
            {
                Assert.That(hit.PartId, Is.EqualTo(1));
                // 0.5×D₀ per missile in BU: M1DmgPerMissileMult(0.5) × BuPerD0(10) = 5 BU (2 missiles = 1×D₀).
                Assert.That(hit.BreakDeltaBase, Is.EqualTo(5f).Within(1e-3f));
                Assert.That(hit.Weapon, Is.EqualTo(WeaponId.M1));
            }
        }

        [Test]
        public void test_m1_can_track_target_returns_false_beyond_max_deflection()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, worldPosition: new Vector2(0f, -10f)); // directly behind = 180 deg
            var m1 = new M1HomingMissile(bus, new StubWeaponTierQuery(), parts, Balance(), M1Def());

            bool canTrack = m1.CanTrackTarget(Vector2.zero, Vector2.up, targetPartId: 1);

            Assert.That(canTrack, Is.False, "180 deg reverse target exceeds the +-60 deg tracking cone");
        }

        [Test]
        public void test_m1_can_track_target_returns_true_within_cone()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, worldPosition: new Vector2(1f, 10f)); // near-forward, small deflection
            var m1 = new M1HomingMissile(bus, new StubWeaponTierQuery(), parts, Balance(), M1Def());

            bool canTrack = m1.CanTrackTarget(Vector2.zero, Vector2.up, targetPartId: 1);

            Assert.That(canTrack, Is.True);
        }

        // ── AC-2: M2 Chain Hive — first salvo of 8 now, full 3×8 over the burst ───

        [Test]
        public void test_m2_chain_hive_first_salvo_emits_eight_then_completes_over_salvos()
        {
            var bus = new RecordingEventBus();
            var m2 = new M2SwarmLauncher(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M2Def());
            var hits = new List<int> { 1, 1, 1, 1, 1, 1, 1, 1 };

            // First salvo fires immediately.
            bool fired = m2.TryFire(hits, kaijuId: 0);
            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(8), "first of 3 salvos fires now");
            Assert.That(m2.IsBurstInProgress, Is.True);
            // Per micro in BU: M2DmgPerMissileMult(0.125) × BuPerD0(10) = 1.25 BU (8 micros = 1×D₀/salvo).
            foreach (var hit in bus.Events<MissileHit>())
                Assert.That(hit.BreakDeltaBase, Is.EqualTo(1.25f).Within(1e-3f));

            // Remaining salvos auto-fire each inter-salvo interval (0.8s).
            m2.Tick(0.8f);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(16), "salvo 2");
            m2.Tick(0.8f);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(24), "salvo 3 — full 3×8 Chain Hive burst");
            Assert.That(m2.IsBurstInProgress, Is.False);
            Assert.That(m2.IsReloading, Is.True, "24-round burst magazine empty → reload");
        }

        [Test]
        public void test_m2_tryfire_mid_burst_is_ignored()
        {
            var bus = new RecordingEventBus();
            var m2 = new M2SwarmLauncher(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M2Def());
            m2.TryFire(new List<int> { 1, 1, 1, 1, 1, 1, 1, 1 }, kaijuId: 0);

            bool second = m2.TryFire(new List<int> { 2, 2, 2, 2, 2, 2, 2, 2 }, kaijuId: 0);

            Assert.That(second, Is.False, "a TryFire mid-burst is a no-op");
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(8), "ignored call adds no hits");
        }

        [Test]
        public void test_m2_tryfire_seven_landed_one_miss_emits_seven_hits()
        {
            var bus = new RecordingEventBus();
            var m2 = new M2SwarmLauncher(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M2Def());
            var hits = new List<int> { 1, 1, 1, 1, 1, 1, 1 }; // 7 landed, 1 missed (absent)

            m2.TryFire(hits, kaijuId: 0);

            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(7), "misses are absent from the resolved list and publish no event");
        }

        [Test]
        public void test_m2_tryfire_exhausts_magazine_and_starts_reload_immediately()
        {
            var bus = new RecordingEventBus();
            var m2 = new M2SwarmLauncher(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M2Def());

            // A salvo always consumes M2MicroCount (8) rounds regardless of how many actually land.
            m2.TryFire(new List<int> { 1 }, kaijuId: 0);
            Assert.That(m2.Ammo, Is.EqualTo(16), "first salvo consumed 8 of the 24-round Chain Hive magazine");
            Assert.That(m2.IsReloading, Is.False, "burst still in progress, not reloading yet");

            m2.Tick(0.8f); // salvo 2 → 8 left
            m2.Tick(0.8f); // salvo 3 → 0 left → reload
            Assert.That(m2.Ammo, Is.EqualTo(0));
            Assert.That(m2.IsReloading, Is.True, "24-round burst spent → reload");

            m2.Tick(5.0f);
            Assert.That(m2.IsReloading, Is.False);
            Assert.That(m2.Ammo, Is.EqualTo(24));
        }

        // ── AC-3: M4 AoE piecewise output — N=1 -> 2xD0, N=2 -> D0/2 each ────────

        [Test]
        public void test_m4_tryfire_two_targets_each_receive_half_output()
        {
            var bus = new RecordingEventBus();
            var m4 = new M4ClusterBomb(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M4Def());

            bool fired = m4.TryFire(new List<int> { 1, 2 }, kaijuId: 0);

            Assert.That(fired, Is.True);
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(2));
            // N=2 AoE split in BU: (M4TotalOutputCapMult(1) / N(2)) × BuPerD0(10) = 5 BU each (total cap = 1×D₀).
            foreach (var hit in bus.Events<MissileHit>())
                Assert.That(hit.BreakDeltaBase, Is.EqualTo(5f).Within(1e-3f));
        }

        [Test]
        public void test_m4_tryfire_single_target_receives_double_output()
        {
            var bus = new RecordingEventBus();
            var m4 = new M4ClusterBomb(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M4Def());

            m4.TryFire(new List<int> { 1 }, kaijuId: 0);

            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(1));
            // N=1 single-target in BU: M4SingleTargetMult(2) × BuPerD0(10) = 20 BU (2×D₀).
            Assert.That(bus.Events<MissileHit>()[0].BreakDeltaBase, Is.EqualTo(20f).Within(1e-3f));
        }

        [Test]
        public void test_m4_tryfire_empty_target_list_consumes_round_emits_nothing()
        {
            var bus = new RecordingEventBus();
            var m4 = new M4ClusterBomb(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M4Def());

            bool fired = m4.TryFire(new List<int>(), kaijuId: 0);

            Assert.That(fired, Is.True, "the bomb still drops and consumes ammo even with nothing in the AoE");
            Assert.That(bus.CountOf<MissileHit>(), Is.EqualTo(0));
            Assert.That(m4.Ammo, Is.EqualTo(3));
        }

        [Test]
        public void test_m4_magazine_exhausts_after_four_shots_then_reloads()
        {
            var bus = new RecordingEventBus();
            var m4 = new M4ClusterBomb(bus, new StubWeaponTierQuery(), new StubPartStateQuery(), Balance(), M4Def());

            for (int i = 0; i < 4; i++)
                Assert.That(m4.TryFire(new List<int> { 1 }, kaijuId: 0), Is.True, $"shot {i + 1}");

            Assert.That(m4.Ammo, Is.EqualTo(0));
            Assert.That(m4.IsReloading, Is.True);
            Assert.That(m4.TryFire(new List<int> { 1 }, kaijuId: 0), Is.False);

            m4.Tick(3.5f);
            Assert.That(m4.IsReloading, Is.False);
            Assert.That(m4.Ammo, Is.EqualTo(4));
        }

        // ── AC-4: M1 magazine (6 = 3 shots) exhausts then reloads at 3.0s ────────

        [Test]
        public void test_m1_magazine_exhausts_after_three_shots_then_reloads()
        {
            var bus = new RecordingEventBus();
            var parts = new StubPartStateQuery();
            parts.Configure(1, worldPosition: Vector2.zero);
            var m1 = new M1HomingMissile(bus, new StubWeaponTierQuery(), parts, Balance(), M1Def());

            Assert.That(m1.TryFire(1, 0), Is.True, "shot 1");
            Assert.That(m1.TryFire(1, 0), Is.True, "shot 2");
            Assert.That(m1.TryFire(1, 0), Is.True, "shot 3");
            Assert.That(m1.Ammo, Is.EqualTo(0));
            Assert.That(m1.IsReloading, Is.True, "auto-reload starts on empty magazine");
            Assert.That(m1.TryFire(1, 0), Is.False, "4th shot blocked during reload");

            m1.Tick(3.0f);
            Assert.That(m1.IsReloading, Is.False);
            Assert.That(m1.Ammo, Is.EqualTo(6));
            Assert.That(m1.TryFire(1, 0), Is.True);
        }
    }
}
