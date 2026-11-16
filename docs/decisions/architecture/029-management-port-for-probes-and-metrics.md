# ADR-029: Management port for probes and metrics

- **Scope:** `src/Recyclarr.Server` listeners, health probes, Prometheus metrics, `Dockerfile`

## Context

Health probes and Prometheus scrapes need anonymous HTTP endpoints. Metrics reveal operational data
(instance counts, timings, failures). A persistent server is often reachable through a port forward
or a simple reverse proxy whose operator writes no path rules, so anonymous endpoints on the API
port are public unless every operator blocks them. Kubernetes probes and Prometheus reach the pod IP
directly and need no Service or Ingress route.

## Decision

The persistent server MUST serve probes (`/livez`, `/readyz`) and metrics (`/metrics`) only on a
separate management port, and MUST NOT serve them on the API port.

- One management port serves every management endpoint. Its default (7983) is fixed and independent
  of `server.port`; changing the API port does not move it.
- `server.management.port` in `settings.yml` sets it. The Docker image fixes it with
  `ASPNETCORE_MANAGEMENTPORT`, the same way it fixes the API port with `ASPNETCORE_URLS`; users
  remap ports in Docker or Kubernetes.
- Routing checks the port a connection arrived on (`HttpContext.Connection.LocalPort`), never the
  `Host` header, which clients can forge. Management routes return 404 on the API port; every other
  route returns 404 on the management port.
- The management port binds the same host as the API endpoint; restricting who reaches it is the
  operator's job (ADR-032).
- A management port equal to the API port fails startup.
- The ephemeral server opens no management port; it lives only as long as one CLI command.
- `server.management.metrics` turns Prometheus metrics off without affecting probes.
- API documentation (OpenAPI document, API reference UI) stays on the API port for API consumers.

## Rejected Options

- Anonymous endpoints on the API port behind gateway rules: safety depends on every operator writing
  a deny rule, and a denylist fails open when a rule is missing or a path variant slips through.
- A value that shares the API port: it restores the exposure this decision removes. Keycloak
  deprecated its equivalent option ([management interface][keycloak]).
- An IP allowlist in the app: every network layout (Tailscale ranges, cluster CIDRs) becomes app
  configuration that the network already expresses.
- A port derived from `server.port`: changing one port silently moves the other, and probe manifests
  break.

## Consequences

- Kubernetes Services and Ingresses publish only the API port; probes and scrapers use the pod IP.
- Docker users map 7983 only when an external system needs probes or metrics; the image's own
  `HEALTHCHECK` uses it internally.
- Microsoft documents the same port filtering for container health checks
  ([health checks: filter by port][aspnet]).

## History

- 2026-10-17: Serve probes and metrics only on a separate management port.
- 2026-10-24: The management port binds the API endpoint's host; no separate bind address.

[keycloak]: https://www.keycloak.org/server/management-interface
[aspnet]: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0
