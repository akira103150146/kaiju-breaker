using KaijuBreaker.Stage;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Clear-gated wave pacing (<see cref="WavePacing.ShouldReleaseNextWave"/>): the next 道中 wave must not stack
    /// on an unkilled one. Regression for the old fixed-6s cadence that spawned a fresh wave on top of survivors.
    /// A wave releases only once the field is mostly clear OR after an anti-stall wait, and never before a
    /// minimum gap.
    /// </summary>
    [TestFixture]
    public sealed class WavePacingTests
    {
        // thresh=2 alive, maxWait=8s, minGap=2.2s — the shipped defaults.
        private static bool Gate(int alive, float sinceFullySpawned, float waveElapsed) =>
            WavePacing.ShouldReleaseNextWave(alive, aliveThreshold: 2, sinceFullySpawned, maxWaitSeconds: 8f,
                                             waveElapsed, minGapSeconds: 2.2f);

        [Test]
        public void test_pacing_holds_next_wave_while_field_is_crowded()
        {
            // 6 alive, well past the min gap, not waited too long → still held (this is the anti-stacking fix).
            Assert.IsFalse(Gate(alive: 6, sinceFullySpawned: 3f, waveElapsed: 5f));
        }

        [Test]
        public void test_pacing_releases_when_field_mostly_clear()
        {
            Assert.IsTrue(Gate(alive: 1, sinceFullySpawned: 1f, waveElapsed: 3f), "alive ≤ threshold releases");
        }

        [Test]
        public void test_pacing_never_releases_before_min_gap_even_if_clear()
        {
            Assert.IsFalse(Gate(alive: 0, sinceFullySpawned: 1f, waveElapsed: 1.0f), "before the 2.2s floor → held");
        }

        [Test]
        public void test_pacing_anti_stall_releases_after_max_wait_even_if_crowded()
        {
            // A straggler stuck at an edge keeps 5 alive, but we have waited past maxWait → release anyway.
            Assert.IsTrue(Gate(alive: 5, sinceFullySpawned: 8.5f, waveElapsed: 9f));
        }
    }
}
