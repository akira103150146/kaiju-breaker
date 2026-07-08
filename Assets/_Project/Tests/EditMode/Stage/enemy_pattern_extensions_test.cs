using KaijuBreaker.Content;
using KaijuBreaker.Stage;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// per-part-firing-schema.md §1.3/§1.4 (task #6a) — the new emitter/movement archetypes that both the
    /// expanded mob roster and boss parts use: EmitterPatternType.Spiral (a phase-rotated ring) and
    /// MovementType.DiveSwoop / HoverStrafe. Pure-math EditMode coverage, difficulty-invariant speeds.
    /// </summary>
    public sealed class EnemyPatternExtensionsTests
    {
        // ── Spiral emission ─────────────────────────────────────────────────────────
        [Test]
        public void Spiral_EmitsEvenRing_AtZeroPhase()
        {
            var v = EnemyEmission.Velocities(EmitterPatternType.Spiral, 6, 0f, 2f, Vector2.down, spinPhaseDeg: 0f);
            Assert.AreEqual(6, v.Length);
            foreach (var vel in v) Assert.AreEqual(2f, vel.magnitude, 0.001f, "speed preserved");
            // First arm at 0° = (2,0).
            Assert.AreEqual(2f, v[0].x, 0.001f);
            Assert.AreEqual(0f, v[0].y, 0.001f);
        }

        [Test]
        public void Spiral_PhaseRotatesTheRing()
        {
            var v = EnemyEmission.Velocities(EmitterPatternType.Spiral, 4, 0f, 1f, Vector2.down, spinPhaseDeg: 90f);
            // First arm rotated to 90° = (0,1).
            Assert.AreEqual(0f, v[0].x, 0.001f);
            Assert.AreEqual(1f, v[0].y, 0.001f);
        }

        // ── DiveSwoop movement ──────────────────────────────────────────────────────
        [Test]
        public void DiveSwoop_DescendsAndCurvesAway()
        {
            var m = ContentTestFactory.Create<MovementPatternSO>(
                ("_movementType", MovementType.DiveSwoop),
                ("_moveSpeedPxPerSec", 200f),
                ("_entryAngleDeg", 35f));

            var st = new EnemyMovementState();
            var pos = new Vector2(0f, 6f); // spawnX = 0 -> sweeps to +x
            for (int i = 0; i < 60; i++) pos = EnemyMovement.Advance(pos, m, ref st, 1f / 60f);

            Assert.Less(pos.y, 6f, "descended");
            Assert.Greater(pos.x, 0f, "curved away from entry X toward +x");
        }

        // ── HoverStrafe movement ────────────────────────────────────────────────────
        [Test]
        public void HoverStrafe_SettlesThenStrafesAboutEntryX()
        {
            var m = ContentTestFactory.Create<MovementPatternSO>(
                ("_movementType", MovementType.HoverStrafe),
                ("_moveSpeedPxPerSec", 300f),
                ("_strafeHalfWidthPx", 100f)); // amp = 100 * 0.025 = 2.5 world

            var st = new EnemyMovementState();
            var pos = new Vector2(0f, 6f);
            float maxAbsX = 0f;
            for (int i = 0; i < 240; i++)
            {
                pos = EnemyMovement.Advance(pos, m, ref st, 1f / 60f);
                maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(pos.x));
            }

            Assert.IsTrue(st.HoverReached, "reached the hover station");
            Assert.LessOrEqual(pos.y, EnemyMovement.HoverStationY + 0.05f, "held at the station line");
            Assert.Greater(maxAbsX, 1.0f, "strafed laterally");
            Assert.LessOrEqual(maxAbsX, 2.6f, "strafe stayed within amplitude");
        }
    }
}
