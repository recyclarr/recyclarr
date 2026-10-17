# ADR-004: Automatic migrations at startup

- **Scope:** `src/Recyclarr.Core` migration steps, `src/Recyclarr.Server/ServerBootstrapService.cs`

## Context

Migrations move files and directories between Recyclarr versions. A manual `recyclarr migrate`
step confused users: commands failed until they ran it, and migrations must run eventually anyway.
The server owns the data directory, so it is the process that must find the data in its current
layout.

## Decision

The server runs every migration step at startup (`ServerBootstrapService`), before it loads
configuration and before Kestrel binds. A failed migration stops startup with a non-zero exit and
remediation steps. There is no migration command and no optional migration.

Migrations MUST only change files and subdirectories inside the already-resolved paths. They MUST
NOT move or rename the root config or data directories: `IAppPaths` is resolved once when the DI
container is built (`DefaultAppDataSetup`) and would hold stale paths for the rest of the process.
A root relocation needs a user-facing diagnostic outside the migration system instead, as done for
`RECYCLARR_APP_DATA` (`ConfigDirectoryLocator`).

## Rejected Options

- Manual `migrate` command as the only path: adds a step with no benefit and blocks other commands.

## Consequences

- Users never run a migration step.
- Root path changes cannot be migrations.

## History

- 2026-02-20: Run migrations automatically at startup; `migrate` becomes a deprecated no-op.
- 2026-09-26: Migrations run in the server; the CLI no longer migrates and `migrate` is removed.
