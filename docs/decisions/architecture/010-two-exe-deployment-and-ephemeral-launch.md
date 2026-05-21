# ADR-010: Two-exe deployment and ephemeral launch

- **Status:** Accepted
- **Date:** 2026-05-20

## Context and Problem Statement

Recyclarr needs an HTTP server for Kubernetes deployments and as the transport layer between the CLI
and the sync engine (per ADR-008). ADR-008 established the two-layer project structure (Cli + Core,
Server + Core) and the strangler fig migration, but left the relationship between the CLI and server
binaries ambiguous: it shows "Recyclarr.Server (exe -- thin adapter, future)" without specifying
whether the CLI embeds the server in-process or treats it as a separate process.

The CLI must work in two modes: standalone (no server running) and against a persistent server
(Kubernetes, Docker). The question is how the CLI gets a server when one isn't already running.

## Decision Drivers

- Cross-platform: Linux, macOS, Windows (all .NET 10 targets)
- No ASP.NET Core types should leak into the CLI project
- The CLI eventually becomes a pure HTTP client (Phase 3), severing its Core dependency
- Orphan prevention: if the CLI crashes, the server must not linger
- Simple MVP first; complex models (idle timeout, shared daemon) only if needed later

## Considered Options

1. Embed server in the CLI binary (single exe, server as library)
2. Two separate executables with attached ephemeral launch (stdin pipe lifeline)
3. Two separate executables with detached ephemeral launch (idle timeout daemon)

## Decision Outcome

Chosen option: "Two separate executables with attached ephemeral launch", because it keeps the CLI
and server fully independent at build time while providing a simple, cross-platform mechanism for
the CLI to manage the server's lifecycle when no persistent server exists.

### Two launch models

**Centralized**: User configures a server address in `settings.yml`. CLI commands connect to that
address via the generated HTTP client (REC-151). No process management. This is the Kubernetes /
Docker / long-lived deployment model.

**Ephemeral (attached)**: No server configured. The CLI spawns the server binary as a child process,
uses it for the duration of the command, and the server dies with the CLI. The flow:

1. CLI starts `recyclarr-server --parent-pid={ownPid}` with redirected stdout and stdin
2. Server binds `localhost:0` (OS assigns port), writes `READY:{port}` to stdout
3. CLI reads the handshake line, stores the base address for the generated client
4. CLI makes HTTP calls for the duration of the command
5. CLI exits (or crashes) -- lifeline monitor detects it -- graceful shutdown

The stdout handshake replaces health-check polling for readiness detection. The `READY` line means
the server is listening; no HTTP round-trip needed.

### Lifeline: explicit opt-in via `--parent-pid`

The server only monitors for parent death when launched with `--parent-pid={pid}`. Without the flag
(standalone `recyclarr serve`, Docker, systemd), no monitoring runs and the server stays up until
SIGTERM. This avoids false shutdowns when stdin is `/dev/null` or a non-TTY pipe in container
environments.

When the flag is present, the server runs two concurrent watchdogs (belt-and-suspenders):

- **Stdin EOF**: the parent holds the child's stdin pipe open. When the parent exits (cleanly or via
  crash), the OS closes the pipe and the server reads EOF.
- **Parent PID polling**: the server polls `Process.GetProcessById(parentPid)` every 2 seconds. If
  the parent no longer exists, the server shuts down.

Either watchdog firing triggers `IHostApplicationLifetime.StopApplication()`. The stdin pipe handles
the common case (parent exits normally or crashes); PID polling covers edge cases where the pipe
doesn't close cleanly.

The `--parent-pid` pattern follows LSP servers (`--clientProcessId`) and HashiCorp go-plugin (env
var gate). `Console.IsInputRedirected` was rejected as unreliable: false positives in Docker (no
`-t`), Git Bash on Windows, and CI runners that redirect stdin.

### Why not embedded (option 1)

Embedding the server as a library (`Recyclarr.Server` as a lib referenced by the CLI) creates a
build-time dependency from CLI to Server. The CLI project would need `FrameworkReference
Include="Microsoft.AspNetCore.App"` or a wrapper abstraction to hide ASP.NET Core types. This
dependency gets removed in Phase 3 anyway when the CLI becomes a pure HTTP client. Separate exes
from day one avoids both the temporary coupling and the later decoupling work.

### Why not detached with idle timeout (option 3)

Detached ephemeral (server outlives the CLI, self-terminates after idle timeout) would let multiple
rapid CLI invocations share one server. But it adds complexity: port discovery via temp file or
socket, stale process detection, idle tracking, and platform-varying detach semantics. The benefit
(avoiding cold start on repeated commands) isn't proven to matter for Recyclarr's usage patterns.
Start simple; upgrade to detached if cold start becomes a real problem. The stdout handshake and
HTTP interface don't change, so the migration is additive.

### Project structure

```text
Recyclarr.Cli (exe, Microsoft.NET.Sdk)
    no project ref to Server
    refs -> Recyclarr.Core [today; drops in Phase 3]

Recyclarr.Server (exe, Microsoft.NET.Sdk.Web)
    refs -> Recyclarr.Core
```

The CLI discovers the server binary by convention: same directory as the CLI executable. The server
binary name is a build-time constant in the CLI project.

### Supersedes ADR-008

ADR-008's target structure diagram showed `Recyclarr.Server (exe -- thin adapter, future)` but did
not specify the launch model or the relationship between the two binaries. This ADR fills that gap.
ADR-008's project boundary decisions (two-layer, Core as shared library, adapter constraint) remain
valid and unchanged.

### Consequences

- Good, because CLI and Server are fully independent at build time from day one
- Good, because the lifeline is opt-in, cross-platform, and handles crash recovery
- Good, because Phase 3 extraction requires no structural changes to the server
- Good, because the stdout handshake avoids HTTP polling for readiness
- Bad, because two binaries must be distributed together (packaging, Docker image, CI artifacts)
- Bad, because the attached model means cold start on every CLI invocation (acceptable for MVP;
  detached model is a future option if this matters)
