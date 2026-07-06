# Story 002: Atomic Temp-Then-Rename Write & Backup

> **Epic**: 元進度與存檔系統
> **Status**: ✅ Complete (2026-07-06 — 6/6 EditMode GREEN, part of 313-case suite)
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: 2026-07-06

## Context

**GDD**: `design/gdd/meta-progression-system.md`
**Requirement**: `TR-meta-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time. Note: tr-registry.yaml not yet formalized; TR-ID inferred from GDD §H.4.)*

**ADR Governing Implementation**: ADR-0004: 存檔系統（JSON + 原子寫入 + CRC32）
**ADR Decision Summary**: Atomic disk write uses the temp-then-rename strategy: serialize to `save.tmp.json` → `flush + fsync` (ensure bytes reach disk, not just OS buffer) → `rename(tmp → save.json)` (atomic within same filesystem) → `copy(save.json → save.bak.json)`. This guarantees the disk never holds a partial or corrupted file. Background Save Worker thread with queue depth 1 (overwrite-mode newest-wins) handles all non-suspend writes; `on_app_suspend`/`on_app_quit` trigger a synchronous blocking write as a safety net.

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**:
- `File.WriteAllText` / `FileStream.Flush()` vs `FlushAsync()` and whether Unity 6.3's .NET 8 runtime exposes `FileOptions.WriteThrough` or equivalent fsync — **must verify** [需查證 6.3 API]. On most desktop platforms `File.Move()` within the same filesystem is atomic; mobile sandbox (Android/iOS) requires verification.
- `OnApplicationPause` / `OnApplicationQuit` platform availability and the guaranteed write-time window (especially iOS which may kill the process 5 s after `applicationDidEnterBackground`) — **must verify** [需查證 6.3 API].

**Control Manifest Rules (Feature layer — Meta)**:
- Required: Atomic write sequence exactly: canonical serialize → CRC32 → write `.tmp` → flush/fsync → rename → copy backup
- Required: All file paths read from `SaveConfig` (no hardcoded string literals for file names)
- Required: Deep-copy snapshot before handing to Save Worker (thread safety)
- Forbidden: Direct overwrite of `save.json` without the tmp-rename step (ADR-0004)
- Forbidden: No `PlayerPrefs`, `BinaryFormatter`
- Forbidden: Static singletons holding write queue state — inject `ISaveWorker` via DI

---

## Acceptance Criteria

*From GDD `design/gdd/meta-progression-system.md` §C.5.2 + §H.4, scoped to this story:*

- [ ] `AtomicSaveWriter` class implements the 7-step write sequence from GDD §C.5.2: (1) canonical serialize without hash, (2) compute CRC32 hex, (3) embed hash, (4) canonical serialize final, (5) write to `save.tmp.json`, (6) flush + fsync, (7) rename to `save.json`, (8) copy to `save.bak.json`
- [ ] Process kill simulated between step 5 (write_file complete) and step 7 (rename): `save.json` on disk remains either the previous valid JSON or the new complete JSON — never an incomplete or invalid JSON document
- [ ] `SaveWorker` background thread: queue depth 1 (overwrite semantics — if a pending snapshot exists, the newer one replaces it); new snapshot does not block the enqueuing thread
- [ ] `EnqueueSave(SaveData snapshot)` performs a **deep copy** of the snapshot before passing it to the worker; mutations to the original `SaveData` after enqueue do not affect the pending write
- [ ] When two snapshots are enqueued in rapid succession before the worker has written either, only the second (most recent) snapshot is written to disk
- [ ] `SyncWrite(SaveData snapshot)` blocking method writes synchronously on the calling thread (for use by suspend/quit handlers); completes before returning
- [ ] All file paths (`save.json`, `save.bak.json`, `save.tmp.json`) are read from `SaveConfig` — no string literals in implementation code
- [ ] Save Worker thread is cleanly terminated on application quit (flush pending snapshot before exit)

---

## Implementation Notes

*Derived from ADR-0004 Decision §2 + §5, and control-manifest.md §3 Meta rules:*

**Write sequence (authoritative from ADR-0004 §2)**:
```
1. json_body  = CanonicalSerializer.Serialize(snapshot, excludeIntegrityHash: true)
2. hash       = CRC32Calculator.Compute(json_body)
3. snapshot.integrity_hash = hash
4. json_final = CanonicalSerializer.Serialize(snapshot)   // includes hash field
5. File.WriteAllText(TEMP_PATH, json_final, Encoding.UTF8)
6. // fsync: flush OS buffers to physical disk [需查證 6.3 API — use FileStream + Flush(true) or P/Invoke]
7. File.Move(TEMP_PATH, SAVE_PATH, overwrite: true)       // atomic within same volume
8. File.Copy(SAVE_PATH, BACKUP_PATH, overwrite: true)
```

**Save Worker thread design**:
```csharp
// Shared state (lock-protected or Interlocked):
private volatile SaveData _pendingSnapshot;   // null = nothing pending
private readonly object _lock = new();

// Enqueue (main thread — returns immediately):
public void EnqueueSave(SaveData state) {
    lock (_lock) { _pendingSnapshot = DeepCopy(state); }
}

