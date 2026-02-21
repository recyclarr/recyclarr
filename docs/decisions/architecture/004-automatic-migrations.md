# ADR-004: Automatic migrations at startup

- **Status:** accepted
- **Date:** 2026-02-20

## Context and Problem Statement

Recyclarr has a migration system that handles data/directory changes between versions. Users must
run `recyclarr migrate` manually before other commands work, which adds friction and causes
confusion when required migrations block normal usage. The extra command provides little value since
migrations must eventually run regardless.

## Decision Drivers

- Migrations are prerequisites for normal operation; deferring them to a manual step adds complexity
  without benefit
- Users encounter confusing errors when they forget to run migrations before `sync`
- The migration system is simple (filesystem operations on subdirectories within the config root)

## Considered Options

1. Run migrations automatically at startup before command dispatch
2. Keep the manual `migrate` command as the only migration path

## Decision Outcome

Chosen option: "Run migrations automatically at startup", because it eliminates unnecessary user
friction while maintaining the same safety guarantees (failure still exits non-zero with remediation
steps).

Migrations run in `Program.Main()` after the DI container is built but before Spectre.Console
dispatches to any command. The `migrate` subcommand is retained as a deprecated no-op for backward
compatibility.

### Constraint: Migrations must not affect root path resolution

`IAppPaths` is resolved as a singleton during DI container construction via `DefaultAppDataSetup`.
Migrations depend on `IAppPaths` and run after the container is built. Therefore, migrations must
never relocate or rename the root config or data directories, as `IAppPaths` would hold stale
references for the remainder of the process.

Migrations are constrained to operating on files and subdirectories *within* the already-resolved
paths. If a future version needs to relocate root directories, that must be handled outside the
migration system (e.g., environment variable deprecation with user-facing error, as was done for
`RECYCLARR_APP_DATA`).

### Consequences

- Good, because users never need to remember a separate migration step
- Good, because every command automatically benefits from up-to-date data layout
- Good, because the `Required` vs optional migration distinction is eliminated (all migrations just
  run)
- Bad, because migrations cannot relocate root app paths (documented constraint above)
- Bad, because the `migrate` command becomes dead weight that must be maintained for backward
  compatibility
