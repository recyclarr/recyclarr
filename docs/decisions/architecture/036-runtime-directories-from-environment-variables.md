# ADR-036: Runtime directories come from environment variables

- **Scope:** `src/Recyclarr.Platform` and `src/Recyclarr.Core/Platform` directory resolution, both
  executables' command lines

## Context

Recyclarr keeps user configuration, throwaway data, and sync state in separate root directories, so
users can back up configuration without resources and logs. The CLI, a persistent server, and the
temporary server the CLI starts for each command must all resolve the same directories. Users
deploying without Docker ask for a command-line flag for the config directory, as other self-hosted
servers offer.

## Decision

Each runtime root directory MUST be selected only by its `RECYCLARR_*_DIR` environment variable,
falling back to a default derived from the config directory or the platform. Neither executable
offers a command-line flag for a directory.

- One way to set each directory. Every launcher (a shell script, a Dockerfile, a systemd unit, a
  Kubernetes manifest) can set environment variables before it starts the process, so a flag adds no
  capability.
- Environment variables reach every Recyclarr process. A child process inherits them, so the
  temporary server the CLI starts resolves the same directories without the CLI forwarding a flag.
- Secrets follow their own rule (ADR-027); this record covers paths only.

## Rejected Options

- A flag per directory alongside the variables: two sources for one value need a precedence rule,
  and each flag must be forwarded to every child process the CLI starts.
- Flags only: every launcher must pass them, and the CLI and its temporary server must agree on them
  for each command.

## Consequences

- A systemd unit sets the directories with `Environment=` lines.
- Adding a root directory means adding an environment variable, not a flag.
- The server rejects a directory passed as an argument (ADR-037).

## History

- 2026-02-05: Directories come from environment variables only; the `--app-data` option is removed.
