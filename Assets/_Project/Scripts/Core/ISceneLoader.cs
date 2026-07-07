using System;

namespace KaijuBreaker.Core
{
    /// <summary>
    /// Scene lifecycle abstraction so gameplay systems can request the boss arena to preload without
    /// referencing <c>UnityEngine.SceneManagement</c> directly (ADR-0005: only <c>App</c> touches
    /// SceneManager). Implemented in <c>App</c>; injected into the Stage flow (Story 006). A test double
    /// invokes <paramref name="onComplete"/> synchronously (immediate) or on demand (delayed load).
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// Begin an additive async load of <paramref name="sceneName"/>; invoke <paramref name="onComplete"/>
        /// once the scene is loaded and ready (meta-progression / stage-system.md §G.1.3 pre-boss preload).
        /// </summary>
        void LoadAdditiveAsync(string sceneName, Action onComplete);

        /// <summary>Begin an additive async unload of <paramref name="sceneName"/> (after the hunt ends).</summary>
        void UnloadAsync(string sceneName, Action onComplete);
    }
}
