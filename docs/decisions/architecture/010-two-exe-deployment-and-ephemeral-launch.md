# ADR-010: Two-exe deployment and ephemeral launch

- **Scope:** `src/Recyclarr.Cli/Server/`, `src/Recyclarr.Server/Program.cs`, lifeline monitoring,
  packaging

## Context

The CLI is an HTTP client of the server. It must work with no server running and against a
persistent server (Kubernetes, Docker). On Linux, macOS, and Windows, a server that the CLI starts
must not outlive it, even when the CLI crashes. ASP.NET Core types must not reach the CLI.

## Decision

The CLI and the server are separate executables. The CLI never links the server; its build-only
project reference (`ReferenceOutputAssembly="false"`) only places the server binary beside it. The
CLI finds the server binary in its own directory by a build-time name.

### Launch models

**Centralized**: a server address is configured (`cli.yml` or `RECYCLARR_SERVER_URL`, ADR-021). The
CLI connects to it and manages no process. The Docker image sets `RECYCLARR_SERVER_URL` to its own
server, so `docker exec` commands never start an ephemeral server.

**Ephemeral (attached)**: no address is configured. For each command:

1. The CLI starts `recyclarr-server --urls=http://127.0.0.1:0 --parent-pid={ownPid}
   --ephemeral=true` with stdin, stdout, and stderr redirected. The literal loopback address is
   required because Kestrel rejects a dynamic port on `localhost`.
2. The server binds, then writes `READY:{port}` to stdout.
3. The CLI reads that line and uses the port for the rest of the command.
4. When the CLI exits or crashes, the lifeline stops the server.

The `READY` line replaces health-check polling. An ephemeral server writes nothing else to stdout
and only error log events to stderr, so a server that exits before `READY` (for example, invalid
configuration) can tell the CLI why. The CLI does not forward server logs; the server's own log
files hold them.

### Lifeline and mode are separate flags

`--parent-pid={pid}` makes the server stop when the process that launched it ends.
`--ephemeral=true` selects ephemeral mode: an in-memory database and no sync schedule. They vary
independently:

| Launcher                         | `--parent-pid` | `--ephemeral` |
| -------------------------------- | -------------- | ------------- |
| API commands, no server address  | yes            | yes           |
| `recyclarr serve`                | yes            | no            |
| Docker, systemd, direct launch   | no             | no            |

`recyclarr serve` is a foreground wrapper around a persistent server; the lifeline stops that server
when the CLI ends any way other than Ctrl+C (SIGTERM, a crash, a `dotnet watch` restart). Without
`--parent-pid` (Docker, systemd), no monitoring runs and the server stays up until SIGTERM, so a
`/dev/null` or non-TTY stdin in a container never causes a false shutdown.

With the flag, the server runs two watchdogs; either one calls
`IHostApplicationLifetime.StopApplication()`:

- **Stdin EOF**: the parent holds the child's stdin pipe open; the OS closes it when the parent
  exits or crashes.
- **Parent PID polling**: every 2 seconds, for cases where the pipe does not close cleanly.

MUST NOT infer the lifeline from `Console.IsInputRedirected`: it gives false positives in Docker
without `-t`, Git Bash on Windows, and CI runners. The explicit flag follows LSP servers
(`--clientProcessId`) and HashiCorp go-plugin.

## Rejected Options

- Server embedded in the CLI as a library: puts ASP.NET Core types or a wrapper abstraction into
  the CLI.
- Detached ephemeral server with an idle timeout, shared by rapid invocations: needs port
  discovery, stale-process detection, idle tracking, and per-platform detach semantics to avoid a
  cold start that has not proven to matter. Revisit if cold start becomes a measured problem; the
  handshake and HTTP interface would not change.

## Consequences

- The two binaries ship together (packages, Docker image, CI artifacts).
- Every ephemeral command pays a server cold start.

## History

- 2026-05-20: Separate executables; the CLI launches an attached ephemeral server with a lifeline.
- 2026-10-08: The Docker image targets its own server over IPv4 through `RECYCLARR_SERVER_URL`.
- 2026-10-11: `recyclarr serve` uses the lifeline; ephemeral servers stop forwarding logs to the
  CLI.
