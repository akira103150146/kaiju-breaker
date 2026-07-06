using UnityEngine;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Bridges Unity's application-lifecycle callbacks to the save safety net
    /// (meta-progression-system.md §C.5.1): on suspend/quit it forces a blocking flush of the latest state so
    /// no banked progress is lost when the OS backgrounds or kills the process. Wired by the App composition
    /// root via <see cref="Bind"/> (no <c>FindObjectOfType</c> / singleton — DI per ADR-0005).
    ///
    /// <para><b>Test evidence:</b> the lifecycle callbacks require the MonoBehaviour runtime, so they are
    /// covered by manual QA (`production/qa/evidence/save-autosave-suspend-evidence.md`) rather than an
    /// EditMode test; the <see cref="MetaSaveService.FlushSyncNow"/> logic it calls is EditMode-tested.</para>
    /// </summary>
    public sealed class MetaSaveLifecycleBridge : MonoBehaviour
    {
        private MetaSaveService _service;

        /// <summary>Inject the save service (called once by the App composition root).</summary>
        public void Bind(MetaSaveService service) => _service = service;

        private void OnApplicationPause(bool pauseStatus)
        {
            // pauseStatus == true → app is being backgrounded (the critical mobile safety net).
            if (pauseStatus) _service?.FlushSyncNow();
        }

        private void OnApplicationQuit() => _service?.FlushSyncNow();
    }
}
