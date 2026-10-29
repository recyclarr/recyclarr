# ADR-018: Report instance-level sync progress through snapshots

- **Scope:** Core progress interface, `src/Recyclarr.Server/Sync/Progress/`

## Context

Most pipelines finish too quickly for their intermediate states to be useful, and terminal results
already give the complete pipeline account. A client joining mid-run must render current progress
from one small response, and progress must never decide terminal status.

## Decision

Core reports instance lifecycle changes; the server reduces them into a cumulative instance
snapshot that clients poll.

Core reports when an instance starts and when its terminal result is available. The Server reduces
those callbacks into an ordered snapshot. Polling exposes only the instance name and one of these
states: Pending, Running, Succeeded, Partial, Failed, Interrupted, or NotRun.

Partial is a terminal instance status. It is never inferred while an instance is running. If a
run-wide cancellation or fault stops execution, the active instance becomes Interrupted and
instances that never started become NotRun. Completed instances keep the status derived from their
terminal results.

Progress reporting is best effort and cannot alter execution or terminal results. The final
`SyncRunResult` remains authoritative for faults, planning outcomes, pipeline outcomes, and resource
deltas. The polled job resource carries no pipeline counts, item changes, or event history.

Clients continue to poll. A client may miss a short Running state and move directly from Pending to
a terminal status. SSE should be reconsidered only if measured polling latency or request volume
becomes a problem; it does not require Rx streams in Core.

## Rejected Options

- Rx pipeline events with a per-pipeline polling response: reports transitions nobody can act on.
- Server-Sent Events: not needed while polling latency and volume are acceptable.

## Consequences

- Skipped polls and reconnects lose no current state.
- Clients cannot observe individual pipeline transitions.

## History

- 2026-09-17: Replace Rx pipeline events with polled instance-level snapshots.
