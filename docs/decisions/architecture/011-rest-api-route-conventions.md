# ADR-011: REST API route conventions and resource model

- **Status:** accepted
- **Date:** 2026-07-24

## Context and Problem Statement

The HTTP server (ADR-008, ADR-010) needs an API surface covering sync operations, TRaSH Guides data,
and configured instance data. The original endpoint sketch placed all resources in one flat
namespace (`/api/custom-formats`, `/api/quality-profiles`), which conflated three distinct domains:
`GET /api/custom-formats` would have returned guide data while `DELETE /api/custom-formats` mutated
a Sonarr/Radarr instance. The OpenAPI spec and the Kiota-generated client bind permanently to these
URLs, so the structure has to be settled before endpoint work starts.

## Decision Drivers

- Guide resources, instance resources, and Recyclarr's own state have different owners, scopes, and
  lifecycles; the URL structure should make that visible
- Recyclarr is multi-instance; instance addressing must be first-class, not bolted on later
- Kiota generates the client from the committed spec; URL mistakes become client API mistakes
- Sync is long-running (10-60s) and needs a job-based lifecycle, not a request/response call
- Kubernetes probes and load balancers need a stable, unauthenticated health URL

## Considered Options

1. Flat namespace: all resources directly under `/api` (original sketch)
2. Domain namespaces: `guide`, `instances`, and `sync` groupings under a versioned `/api` prefix

## Decision Outcome

Chosen option: "domain namespaces", because the flat namespace forced unrelated resources to share
URLs and left instance addressing unsolved.

### Route grammar

```txt
/api/{version}/{namespace?}/{resource}[/{id}/{resource}[/{id}...]]
```

- **Version** is a path segment (`/api/v1/...`). The API version is independent from the application
  semver. Path versioning keeps per-version OpenAPI specs and Kiota clients unambiguous, and matches
  the `/api/v3/...` shape users already know from Sonarr/Radarr.
- **Namespaces** group related resources (`sync/`, `guide/{service}/`). They never resolve: no
  handler mounts at `/api/v1/sync` or `/api/v1/guide/{service}`; both 404. At most one conceptual
  namespace level. Needing a namespace inside a namespace signals a modeling error.
- **`{service}`** in guide paths is a closed enum discriminator (`radarr|sonarr`), not an
  identifier into a collection.
- **Resources** are either collections or singletons. Collections are plural and id-addressable
  (`instances/{name}`, `sync/jobs/{id}`). Singletons are singular and take no id (`sync/schedule`).
  The test: more than one can exist means collection; exactly one means singleton.
- **Naming** is kebab-case (`custom-formats`, `score-sets`).

### Resource model

```txt
GET    /health                                        # anonymous; outside /api

GET    /api/v1/guide/{service}/custom-formats
GET    /api/v1/guide/{service}/custom-format-groups
GET    /api/v1/guide/{service}/quality-profiles
GET    /api/v1/guide/{service}/qualities
GET    /api/v1/guide/{service}/score-sets
GET    /api/v1/guide/{service}/naming

GET    /api/v1/instances
GET    /api/v1/instances/{name}
GET    /api/v1/instances/{name}/custom-formats
DELETE /api/v1/instances/{name}/custom-formats/{id}
GET    /api/v1/instances/{name}/quality-profiles

POST   /api/v1/sync/jobs                              # 202 + Location
GET    /api/v1/sync/jobs                              # history and running state
GET    /api/v1/sync/jobs/{id}                         # cumulative snapshot
GET    /api/v1/sync/schedule                          # singleton; serve mode only
```

The three namespaces map to the three data owners: `guide` is read-only TRaSH Guides data scoped by
service type; `instances` is the user's configured Sonarr/Radarr instances scoped by instance name
(names are unique across services, enforced by config validation); `sync` is Recyclarr's own job and
schedule state.

### Supporting rules

- **Long-running operations are job resources.** Starting a sync means creating a job: `POST
  /sync/jobs` returns 202 with a `Location` header. There is no verb-style trigger endpoint. `GET
  /sync/jobs/{id}` returns the full cumulative progress snapshot (per-instance, per-operation), so a
  slow or skipped poll loses nothing. Clients poll; there is no SSE. "Is a sync running" is answered
  by the jobs collection, never by a singleton status resource.
- **No DELETE with a request body.** Bulk deletion happens per-item (`DELETE
  .../custom-formats/{id}`); the client loops. RFC 9110 gives DELETE bodies no defined semantics and
  intermediaries may drop them.
- **Health lives outside `/api`.** Probe URLs are configured statically in deployment manifests and
  must survive API version bumps. Keeping health outside `/api` also yields a single auth rule:
  everything under `/api` requires the API key; everything outside does not. Health is not part of
  the OpenAPI spec or the generated client. A future `/health/live` and `/health/ready` split is
  compatible with this placement.
- **Errors** follow RFC 9457 Problem Details on every endpoint.

### Consequences

- Good, because each URL identifies exactly one resource with one owner; GET and DELETE on the same
  path operate on the same thing
- Good, because instance addressing is explicit and the scheme survives the multi-instance reality
- Good, because mutation verbs (PUT/PATCH on `instances`) can be added later without restructuring
- Bad, because URLs are longer than the flat sketch and the CLI must loop for bulk deletes
- Bad, because the non-resolvable namespace rule is a discipline the code cannot enforce; it lives
  here and in the `http-server` skill
