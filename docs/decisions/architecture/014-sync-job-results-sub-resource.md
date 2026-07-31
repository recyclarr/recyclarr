# ADR-014: Sync job results as a separate sub-resource

- **Status:** accepted
- **Date:** 2026-07-26

## Context and Problem Statement

`GET /api/v1/sync/jobs/{id}` returns the cumulative progress snapshot and is polled roughly every
500ms for the 10-60 seconds a sync takes. ADR-009 also requires the job to expose per-instance,
per-operation transaction data so the CLI can render previews from JSON instead of from in-process
domain objects. Putting that data on the polled resource means the same payload is serialized and
transferred on every poll, even though a client reads it exactly once.

## Decision Drivers

- Progress is read ~120 times per sync; transaction data is read once, after the job terminates
- Transaction data is tens of KB per instance even after projecting it down to what renderers use
- Mapping domain results to DTOs on every poll wastes server work proportional to poll frequency
- The CLI renderer needs every operation's data at once; per-operation fetches would multiply round
  trips (rejected in REC-141 discussion for the same reason aggregation exists at all)
- Results apply to preview and non-preview runs alike (ADR-009), so this is not a preview-only
  concern

## Considered Options

1. Embed results in the job resource returned by `GET /sync/jobs/{id}`
2. Expose results as one sub-resource: `GET /sync/jobs/{id}/results`
3. Expose one sub-resource per operation type, e.g. `GET /sync/jobs/{id}/results/{instance}/custom-formats`

## Decision Outcome

Chosen option: "one results sub-resource", because it splits the two payloads along their access
patterns without fragmenting the aggregate a renderer needs.

```txt
GET /api/v1/sync/jobs/{id}          # polled; status and progress only
GET /api/v1/sync/jobs/{id}/results  # fetched once; per-instance transaction data
```

Option 1 keeps one URL but couples a large, static payload to a high-frequency poll. Option 3
restores the N-fetch problem the aggregate was designed to avoid, and no consumer wants a single
operation in isolation.

### Response shape

The results document is a list of instances, each carrying the nullable per-operation properties
from `SyncInstanceResult` (custom formats, quality profiles, quality sizes, Sonarr naming, Radarr
naming, media management). Null means the operation produced nothing for that instance, and
`JsonIgnoreCondition.WhenWritingNull` keeps it off the wire, exactly as ADR-009 describes.

DTOs are shaped for what consumers render, not as mirrors of the domain types. The custom format
renderer needs an action, a name, a trash ID, a source label, and an inclusion reason; it never
touches `CustomFormatResource.Specifications`. Mirroring the domain would ship full custom format
bodies that nothing reads.

### Availability during a run

The endpoint always returns 200 for a known job. A 404 means the id is unknown or evicted, matching
the job resource. Fetching before the sync finishes yields an empty instance list rather than an
error the client has to special-case, so a client that races the job still gets a valid document.

Results are captured into the server's job record when the run completes, before the run's lifetime
scope is disposed. `ISyncRunStorage` lives inside that scope, so reading it lazily when the request
arrives would be too late. Capture is all-at-once rather than per instance because no per-instance
completion event exists to hang incremental capture on, and progress polling already covers the
"what is happening right now" case, so partial results mid-run would buy nothing.

### Consequences

- Good, because the polled resource stays small and cheap to serialize no matter how large a sync is
- Good, because domain to DTO mapping runs once per job instead of once per poll
- Good, because the CLI still gets every operation for every instance in a single fetch
- Good, because the results document can grow (new operations, richer fields) without affecting poll
  cost
- Bad, because a client that wants both needs two requests, and the CLI must decide when to make the
  second one
- Bad, because job records now hold both progress and mapped results, so the 50-job retention cap
  bounds more memory per entry than it did before
