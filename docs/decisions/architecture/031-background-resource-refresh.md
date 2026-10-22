# ADR-031: Background resource refresh

- **Scope:** `src/Recyclarr.Server/Resources/`, `src/Recyclarr.Core/ResourceProviders/`, every
  reader of guide data in the server

## Context

A persistent server runs for weeks, but git resource providers were fetched only at startup, so
guide data aged without bound. An ephemeral server fetched every git provider on every CLI command,
which made each command wait for the network. Provider files change in place, so a refresh must not
rewrite them while a sync or guide request reads them. Template includes come from providers, so the
resolved configuration depends on provider data too.

## Decision

`ResourceCoordinator` MUST be the only writer of provider data, state files, and the published
snapshot. A snapshot holds the guide index (the guide files of each resource type) and the
configuration resolved against it.

### Refresh

- Startup refreshes before the server reports READY. In persistent mode, `ResourceRefreshWorker`
  asks for a refresh every minute. Readers MUST NOT refresh; no request or sync waits for a fetch.
- A git provider is fetched when it has no state file, its source identity (clone URL plus requested
  references) changed, or its last successful fetch is 1 hour old. After a failed fetch it is not
  fetched again for 5 minutes. Both values are hardcoded.
- The state lives in `<type>/git/<name>.state.json` beside the cache directory, outside the data
  that resource loading reads and outside what a cache rebuild deletes.

### Locking

- A reader-writer lock (DotNext `AsyncReaderWriterLock`) guards provider data. Every scope that
  reads guide data MUST hold one read lease for its whole life and read only that lease's snapshot:
  `GuideLeaseMiddleware` for guide requests, `SyncJobTickerFunction` for sync runs.
- A refresh MUST NOT wait for the write lock. A waiting writer blocks new readers, which would stall
  guide requests behind a long sync. When readers hold the lock, the refresh is skipped and the next
  tick retries.
- Repairs (the cache size limit and the corrupt-repository retry) delete and re-clone in place under
  the write lock. Revisit with a clone into a new directory and a swap if readers often wait for
  repairs.

### Validation and rollback

- Each source implements update, mark good, and revert to last good. Git marks the good commit with
  `refs/recyclarr/last-good`, which also keeps its objects alive across shallow fetches.
- After an update, the configuration is resolved against the new data. Valid: mark good and publish.
  Invalid: revert and keep the current snapshot. Startup fails only when no valid snapshot results.
- Configuration counts as invalid when it does not load, for example when an include names a
  template that a provider update removed. Guide content that a sync cannot use, such as a removed
  custom format, is reported by the sync, not rolled back.

## Rejected Options

- Refreshing on the request path when data is stale: the request that finds stale data pays for the
  fetch.
- A new directory per refresh with a swap: each refresh downloads the full shallow clone again, and
  old directories need reference-counted cleanup.
- Git worktrees sharing one repository: old worktree heads keep objects alive, and a repair of the
  shared repository breaks the live worktree.
- Fetching outside the write lock: the repair paths delete the live directory as part of fetching.
- A warm ephemeral server shared by CLI commands: see ADR-010.

## Consequences

- Guide data in a persistent server is at most about 1 hour and 1 minute old while the network is
  reachable.
- An ephemeral server fetches only when the cache is stale.
- Edits to local provider directories appear only after the next git refresh or a restart.
- A repair removes the good mark, so a bad update right after a repair cannot be reverted; the
  server logs it and keeps its in-memory snapshot, whose files may then differ on disk.
- Readers that hold the lock on every tick postpone refreshes; current traffic makes this unlikely.

## History

- 2026-10-21: Refresh provider data in the background with a TTL, guard it with a reader-writer
  lock, and revert updates that make the configuration invalid.
