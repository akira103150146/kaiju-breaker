using System;
using System.Threading;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Background save queue with depth-1 overwrite semantics (meta-progression-system.md §C.5.3; ADR-0004 §5).
    /// The main thread calls <see cref="EnqueueSave"/> — a non-blocking deep-copy snapshot store; a worker
    /// thread drains the newest pending snapshot and hands it to <see cref="AtomicSaveWriter"/>. Only the most
    /// recent snapshot survives (older pending snapshots are simply overwritten), so a burst of credits
    /// collapses to one write. Suspend/quit paths use the blocking <see cref="SyncWrite"/> safety net.
    ///
    /// <para>The drain step is exposed as <see cref="DrainOnce"/> so the overwrite / deep-copy / write
    /// behaviour is unit-testable deterministically without racing the live thread; <see cref="Start"/> /
    /// <see cref="Stop"/> run the same drain on a real background thread.</para>
    /// </summary>
    public sealed class SaveWorker
    {
        private readonly AtomicSaveWriter _writer;
        private readonly int _idleMs;
        private readonly object _lock = new object();

        private SaveData _pending;      // guarded by _lock; null = nothing to write
        private Thread _thread;
        private volatile bool _running;

        public SaveWorker(AtomicSaveWriter writer, int idleMs)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _idleMs = Math.Max(1, idleMs);
        }

        /// <summary>True while a snapshot is queued and not yet written. Test/diagnostic aid.</summary>
        public bool HasPending { get { lock (_lock) { return _pending != null; } } }

        /// <summary>Number of <see cref="EnqueueSave"/> calls (diagnostic; counts enqueues, not writes).</summary>
        public int EnqueueCount { get; private set; }

        /// <summary>
        /// Queue a deep-copied snapshot for background write and return immediately. If a snapshot is already
        /// pending it is replaced (depth-1 overwrite). Mutating <paramref name="state"/> afterwards does not
        /// affect the queued write (the copy is isolated).
        /// </summary>
        public void EnqueueSave(SaveData state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var copy = state.DeepCopy();
            lock (_lock) { _pending = copy; EnqueueCount++; }
        }

        /// <summary>
        /// Drain the current pending snapshot (if any) and write it atomically. Returns true if a write
        /// happened. Called by the worker loop and directly by tests.
        /// </summary>
        public bool DrainOnce()
        {
            SaveData snapshot;
            lock (_lock) { snapshot = _pending; _pending = null; }
            if (snapshot == null) return false;
            _writer.AtomicWrite(snapshot);
            return true;
        }

        /// <summary>
        /// Blocking synchronous write on the calling thread (for OnApplicationPause/Quit safety net, wired in
        /// Story 006). Writes a deep copy so the caller's object is never mutated. Returns only once
        /// <c>save.json</c> and its backup are fully written.
        /// </summary>
        public void SyncWrite(SaveData state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _writer.AtomicWrite(state.DeepCopy());
        }

        /// <summary>Start the background worker thread. Idempotent while running.</summary>
        public void Start()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(WorkerLoop) { IsBackground = true, Name = "KaijuBreaker.SaveWorker" };
            _thread.Start();
        }

        /// <summary>
        /// Stop the worker and flush any last pending snapshot before returning (no lost write on quit).
        /// </summary>
        public void Stop()
        {
            _running = false;
            _thread?.Join();
            _thread = null;
            DrainOnce(); // flush a snapshot enqueued right before shutdown
        }

        private void WorkerLoop()
        {
            while (_running)
            {
                if (!DrainOnce()) Thread.Sleep(_idleMs);
            }
        }
    }
}
