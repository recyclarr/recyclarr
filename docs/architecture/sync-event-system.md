# Sync Event System

The sync event system provides a unified, observable-based approach for tracking sync run state. All
status changes, diagnostics, and progress flow as events through typed observables. Producers emit
events without knowing who consumes them; consumers subscribe to the streams they need.

This replaces three earlier systems that handled the same concerns inconsistently: a
BehaviorSubject-based progress source, an imperative `Add()`/`Clear()` diagnostic storage, and
ambient state for event attribution. The observable model eliminates all three in favor of a single
pattern.

## Scope Hierarchy

Sync execution uses nested Autofac lifetime scopes to manage component lifecycles and event
boundaries. Each scope level owns a distinct set of services, and disposal propagates from outer to
inner.

```txt
Root Container (singletons, shared infrastructure)
  |
  +-- "sync" scope (per sync run)
  |     SyncRunScope (event hub: ISyncRunScope + ISyncRunPublisher)
  |     ISyncOrchestrator, DiagnosticsLogger, NotificationService
  |     CLI-only: SyncCommandHandler, SyncProgressRenderer, DiagnosticsRenderer
  |
  +----+-- "instance" scope (per service instance, child of sync)
       |     IInstancePublisher
       |     InstanceSyncProcessor, IPipelineExecutor
       |     IServiceConfiguration (the specific instance config)
       |     ISyncOperation implementations, API services
       |
       +-- IPipelinePublisher (runtime object, not a scope)
             Created per sync operation by IInstancePublisher.ForPipeline()
```

### Scope wrappers

Named scopes are created through scope factory classes, which begin a child scope and resolve a
wrapper from it. Each wrapper is the single service-locator touch point for its scope; everything
else flows through constructor injection.

- The "sync" scope is created by the CLI command and wraps the orchestrator plus event consumers.
- The "instance" scope is created per service configuration inside the orchestrator loop.

Both wrappers implement `IDisposable` to dispose the underlying Autofac scope. The CLI command
creates and disposes the sync scope; the orchestrator creates and disposes instance scopes.

A noop `IInstancePublisher` is also registered at root scope for code paths that run outside a sync
context (tests, deprecated commands).

## Event Model

Three typed event streams flow through `SyncRunScope`, which holds one `Subject<T>` per stream:

| Stream        | Event type            | Carries                                                            |
|---------------|-----------------------|--------------------------------------------------------------------|
| `Instances`   | `InstanceEvent`       | Instance name, status (Pending/Running/Succeeded/Failed)           |
| `Pipelines`   | `PipelineEvent`       | Instance name, operation type, status, optional count              |
| `Diagnostics` | `SyncDiagnosticEvent` | Nullable instance name, level (Error/Warning/Deprecation), message |

All three inherit from `SyncRunEvent`, which exists solely to enable `Observable.Merge` in the
progress renderer. It is not used for polymorphic dispatch; consumers subscribe to the typed
observables directly.

### Why Three Separate Streams

Separate observables give compile-time safety: consumers subscribe to exactly what they need with no
`OfType<T>()` filtering. Rx composition operators (`Merge`, `Scan`, `CombineLatest`) work naturally
across independently typed streams.

### Producer/Consumer Interface Split

`SyncRunScope` implements two interfaces:

- `ISyncRunScope` (consumer-facing): exposes `IObservable<T>` properties for subscription.
- `ISyncRunPublisher` (producer-facing): exposes `Publish()` methods for each event type.

Consumers inject `ISyncRunScope`. Publishers never see it; they use `IInstancePublisher` or
`IPipelinePublisher` which internally delegate to `ISyncRunPublisher`.

On disposal, `SyncRunScope` calls `OnCompleted()` on all subjects, signaling end-of-run to any
operator that depends on stream completion (e.g., `ToList()`).

## Publishers

Publishers are layered objects that capture identity at creation, so callers emit events without
knowing their context. This provides the same ergonomics as ambient state but with explicit data
flow.

### IInstancePublisher

DI-managed, scoped to the "instance" lifetime. Takes `IServiceConfiguration` and `ISyncRunPublisher`
via constructor injection. Stamps the instance name on every event it emits.

Key behaviors:

- `SetStatus()` publishes an `InstanceEvent`.
- `AddError()` / `AddWarning()` / `AddDeprecation()` publish `SyncDiagnosticEvent` with the instance
  name.
