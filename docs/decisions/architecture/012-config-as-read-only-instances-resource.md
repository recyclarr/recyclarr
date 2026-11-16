# ADR-012: Configuration served as a read-only instances resource

- **Scope:** any HTTP representation of configuration under `/api/v1/instances`

## Context

Configuration is a set of named instance sections, so a `/config` resource and an `/instances`
resource would expose the same state under two URLs. Configuration contains secrets (instance API
keys) that must never reach clients. Once a DTO ships in the OpenAPI spec, generated clients bind
to it, and storage may later move from YAML to a database with mutation endpoints.

## Decision

`/api/v1/instances` is the only HTTP view of configuration. MUST NOT add a `/config` or `/configs`
resource. No consumer needs instance configuration today, so `GET /instances` and
`GET /instances/{name}` are not implemented (ADR-019); when one does, these rules apply:

- `GET /instances` lists instances (name, service type, summary); `GET /instances/{name}` returns
  one instance's parsed, validated configuration.
- **DTOs are domain-shaped, not YAML-shaped.** Deprecated aliases, compatibility shims, and file
  layout stay behind the parse boundary, so a storage change is invisible to clients.
- **Secrets are absent, not masked.** `api_key` and similar fields do not exist in the DTO.
- **Mutation is additive.** If storage moves into the server, `PUT`/`PATCH` on
  `/instances/{name}` extend the same URLs.

## Rejected Options

- `GET /api/config` as one document: no consumer wants a config blob, and it duplicates instances.
- `/configs` collection mirroring YAML sections: exposes file layout as the contract.

## Consequences

- One URL owns instance configuration.
- Secrets cannot leak through the API whatever lands in a gitops repository.
- Non-instance settings (`settings.yml`) have no API view; one must be designed if a consumer
  appears.

## History

- 2026-07-24: `/instances` is the only configuration resource; secrets absent; read-only.
- 2026-09-26: Instance configuration routes deferred until a consumer exists (ADR-019).
