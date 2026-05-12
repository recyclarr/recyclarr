# ADR-008: Project boundaries for multi-host deployment

- **Status:** Accepted
- **Date:** 2026-05-11

## Context and Problem Statement

Recyclarr is a CLI-only application today. The primary motivation for adding an HTTP server is
Kubernetes: running Recyclarr as a long-lived process with a sync scheduler and API surface is a
better fit for container orchestration than cron-triggered CLI invocations. This requires a second
executable (the server) that shares all sync logic with the CLI.

The current project structure doesn't support this. `Recyclarr.Core` exists as a shared library but
has no independent consumers beyond `Recyclarr.Cli` -- it's an artifact of past organizational
attempts rather than a deliberate deployment boundary. Meanwhile, the sync orchestration (Pipelines,
Processors) lives in the CLI project despite not being CLI-specific. Some of that orchestration code
is coupled to `Spectre.Console`, which prevents reuse from a non-CLI host.

## Decision Drivers

- Two executables (CLI and server) need to share sync logic without duplication
- Sync orchestration (Pipelines, Processors) currently lives in the CLI project but isn't
  CLI-specific
- Processors are coupled to `Spectre.Console` for output rendering, which a server host can't use
- `Recyclarr.Core` already exists as a library project with no independent deployment purpose;
  introducing a third "application layer" project adds structure without adding value
- The CLI will eventually become a pure HTTP client, severing its Core dependency entirely
- Minimize project churn across the full evolution (CLI-direct → CLI+Server → CLI-as-HTTP-client)

## Considered Options

1. Three-layer: new `Recyclarr.App` project between adapters and Core
2. Two-layer: absorb orchestration into the existing `Recyclarr.Core` project
3. Monorepo merge: collapse everything into each executable project

## Decision Outcome

Chosen option: "Two-layer", because Core already exists as a library and now has a genuine reason to
exist -- two hosts need it. Adding a third project between adapters and Core creates an extra layer
of indirection for the same practical result.

### Target structure

```txt
Recyclarr.Cli (exe -- thin adapter)
├── Commands/        parse args, call port, render to console
└── refs → Recyclarr.Core

Recyclarr.Server (exe -- thin adapter, future)
├── Endpoints/       HTTP routes, call ports, return JSON
└── refs → Recyclarr.Core

Recyclarr.Core (lib -- everything shared)
├── Ports/           port interfaces (ISyncPort, IListPort, etc.)
├── Pipelines/       sync engine (moved from Cli)
├── Processors/      use-case orchestration (moved from Cli, implements ports)
├── Config/          YAML parsing, validation (already here)
├── Servarr/         domain logic (already here)
├── ServarrApi/      API adapters and mappers (already here)
├── TrashGuide/      guides integration (already here)
└── refs → Api.Sonarr, Api.Radarr
```

### Adapter constraint

Core must not reference any adapter-specific package. No `Spectre.Console`, no Kestrel, no
transport-specific dependency. The build enforces this: if Core doesn't have the package reference,
the compiler rejects any usage. Existing `Spectre.Console` coupling in Processors must be broken
before or during the move to Core.

### DI composition

Each host has its own composition root. Core provides shared Autofac modules (`CoreAutofacModule`,
`PipelineAutofacModule`, etc.) that both hosts register. Each host adds its own adapter module for
host-specific bindings (Spectre.Console registrations in CLI, Kestrel/endpoint registrations in
Server).

### Migration approach (strangler fig)

No big-bang rewrite. Processors and Pipelines move to Core one command at a time:

1. Create the project structure (empty Core folders, references wired up)
2. Pick one Processor (starting with sync, the primary use case)
3. Decouple it from Spectre.Console and move it to Core in the same PR
4. Define a port interface for it; thin out the CLI command to call through the port
5. Repeat for remaining commands

Each step is a self-contained change. The application works at every intermediate state.

### Evolution to final state

```txt
Phase 1 (current work):
  Cli (exe) → Core (lib)

Phase 2 (server introduction):
  Cli (exe) → Core (lib)
  Server (exe) → Core (lib)

Phase 3 (CLI becomes HTTP client):
  Cli (exe) → HTTP → Server (exe) → Core (lib)
```

In Phase 3, Cli drops its Core reference entirely. It becomes a standalone HTTP client, consuming
the Server's OpenAPI spec through code generation (the same Refit/Refitter pattern already used for
Sonarr and Radarr APIs). At that point, Core becomes the Server's private dependency.

### Consequences

- Good, because two projects (not three) keeps the dependency graph simple
- Good, because Core's existence is now justified by a real deployment concern (two consumers)
- Good, because the adapter constraint is compiler-enforced, not convention-based
- Good, because strangler fig means no big-bang migration and the app works at every step
- Bad, because Core becomes the largest project by far (domain + infrastructure + orchestration)
- Bad, because moving Pipelines/Processors requires decoupling Spectre.Console first, which adds
  scope to each migration PR
