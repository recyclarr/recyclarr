# ADR-016: Semantic sync job results API

- **Status:** Accepted
- **Date:** 2026-08-19
- **Supersedes:** ADR-014

## Context and Problem Statement

Sync job progress is polled repeatedly, while terminal outcomes and deltas are read after completion.
The existing response work carries diagnostic prose and shapes DTOs around the current CLI. It also
returns an empty successful result while a job is still running, which hides resource state.

The HTTP contract needs stable semantic DTOs independent of Core serialization and current consumer
presentation.

## Decision Drivers

- Progress and terminal results have different access patterns
- HTTP status must describe the request, not the background sync outcome
- Generated clients need explicit schemas without arbitrary payload dictionaries
- Server-owned configuration details must not leak through the sync API
- New outcome variants need predictable API versioning rules

## Considered Options

1. Embed terminal results in the polled job resource
2. Keep one terminal results sub-resource with semantic DTOs
3. Expose one result endpoint per pipeline or resource

## Decision Outcome

Chosen option: "Keep one terminal results sub-resource with semantic DTOs", because it keeps the
poll response small while returning one coherent terminal aggregate.

### Request and job lifecycle

`POST /api/v1/sync/jobs` performs only request and server-configuration validation before creating a
job.

- Invalid request selectors return 400 and create no job
- Invalid server-owned configuration returns 500 and creates no job
- A valid request creates a pending job and returns 202 with its location
- Planning, transaction, and persistence happen inside the background job

Configuration parsing details and deprecations remain in server logs. A future remote configuration
write API will define its own typed validation responses when that capability exists.

### Resource status

```txt
GET /api/v1/sync/jobs/{id}
  200  known job in any lifecycle state
  404  unknown or evicted job

GET /api/v1/sync/jobs/{id}/results
  200  terminal results, including partial and failed jobs
  409  known job is not terminal
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
    ├── Service
    ├── Status
    └── Pipelines
```

`Pipelines` is an object with named optional properties such as `customFormats`,
`qualityProfiles`, and `qualitySizes`. Pipelines that are not configured or do not apply are
omitted. A blocked pipeline includes its status and blocking dependency.

Each pipeline response contains explicit create, update, and delete collections where applicable.
Outcomes are grouped under one pipeline-specific `outcomes` object with named typed collections.
Empty or inapplicable collections may be omitted.

The response does not include request settings, progress snapshots, diagnostic messages, unchanged
resource counts, full Sonarr or Radarr resources, or raw Core models.

### Contract ownership

Core owns semantic results. Server explicitly maps Core results into transport DTOs. Server DTOs
may flatten Core aggregates, sanitize fields, and omit implementation data, but no current renderer
or logger dictates their shape.

Core types contain no serialization attributes for this API. Server does not directly serialize
Core domain models.

### Failures

Resource-local failures appear as typed pipeline outcomes. Instance-wide service failures attach to
the instance result. Unexpected background failures attach to the job result as an opaque reference.
Parent status derives from child results; one failure is not duplicated at every level.

The API never exposes credentials, server paths, service URLs, raw response bodies, exception
messages, stack traces, or C# type names.

### Versioning

- Adding an optional pipeline or outcome collection is additive within an API version
- Adding a status value is breaking because generated clients may use exhaustive switches
- Removing, renaming, or changing field meaning requires a new API version
- Core changes do not affect API versioning unless the mapped HTTP contract changes

### Consequences

- Good, because polling cost is independent of terminal result size
- Good, because generated clients receive explicit, reviewable contracts
- Good, because HTTP and sync statuses cannot be confused
- Good, because Core and transport models can evolve for their own responsibilities
- Bad, because Server needs explicit mapping for every supported result variant
- Bad, because clients make a second request after job completion
