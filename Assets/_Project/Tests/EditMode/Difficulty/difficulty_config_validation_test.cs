using System;
using System.Linq;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KaijuBreaker.Tests.EditMode.Difficulty
{
    /// <summary>
    /// Difficulty Story 001 — Core types + DifficultyConfig validation (difficulty-system.md §G.1/§G.2/§D.3).
    /// Verifies the Core contract (DifficultyTier enum, IDifficultyProvider read-only surface) and the
    /// Content SO's OnValidate safety assertions (D1 hard gate = LogError; D2–D4 §G.1 bands = LogWarning).
    ///
    /// Reconciliation vs. the story text: the committed <see cref="IDifficultyProvider"/> exposes the
    /// CURRENT tier's multipliers as read-only PROPERTIES (BulletDensityMult / EnemyCountMult) rather than
    /// the story's GetXxxMult(tier) methods. The per-tier lookup the story describes lives on
    /// <see cref="DifficultyConfig.GetEnemyCountMult"/> / GetBulletDensityMult (any tier), which the
    /// concrete DifficultySystem (Story 002) delegates to. Consumers (Stage/BulletSim) only ever need the
    /// current tier, so the interface stays minimal. Tests here assert the committed property surface.
    /// </summary>
    [TestFixture]
    public sealed class DifficultyConfigValidationTests
    {
        private static void InvokeOnValidate(DifficultyConfig config)
        {
            MethodInfo m = typeof(DifficultyConfig).GetMethod(
                "OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "DifficultyConfig should have an editor-time OnValidate.");
            m.Invoke(config, null);
        }

        // ── AC-1: DifficultyTier enum is exactly D1..D4 (0..3) ──────────────────────────────────

        [Test]
        public void test_difficulty_tier_enum_has_exactly_four_ordered_members()
        {
            DifficultyTier[] values = (DifficultyTier[])Enum.GetValues(typeof(DifficultyTier));
            Assert.AreEqual(4, values.Length, "Exactly four tiers D1–D4.");
            Assert.AreEqual(0, (int)DifficultyTier.D1);
            Assert.AreEqual(1, (int)DifficultyTier.D2);
            Assert.AreEqual(2, (int)DifficultyTier.D3);
            Assert.AreEqual(3, (int)DifficultyTier.D4);
            CollectionAssert.AreEqual(
                new[] { "D1", "D2", "D3", "D4" }, Enum.GetNames(typeof(DifficultyTier)),
                "No stray D0/D5 members.");
        }

        // ── AC-2: IDifficultyProvider is read-only (no setters, no write methods) ───────────────

        [Test]
        public void test_difficulty_provider_interface_is_read_only()
        {
            Type t = typeof(IDifficultyProvider);

            PropertyInfo[] props = t.GetProperties();
            Assert.AreEqual(3, props.Length, "CurrentTier + BulletDensityMult + EnemyCountMult only.");
            foreach (PropertyInfo p in props)
                Assert.IsFalse(p.CanWrite, $"{p.Name} must be get-only (no setter).");

            Assert.AreEqual(typeof(DifficultyTier), t.GetProperty("CurrentTier")?.PropertyType);
            Assert.AreEqual(typeof(float), t.GetProperty("BulletDensityMult")?.PropertyType);
            Assert.AreEqual(typeof(float), t.GetProperty("EnemyCountMult")?.PropertyType);

            // No write/mutator methods (only the property getters may exist).
            foreach (MethodInfo m in t.GetMethods())
            {
                Assert.IsFalse(m.Name.StartsWith("set_"), $"No setter {m.Name}.");
                Assert.IsFalse(m.Name.StartsWith("Set"), $"No mutator {m.Name} (e.g. SetTier).");
            }
        }

        // ── AC-3: OnValidate rejects D1 baseline != 1.0 (hard gate = LogError) ──────────────────

        [Test]
        public void test_onvalidate_rejects_d1_enemy_multiplier_not_one()
        {
            var config = ContentTestFactory.Create<DifficultyConfig>(
                ("_enemyCountMult", new[] { 1.1f, 1.25f, 1.50f, 1.75f }));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("EnemyCountMult\\[0\\]"));
            InvokeOnValidate(config);
        }

        [Test]
        public void test_onvalidate_rejects_d1_bullet_multiplier_not_one()
        {
            var config = ContentTestFactory.Create<DifficultyConfig>(
                ("_bulletDensityMult", new[] { 0.9f, 1.25f, 1.50f, 2.00f }));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BulletDensityMult\\[0\\]"));
            InvokeOnValidate(config);
        }

        // ── AC-4: OnValidate accepts the §D.3 defaults (no error, no warning) ───────────────────

        [Test]
        public void test_onvalidate_accepts_gdd_default_values()
        {
            // Factory with no overrides = the SO's own defaults. Count-first model (director 2026-07):
            // enemy {1,1.25,1.5,1.75}, bullet {1,2,3,4} (D1 sparse base, scaled up steeply), cap 20, default D1.
            var config = ContentTestFactory.Create<DifficultyConfig>();
            InvokeOnValidate(config);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void test_onvalidate_accepts_upper_band_edges()
        {
            // Bullet-density D2–D4 at the count-first safe-range ceilings (2.5 / 3.5 / 5.0) and enemy_cap at
            // its floor (15) are accepted with no warning.
            var config = ContentTestFactory.Create<DifficultyConfig>(
                ("_bulletDensityMult", new[] { 1.00f, 2.50f, 3.50f, 5.00f }),
                ("_enemyCapPerScene", 15));
            InvokeOnValidate(config);
            LogAssert.NoUnexpectedReceived();
        }

        // ── AC-5: OnValidate warns on D2–D4 out-of-band (non-blocking) ──────────────────────────

        [Test]
        public void test_onvalidate_warns_on_d4_bullet_multiplier_above_band()
        {
            var config = ContentTestFactory.Create<DifficultyConfig>(
                ("_bulletDensityMult", new[] { 1.00f, 2.00f, 3.00f, 5.01f }));  // > 5.00 ceiling (count-first band)
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("BulletDensityMult\\[D4\\]"));
            InvokeOnValidate(config);
        }

        [Test]
        public void test_onvalidate_warns_on_d4_bullet_multiplier_below_band()
        {
            var config = ContentTestFactory.Create<DifficultyConfig>(
                ("_bulletDensityMult", new[] { 1.00f, 2.00f, 3.00f, 2.49f }));  // < 2.50 floor (count-first band)
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("BulletDensityMult\\[D4\\]"));
            InvokeOnValidate(config);
        }

        // ── AC-6: DifficultyConfig carries no runtime behaviour ─────────────────────────────────

        [Test]
        public void test_difficulty_config_has_no_monobehaviour_hooks()
        {
            Type t = typeof(DifficultyConfig);
            Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(t),
                "DifficultyConfig must be a ScriptableObject, not a MonoBehaviour.");
            Assert.IsFalse(typeof(MonoBehaviour).IsAssignableFrom(t));

            foreach (string hook in new[] { "Update", "Start", "Awake", "FixedUpdate", "LateUpdate" })
                Assert.IsNull(
                    t.GetMethod(hook, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                    $"DifficultyConfig must not define a runtime hook '{hook}'.");
        }
    }
}
