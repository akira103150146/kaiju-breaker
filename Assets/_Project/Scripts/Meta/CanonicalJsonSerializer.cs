using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Hand-rolled canonical JSON serializer/deserializer for <see cref="SaveData"/> (ADR-0004 Option C).
    /// A dependency-free implementation guarantees the canonical form the CRC32 integrity hash requires
    /// (meta-progression-system.md §D.2): every object's keys sorted ordinally, no whitespace, floats in
    /// round-trip form, nulls as literal <c>null</c>. Byte-identical output for equal input, every run and
    /// platform. <c>JsonUtility</c> is deliberately avoided (no dictionary support, no key ordering control).
    /// </summary>
    public sealed class CanonicalJsonSerializer : ICanonicalSerializer
    {
        /// <inheritdoc/>
        public string Serialize(SaveData data) => Emit(data, includeIntegrity: true);

        /// <summary>
        /// Serialize omitting the <c>integrity_hash</c> field — the exact byte string the CRC32 is computed
        /// over (meta-progression-system.md §D.2 <c>S = CRC32_hex(canonical_json(D \ {S}))</c>).
        /// </summary>
        public string SerializeWithoutIntegrity(SaveData data) => Emit(data, includeIntegrity: false);

        private string Emit(SaveData data, bool includeIntegrity)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var root = new List<KeyValuePair<string, string>>
            {
                Kv("version", Int(data.Version)),
                Kv("flags", EmitBoolMap(data.Flags)),
                Kv("weapons", EmitWeapons(data.Weapons)),
                Kv("materials", EmitLongMap(data.Materials)),
                Kv("kaiju_records", EmitKaijuRecords(data.KaijuRecords)),
                Kv("meta", EmitMeta(data.Meta)),
                Kv("settings", EmitSettings(data.Settings)),
                Kv("stats", EmitStats(data.Stats)),
            };
            if (includeIntegrity)
                root.Add(Kv("integrity_hash", Str(data.IntegrityHash)));

            return Obj(root);
        }

        // ── Section emitters ──────────────────────────────────────────────────

        private static string EmitWeapons(Dictionary<string, WeaponSaveData> weapons)
        {
            var members = new List<KeyValuePair<string, string>>();
            if (weapons != null)
                foreach (var kv in weapons)
                {
                    var w = kv.Value ?? new WeaponSaveData();
                    members.Add(Kv(kv.Key, Obj(new List<KeyValuePair<string, string>>
                    {
                        Kv("tier", Int(w.Tier)),
                        Kv("owned", Bool(w.Owned)),
                    })));
                }
            return Obj(members);
        }

        private static string EmitLongMap(Dictionary<string, long> map)
        {
            var members = new List<KeyValuePair<string, string>>();
            if (map != null)
                foreach (var kv in map) members.Add(Kv(kv.Key, Int(kv.Value)));
            return Obj(members);
        }

        private static string EmitBoolMap(Dictionary<string, bool> map)
        {
            var members = new List<KeyValuePair<string, string>>();
            if (map != null)
                foreach (var kv in map) members.Add(Kv(kv.Key, Bool(kv.Value)));
            return Obj(members);
        }

        private static string EmitKaijuRecords(Dictionary<string, KaijuRecordData> records)
        {
            var members = new List<KeyValuePair<string, string>>();
            if (records != null)
                foreach (var kv in records)
                {
                    var r = kv.Value ?? new KaijuRecordData();
                    members.Add(Kv(kv.Key, Obj(new List<KeyValuePair<string, string>>
                    {
                        Kv("parts_ever_broken", StrArraySorted(r.PartsEverBroken)),
                        Kv("full_clear_count", Int(r.FullClearCount)),
                        Kv("hunt_count_per_difficulty", EmitIntMap(r.HuntCountPerDifficulty)),
                        Kv("best_time_per_difficulty", EmitNullableFloatMap(r.BestTimePerDifficulty)),
                    })));
                }
            return Obj(members);
        }

        private static string EmitIntMap(Dictionary<string, int> map)
        {
            var members = new List<KeyValuePair<string, string>>();
            if (map != null)
                foreach (var kv in map) members.Add(Kv(kv.Key, Int(kv.Value)));
            return Obj(members);
        }

        private static string EmitNullableFloatMap(Dictionary<string, float?> map)
        {
            var members = new List<KeyValuePair<string, string>>();
            if (map != null)
                foreach (var kv in map) members.Add(Kv(kv.Key, NullableFloat(kv.Value)));
            return Obj(members);
        }

        private static string EmitMeta(MetaBlock m)
        {
            m ??= new MetaBlock();
            var loadout = m.LastLoadout ?? new LoadoutData();
            return Obj(new List<KeyValuePair<string, string>>
            {
                Kv("last_selected_difficulty", Str(m.LastSelectedDifficulty)),
                Kv("last_loadout", Obj(new List<KeyValuePair<string, string>>
                {
                    Kv("primary", Str(loadout.Primary)),
                    Kv("secondary", Str(loadout.Secondary)),
                })),
                Kv("first_launch_complete", Bool(m.FirstLaunchComplete)),
            });
        }

        private static string EmitSettings(SettingsData s)
        {
            s ??= new SettingsData();
            return Obj(new List<KeyValuePair<string, string>>
            {
                Kv("reduce_motion", Bool(s.ReduceMotion)),
                Kv("colorblind_mode", Str(s.ColorblindMode)),
                Kv("text_scale", Float(s.TextScale)),
                Kv("bgm_volume", Float(s.BgmVolume)),
                Kv("sfx_volume", Float(s.SfxVolume)),
            });
        }

        private static string EmitStats(StatsData s)
        {
            s ??= new StatsData();
            return Obj(new List<KeyValuePair<string, string>>
            {
                Kv("total_runs_started", Int(s.TotalRunsStarted)),
                Kv("total_runs_completed", Int(s.TotalRunsCompleted)),
                Kv("total_parts_broken", Int(s.TotalPartsBroken)),
                Kv("total_full_clears", Int(s.TotalFullClears)),
                Kv("total_play_time_seconds", Int(s.TotalPlayTimeSeconds)),
            });
        }

        // ── Primitive emitters ────────────────────────────────────────────────

        private static KeyValuePair<string, string> Kv(string k, string v) => new KeyValuePair<string, string>(k, v);

        /// <summary>Render an object with keys sorted ordinally, compact, no whitespace.</summary>
        private static string Obj(List<KeyValuePair<string, string>> members)
        {
            members.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            var sb = new StringBuilder(64);
            sb.Append('{');
            for (int i = 0; i < members.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Str(members[i].Key)).Append(':').Append(members[i].Value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string StrArraySorted(List<string> items)
        {
            var copy = items != null ? new List<string>(items) : new List<string>();
            copy.Sort(StringComparer.Ordinal); // set semantics → deterministic (canonical) order
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < copy.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Str(copy[i]));
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string Int(long v) => v.ToString(CultureInfo.InvariantCulture);

        private static string Bool(bool v) => v ? "true" : "false";

        private static string Float(float v) => v.ToString("R", CultureInfo.InvariantCulture);

        private static string NullableFloat(float? v) => v.HasValue ? Float(v.Value) : "null";

        private static string Str(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        /// <inheritdoc/>
        public SaveData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("json must not be empty.", nameof(json));
            object dom = new JsonReader(json).Parse();
            if (!(dom is Dictionary<string, object> root))
                throw new FormatException("Save JSON root must be an object.");
            return Map(root);
        }

        private static SaveData Map(Dictionary<string, object> root)
        {
            var d = new SaveData
            {
                Version = (int)GetDouble(root, "version", 1),
                IntegrityHash = GetString(root, "integrity_hash", string.Empty),
            };

            foreach (var kv in GetObj(root, "weapons"))
            {
                var w = AsObj(kv.Value);
                d.Weapons[kv.Key] = new WeaponSaveData((int)GetDouble(w, "tier", 0), GetBool(w, "owned", false));
            }

            foreach (var kv in GetObj(root, "materials"))
                d.Materials[kv.Key] = (long)AsDouble(kv.Value);

            foreach (var kv in GetObj(root, "flags"))
                d.Flags[kv.Key] = kv.Value is bool b && b;

            foreach (var kv in GetObj(root, "kaiju_records"))
            {
                var ro = AsObj(kv.Value);
                var rec = new KaijuRecordData { FullClearCount = (int)GetDouble(ro, "full_clear_count", 0) };
                foreach (var p in GetList(ro, "parts_ever_broken")) rec.PartsEverBroken.Add((string)p);
                foreach (var h in GetObj(ro, "hunt_count_per_difficulty")) rec.HuntCountPerDifficulty[h.Key] = (int)AsDouble(h.Value);
                foreach (var b in GetObj(ro, "best_time_per_difficulty"))
                    rec.BestTimePerDifficulty[b.Key] = b.Value == null ? (float?)null : (float)AsDouble(b.Value);
                d.KaijuRecords[kv.Key] = rec;
            }

            var meta = GetObj(root, "meta");
            d.Meta.LastSelectedDifficulty = GetString(meta, "last_selected_difficulty", "D1");
            d.Meta.FirstLaunchComplete = GetBool(meta, "first_launch_complete", false);
            var loadout = GetObj(meta, "last_loadout");
            d.Meta.LastLoadout = new LoadoutData(GetString(loadout, "primary", "L1"), GetString(loadout, "secondary", "M1"));

            var settings = GetObj(root, "settings");
            d.Settings.ReduceMotion = GetBool(settings, "reduce_motion", false);
            d.Settings.ColorblindMode = GetString(settings, "colorblind_mode", "default");
            d.Settings.TextScale = (float)GetDouble(settings, "text_scale", 1.0);
            d.Settings.BgmVolume = (float)GetDouble(settings, "bgm_volume", 1.0);
            d.Settings.SfxVolume = (float)GetDouble(settings, "sfx_volume", 1.0);

            var stats = GetObj(root, "stats");
            d.Stats.TotalRunsStarted = (long)GetDouble(stats, "total_runs_started", 0);
            d.Stats.TotalRunsCompleted = (long)GetDouble(stats, "total_runs_completed", 0);
            d.Stats.TotalPartsBroken = (long)GetDouble(stats, "total_parts_broken", 0);
            d.Stats.TotalFullClears = (long)GetDouble(stats, "total_full_clears", 0);
            d.Stats.TotalPlayTimeSeconds = (long)GetDouble(stats, "total_play_time_seconds", 0);

            return d;
        }

        // ── DOM accessors ─────────────────────────────────────────────────────

        private static Dictionary<string, object> AsObj(object o) =>
            o as Dictionary<string, object> ?? new Dictionary<string, object>();

        private static double AsDouble(object o) => o is double dd ? dd : 0.0;

        private static Dictionary<string, object> GetObj(Dictionary<string, object> o, string key) =>
            o.TryGetValue(key, out var v) ? AsObj(v) : new Dictionary<string, object>();

        private static List<object> GetList(Dictionary<string, object> o, string key) =>
            o.TryGetValue(key, out var v) && v is List<object> l ? l : new List<object>();

        private static string GetString(Dictionary<string, object> o, string key, string fallback) =>
            o.TryGetValue(key, out var v) && v is string s ? s : fallback;

        private static double GetDouble(Dictionary<string, object> o, string key, double fallback) =>
            o.TryGetValue(key, out var v) && v is double d ? d : fallback;

        private static bool GetBool(Dictionary<string, object> o, string key, bool fallback) =>
            o.TryGetValue(key, out var v) && v is bool b ? b : fallback;

        // ── Minimal recursive-descent JSON parser ─────────────────────────────

        private sealed class JsonReader
        {
            private readonly string _s;
            private int _i;

            public JsonReader(string s) { _s = s; _i = 0; }

            public object Parse()
            {
                SkipWs();
                object v = ParseValue();
                SkipWs();
                if (_i != _s.Length) throw new FormatException($"Trailing characters at index {_i}.");
                return v;
            }

            private object ParseValue()
            {
                SkipWs();
                char c = Peek();
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': case 'f': return ParseBool();
                    case 'n': Expect("null"); return null;
                    default: return ParseNumber();
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                var obj = new Dictionary<string, object>();
                _i++; // {
                SkipWs();
                if (Peek() == '}') { _i++; return obj; }
                while (true)
                {
                    SkipWs();
                    string key = ParseString();
                    SkipWs();
                    if (Next() != ':') throw new FormatException($"Expected ':' after key '{key}'.");
                    obj[key] = ParseValue();
                    SkipWs();
                    char c = Next();
                    if (c == ',') continue;
                    if (c == '}') break;
                    throw new FormatException($"Expected ',' or '}}' in object at index {_i}.");
                }
                return obj;
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _i++; // [
                SkipWs();
                if (Peek() == ']') { _i++; return list; }
                while (true)
                {
                    list.Add(ParseValue());
                    SkipWs();
                    char c = Next();
                    if (c == ',') continue;
                    if (c == ']') break;
                    throw new FormatException($"Expected ',' or ']' in array at index {_i}.");
                }
                return list;
            }

            private string ParseString()
            {
                if (Next() != '"') throw new FormatException($"Expected '\"' at index {_i - 1}.");
                var sb = new StringBuilder();
                while (true)
                {
                    char c = Next();
                    if (c == '"') break;
                    if (c == '\\')
                    {
                        char e = Next();
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                string hex = _s.Substring(_i, 4);
                                _i += 4;
                                sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                                break;
                            default: throw new FormatException($"Invalid escape '\\{e}'.");
                        }
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            private bool ParseBool()
            {
                if (Peek() == 't') { Expect("true"); return true; }
                Expect("false");
                return false;
            }

            private double ParseNumber()
            {
                int start = _i;
                while (_i < _s.Length && "+-0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
                string token = _s.Substring(start, _i - start);
                if (token.Length == 0) throw new FormatException($"Expected value at index {start}.");
                return double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private void Expect(string literal)
            {
                if (_i + literal.Length > _s.Length || _s.Substring(_i, literal.Length) != literal)
                    throw new FormatException($"Expected '{literal}' at index {_i}.");
                _i += literal.Length;
            }

            private void SkipWs()
            {
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _i++;
                    else break;
                }
            }

            private char Peek() => _i < _s.Length ? _s[_i] : '\0';
            private char Next() => _i < _s.Length ? _s[_i++] : throw new FormatException("Unexpected end of JSON.");
        }
    }
}
