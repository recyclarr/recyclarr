# ADR-025: Recyclarr-owned sync schedule

- **Status:** accepted
- **Date:** 2026-10-05

## Context and Problem Statement

The container ran sync on a supercronic `@daily` job that invoked the CLI. The persistent server now
owns sync execution (ADR-024), so it must also own the schedule: one global cron schedule from
`settings.yml`, an explicit off switch for external schedulers, and an API that reports it.

## Decision Drivers

- `settings.yml` is the only source of schedule state; nothing to reconcile with the database.
- The next occurrence must be visible through `GET /api/v1/sync/schedule`.
- An occurrence that overlaps an active sync is recorded as skipped and never runs later.
- Missed occurrences (server down) are not replayed.
- Tests must drive time without waiting on the real clock.

## Considered Options

1. Recyclarr computes occurrences (Cronos) in a `BackgroundService` and enqueues TickerQ jobs
2. TickerQ cron tickers

## Decision Outcome

Chosen option: "Recyclarr computes occurrences", because TickerQ cron tickers live in the database
(needing reconciliation with `settings.yml` on every start), hide the next occurrence, and run on
the real clock.

- `SyncSchedule` parses `server.schedule` (`enabled`, `cron`) with Cronos at startup; an invalid
  cron fails startup. Default: `0 0 * * *`.
- Occurrences use the system time zone (`TZ`, then `/etc/localtime` on Linux and macOS) through
  `TimeProvider.LocalTimeZone`. No time zone setting: the OS convention already covers containers
  and native installs.
- `SyncScheduler` waits on the server's `TimeProvider` until each occurrence, then
  `ScheduledSyncTrigger` either queues a scheduled job or records a skipped one.
- Each next occurrence is computed from the current time, so missed occurrences are never replayed.
- The schedule endpoint's next occurrence uses the same calculation the scheduler waits on.
- Ephemeral servers never schedule.

### Consequences

- Good, because the schedule is plain configuration and fully visible through the API.
- Good, because tests advance a fake `TimeProvider` instead of waiting.
- Bad, because a clock jump past several occurrences in a running server produces one catch-up job.
