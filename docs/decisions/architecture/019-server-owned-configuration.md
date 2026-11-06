# ADR-019: Server-owned configuration validated at startup

- **Scope:** `src/Recyclarr.Server` startup and configuration loading, sync job creation

## Context

Configuration problems can only be fixed where the server runs. Parsing on each sync request hid
them until a client started a sync, and clients received only a generic error. Config paths in a
request also meant different things for an ephemeral server, which shares the caller's filesystem,
and a remote server, which does not.

## Decision

Configuration files are read from the server's config directory. The resolved configuration (files
plus the provider data their includes read) follows both the files and provider data, and only a
configuration that validates is ever published.

### Configuration lifecycle

- The server validates its configuration files at startup, after resource providers are available
  for template includes. The result is an immutable snapshot of instances.
- Configuration inputs are every file the configuration reads from outside provider data, for
  example local include files. A path that was read or looked up counts even when the file is
  missing, so creating it is a change.
- A persistent server compares the contents of its configuration inputs on each resource refresh
  tick (ADR-031). Content comparison works on bind mounts and network shares, where file change
  notifications are unreliable. A change resolves the configuration again and publishes it when it
  validates.
- A change that does not validate keeps the published snapshot and reverts any provider update from
  the same tick, so everything stays at its last good state. Its errors are logged once; the server
  resolves again only after the inputs change again.
- Each resource refresh that changes provider data resolves the configuration again; a provider
  update that makes it invalid is reverted (ADR-031).
- `settings.yml` and environment variables require a restart: they select the providers and schedule
  that the running server is built from.
- Clients cannot name configuration files. The `configs` request field and the CLI `--config` option
  are removed in Recyclarr 9. `RECYCLARR_CONFIG_DIR` selects a different config directory for either
  launch model.

### Startup failures and warnings

- Parse failures and invalid, duplicate, or split instances stop startup. The server logs every
  problem, exits with a non-zero status, and writes no READY handshake.
- Deprecations are logged as warnings at startup and on each reload. No API exposes them, because
  their only remedy is a change on the server.
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

Configuration is hand-edited YAML on the server. The server never writes configuration files:

- No endpoints list, read, or write configuration files.
- The server may return generated configuration text, such as starter templates. The client decides
  where to write it; the server's config directory may be read-only, or on another machine.
- `GET /api/v1/instances` and `GET /api/v1/instances/{name}` (ADR-012) have no consumer and are not
  implemented.
- The instances namespace provides only what `delete custom-formats` needs: listing an instance's
  custom formats and deleting one by id.

## Rejected Options

- Per-request loading with typed configuration problems returned to clients: clients cannot fix
  server files, and administrators learn of problems only when a request fails.
- Client config paths passed to ephemeral servers only: a command would mean different things per
  launch model.
- Server writes starter configs into its config directory: fails for read-only mounts, and a remote
  server writes where the user cannot edit the file.
- Restart-only configuration: an edit made without a restart looks applied but is silently ignored,
  unlike scheduled runs that read the configuration fresh.
- Reloading before each scheduled sync: configuration errors surface only when a sync runs.
- File system change notifications: unreliable on bind mounts and network shares, and editors save
  in several steps.

## Consequences

- Administrators see configuration problems as soon as the server starts or an edit is checked.
- Sync requests never parse configuration.
- A configuration edit applies within about one refresh tick when no reader holds provider data; a
  sync in progress keeps the configuration it started with.
- One invalid instance blocks all instances, at startup and on reload.
- A provider update that breaks the configuration is logged and reverted; the server keeps running.
- Clients never see deprecations; administrators read the server log.

## History

- 2026-09-26: Load configuration once at startup; remove client-supplied config paths.
- 2026-10-21: Resolve configuration again when provider data changes; files stay fixed.
- 2026-10-31: Allow returning generated configuration text; the server still never writes files.
- 2026-11-05: Reload configuration when its input files change; `settings.yml` still needs a
  restart.
