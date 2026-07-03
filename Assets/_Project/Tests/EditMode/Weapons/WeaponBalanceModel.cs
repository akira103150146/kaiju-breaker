using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    /// <summary>
    /// Story 002 H.1/H.2/H.7 — pure-math formula model for the D₀ equal-power verification suite.
    /// Computes each weapon's Sustained_Output (×D₀) and its TTB contribution (T_soften for
    /// lasers, T_break for missiles) directly from <see cref="WeaponDef"/> +
    /// <see cref="WeaponBalanceConfig"/>, per design/gdd/weapon-tiering-and-equal-power.md §D
    /// (D.1 main formula, D.2 continuous lasers, D.3 L3 dual-mode, D.4 discrete missiles,
    /// D.5 M2 Chain Hive). This is the closed-form model the GDD's formulas describe — it does
    /// NOT drive the runtime FireFrame/TryFire behaviour objects in KaijuBreaker.Weapons (those are
    /// exercised separately by weapons_*_family_test.cs / weapons_*_tier3_test.cs).
    ///
    /// Sustained_Output values are dimensionless multiples of D₀ (e.g. 1.00 == exactly D₀), matching
    /// the GDD's "0.975×D₀" notation — the H.1 acceptance criterion "falls within
    /// D0Reference × [1-tol, 1+tol]" is therefore checked against the band [1-tol, 1+tol] directly;
    /// D0Reference cancels out of the ratio (weapon-tiering-and-equal-power.md D.1 "輸出範圍" note).
    ///
    /// L3 Tap mode (D.3) and the L3-T3 resonance-diffusion heat-inject (D.6) are BURST/situational
    /// values excluded from steady-state per the director's I#1 "(c) situational burst value"
    /// ruling — SustainedOutput() below models ONLY the L3 Charge basis. Callers that need the Tap
    /// figure use <see cref="L3TapSustainedOutput"/> separately; the heat-inject value is not
    /// modelled here at all.
    ///
    /// TBreak (the missile half of TTB) reuses the same Sustained_Output → ×BuPerD0 conversion used
    /// for the H.1 equal-power check as the softened-state break rate. This deliberately does NOT
    /// special-case M3's heat-shock 2× detonation bonus (weapons_m3_heat_shock_test.cs /
    /// weapons_m3_gate_validation_test.cs cover that mechanic on its own terms) — folding the
    /// heat-shock burst into the steady-state TTB basis would let M3 dominate the H.2 loadout
    /// matrix and contradict the equal-power premise this suite exists to verify. M3's
    /// Sustained_Output here uses the UNSOFTENED basis, matching the GDD's D.1 worked example.
    /// </summary>
    public static class WeaponBalanceModel
    {
        /// <summary>Floor applied to (H_rate - H_decay_rate) so T_soften never divides by ~0 for a
        /// pathological fixture (weapon-system.md D.4 "max(H_rate - H_decay_rate, ε)").</summary>
        private const float HeatRateEpsilon = 0.01f;

        // ── Sustained_Output (×D₀) — D.1-D.5 ──────────────────────────────────────

        /// <summary>
        /// Sustained_Output (×D₀) for the given weapon at the given tier (0-3). Laser weapons use
        /// the D.2 continuous-rate model (L3 uses the D.3 Charge basis). Missile weapons use the
        /// D.4 discrete-magazine model (M2 uses the D.5 Chain Hive model, tier-invariant by design).
        /// </summary>
        public static float SustainedOutput(WeaponDef w, WeaponBalanceConfig b, int tier)
        {
            switch (w.Id)
            {
                case WeaponId.L1:
                    // C.3 equal-power invariant: per-beam rate = L1HRateFull / beamCount, so the
                    // all-hit total is L1HRateFull regardless of tier/beam count (verified
                    // explicitly via L1BeamCount/L1PerBeamHeatRate below, not just assumed here).
                    return w.L1HRateFull * w.EffectiveHitRate / b.HuPerD0;

                case WeaponId.L2:
                    return w.L2HRate * w.EffectiveHitRate / b.HuPerD0;

                case WeaponId.L3:
                    return L3ChargeSustainedOutput(w);

                case WeaponId.L4:
                    return w.L4HRate * w.EffectiveHitRate / b.HuPerD0;

                case WeaponId.M1:
                    return M1SustainedOutput(w, tier);

                case WeaponId.M2:
                    return M2SustainedOutput(w);

                case WeaponId.M3:
                    return M3SustainedOutput(w);

                case WeaponId.M4:
                    return M4SustainedOutput(w, tier);

                default:
                    throw new ArgumentOutOfRangeException(nameof(w), w.Id, "WeaponBalanceModel: unknown weapon id.");
            }
        }

        /// <summary>L1 beam count at the given tier — G.3 <c>L1BeamCountByTier</c>, expressed as
        /// <c>L1BaseBeamCount + tier</c> (2/3/4/5 at Tier 0-3).</summary>
        public static int L1BeamCount(WeaponDef w, int tier) => w.L1BaseBeamCount + tier;

        /// <summary>Per-beam heat rate at the given tier — <c>L1HRateFull / beamCount</c>
        /// (C.3's "even split" rule). Total across all beams equals L1HRateFull at any tier.</summary>
        public static float L1PerBeamHeatRate(WeaponDef w, int tier) => w.L1HRateFull / L1BeamCount(w, tier);

        /// <summary>L3 Tap-mode Sustained_Output (×D₀) — D.3. Recorded as its own figure; excluded
        /// from the H.1 in-band assertion loop per the GDD's "deliberately weakened filler mode"
        /// note (this value is expected to sit BELOW the equal-power band, by design).</summary>
        public static float L3TapSustainedOutput(WeaponDef w) => w.L3TapOutputMult;

        private static float L3ChargeSustainedOutput(WeaponDef w) =>
            w.L3ChargeOutputMult / (w.L3ChargeTime + w.L3ChargeCooldown);

        // M1 — D.4 discrete-magazine model. ShotCount = MagSize / MissilesPerShot(tier); Tier-3
        // fires fewer, bigger shots from the SAME magazine (total ammo is tier-invariant, D.6).
        private static float M1SustainedOutput(WeaponDef w, int tier)
        {
            int missilesPerShot = tier >= 3 ? w.M1T3MissilesPerShot : w.M1MissilesPerShot;
            int shotCount = w.M1MagSize / missilesPerShot;
            float outputPerShot = missilesPerShot * w.M1DmgPerMissileMult;
            float magDuration = shotCount * w.M1ShotInterval;
            float totalOutputRaw = shotCount * outputPerShot;
            return totalOutputRaw * w.EffectiveHitRate / (magDuration + w.M1ReloadTime);
        }

        // M2 — D.5 Chain Hive model. Tier 0-3 share identical SalvoCount/MicroCount/
        // DmgPerMissileMult/InterSalvoInterval/ReloadTime (Option B), so this is intentionally NOT
        // a function of tier — Sustained_Output is tier-invariant by construction (D.6/H.7 trivial).
        // EffectiveHitRate is NOT applied for M2 (D.5: folded into the per-missile design already).
        private static float M2SustainedOutput(WeaponDef w)
        {
            float totalOutputRaw = w.M2SalvoCount * w.M2MicroCount * w.M2DmgPerMissileMult;
            float magDuration = (w.M2SalvoCount - 1) * w.M2InterSalvoInterval;
            return totalOutputRaw / (magDuration + w.M2ReloadTime);
        }

        // M3 — D.1/D.4 discrete-magazine model, UNSOFTENED basis (the GDD's D.1 worked example is
        // explicitly the "未軟化基礎值" reference point for the equal-power check; the softened
        // heat-shock detonation bonus is a separate mechanic validated in
        // weapons_m3_heat_shock_test.cs / weapons_m3_gate_validation_test.cs, not here).
        private static float M3SustainedOutput(WeaponDef w)
        {
            float magDuration = w.M3MagSize * w.M3ShotInterval;
            float totalOutputRaw = w.M3MagSize * w.M3DmgUnsoftenedMult;
            return totalOutputRaw * w.EffectiveHitRate / (magDuration + w.M3ReloadTime);
        }

        // M4 — D.4 discrete-magazine model (Tier 0-2: shared AoE output cap) / D.6 Tier-3 basis
        // (child-bomb split, uncapped per-cluster total — see D.6 M4 worked example).
        private static float M4SustainedOutput(WeaponDef w, int tier)
        {
            float outputPerShot = tier >= 3
                ? w.M4T3ChildCount * w.M4T3ChildDmgPct
                : w.M4TotalOutputCapMult;
            float magDuration = w.M4MagSize * w.M4ShotInterval;
            float totalOutputRaw = w.M4MagSize * outputPerShot;
            return totalOutputRaw * w.EffectiveHitRate / (magDuration + w.M4ReloadTime);
        }

        // ── TTB — weapon-system.md D.4: TTB = T_soften + T_break ──────────────────

        /// <summary>
        /// This weapon's own contribution to a loadout's TTB on the given part type/tier: a laser
        /// contributes T_soften (time to bring a fresh part up to θ_S); a missile contributes
        /// T_break (time to fill B_max once softened). A full loadout's TTB is the SUM of its
        /// primary's and secondary's <see cref="Ttb"/> — see <see cref="LoadoutTtb"/>.
        /// </summary>
        public static float Ttb(WeaponDef w, WeaponBalanceConfig b, PartType partType, int tier) =>
            w.Type == WeaponType.Laser ? TSoften(w, b, tier) : TBreak(w, b, partType, tier);

        /// <summary>weapon-system.md D.4: <c>T_soften = θ_S / max(H_rate - H_decay_rate, ε)</c>,
        /// where H_rate is this weapon's Sustained_Output expressed back in HU/s.</summary>
        private static float TSoften(WeaponDef w, WeaponBalanceConfig b, int tier)
        {
            float hRateEffective = SustainedOutput(w, b, tier) * b.HuPerD0;
            float netRate = Math.Max(hRateEffective - b.HDecayRate, HeatRateEpsilon);
            return b.ThetaS / netRate;
        }

        /// <summary>weapon-system.md D.4: <c>T_break = B_max / B_rate_softened</c>, where
        /// B_rate_softened is this weapon's Sustained_Output expressed back in BU/s.</summary>
        private static float TBreak(WeaponDef w, WeaponBalanceConfig b, PartType partType, int tier)
        {
            float bMax = RequiredBreakThreshold(b, partType);
            float bRateSoftened = SustainedOutput(w, b, tier) * b.BuPerD0;
            return bMax / bRateSoftened;
        }

        private static float RequiredBreakThreshold(WeaponBalanceConfig b, PartType partType)
        {
            switch (partType)
            {
                case PartType.Normal: return b.RequiredBreakThresholdNormal;
                case PartType.Armored: return b.RequiredBreakThresholdArmored;
                case PartType.BossCore: return b.RequiredBreakThresholdBossCore;
                // MidCore uses Normal heat/break caps unless a part overrides them (PartType.cs).
                case PartType.MidCore: return b.RequiredBreakThresholdNormal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(partType), partType, "WeaponBalanceModel: unknown part type.");
            }
        }

        /// <summary>A full loadout's TTB (weapon-system.md D.4/D.5): the primary (laser) softens,
        /// the secondary (missile) breaks. Used by the H.2 loadout matrix.</summary>
        public static float LoadoutTtb(WeaponDef primary, WeaponDef secondary, WeaponBalanceConfig b,
            PartType partType, int tier) =>
            Ttb(primary, b, partType, tier) + Ttb(secondary, b, partType, tier);
    }
}
