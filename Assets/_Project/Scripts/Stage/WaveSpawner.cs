using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Runtime driver that instantiates a segment's enemies from a <see cref="WavePlanner"/> plan
    /// (stage-system.md §D.2), one WAVE at a time with CLEAR-GATED pacing: a wave's enemies spawn with a small
    /// intra-wave stagger, then the next wave is held until the field is mostly clear (alive ≤ threshold) OR an
    /// anti-stall cap elapses — never sooner than a minimum gap. This stops a fresh wave from stacking on top of
    /// one the player hasn't killed yet (the old fixed-cadence spawner did exactly that). Difficulty scaling /
    /// layout / elite selection all live in the (pure, tested) planner.
    ///
    /// <para>MVP uses <see cref="Object.Instantiate"/>; a pooled path is a profiling follow-up
    /// (control-manifest guardrail).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaveSpawner : MonoBehaviour
    {
        private List<List<WaveSpawnInstruction>> _waves;
        private WaveTimingConfig _timing;
        private int _currentWave;
        private int _spawnedInWave;
        private float _waveElapsed;         // seconds since the current wave started
        private float _sinceFullySpawned;   // seconds since the current wave finished spawning (−1 until it does)
        private bool _running;
        private bool _lastWaveSpawned;
        private int _plannedCount;
        private GameObject _enemyPrefab;
        private Transform _parent;
        private EnemyCombatContext _context;

        /// <summary>Every enemy spawned so far this segment (in spawn order).</summary>
        public List<EnemyController> Spawned { get; } = new List<EnemyController>();

        /// <summary>Total spawn instructions the current plan holds.</summary>
        public int PlannedCount => _plannedCount;

        /// <summary>True once every planned wave has fully spawned (the segment can then clear).</summary>
        public bool IsComplete => _lastWaveSpawned;

        /// <summary>Number of currently-alive spawned enemies (active + not destroyed).</summary>
        public int AliveCount()
        {
            int n = 0;
            for (int i = 0; i < Spawned.Count; i++)
            {
                var e = Spawned[i];
                if (e != null && e.gameObject.activeSelf) n++;
            }
            return n;
        }

        /// <summary>
        /// Build the spawn plan for <paramref name="segment"/> and prepare to run. Injected deps keep this
        /// testable (fake difficulty, seeded rng). Call <see cref="Begin"/> to start the timed spawns.
        /// </summary>
        public void Configure(SegmentDef segment, WaveTimingConfig timing, IDifficultyProvider difficulty,
                              GameObject enemyPrefab, System.Random rng, Transform parent,
                              EnemyCombatContext context = null)
        {
            _timing = timing;
            _enemyPrefab = enemyPrefab;
            _parent = parent;
            _context = context;

            var flat = new WavePlanner(timing, difficulty, rng).Plan(segment);
            _plannedCount = flat.Count;
            int waveCount = 0;
            for (int i = 0; i < flat.Count; i++) waveCount = Mathf.Max(waveCount, flat[i].WaveIndex + 1);
            _waves = new List<List<WaveSpawnInstruction>>(waveCount);
            for (int w = 0; w < waveCount; w++) _waves.Add(new List<WaveSpawnInstruction>());
            for (int i = 0; i < flat.Count; i++) _waves[flat[i].WaveIndex].Add(flat[i]);

            _currentWave = 0;
            _spawnedInWave = 0;
            _waveElapsed = 0f;
            _sinceFullySpawned = -1f;
            _running = false;
            _lastWaveSpawned = waveCount == 0; // an empty plan is trivially complete
            Spawned.Clear();
        }

        /// <summary>Start the timed, clear-gated spawn loop.</summary>
        public void Begin() => _running = true;

        private void Update()
        {
            if (!_running || _waves == null || _currentWave >= _waves.Count) return;
            float dt = Time.deltaTime;
            _waveElapsed += dt;

            var wave = _waves[_currentWave];
            // Spawn this wave's due enemies (stagger is measured from the wave's own start).
            while (_spawnedInWave < wave.Count && wave[_spawnedInWave].SpawnTime <= _waveElapsed)
            {
                SpawnOne(wave[_spawnedInWave]);
                _spawnedInWave++;
            }
            if (_spawnedInWave < wave.Count) return; // still spawning the current wave

            // Current wave fully spawned — track how long we've been waiting to release the next one.
            _sinceFullySpawned = _sinceFullySpawned < 0f ? 0f : _sinceFullySpawned + dt;

            if (_currentWave >= _waves.Count - 1) { _lastWaveSpawned = true; _running = false; return; }

            // Release the next wave once the field is mostly clear OR we've waited too long (anti-stall), but
            // never before the minimum gap — so waves never stack on an unkilled one, nor machine-gun out.
            if (WavePacing.ShouldReleaseNextWave(AliveCount(), _timing.NextWaveAliveThreshold, _sinceFullySpawned,
                                                 _timing.MaxWaveWaitSeconds, _waveElapsed, _timing.MinWaveGapSeconds))
            {
                _currentWave++;
                _spawnedInWave = 0;
                _waveElapsed = 0f;
                _sinceFullySpawned = -1f;
            }
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
                controller.SetCombatContext(_context?.BulletPool, _context?.PlayerTarget, _context?.OnEnemyKilled,
                                            _context?.BulletDensityMult ?? 1f);
                Spawned.Add(controller);
            }
        }
    }
}
