using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Shared spec-default WeaponDef + WeaponBalanceConfig fixtures for the closed-form
    /// <c>WeaponBalanceModel</c> suites (weapon-tiering-and-equal-power.md §D knob values).
    /// A single source of truth for the H.2 loadout matrix (Weapons epic) and the economy
    /// anti-dominant-loadout guard (Economy Story 005) so both assert against identical inputs —
    /// change a knob here and BOTH suites re-run against it (story 005 DRY mandate).
    /// </summary>
    public static class WeaponBalanceFixtures
    {
        public static WeaponBalanceConfig Balance() => ContentTestFactory.Create<WeaponBalanceConfig>(
            ("_d0Reference", 100f), ("_buPerD0", 10f), ("_huPerD0", 25f),
            ("_hDecayRate", 3f), ("_thetaS", 100f),
            ("_requiredBreakThresholdNormal", 100f), ("_requiredBreakThresholdArmored", 150f),
            ("_requiredBreakThresholdBossCore", 200f),
            ("_equalPowerBandTolerance", 0.10f));

        public static WeaponDef L1() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.L1), ("_type", WeaponType.Laser),
            ("_l1HRateFull", 25f), ("_l1BaseBeamCount", 2), ("_effectiveHitRate", 1.0f));

        public static WeaponDef L2() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.L2), ("_type", WeaponType.Laser),
            ("_l2HRate", 37.5f), ("_effectiveHitRate", 0.65f));

        public static WeaponDef L3() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.L3), ("_type", WeaponType.Laser),
            ("_l3TapOutputMult", 0.60f),
            ("_l3ChargeTime", 1.2f), ("_l3ChargeOutputMult", 2.50f), ("_l3ChargeCooldown", 1.5f),
            ("_effectiveHitRate", 1.0f));

        public static WeaponDef L4() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.L4), ("_type", WeaponType.Laser),
            ("_l4HRate", 25f), ("_effectiveHitRate", 1.0f));

        public static WeaponDef M1() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M1), ("_type", WeaponType.Missile),
            ("_m1MissilesPerShot", 2), ("_m1DmgPerMissileMult", 0.50f),
            ("_m1MagSize", 6), ("_m1ReloadTime", 3.0f), ("_m1T3MissilesPerShot", 3),
            ("_m1ShotInterval", 0.10f), ("_effectiveHitRate", 1.0f));

        public static WeaponDef M2() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M2), ("_type", WeaponType.Missile),
            ("_m2MicroCount", 8), ("_m2SalvoCount", 3), ("_m2DmgPerMissileMult", 0.25f),
            ("_m2InterSalvoInterval", 0.4f), ("_m2ReloadTime", 5.0f), ("_effectiveHitRate", 1.0f));

        public static WeaponDef M3() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M3), ("_type", WeaponType.Missile),
            ("_m3DmgUnsoftenedMult", 3.0f), ("_m3MagSize", 3), ("_m3ReloadTime", 4.0f),
            ("_m3ShotInterval", 1.0f), ("_effectiveHitRate", 0.80f));

        public static WeaponDef M4() => ContentTestFactory.Create<WeaponDef>(
            ("_id", WeaponId.M4), ("_type", WeaponType.Missile),
            ("_m4TotalOutputCapMult", 1.0f), ("_m4MagSize", 4), ("_m4ReloadTime", 3.5f),
            ("_m4ShotInterval", 0.20f), ("_m4T3ChildCount", 6),
            ("_m4T3ChildDmgPct", 0.18f), ("_effectiveHitRate", 1.0f));

        /// <summary>The 4 primary-pool lasers (L1-L4).</summary>
        public static List<WeaponDef> Primaries() => new List<WeaponDef> { L1(), L2(), L3(), L4() };

        /// <summary>The 4 secondary-pool missiles (M1-M4).</summary>
        public static List<WeaponDef> Secondaries() => new List<WeaponDef> { M1(), M2(), M3(), M4() };

        /// <summary>All 8 weapons.</summary>
        public static List<WeaponDef> All()
        {
            var list = Primaries();
            list.AddRange(Secondaries());
            return list;
        }
    }
}
