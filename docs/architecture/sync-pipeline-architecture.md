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

Each resource type implements `ISyncOperation` with two main methods:

- `Compute()` fetches current state from the Servarr API and computes the diff against the plan. All
  validation and conflict detection happens here.
- `Persist()` applies the computed changes to the service. Only called when not in preview mode.

The orchestrator calls these separately:

```csharp
await operation.Compute(plan, publisher, ct);

if (settings.Preview)
    operation.RenderPreview(config.InstanceName);
else
    await operation.Persist(publisher, ct);
```

Operations store compute results as internal state, used by both `Persist()` and `RenderPreview()`.
Each operation instance is scoped to one sync run for one service instance and discarded afterward.

### Skipping operations

Each operation declares whether it should skip via `ShouldSkip(plan)`. Service affinity is encoded
in the plan components themselves (e.g. the Sonarr naming plan component produces nothing for Radarr
instances), and config presence is checked against the plan (no `quality_sizes` section means the
quality size operation skips). The orchestrator partitions operations by skip status before
topological sorting.

## Dependency management

Operations declare dependencies via `ISyncOperation.Dependencies`. The orchestrator uses topological
sort to determine execution order. When an operation fails, only its dependents are skipped;
independent operations continue.

- CF fails: QP skipped (depends on CF), QS/MN/MM continue (independent)
- QS fails: all others continue (nothing depends on QS)

## Sync atomicity

Operations fall into two categories based on dependency relationships:

**Independent operations** (Quality Profiles, Quality Sizes, Media Naming, Media Management): no
other operation depends on these resources. Individual items can sync independently. If item A fails
validation, item B can still sync. Partial sync is acceptable because each item is self-contained.

**Dependent operations** (Custom Formats): other operations depend on these (QP uses CFs for
scoring). ALL items must sync successfully or the entire operation fails. Partial sync would cause
silent, non-deterministic behavior (incomplete CFs lead to incorrect QP scoring). Operation failure
cascades to skip dependent operations.

## Processing model

Each sync category follows an identical pattern in two stages:

Plan (pre-sync) then Sync: Compute (fetch + diff) then Persist

### Plan (pre-sync)

The plan validates configuration against TRaSH Guides data, catching invalid TrashIds and resource
conflicts before any server interaction. Plan components execute sequentially because some depend on
others (QP planning reads from the CF plan to build score assignments). The output is a
`PipelinePlan` consumed by sync operations.

### Compute

Fetches current state from the Servarr API (non-deterministic, changes independently) and computes
the transaction: what to create, update, delete, or skip. This is where validation complexity lives,
handling naming conflicts, dependency validation, and update-vs-create decisions.

### Persist

Applies the computed changes. All validation is complete by this point, so this focuses on execution
reliability and state maintenance. Skipped entirely in preview mode.

## Preview rendering

Preview (dry-run) is a CLI adapter concern, not a sync engine concern. In preview mode, the pipeline
runs `Compute()` for every operation but skips `Persist()`. Compute results are stored in job storage
and retrieved after the sync run completes.

The CLI adapter (`PreviewRenderer`) calls `ISyncJobResults.GetInstanceResult()` to retrieve typed
per-operation results, then dispatches to per-operation renderer classes that format the output using
Spectre.Console tables and trees. The HTTP server adapter will map the same results to JSON DTOs via
Mapperly.

## Error collection

The "collect and report later" pattern categorizes errors by source and timing:

- Configuration errors (plan): invalid TrashIds, malformed YAML, resource provider conflicts
- Server validation errors (compute): naming conflicts, missing dependencies, API constraint
  violations
- Runtime errors (persist): network issues, authentication failures, service unavailability

Diagnostics flow through `IPipelinePublisher`, which operations receive as a parameter. Operations
call `publisher.AddError()` and `publisher.AddWarning()` during compute, and `publisher.SetStatus()`
during persist.

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
