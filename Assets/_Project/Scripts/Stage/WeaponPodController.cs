using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// A cycling weapon pod (stage-system.md §F.1/§F.2). Descends into the player's reachable band, dwells
    /// there for <see cref="PodDropConfig.PodDwellTimeSeconds"/> while cycling which weapon it offers (so the
    /// player can wait for the one they want — the agency guarantee), then fades out. Touching it grabs the
    /// CURRENTLY-displayed weapon and publishes <see cref="WeaponPodGrabbed"/>; Weapons (equip) and Meta
    /// (permanent unlock) subscribe independently — the pod references neither system (ADR-0005). All timing/
    /// geometry comes from the injected <see cref="PodDropConfig"/> (ADR-0003, no hardcoded values).
    ///
    /// <para><b>Reconciliation:</b> the committed <see cref="WeaponPodGrabbed"/> carries only the weapon
    /// (no <c>isFirstTime</c>) — Meta derives first-pickup itself (meta-save Story 007), so the pod needs no
    /// ISaveService. Pickup is exposed as <see cref="Collect"/> so the physics trigger and tests share one
    /// path.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponPodController : MonoBehaviour
    {
        /// <summary>Lifecycle phase of the pod.</summary>
        public enum PodPhase { Descending, Dwelling, Despawning }

        private IEventBus _bus;
        private PodDropConfig _config;
        private List<WeaponId> _pool;
        private float _bandMinY, _bandMaxY, _centerY;
        private float _dwellTimer, _cycleTimer, _despawnTimer, _bobPhase;
        private int _displayIndex;
        private bool _collected;

        /// <summary>Which pool (laser/missile) this pod belongs to.</summary>
        public PodType PodType { get; private set; }

        /// <summary>Current phase.</summary>
        public PodPhase Phase { get; private set; } = PodPhase.Descending;

        /// <summary>The weapon currently displayed (and what a pickup grabs). Cycles during Dwelling.</summary>
        public WeaponId CurrentWeapon => _pool != null && _pool.Count > 0 ? _pool[_displayIndex] : default;

        /// <summary>Zero-based index of the displayed weapon within the pool.</summary>
        public int DisplayIndex => _displayIndex;

        /// <summary>
        /// Configure the pod after Instantiate. <paramref name="bandMinY"/>/<paramref name="bandMaxY"/> is the
        /// reachable band the pod descends into and dwells within (bob is clamped inside it).
        /// </summary>
        public void Init(IEventBus bus, PodDropConfig config, List<WeaponId> weaponPool, PodType podType,
                         float bandMinY, float bandMaxY)
        {
            _bus = bus;
            _config = config;
            _pool = weaponPool ?? new List<WeaponId>();
            PodType = podType;
            _bandMinY = Mathf.Min(bandMinY, bandMaxY);
            _bandMaxY = Mathf.Max(bandMinY, bandMaxY);
            _centerY = (_bandMinY + _bandMaxY) * 0.5f;
            _displayIndex = 0;
            Phase = PodPhase.Descending;
        }

        private void Update()
        {
            if (_config == null || _collected) return;
            float dt = Time.deltaTime;
            _despawnTimer += dt;

            switch (Phase)
            {
                case PodPhase.Descending: TickDescend(dt); break;
                case PodPhase.Dwelling: TickDwell(dt); break;
                case PodPhase.Despawning: TickDespawn(dt); break;
            }

            // Absolute safety net: never outlive the despawn cap.
            if (_despawnTimer >= _config.PodDespawnAfterSeconds && Phase != PodPhase.Despawning)
                Phase = PodPhase.Despawning;
        }

        private void TickDescend(float dt)
        {
            transform.position += Vector3.down * (_config.PodDescendSpeedPxPerSec * dt);
            if (transform.position.y <= _bandMaxY)
            {
                var p = transform.position;
                p.y = _centerY;
                transform.position = p;
                Phase = PodPhase.Dwelling;
                _dwellTimer = 0f;
                _cycleTimer = 0f;
                _bobPhase = 0f;
            }
        }

        private void TickDwell(float dt)
        {
            _dwellTimer += dt;

            // Bob around centre, clamped to the reachable band so the pod is always grabbable.
            _bobPhase += dt * (2f * Mathf.PI / 2f); // ~2s period
            float y = _centerY + Mathf.Sin(_bobPhase) * _config.PodBobAmplitudePx;
            var p = transform.position;
            p.y = Mathf.Clamp(y, _bandMinY, _bandMaxY);
            transform.position = p;

            // Cycle the displayed weapon (skip if the pool has a single weapon — static display).
            if (_pool.Count > 1)
            {
                _cycleTimer += dt;
                if (_cycleTimer >= _config.PodCycleIntervalSeconds)
                {
                    _cycleTimer -= _config.PodCycleIntervalSeconds;
                    _displayIndex = (_displayIndex + 1) % _pool.Count;
                }
            }

            if (_dwellTimer >= _config.PodDwellTimeSeconds)
                Phase = PodPhase.Despawning;
        }

        private void TickDespawn(float dt)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var c = sr.color;
                c.a = Mathf.Max(0f, c.a - dt / 0.5f); // 0.5s fade
                sr.color = c;
                if (c.a <= 0f) Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Grab the currently-displayed weapon: publish <see cref="WeaponPodGrabbed"/> and vanish immediately
        /// (no explosion, no waiting for dwell to end). Called by the physics trigger and by tests.
        /// </summary>
        public void Collect()
        {
            if (_collected || _pool == null || _pool.Count == 0) return;
            _collected = true;
            _bus?.Publish(new WeaponPodGrabbed(CurrentWeapon));
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.CompareTag("Player")) Collect();
        }
    }
}
