using System;
using KaijuBreaker.Core;
using KaijuBreaker.Meta;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Meta
{
    /// <summary>
    /// Regression guard: EVERY <see cref="MaterialId"/> must have a <see cref="MaterialKeys"/> save-key entry.
    /// A missing entry made <see cref="MaterialKeys.ToKey"/> throw KeyNotFoundException mid-PartBroke dispatch
    /// (a new-kaiju core had no key), and because the event bus is fail-loud that aborted the dispatch before
    /// BossController hid the broken part — new-boss parts broke but never disappeared. This test makes the
    /// "add an enum value, forget the key map" trap fail in CI instead of on the boss arena floor.
    /// </summary>
    public sealed class MaterialKeysCoverAllIdsTest
    {
        [Test]
        public void ToKey_HasAnEntry_ForEveryMaterialId()
        {
            foreach (MaterialId id in Enum.GetValues(typeof(MaterialId)))
            {
                Assert.DoesNotThrow(() => MaterialKeys.ToKey(id),
                    $"MaterialId.{id} has no MaterialKeys.ToKey mapping — add it to ToKeyMap.");
                Assert.IsFalse(string.IsNullOrEmpty(MaterialKeys.ToKey(id)),
                    $"MaterialId.{id} maps to an empty save key.");
            }
        }

        [Test]
        public void ToKey_RoundTrips_ThroughTryFromKey_ForEveryMaterialId()
        {
            foreach (MaterialId id in Enum.GetValues(typeof(MaterialId)))
            {
                string key = MaterialKeys.ToKey(id);
                Assert.IsTrue(MaterialKeys.TryFromKey(key, out var back) && back == id,
                    $"MaterialId.{id} key '{key}' does not round-trip through TryFromKey.");
            }
        }
    }
}
