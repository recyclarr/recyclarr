# ADR-015: Pipeline-owned structured sync results

- **Scope:** `src/Recyclarr.Core/Sync/`, `src/Recyclarr.Core/Pipelines/`

## Context

Diagnostic prose cannot carry sync decisions to the HTTP API without transporting messages or
reconstructing context that producers discarded. Every consumer (CLI rendering, HTTP results,
notifications) needs the same facts. Core must stay free of console, logging, notification, HTTP,
and serialization concerns; preview and apply must report the same planned state; and sync state
must never claim a service write that was not acknowledged. Clients that check a planned sync
against a policy need every value the sync manages, not only the changed ones.

## Decision

Core returns pipeline-owned semantic results: typed outcomes and planned resources that each
pipeline defines. Producers keep the facts every adapter needs, and no adapter's presentation shapes
them.

### Lifecycle

The stages remain distinct:

```txt
Plan
  configuration + cached guide data
  no Sonarr or Radarr interaction

Transaction
  fetch current service state
  calculate outcomes and planned resources

Persistence
  apply calculated changes
```

Preview runs Plan and Transaction but skips Persistence. In apply mode, each pipeline runs its
Transaction and Persistence before a dependent pipeline starts. This preserves Custom Format ID
hydration before Quality Profile processing.

Planning and execution happen inside the background sync job; job creation checks only the request
(ADR-019).

### Result hierarchy

Core returns one self-contained aggregate:

```txt
SyncRunResult
├── Fault
└── SyncInstanceResult
    ├── Fault
    ├── PlanningOutcomes
    └── PipelineResult
        ├── Outcomes
        └── Planned resources
```

`SyncInstanceResult` includes the instance name and service type. Core results do not contain an
HTTP job ID. The server owns the job and associates the returned run result with it.

Planning outcomes belong to the instance because planning validates relationships across pipelines
before any pipeline executes. Blocking planning outcomes fail the instance; non-blocking planning
outcomes retain semantic context without changing terminal status.

Instances and pipeline results use ordered, self-identifying lists. Dictionaries are private lookup
indexes only. Each pipeline owns concrete result, outcome, and planned resource types. A minimal
`PipelineResult` base carries common status; generic result abstractions are not required.

### Status and dependencies

Terminal pipeline status has four values:

- `Succeeded`: every intended resource completed its required stage
- `Partial`: at least one intended resource completed and at least one did not
- `Failed`: no resource completed, or a pipeline-wide failure prevented a usable result
- `Blocked`: the pipeline did not run because a dependency was not `Succeeded`

An intended resource is incomplete when its planned change did not apply, so the service still holds
stale or wrong data for it. A planned target that does not exist in the service (for example, a
guide quality an older service version lacks) has no data to be wrong and is not incomplete.

No resources and fully up-to-date resources are no-op successes. Pipelines that are not configured
or do not apply to the service are absent from the terminal result.

Only `Succeeded` satisfies a dependency. `Partial`, `Failed`, and `Blocked` block dependents.
Independent pipelines continue unless an instance-wide failure stops the instance.

Instance and run status derive from child terminal results. Progress never determines terminal
status.

### Semantic model

- An outcome explains a decision, rejection, skip, or failure
- A planning outcome explains a condition found before pipeline execution
- A planned resource is one resource the sync manages, with every value it manages, changed or not;
  it is calculated before persistence and is not proof of application
- A planned resource's action is create, update (at least one managed value changes), or unchanged
- A planned value carries the desired value, the service value (absent on create), and whether it
  changes, decided by the same equality the pipeline uses to decide whether to write
- A fault is unexpected and is not an outcome
- Progress is transient and is not part of a terminal result

Core outcomes contain no diagnostic severity or rendered message. Pipeline status and blocking
planning outcome types communicate hard stops. The server assigns each outcome type one severity by
the criteria of ADR-033.

### Shared execution invariants

Every pipeline follows the same rules:

1. Unexpected exceptions propagate to the owning boundary. Pipeline exceptions reach the instance
   boundary; cancellation and failures outside an instance attempt reach the run boundary.
