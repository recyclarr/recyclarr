# ADR-016: Semantic sync job results API

- **Scope:** `src/Recyclarr.Server/Features/Sync/` (job creation, job, and results endpoints)

## Context

Job progress is polled about every 500 ms for the 10-60 seconds a sync takes; terminal outcomes and
deltas are read once, after completion, and can be tens of KB per instance. Generated clients need
explicit schemas, HTTP status must describe the request rather than the background sync, and
server-owned configuration details must not leak through the sync API.

## Decision

Terminal results live on one sub-resource, `GET /api/v1/sync/jobs/{id}/results`, as semantic DTOs
that the server maps from Core results. The polled job resource carries only status and progress.

### Request and job lifecycle

`POST /api/v1/sync/jobs` validates only the request against the loaded configuration (ADR-019).

- Invalid request selectors return 400 and create no job
- A valid request creates a pending job and returns 202 with its location
- Planning, transaction, and persistence happen inside the background job

### Resource status

```txt
GET /api/v1/sync/jobs/{id}
  200  known job in any lifecycle state
  404  unknown or evicted job

GET /api/v1/sync/jobs/{id}/results
  200  terminal results, including partial and failed jobs
  409  known job is not terminal, or was skipped
  404  unknown or evicted job
```

A failed background sync still returns 200 when its result is retrieved successfully. HTTP 500 is
reserved for failure while handling the current HTTP request.

### Response hierarchy

The terminal response contains:

```txt
SyncJobResultsResponse
├── Id
├── Status
├── Fault
└── Instances
    ├── Name
    ├── Service (discriminator)
    ├── Status
    ├── Failure
    ├── Fault
    ├── PlanningOutcomes
    └── Pipelines
```

`Pipelines` is an object with named optional properties such as `customFormats`, `qualityProfiles`,
and `qualitySizes`. Pipelines that are not configured or do not apply are omitted. A blocked
pipeline includes its status and blocking dependency.

An instance's `service` field is the discriminator for its Sonarr or Radarr response type. Both
types use one `naming` pipeline property; its schema is selected by the instance type. The service
is represented by that concrete type rather than by a second ordinary property on the DTO.

Each pipeline response contains explicit create, update, and delete collections where applicable.
Outcomes are grouped under one pipeline-specific `outcomes` object with named typed collections.
Empty or inapplicable collections may be omitted.

An outcome appears under each resource it affects, even when that repeats it. A response never
refers to a resource elsewhere in the response by identity. One instance-level source of a problem
that affects several quality profiles, such as the instance `quality_definition` on Sonarr v5,
appears under each affected profile; profiles that use their own source show nothing.

The response does not include request settings, progress snapshots, diagnostic messages, unchanged
resource counts, full Sonarr or Radarr resources, or raw Core models.

### Contract ownership

Core owns semantic results. Server explicitly maps Core results into transport DTOs. Server DTOs may
flatten Core aggregates, sanitize fields, and omit implementation data, but no current renderer or
logger dictates their shape.

Core types contain no serialization attributes for this API. Server does not directly serialize Core
domain models.

### Failures

Resource-local failures appear as typed pipeline outcomes. Instance-wide service failures and
unexpected instance faults attach to the instance result. Unexpected failures outside an instance
attempt attach to the job result. Faults expose only opaque references. Parent status derives from
child results; one failure is not duplicated at every level.

The API never exposes credentials, server paths, service URLs, raw response bodies, exception
messages, stack traces, or C# type names.

### Versioning

- Adding an optional pipeline or outcome collection is additive within an API version
- Adding a status value is breaking because generated clients may use exhaustive switches
- Removing, renaming, or changing field meaning requires a new API version
- Core changes do not affect API versioning unless the mapped HTTP contract changes

## Rejected Options

- Results embedded in the polled job resource: serializes a large static payload on every poll.
- One endpoint per pipeline or resource: the CLI needs every operation at once, so this multiplies
  round trips.
- Reporting a shared problem once with a list of affected resource identities: adds cross-references
  that no other part of the response uses.
- An empty successful result while the job runs: hides that results do not exist yet.

## Consequences

- Polling cost is independent of terminal result size.
- HTTP and sync statuses cannot be confused.
- Core and transport models evolve separately; the server maps every result variant explicitly.
- Clients make a second request after the job completes.

## History

- 2026-07-26: Results move to a separate sub-resource of the job.
- 2026-08-19: Results become semantic DTOs; 409 until the job is terminal.
- 2026-09-17: Faults attach to instances and jobs (ADR-017).
- 2026-09-26: Job creation no longer validates configuration (ADR-019).
- 2026-10-18: Outcomes appear under each affected resource; no cross-references by identity.
