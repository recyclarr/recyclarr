# ADR-035: Server diagnostics resource

- **Scope:** `src/Recyclarr.Server/Diagnostics/`, `GET /api/v1/diagnostics`, sync notifications, CLI
  `sync` output

## Context

Some problems last across syncs: a deprecated setting, a configuration edit the server rejected, a
guide update held back because it broke the configuration. When only the server log reported them,
users rarely saw them; the log is read after something visibly fails, and these problems do not fail
a sync. Who can fix a problem does not decide where it should be reported. Users read notifications
and CLI output, and a future GUI needs the same data for banners.

## Decision

A condition MUST be a server diagnostic when it lasts across syncs and changes, or will change, what
syncs do. A deprecation qualifies because it breaks at the next major version. A condition found
during one operation and specific to it stays an outcome of that operation (ADR-016).

- Diagnostics are current state, not a history. Reading them changes nothing. An entry exists while
  its condition holds and leaves the list when the server next sees the condition is gone.
- The producer that checks a condition owns its entries and replaces its whole set each time it
  checks. A producer MUST NOT change another producer's entries.
- Each entry has a stable ID that stays the same while the problem lasts, plus the outcome envelope
  fields: string code, severity, server-rendered message, and per-code data.
- A diagnostic appears once, in the diagnostics resource. It is not copied into sync results as an
  outcome. Sync notifications and CLI `sync` output show the current diagnostics with every sync, so
  the problem repeats until it is fixed.
- Diagnostics do not change sync status or the CLI exit code.
- Diagnostics live in memory. After a restart, each producer rebuilds its entries the next time it
  checks its condition.

## Rejected Options

- Copying diagnostics into sync results as outcomes: the same problem appears in two places with two
  lifecycles, and clients that show both repeat it.
- Server log only: users who do not read the log never learn of problems that do not fail a sync.
- The HTTP `Warning` header or another per-response signal: clients rarely surface it, which is why
  HTTP made `Warning` obsolete.
- Persisting diagnostics: every condition can be checked again, so stored entries add a way to go
  stale without adding information.

## Consequences

- A held-back guide update is not remembered across a restart; it shows again at the next guide
  check. If that gap matters, store the held-back state with the provider's refresh state.
- Entries do not name instances yet, because deprecation messages carry no instance.
- A notification is sent for every sync while any diagnostic exists.

## History

- 2026-11-15: Ongoing server problems are listed as server diagnostics until fixed.
