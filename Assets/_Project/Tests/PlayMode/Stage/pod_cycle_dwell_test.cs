using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KaijuBreaker.Tests.PlayMode.Stage
{
    /// <summary>
    /// Stage Story 005 — cycling weapon pod behaviour (stage-system.md §F.1/§F.2). PlayMode because it drives
    /// the pod's descend→dwell→despawn state machine, bob clamp, weapon cycling, and pickup on real
    /// GameObjects with a scaled-down <see cref="PodDropConfig"/> (fast dwell/cycle) so tests stay quick.
    ///
    /// <para><b>Reconciliation:</b> pickup publishes the committed <see cref="WeaponPodGrabbed"/> (weapon
    /// only — Meta derives first-pickup itself, meta-save Story 007), so no ISaveService is injected. Pickup
    /// is driven via the public <see cref="WeaponPodController.Collect"/> to avoid a two-body physics setup.</para>
    /// </summary>
    [TestFixture]
    public sealed class PodCycleDwellTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _spawned) if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
            foreach (var pod in Object.FindObjectsByType<WeaponPodController>(FindObjectsSortMode.None))
                if (pod != null) Object.DestroyImmediate(pod.gameObject);
        }

        private static void Set(object o, string field, object val) =>
            o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, val);

        private PodDropConfig FastConfig(float dwell = 0.4f, float cycle = 0.1f)
        {
            var c = ScriptableObject.CreateInstance<PodDropConfig>();
            Set(c, "_podDwellTimeSeconds", dwell);
            Set(c, "_podCycleIntervalSeconds", cycle);
            Set(c, "_podDescendSpeedPxPerSec", 60f);
            Set(c, "_podBobAmplitudePx", 0.5f);
            Set(c, "_podDespawnAfterSeconds", 5f);
            _spawned.Add(c);
            return c;
        }

        private WeaponPodController MakePod(RecordingEventBusStub bus, PodDropConfig config, List<WeaponId> pool,
                                            float startY = 5f, float bandMin = 0f, float bandMax = 2f)
        {
            var go = new GameObject("WeaponPod");
            go.transform.position = new Vector3(0f, startY, 0f);
            go.AddComponent<SpriteRenderer>();
            var pod = go.AddComponent<WeaponPodController>();
            pod.Init(bus, config, pool, PodType.Primary, bandMin, bandMax);
            _spawned.Add(go);
            return pod;
        }

        /// <summary>Minimal recording bus (PlayMode asmdef can't see the EditMode helpers).</summary>
        private sealed class RecordingEventBusStub : IEventBus
        {
            private readonly TypedEventBus _inner = new TypedEventBus();
            public readonly List<WeaponPodGrabbed> Grabs = new List<WeaponPodGrabbed>();
            public RecordingEventBusStub() { _inner.Subscribe<WeaponPodGrabbed>(g => Grabs.Add(g)); }
            public void Publish<T>(in T evt) where T : struct, IGameEvent => _inner.Publish(in evt);
            public void Subscribe<T>(System.Action<T> h) where T : struct, IGameEvent => _inner.Subscribe(h);
            public void Unsubscribe<T>(System.Action<T> h) where T : struct, IGameEvent => _inner.Unsubscribe(h);
        }

        private static IEnumerator WaitForPhase(WeaponPodController pod, WeaponPodController.PodPhase phase, int maxFrames = 300)
        {
            for (int i = 0; i < maxFrames && pod != null && pod.Phase != phase; i++) yield return null;
        }

        // ── Descend → Dwell + cycling ─────────────────────────────────────────

        [UnityTest]
        public IEnumerator test_pod_descends_then_dwells_and_cycles_weapons()
        {
            var bus = new RecordingEventBusStub();
            var pod = MakePod(bus, FastConfig(), new List<WeaponId> { WeaponId.L1, WeaponId.L2 });

            Assert.AreEqual(WeaponPodController.PodPhase.Descending, pod.Phase);
            yield return WaitForPhase(pod, WeaponPodController.PodPhase.Dwelling);
            Assert.AreEqual(WeaponPodController.PodPhase.Dwelling, pod.Phase, "reached reachable band");

            // Over the dwell, the display index must advance past 0 at least once (cycling works).
            bool cycled = false;
            for (int i = 0; i < 120 && pod != null && pod.Phase == WeaponPodController.PodPhase.Dwelling; i++)
            {
                if (pod.DisplayIndex != 0) cycled = true;
                yield return null;
            }
            Assert.IsTrue(cycled, "the pod cycled to the second weapon during dwell");
        }

        [UnityTest]
        public IEnumerator test_pod_stays_within_reachable_band_during_dwell()
        {
            var bus = new RecordingEventBusStub();
            var pod = MakePod(bus, FastConfig(dwell: 0.5f), new List<WeaponId> { WeaponId.L1, WeaponId.L2 },
                              bandMin: 0f, bandMax: 2f);

            yield return WaitForPhase(pod, WeaponPodController.PodPhase.Dwelling);
            for (int i = 0; i < 60 && pod != null && pod.Phase == WeaponPodController.PodPhase.Dwelling; i++)
            {
                float y = pod.transform.position.y;
                Assert.GreaterOrEqual(y, -0.001f, "y within band min");
                Assert.LessOrEqual(y, 2.001f, "y within band max");
                yield return null;
            }
        }

        // ── Single-weapon pool: static, still collectable ─────────────────────

        [UnityTest]
        public IEnumerator test_single_weapon_pool_does_not_cycle()
        {
            var bus = new RecordingEventBusStub();
            var pod = MakePod(bus, FastConfig(), new List<WeaponId> { WeaponId.M1 });

            yield return WaitForPhase(pod, WeaponPodController.PodPhase.Dwelling);
            for (int i = 0; i < 30 && pod != null && pod.Phase == WeaponPodController.PodPhase.Dwelling; i++)
            {
                Assert.AreEqual(0, pod.DisplayIndex, "single-weapon pool never cycles");
                yield return null;
            }
        }

        // ── Pickup grabs the CURRENT weapon + publishes + vanishes ────────────

        [UnityTest]
        public IEnumerator test_collect_grabs_current_weapon_and_publishes()
        {
            var bus = new RecordingEventBusStub();
            var pod = MakePod(bus, FastConfig(dwell: 2f), new List<WeaponId> { WeaponId.L1, WeaponId.L2 });

            yield return WaitForPhase(pod, WeaponPodController.PodPhase.Dwelling);
            WeaponId current = pod.CurrentWeapon;
            var podGo = pod.gameObject;
            pod.Collect();

            Assert.AreEqual(1, bus.Grabs.Count, "exactly one WeaponPodGrabbed");
            Assert.AreEqual(current, bus.Grabs[0].Weapon, "grabbed the currently-displayed weapon");
            yield return null;
            Assert.IsTrue(podGo == null, "pod vanished immediately on pickup");
        }

        // ── Dwell expiry → despawns ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator test_pod_despawns_after_dwell()
        {
            var bus = new RecordingEventBusStub();
            var pod = MakePod(bus, FastConfig(dwell: 0.3f), new List<WeaponId> { WeaponId.L1, WeaponId.L2 });

            yield return WaitForPhase(pod, WeaponPodController.PodPhase.Despawning);
            Assert.AreEqual(WeaponPodController.PodPhase.Despawning, pod.Phase, "entered despawn after dwell expired");
        }
    }
}
