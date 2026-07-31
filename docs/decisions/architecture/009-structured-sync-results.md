# ADR-009: Structured sync results via generic base class and typed aggregate

- **Status:** Accepted
- **Date:** 2026-05-16

## Context and Problem Statement

The sync port interface (`ISyncOrchestrator`) returns `ExitStatus` (pass/fail). For the HTTP server
work (REC-141), it needs to return structured per-instance, per-operation transaction data that can
be serialized as JSON. The challenge is that each sync operation produces a different result type
(`CustomFormatTransactionData`, `QualityProfileTransactionData`, etc.) and the orchestrator must
collect them without compile-time knowledge of each specific type.

## Decision Drivers

- Operations produce genuinely different data shapes (no natural common interface beyond "some
  data")
- The orchestrator is a pass-through; it routes results by key and never inspects contents
- Results must serialize cleanly to JSON for the HTTP API
- Operations should remain testable in isolation (Compute and Persist independently)
- Core must stay free of serialization concerns (no STJ attributes on domain types)
- The same result shape serves both preview and non-preview runs
- Job storage persists everything in `TResult`; it can't distinguish "for Persist" vs "for external
  consumers"

## Considered Options

1. Typed container with named nullable properties (for the aggregate DTO)
2. Dictionary of tagged results (`IReadOnlyDictionary<PipelineType, object>`)
3. Polymorphic interface with `[JsonDerivedType]` per operation result type
4. Flatten to a uniform "changes" structure

For how operations expose results:

1. Mutable internal state (current design: `_transactionOutput` field)
2. Immutable passthrough via generic base class (`SyncOperation<TResult>`)

## Decision Outcome

Chosen option: "Typed container with nullable properties" (1) for the aggregate DTO, and "Immutable
passthrough via generic base class" (6) for how operations produce and consume results.

### Aggregate DTO: typed container

```csharp
record SyncInstanceResult
{
    public CustomFormatSyncResult? CustomFormats { get; init; }
    public QualityProfileSyncResult? QualityProfiles { get; init; }
    public QualitySizeSyncResult? QualitySizes { get; init; }
    public SonarrNamingSyncResult? SonarrNaming { get; init; }
    public RadarrNamingSyncResult? RadarrNaming { get; init; }
    public MediaManagementSyncResult? MediaManagement { get; init; }
}
```

Null means "this operation didn't produce results for this instance" (skipped, not applicable, or
dependency failed). With `JsonIgnoreCondition.WhenWritingNull`, absent operations are omitted from
the wire format. This handles service affinity naturally: Sonarr naming is null for Radarr
instances, Radarr naming is null for Sonarr instances. Consumers don't need to reason about why a
property is absent.

Option 3 (polymorphic interface) was rejected because OCP doesn't apply to a DTO aggregate. The set
of sync operation types is a closed domain concept that changes rarely (roughly once per year). When
it does change, every consumer (CLI renderer, HTTP response mapper, SSE event formatter) needs
deliberate work to handle the new type. A compile-time signal (new property, unhandled) is more
correct than a runtime polymorphic dispatch that silently ignores unknown types. STJ polymorphism
also requires declaring all derived types centrally anyway, so the "open for extension" benefit is
illusory at the serialization boundary.

### Operations: generic base class with immutable passthrough

```csharp
internal interface ISyncOperation
{
    PipelineType Type { get; }
    string Description { get; }
    IReadOnlyList<PipelineType> Dependencies { get; }
    bool ShouldSkip(PipelinePlan plan);
    Task<object?> Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct);
    Task Persist(object? computeResult, IPipelinePublisher publisher, CancellationToken ct);
}

internal abstract class SyncOperation<TResult> : ISyncOperation
{
    async Task<object?> ISyncOperation.Compute(...)
        => await Compute(plan, publisher, ct);

    Task ISyncOperation.Persist(object? computeResult, ...)
        => Persist((TResult)computeResult!, publisher, ct);

    protected abstract Task<TResult> Compute(...);
    protected abstract Task Persist(TResult computeResult, ...);
}
```

The orchestrator calls `Compute`, stores the returned `object?` in job storage, and passes it back
into `Persist`. Operations have no mutable state. Each method is independently testable: Compute by
asserting on the return value, Persist by constructing a `TResult` directly.

The reference cast (`object?` → `TResult`) has no performance cost because all result types are
reference types (records). No boxing occurs.

### TResult = job result model (not "everything Persist needs")

`TResult` contains only data that represents the operation's outcome. Everything in it gets
persisted to job storage. Lookup data, API clients, caches, and state management remain as injected
services on the operation class. Persist receives the result model as a parameter and uses its
injected services for anything else.

### Job ID and storage

The orchestrator creates a `JobId` at the start of a sync run, stores per-operation results keyed by
`(JobId, instanceName, PipelineType)`, and returns the `JobId` to the caller. The caller retrieves
results from storage and maps domain objects to DTOs at the adapter boundary.

This applies even for today's CLI (in-memory storage, synchronous flow). The indirection costs
nothing and means the HTTP server later uses the same pattern without rework.

### Boundaries

```text
Core (produces domain objects via Compute)
    ↕ job storage interface: store/retrieve domain objects
Job Storage (opaque implementation, own internal entity model)
    ↕ retrieve domain objects
Adapter (maps domain → DTO via Mapperly, delivers to caller)
```

Core doesn't know how storage works. The adapter doesn't know either. Both talk to it through an
interface that accepts and returns domain objects.

### Consequences

- Good, because operations become pure function pairs (Compute produces data, Persist consumes data)
- Good, because Persist is independently testable without running Compute first
- Good, because the aggregate DTO gives compile-time signals when operation types are added
- Good, because null properties naturally express service affinity and skip semantics
- Good, because `RenderPreview()` is removed from the operation interface (adapter concern)
- Good, because job ID + storage pattern works for both CLI and HTTP without rework
- Bad, because adding a new sync operation type requires updating the aggregate DTO (acceptable for
  a change that happens roughly annually)
- Bad, because `object?` at the interface boundary loses type safety (contained to a single
  pass-through point; operations and consumers are fully typed)
