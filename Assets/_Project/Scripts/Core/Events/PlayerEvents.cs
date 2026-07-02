using UnityEngine;

namespace KaijuBreaker.Core
{
    /// <summary>
    /// The player ship was hit by an enemy bullet. Republished by the BulletSim
    /// bridge from the ECS <see cref="HitEvent"/> queue (ADR-0002 §4). Consumed by
    /// the run/health system, GameFeel (damage feedback), and UI.
    /// </summary>
    public readonly struct PlayerHit : IGameEvent
    {
        public readonly Vector2 WorldPosition;

        public PlayerHit(Vector2 worldPosition)
        {
            WorldPosition = worldPosition;
        }
    }
}
