using KaijuBreaker.Stage;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Timed, clear-gated wave pacing (<see cref="WavePacing.Decide"/>): each wave has a time limit. Clearing it
    /// releases the next wave immediately; running out the limit with enemies still alive orders the leftovers to
    /// RETREAT off the top, and the next wave only enters once they have cleared — a fresh wave never stacks on an
    /// unkilled one. A minimum gap floors how fast consecutive waves start.
    /// </summary>
    [TestFixture]
    public sealed class WavePacingTests
    {
        // thresh=2 alive, minGap=2.2s, timeLimit=12s — the shipped defaults.
        private static WaveAdvance Gate(int alive, float waveElapsed, bool retreating = false) =>
            WavePacing.Decide(alive, aliveThreshold: 2, waveElapsed, minGapSeconds: 2.2f,
                              timeLimitSeconds: 12f, retreating);

        [Test]
        public void test_pacing_holds_next_wave_while_field_is_crowded_and_within_time_limit()
        {
            // 6 alive, past the min gap, well within the time limit → still held (the anti-stacking rule).
            Assert.AreEqual(WaveAdvance.Hold, Gate(alive: 6, waveElapsed: 5f));
        }

        [Test]
        public void test_pacing_releases_when_field_mostly_clear()
        {
            Assert.AreEqual(WaveAdvance.ReleaseNext, Gate(alive: 1, waveElapsed: 3f), "alive ≤ threshold releases");
        }

        [Test]
        public void test_pacing_never_releases_before_min_gap_even_if_clear()
        {
            Assert.AreEqual(WaveAdvance.Hold, Gate(alive: 0, waveElapsed: 1.0f), "before the 2.2s floor → held");
        }

        [Test]
        public void test_pacing_retreats_leftovers_when_time_limit_expires_and_field_crowded()
        {
            // Time is up but 5 enemies remain → order the leftovers to retreat (they leave before the next wave).
            Assert.AreEqual(WaveAdvance.RetreatLeftovers, Gate(alive: 5, waveElapsed: 12.5f));
        }

        [Test]
        public void test_pacing_holds_while_leftovers_still_retreating()
        {
            // Already retreating and some are still on the field → keep holding the next wave.
            Assert.AreEqual(WaveAdvance.Hold, Gate(alive: 3, waveElapsed: 14f, retreating: true));
        }

        [Test]
        public void test_pacing_releases_next_wave_once_retreat_has_cleared_the_field()
        {
            // Retreat finished (field empty) → release the next wave.
            Assert.AreEqual(WaveAdvance.ReleaseNext, Gate(alive: 0, waveElapsed: 15f, retreating: true));
        }

        [Test]
        public void test_pacing_clearing_before_time_limit_wins_over_retreat()
        {
            // Cleared to threshold just before the limit → next wave now, no retreat needed.
            Assert.AreEqual(WaveAdvance.ReleaseNext, Gate(alive: 2, waveElapsed: 11.9f));
        }
    }
}
