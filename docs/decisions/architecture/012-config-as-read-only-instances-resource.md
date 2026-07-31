# ADR-012: Configuration served as a read-only instances resource

- **Status:** accepted
- **Date:** 2026-07-24

## Context and Problem Statement

The original HTTP API sketch included `GET /api/config` returning "parsed and validated
configuration" with no stated consumer. Recyclarr's configuration is essentially a set of named
instance sections, so a config resource and an instances resource would expose the same underlying
state under two URLs. The API needs one authoritative representation of configuration, and that
choice depends on who owns the config: the user (hand-edited YAML) or the server (structured
storage).

## Decision Drivers

- The MVP deployment model is gitops: users store YAML in version control, Kubernetes mounts it as
  ConfigMaps, and the server reads it at startup
- Configuration contains secrets (instance API keys) that must not flow from version control through
  the API to clients
- The DTO shape becomes a permanent contract once the OpenAPI spec ships and Kiota clients bind to
  it
- Long term, config storage may move to a database with mutation endpoints (the Sonarr/Radarr
  model), and that shift must not break the API
- Two URLs for the same state invite drift and client confusion

## Considered Options

1. `GET /api/config`: whole parsed config as one document
2. `/configs` collection mirroring YAML instance sections
3. No config resource: `/instances` is the authoritative view of parsed configuration

## Decision Outcome

Chosen option: "no config resource", because every actual consumer wants instances, not a config
blob, and a separate config resource would duplicate `/instances` state under a second URL.

- `GET /api/v1/instances` lists configured instances (name, service type, summary)
- `GET /api/v1/instances/{name}` returns the full parsed, validated configuration for one instance
- Both are read-only in MVP; config flows from version control, through ConfigMaps, into the server

Rules that make this hold:

- **DTOs are domain-shaped, not YAML-shaped.** YAML is an implementation detail. Deprecated aliases,
  compat shims, and file layout stay behind the parse boundary; the DTO reflects the domain model.
  This keeps the future storage swap (ConfigMap to database) invisible to clients.
- **Secrets are absent, not masked.** `api_key` and similar fields do not exist in the DTO. Absent
  fields never need a breaking change when storage or masking policy evolves.
- **Config changes require a server restart in MVP.** No file watching; a gitops deploy rolls the
  pod (Reloader-style annotations work today). "Read-only" means read-only through the API, mutable
  underneath it via redeploy.
- **Mutation comes later on the same URLs.** When storage moves server-side, `PUT`/`PATCH` on
  `/instances/{name}` are additive. The YAML import path for that migration is explicitly out of
  scope here.
- Config-file and template management endpoints (backing `config list` and `config create`) are
  deferred until those CLI commands migrate; they are file/document resources, not config state.

### Consequences

- Good, because one URL owns instance configuration and clients never choose between two views
- Good, because secrets cannot leak through the API regardless of what lands in a gitops repo
- Good, because the Model A (user-owned YAML) to Model B (server-owned storage) transition is
  additive rather than breaking
- Bad, because there is no API view of non-instance config (settings.yml); one must be designed if a
  consumer appears
- Bad, because restart-on-change is coarser than hot reload; acceptable for gitops deploy flows
