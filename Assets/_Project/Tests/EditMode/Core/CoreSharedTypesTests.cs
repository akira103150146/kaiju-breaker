using System;
using NUnit.Framework;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Core
{
    /// <summary>
    /// Story 001 — verifies the shared Core enums exist with exactly their contracted
    /// value sets, so downstream systems (and their tests) can rely on them.
    /// Referencing these types at all also proves Core is consumable across assemblies (AC-4).
    /// </summary>
    public sealed class CoreSharedTypesTests
    {
        [Test]
        public void BreakQuality_HasExactlyThreeValues()
        {
            Assert.AreEqual(3, Enum.GetValues(typeof(BreakQuality)).Length);
            Assert.IsTrue(Enum.IsDefined(typeof(BreakQuality), BreakQuality.Normal));
            Assert.IsTrue(Enum.IsDefined(typeof(BreakQuality), BreakQuality.Softened));
            Assert.IsTrue(Enum.IsDefined(typeof(BreakQuality), BreakQuality.SoftenedStaggered));
        }

        [Test]
        public void RunState_HasExactlyFourStates_InContiguousOrder()
        {
            Assert.AreEqual(4, Enum.GetValues(typeof(RunState)).Length);
            Assert.AreEqual(0, (int)RunState.Loadout);
            Assert.AreEqual(1, (int)RunState.Stage);
            Assert.AreEqual(2, (int)RunState.Boss);
            Assert.AreEqual(3, (int)RunState.Results);
        }

        [Test]
        public void PartType_MatchesGddContract()
        {
            // Normal/Armored/BossCore + MidCore (mid-encounter core, feedback point 7).
            Assert.AreEqual(4, Enum.GetValues(typeof(PartType)).Length);
            Assert.IsTrue(Enum.IsDefined(typeof(PartType), PartType.Normal));
            Assert.IsTrue(Enum.IsDefined(typeof(PartType), PartType.Armored));
            Assert.IsTrue(Enum.IsDefined(typeof(PartType), PartType.BossCore));
            Assert.IsTrue(Enum.IsDefined(typeof(PartType), PartType.MidCore));
        }

        [Test]
        public void WeaponId_HasEightWeaponsAcrossTwoPools()
        {
            Assert.AreEqual(8, Enum.GetValues(typeof(WeaponId)).Length);
            // Primary pool 0..3, secondary pool 4..7.
            Assert.AreEqual(0, (int)WeaponId.L1);
            Assert.AreEqual(4, (int)WeaponId.M1);
            Assert.AreEqual(7, (int)WeaponId.M4);
        }

        [Test]
        public void DifficultyTier_HasFourTiers()
        {
            Assert.AreEqual(4, Enum.GetValues(typeof(DifficultyTier)).Length);
        }

        [Test]
        public void PartStateEnums_HaveExpectedShape()
        {
            Assert.AreEqual(2, Enum.GetValues(typeof(HeatState)).Length);   // Intact, Softened
            Assert.AreEqual(2, Enum.GetValues(typeof(ArmorState)).Length);  // Intact, Stripped
        }
    }
}
