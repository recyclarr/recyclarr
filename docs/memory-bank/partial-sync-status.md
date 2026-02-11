# Partial Sync Status

Branch: `partial-sync-status` (Phase 1 + 1.5a complete)

## Problem

The sync summary table has two states (green checkmark = success, red X = failure) but pipelines
can partially succeed. When some items sync and others are skipped due to validation errors, the
table shows a green checkmark, misleading users.

Example: radarr has 5 quality profiles configured, 1 fails validation (SQP-1), the other 4 are
valid but unchanged. The table shows `✓` for Quality Profiles because the pipeline completed
mechanically and the logger set `Succeeded`.

## Design Decisions

### Three-state status model

Pipeline and instance status enums get a `Partial` value between `Succeeded` and `Failed`:

- `Succeeded`: all requested items processed without errors
- `Partial`: pipeline completed, some items had errors but others succeeded
- `Failed`: mechanical failure (exception/interrupt) OR all items failed

### Rendering

```
Legend: ✓ ok · ~ partial · ✗ failed · -- skipped

           Custom  Quality Quality  Media Media
          Formats Profiles   Sizes Naming  Mgmt
~ radarr        3        2       ✓     --     ✓
```

- Pipeline `Partial`: yellow count (if items changed) or yellow `✓` (all up-to-date)
- Pipeline `Failed` with count 0: red `✗` (all items failed validation, nothing accomplished)
- Instance `Partial`: yellow `~`

### PipelineResult stays binary

`PipelineResult` (Completed/Failed) controls dependency cascade only. A mechanically completed
pipeline with partial success does NOT cascade failure to dependents. Display status is a separate
concern.

### Status set at the source, not resolved after the fact

Pipeline loggers/persistence phases already have the transaction data (valid, invalid, unchanged
items). They should set the correct status directly rather than always setting `Succeeded` and having
an external resolver fix it up.

Rejected approach: a `SyncStatusResolver` that reads back from `SyncEventStorage` and
`IProgressSource.Current` to retroactively determine status. This created a roundtrip where the
logger published errors from transaction data, then the resolver read those events back to infer
what the logger already knew. It also required adding `TotalProcessed` to `PipelineSnapshot` and
plumbing a third parameter through `SetPipelineStatus`, which was leaky.

### IProgressSource.Current should be removed (DONE - Phase 1)

`Current` removed. `IProgressSource` no longer inherits `IObservable<T>`; it exposes an
`IObservable<ProgressSnapshot> Observable` property instead, wrapped with `AsObservable()` to
prevent downcasting. `SyncProgressRenderer` relies on BehaviorSubject's initial emission via
Subscribe. `NotificationService` no longer depends on `IProgressSource`; it receives the final
`ProgressSnapshot` as a parameter from `SyncProcessor`.

Additional changes beyond original Phase 1 scope:

