# ADR-019: Server-owned configuration validated at startup

- **Scope:** `src/Recyclarr.Server` startup and configuration loading, sync job creation

## Context

Configuration problems can only be fixed where the server runs. Parsing on each sync request hid
them until a client started a sync, and clients received only a generic error. Config paths in a
request also meant different things for an ephemeral server, which shares the caller's filesystem,
and a remote server, which does not.

## Decision

Configuration files are read from the server's config directory and fixed for the life of the
process. The resolved configuration (files plus the provider data their includes read) follows
provider data.

### Configuration lifecycle

- The server validates its configuration files at startup, after resource providers are available
  for template includes. The result is an immutable snapshot of instances.
- Each resource refresh that changes provider data resolves the same files again and publishes a new
  snapshot; one that makes the configuration invalid is reverted (ADR-031).
- Configuration file changes require a restart. There is no hot reload; a gitops deploy rolls the
  pod.
- Clients cannot name configuration files. The `configs` request field and the CLI `--config` option
  are removed in Recyclarr 9. `RECYCLARR_CONFIG_DIR` selects a different config directory for either
  launch model.

### Startup failures and warnings

- Parse failures and invalid, duplicate, or split instances stop startup. The server logs every
  problem, exits with a non-zero status, and writes no READY handshake.
- Deprecations are logged as warnings at startup. No API exposes them, because their only remedy is
  a change on the server.
- A server with no configuration files starts. Guide and template resources remain available, and
  sync job creation returns 409.
- If a git resource provider cannot refresh but data from an earlier run exists, the server logs a
  warning and uses the cached data. Startup fails only when no provider data exists, or when the
  configuration is invalid even against the last data that validated.

### Request-time failures

- A sync request that names unknown instances returns 400 with the unknown names and the available
  names. It creates no job.
- Sonarr and Radarr failures can happen at any time, so only requests detect them. Every endpoint
  that calls a service reports the same typed failure categories used by sync results. Endpoints
  outside sync jobs return them as 502 Problem Details with a `failure` extension.

### Configuration authoring

Configuration is hand-edited YAML on the server. No API client can act on configuration files or
templates, so the API exposes neither:

- No endpoints list, read, or write configuration files or configuration templates.
- `GET /api/v1/instances` and `GET /api/v1/instances/{name}` (ADR-012) have no consumer and are not
  implemented.
- The instances namespace provides only what `delete custom-formats` needs: listing an instance's
  custom formats and deleting one by id.

## Rejected Options

- Per-request loading with typed configuration problems returned to clients: clients cannot fix
  server files, and administrators learn of problems only when a request fails.
- Client config paths passed to ephemeral servers only: a command would mean different things per
  launch model.

## Consequences

- Administrators see configuration problems as soon as the server starts.
- Sync requests never parse configuration.
- Any configuration file change needs a restart, and one invalid instance blocks all instances.
- A provider update that breaks the configuration is logged and reverted; the server keeps running.
- Clients never see deprecations; administrators read the server log.

## History

- 2026-09-26: Load configuration once at startup; remove client-supplied config paths.
- 2026-10-21: Resolve configuration again when provider data changes; files stay fixed.