// Worker loop (background thread):
while (_running) {
    SaveData snapshot;
    lock (_lock) { snapshot = _pendingSnapshot; _pendingSnapshot = null; }
    if (snapshot != null) AtomicWrite(snapshot);
    else Thread.Sleep(_config.SaveWorkerIdleMs);
}
```

**Deep copy**: `SaveData` must implement `DeepCopy()` that clones all nested collections (weapon dict, material dict, kaiju_records sub-collections). Shallow copy is insufficient — main thread continues modifying materials during ongoing writes.

**Suspend/Quit integration**: `SyncWrite()` is called from `OnApplicationPause(true)` and `OnApplicationQuit` handlers (implemented in Story 006). This story only provides the synchronous write path; wiring to Unity lifecycle is done in Story 006.

**fsync note [需查證 6.3 API]**: On desktop, `FileStream.Flush(flushToDisk: true)` calls OS-level `fsync`/`FlushFileBuffers`. On mobile, the sandbox filesystem may handle this differently. Record the verified approach in the implementation commit and `docs/engine-reference/unity/VERSION.md`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: Canonical serializer, CRC32 calculator, SaveData DTO (required by this story — must be done first)
- Story 003: Load flow, CRC32 verification on read, corruption recovery
- Story 006: `OnApplicationPause`/`OnApplicationQuit` handler wiring; `EnqueueSave` calls from event handlers

---

## QA Test Cases

*Authored by lead-programmer (lean mode). Developer implements against these.*

**AC-1**: Process kill between write and rename leaves save.json in valid prior state

- Given: `save.json` contains a known valid snapshot (v1)
- When: `AtomicSaveWriter.AtomicWrite()` is called with a new snapshot; an exception is injected immediately after step 5 (`WriteAllText` to `.tmp` completes) before step 7 (rename)
- Then: `save.json` still contains the v1 snapshot; the content is valid JSON parseable by `CanonicalSerializer.Deserialize`
- Edge cases: `.tmp` file left on disk after simulated kill — next load must not read `.tmp`; next write must safely overwrite `.tmp`

**AC-2**: Queue overwrite — only the latest snapshot is persisted

- Given: Save Worker is paused (worker thread suspended for test control)
- When: `EnqueueSave(snapshotA)` then `EnqueueSave(snapshotB)` called before worker resumes
- Then: after worker resumes and completes, `save.json` contains snapshotB; snapshotA is not present
- Edge cases: three rapid enqueues — only the third survives

**AC-3**: Deep copy isolation — mutating original after enqueue does not affect pending write

- Given: `SaveData` object with `materials["shard_common"] = 10`
- When: `EnqueueSave(saveData)` is called, then `saveData.materials["shard_common"] = 99` before the worker writes
- Then: the file written to disk shows `shard_common = 10` (the snapshot value at enqueue time)
- Edge cases: nested collection mutation (append to `parts_ever_broken` list after enqueue)

**AC-4**: `SyncWrite` blocks until disk write is complete

- Given: a valid `SaveData` snapshot
- When: `SyncWrite(snapshot)` is called on the main thread
- Then: method returns only after `save.json` and `save.bak.json` are both written and closed; `save.json` contains the expected canonical JSON with correct CRC32

**AC-5**: Backup is in sync with save.json after successful write

- Given: any successful `AtomicWrite` call
- When: both `save.json` and `save.bak.json` are read from disk after the call returns
- Then: their contents are identical (byte-for-byte)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/Meta/save_atomic_write_test.cs` — must exist and all tests pass
*(ADR-0005: EditMode test assembly; pure C# file I/O logic with injected path overrides for test isolation.)*

**Status**: [x] ✅ 6/6 GREEN (`Assets/_Project/Tests/EditMode/Meta/save_atomic_write_test.cs`, Unity MCP, 2026-07-06). Covers AC-1 (kill-before-rename via split `WriteTempFile`/`PromoteTempToSave`), AC-2 (depth-1 overwrite), AC-3 (deep-copy isolation incl nested list), AC-4 (SyncWrite valid CRC), AC-5 (backup byte-identical) + live-thread write/flush-on-stop.

**Reconciliations vs story text** (surfaced for review):
1. **`File.Move(…, overwrite)` overload is unavailable** in Unity's .NET Standard runtime → use `File.Replace(tmp, save, null)` (atomic ReplaceFile/rename) when the target exists, plain `File.Move` on first-ever write. Backup is a separate `File.Copy(save, bak, overwrite)` (new save → bak), NOT File.Replace's backup slot (which would hold the OLD file).
2. Save directory is **injected** (`AtomicSaveWriter(config, saveDirectory, serializer)`) — production passes `Application.persistentDataPath` (wired in Story 006), tests pass a temp dir. File names all from `SaveConfig`.
3. `SaveWorker` exposes `DrainOnce()` seam so queue/deep-copy behaviour is deterministically testable without racing the thread; `Start()`/`Stop()` run the same drain on a background thread; `Stop()` flushes the last pending snapshot.
4. fsync = `FileStream.Flush(flushToDisk: true)` (desktop fsync/FlushFileBuffers). Mobile sandbox behaviour still [需查證] at device-test time.

---

## Dependencies

- Depends on: Story 001 must be DONE (`CanonicalJsonSerializer`, `CRC32Calculator`, `SaveData.DeepCopy()` required)
- Unlocks: Story 003 (load uses backup file written by this story), Story 006 (suspend/quit wiring needs `SyncWrite`)
