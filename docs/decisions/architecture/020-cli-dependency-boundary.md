# ADR-020: CLI depends only on the API client and platform primitives

- **Status:** accepted
- **Date:** 2026-09-27

## Context and Problem Statement

ADR-008 planned for the CLI to drop its `Recyclarr.Core` reference once it became an HTTP client of
the server. After that change the CLI still used Core for a few process-level concerns: the
environment abstraction, the configuration directory lookup, and exit codes. The CLI and the server
must resolve the same configuration directory, because both read files from it on one machine.

## Decision Drivers

- The CLI must never link the server's domain library
- Both executables must resolve the configuration directory with the same rules
- A general "shared" project tends to collect unrelated code and pass its dependencies to the CLI

## Considered Options

1. Copy the needed pieces into the CLI
2. A library named for its content, `Recyclarr.Platform`, referenced by the CLI and Core
3. A general shared library for anything both executables use

## Decision Outcome

Chosen option: "`Recyclarr.Platform`", because it gives the configuration directory lookup one
source without a catch-all project.

```txt
Recyclarr.Cli    -> Recyclarr.Client, Recyclarr.Platform
Recyclarr.Server -> Recyclarr.Core -> Recyclarr.Platform
```

### Content rule

`Recyclarr.Platform` holds only code that describes the process or its environment: the environment
abstraction, the configuration directory lookup, and exit codes. Code that does not describe the
platform or process does not go in. Log templates stay in each executable, because the CLI and the
server format console output differently. The server's data layout (`AppPaths`) stays in Core.

### Consequences

- Good, because the CLI build cannot reach Core types
- Good, because both executables resolve the configuration directory with one implementation
- Good, because the project name states what belongs in it
- Bad, because there is one more project to maintain
