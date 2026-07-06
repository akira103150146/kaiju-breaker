using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using KaijuBreaker.Content;
using KaijuBreaker.Meta;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KaijuBreaker.Tests.EditMode.Meta
{
    /// <summary>
    /// Meta Story 001 — SaveData schema + canonical JSON serializer + CRC32 (meta-progression-system.md
    /// §C.3/§D.2; ADR-0004). Verifies the byte-exact canonical form the integrity hash depends on.
    ///
    /// <para><b>Reconciliations vs story text (surfaced for review):</b>
    /// (1) <c>ICanonicalSerializer</c> lives in <c>KaijuBreaker.Meta</c>, not Core — it references
    /// <c>SaveData</c> (a Meta type) and Core is the zero-dependency base (ADR-0005). (2) The story's
    /// <c>ISaveService</c> read-query surface is NOT (re)declared here: the committed <c>ISaveService</c>
    /// (from the economy epic) already has a different surface used by Economy; extending it belongs to the
    /// Meta service implementation (Story 006), not this schema/serializer story. (3) Canonical floats use
    /// invariant round-trip ("R"), so integral values render as "1" (not "1.0").</para>
    /// </summary>
    [TestFixture]
    public sealed class SaveSchemaSerializerTests
    {
        private CanonicalJsonSerializer _ser;

        [SetUp]
        public void SetUp() => _ser = new CanonicalJsonSerializer();

        // ── Fixtures ──────────────────────────────────────────────────────────

        /// <summary>Minimal SaveData with a known, hand-verifiable canonical form (AC-6 reference).</summary>
        private static SaveData MinimalSave()
        {
            var d = new SaveData { Version = 1, IntegrityHash = string.Empty };
            d.Weapons["L1"] = new WeaponSaveData(2, true);
            d.Materials["core_carapace"] = 5;
            return d; // Meta/Settings/Stats keep their defaults
        }

        /// <summary>Fully-populated SaveData exercising nested maps + null best-times (AC-5).</summary>
        private static SaveData FullSave()
        {
            var d = new SaveData { Version = 1, IntegrityHash = "A4B3C2D1" };
            d.Weapons["L1"] = new WeaponSaveData(2, true);
            d.Weapons["M1"] = new WeaponSaveData(1, true);
            d.Weapons["L3"] = new WeaponSaveData(0, false);
            d.Materials["shard_common"] = 72;
            d.Materials["core_carapace"] = 5;
            d.Materials["core_energy"] = 0;

            var carapex = new KaijuRecordData { FullClearCount = 2 };
            carapex.PartsEverBroken.AddRange(new[] { "normal_claw_left", "armored_dorsal_cannon", "boss_core" });
            carapex.HuntCountPerDifficulty["D1"] = 5;
            carapex.HuntCountPerDifficulty["D2"] = 1;
            carapex.BestTimePerDifficulty["D1"] = 185.3f;
            carapex.BestTimePerDifficulty["D2"] = null;
            carapex.BestTimePerDifficulty["D3"] = null;
            carapex.BestTimePerDifficulty["D4"] = null;
            d.KaijuRecords["CARAPEX"] = carapex;

            d.Meta.LastSelectedDifficulty = "D2";
            d.Meta.LastLoadout = new LoadoutData("L1", "M1");
            d.Meta.FirstLaunchComplete = true;
            d.Settings.TextScale = 1.25f;
            d.Settings.ColorblindMode = "blue_yellow";
            d.Stats.TotalRunsStarted = 12;
            d.Stats.TotalPartsBroken = 87;
            return d;
        }

        // ── AC-6 / AC-1: canonical byte-for-byte reference (keys sorted at every level) ──

        [Test]
        public void test_serialize_minimal_matches_canonical_reference()
        {
            const string expected =
                "{\"integrity_hash\":\"\",\"kaiju_records\":{},\"materials\":{\"core_carapace\":5}," +
                "\"meta\":{\"first_launch_complete\":false,\"last_loadout\":{\"primary\":\"L1\",\"secondary\":\"M1\"}," +
                "\"last_selected_difficulty\":\"D1\"}," +
                "\"settings\":{\"bgm_volume\":1,\"colorblind_mode\":\"default\",\"reduce_motion\":false," +
                "\"sfx_volume\":1,\"text_scale\":1}," +
                "\"stats\":{\"total_full_clears\":0,\"total_parts_broken\":0,\"total_play_time_seconds\":0," +
                "\"total_runs_completed\":0,\"total_runs_started\":0}," +
                "\"version\":1,\"weapons\":{\"L1\":{\"owned\":true,\"tier\":2}}}";

            Assert.AreEqual(expected, _ser.Serialize(MinimalSave()));
        }

        [Test]
        public void test_serialize_keys_are_sorted_at_every_nesting_level()
        {
            string json = _ser.Serialize(FullSave());

            // Spot-check a few nested objects: keys must appear in ascending ordinal order.
            AssertKeyOrder(json, "\"owned\":", "\"tier\":");                                   // weapons[id]
            AssertKeyOrder(json, "\"first_launch_complete\":", "\"last_loadout\":");           // meta
            AssertKeyOrder(json, "\"last_loadout\":", "\"last_selected_difficulty\":");        // meta
            AssertKeyOrder(json, "\"bgm_volume\":", "\"colorblind_mode\":");                   // settings
            AssertKeyOrder(json, "\"full_clear_count\":", "\"hunt_count_per_difficulty\":");   // kaiju_records[id]
            Assert.IsFalse(json.Contains(" "), "canonical form must contain no whitespace");
        }

        // ── AC-2: determinism ─────────────────────────────────────────────────

        [Test]
        public void test_serialize_is_deterministic_across_calls_and_instances()
        {
            var data = FullSave();
            string a = _ser.Serialize(data);
            string b = _ser.Serialize(data);
            string c = new CanonicalJsonSerializer().Serialize(data);
            Assert.AreEqual(a, b, "same instance, two calls");
            Assert.AreEqual(a, c, "fresh serializer instance (cross-instance determinism proxy for cross-reload)");
        }

        [Test]
        public void test_serialize_null_best_time_encodes_as_json_null()
        {
            string json = _ser.Serialize(FullSave());
            StringAssert.Contains("\"D2\":null", json);
            Assert.IsFalse(json.Contains("\"D2\":\"null\""), "null must be a literal, not the string \"null\"");
        }

        // ── AC-3: CRC32 ───────────────────────────────────────────────────────

        [Test]
        public void test_crc32_matches_standard_ieee_vector()
        {
            Assert.AreEqual("CBF43926", CRC32Calculator.Compute("123456789"));
        }

        [Test]
        public void test_crc32_edge_inputs()
        {
            Assert.AreEqual("00000000", CRC32Calculator.Compute(string.Empty));
            Assert.AreEqual("E8B7BE43", CRC32Calculator.Compute("a"));
            Assert.AreEqual("00000000", CRC32Calculator.Compute(null)); // null treated as empty
        }

        [Test]
        public void test_crc32_is_uppercase_8_hex_chars()
        {
            string hash = CRC32Calculator.Compute(_ser.SerializeWithoutIntegrity(FullSave()));
            Assert.IsTrue(Regex.IsMatch(hash, "^[0-9A-F]{8}$"), $"expected 8 uppercase hex chars, got '{hash}'");
        }

        // ── AC-5: round-trip ──────────────────────────────────────────────────

        [Test]
        public void test_round_trip_preserves_all_fields()
        {
            var original = FullSave();
            SaveData restored = _ser.Deserialize(_ser.Serialize(original));

            Assert.AreEqual(original.Version, restored.Version);
            Assert.AreEqual(original.IntegrityHash, restored.IntegrityHash);
            Assert.AreEqual(2, restored.Weapons["L1"].Tier);
            Assert.IsTrue(restored.Weapons["L1"].Owned);
            Assert.IsFalse(restored.Weapons["L3"].Owned);
            Assert.AreEqual(72, restored.Materials["shard_common"]);
            Assert.AreEqual(0, restored.Materials["core_energy"]);

            var rec = restored.KaijuRecords["CARAPEX"];
            Assert.AreEqual(2, rec.FullClearCount);
            CollectionAssert.Contains(rec.PartsEverBroken, "boss_core");
            Assert.AreEqual(5, rec.HuntCountPerDifficulty["D1"]);
            Assert.AreEqual(185.3f, rec.BestTimePerDifficulty["D1"].Value, 0.001f);
            Assert.IsNull(rec.BestTimePerDifficulty["D2"], "null best-time survives round-trip as null");

            Assert.AreEqual("D2", restored.Meta.LastSelectedDifficulty);
            Assert.AreEqual("M1", restored.Meta.LastLoadout.Secondary);
            Assert.IsTrue(restored.Meta.FirstLaunchComplete);
            Assert.AreEqual(1.25f, restored.Settings.TextScale, 0.0001f);
            Assert.AreEqual("blue_yellow", restored.Settings.ColorblindMode);
            Assert.AreEqual(87, restored.Stats.TotalPartsBroken);
        }

        [Test]
        public void test_round_trip_is_idempotent_at_the_byte_level()
        {
            // Serialize → deserialize → serialize must reproduce the exact same bytes (canonical stability).
            var original = FullSave();
            string once = _ser.Serialize(original);
            string twice = _ser.Serialize(_ser.Deserialize(once));
            Assert.AreEqual(once, twice);
        }

        // ── AC-4: SaveConfig.OnValidate range checks ──────────────────────────

        [Test]
        public void test_save_config_onvalidate_flags_out_of_range_queue_depth()
        {
            var cfg = ScriptableObject.CreateInstance<SaveConfig>();
            ContentTestFactory.SetField(cfg, "_saveAsyncQueueDepth", 0); // below min 1

            LogAssert.Expect(LogType.Error, new Regex("SaveAsyncQueueDepth must be in \\[1, 3\\]"));
            InvokeOnValidate(cfg);

            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void test_save_config_onvalidate_flags_out_of_range_migration_generations()
        {
            var cfg = ScriptableObject.CreateInstance<SaveConfig>();
            ContentTestFactory.SetField(cfg, "_saveMaxMigrationGenerations", 6); // above max 5

            LogAssert.Expect(LogType.Error, new Regex("SaveMaxMigrationGenerations must be in \\[2, 5\\]"));
            InvokeOnValidate(cfg);

            Object.DestroyImmediate(cfg);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void AssertKeyOrder(string json, string firstKey, string secondKey)
        {
            int a = json.IndexOf(firstKey, System.StringComparison.Ordinal);
            int b = json.IndexOf(secondKey, System.StringComparison.Ordinal);
            Assert.Greater(a, -1, $"missing key {firstKey}");
            Assert.Greater(b, a, $"{firstKey} must precede {secondKey} (sorted keys)");
        }

        private static void InvokeOnValidate(SaveConfig cfg)
        {
            var m = typeof(SaveConfig).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            m.Invoke(cfg, null);
        }
    }
}
