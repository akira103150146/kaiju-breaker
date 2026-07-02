using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaijuBreaker.BulletSim.PerfSpike
{
    /// <summary>
    /// ADR-0001 spike: spawns <see cref="BulletPerfSpikeConfig.Count"/> bullets ONCE, entirely from code —
    /// no prefab, no SubScene, no Baker. Runs automatically in the default World the moment you enter
    /// Play mode in ANY scene, then disables itself.
    ///
    /// Bullets fan out evenly in a ring so they never overlap-cull; the move job wraps them at the bounds.
    /// This is a pure-simulation test: it measures per-frame GC and job throughput, NOT render cost.
    /// See README.md in this folder for how to add rendering and how to read the Profiler.
    ///
    /// THROWAWAY: delete this whole PerfSpike folder once ADR-0001 is decided.
    /// </summary>
    public partial struct BulletSpawnSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Spawn exactly once, then stop this system from running again.
            state.Enabled = false;
            if (!BulletPerfSpikeConfig.Enabled)
                return;

            int count = BulletPerfSpikeConfig.Count;
            float speed = BulletPerfSpikeConfig.Speed;

            var em = state.EntityManager;
            var archetype = em.CreateArchetype(typeof(LocalTransform), typeof(BulletVelocity));

            // Batch-create all entities in one call (no per-entity managed allocation).
            var entities = em.CreateEntity(archetype, count, Allocator.Temp);
            for (int i = 0; i < count; i++)
            {
                float ang = math.radians(i * (360f / count));
                em.SetComponentData(entities[i], LocalTransform.FromPosition(0f, 0f, 0f));
                em.SetComponentData(entities[i], new BulletVelocity
                {
                    Value = new float2(math.cos(ang), math.sin(ang)) * speed
                });
            }
            entities.Dispose();
        }
    }
}
