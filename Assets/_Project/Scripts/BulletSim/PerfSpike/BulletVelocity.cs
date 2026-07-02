using Unity.Entities;
using Unity.Mathematics;

namespace KaijuBreaker.BulletSim.PerfSpike
{
    /// <summary>
    /// ADR-0001 spike component — 2D velocity for a stress-test bullet.
    /// THROWAWAY: delete this whole PerfSpike folder once ADR-0001 is decided.
    /// </summary>
    public struct BulletVelocity : IComponentData
    {
        public float2 Value;
    }
}
