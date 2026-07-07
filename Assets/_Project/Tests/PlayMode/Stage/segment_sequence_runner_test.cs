using System.Collections;
using System.Collections.Generic;
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
    /// Stage last-mile (2) — <see cref="SegmentSequenceRunner"/> drives a run's segments through the
    /// WaveSpawner in turn: spawn a segment, advance once it's spawned AND cleared, fire the completion
    /// callback after the last (which the director turns into the pre-boss lull). PlayMode (real Instantiate +
    /// MonoBehaviour lifecycle); enemies "die" by being destroyed to advance the sequence.
    /// </summary>
    [TestFixture]
    public sealed class SegmentSequenceRunnerTests
    {
        private GameObject _template;
        private GameObject _runnerGo;
        private readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _template = new GameObject("EnemyTemplate");
            _template.AddComponent<SpriteRenderer>();
            _template.AddComponent<EnemyController>();
            _template.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_runnerGo != null) Object.DestroyImmediate(_runnerGo);
            if (_template != null) Object.DestroyImmediate(_template);
            foreach (var a in _assets) if (a != null) Object.DestroyImmediate(a);
            _assets.Clear();
            foreach (var ec in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                if (ec != null) Object.DestroyImmediate(ec.gameObject);
        }

        private sealed class FakeDifficulty : IDifficultyProvider
        {
            public DifficultyTier CurrentTier => DifficultyTier.D1;
            public float BulletDensityMult => 1f;
            public float EnemyCountMult => 1f;
        }

        private static void Set(object o, string f, object v) =>
            o.GetType().GetField(f, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, v);

        private T Asset<T>() where T : ScriptableObject { var so = ScriptableObject.CreateInstance<T>(); _assets.Add(so); return so; }

        private EnemyDef Enemy(string id) { var e = Asset<EnemyDef>(); Set(e, "_enemyId", id); return e; }

        private SegmentDef Seg(string id)
        {
            var s = Asset<SegmentDef>();
            Set(s, "_segmentId", id);
            Set(s, "_difficultyWeight", 1);
            Set(s, "_waveCount", 1);
            Set(s, "_enemyPool", new[] { Enemy("ram_grub") });
            return s;
        }

        private WaveTimingConfig Timing()
        {
            var t = Asset<WaveTimingConfig>();
            Set(t, "_enemiesPerWaveBase", 2);
            Set(t, "_waveIntervalSeconds", 0.05f);
            Set(t, "_defaultLayout", SpawnLayout.HorizontalSpread);
            Set(t, "_fieldWidth", 8f); Set(t, "_spawnY", 6f); Set(t, "_columnSpacing", 1.2f);
            return t;
        }

        private static IEnumerator SpawnFrames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator test_runner_spawns_each_segment_then_completes()
        {
            var seq = new SegmentSequence(null, new List<SegmentDef> { Seg("s1_01"), Seg("s1_02") }, null, "carapex");
            _runnerGo = new GameObject("Runner");
            var runner = _runnerGo.AddComponent<SegmentSequenceRunner>();

            bool completed = false;
            runner.Run(seq, new FakeDifficulty(), Timing(), _template, new Random(1), () => completed = true);

            // Segment 0 spawns its enemies.
            yield return SpawnFrames(3);
            Assert.AreEqual(0, runner.CurrentSegmentIndex, "on the first segment");
            Assert.Greater(_runnerGo.transform.childCount, 0, "segment 0 spawned enemies");

            // Clear segment 0 → runner advances to segment 1.
            ClearEnemies();
            yield return SpawnFrames(3);
            Assert.AreEqual(1, runner.CurrentSegmentIndex, "advanced to segment 1 after clearing segment 0");
            Assert.Greater(_runnerGo.transform.childCount, 0, "segment 1 spawned enemies");

            // Clear segment 1 → sequence complete.
            ClearEnemies();
            yield return SpawnFrames(3);
            Assert.IsTrue(completed, "onSequenceComplete fired after the last segment");
            Assert.IsFalse(runner.IsRunning);
        }

        private void ClearEnemies()
        {
            foreach (var ec in _runnerGo.GetComponentsInChildren<EnemyController>())
                Object.DestroyImmediate(ec.gameObject);
        }
    }
}