- `IProgressSource` refactored from interface inheritance to property exposure (Rx best practice)
- `NotificationService` dependency on `IProgressSource` eliminated (push data, don't pull)
- `SyncProcessor` subscribes to capture latest snapshot for notification pass-through

Internal read-modify-write in `ProgressSource.SetInstanceStatus`/`SetPipelineStatus` still reads
`_subject.Value` directly; that's internal implementation, not part of the public interface.

## Architecture: Current Pipeline Flow

```
User config -> Plan (prep data) -> Fetch (API DTOs) -> Transaction -> Preview -> Persistence
```

- Transaction phase validates items, separates valid from invalid
- Logger publishes errors for invalid items via `ISyncEventPublisher.AddError()`
- Persistence runs for valid items only
- Logger calls `context.Progress.SetStatus(Succeeded, totalChanged)`

Three independent systems exist:

1. **Progress system** (`ProgressSource`, `BehaviorSubject`): drives the live Spectre.Console table.
   `CompositeSyncPipeline` creates a `PipelineProgressWriter` per pipeline (via
   `IProgressSource.ForPipeline`), passes it to `GenericSyncPipeline`, which sets it on
   `PipelineContext`. Pipeline phases use `context.Progress.SetStatus()`. Renderer subscribes
   reactively on a separate thread.
2. **Diagnostics system** (`SyncEventStorage`): collects error/warning messages rendered in the
   post-sync "Sync Diagnostics" box and used by `NotificationService`. Not involved in status
   resolution.
3. **Context system** (`SyncContextSource`): tracks current instance/pipeline for log scoping
   (Serilog `LogContext`) and diagnostic event attribution via `SyncEventCollector`. Decoupled from
   progress system as of Phase 1.5a.

### Key files

| File | Role |
|---|---|
| `src/Recyclarr.Core/Sync/Progress/IProgressSource.cs` | Write interface for progress state |
| `src/Recyclarr.Core/Sync/Progress/ProgressSource.cs` | BehaviorSubject implementation |
| `src/Recyclarr.Core/Sync/Progress/PipelineProgressWriter.cs` | Scoped writer capturing identity via delegate |
| `src/Recyclarr.Core/Sync/Progress/ProgressSnapshot.cs` | Immutable snapshot records + enums |
| `src/Recyclarr.Core/Sync/Progress/PipelineProgressStatus.cs` | Pipeline status enum |
| `src/Recyclarr.Cli/Processors/Sync/Progress/ProgressTableBuilder.cs` | Renders the summary table |
| `src/Recyclarr.Cli/Processors/Sync/Progress/SyncProgressRenderer.cs` | Live renderer + legend |
| `src/Recyclarr.Cli/Processors/Sync/SyncProcessor.cs` | Orchestrates instances, sets instance status |
| `src/Recyclarr.Cli/Processors/Sync/CompositeSyncPipeline.cs` | Orchestrates pipelines within instance |
| `src/Recyclarr.Core/Sync/Events/SyncEventStorage.cs` | Diagnostics accumulator |

### Progress write sites

Writer is created by `CompositeSyncPipeline` via `IProgressSource.ForPipeline()`, then passed
through `ISyncPipeline.Execute()` to `GenericSyncPipeline`, which sets it on `PipelineContext`.

| File | How | Context |
|---|---|---|
| `CompositeSyncPipeline.cs` | `progress.SetStatus()` | Skipped (dependency skip) |
| `GenericSyncPipeline.cs` | `progress.SetStatus()` | Running, Skipped, Failed (exception) |
| `QualityProfileLogger.cs` | `context.Progress.SetStatus()` | Succeeded + changed count |
| `CustomFormatTransactionLogger.cs` | `context.Progress.SetStatus()` | Succeeded + changed count |
| `MediaManagementApiPersistencePhase.cs` | `context.Progress.SetStatus()` | Succeeded + difference count |
| `MediaNamingApiPersistencePhase.cs` | `context.Progress.SetStatus()` | Succeeded + difference count |
| `QualitySizeApiPersistencePhase.cs` | `context.Progress.SetStatus()` | Succeeded + changed count |

### SetInstanceStatus call sites

| File | Context |
|---|---|
| `SyncProcessor.cs` | Sets Running, Failed/Succeeded (explicit instanceName param) |

## Implementation Plan (Incremental)

### Phase 1: Remove IProgressSource.Current (DONE)

Pure refactor, no behavior change. See "IProgressSource.Current should be removed" in Design
Decisions for details of what was done.

### Phase 1.5a: Replace ambient state with explicit progress writer (DONE)

`ProgressSource` previously read identity (instance name, pipeline type) from `ISyncContextSource`
(a separate singleton) via `contextSource.Current`. This was action-at-a-distance with temporal
coupling ("set context before setting status") that the type system didn't enforce.

Changes:

- `PipelineProgressWriter` captures identity via delegate, exposes `SetStatus(status, count?)`
- `IProgressSource.SetPipelineStatus` replaced by `ForPipeline(instanceName, pipeline)` factory
- `SetInstanceStatus` takes explicit `instanceName` parameter
- `ProgressSource` no longer depends on `ISyncContextSource`
- `CompositeSyncPipeline` creates writer per pipeline, passes through `ISyncPipeline.Execute()`
- `GenericSyncPipeline` receives writer as parameter, sets it on `PipelineContext`; no longer
  depends on `IProgressSource`
- `ISyncPipeline` no longer inherits `IPipelineExecutor` (signatures diverged)
- 5 persistence phases/loggers use `context.Progress.SetStatus()` instead of injecting
  `IProgressSource`
- `ISyncContextSource` remains for diagnostics/logging only (`SyncEventCollector`)

### Phase 1.5b: Scope observable to sync run lifetime (FUTURE)

`ProgressSource` is still a singleton `BehaviorSubject` with `Clear()`. The writer provides a
natural creation point for lifecycle scoping (the `using var progress = ...` pattern with
`OnCompleted()` on disposal), but this is not yet implemented.

Desired model: each sync run produces an observable that starts when sync begins and completes when
sync ends. This would eliminate `Clear()`, enable `LastAsync()` for final snapshot, and remove the
ad-hoc subscription in `SyncProcessor`.

Open questions remain about who owns the per-run observable, how `SyncProgressRenderer` receives it,
and whether this should be combined with diagnostics lifecycle scoping (see discussion below).

### Phase 2: Add Partial enum values + PipelineResult move

Additive, no behavior change. Nothing uses `Partial` yet.

- Add `Partial` to `PipelineProgressStatus` and `InstanceProgressStatus`
- Move `PipelineResult` from `Recyclarr.Cli.Pipelines` to `Recyclarr.Sync` (Core). It's a
  mechanical result enum used across assembly boundaries. Make it `public`.
- Update `ProgressTableBuilder`: render `Partial` as yellow count/checkmark for pipelines, yellow
  `~` for instances
- Update `SyncProgressRenderer` legend: `✓ ok · ~ partial · ✗ failed · -- skipped`
- Add `HasPipelineErrors` to `SyncEventStorage` (will be needed in Phase 3)

### Phase 3: Pipeline-level partial status

Behavior change: pipelines that complete with errors show `Partial` or `Failed` instead of
`Succeeded`.

- `QualityProfileLogger.LogPersistenceResults`: check `InvalidProfiles` from transaction data. Set
  `Partial` if some profiles are invalid but valid ones exist. Set `Failed` if all profiles are
  invalid. Set `Succeeded` if none are invalid.
- Similar for other pipeline loggers where partial success is possible (assess each)
- Note: CustomFormat pipeline is atomic (throws on error); it can never be partial

### Phase 4: Instance-level partial status

- `SyncProcessor`: after `CompositeSyncPipeline.Execute()` returns, derive instance status:
  - `PipelineResult.Failed` -> `InstanceProgressStatus.Failed`
  - `PipelineResult.Completed` + `eventStorage.HasInstanceErrors(instanceName)` ->
    `InstanceProgressStatus.Partial`
  - `PipelineResult.Completed` + no errors -> `InstanceProgressStatus.Succeeded`
- `HasInstanceErrors` already exists on `SyncEventStorage`

### Phase 5: Tests

- Unit tests for pipeline loggers setting correct status based on transaction data
- Integration tests for CompositeSyncPipeline partial scenarios
- Update existing ProgressTableBuilder tests if any

## Key Learnings

- The progress system is write-only from pipeline phases and subscribe-only from the renderer.
  Reading state back for business logic decisions is a design smell.
- Pipeline loggers already have full transaction data (valid, invalid, unchanged items). Status
  resolution belongs there, not in an external utility that reads events.
- `PipelineResult` (mechanical) and `PipelineProgressStatus` (display) are separate concerns.
  Mechanical failure cascades dependencies; display status informs the user. Don't conflate them.
- `Count` in the progress snapshot means "items that changed," not "items successfully processed."
  These are different values. The status decision should be based on the transaction data directly,
  not inferred from the display count.
- Expose `IObservable<T>` as a property, not via interface inheritance. Wrap subjects with
  `AsObservable()` to prevent downcasting. (Rx.NET official guidance.)
- Push data to consumers (pass as parameters) rather than letting them pull via imperative
  read-back (`.Current`, `.Value`). Makes dependencies explicit and testable.
- Long-lived subjects with manual `Clear()` are a lifecycle smell. Observable lifetime should match
  the operation it represents. A per-sync-run observable with natural completion would eliminate
  ad-hoc subscriptions and synthetic lifecycle management.
- Progress and diagnostics are distinct concerns with different data shapes and lifecycles. Progress
  is continuous state (latest snapshot replaces previous); diagnostics are discrete accumulated
  events. They share a lifecycle boundary (the sync run) but should not be merged into one stream.
- `NotificationService` consumes both: `ProgressSnapshot` for "Information" lines (pipeline
  counts), `SyncEventStorage` for errors/warnings. The two remain separate data sources.
- `ISyncContextSource` serves diagnostics/logging only (Serilog log scoping, diagnostic event
  attribution via `SyncEventCollector`). It is no longer involved in progress updates.
- `ISyncPipeline` and `IPipelineExecutor` are separate concerns (individual pipeline vs. composite
  executor). They shared an interface only because signatures happened to match; when the writer
  parameter was added to `ISyncPipeline.Execute()`, they correctly diverged.
