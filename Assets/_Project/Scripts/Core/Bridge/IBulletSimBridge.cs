namespace KaijuBreaker.Core
{
    /// <summary>
    /// The single translation layer between the ECS/Burst bullet simulation and the
    /// managed <see cref="IEventBus"/> (ADR-0002 §4, architecture.md §5.3).
    ///
    /// The concrete bridge (implemented in the BulletSim assembly) owns the
    /// <c>NativeQueue&lt;HitEvent&gt;</c> that Burst jobs write to; each frame the main
    /// thread calls <see cref="Pump"/> to drain it and republish each hit onto the bus
    /// as a <see cref="MissileHit"/> or <see cref="PlayerHit"/>.
    ///
    /// Keeping this as a Core interface means the DOTS↔Mono boundary is a single,
    /// testable, replaceable seam — ADR-0001's Mono-pool fallback swaps only this impl.
    /// </summary>
    public interface IBulletSimBridge
    {
        /// <summary>
        /// Drain this frame's queued hit events and republish them onto the event bus.
        /// Must be called on the main thread after the bullet sim job has completed.
        /// </summary>
        void Pump();
    }
}
