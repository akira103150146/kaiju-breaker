using NUnit.Framework;
using UnityEngine;
using KaijuBreaker.Core;
using KaijuBreaker.Content;
using KaijuBreaker.Tests.EditMode.Helpers;

namespace KaijuBreaker.Tests.EditMode.Content
{
    /// <summary>
    /// per-part-firing-schema.md §1–§6 — verifies the additive per-part firing/movement schema:
    /// new enum values exist, the economy theme→core map covers all 8 themes (the fix for the
    /// added KaijuTheme values), and the new PartDef/KaijuDef fields default to inert values so
    /// existing assets and prior tests are unaffected. Struct fields round-trip via JsonUtility
    /// (which honours [SerializeField] private fields).
    /// </summary>
    public sealed class PartSchemaExtensionTests
    {
        // ── §1 New enum values exist ────────────────────────────────────────────────
        [Test]
        public void KaijuTheme_HasFiveNewThemes()
        {
            Assert.AreEqual(3, (int)KaijuTheme.Swarm);
            Assert.AreEqual(4, (int)KaijuTheme.Crystal);
            Assert.AreEqual(5, (int)KaijuTheme.Abyss);
            Assert.AreEqual(6, (int)KaijuTheme.Ember);
            Assert.AreEqual(7, (int)KaijuTheme.Void);
        }

        [Test]
        public void MaterialId_HasFiveNewCores()
        {
            Assert.AreEqual(5, (int)MaterialId.CoreSwarm);
            Assert.AreEqual(6, (int)MaterialId.CoreCrystal);
            Assert.AreEqual(7, (int)MaterialId.CoreAbyss);
            Assert.AreEqual(8, (int)MaterialId.CoreEmber);
            Assert.AreEqual(9, (int)MaterialId.CoreVoid);
        }

