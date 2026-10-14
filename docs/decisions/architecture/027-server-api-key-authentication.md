# ADR-027: Server API key authentication

- **Status:** accepted
- **Date:** 2026-10-12

## Context and Problem Statement

The HTTP server can sync and delete resources in every configured Sonarr and Radarr instance. Before
this decision, any process that could reach the server could call its API. A persistent server often
listens on a LAN or in a Kubernetes cluster, and even an ephemeral server listens on a loopback port
that other local users can reach. The server needs one authentication model that works in both
modes and that Docker, Docker Compose, and Kubernetes deployments can supply without extra tooling.

## Decision Drivers

- Clients are machines (the CLI, schedulers, scripts); there are no interactive users to log in
- One secret must serve the server and a CLI that runs in the same container
- Kubernetes secret tooling such as External Secrets Operator delivers secrets as environment
  variables
- A server that starts without protection must not be possible by accident
- The ephemeral server must stay invisible to users: no key to create or configure

## Considered Options

1. Static key in `Authorization: Bearer`, read by the server only from `RECYCLARR_API_KEY`
2. Static key in an `X-Api-Key` header, as Sonarr and Radarr use
3. Static key that the server also reads from `settings.yml`
4. Key that the server generates and writes to disk when none is configured
5. OAuth2 client credentials

## Decision Outcome

Chosen option: "Static key in `Authorization: Bearer`, read only from `RECYCLARR_API_KEY`", because
it is the standard header for credentials and one environment variable serves every deployment.

### Rules

- Clients send `Authorization: Bearer <key>` (RFC 6750). The key is opaque, not a JWT. The server
  compares it in constant time.
- The server reads the key only from `RECYCLARR_API_KEY`. If the variable is unset or empty, the
  server logs a fatal error, exits with a non-zero status, and writes no READY handshake (ADR-019).
  Both server modes follow this rule.
- The server does not limit key length or content. A strong key is the operator's responsibility,
  and short keys keep tests and local development simple.
- Everything under `/api` requires the key; everything outside it (`/health`, the OpenAPI document,
  the API reference UI) is anonymous (ADR-011). No endpoint inside `/api` opts out.
- A rejected request gets 401 with `WWW-Authenticate: Bearer` and a Problem Details body.
- The CLI reads the key from `server.api_key` in `cli.yml`; a non-empty `RECYCLARR_API_KEY`
  overrides it (ADR-021). Because the server reads the same variable, a container that sets it once
  serves both the server and `docker exec` CLI commands.
- For an ephemeral server, the CLI generates a random 256-bit key for each command, passes it in the
  child's environment, and sends it on every request. The key exists only in memory.

### Rejected options

- `X-Api-Key` is a convention, not a standard. RFC 6648 deprecates new `X-` headers, and proxies,
  log scrubbers, and HTTP clients do not treat it as a credential the way they treat
  `Authorization`.
- A key in `settings.yml` gives the server and a CLI in the same container two different sources.
  A user who sets only the file gets 401 from every `docker exec` command, because the CLI never
  reads `settings.yml` (ADR-021).
- A generated key needs a writable location, a way to show it to the user, and rotation rules. It
  also hides a missing configuration instead of reporting it.
- OAuth2 adds an identity provider and token lifecycle for clients that are all machines on a
  local network.

### Consequences

- Good, because a misconfigured server fails at startup instead of serving its API to anyone
- Good, because one environment variable configures Docker, Compose, Kubernetes, and `serve`
- Good, because ephemeral users configure nothing
- Bad, because every persistent deployment must now set `RECYCLARR_API_KEY`
- Bad, because all clients share one key; there are no per-client keys or scopes
