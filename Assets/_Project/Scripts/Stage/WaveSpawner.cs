using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Runtime driver that instantiates a segment's enemies from a <see cref="WavePlanner"/> plan
    /// (stage-system.md §D.2). It owns only the spawn side: it turns the deterministic
    /// <see cref="WaveSpawnInstruction"/> list into actual enemy GameObjects at their planned positions and
    /// time offsets, injecting each spawned <see cref="EnemyController"/> with its <see cref="EnemyDef"/> +
    /// pattern SOs. Difficulty scaling / layout / elite selection all live in the (pure, tested) planner.
    ///
    /// <para>MVP uses <see cref="Object.Instantiate"/>; a pooled path is a profiling follow-up
    /// (control-manifest guardrail). Enemy bullet emission stays blocked by ADR-0001.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaveSpawner : MonoBehaviour
    {
        private List<WaveSpawnInstruction> _plan;
        private int _next;
        private float _elapsed;
        private bool _running;
        private GameObject _enemyPrefab;
        private Transform _parent;

        /// <summary>Every enemy spawned so far this segment (in spawn order).</summary>
        public List<EnemyController> Spawned { get; } = new List<EnemyController>();

        /// <summary>Total spawn instructions the current plan holds.</summary>
        public int PlannedCount => _plan?.Count ?? 0;

        /// <summary>True once every planned instruction has spawned.</summary>
        public bool IsComplete => _plan != null && _next >= _plan.Count;

        /// <summary>
        /// Build the spawn plan for <paramref name="segment"/> and prepare to run. Injected deps keep this
        /// testable (fake difficulty, seeded rng). Call <see cref="Begin"/> to start the timed spawns.
        /// </summary>
        public void Configure(SegmentDef segment, WaveTimingConfig timing, IDifficultyProvider difficulty,
                              GameObject enemyPrefab, System.Random rng, Transform parent)
        {
            _enemyPrefab = enemyPrefab;
            _parent = parent;
            _plan = new WavePlanner(timing, difficulty, rng).Plan(segment);
            _next = 0;
            _elapsed = 0f;
            _running = false;
            Spawned.Clear();
        }

        /// <summary>Start the timed spawn loop (instructions fire as their SpawnTime is reached).</summary>
        public void Begin() => _running = true;

        private void Update()
        {
            if (!_running || _plan == null) return;

            _elapsed += Time.deltaTime;
            while (_next < _plan.Count && _plan[_next].SpawnTime <= _elapsed)
            {
                SpawnOne(_plan[_next]);
                _next++;
            }
            if (_next >= _plan.Count) _running = false;
        }

        private void SpawnOne(WaveSpawnInstruction instruction)
        {
            if (_enemyPrefab == null) return; // guarded: missing prefab → no crash, spawns nothing
            var go = Object.Instantiate(
                _enemyPrefab, new Vector3(instruction.Position.x, instruction.Position.y, 0f),
                Quaternion.identity, _parent);
            go.SetActive(true); // a spawned enemy is always live (the prefab/template may be inactive)

            var controller = go.GetComponent<EnemyController>();
            if (controller != null)
            {
                controller.Init(instruction.Enemy, instruction.IsElite);
                Spawned.Add(controller);
            }
        }
    }
}
