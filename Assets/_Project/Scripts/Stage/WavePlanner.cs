using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>One concrete enemy spawn: which enemy, where, when (offset from segment start), elite or not.</summary>
    public readonly struct WaveSpawnInstruction
    {
        public readonly EnemyDef Enemy;
        public readonly Vector2 Position;
        public readonly float SpawnTime;
        public readonly bool IsElite;

        public WaveSpawnInstruction(EnemyDef enemy, Vector2 position, float spawnTime, bool isElite)
        {
            Enemy = enemy;
            Position = position;
            SpawnTime = spawnTime;
            IsElite = isElite;
        }
    }

    /// <summary>
    /// Turns a <see cref="SegmentDef"/> (enemy pool + wave count + elite index) into a deterministic, ordered
    /// list of <see cref="WaveSpawnInstruction"/> — the pool-model wave build (stage-system.md §D.2). Pure C#
    /// (no Instantiate), so the difficulty-scaled counts, timing offsets, layout, and elite flagging are all
    /// EditMode-testable with a seeded RNG. The runtime <c>WaveSpawner</c> (a follow-up, needs enemy prefabs +
    /// PlayMode) consumes this plan and instantiates; enemy bullet emission is blocked by ADR-0001.
    ///
    /// <para><b>Reconciliation:</b> the committed <see cref="SegmentDef"/> uses the pool model (WHAT can
    /// appear), not the story's authored <c>waves[]</c>. Per-wave count/pacing/layout come from
    /// <see cref="WaveTimingConfig"/> (ADR-0003), so no behaviour/numbers live on the segment data.</para>
    /// </summary>
    public sealed class WavePlanner
    {
        private readonly WaveTimingConfig _timing;
        private readonly IDifficultyProvider _difficulty;
        private readonly System.Random _rng;

        public WavePlanner(WaveTimingConfig timing, IDifficultyProvider difficulty, System.Random rng)
        {
            _timing = timing ?? throw new ArgumentNullException(nameof(timing));
            _difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        /// <summary>
        /// Build the full spawn plan for <paramref name="segment"/>. Per wave: count =
        /// <c>ceil(EnemiesPerWaveBase × EnemyCountMult)</c> (difficulty-scaled, §L.3); enemies sampled from the
        /// segment pool; positions from <see cref="SpawnLayoutHelper"/>; spawn time = waveIndex × interval; one
        /// elite seeded on the segment's elite wave (<see cref="SegmentDef.EliteWaveIndex"/>, −1 = none).
        /// </summary>
        public List<WaveSpawnInstruction> Plan(SegmentDef segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            var plan = new List<WaveSpawnInstruction>();

            EnemyDef[] pool = segment.EnemyPool ?? Array.Empty<EnemyDef>();
            if (pool.Length == 0) return plan; // nothing to spawn (guarded, no crash)

            float mult = _difficulty.EnemyCountMult;

            for (int wave = 0; wave < segment.WaveCount; wave++)
            {
                int count = Mathf.CeilToInt(_timing.EnemiesPerWaveBase * mult);
                float spawnTime = wave * _timing.WaveIntervalSeconds;
                bool isEliteWave = wave == segment.EliteWaveIndex;

                Vector2[] positions = SpawnLayoutHelper.Positions(
                    _timing.DefaultLayout, count, _timing.FieldWidth, _timing.SpawnY, _timing.ColumnSpacing);

                for (int i = 0; i < count; i++)
                {
                    EnemyDef enemy = pool[_rng.Next(pool.Length)];
                    bool isElite = isEliteWave && i == 0; // one elite leads an elite wave (Story 004 details)
                    plan.Add(new WaveSpawnInstruction(enemy, positions[i], spawnTime, isElite));
                }
            }
            return plan;
        }
    }
}
