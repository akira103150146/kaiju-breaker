// SCAFFOLD STUB — structural placeholder only, not functional.
// See docs/architecture/architecture.md §5.2 and ADR-0002 for contract details.
using UnityEngine;

namespace KaijuBreaker.Core
{
    /// <summary>
    /// Read-only query interface for kaiju part state (ADR-0002, ADR-0005).
    /// Defined in Core so that Weapons, UI, and GameFeel can read part state
    /// without taking a direct assembly reference to KaijuBreaker.KaijuParts.
    /// App (composition root) injects the concrete KaijuParts implementation.
    /// </summary>
    public interface IPartStateQuery
    {
        PartHeatState GetHeatState(int partId);
        Vector3 GetWorldPosition(int partId);
        bool IsPartAlive(int partId);
    }

    /// <summary>
    /// Mirrors the three-state break_quality carrier described in architecture.md §5.2.
    /// KaijuParts sets this; Economy reads it to compute shard/core yield.
    /// </summary>
    public enum PartHeatState
    {
        Normal,
        Softened,
        SoftenedStaggered
    }

    /// <summary>
    /// Read-only difficulty values needed by systems that only depend on Core.
    /// Difficulty assembly provides the concrete implementation; App injects it.
    /// </summary>
    public interface IDifficultyProvider
    {
        float BulletDensityMult { get; }
        float EnemyCountMult { get; }
    }

    /// <summary>
    /// Save service abstraction so systems can trigger autosave events
    /// without referencing KaijuBreaker.Meta directly.
    /// </summary>
    public interface ISaveService
    {
        void EnqueueAutosave();
        void FlushSync(); // called on app suspend
    }
}
