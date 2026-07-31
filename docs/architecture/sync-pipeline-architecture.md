# Sync architecture

## Why this architecture exists

Sync processing contains the majority of Recyclarr's business logic. The complexity comes from
validating user configuration, reconciling server state, handling dependencies between data types
(Quality Profiles reference Custom Formats), and managing service differences between Sonarr and
Radarr.

The original service-first design (Radarr processing, then Sonarr processing) created significant
code duplication since Custom Format processing is nearly identical between services. The current
resource-first approach (Custom Formats, then Quality Profiles, etc.) eliminates this duplication
while handling service differences through targeted injection points.

Most user errors come from configuration mistakes and server-side conflicts. The architecture
prioritizes comprehensive error collection and user-friendly reporting over fail-fast approaches,
explaining problems in YAML terms rather than technical internals.

## Overview

The system processes five sync categories within each server instance. Execution order is derived
from explicit dependency declarations:

1. Custom Formats (foundation, no dependencies)
2. Quality Profiles (depends on Custom Formats)
3. Quality Definitions (independent)
4. Media Naming (independent)
5. Media Management (independent)

Each category is a self-contained `ISyncOperation` class that owns its full lifecycle: fetching
current state from the service, computing what needs to change, and persisting changes. The
orchestrator (`CompositeSyncPipeline`) controls sequencing, dependency tracking, and the
preview/persist decision.

## Sync operations

Each resource type implements `ISyncOperation`. Its execution keeps two internal stages distinct:

- `Compute()` fetches current state from the Servarr API and computes the diff against the plan. All
  validation and conflict detection happens here.
- `Persist()` applies the computed changes to the service. Only called when not in preview mode.

The operation executes both stages and returns its final semantic result:

```csharp
var result = await operation.Execute(
    settings.Preview,
    plan,
    publisher,
    capture,
    ct
);
```

Transaction and sync-state carriers stay private to the operation. They exist only between Compute
and Persist. The composite pipeline sees pipeline-owned semantic results rather than transaction or
service models.

### Skipping operations

Each operation declares whether it should skip via `ShouldSkip(plan)`. Service affinity is encoded
in the plan components themselves (e.g. the Sonarr naming plan component produces nothing for Radarr
instances), and config presence is checked against the plan (no `quality_sizes` section means the
quality size operation skips). The orchestrator partitions operations by skip status before
topological sorting.

## Dependency management

Operations declare dependencies via `ISyncOperation.Dependencies`. The orchestrator uses topological
sort to determine execution order. `Partial`, `Failed`, and `Blocked` results give dependents a typed
`Blocked` result. Blocking is transitive, while independent operations continue.

- CF fails: QP skipped (depends on CF), QS/MN/MM continue (independent)
- QS fails: all others continue (nothing depends on QS)

## Sync atomicity

Recognized resource-local failures may produce a `Partial` result when other independent resource
units complete. Custom Formats may finish partially, but only a fully successful Custom Format
result permits Quality Profile execution. This prevents profiles from being scored against an
incomplete Custom Format set. Independent pipelines continue after another pipeline fails.

## Processing model

Each sync category follows three stages:

Plan, then Transaction (fetch + diff), then Persistence

### Plan (pre-sync)

The plan validates configuration against TRaSH Guides data, catching invalid TrashIds and resource
conflicts before any server interaction. Plan components execute sequentially because some depend on
others (QP planning reads from the CF plan to build score assignments). The output is a
`PipelinePlan` consumed by sync operations.

### Transaction

Fetches current state from the Servarr API (non-deterministic, changes independently) and computes
the transaction: what to create, update, delete, or skip. This is where validation complexity lives,
handling naming conflicts, dependency validation, and update-vs-create decisions.

### Persistence

Applies the computed changes. All validation is complete by this point, so this focuses on execution
reliability and state maintenance. Skipped entirely in preview mode.

## Terminal results and preview

Preview runs Plan and Transaction but skips Persistence. It returns the same calculated outcomes and
deltas that apply mode starts with, without writing service or sync state.

`ISyncOrchestrator.RunAsync()` returns one `SyncRunResult` containing ordered instance and pipeline
results. The Server stores this aggregate on the job before the run scope is disposed. No ambient
result store or lookup step exists. Progress remains a separate transient stream and never
determines terminal status. The Server does not currently expose the aggregate through HTTP; the
semantic results endpoint is a separate adapter concern governed by ADR-016.

## Error collection

The "collect and report later" pattern categorizes errors by source and timing:

- Configuration errors (plan): invalid TrashIds, malformed YAML, resource provider conflicts
- Server validation errors (transaction): naming conflicts, missing dependencies, API constraint
  violations
- Runtime errors (persistence): network issues, authentication failures, service unavailability

Diagnostics flow through `IPipelinePublisher`, which operations receive as a parameter. These events
support transient progress and presentation; semantic results alone determine terminal status.

Expected instance-wide failures attach a typed `OperationalFailure` to the instance result.
Unexpected failures stop the run and attach one opaque fault reference. Results completed before
the failure remain in the aggregate. Cancellation propagates and does not become a terminal result.

## Service abstraction

Sonarr and Radarr are similar but have differences in areas like Media Naming formats and Quality
Definition size limits. These differences are handled through service-specific implementations
behind domain interfaces (see [service-gateway-layer.md](service-gateway-layer.md)).

Media Naming has separate sync operations for Sonarr and Radarr since the APIs are completely
different. Both share the same `PipelineType.MediaNaming` identity; the `ShouldSkip` check ensures
only the relevant operation runs per instance.

## Extensibility

Adding a new sync category: implement a plan component (`IPlanComponent`) and an `ISyncOperation`
class. Declare dependencies in the operation. The orchestrator handles ordering automatically via
topological sort.

Handling service differences: use dependency injection for service-specific implementations rather
than duplicating processing paths.

## Architecture evolution

The sync system originally used a pipeline-of-phases architecture where each resource type had
separate fetch, transaction, preview, and persistence phase classes sharing mutable state through
context objects. Over time, phases were pulled out one by one (config became the plan system,
preview became a CLI concern, persistence became conditional). What remained was a complicated shell
around what is really just "fetch, diff, persist." The pipeline was replaced with self-contained
`ISyncOperation` classes that own their full lifecycle.
