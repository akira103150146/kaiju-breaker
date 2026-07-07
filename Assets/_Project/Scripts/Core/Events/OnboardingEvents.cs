namespace KaijuBreaker.Core
{
    // Stage 1 onboarding events (stage-system.md §H.2). Published/consumed inside the Stage system + UI.

    /// <summary>
    /// Emitted by the Stage flow as it spawns a wave, tagging whether it is the fixed intro segment and which
    /// wave index. The <c>OnboardingController</c> subscribes to apply the D1 intro slow-down (§H.2 rule 1).
    /// </summary>
    public readonly struct IntroSegmentWaveSpawning : IGameEvent
    {
        public readonly bool IsIntroSegment;
        public readonly int WaveIndex;

        public IntroSegmentWaveSpawning(bool isIntroSegment, int waveIndex)
        {
            IsIntroSegment = isIntroSegment;
            WaveIndex = waveIndex;
        }
    }

    /// <summary>
    /// Requests that a spawning enemy type use a scaled move speed (stage-system.md §H.2 rule 1). Published by
    /// the <c>OnboardingController</c>; the wave spawner applies it at spawn time. <see cref="EnemyId"/> is the
    /// enemy's string id (e.g. "ram_grub").
    /// </summary>
    public readonly struct EnemySpeedOverride : IGameEvent
    {
        public readonly string EnemyId;
        public readonly float SpeedMultiplier;

        public EnemySpeedOverride(string enemyId, float speedMultiplier)
        {
            EnemyId = enemyId;
            SpeedMultiplier = speedMultiplier;
        }
    }

    /// <summary>
    /// Forces a guaranteed pod carrier into a run's first escalating segment (stage-system.md §H.2 rule 2), so
    /// Stage 1 always offers a pickup early even when the drawn first segment has no elite. Published by the
    /// <c>OnboardingController</c>; the wave/pod spawner appends the pod.
    /// </summary>
    public readonly struct ForceFirstSegmentPodCarrier : IGameEvent
    {
        public readonly int SegmentIndex;
        public readonly PodType PoolType;

        public ForceFirstSegmentPodCarrier(int segmentIndex, PodType poolType)
        {
            SegmentIndex = segmentIndex;
            PoolType = poolType;
        }
    }

    /// <summary>
    /// on_show_onboarding_tooltip — a one-time HUD hint (stage-system.md §H.2 rule 3). Published by the
    /// <c>OnboardingController</c> on the first-ever pod pickup; the UI layer subscribes to render it
    /// (Stage never touches UI directly, ADR-0002/0005).
    /// </summary>
    public readonly struct ShowOnboardingTooltip : IGameEvent
    {
        public readonly string Text;
        public readonly float DurationSec;

        public ShowOnboardingTooltip(string text, float durationSec)
        {
            Text = text;
            DurationSec = durationSec;
        }
    }
}
