using System.Collections.Generic;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.App
{
    /// <summary>Sound-effect identifiers, mapped to WAV clips under <c>Resources/Sfx</c>.</summary>
    public enum SfxId { Shoot, EnemyHit, EnemyExplode, PlayerHit, PartBreak, Pickup }

    /// <summary>
    /// Minimal presentation-layer sound sink. Loads the (free, procedurally-generated) SFX clips from Resources
    /// and plays them on the key game beats — some via the event bus (player hit / part break / elite kill / pod
    /// grab), some via direct calls from the scene glue (shoot, trash explode). Created + owned by
    /// <see cref="GameBootstrap"/> (composition root), not a singleton; systems reach it through the bootstrap.
    /// Throttles the high-frequency shoot/explode sounds so dense action doesn't clip the mixer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SfxPlayer : MonoBehaviour
    {
        private static readonly (SfxId id, string res)[] Manifest =
        {
            (SfxId.Shoot,        "Sfx/player_shoot"),
            (SfxId.EnemyHit,     "Sfx/enemy_hit"),
            (SfxId.EnemyExplode, "Sfx/enemy_explode"),
            (SfxId.PlayerHit,    "Sfx/player_hit"),
            (SfxId.PartBreak,    "Sfx/part_break"),
            (SfxId.Pickup,       "Sfx/pickup"),
        };

        private readonly Dictionary<SfxId, AudioClip> _clips = new Dictionary<SfxId, AudioClip>();
        private AudioSource _src;
        private IEventBus _bus;
        private float _lastShoot, _lastExplode;
        private const float ShootMinInterval = 0.05f;
        private const float ExplodeMinInterval = 0.03f;

        /// <summary>Load clips, create the AudioSource, and subscribe to the bus beats.</summary>
        public void Init(IEventBus bus)
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f; // 2D

            foreach (var (id, res) in Manifest)
            {
                var clip = Resources.Load<AudioClip>(res);
                if (clip != null) _clips[id] = clip;
                else Debug.LogWarning($"[SfxPlayer] Missing SFX clip: Resources/{res}");
            }

            _bus = bus;
            if (_bus != null)
            {
                _bus.Subscribe<PlayerHit>(OnPlayerHit);
                _bus.Subscribe<PartBroke>(OnPartBroke);
                _bus.Subscribe<EliteKilled>(OnEliteKilled);
                _bus.Subscribe<WeaponPodGrabbed>(OnPodGrabbed);
            }
        }

        private void OnDestroy()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<PlayerHit>(OnPlayerHit);
            _bus.Unsubscribe<PartBroke>(OnPartBroke);
            _bus.Unsubscribe<EliteKilled>(OnEliteKilled);
            _bus.Unsubscribe<WeaponPodGrabbed>(OnPodGrabbed);
        }

        /// <summary>Play a one-shot at <paramref name="volume"/> (no-op if the clip is missing).</summary>
        public void Play(SfxId id, float volume = 1f)
        {
            if (_src == null) return;
            if (_clips.TryGetValue(id, out var clip) && clip != null) _src.PlayOneShot(clip, volume);
        }

        /// <summary>Throttled primary-fire blip (primary auto-fires ~8×/s; keep it from stacking).</summary>
        public void PlayShoot()
        {
            float now = Time.unscaledTime;
            if (now - _lastShoot < ShootMinInterval) return;
            _lastShoot = now;
            Play(SfxId.Shoot, 0.35f);
        }

        /// <summary>Throttled trash-explosion (many mobs can die on the same frame).</summary>
        public void PlayEnemyExplode(float volume = 0.7f)
        {
            float now = Time.unscaledTime;
            if (now - _lastExplode < ExplodeMinInterval) return;
            _lastExplode = now;
            Play(SfxId.EnemyExplode, volume);
        }

        private void OnPlayerHit(PlayerHit e) => Play(SfxId.PlayerHit, 0.9f);
        private void OnPartBroke(PartBroke e) => Play(SfxId.PartBreak, 1f);
        private void OnEliteKilled(EliteKilled e) => PlayEnemyExplode(0.9f);
        private void OnPodGrabbed(WeaponPodGrabbed e) => Play(SfxId.Pickup, 0.9f);
    }
}
