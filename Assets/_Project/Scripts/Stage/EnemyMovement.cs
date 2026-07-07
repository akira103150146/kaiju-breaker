using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>Per-instance mutable state an enemy carries between <see cref="EnemyMovement.Advance"/> calls.</summary>
    public struct EnemyMovementState
    {
        public bool Init;
        public float SpawnX;
        public float Elapsed;
        public bool HoverReached;
        public bool UTurnReached;
    }

    /// <summary>
    /// Executes a <see cref="MovementPatternSO"/> as concrete per-frame motion — the five frozen archetypes
    /// (StraightRush / HorizontalDrift / Hover / UTurn / Sinusoidal), each with a visibly distinct entrance and
    /// path (stage-system.md §E.1–E.2). Pure math (takes/returns a position + a small state struct, no Unity
    /// transform or Time), so movement is EditMode-testable and difficulty-invariant (§I.2 — speed never scales
    /// with difficulty). SO speeds are authored in the 320px design space; <see cref="PxToWorld"/> maps that to
    /// the world (320px design width ↔ 8 world units), so the numbers stay re-tunable placeholders.
    /// </summary>
    public static class EnemyMovement
    {
        /// <summary>Design-pixels → world-units (320px design width ↔ 8 world units).</summary>
        public const float PxToWorld = 0.025f;

        /// <summary>Y the Hover archetype settles at (≈30% down from the top band).</summary>
        public const float HoverStationY = 2.6f;

        /// <summary>Y the UTurn archetype reverses at.</summary>
        public const float UTurnReverseY = 0.5f;

        /// <summary>
        /// Advance <paramref name="pos"/> by one step of <paramref name="m"/>'s pattern. <paramref name="st"/>
        /// self-initialises on first call (captures the entry X). A null pattern falls back to a plain descent.
        /// </summary>
        public static Vector2 Advance(Vector2 pos, MovementPatternSO m, ref EnemyMovementState st, float dt)
        {
            if (!st.Init) { st.SpawnX = pos.x; st.Init = true; }
            st.Elapsed += dt;

            float speed = (m != null ? m.MoveSpeedPxPerSec : 100f) * PxToWorld;
            if (m == null) { pos.y -= speed * dt; return pos; }

            switch (m.MovementType)
            {
                case MovementType.StraightRush:
                {
                    // Fast dive; the intro multiplier eases the first moment of entry (stage-system.md §E.1).
                    float s = speed * (st.Elapsed < 0.8f ? m.IntroSpeedMult : 1f);
                    pos.y -= s * dt;
                    break;
                }
                case MovementType.HorizontalDrift:
                {
                    // Slow drift in with a gentle downward creep and a lateral sway.
                    pos.y -= speed * 0.45f * dt;
                    pos.x += Mathf.Sin(st.Elapsed * 1.6f) * speed * dt;
                    break;
                }
                case MovementType.Hover:
                {
                    // Descend to a station line, then hold (a stationary shooter).
                    if (!st.HoverReached && pos.y > HoverStationY) pos.y -= speed * dt;
                    else st.HoverReached = true;
                    break;
                }
                case MovementType.UTurn:
                {
                    // Descend, then reverse into an upward exit arc.
                    if (!st.UTurnReached && pos.y > UTurnReverseY) pos.y -= speed * dt;
                    else { st.UTurnReached = true; pos.y += speed * 0.8f * dt; pos.x += speed * 0.3f * dt; }
                    break;
                }
                case MovementType.Sinusoidal:
                {
                    // Weave laterally about the entry X while descending.
                    pos.y -= speed * dt;
                    float amp = m.AmplitudePx * PxToWorld;
                    pos.x = st.SpawnX + Mathf.Sin(st.Elapsed * 2f * Mathf.PI * m.FrequencyHz) * amp;
                    break;
                }
            }
            return pos;
        }
    }
}
