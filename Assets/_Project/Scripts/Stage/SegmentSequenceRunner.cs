using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Runs a whole run's escalating segments through a <see cref="WaveSpawner"/> in turn (stage-system.md
    /// §D). Given a <see cref="SegmentSequence"/> (from <see cref="StageDirector"/>), it spawns each segment's
    /// enemies, advances to the next once the current segment is spawned AND cleared (its enemies gone), and
    /// fires <c>onSequenceComplete</c> after the last one — which the director turns into the pre-boss lull.
    /// The per-segment spawning + difficulty scaling + layout are the (tested) <see cref="WaveSpawner"/> /
    /// <see cref="WavePlanner"/>; this is the scene-side glue that drives them across the sequence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SegmentSequenceRunner : MonoBehaviour
    {
        private IReadOnlyList<SegmentDef> _segments;
        private IDifficultyProvider _difficulty;
        private WaveTimingConfig _timing;
        private GameObject _enemyPrefab;
        private System.Random _rng;
        private Action _onSequenceComplete;
        private EnemyCombatContext _context;

        private WaveSpawner _spawner;
        private int _index = -1;
        private bool _running;

        /// <summary>Index of the segment currently spawning (−1 before start).</summary>
        public int CurrentSegmentIndex => _index;

        /// <summary>True while spawning through the sequence.</summary>
        public bool IsRunning => _running;

        /// <summary>
        /// Begin running <paramref name="sequence"/>'s escalating segments. <paramref name="onSequenceComplete"/>
        /// fires after the final segment clears (wire it to <see cref="StageDirector.NotifyLastSegmentEnded"/>).
        /// </summary>
        public void Run(SegmentSequence sequence, IDifficultyProvider difficulty, WaveTimingConfig timing,
                        GameObject enemyPrefab, System.Random rng, Action onSequenceComplete,
                        EnemyCombatContext context = null)
        {
            _segments = sequence != null ? sequence.EscalatingSegments : new List<SegmentDef>();
            _difficulty = difficulty;
            _timing = timing;
            _enemyPrefab = enemyPrefab;
            _rng = rng ?? new System.Random();
            _onSequenceComplete = onSequenceComplete;
            _context = context;

            _spawner = GetComponent<WaveSpawner>();
            if (_spawner == null) _spawner = gameObject.AddComponent<WaveSpawner>();

            _index = -1;
            _running = true;
            AdvanceSegment();
        }

        private void AdvanceSegment()
        {
            _index++;
            if (_index >= _segments.Count)
            {
                _running = false;
                _onSequenceComplete?.Invoke();
                return;
            }
            _spawner.Configure(_segments[_index], _timing, _difficulty, _enemyPrefab, _rng, transform, _context);
            _spawner.Begin();
        }

        private void Update()
        {
            if (!_running || _spawner == null) return;
            // A segment is done when everything spawned and the field is clear (enemies killed or gone).
            if (_spawner.IsComplete && AllCleared()) AdvanceSegment();
        }

        private bool AllCleared()
        {
            var spawned = _spawner.Spawned;
            for (int i = 0; i < spawned.Count; i++)
            {
                var e = spawned[i];
                // Cleared once an enemy is destroyed (null) OR deactivated — a kill and an off-bottom escape
                // both deactivate it, so the segment can advance instead of waiting on stragglers forever.
                if (e != null && e.gameObject.activeSelf) return false;
            }
            return true;
        }
    }
}
