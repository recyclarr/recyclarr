# ADR-024: Durable sync jobs with TickerQ and SQLite

- **Scope:** `src/Recyclarr.Server/Persistence/`, `src/Recyclarr.Server/Sync/`

## Context

A persistent server needs sync jobs whose status and results survive a restart, one execution path
for manual and scheduled syncs, and one sync at a time in creation order. Deployment is one
container with one config volume and no external database, and a later external backend (for
example Postgres) must not need a rewrite.

## Decision

Sync jobs are queued and executed by TickerQ over an EF Core SQLite store; Recyclarr owns job
identity, status, and results.

- One database file, `state/server.db`, holds two EF contexts with separate migration sets:
  `ServerDbContext` (Recyclarr job tables) and `TickerQueueDbContext` (TickerQ tables). TickerQ
  upgrades touch only their own migrations. Both migrate at startup before Kestrel binds.
- Ephemeral servers use a shared-cache in-memory SQLite database: same code, no file.
- TickerQ only queues and executes. Recyclarr owns job identity, status, and results; the queued
  payload is the job id.
- Status model: `Pending`, `Running`, `Succeeded`, `Partial`, `Failed`, `Interrupted`, `Skipped`.
  Jobs active at startup become `Interrupted` (finished instances keep results); they never re-run.
  A leftover ticker for a terminal job does nothing.
- One sync at a time, oldest first, through a Recyclarr-owned gate, because TickerQ's concurrency
  semaphores do not preserve order.
- Data access is LINQ only (no SQLite-specific SQL); `DateTimeOffset` is stored as binary so SQLite
  can order it.

## Rejected Options

- Hand-written queue table and `BackgroundService` worker: owns queue and worker lifecycle code
  that TickerQ provides.
- In-memory jobs on `Task.Run`: a restart loses every job and its results.

## Consequences

- Jobs and results survive restarts; manual and scheduled jobs share one path.
- Each waiting job holds a TickerQ worker. When waiting jobs reach the worker count,
  later jobs start late (still in order).
- Per-instance results are stored as JSON of the v1 results DTO. A breaking change to that DTO
  needs a data migration of stored rows.
- A Postgres backend needs a second migration set and provider selection in settings. The trigger
  is a request for an external database.
- TickerQ's `MapTicker` writes a process-wide dictionary without locking; Recyclarr serializes the
  call so in-process test servers can register in parallel.

## History

- 2026-10-05: Persist sync jobs with TickerQ and SQLite; one sync at a time.
