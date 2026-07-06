using System;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Stage Story 001 — Run 狀態機 LOADOUT → STAGE → BOSS → RESULTS (stage-system.md TR-stage-007; ADR-0005).
    /// Drives the pure-C# <see cref="RunController"/> with a <see cref="RecordingEventBus"/> (real synchronous
    /// delivery + boxed recording) and a <see cref="RecordingSaveService"/> counting autosave enqueues, so no
    /// scene or Unity runtime is needed.
    ///
    /// <para><b>Reconciliations vs story text (surfaced for review):</b>
    /// (1) The committed save interface is <c>ISaveService.EnqueueAutosave()</c>, not the story's aspirational
    /// <c>EnqueueSave()</c>. (2) The committed core-break event is <see cref="BossCoreBroke"/>, not the story's
    /// <c>BossCoreBreak</c>. (3) Full-clear status for <see cref="HuntEnded.IsAllPartsBroken"/> is derived from
    /// distinct <see cref="PartBroke"/> ids counted during BOSS vs the total passed to
    /// <see cref="RunController.EnterBoss(int)"/> — the core's PartBroke fires the same frame just before
    /// BossCoreBroke, so it is counted at settlement. (4) Same-frame PartBroke autosave coalescing (depth-1)
    /// is the meta-save layer's job (ADR-0004); RunController enqueues per event, so the count here is a lower
    /// bound assertion (≥), matching AC-4.</para>
    /// </summary>
    [TestFixture]
    public sealed class RunStateMachineTests
    {
        private RecordingEventBus _bus;
        private RecordingSaveService _save;
        private RunController _run;

        [SetUp]
        public void SetUp()
        {
            _bus = new RecordingEventBus();
            _save = new RecordingSaveService();
            _run = new RunController(_bus, _save);
        }

        [TearDown]
        public void TearDown() => _run.Dispose();

        // ── Fixture helpers ───────────────────────────────────────────────────

        private static PartBroke MakePartBroke(int partId, PartType type = PartType.Normal) =>
            new PartBroke(partId, kaijuId: 1, type, Vector2.zero, dropTableId: 0,
                          BreakQuality.Normal, adjacencyIds: null, isChainBreak: false);

        /// <summary>Advance LOADOUT → STAGE → BOSS via the normal path, then clear recorded events.</summary>
        private void AdvanceToBoss(int totalBreakableParts = 0)
        {
            _bus.Publish(new LoadoutConfirmed());
            _run.EnterBoss(totalBreakableParts);
            _bus.Clear();
        }

        // ── AC-1: LOADOUT → STAGE ─────────────────────────────────────────────

        [Test]
        public void test_run_initial_state_is_loadout()
        {
            Assert.AreEqual(RunState.Loadout, _run.CurrentState);
        }

        [Test]
        public void test_run_loadout_confirmed_transitions_to_stage()
        {
            // Act
            _bus.Publish(new LoadoutConfirmed());

            // Assert
            Assert.AreEqual(RunState.Stage, _run.CurrentState);
            var changes = _bus.Events<RunStateChanged>();
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(RunState.Loadout, changes[0].From);
            Assert.AreEqual(RunState.Stage, changes[0].To);
        }

        [Test]
        public void test_run_loadout_confirmed_outside_loadout_throws()
        {
            // Arrange — move to STAGE first
            _bus.Publish(new LoadoutConfirmed());

            // Act + Assert — re-confirming from STAGE is illegal
            Assert.Throws<InvalidOperationException>(() => _bus.Publish(new LoadoutConfirmed()));
        }

        // ── AC-2: EnterBoss() STAGE → BOSS ────────────────────────────────────

        [Test]
        public void test_run_enter_boss_transitions_stage_to_boss()
        {
            // Arrange
            _bus.Publish(new LoadoutConfirmed());
            _bus.Clear();

            // Act
            _run.EnterBoss();

            // Assert
            Assert.AreEqual(RunState.Boss, _run.CurrentState);
            var changes = _bus.Events<RunStateChanged>();
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(RunState.Stage, changes[0].From);
            Assert.AreEqual(RunState.Boss, changes[0].To);
        }

        [Test]
        public void test_run_enter_boss_from_loadout_throws()
        {
            Assert.Throws<InvalidOperationException>(() => _run.EnterBoss());
        }

        // ── AC-3: BOSS → RESULTS via BossCoreBroke ────────────────────────────

        [Test]
        public void test_run_boss_core_broke_transitions_boss_to_results_and_emits_hunt_ended()
        {
            // Arrange
            AdvanceToBoss();
            int savesBefore = _save.EnqueueCalls;

            // Act
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero));

            // Assert
            Assert.AreEqual(RunState.Results, _run.CurrentState);
            Assert.AreEqual(1, _bus.CountOf<HuntEnded>(), "exactly one HuntEnded at settlement");
            Assert.GreaterOrEqual(_save.EnqueueCalls - savesBefore, 1, "settlement enqueues an autosave");
        }

        [Test]
        public void test_run_boss_core_broke_outside_boss_is_ignored()
        {
            // Arrange — in STAGE, not BOSS
            _bus.Publish(new LoadoutConfirmed());
            _bus.Clear();

            // Act — a stray/mid core break must not end the run
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero));

            // Assert
            Assert.AreEqual(RunState.Stage, _run.CurrentState);
            Assert.AreEqual(0, _bus.CountOf<HuntEnded>());
        }

        [Test]
        public void test_run_full_clear_reports_all_parts_broken_true()
        {
            // Arrange — boss has 3 breakable parts (2 normal + the core)
            AdvanceToBoss(totalBreakableParts: 3);

            // Act — break every part; the core's PartBroke precedes BossCoreBroke the same frame
            _bus.Publish(MakePartBroke(10));
            _bus.Publish(MakePartBroke(11));
            _bus.Publish(MakePartBroke(12, PartType.BossCore));
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero));

            // Assert
            var ended = _bus.Events<HuntEnded>();
            Assert.AreEqual(1, ended.Count);
            Assert.IsTrue(ended[0].IsAllPartsBroken, "all 3 parts broken → full clear");
        }

        [Test]
        public void test_run_partial_clear_reports_all_parts_broken_false()
        {
            // Arrange — boss has 3 breakable parts but the player only breaks the core
            AdvanceToBoss(totalBreakableParts: 3);

            // Act
            _bus.Publish(MakePartBroke(12, PartType.BossCore));
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero));

            // Assert
            var ended = _bus.Events<HuntEnded>();
            Assert.AreEqual(1, ended.Count);
            Assert.IsFalse(ended[0].IsAllPartsBroken, "only 1 of 3 parts broken → not a full clear");
        }

        // ── AC-4: Autosave trigger points ─────────────────────────────────────

        [Test]
        public void test_run_autosave_enqueued_at_every_trigger_point()
        {
            // Arrange + Act — a full run touching every documented trigger
            _bus.Publish(new LoadoutConfirmed());               // trigger: on_loadout_confirmed
            _run.EnterBoss(totalBreakableParts: 1);
            _bus.Publish(new WeaponPodGrabbed(WeaponId.L1));    // trigger: weapon pod (x2)
            _bus.Publish(new WeaponPodGrabbed(WeaponId.M1));
            _bus.Publish(MakePartBroke(20, PartType.BossCore)); // trigger: on_part_break
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero)); // trigger: hunt end

            // Assert — at least one enqueue per trigger point (≥4; coalescing is meta-save's job)
            Assert.GreaterOrEqual(_save.EnqueueCalls, 4,
                "each of loadout-confirm, pod-grab, part-break and hunt-end enqueues ≥1 autosave");
        }

        [Test]
        public void test_run_weapon_pod_grabbed_enqueues_autosave()
        {
            // Arrange
            AdvanceToBoss();
            int before = _save.EnqueueCalls;

            // Act
            _bus.Publish(new WeaponPodGrabbed(WeaponId.M2));

            // Assert
            Assert.AreEqual(before + 1, _save.EnqueueCalls);
        }

        // ── AC-5: invalid-transition guards + full loop ───────────────────────

        [Test]
        public void test_run_confirm_results_from_results_returns_to_loadout()
        {
            // Arrange — drive all the way to RESULTS
            AdvanceToBoss();
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero));
            Assert.AreEqual(RunState.Results, _run.CurrentState);
            _bus.Clear();

            // Act
            _run.ConfirmResults();

            // Assert
            Assert.AreEqual(RunState.Loadout, _run.CurrentState);
            var changes = _bus.Events<RunStateChanged>();
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(RunState.Results, changes[0].From);
            Assert.AreEqual(RunState.Loadout, changes[0].To);
        }

        [Test]
        public void test_run_enter_boss_from_results_throws()
        {
            // Arrange
            AdvanceToBoss();
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero));
            Assert.AreEqual(RunState.Results, _run.CurrentState);

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() => _run.EnterBoss());
        }

        [Test]
        public void test_run_confirm_results_outside_results_throws()
        {
            Assert.Throws<InvalidOperationException>(() => _run.ConfirmResults());
        }

        [Test]
        public void test_run_full_loop_returns_to_loadout_and_can_run_again()
        {
            // A second full lap proves state resets cleanly (no leftover part counts, no stuck subscriptions).
            AdvanceToBoss(totalBreakableParts: 1);
            _bus.Publish(MakePartBroke(1, PartType.BossCore));
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero));
            _run.ConfirmResults();
            Assert.AreEqual(RunState.Loadout, _run.CurrentState);

            // Second lap — partial clear this time
            _bus.Clear();
            _bus.Publish(new LoadoutConfirmed());
            _run.EnterBoss(totalBreakableParts: 2);
            _bus.Publish(MakePartBroke(1, PartType.BossCore));
            _bus.Publish(new BossCoreBroke(kaijuId: 1, worldPosition: Vector2.zero));

            var ended = _bus.Events<HuntEnded>();
            Assert.AreEqual(1, ended.Count);
            Assert.IsFalse(ended[0].IsAllPartsBroken, "second run broke 1 of 2 parts → not a full clear");
            Assert.AreEqual(RunState.Results, _run.CurrentState);
        }
    }
}
