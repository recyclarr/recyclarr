# ADR-011: REST API route conventions and resource model

- **Scope:** every HTTP endpoint in `src/Recyclarr.Server/Features/`

## Context

The server exposes TRaSH Guides data, the user's Sonarr and Radarr instances, and Recyclarr's own
sync jobs. These have different owners and lifecycles. A flat namespace (`/api/custom-formats`)
would let `GET` return guide data while `DELETE` on the same path changed an instance. The committed
OpenAPI spec and the generated client bind to the URLs, so URL mistakes become client API mistakes.
Sync runs for 10-60 seconds.

## Decision

Routes use domain namespaces (`guide`, `instances`, `sync`) under a versioned `/api` prefix.

### Route grammar

```txt
/api/{version}/{namespace?}/{resource}[/{id}/{resource}[/{id}...]]
```

- **Version** is a path segment (`/api/v1/...`). The API version is independent from the application
  semver. Path versioning keeps per-version OpenAPI specs and clients unambiguous, and matches the
  `/api/v3/...` shape users already know from Sonarr/Radarr.
- **Namespaces** group related resources (`sync/`, `guide/{service}/`). They never resolve: no
  handler mounts at `/api/v1/sync` or `/api/v1/guide/{service}`; both 404. At most one conceptual
  namespace level. Needing a namespace inside a namespace signals a modeling error.
- **`{service}`** in guide paths is a closed enum discriminator (`radarr|sonarr`), not an identifier
  into a collection.
- **Resources** are either collections or singletons. Collections are plural and id-addressable
  (`instances/{name}`, `sync/jobs/{id}`). Singletons are singular and take no id (`sync/schedule`).
  The test: more than one can exist means collection; exactly one means singleton.
- **Naming** is kebab-case (`custom-formats`, `score-sets`).

### Resource model

```txt
GET    /api/v1/guide/{service}/custom-formats
GET    /api/v1/guide/{service}/custom-format-groups
GET    /api/v1/guide/{service}/quality-profiles
GET    /api/v1/guide/{service}/qualities
GET    /api/v1/guide/{service}/score-sets
GET    /api/v1/guide/{service}/naming

GET    /api/v1/instances/{name}/custom-formats
DELETE /api/v1/instances/{name}/custom-formats/{id}

POST   /api/v1/sync/jobs                              # 202 + Location
GET    /api/v1/sync/jobs                              # history and running state
GET    /api/v1/sync/jobs/{id}                         # cumulative snapshot
GET    /api/v1/sync/jobs/{id}/results                 # terminal results (ADR-016)
GET    /api/v1/sync/schedule                          # singleton
```

The three namespaces map to the three data owners: `guide` is read-only TRaSH Guides data scoped by
service type; `instances` is the user's configured Sonarr/Radarr instances scoped by instance name
(names are unique across services, enforced by config validation); `sync` is Recyclarr's own job and
schedule state. New routes MUST fit this grammar; instance configuration routes follow ADR-012 when
a consumer needs them.

### Supporting rules

- **Long-running operations are job resources.** Starting a sync means creating a job:
  `POST /sync/jobs` returns 202 with a `Location` header. There is no verb-style trigger endpoint.
  `GET /sync/jobs/{id}` returns the cumulative instance progress snapshot defined by ADR-018, so a
  slow or skipped poll loses no current state. Clients poll; there is no SSE. "Is a sync running" is
  answered by the jobs collection, never by a singleton status resource.
- **No DELETE with a request body.** Bulk deletion happens per-item
  (`DELETE .../custom-formats/{id}`); the client loops. RFC 9110 gives DELETE bodies no defined
  semantics and intermediaries may drop them.
- **Probes and metrics are not API routes.** They live on the management port (ADR-029), outside
  this grammar, the OpenAPI spec, and the generated client, so API version bumps never move them.
- **Errors** follow RFC 9457 Problem Details on every endpoint.
- **Collections return summaries.** A collection response is an object that wraps its item array, so
  paging or metadata can be added without a breaking change. Items carry only the fields list
  consumers need, plus a stable id that a `/{id}` route can use later. Name item DTOs
  `*SummaryResponse` so the full-resource name stays free for the `/{id}` representation. Adding
  summary fields is additive; removing them is breaking.

## Rejected Options

- Flat namespace under `/api`: unrelated resources share URLs and instance addressing is unsolved.

## Consequences

- Each URL identifies one resource with one owner; `GET` and `DELETE` on a path act on the same
  thing.
- Mutation verbs on `instances` can be added without restructuring.
- URLs are longer, and the CLI loops for bulk deletes.
- The non-resolvable namespace rule is not enforced by code; it lives here and in the `http-server`
  skill.

## History

- 2026-07-24: Adopt versioned domain namespaces, job resources, and health outside `/api`.
- 2026-09-17: Job resource returns the cumulative instance snapshot from ADR-018.
- 2026-09-26: Collections return wrapped summary items.
- 2026-10-16: Resource model matched to implemented routes.
- 2026-10-17: Health moves from `/health` to the management port (ADR-029).
