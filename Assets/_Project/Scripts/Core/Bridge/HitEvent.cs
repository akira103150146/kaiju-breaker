using UnityEngine;

namespace KaijuBreaker.Core
{
    /// <summary>What a queued bullet-collision hit resolves to when the bridge republishes it.</summary>
    public enum HitKind
    {
        /// <summary>A pooled player missile hit a kaiju part → republished as <see cref="MissileHit"/>.</summary>
        MissileHitPart = 0,

        /// <summary>An enemy bullet hit the player ship → republished as <see cref="PlayerHit"/>.</summary>
        PlayerHit = 1
    }

    /// <summary>
    /// A single bullet-collision result produced inside the ECS/Burst bullet simulation
    /// and queued (NativeQueue&lt;HitEvent&gt;) for the main-thread bridge to drain (ADR-0002 §4).
    /// Deliberately UNMANAGED (only blittable fields) so it is Burst/NativeContainer-safe.
    /// Lives in Core (no DOTS dependency) so both BulletSim and the bridge share one contract.
    /// </summary>
    public readonly struct HitEvent
    {
        public readonly HitKind Kind;
        public readonly int PartId;      // valid when Kind == MissileHitPart
        public readonly int KaijuId;     // valid when Kind == MissileHitPart
        public readonly WeaponId Weapon; // the firing player weapon (missile hits)
        public readonly float BreakDeltaBase;
        public readonly Vector2 Position;

        public HitEvent(HitKind kind, int partId, int kaijuId, WeaponId weapon, float breakDeltaBase, Vector2 position)
        {
            Kind = kind;
            PartId = partId;
            KaijuId = kaijuId;
            Weapon = weapon;
            BreakDeltaBase = breakDeltaBase;
            Position = position;
        }
    }
}
