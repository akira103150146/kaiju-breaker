using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Difficulty;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Difficulty
{
    /// <summary>
    /// Difficulty Story 002 — DifficultySystem + runtime multiplier application (difficulty-system.md
    /// §H.1, §C.1, §D.1, §D.2). Verifies the enemy-count and bullet-density scaling matrices (the
    /// numbers Stage/BulletSim will produce), the run-lock (§E.1 mid-run change forbidden), and the
    /// remember-last-difficulty start-tier resolution (§G.2, config-flag driven).
    ///
    /// The scaling itself lives in the pure <see cref="DifficultyScaling"/> helper (single source of the
    /// ceil/cap formula); DifficultySystem supplies the tier's multiplier + the cap. Testing them together
    /// mirrors the real call site (Stage reads IDifficultyProvider → feeds DifficultyScaling).
    /// </summary>
    [TestFixture]
    public sealed class DifficultySystemTests
    {
        // Default multipliers, asserted directly so a config regression is caught here too. Bullet density is the
        // count-first model (director 2026-07): D1..D4 = 1/2/3/4 (sparse D1 base scaled up steeply).
        private const float D2Enemy = 1.25f, D3Enemy = 1.50f, D4Enemy = 1.75f;
        private const float D4Bullet = 4.00f;

        private static DifficultySystem MakeSystem() =>
            new DifficultySystem(ContentTestFactory.Create<DifficultyConfig>());

        // ── AC-1 / H.1: enemy-count matrix (base × tier), cap = 20 ──────────────────────────────
        // base=1 →1/2/2/2 · base=5 →5/7/8/9 · base=11 →11/14/17/20(cap)

        [TestCase(1, DifficultyTier.D1, 1)]
        [TestCase(1, DifficultyTier.D2, 2)]
        [TestCase(1, DifficultyTier.D3, 2)]
        [TestCase(1, DifficultyTier.D4, 2)]
        [TestCase(5, DifficultyTier.D1, 5)]
        [TestCase(5, DifficultyTier.D2, 7)]
        [TestCase(5, DifficultyTier.D3, 8)]
        [TestCase(5, DifficultyTier.D4, 9)]
        [TestCase(11, DifficultyTier.D1, 11)]
        [TestCase(11, DifficultyTier.D2, 14)]
        [TestCase(11, DifficultyTier.D3, 17)]
        [TestCase(11, DifficultyTier.D4, 20)]  // ceil(19.25)=20, == cap
        public void test_scaled_enemy_count_matches_gdd_matrix(int baseCount, DifficultyTier tier, int expected)
        {
            var sys = MakeSystem();
            sys.SetTier(tier);
            int actual = DifficultyScaling.ScaledEnemyCount(baseCount, sys.EnemyCountMult, sys.EnemyCapPerScene);
            Assert.AreEqual(expected, actual);
        }

        // ── AC-2 / H.1: bullet-density matrix (base × tier), no cap ─────────────────────────────
        // Count-first mults 1/2/3/4: base=1 →1/2/3/4 · base=5 →5/10/15/20 · base=8 →8/16/24/32

        [TestCase(1, DifficultyTier.D1, 1)]
        [TestCase(1, DifficultyTier.D2, 2)]
        [TestCase(1, DifficultyTier.D3, 3)]
        [TestCase(1, DifficultyTier.D4, 4)]
        [TestCase(5, DifficultyTier.D1, 5)]
        [TestCase(5, DifficultyTier.D2, 10)]
        [TestCase(5, DifficultyTier.D3, 15)]
        [TestCase(5, DifficultyTier.D4, 20)]
        [TestCase(8, DifficultyTier.D1, 8)]
        [TestCase(8, DifficultyTier.D2, 16)]
        [TestCase(8, DifficultyTier.D3, 24)]
        [TestCase(8, DifficultyTier.D4, 32)]
        public void test_scaled_bullet_count_matches_gdd_matrix(int baseBullets, DifficultyTier tier, int expected)
        {
            var sys = MakeSystem();
            sys.SetTier(tier);
            int actual = DifficultyScaling.ScaledBulletCount(baseBullets, sys.BulletDensityMult);
            Assert.AreEqual(expected, actual);
        }

        // ── Edge cases: empty wave, cap enforcement ─────────────────────────────────────────────

        [Test]
        public void test_zero_base_count_stays_zero()
        {
            var sys = MakeSystem();
            sys.SetTier(DifficultyTier.D4);
            // 0 enemies at any tier is still 0 — we never fabricate a phantom enemy (reconciliation
            // vs. story's ">=1" edge note: an empty wave spawns nothing).
            Assert.AreEqual(0, DifficultyScaling.ScaledEnemyCount(0, sys.EnemyCountMult, sys.EnemyCapPerScene));
            Assert.AreEqual(0, DifficultyScaling.ScaledBulletCount(0, sys.BulletDensityMult));
        }

        [Test]
        public void test_enemy_count_never_exceeds_cap()
        {
            var sys = MakeSystem();  // cap = 20
            sys.SetTier(DifficultyTier.D1);  // even at ×1.0, a huge base is capped
            Assert.AreEqual(20, DifficultyScaling.ScaledEnemyCount(100, sys.EnemyCountMult, sys.EnemyCapPerScene));
        }

        // ── AC-3: run-lock freezes the tier (§E.1) ──────────────────────────────────────────────

        [Test]
        public void test_lock_freezes_tier_and_multiplier()
        {
            var sys = MakeSystem();
            sys.SetTier(DifficultyTier.D2);
            sys.LockForRun();

            bool changed = sys.SetTier(DifficultyTier.D4);

            Assert.IsFalse(changed, "SetTier must no-op while locked.");
            Assert.AreEqual(DifficultyTier.D2, sys.CurrentTier);
            Assert.AreEqual(D2Enemy, sys.EnemyCountMult, "Multiplier still reflects the locked tier.");
        }

        [Test]
        public void test_unlock_allows_tier_change_again()
        {
            var sys = MakeSystem();
            sys.SetTier(DifficultyTier.D2);
            sys.LockForRun();
            sys.UnlockForRunEnd();

            bool changed = sys.SetTier(DifficultyTier.D4);

            Assert.IsTrue(changed);
            Assert.AreEqual(DifficultyTier.D4, sys.CurrentTier);
            Assert.AreEqual(D4Enemy, sys.EnemyCountMult);
        }

        [Test]
        public void test_current_tier_multiplier_properties_follow_selected_tier()
        {
            var sys = MakeSystem();
            sys.SetTier(DifficultyTier.D4);
            Assert.AreEqual(D4Enemy, sys.EnemyCountMult);
            Assert.AreEqual(D4Bullet, sys.BulletDensityMult);
            Assert.AreEqual(sys.GetEnemyCountMult(DifficultyTier.D3), D3Enemy);
        }

        // ── AC-4: remember-last-difficulty start-tier resolution (§G.2) ─────────────────────────

        [Test]
        public void test_start_tier_uses_remembered_value_when_flag_enabled()
        {
            var config = ContentTestFactory.Create<DifficultyConfig>();  // remember = true (default)
            Assert.AreEqual(DifficultyTier.D3, DifficultySystem.ResolveStartTier(config, DifficultyTier.D3));
        }

        [Test]
        public void test_start_tier_falls_back_to_default_on_fresh_save()
        {
            var config = ContentTestFactory.Create<DifficultyConfig>();  // default = D1
            Assert.AreEqual(DifficultyTier.D1, DifficultySystem.ResolveStartTier(config, null));
        }

        [Test]
        public void test_start_tier_ignores_remembered_value_when_flag_disabled()
        {
            var config = ContentTestFactory.Create<DifficultyConfig>(("_rememberLastDifficulty", false));
            Assert.AreEqual(DifficultyTier.D1, DifficultySystem.ResolveStartTier(config, DifficultyTier.D3));
        }

        [Test]
        public void test_initialize_start_tier_applies_remembered_tier()
        {
            var sys = MakeSystem();
            sys.InitializeStartTier(DifficultyTier.D3);
            Assert.AreEqual(DifficultyTier.D3, sys.CurrentTier);
        }

        [Test]
        public void test_initialize_start_tier_noops_while_locked()
        {
            var sys = MakeSystem();
            sys.SetTier(DifficultyTier.D2);
            sys.LockForRun();
            bool applied = sys.InitializeStartTier(DifficultyTier.D4);
            Assert.IsFalse(applied);
            Assert.AreEqual(DifficultyTier.D2, sys.CurrentTier);
        }
    }
}
