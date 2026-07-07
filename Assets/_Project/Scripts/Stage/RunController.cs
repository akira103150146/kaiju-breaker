using System;
using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Drives the run/hunt lifecycle state machine LOADOUT → STAGE → BOSS → RESULTS → LOADOUT
    /// (stage-system.md TR-stage-007; ADR-0005). A pure C# class with no Unity API dependency —
    /// event-driven and constructor-injected, so it is fully EditMode-testable with fake
    /// <see cref="IEventBus"/> + <see cref="ISaveService"/> doubles.
    ///
    /// Communication is bus-only (ADR-0002): it subscribes to <see cref="LoadoutConfirmed"/>,
    /// <see cref="BossCoreBroke"/>, <see cref="PartBroke"/>, <see cref="WeaponPodGrabbed"/> and
    /// publishes <see cref="RunStateChanged"/> on every transition plus <see cref="HuntEnded"/> at
    /// settlement. It references no other Feature system directly (ADR-0005) and holds no static state.
    ///
    /// Autosave: every legal transition and every mid-run banking point (pod pickup, part break)
    /// enqueues an autosave via <see cref="ISaveService.EnqueueAutosave"/>. Coalescing same-frame
    /// enqueues to a depth-1 queue is the responsibility of the meta-save layer (ADR-0004), not here.
    /// </summary>
    public sealed class RunController
    {
        private readonly IEventBus _bus;
        private readonly ISaveService _save;

        // Cached delegates so Subscribe/Unsubscribe use the same references (TypedEventBus identity).
        private readonly Action<LoadoutConfirmed> _onLoadoutConfirmed;
        private readonly Action<BossCoreBroke> _onBossCoreBroke;
        private readonly Action<PartBroke> _onPartBroke;
        private readonly Action<WeaponPodGrabbed> _onWeaponPodGrabbed;

        // Full-clear tracking: distinct parts broken while in BOSS, vs the boss's total breakable count.
        private readonly HashSet<int> _brokenBossParts = new HashSet<int>();
        private int _totalBreakableParts;

        /// <summary>The current run phase. Starts at <see cref="RunState.Loadout"/>.</summary>
        public RunState CurrentState { get; private set; }

        /// <summary>
        /// Construct and subscribe to the run-flow events. The composition root (<c>App</c>) owns the
        /// lifetime; call <see cref="Dispose"/> on teardown to unsubscribe.
        /// </summary>
        /// <param name="bus">The application event bus (ADR-0002). Must not be null.</param>
        /// <param name="save">Save abstraction for autosave enqueue points. Must not be null.</param>
        public RunController(IEventBus bus, ISaveService save)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _save = save ?? throw new ArgumentNullException(nameof(save));

            _onLoadoutConfirmed = OnLoadoutConfirmed;
            _onBossCoreBroke = OnBossCoreBroke;
            _onPartBroke = OnPartBroke;
            _onWeaponPodGrabbed = OnWeaponPodGrabbed;

            _bus.Subscribe(_onLoadoutConfirmed);
            _bus.Subscribe(_onBossCoreBroke);
            _bus.Subscribe(_onPartBroke);
            _bus.Subscribe(_onWeaponPodGrabbed);

            CurrentState = RunState.Loadout;
        }

        /// <summary>
        /// Advance STAGE → BOSS. Called by the Stage flow scheduler once all escalating wave segments and
        /// the pre-boss lull have completed — NOT event-driven, to prevent accidental early entry.
        /// </summary>
        /// <param name="totalBreakableParts">
        /// The boss's total breakable part count (including the core). Used to decide
        /// <see cref="HuntEnded.IsAllPartsBroken"/> at settlement. Pass 0 when unknown — a full clear then
        /// cannot be confirmed and <see cref="HuntEnded"/> reports false.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown when not currently in <see cref="RunState.Stage"/>.</exception>
        public void EnterBoss(int totalBreakableParts = 0)
        {
            if (CurrentState != RunState.Stage)
                throw new InvalidOperationException($"EnterBoss() is only valid from STAGE (was {CurrentState}).");

            _totalBreakableParts = totalBreakableParts;
            _brokenBossParts.Clear();
            TransitionTo(RunState.Boss);
        }

        /// <summary>
        /// End the run in defeat from STAGE or BOSS (the player died). Transitions to RESULTS and settles the
        /// hunt as a non-full-clear. No-op outside STAGE/BOSS so a stray call cannot corrupt the state machine.
        /// (The win path stays exclusively <see cref="BossCoreBroke"/>.)
        /// </summary>
        public void Defeat()
        {
            if (CurrentState != RunState.Stage && CurrentState != RunState.Boss) return;
            TransitionTo(RunState.Results);
            _bus.Publish(new HuntEnded(false));
        }

        /// <summary>
        /// Confirm the RESULTS screen and return to LOADOUT for the next run (loadout scene reloads).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when not currently in <see cref="RunState.Results"/>.</exception>
        public void ConfirmResults()
        {
            if (CurrentState != RunState.Results)
                throw new InvalidOperationException($"ConfirmResults() is only valid from RESULTS (was {CurrentState}).");

            TransitionTo(RunState.Loadout);
        }

        /// <summary>Unsubscribe from the bus. Call on teardown (App owns lifetime).</summary>
        public void Dispose()
        {
            _bus.Unsubscribe(_onLoadoutConfirmed);
            _bus.Unsubscribe(_onBossCoreBroke);
            _bus.Unsubscribe(_onPartBroke);
            _bus.Unsubscribe(_onWeaponPodGrabbed);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnLoadoutConfirmed(LoadoutConfirmed _)
        {
            if (CurrentState != RunState.Loadout)
                throw new InvalidOperationException($"LoadoutConfirmed received outside LOADOUT (was {CurrentState}).");

            TransitionTo(RunState.Stage);
        }

        private void OnBossCoreBroke(BossCoreBroke _)
        {
            // Ignore outside BOSS — a mid-encounter core or a stray publish must not end the run.
            if (CurrentState != RunState.Boss) return;

            bool fullClear = _totalBreakableParts > 0 && _brokenBossParts.Count >= _totalBreakableParts;
            TransitionTo(RunState.Results);
            _bus.Publish(new HuntEnded(fullClear));
        }

        private void OnPartBroke(PartBroke evt)
        {
            // Count toward the boss full-clear only while fighting the boss; the core's PartBroke fires
            // the same frame just before BossCoreBroke, so it is already counted at settlement time.
            if (CurrentState == RunState.Boss)
                _brokenBossParts.Add(evt.PartId);

            _save.EnqueueAutosave(); // autosave-on-bank (stage-system.md TR-stage-007)
        }

        private void OnWeaponPodGrabbed(WeaponPodGrabbed _)
        {
            _save.EnqueueAutosave();
        }

        // ── Transition core ───────────────────────────────────────────────────

        /// <summary>
        /// Apply a state transition: record it, publish <see cref="RunStateChanged"/>, and enqueue an
        /// autosave at the new checkpoint. Callers are responsible for validating legality before calling.
        /// </summary>
        private void TransitionTo(RunState next)
        {
            RunState prev = CurrentState;
            CurrentState = next;
            _bus.Publish(new RunStateChanged(prev, next));
            _save.EnqueueAutosave();
        }
    }
}
