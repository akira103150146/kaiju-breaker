namespace KaijuBreaker.Core
{
    // Weapon → KaijuParts hit events. Emitted by Weapons (and by the BulletSim
    // bridge for pooled missiles); consumed by KaijuParts to fill heat / break bars.
    // Payloads mirror weapon-system.md F.1. IDs are ints (stable, zero-GC); the
    // ScriptableObject/content layer maps its string ids to these ints at load.

    /// <summary>on_laser_hit — a laser tick deposited <see cref="HeatDelta"/> heat on a part.</summary>
    public readonly struct LaserHit : IGameEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public readonly float HeatDelta;

        public LaserHit(int partId, int kaijuId, float heatDelta)
        {
            PartId = partId;
            KaijuId = kaijuId;
            HeatDelta = heatDelta;
        }
    }

    /// <summary>
    /// on_missile_hit — a missile deposited base break damage on a part.
    /// KaijuParts applies the softened/armor multipliers; it does NOT come pre-multiplied.
    /// </summary>
    public readonly struct MissileHit : IGameEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;
        public readonly float BreakDeltaBase;
        public readonly WeaponId Weapon;

        public MissileHit(int partId, int kaijuId, float breakDeltaBase, WeaponId weapon)
        {
            PartId = partId;
            KaijuId = kaijuId;
            BreakDeltaBase = breakDeltaBase;
            Weapon = weapon;
        }
    }

    /// <summary>on_l3_wave_hit — an L3 Wave Cannon charged shockwave struck a part (armor-strip / stagger trigger).</summary>
    public readonly struct WaveHit : IGameEvent
    {
        public readonly int PartId;
        public readonly int KaijuId;

        public WaveHit(int partId, int kaijuId)
        {
            PartId = partId;
            KaijuId = kaijuId;
        }
    }
}
