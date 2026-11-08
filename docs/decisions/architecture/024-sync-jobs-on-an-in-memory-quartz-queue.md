# ADR-024: Durable sync job records on an in-memory Quartz queue

- **Scope:** `src/Recyclarr.Server/Persistence/`, `src/Recyclarr.Server/Sync/`

## Context

A persistent server needs sync jobs whose status and results survive a restart, one execution path
for manual and scheduled syncs, and one sync at a time in creation order. A hung sync must not block
every later job. Deployment is one container with one config volume and no external database, and a
later external backend (for example Postgres) must not need a rewrite. Users must be able to build
and run Recyclarr from source without a license file.

## Decision

Recyclarr's own job records in EF Core are the only durable job data. Quartz.NET queues and runs
jobs from an in-memory store.

- `state/server.db` holds `ServerDbContext` (Recyclarr job records) and migrates at startup before
  Kestrel binds. It is the one EF Core database for state that must persist.
- Ephemeral servers use a shared-cache in-memory SQLite database: same code, no file.
- The queued payload is the job id; Recyclarr owns job identity, status, and results.
- Status model: `Pending`, `Running`, `Succeeded`, `Partial`, `Failed`, `Interrupted`, `Skipped`.
  Jobs active at startup become `Interrupted` (finished instances keep results) and MUST NOT re-run,
  automatically or on request: a re-run reads current guide data and config, so it equals a new sync
  request. A queued run whose job is missing or terminal does nothing.
- The queue MUST NOT be persisted. Every entry left after a restart would be discarded by the rule
  above, so a queue database only adds migrations, write contention, and cleanup.
- One sync at a time, oldest first: one Quartz worker, and each trigger is due at its job's creation
  time, so the earliest due trigger is the oldest job.
- Each run is limited to `server.sync_timeout` minutes from when it starts; past the limit it is
  canceled and fails, freeing the worker.
- Data access is LINQ only (no SQLite-specific SQL); `DateTimeOffset` is stored as binary so SQLite
  can order it.

## Rejected Options

- TickerQ: releases from 10.5 are source-available and block job execution without a license
  certificate, which breaks source builds, forks, and CI.
- Hangfire: LGPL, no official SQLite storage, retries and re-runs jobs after a crash by default.
- Persisted queue (Quartz ADO store, in `server.db` or a separate file): holds only entries startup
  would discard.
- Hand-written queue and worker: owns queue and worker lifecycle code that Quartz provides.
- In-memory job records: a restart loses every job and its results.
- No-progress (heartbeat) detection of hung jobs: needs a custom watchdog; a fixed limit suffices
  because syncs are short.

## Consequences

- Jobs and results survive restarts; manual and scheduled jobs share one path.
- Two jobs created at the same instant have no defined order.
- Cancellation is cooperative: a sync stuck in code that ignores its token keeps the worker until it
  returns.
- Per-instance results are stored as JSON of the v1 results DTO. A breaking change to that DTO needs
  a data migration of stored rows.
- A Postgres backend needs a second migration set and provider selection in settings. The trigger is
  a request for an external database.
- Several server instances sharing jobs (high availability or scale-out) need a shared persistent
  queue on an external database; that request is the trigger to revisit the in-memory queue.

## History

- 2026-10-05: Persist sync jobs with TickerQ and SQLite; one sync at a time.
- 2026-11-08: Replace TickerQ with an in-memory Quartz queue; limit each run's duration.
