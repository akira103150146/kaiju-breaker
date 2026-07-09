using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Canonical mapping between the <see cref="MaterialId"/> enum and its snake_case save-file key
    /// (meta-progression-system.md §C.3 materials{}). This is a wire-format name map (like an enum's
    /// serialization name), the single place the save keys are defined — not a balance/tuning value.
    /// Weapon and difficulty keys need no map: <c>WeaponId.L1.ToString()</c> is "L1" and
    /// <c>DifficultyTier.D1.ToString()</c> is "D1" already.
    /// </summary>
    public static class MaterialKeys
    {
        private static readonly Dictionary<MaterialId, string> ToKeyMap = new Dictionary<MaterialId, string>
        {
            { MaterialId.ShardCommon, "shard_common" },
            { MaterialId.CoreCarapace, "core_carapace" },
            { MaterialId.CoreLimb, "core_limb" },
            { MaterialId.CoreEnergy, "core_energy" },
            // The 5 new-kaiju theme cores (session-9 roster expansion). MUST be here: a break on a new boss
            // credits one of these via Economy → MetaSaveService.CreditMaterials → ToKey; a missing entry threw
            // KeyNotFoundException mid-PartBroke dispatch, and because the bus is fail-loud the later-subscribed
            // BossController.OnPartBroke (which hides + silences the part) never ran — so new-boss parts broke but
            // never disappeared. (Same half-done enum-expansion trap as EconomyConfig.GetCoreForTheme.)
            { MaterialId.CoreSwarm, "core_swarm" },
            { MaterialId.CoreCrystal, "core_crystal" },
            { MaterialId.CoreAbyss, "core_abyss" },
            { MaterialId.CoreEmber, "core_ember" },
            { MaterialId.CoreVoid, "core_void" },
            { MaterialId.EssenceKaiju, "essence_kaiju" },
        };

        private static readonly Dictionary<string, MaterialId> FromKeyMap = BuildReverse();

        private static Dictionary<string, MaterialId> BuildReverse()
        {
            var r = new Dictionary<string, MaterialId>();
            foreach (var kv in ToKeyMap) r[kv.Value] = kv.Key;
            return r;
        }

        /// <summary>All material save keys, in enum order.</summary>
        public static IEnumerable<string> AllKeys => ToKeyMap.Values;

        /// <summary>The snake_case save key for a material id (e.g. <c>ShardCommon → "shard_common"</c>).</summary>
        public static string ToKey(MaterialId id) => ToKeyMap[id];

        /// <summary>The material id for a save key; false if the key is unknown.</summary>
        public static bool TryFromKey(string key, out MaterialId id) => FromKeyMap.TryGetValue(key, out id);
    }
}
