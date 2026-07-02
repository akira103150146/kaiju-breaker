using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaijuBreaker.BulletSim.PerfSpike
{
    /// <summary>
    /// ADR-0001 spike: advances every bullet each frame on worker threads via Burst + IJobEntity.
    /// The whole point of this system is that it allocates ZERO managed memory per frame — verify that
    /// in the Profiler's "GC Alloc" column (should read 0 B while this runs).
    ///
    /// THROWAWAY: delete this whole PerfSpike folder once ADR-0001 is decided.
    /// </summary>
    [BurstCompile]
    public partial struct BulletMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new BulletMoveJob
            {
                Dt = SystemAPI.Time.DeltaTime,
                Bound = BulletPerfSpikeConfig.Bound
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct BulletMoveJob : IJobEntity
    {
        public float Dt;
        public float Bound;

        private void Execute(ref LocalTransform transform, in BulletVelocity velocity)
        {
            float3 p = transform.Position;
            p.x += velocity.Value.x * Dt;
            p.y += velocity.Value.y * Dt;

            // Wrap around so bullets stay near the origin for the entire test run.
            if (p.x > Bound) p.x = -Bound;
            else if (p.x < -Bound) p.x = Bound;
            if (p.y > Bound) p.y = -Bound;
            else if (p.y < -Bound) p.y = Bound;

            transform.Position = p;
        }
    }
}
