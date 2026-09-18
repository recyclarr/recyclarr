# Sync lifecycle reporting

Sync execution reports a small instance lifecycle to the Server. Detailed pipeline outcomes,
deltas, operational failures, and fault references live in terminal results. Progress never decides
terminal status and does not duplicate pipeline result data.

The reporting path is synchronous and does not use observable streams. This keeps execution order
explicit and lets the Server expose current snapshots without retaining an event history.

## Scope hierarchy

Sync execution uses nested Autofac lifetime scopes:

```txt
Root container
  |
  +-- "run" scope
  |     ISyncOrchestrator, SyncJobRunner, NotificationService
  |
  +----+-- "instance" scope
       |     InstanceSyncProcessor, IPipelineExecutor
       |     IServiceConfiguration
       |     ISyncOperation implementations and API services
```

`SyncJobLauncher` opens the run scope as a child of the root container so a background job can
outlive the HTTP request that created it. `SyncOrchestrator` opens and disposes one instance scope
for each selected configuration.

The instance attempt includes scope creation, processing, and disposal. A catchable fault in that
boundary produces one faulted instance result, then execution continues with the next instance.
Cancellation and faults outside an instance boundary stop the run.

## Core lifecycle port

`IInstanceSyncProgress` has two callbacks:

- `InstanceStarted(string instanceName)`
- `InstanceCompleted(SyncInstanceResult result)`

`SyncOrchestrator` calls `InstanceStarted` immediately before the instance attempt. It calls
`InstanceCompleted` only after processing and cleanup have produced the final instance result.
Callbacks are ordered because instances execute sequentially.

The orchestrator guards both callbacks. A reporting failure cannot change execution, create an
instance fault, or replace a terminal result. Core has no job ID, store, or HTTP dependency.

## Server snapshots

`SyncJobProgress` binds the Core port to one job and reduces callbacks through
`ISyncJobStore.Update`. The in-memory store applies every update under its existing lock.

A new job starts with one ordered entry per resolved instance. Entries use these states:

- `Pending`
- `Running`
- `Succeeded`
- `Partial`
- `Failed`
- `Interrupted`
- `NotRun`

Start changes `Pending` to `Running`. Completion changes `Running` to a terminal status derived from
the supplied `SyncInstanceResult`. Late, duplicate, and unknown-instance updates do not mutate the
snapshot. Completed entries retain their exact instance result for terminal reconciliation.

When a run stops, active entries become `Interrupted` and pending entries become `NotRun`.
Completed entries do not regress. Stopped entries do not receive invented results.

## Terminal finalization

`SyncJobFinalizer` is the shared terminal path for the runner and launcher fallback. Normal
completion reconciles the snapshot from `SyncRunResult`, closes unfinished entries, then stores the
result and job status atomically.

For cancellation or a run-wide fault, finalization preserves completed instance results from the
snapshot, attaches a run fault, and closes unfinished entries. Cleanup after an already stored
terminal result cannot overwrite it.

## Consumers

The polling endpoint projects each progress entry as `name` and `status`. It does not expose
pipeline progress, counts, changes, or retained interim results. Clients retrieve pipeline details
from the terminal results endpoint after the job reaches a terminal state.

`SyncResultLogger` and `NotificationService` also consume the terminal result. They format typed
outcomes and opaque fault references at the Server boundary; exception details remain in the
server log.

See [Sync architecture](sync-pipeline-architecture.md) for operation ordering, dependency blocking,
and semantic pipeline results.
