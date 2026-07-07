using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Applies Stage 1 first-run onboarding rules (stage-system.md §H.2). Pure C#, constructor-injected, and
    /// active ONLY when the run is <c>stage_01</c> — for any other stage it subscribes to nothing and is a
    /// no-op (§H.2 guardrail). Rules: (1) slow the intro segment's <c>ram_grub</c> at D1; (2) force a Primary
    /// pod carrier into the first escalating segment when it has no elite; (3) show a one-time HUD tooltip on
    /// the player's first-ever pod pickup, persisted via <see cref="ISaveService"/> flags (ADR-0004, not
    /// PlayerPrefs). Communicates only through the event bus + save interface — never touches UI or other
    /// Feature systems directly (ADR-0005).
    /// </summary>
    public sealed class OnboardingController
    {
        /// <summary>The stage id these onboarding rules apply to.</summary>
        public const string OnboardingStageId = "stage_01";

        /// <summary>Content id of the intro enemy the D1 slow-down targets.</summary>
        public const string IntroEnemyId = "ram_grub";

        /// <summary>Persistent flag key for the one-time first-pickup tooltip.</summary>
        public const string FirstPodPickupShownFlag = "first_pod_pickup_shown";

        private readonly IEventBus _bus;
        private readonly ISaveService _save;
        private readonly OnboardingConfig _config;
        private readonly IDifficultyProvider _difficulty;
        private readonly Action<IntroSegmentWaveSpawning> _onIntroWave;
        private readonly Action<WeaponPodGrabbed> _onPodGrabbed;

        /// <summary>True when the run is stage_01 and the rules are wired.</summary>
        public bool IsActive { get; }

        public OnboardingController(IEventBus bus, ISaveService save, OnboardingConfig config,
                                    IDifficultyProvider difficulty, string currentStageId)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));

            IsActive = currentStageId == OnboardingStageId;
            if (!IsActive) return; // any other stage → zero subscriptions, zero side effects

            _onIntroWave = OnIntroWave;
            _onPodGrabbed = OnWeaponPodGrabbed;
            _bus.Subscribe(_onIntroWave);
            _bus.Subscribe(_onPodGrabbed);
        }

        /// <summary>Unsubscribe on teardown (no-op when inactive).</summary>
        public void Dispose()
        {
            if (!IsActive) return;
            _bus.Unsubscribe(_onIntroWave);
            _bus.Unsubscribe(_onPodGrabbed);
        }

        /// <summary>
        /// Review the recombined run layout (called by <c>RunController</c> after <c>SegmentRecombinator</c>):
        /// if the first escalating segment has no elite (EliteWaveIndex &lt; 0), force a Primary pod carrier
        /// so Stage 1 always offers an early pickup (§H.2 rule 2). No-op when inactive or the segment already
        /// has an elite (its pod is already guaranteed by Story 004).
        /// </summary>
        public void ReviewFirstSegment(SegmentSequence sequence)
        {
            if (!IsActive || sequence == null || sequence.EscalatingSegments.Count == 0) return;
            if (sequence.EscalatingSegments[0].EliteWaveIndex < 0)
                _bus.Publish(new ForceFirstSegmentPodCarrier(segmentIndex: 0, PodType.Primary));
        }

        private void OnIntroWave(IntroSegmentWaveSpawning evt)
        {
            // Rule 1: only the intro segment's first wave, only at D1.
            if (evt.IsIntroSegment && evt.WaveIndex == 0 && _difficulty.CurrentTier == DifficultyTier.D1)
                _bus.Publish(new EnemySpeedOverride(IntroEnemyId, _config.RamGrubIntroSpeedMult));
        }

        private void OnWeaponPodGrabbed(WeaponPodGrabbed evt)
        {
            // Rule 3: show the tooltip once ever, then persist the flag so it never shows again.
            if (_save.GetFlag(FirstPodPickupShownFlag)) return;
            _bus.Publish(new ShowOnboardingTooltip(_config.TooltipText, _config.TooltipDurationSec));
            _save.SetFlag(FirstPodPickupShownFlag, true);
        }
    }
}
