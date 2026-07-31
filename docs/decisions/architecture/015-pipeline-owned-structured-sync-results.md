# ADR-015: Pipeline-owned structured sync results

- **Status:** Accepted
- **Date:** 2026-08-19
- **Supersedes:** ADR-009

## Context and Problem Statement

Core reduces many sync decisions to diagnostic prose. The HTTP server cannot expose those decisions
without transporting messages or reconstructing context that producers discarded. The existing
result work also combines planning, transaction data, presentation compatibility, and every
pipeline in changes too large to review safely.

Core needs a consumer-independent terminal result that preserves semantic outcomes and deltas. It
must retain the current Plan, Transaction, and Persistence stages, support preview, and make
dependency behavior explicit.

## Decision Drivers

- Core must contain no console, logging, notification, HTTP, or serialization concerns
- Preview and apply must report the same calculated deltas
- Pipeline dependencies require deterministic status and blocking rules
- Independent resource failures should not discard valid work
- Sync state must never claim an unacknowledged service write
- Each pipeline contract must be independently reviewable

## Considered Options

1. Keep diagnostic events as the authoritative result
2. Serialize existing transaction and service resource models
3. Return pipeline-owned semantic results from Core

## Decision Outcome

Chosen option: "Return pipeline-owned semantic results from Core", because producers retain the
facts needed by every adapter without coupling Core to any presentation or transport.

### Lifecycle

The stages remain distinct:

```txt
Plan
  configuration + cached guide data
  no Sonarr or Radarr interaction

Transaction
  fetch current service state
  calculate outcomes and deltas

Persistence
  apply calculated changes
```

Preview runs Plan and Transaction but skips Persistence. In apply mode, each pipeline runs its
Transaction and Persistence before a dependent pipeline starts. This preserves Custom Format ID
hydration before Quality Profile processing.

Planning and execution happen inside the background sync job. Only request selectors and
server-owned configuration validity are checked before job creation.

### Result hierarchy

Core returns one self-contained aggregate:

```txt
SyncRunResult
└── SyncInstanceResult
    └── PipelineResult
        ├── Outcomes
        └── Deltas
```

`SyncInstanceResult` includes the instance name and service type. Core results do not contain an
HTTP job ID. The server owns the job and associates the returned run result with it.

Instances and pipeline results use ordered, self-identifying lists. Dictionaries are private lookup
indexes only. Each pipeline owns concrete result, outcome, and delta types. A minimal
`PipelineResult` base carries common status; generic result abstractions are not required.

### Status and dependencies

Terminal pipeline status has four values:

- `Succeeded`: every intended resource completed its required stage
- `Partial`: at least one intended resource completed and at least one did not
- `Failed`: no resource completed, or a pipeline-wide failure prevented a usable result
- `Blocked`: the pipeline did not run because a dependency was not `Succeeded`

No resources and fully up-to-date resources are no-op successes. Pipelines that are not configured
or do not apply to the service are absent from the terminal result.

Only `Succeeded` satisfies a dependency. `Partial`, `Failed`, and `Blocked` block dependents.
Independent pipelines continue unless an instance-wide failure stops the instance.

Instance and run status derive from child terminal results. Progress never determines terminal
status.

### Semantic model

The canonical language is defined in `CONTEXT.md`.

- An outcome explains a decision, rejection, skip, or failure
- A delta is a calculated semantic difference, not proof of application
- A resource delta may contain value or structure deltas
- A fault is unexpected and is not an outcome
- Progress is transient and is not part of a terminal result

Core outcomes contain no diagnostic severity or rendered message. Pipeline status communicates hard
stops. Adapters may assign presentation severity from the typed contract.

### Shared execution invariants

Every pipeline follows the same rules:

1. Unexpected exceptions, cancellation, and instance-wide service failures propagate.
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

#### Custom Formats

- Managed identity is Trash ID; name is descriptive context
- One create, update, or delete is one resource delta
- Create, update, and delete are concrete delta variants
- Update details describe changed components, not full service resources
- Selection provenance may explain configuration, profile, or group selection
- Outcomes cover ambiguous matches, adoption, reference mismatches, and state conflicts
- Unchanged formats and counts are omitted
- Disabled deletion produces no deletion delta

#### Quality Profiles

- Guide-backed identity reuses `MappingKey` for Trash ID and configured name
- User-defined identity contains only the configured name
- One profile create or update is one resource delta
- Updates contain explicit value, quality-layout, and Custom Format score deltas
- Created profiles contain only effective state controlled by Recyclarr
- Outcomes cover reference mismatches, validation constraints, adoption, rename conflicts,
  ambiguous matches, and score collisions
- Quality Profile resources are independent, so recognized per-profile failures may continue
- Unchanged profiles and full service resource snapshots are omitted

#### Quality Sizes

- Reuse the domain `QualitySizeValue` numeric and unlimited variants
- One changed service quality is one delta
- Only changed minimum, preferred, and maximum values are present
- Outcomes cover missing definitions, missing qualities, and invalid ordering
- Service limits, magic unlimited values, and unchanged resources are omitted

#### Media Naming

- Sonarr and Radarr have separate result and delta types
- One naming settings resource is one delta with explicit value deltas
- Unknown guide format keys are reference mismatch outcomes
- Full current and desired naming resources are omitted

#### Media Management

- One settings resource is one optional delta
- Reuse the domain `PropersAndRepacksMode`
- No outcomes collection exists until a real outcome is identified

### Operational failures

Expected service failures use safe categories such as unavailable, unauthenticated, unauthorized,
rate limited, or rejected change. Resource-local rejections belong to pipeline results.
Instance-wide failures stop the instance. Unexpected failures expose only an opaque fault reference
outside Core presentation concerns.

### Consequences

- Good, because Core results preserve semantic context without consumer coupling
- Good, because preview and apply share one calculated result contract
- Good, because status and dependency behavior are explicit and testable
- Good, because each pipeline can migrate in an independent review slice
- Bad, because every pipeline needs deliberate outcome and delta models
- Bad, because partial persistence requires careful sync-state bookkeeping
