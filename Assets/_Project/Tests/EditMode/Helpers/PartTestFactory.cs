using System;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Fixture builders for KaijuParts tests. <see cref="PartDef"/> is a plain
    /// [Serializable] class with private serialized fields, and <see cref="KaijuDef"/> /
    /// the config SOs likewise expose only read-only properties, so tests set them by
    /// reflection here rather than via the Inspector. Configs are created with their C#
    /// field-initializer defaults (which match GDD §G), then selectively overridden.
    /// </summary>
    public static class PartTestFactory
    {
        /// <summary>Build a <see cref="PartDef"/> with the given identity, drop table, adjacency and overrides.</summary>
        public static PartDef Part(
            string id,
            PartType type,
            string dropTableId = "drop_default",
            string[] adjacency = null,
            float? hMaxOverride = null,
            float? bMaxOverride = null)
        {
            var pd = new PartDef();
            SetField(pd, "_partId", id);
            SetField(pd, "_partType", type);
            SetField(pd, "_dropTableId", dropTableId);
            SetField(pd, "_adjacency", adjacency ?? Array.Empty<string>());
            if (hMaxOverride.HasValue)
            {
                SetField(pd, "_hMaxUseOverride", true);
                SetField(pd, "_hMaxOverride", hMaxOverride.Value);
            }
            if (bMaxOverride.HasValue)
            {
                SetField(pd, "_bMaxUseOverride", true);
                SetField(pd, "_bMaxOverride", bMaxOverride.Value);
            }
            return pd;
        }

        /// <summary>Build a <see cref="KaijuDef"/> from a string id and a set of parts.</summary>
        public static KaijuDef Kaiju(string kaijuId, params PartDef[] parts)
        {
            var def = ScriptableObject.CreateInstance<KaijuDef>();
            SetPrivate(def, "_kaijuId", kaijuId);
            SetPrivate(def, "_parts", parts);
            return def;
        }

        /// <summary>WeaponBalanceConfig fixture with GDD defaults, plus optional (privateField, value) overrides.</summary>
        public static WeaponBalanceConfig Balance(params (string field, object value)[] overrides)
            => ContentTestFactory.Create<WeaponBalanceConfig>(overrides);

        /// <summary>
        /// Balance fixture pinned to the CLASSIC break thresholds (Normal 100 / Armored 150 / BossCore 200) so
        /// break-MECHANICS tests stay independent of the shipped tunable defaults — which were raised (200/320/420)
        /// to make bosses a real fight after the primary-weapon power buff. Mechanics (fill/clamp/threshold/chain)
        /// are unchanged; only the numeric durability moved, so these tests pin the numbers they were written for.
        /// </summary>
        public static WeaponBalanceConfig BalanceClassicBreak()
            => Balance(
                ("_bMaxNormal", 100f), ("_bMaxArmored", 150f), ("_bMaxBossCore", 200f),
                ("_requiredBreakThresholdNormal", 100f), ("_requiredBreakThresholdArmored", 150f),
                ("_requiredBreakThresholdBossCore", 200f));

        /// <summary>PartSystemConfig fixture with GDD defaults, plus optional (privateField, value) overrides.</summary>
        public static PartSystemConfig PartConfig(params (string field, object value)[] overrides)
            => ContentTestFactory.Create<PartSystemConfig>(overrides);

        // ── Reflection helpers ───────────────────────────────────────────────────

        /// <summary>Set a private serialized field on any object (walks the type hierarchy).</summary>
        public static void SetField(object target, string fieldName, object value)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type = type.BaseType;
            }
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found on {target.GetType().Name}.", nameof(fieldName));
            field.SetValue(target, value);
        }

        private static void SetPrivate(ScriptableObject so, string fieldName, object value)
            => ContentTestFactory.SetField(so, fieldName, value);
    }
}