2. Only recognized resource-local failures may become outcomes.
3. Resource-local continuation is allowed only for independent resource units.
4. Preview never writes service state or sync state.
5. Sync state records only acknowledged successful writes and deletions.
6. Handled partial persistence saves acknowledged state before returning.
7. Sync-state persistence failure propagates as an operational failure.
8. Failed or partial pipelines block dependents; independent pipelines may continue.
9. Logs and progress never affect terminal status.

Quality Profiles may continue after one profile is rejected. Custom Formats may also end partial if
some formats completed, but Quality Profiles remain blocked because the dependency was not fully
successful. Batch and singleton pipelines follow their API request granularity.

### Pipeline contracts

#### Planning

- Planning outcomes belong to the instance rather than an individual pipeline
- A planning outcome blocks the instance when the configuration it describes cannot be resolved
  unambiguously, because dependent pipelines would then sync an incomplete or wrong selection
- A planning outcome that leaves the resolved selection intact is non-blocking
- Outcomes retain the configuration context needed to locate and fix the problem

#### Custom Formats

- Managed identity is Trash ID; name is descriptive context
- Every configured format is one planned resource; a format skipped by an outcome has none
- Deletes are a separate list of identities; disabled deletion produces none
- Conditions are listed by name with their change, not as full specification contents
- Selection provenance may explain configuration, profile, or group selection

#### Quality Profiles

- Guide-backed identity reuses `MappingKey` for Trash ID and configured name
- User-defined identity contains only the configured name
- Every valid configured profile is one planned resource with every field the sync writes
- Every Custom Format score in the profile is present, with a reason: set by configuration or guide,
  reset by `reset_unmatched_scores`, or unmanaged (kept from the service)
- Quality Profile resources are independent, so recognized per-profile failures may continue
- Full service resource snapshots are omitted

#### Quality Sizes

- Reuse the domain `QualitySizeValue` numeric and unlimited variants
- Every planned quality the service has is one planned resource; other qualities are not managed
- Service limits and magic unlimited values are omitted

#### Media Naming

- Sonarr and Radarr have separate result and planned resource types
- One naming settings resource holds only configured fields; unconfigured fields are not managed
- Unknown guide format keys are reference mismatch outcomes
- Full current and desired naming resources are omitted

#### Media Management

- One settings resource is one optional planned resource
- Reuse the domain `PropersAndRepacksMode`
- No outcomes collection exists until a real outcome is identified

### Operational failures

Expected service failures use safe categories such as unavailable, unauthenticated, unauthorized,
rate limited, or rejected change. Resource-local rejections belong to pipeline results.
Instance-wide failures stop the instance. Unexpected failures expose only an opaque fault reference
outside Core presentation concerns. ADR-017 defines fault attribution and continuation across
independent instances.

## Rejected Options

- Diagnostic events as the authoritative result: prose loses the context consumers need.
- Serializing transaction and service resource models: couples consumers to internal and service
  shapes, and ships full resources nothing reads.
- A typed aggregate with one nullable property per operation behind an `object?` operation contract:
  hides status and dependency rules and loses typing at the boundary.

## Consequences

- Preview and apply share one calculated result contract.
- Status and dependency behavior are explicit and testable.
- Every pipeline needs deliberate outcome and planned resource models.
- Consumers that report only changes filter by action and changed values.
- Partial persistence needs careful sync-state bookkeeping.

## History

- 2026-05-16: Operations return typed results collected into a per-instance aggregate.
- 2026-08-19: Replace the aggregate with pipeline-owned semantic outcomes, deltas, and statuses.
- 2026-09-13: Planning outcomes belong to the instance.
- 2026-09-17: Fault attribution moves to ADR-017.
- 2026-11-02: Unknown Custom Formats are blocking planning outcomes.
- 2026-11-03: Blocking planning outcomes are defined by a criterion instead of a list.
- 2026-11-05: Targets missing from the service are not incomplete resources.
- 2026-11-08: Deltas become planned resources with every managed value, changed or not.