- `HasErrors` tracks whether any errors were emitted, used by `InstanceSyncProcessor` to
  short-circuit after plan validation failures.
- `ForPipeline(PipelineType)` creates an `IPipelinePublisher` for a specific sync operation.

### IPipelinePublisher

Runtime object, not DI-managed. Created by `IInstancePublisher.ForPipeline()` for each sync
operation during orchestration. Stamps both instance name and operation type on events.

The orchestrator creates one publisher per sync operation and passes it as a parameter to
`Compute()` and `Persist()`. Operations use the publisher to emit status changes and diagnostics.

### Noop implementations

Both interfaces provide static `Noop` properties (`IInstancePublisher.Noop`,
`IPipelinePublisher.Noop`) for contexts where event emission is unnecessary (tests, code paths
outside sync scope).

## Consumers

All consumers inject `ISyncRunScope` and subscribe in their constructors. Events accumulate in lists
during the sync run. Explicit method calls trigger final processing after the run completes.

### SyncProgressRenderer

Renders a live progress table showing instance and pipeline status.

Subscribes to both `Instances` and `Pipelines` streams via `Observable.Merge` (upcasting to
`SyncRunEvent`), then folds events into immutable `ProgressSnapshot` records using `Scan`. The
render loop polls the latest snapshot reference on a timer. Thread safety comes from `Scan`
producing immutable snapshots and `Subscribe` performing an atomic reference swap.

### DiagnosticsLogger

Subscribes to the `Diagnostics` stream and logs each event immediately via `ILogger` at the
appropriate level (Error, Warning). This ensures diagnostic messages appear when `--log` is active
(where `IAnsiConsole` output is suppressed). Has no explicit call site; activated by DI resolution
in the sync scope.

### DiagnosticsRenderer

Accumulates `SyncDiagnosticEvent` entries via a simple `Subscribe` that appends to a list.
`Report()` formats and renders errors and warnings to the console, grouped by severity and color
coded by instance.

### NotificationService

Subscribes to all three streams, accumulating events into separate lists. `SendNotification()`
derives overall success from `InstanceEvent` statuses, builds per-instance pipeline snapshots from
`PipelineEvent` groups, and formats diagnostics into the Apprise notification body.

### Why Explicit Calls Instead of OnCompleted

The original design had consumers react automatically when `SyncRunScope.Dispose()` fired
`OnCompleted()`. This was rejected for two reasons:

1. `NotificationService` does async HTTP work. Triggering it from an `OnCompleted` handler requires
   fire-and-forget or blocking, neither of which is acceptable.
2. Autofac disposes components in reverse-resolution order. Consumers that depend on `ISyncRunScope`
   would be disposed before `SyncRunScope` fires `OnCompleted`, causing the signal to arrive after
   the consumer is already torn down.

The current approach (eager `Subscribe` for accumulation, explicit `Report()` / `SendNotification()`
calls from the orchestration layer) is the least-surprise alternative.

## Event Flow Walkthrough

```mermaid
sequenceDiagram
    participant Orch as Orchestrator
    participant Hub as SyncRunScope
    participant Con as Consumers
    participant Op as Sync Operations

    Orch->>Hub: Start sync scope
    activate Hub
    Note over Hub,Con: Consumers subscribe in constructors

    loop Each instance
        Orch->>Hub: InstanceEvent Running
        Hub-->>Con: Instances stream

        loop Each sync operation
            Orch->>Op: Compute/Persist with IPipelinePublisher
            activate Op
            Op->>Hub: PipelineEvent Running
            Op->>Hub: SyncDiagnosticEvent
            Op->>Hub: PipelineEvent Succeeded or Failed
            Hub-->>Con: Pipelines and Diagnostics streams
            deactivate Op
        end

        Orch->>Hub: InstanceEvent Succeeded or Failed
        Hub-->>Con: Instances stream
    end

    Orch->>Con: Report and SendNotification
    Orch->>Hub: Dispose scope
    Hub-->>Hub: OnCompleted
    deactivate Hub
```

The diagram is intentionally abstract. The orchestration layer spans several components (CLI
command, sync orchestrator, instance processor, operation executor) but the event flow pattern is
the same regardless of which component is the immediate caller.

## Relationship to sync architecture

This system handles status tracking and diagnostics for the sync run. It does not control sync
execution flow. For operation ordering, dependency cascading, and the plan/sync split, see [Sync
architecture](sync-pipeline-architecture.md).
