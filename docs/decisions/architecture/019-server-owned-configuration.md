# ADR-019: Server-owned configuration validated at startup

- **Status:** accepted
- **Date:** 2026-09-26
- **Supersedes:** parts of ADR-012 and ADR-016 (see below)

## Context and Problem Statement

The server parses and validates configuration again for every sync request, and a request can name
its own config file paths. Configuration problems therefore stay unknown until a client starts a
sync. The client gets only a generic error, because ADR-016 keeps configuration details in the
server log, and the server administrator has nothing to act on until a request fails. Paths in a
request also mean different things for an ephemeral server, which shares the caller's filesystem,
and a remote server, which does not.

## Decision Drivers

- Configuration problems can only be corrected where the server runs
- Administrators need configuration problems when the server starts, not when a client first uses it
- A command must mean the same thing for an ephemeral server and a remote server
- ADR-012 already describes configuration as read once and changed only by restart

## Considered Options

1. Keep per-request loading and return typed configuration problems to clients
2. Pass client config paths through to ephemeral servers only
3. Load configuration once at startup and treat it as fixed for the life of the process

## Decision Outcome

Chosen option: "Load configuration once at startup", because it reports server-owned problems to the
server administrator at the earliest point and gives every client the same behavior.

### Configuration lifecycle

- The server loads all configuration files from its config directory during startup, after resource
  providers are available for template includes. The result is an immutable snapshot of instances.
- Configuration changes require a restart. There is no hot reload.
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
  warning and uses the cached data. Startup fails only when no provider data exists.

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
  implemented. ADR-012's rules apply when a consumer appears.
- The instances namespace provides only what `delete custom-formats` needs: listing an instance's
  custom formats and deleting one by id.

### Superseded rules

- ADR-016: invalid server-owned configuration no longer returns 500 from `POST /api/v1/sync/jobs`,
  because such a server does not start. Job creation validates only the request against the
  snapshot.
- ADR-012: config-file and template management endpoints are no longer deferred; they do not exist.

### Consequences

- Good, because administrators see configuration problems as soon as the server starts
- Good, because sync requests never parse configuration
- Good, because every command behaves the same for ephemeral and remote servers
- Bad, because any configuration change needs a server restart
- Bad, because one invalid instance prevents all valid instances from syncing
- Bad, because clients never see deprecations; administrators must read the server log
