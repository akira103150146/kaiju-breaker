using System;
using System.Reflection;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Story 009 — test-fixture support for the ScriptableObject config layer.
    ///
    /// Config SOs use <c>[SerializeField] private</c> fields with public read-only
    /// properties, so tests cannot set values directly. This factory creates in-memory
    /// SO instances (no on-disk asset needed) and sets private serialized fields by name
    /// via reflection, so a system-under-test can be injected with a KNOWN config fixture
    /// without touching Assets or the Inspector. Realizes coding-standards testability +
    /// content-config story 009.
    ///
    /// Usage:
    /// <code>
    ///   var econ = ContentTestFactory.Create&lt;EconomyConfig&gt;(
    ///       ("_shardYieldBase", 3),
    ///       ("_shardYieldSoftenedMult", 1.5f));
    ///   Assert.AreEqual(3, econ.ShardYieldBase);
    /// </code>
    /// </summary>
    public static class ContentTestFactory
    {
        /// <summary>Create a config SO with default values, then apply the given (privateFieldName, value) overrides.</summary>
        public static T Create<T>(params (string field, object value)[] overrides) where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            if (overrides != null)
                for (int i = 0; i < overrides.Length; i++)
                    SetField(so, overrides[i].field, overrides[i].value);
            return so;
        }

        /// <summary>Set a private serialized field on a ScriptableObject by name (walks the type hierarchy).</summary>
        public static void SetField(ScriptableObject target, string fieldName, object value)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type = type.BaseType;
            }

            if (field == null)
                throw new ArgumentException(
                    $"Field '{fieldName}' not found on {target.GetType().Name}.", nameof(fieldName));

            field.SetValue(target, value);
        }
    }
}
