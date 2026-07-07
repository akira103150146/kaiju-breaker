using System;
using KaijuBreaker.Core;
using KaijuBreaker.GameFeel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KaijuBreaker.App
{
    /// <summary>
    /// Maps the game-feel time-scale abstraction onto Unity's <c>Time.timeScale</c> (ADR-0005: only App
    /// touches Unity subsystems). Reading <see cref="TimeScale"/> returns the live global scale.
    /// </summary>
    public sealed class UnityTimeScaleControl : ITimeScaleControl
    {
        public float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }
    }

    /// <summary>
    /// <see cref="ISceneLoader"/> over Unity's additive scene loading (ADR-0005). The boss-arena scenes are
    /// kaiju-roster content; until they exist, a missing scene completes immediately so the run flow still
    /// advances (the lull gate then just depends on its timer).
    /// </summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public void LoadAdditiveAsync(string sceneName, Action onComplete)
        {
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                onComplete?.Invoke(); // scene not present yet (kaiju-roster content) → don't block the run
                return;
            }
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null) { onComplete?.Invoke(); return; }
            op.completed += _ => onComplete?.Invoke();
        }

        public void UnloadAsync(string sceneName, Action onComplete)
        {
            if (string.IsNullOrEmpty(sceneName) || !SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                onComplete?.Invoke();
                return;
            }
            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null) { onComplete?.Invoke(); return; }
            op.completed += _ => onComplete?.Invoke();
        }
    }
}
