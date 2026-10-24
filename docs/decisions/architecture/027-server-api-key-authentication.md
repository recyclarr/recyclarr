# ADR-027: Server API key authentication

- **Scope:** `src/Recyclarr.Server` authentication, CLI server connection

## Context

The server can sync and delete resources in every configured Sonarr and Radarr instance. A
persistent server often listens on a LAN or in a Kubernetes cluster, and an ephemeral server listens
on a loopback port other local users can reach. Clients are machines (the CLI, schedulers, scripts).
Secret tooling delivers secrets either as environment variables (External Secrets Operator) or as
mounted files (Docker Compose secrets, Kubernetes Secret volumes, which update in place on
rotation). One secret must serve both the server and a CLI in the same container. The ephemeral
server must need no user configuration.

## Decision

Every `/api` request carries a static key in `Authorization: Bearer`; the key comes from the
environment, either directly or through a file the environment names.

- Clients send `Authorization: Bearer <key>` (RFC 6750). The key is opaque, not a JWT. The server
  compares it in constant time.
- The key comes from `RECYCLARR_API_KEY`, or from the file named by `RECYCLARR_API_KEY_FILE`. The
  two are exclusive: setting both is an error. Empty values count as unset. Trailing line breaks in
  the file are ignored, since editors and `kubectl create secret --from-file` add them.
- `Recyclarr.Platform` resolves the key for both executables (ADR-020), so the server and the CLI
  apply the same rules.
- The key MUST be at least 32 characters, in every environment. Tests and local development use long
  fixed keys; deterministic does not mean short.
- If neither variable is set, both are, the key is shorter than 32 characters, or the key file
  cannot be read, the server logs a fatal error, exits with a non-zero status, and writes no READY
  handshake (ADR-019). Both server modes follow this rule.
- The server reads the file on every request, so a rotated file applies without a restart. If the
  file later becomes unreadable or invalid, requests are rejected and the cause is logged.
- Everything under `/api` requires the key; everything else on the API port (the OpenAPI document,
  the API reference UI) is anonymous. No endpoint inside `/api` opts out. Management endpoints are
  anonymous and live on a separate port (ADR-029).
- A rejected request gets 401 with `WWW-Authenticate: Bearer` and a Problem Details body.
- The CLI resolves the environment the same way, once per command, and falls back to
  `server.api_key` in `cli.yml` only when neither variable is set (ADR-021). Because the server
  reads the same variables, a container that sets one serves both the server and `docker exec` CLI
  commands.
- For an ephemeral server, the CLI generates a random 256-bit key for each command, passes it in the
  child's environment as `RECYCLARR_API_KEY` with `RECYCLARR_API_KEY_FILE` removed, and sends it on
  every request. The key exists only in memory.

## Rejected Options

- `X-Api-Key` is a convention, not a standard. RFC 6648 deprecates new `X-` headers, and proxies,
  log scrubbers, and HTTP clients do not treat it as a credential the way they treat
  `Authorization`.
- A key in `settings.yml` gives the server and a CLI in the same container two different sources. A
  user who sets only the file gets 401 from every `docker exec` command, because the CLI never reads
  `settings.yml` (ADR-021).
- A file key that silently wins over the variable: a leftover variable would hide which key is in
  force. Exclusivity reports the conflict instead.
- Looser key rules in a development mode (for example `ASPNETCORE_ENVIRONMENT=Development`): a user
  who copies that setting into production gets the weak rules, and fixed test keys need no
  exception.
- A generated key needs a writable location, a way to show it to the user, and rotation rules. It
  also hides a missing configuration instead of reporting it.
- OAuth2 adds an identity provider and token lifecycle for clients that are all machines on a local
  network.

## Consequences

- A misconfigured server fails at startup instead of serving its API to anyone.
- One environment variable configures Docker, Compose, Kubernetes, and `serve`; Compose secrets and
  Kubernetes Secret volumes work through the file variable.
- Every persistent deployment must set `RECYCLARR_API_KEY` or `RECYCLARR_API_KEY_FILE`.
- Rotating a file key needs no restart; rotating a variable key does.
- All clients share one key; there are no per-client keys or scopes.

## History

- 2026-10-12: Require a bearer key from `RECYCLARR_API_KEY` on every `/api` request.
- 2026-10-17: Anonymous probes move to the management port (ADR-029).
- 2026-10-24: Accept the key from `RECYCLARR_API_KEY_FILE`; require at least 32 characters.
