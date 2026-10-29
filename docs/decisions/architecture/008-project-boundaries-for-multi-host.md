# ADR-008: Core holds all sync logic; the server is a thin host

- **Scope:** `src/Recyclarr.Core`, `src/Recyclarr.Server`

## Context

Recyclarr runs as a long-lived HTTP server (Kubernetes, Docker, `recyclarr serve`) and as an
ephemeral server behind the CLI. Sync orchestration first lived in the CLI and was coupled to
`Spectre.Console`, so no other host could use it. `Recyclarr.Core` already existed as a library.

## Decision

`Recyclarr.Core` holds all domain logic, sync orchestration (`Pipelines/`, `Sync/`), configuration,
guide integration, and Sonarr/Radarr gateways. `Recyclarr.Server` is a thin host: endpoints,
Kestrel, persistence, scheduling, and its composition root.

- Core MUST NOT reference a host-specific package (no `Spectre.Console`, ASP.NET Core, or Kestrel).
  The missing package reference makes the compiler enforce this.
- Core provides Autofac modules (`CoreAutofacModule`, `PipelineAutofacModule`); each host registers
  them and adds its own host module.
- MUST NOT add an application-layer project between hosts and Core.

ADR-020 owns the CLI's dependency boundary.

## Rejected Options

- Separate application-layer project (`Recyclarr.App`) between hosts and Core: an extra layer with
  the same result.
- Sync logic inside each executable: duplicates it across hosts.

## Consequences

- Core is by far the largest project.
- A new host reuses all sync behavior by registering Core's modules.

## History

- 2026-05-11: Move sync orchestration into Core so a server host can share it with the CLI.
- 2026-09-27: The CLI became an HTTP client of the server; Core is now used only by the server.
