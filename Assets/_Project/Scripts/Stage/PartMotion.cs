using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Pure math for boss-part idle motion (per-part-firing-schema.md §2). Given a <see cref="PartMovement"/>,
    /// the part's static base local position (relative to the kaiju root) and an elapsed time, returns the
    /// animated local position + a Z rotation. No Unity transform / Time access so BossController stays thin and
    /// this is EditMode-testable. None (default) returns the base position unchanged.
    /// </summary>
    public static class PartMotion
    {
        /// <summary>Animated local position (relative to kaiju root) for the part at elapsed time <paramref name="t"/>.</summary>
        public static Vector2 LocalPosition(PartMovement m, Vector2 baseLocal, float t)
        {
            switch (m.Type)
            {
                case PartMovementType.Orbit:
                {
                    float ang = (m.PhaseDeg + m.AngularSpeedDeg * t) * Mathf.Deg2Rad;
                    return m.PivotOffset + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * m.RadiusWorld;
                }
                case PartMovementType.SweepArc:
                case PartMovementType.Oscillate:
                {
                    // Hinge the base position about the pivot by an oscillating angle.
                    float sweepDeg = m.ArcHalfDeg * Mathf.Sin((m.PhaseDeg + m.AngularSpeedDeg * t) * Mathf.Deg2Rad);
                    Vector2 rel = baseLocal - m.PivotOffset;
                    float r = sweepDeg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
                    return m.PivotOffset + new Vector2(rel.x * c - rel.y * s, rel.x * s + rel.y * c);
                }
                default: // None, Spin — position unchanged (Spin rotates the sprite instead)
                    return baseLocal;
            }
        }

        /// <summary>Z-axis sprite rotation (deg) for the part at elapsed time <paramref name="t"/>.</summary>
        public static float ZRotationDeg(PartMovement m, float t)
        {
            switch (m.Type)
            {
                case PartMovementType.Spin:
                    return m.PhaseDeg + m.AngularSpeedDeg * t;
                case PartMovementType.SweepArc:
                case PartMovementType.Oscillate:
                    return m.ArcHalfDeg * Mathf.Sin((m.PhaseDeg + m.AngularSpeedDeg * t) * Mathf.Deg2Rad);
                default:
                    return 0f;
            }
        }
    }
}
