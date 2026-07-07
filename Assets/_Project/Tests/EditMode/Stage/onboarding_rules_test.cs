using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Stage Story 007 — Stage 1 onboarding rules (stage-system.md §H.2; TR-stage-004). The
    /// <see cref="OnboardingController"/> is pure event-driven C#, so this is an EditMode Logic test (AC-6,
    /// the 5-player comprehension playtest, is a separate manual evidence doc). Drives the controller with a
    /// <see cref="RecordingEventBus"/> + <see cref="RecordingSaveService"/> (now with GetFlag/SetFlag).
    ///
    /// <para><b>Reconciliation:</b> `ISaveService` gained `GetFlag`/`SetFlag` (backed by a new
    /// `SaveData.Flags` map + serializer) — the committed interface had no flag persistence. The intro
    /// slow-down's actual application (WaveSpawner honouring `EnemySpeedOverride`) is a wiring follow-up;
    /// this suite verifies the controller emits the right events/flags per the ACs.</para>
    /// </summary>
    [TestFixture]
    public sealed class OnboardingRulesTests
    {
        private RecordingEventBus _bus;
        private RecordingSaveService _save;
        private OnboardingConfig _config;
        private FakeDifficulty _difficulty;

        private sealed class FakeDifficulty : IDifficultyProvider
        {
            public DifficultyTier CurrentTier { get; set; } = DifficultyTier.D1;
            public float BulletDensityMult { get; set; } = 1f;
            public float EnemyCountMult { get; set; } = 1f;
        }

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _save = new RecordingSaveService();
            _config = ContentTestFactory.Create<OnboardingConfig>(
                ("_ramGrubIntroSpeedMult", 0.70f), ("_tooltipText", "拾取武器莢艙以替換當前武器"), ("_tooltipDurationSec", 3.0f));
            _difficulty = new FakeDifficulty();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        private OnboardingController Make(string stageId = "stage_01") =>
            new OnboardingController(_bus, _save, _config, _difficulty, stageId);

        private static SegmentDef Seg(string id, int eliteWaveIndex) =>
            ContentTestFactory.Create<SegmentDef>(("_segmentId", id), ("_difficultyWeight", 1), ("_eliteWaveIndex", eliteWaveIndex));

        // ── AC-1 / AC-2: intro slow-down (D1, wave 0 only) ────────────────────

        [Test]
        public void test_intro_wave0_at_d1_emits_ramgrub_speed_override()
        {
            var c = Make();
            _bus.Publish(new IntroSegmentWaveSpawning(isIntroSegment: true, waveIndex: 0));

            Assert.AreEqual(1, _bus.CountOf<EnemySpeedOverride>());
            var ov = _bus.Events<EnemySpeedOverride>()[0];
            Assert.AreEqual("ram_grub", ov.EnemyId);
            Assert.AreEqual(0.70f, ov.SpeedMultiplier, 0.0001f);
            c.Dispose();
        }

        [Test]
        public void test_intro_slowdown_not_applied_at_d2()
        {
            _difficulty.CurrentTier = DifficultyTier.D2;
            var c = Make();
            _bus.Publish(new IntroSegmentWaveSpawning(true, 0));
            Assert.AreEqual(0, _bus.CountOf<EnemySpeedOverride>());
            c.Dispose();
        }

        [Test]
        public void test_intro_slowdown_only_on_wave0()
        {
            var c = Make();
            _bus.Publish(new IntroSegmentWaveSpawning(true, 1)); // W2
            Assert.AreEqual(0, _bus.CountOf<EnemySpeedOverride>());
            c.Dispose();
        }

        // ── AC-3: force first-segment pod when no elite ───────────────────────

        [Test]
        public void test_force_primary_pod_when_first_segment_has_no_elite()
        {
            var c = Make();
            var seq = new SegmentSequence(null, new List<SegmentDef> { Seg("s1_01", eliteWaveIndex: -1) }, null, "carapex");

            c.ReviewFirstSegment(seq);

            Assert.AreEqual(1, _bus.CountOf<ForceFirstSegmentPodCarrier>());
            var f = _bus.Events<ForceFirstSegmentPodCarrier>()[0];
            Assert.AreEqual(0, f.SegmentIndex);
            Assert.AreEqual(PodType.Primary, f.PoolType);
            c.Dispose();
        }

        [Test]
        public void test_no_force_when_first_segment_already_has_elite()
        {
            var c = Make();
            var seq = new SegmentSequence(null, new List<SegmentDef> { Seg("s1_02", eliteWaveIndex: 1) }, null, "carapex");

            c.ReviewFirstSegment(seq);
            Assert.AreEqual(0, _bus.CountOf<ForceFirstSegmentPodCarrier>());
            c.Dispose();
        }

        // ── AC-4: one-time tooltip + permanent off ────────────────────────────

        [Test]
        public void test_first_pod_pickup_shows_tooltip_once_and_persists_flag()
        {
            var c = Make();

            _bus.Publish(new WeaponPodGrabbed(WeaponId.L1));
            Assert.AreEqual(1, _bus.CountOf<ShowOnboardingTooltip>());
            Assert.AreEqual(3.0f, _bus.Events<ShowOnboardingTooltip>()[0].DurationSec, 0.0001f);
            Assert.IsTrue(_save.GetFlag(OnboardingController.FirstPodPickupShownFlag), "flag persisted");

            _bus.Publish(new WeaponPodGrabbed(WeaponId.M1)); // second pickup → no tooltip
            Assert.AreEqual(1, _bus.CountOf<ShowOnboardingTooltip>(), "tooltip is permanently off after the first");
            c.Dispose();
        }

        [Test]
        public void test_tooltip_not_shown_when_flag_already_set()
        {
            _save.SetFlag(OnboardingController.FirstPodPickupShownFlag, true);
            var c = Make();
            _bus.Publish(new WeaponPodGrabbed(WeaponId.L1));
            Assert.AreEqual(0, _bus.CountOf<ShowOnboardingTooltip>());
            c.Dispose();
        }

        // ── AC-5: silent on any non-stage_01 run ──────────────────────────────

        [Test]
        public void test_controller_is_silent_on_other_stages()
        {
            var c = Make("stage_02");
            Assert.IsFalse(c.IsActive);

            _bus.Publish(new IntroSegmentWaveSpawning(true, 0));
            _bus.Publish(new WeaponPodGrabbed(WeaponId.L1));
            c.ReviewFirstSegment(new SegmentSequence(null, new List<SegmentDef> { Seg("s2_01", -1) }, null, "lacera"));

            Assert.AreEqual(0, _bus.CountOf<EnemySpeedOverride>());
            Assert.AreEqual(0, _bus.CountOf<ShowOnboardingTooltip>());
            Assert.AreEqual(0, _bus.CountOf<ForceFirstSegmentPodCarrier>());
            c.Dispose();
        }
    }
}
