# ADR-018: Report instance-level sync progress through snapshots

- **Status:** accepted
- **Date:** 2026-09-17

## Context and Problem Statement

The sync engine publishes pipeline status, counts, item changes, and diagnostics through Rx streams.
The Server reduces those events into a snapshot that clients poll, but most pipelines finish too
quickly for their intermediate states to be useful. Terminal results already provide the complete
pipeline account.

## Decision Drivers

- A client joining mid-run must be able to render current progress from one response
- Polling should remain small and independent of terminal result detail
- Progress must not determine terminal status or require event replay
- The CLI needs useful feedback without exposing Core execution structure
- The reporting mechanism should match its single state-reduction consumer

## Considered Options

1. Keep Rx pipeline events and the per-pipeline polling response
2. Report instance lifecycle changes and poll a cumulative instance snapshot
3. Stream progress to clients with Server-Sent Events

## Decision Outcome

Chosen option: "Report instance lifecycle changes and poll a cumulative instance snapshot", because
the UI needs current instance state rather than every pipeline transition.

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
becomes a problem; it does not require retaining Core Rx streams.

### Consequences

- Good, because progress matches the granularity users can act on
- Good, because skipped polls and reconnects lose no current state
- Good, because Core no longer needs a reactive transport for progress
- Bad, because clients cannot observe individual pipeline transitions
- Bad, because short instance executions may complete between polls
