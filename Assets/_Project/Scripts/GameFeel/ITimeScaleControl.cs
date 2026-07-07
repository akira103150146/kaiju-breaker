namespace KaijuBreaker.GameFeel
{
    /// <summary>
    /// Abstraction over the global time scale so the feel systems (hitstop, slow-mo) are unit-testable
    /// without touching <c>UnityEngine.Time.timeScale</c>. The App-layer adapter maps <see cref="TimeScale"/>
    /// onto <c>Time.timeScale</c>; tests use a fake to assert freeze/restore arithmetically.
    /// </summary>
    public interface ITimeScaleControl
    {
        /// <summary>The current global time scale (0 = frozen, 1 = normal).</summary>
        float TimeScale { get; set; }
    }
}
