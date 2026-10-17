# ADR-027: Server API key authentication

- **Scope:** `src/Recyclarr.Server` authentication, CLI server connection

## Context

The server can sync and delete resources in every configured Sonarr and Radarr instance. A
persistent server often listens on a LAN or in a Kubernetes cluster, and an ephemeral server listens
on a loopback port other local users can reach. Clients are machines (the CLI, schedulers, scripts).
Kubernetes secret tooling such as External Secrets Operator delivers secrets as environment
variables, and one secret must serve both the server and a CLI in the same container. The ephemeral
server must need no user configuration.

## Decision

Every `/api` request carries a static key in `Authorization: Bearer`; the server reads the key only
from `RECYCLARR_API_KEY`.

- Clients send `Authorization: Bearer <key>` (RFC 6750). The key is opaque, not a JWT. The server
  compares it in constant time.
- The server reads the key only from `RECYCLARR_API_KEY`. If the variable is unset or empty, the
  server logs a fatal error, exits with a non-zero status, and writes no READY handshake (ADR-019).
  Both server modes follow this rule.
- The server does not limit key length or content. A strong key is the operator's responsibility,
  and short keys keep tests and local development simple.
- Everything under `/api` requires the key; everything else on the API port (the OpenAPI document,
  the API reference UI) is anonymous. No endpoint inside `/api` opts out. Management endpoints are
  anonymous and live on a separate port (ADR-029).
- A rejected request gets 401 with `WWW-Authenticate: Bearer` and a Problem Details body.
- The CLI reads the key from `server.api_key` in `cli.yml`; a non-empty `RECYCLARR_API_KEY`
  overrides it (ADR-021). Because the server reads the same variable, a container that sets it once
  serves both the server and `docker exec` CLI commands.
- For an ephemeral server, the CLI generates a random 256-bit key for each command, passes it in the
  child's environment, and sends it on every request. The key exists only in memory.

## Rejected Options

- `X-Api-Key` is a convention, not a standard. RFC 6648 deprecates new `X-` headers, and proxies,
  log scrubbers, and HTTP clients do not treat it as a credential the way they treat
  `Authorization`.
- A key in `settings.yml` gives the server and a CLI in the same container two different sources. A
  user who sets only the file gets 401 from every `docker exec` command, because the CLI never reads
  `settings.yml` (ADR-021).
- A generated key needs a writable location, a way to show it to the user, and rotation rules. It
  also hides a missing configuration instead of reporting it.
- OAuth2 adds an identity provider and token lifecycle for clients that are all machines on a local
  network.

## Consequences

- A misconfigured server fails at startup instead of serving its API to anyone.
- One environment variable configures Docker, Compose, Kubernetes, and `serve`.
- Every persistent deployment must set `RECYCLARR_API_KEY`.
- All clients share one key; there are no per-client keys or scopes.

## History

- 2026-10-12: Require a bearer key from `RECYCLARR_API_KEY` on every `/api` request.
- 2026-10-17: Anonymous probes move to the management port (ADR-029).