        [Test]
        public void EmitterPatternType_HasSpiral()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(EmitterPatternType), EmitterPatternType.Spiral));
        }

        [Test]
        public void MovementType_HasDiveSwoopAndHoverStrafe()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(MovementType), MovementType.DiveSwoop));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MovementType), MovementType.HoverStrafe));
        }

        [Test]
        public void EnemyTier_HasFourTiers()
        {
            Assert.AreEqual(0, (int)EnemyTier.Trash);
            Assert.AreEqual(1, (int)EnemyTier.Elite);
            Assert.AreEqual(2, (int)EnemyTier.Mid);
            Assert.AreEqual(3, (int)EnemyTier.Boss);
        }

        // ── §1.2 Economy theme→core map covers all 8 themes (the fix) ────────────────
        [Test]
        public void EconomyConfig_GetCoreForTheme_CoversAllEightThemes()
        {
            var econ = ContentTestFactory.Create<EconomyConfig>();
            Assert.AreEqual(MaterialId.CoreCarapace, econ.GetCoreForTheme(KaijuTheme.Carapace));
            Assert.AreEqual(MaterialId.CoreLimb, econ.GetCoreForTheme(KaijuTheme.Limb));
            Assert.AreEqual(MaterialId.CoreEnergy, econ.GetCoreForTheme(KaijuTheme.Energy));
            // The new five must NOT throw (the agent added the enum values; this asserts the map fix).
            Assert.AreEqual(MaterialId.CoreSwarm, econ.GetCoreForTheme(KaijuTheme.Swarm));
            Assert.AreEqual(MaterialId.CoreCrystal, econ.GetCoreForTheme(KaijuTheme.Crystal));
            Assert.AreEqual(MaterialId.CoreAbyss, econ.GetCoreForTheme(KaijuTheme.Abyss));
            Assert.AreEqual(MaterialId.CoreEmber, econ.GetCoreForTheme(KaijuTheme.Ember));
            Assert.AreEqual(MaterialId.CoreVoid, econ.GetCoreForTheme(KaijuTheme.Void));
        }

        // ── §2–§6 Backward-compat defaults (existing assets unaffected) ─────────────
        [Test]
        public void PartDef_Defaults_AreInert()
        {
            var part = new PartDef();
            Assert.IsNotNull(part.Emitters);
            Assert.AreEqual(0, part.Emitters.Length, "no emitters by default");
            Assert.AreEqual(PartMovementType.None, part.Movement.Type, "static by default");
            Assert.IsFalse(part.ArmorRegen.Enabled, "no regen by default");
            Assert.AreEqual(PartGateKind.None, part.GateKind, "ungated by default");
            Assert.IsNotNull(part.GatePartIds);
            Assert.AreEqual(0, part.GatePartIds.Length);
        }

        [Test]
        public void KaijuDef_Body_DefaultsToNoMotion()
        {
            var kaiju = ContentTestFactory.Create<KaijuDef>();
            Assert.AreEqual(0f, kaiju.Body.DriftAmpX, 0.0001f);
            Assert.AreEqual(0f, kaiju.Body.DriftAmpY, 0.0001f);
        }

        // ── §2–§5 Struct fields round-trip (JsonUtility honours [SerializeField]) ────
        [Test]
        public void PartMovement_Orbit_RoundTrips()
        {
            var m = JsonUtility.FromJson<PartMovement>(
                "{\"_type\":1,\"_radiusWorld\":2.5,\"_angularSpeedDeg\":15,\"_phaseDeg\":90}");
            Assert.AreEqual(PartMovementType.Orbit, m.Type);
            Assert.AreEqual(2.5f, m.RadiusWorld, 0.0001f);
            Assert.AreEqual(15f, m.AngularSpeedDeg, 0.0001f);
            Assert.AreEqual(90f, m.PhaseDeg, 0.0001f);
        }

        [Test]
        public void PartEmitter_SpawnerFlag_ReflectsSpawnEnemyId()
        {
            var firing = JsonUtility.FromJson<PartEmitter>(
                "{\"_gate\":0,\"_spawnEnemyId\":\"\",\"_spawnCap\":0}");
            Assert.IsFalse(firing.IsSpawner, "empty spawn id = bullet emitter, not a spawner");

            var spawner = JsonUtility.FromJson<PartEmitter>(
                "{\"_gate\":3,\"_gatePartId\":\"chitin_veil\",\"_spawnEnemyId\":\"spore_mite\",\"_spawnCap\":4}");
            Assert.IsTrue(spawner.IsSpawner);
            Assert.AreEqual(PartFireGate.RequireGatePartBroken, spawner.Gate);
            Assert.AreEqual("chitin_veil", spawner.GatePartId);
            Assert.AreEqual("spore_mite", spawner.SpawnEnemyId);
            Assert.AreEqual(4, spawner.SpawnCap);
        }

        [Test]
        public void ArmorRegen_RoundTrips()
        {
            var r = JsonUtility.FromJson<ArmorRegen>(
                "{\"_enabled\":true,\"_graceSeconds\":5.0,\"_regenRatePerSec\":6.0}");
            Assert.IsTrue(r.Enabled);
            Assert.AreEqual(5.0f, r.GraceSeconds, 0.0001f);
            Assert.AreEqual(6.0f, r.RegenRatePerSec, 0.0001f);
        }

        // ── §1.5 EnemyDef.Tier ──────────────────────────────────────────────────────
        [Test]
        public void EnemyDef_Tier_DefaultsToTrash_AndOverrides()
        {
            var trash = ContentTestFactory.Create<EnemyDef>();
            Assert.AreEqual(EnemyTier.Trash, trash.Tier);

            var elite = ContentTestFactory.Create<EnemyDef>(
                ("_tier", EnemyTier.Elite), ("_isElite", true));
            Assert.AreEqual(EnemyTier.Elite, elite.Tier);
            Assert.IsTrue(elite.IsElite);
        }
    }
}
