# ADR-024: Durable sync jobs with TickerQ and SQLite

- **Status:** accepted
- **Date:** 2026-10-05

## Context and Problem Statement

Sync jobs ran on a process-local `Task.Run` with in-memory job state. A server restart lost every
job, its status, and its results, and manual and scheduled syncs had no shared execution path. The
persistent server (`recyclarr serve`) needs jobs that survive restarts and run one at a time.

## Decision Drivers

- Jobs and results stay queryable after a restart; nothing re-runs on its own.
- Manual (API) and scheduled jobs use one execution path.
- Only one sync runs at a time, in creation order.
- Self-hosted deployment: one container, one config volume, no external database.
- A later external database backend (e.g. Postgres) must not need a rewrite.

## Considered Options

1. TickerQ (EF Core operational store) with SQLite in the config volume
2. Hand-written queue table and `BackgroundService` worker over EF Core
3. Keep in-memory jobs

## Decision Outcome

Chosen option: "TickerQ with SQLite", because it gives a persisted queue and worker lifecycle
without owning that code, and its only relational store is EF Core, which Recyclarr's job tables
also use.

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

### Consequences

- Good, because jobs and results survive restarts and the job API contract is unchanged.
- Good, because manual and scheduled jobs share one path.
- Bad, because each waiting job holds a TickerQ worker. When waiting jobs reach the worker count,
  later jobs start late (still in order).
- Bad, because per-instance results are stored as JSON of the v1 results DTO. A breaking change to
  that DTO needs a data migration of stored rows.
- Bad, because a Postgres backend needs a second migration set and provider selection in settings.
  The trigger is a request for an external database.
- Bad, because TickerQ's `MapTicker` writes a process-wide dictionary without locking; Recyclarr
  serializes the call so in-process test servers can register in parallel.
