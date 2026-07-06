using System.Collections;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Random = System.Random;

namespace KaijuBreaker.Tests.PlayMode.Stage
{
    /// <summary>
    /// Stage Story 002 (integration) — WaveSpawner actually instantiates enemies from the planner and wires
    /// each EnemyController with its EnemyDef + movement/emitter SOs (stage-system.md §D.2). PlayMode because
    /// it exercises real Instantiate + MonoBehaviour lifecycle. Uses a runtime-built template GameObject as
    /// the "prefab" (no AssetDatabase — the asmdef is cross-platform) and reflection-built SO fixtures.
    ///
    /// <para>Enemy bullet emission is out of scope (ADR-0001); this proves spawn counts, SO wiring, elite
    /// flag, difficulty scaling, and layout positions on real GameObjects.</para>
    /// </summary>
    [TestFixture]
    public sealed class EnemySpawningTests
    {
        private GameObject _template;
        private GameObject _spawnerGo;

        [SetUp]
        public void SetUp()
        {
            _template = new GameObject("EnemyTemplate");
            _template.AddComponent<SpriteRenderer>();
            _template.AddComponent<EnemyController>();
            _template.SetActive(false); // inert template; Instantiated copies are what spawn
        }

        [TearDown]
        public void TearDown()
        {
            if (_spawnerGo != null) Object.DestroyImmediate(_spawnerGo);
            if (_template != null) Object.DestroyImmediate(_template);
            // clean up any spawned enemies left in the scene
            foreach (var ec in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                if (ec != null) Object.DestroyImmediate(ec.gameObject);
        }

        // ── Fixtures (reflection — the PlayMode asmdef can't see the EditMode helpers) ──

        private static void Set(object o, string field, object val) =>
            o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, val);

        private sealed class FakeDifficulty : IDifficultyProvider
        {
            public DifficultyTier CurrentTier { get; set; } = DifficultyTier.D1;
            public float BulletDensityMult { get; set; } = 1f;
            public float EnemyCountMult { get; set; } = 1f;
        }

        private static EnemyDef Enemy(string id)
        {
            var def = ScriptableObject.CreateInstance<EnemyDef>();
            Set(def, "_enemyId", id);
            Set(def, "_hpTier", HpTier.T1);
            Set(def, "_movementPattern", ScriptableObject.CreateInstance<MovementPatternSO>());
            Set(def, "_emitterPattern", ScriptableObject.CreateInstance<EmitterPatternSO>());
            return def;
        }

        private static WaveTimingConfig Timing(int baseCount, float interval = 0.05f)
        {
            var t = ScriptableObject.CreateInstance<WaveTimingConfig>();
            Set(t, "_enemiesPerWaveBase", baseCount);
            Set(t, "_waveIntervalSeconds", interval);
            Set(t, "_defaultLayout", SpawnLayout.HorizontalSpread);
            Set(t, "_fieldWidth", 8f);
            Set(t, "_spawnY", 6f);
            Set(t, "_columnSpacing", 1.2f);
            return t;
        }

        private static SegmentDef Segment(EnemyDef[] pool, int waveCount, int eliteWaveIndex)
        {
            var s = ScriptableObject.CreateInstance<SegmentDef>();
            Set(s, "_segmentId", "s1_01");
            Set(s, "_difficultyWeight", 1);
            Set(s, "_enemyPool", pool);
            Set(s, "_waveCount", waveCount);
            Set(s, "_eliteWaveIndex", eliteWaveIndex);
            return s;
        }

        private WaveSpawner MakeSpawner(SegmentDef seg, WaveTimingConfig timing, FakeDifficulty diff)
        {
            _spawnerGo = new GameObject("WaveSpawner");
            var spawner = _spawnerGo.AddComponent<WaveSpawner>();
            spawner.Configure(seg, timing, diff, _template, new Random(1), null);
            spawner.Begin();
            return spawner;
        }

        private static IEnumerator RunUntilComplete(WaveSpawner spawner, int maxFrames = 240)
        {
            for (int i = 0; i < maxFrames && !spawner.IsComplete; i++) yield return null;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator test_spawns_planned_count_and_wires_sos()
        {
            var pool = new[] { Enemy("ram_grub"), Enemy("tri_shot") };
            var spawner = MakeSpawner(Segment(pool, waveCount: 2, eliteWaveIndex: 1), Timing(3), new FakeDifficulty());

            Assert.AreEqual(6, spawner.PlannedCount, "2 waves × 3 at D1");
            yield return RunUntilComplete(spawner);

            Assert.IsTrue(spawner.IsComplete, "all waves spawned within the frame budget");
            Assert.AreEqual(6, spawner.Spawned.Count);
            foreach (var ec in spawner.Spawned)
            {
                Assert.IsNotNull(ec.Def, "EnemyDef wired");
                Assert.IsNotNull(ec.Movement, "MovementPatternSO wired");
                Assert.IsNotNull(ec.Emitter, "EmitterPatternSO wired");
            }
        }

        [UnityTest]
        public IEnumerator test_difficulty_multiplier_scales_spawn_count()
        {
            var pool = new[] { Enemy("ram_grub") };
            var spawner = MakeSpawner(Segment(pool, waveCount: 2, eliteWaveIndex: -1),
                                      Timing(4), new FakeDifficulty { EnemyCountMult = 1.5f });

            yield return RunUntilComplete(spawner);
            Assert.AreEqual(12, spawner.Spawned.Count, "ceil(4×1.5)=6 per wave × 2 = 12");
        }

        [UnityTest]
        public IEnumerator test_one_elite_spawned_on_elite_wave()
        {
            var pool = new[] { Enemy("ram_grub"), Enemy("tri_shot") };
            var spawner = MakeSpawner(Segment(pool, waveCount: 2, eliteWaveIndex: 1), Timing(3), new FakeDifficulty());

            yield return RunUntilComplete(spawner);
            int elites = 0;
            foreach (var ec in spawner.Spawned) if (ec.IsElite) elites++;
            Assert.AreEqual(1, elites, "exactly one elite instance across the segment");
        }

        [UnityTest]
        public IEnumerator test_spawn_positions_within_field()
        {
            var pool = new[] { Enemy("ram_grub") };
            var spawner = MakeSpawner(Segment(pool, waveCount: 1, eliteWaveIndex: -1), Timing(5), new FakeDifficulty());

            // Capture spawn X before the placeholder descent moves them far (Y only changes).
            yield return RunUntilComplete(spawner);
            Assert.AreEqual(5, spawner.Spawned.Count);
            foreach (var ec in spawner.Spawned)
                Assert.LessOrEqual(Mathf.Abs(ec.transform.position.x), 4.001f, "spawn x within ±fieldWidth/2");
        }
    }
}
