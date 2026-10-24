# ADR-032: Network security belongs to the operator

- **Scope:** `src/Recyclarr.Server` listeners, `Dockerfile`, deployment documentation

## Context

A persistent server runs behind many network layouts: a port published on a home LAN, a Cloudflare
or Tailscale tunnel, a Kubernetes Ingress, a service mesh. Each layout already has its own way to
encrypt traffic and to limit who can reach a port. RFC 6750 requires TLS for bearer tokens, but
where TLS terminates depends on the layout: terminating at the network edge and running plain HTTP
inside a trusted network is a common, deliberate design. The server cannot see the layout, so any
transport or reachability rule it enforces would be a guess.

## Decision

The server MUST NOT own transport encryption or network reachability. It serves plain HTTP and
listens where it is told; operators supply TLS and decide which clients can reach each port.

- The application's own security boundary is the API key on every `/api` request (ADR-027) and the
  separation of anonymous endpoints onto the management port (ADR-029).
- The server offers no TLS configuration. Operators terminate TLS at a boundary they own (reverse
  proxy, tunnel, Ingress, mesh).
- The management port binds the same host as the API endpoint; there is no separate bind address for
  it. Kubernetes probes and Prometheus reach the pod IP, so the management port must listen on all
  interfaces there anyway, and Docker exposes only published ports. Operators restrict it with port
  publishing or a Kubernetes `NetworkPolicy`.
- Documentation states what the server does not do: the key travels in plain HTTP to the server, and
  the management port is anonymous. Guidance shows how to add TLS and restrict ports; it does not
  prescribe one topology.

## Rejected Options

- Built-in TLS configuration: certificate issuance and renewal duplicate what proxies, tunnels, and
  meshes already do, and a server-side certificate is wasted when TLS terminates upstream.
- A separate bind address for the management port: binding it to loopback breaks Kubernetes probes,
  and in Docker exposure follows published ports, not the bind address. It adds a setting that
  changes nothing in either layout.
- Warning when a client or server uses plain HTTP off loopback: the server cannot tell an edge-TLS
  layout from an unprotected one, so the warning would fire on correct deployments.

## Consequences

- Deployment documentation must cover TLS termination and port restriction for Docker Compose and
  Kubernetes, including a `NetworkPolicy` example for the management port.
- A deployment that publishes the API port on an untrusted network without TLS exposes the key; the
  documentation, not the server, prevents this.

## History

- 2026-10-24: The operator owns transport encryption and network reachability.
