using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Reflection helper that scans a compiled assembly for any static reference to a target type.
    /// Used by the difficulty invariance tests (Story 003/004) to PROVE, at the architecture level, that
    /// KaijuParts / Weapons / Economy never reference <c>IDifficultyProvider</c> — the structural guarantee
    /// behind "part TTB / weapon output / material yield are difficulty-invariant" (difficulty-system.md
    /// §H.2/§H.3/§H.4). If a future refactor wires difficulty into one of those systems, the scan trips.
    ///
    /// Scans fields, properties, method returns/parameters, and constructor parameters of every declared
    /// type in the assembly. A match is exact type identity or assignability (interface implementation).
    /// </summary>
    public static class AssemblyReferenceScanner
    {
        private const BindingFlags All =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Return a list of human-readable reference sites where <paramref name="target"/> appears in the
        /// named assembly. Empty list = no reference (the invariance guarantee holds). If the assembly is
        /// not loaded, returns a single diagnostic entry so the caller's assertion fails loudly rather than
        /// silently passing on a typo'd assembly name.
        /// </summary>
        public static List<string> FindReferencesTo(string assemblyName, Type target)
        {
            var hits = new List<string>();

            Assembly asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);
            if (asm == null)
            {
                hits.Add($"[assembly '{assemblyName}' not loaded — cannot verify]");
                return hits;
            }

            foreach (Type t in SafeGetTypes(asm))
            {
                foreach (FieldInfo f in t.GetFields(All))
                    if (Matches(f.FieldType, target)) hits.Add($"{t.Name}.{f.Name} (field)");

                foreach (PropertyInfo p in t.GetProperties(All))
                    if (Matches(p.PropertyType, target)) hits.Add($"{t.Name}.{p.Name} (property)");

                foreach (MethodInfo m in t.GetMethods(All))
                {
                    if (Matches(m.ReturnType, target)) hits.Add($"{t.Name}.{m.Name}() (return)");
                    foreach (ParameterInfo pr in m.GetParameters())
                        if (Matches(pr.ParameterType, target)) hits.Add($"{t.Name}.{m.Name}(…{pr.Name}) (param)");
                }

                foreach (ConstructorInfo c in t.GetConstructors(All))
                    foreach (ParameterInfo pr in c.GetParameters())
                        if (Matches(pr.ParameterType, target)) hits.Add($"{t.Name}.ctor(…{pr.Name}) (param)");
            }

            return hits;
        }

        private static bool Matches(Type candidate, Type target)
        {
            if (candidate == null) return false;
            if (candidate == target) return true;
            if (target.IsAssignableFrom(candidate)) return true;
            // Unwrap arrays / by-ref / generic args (e.g. List<IDifficultyProvider>, IDifficultyProvider[]).
            if (candidate.IsArray && Matches(candidate.GetElementType(), target)) return true;
            if (candidate.IsByRef && Matches(candidate.GetElementType(), target)) return true;
            if (candidate.IsGenericType)
                foreach (Type arg in candidate.GetGenericArguments())
                    if (Matches(arg, target)) return true;
            return false;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
        }
    }
}
