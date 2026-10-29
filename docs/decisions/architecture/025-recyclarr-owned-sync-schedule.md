# ADR-025: Recyclarr-owned sync schedule

- **Scope:** `src/Recyclarr.Server/Sync/Schedule/`, `server.schedule` in `settings.yml`

## Context

The persistent server owns sync execution (ADR-024), so it also owns the schedule: one global cron
schedule from `settings.yml`, an off switch for external schedulers, and an API that reports the
next occurrence. Overlapping occurrences must be recorded as skipped, missed ones must not replay,
and tests must drive time without the real clock.

## Decision

Recyclarr computes occurrences with Cronos in a background service and enqueues TickerQ jobs.
`settings.yml` is the only source of schedule state.

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

## Rejected Options

- TickerQ cron tickers: they live in the database (reconciled with `settings.yml` on every start),
  hide the next occurrence, and run on the real clock.

## Consequences

- The schedule is plain configuration and fully visible through the API.
- Tests advance a fake `TimeProvider` instead of waiting.
- A clock jump past several occurrences in a running server produces one catch-up job.

## History

- 2026-10-05: The server schedules syncs from `settings.yml`, replacing the container cron job.
