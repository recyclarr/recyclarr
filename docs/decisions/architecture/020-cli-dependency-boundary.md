# ADR-020: CLI depends only on the API client and platform primitives

- **Scope:** project references of `src/Recyclarr.Cli`, `src/Recyclarr.Platform`

## Context

The CLI is an HTTP client of the server, yet it still needs a few process-level pieces that Core
had: the environment abstraction, the configuration directory lookup, and exit codes. Both
executables read the same configuration directory and server API key on one machine, so they must
resolve them with the same rules. A general "shared" project tends to collect unrelated code and
pass its dependencies to the CLI.

## Decision

The CLI MUST NOT reference `Recyclarr.Core`. It links only the generated client and
`Recyclarr.Platform`:

```txt
Recyclarr.Cli    -> Recyclarr.Client, Recyclarr.Platform
Recyclarr.Server -> Recyclarr.Core -> Recyclarr.Platform
```

The CLI's and the client's build-only references to `Recyclarr.Server`
(`ReferenceOutputAssembly="false"`) order the build and place the server binary beside the CLI
(ADR-010); they link nothing.

`Recyclarr.Platform` holds only code that describes the process or its environment: the environment
abstraction (`IEnvironment`), the configuration directory lookup (`ConfigDirectoryLocator`), the
server API key lookup (`ServerApiKeyLocator`, ADR-027), exit codes (`ExitStatus`), and its Autofac
module. Anything else stays out. Log templates are not platform code: the CLI owns its own, and the
server's live in Core (`LogSetup`). The server's data layout (`AppPaths`) stays in Core.

## Rejected Options

- Copy the pieces into the CLI: two implementations of the configuration directory rules.
- A general shared library: becomes a catch-all that leaks dependencies into the CLI.

## Consequences

- The CLI build cannot reach Core types.
- Both executables resolve the configuration directory with one implementation.
- One more project to maintain.

## History

- 2026-09-27: The CLI depends only on the client and `Recyclarr.Platform`.
- 2026-10-24: `Recyclarr.Platform` also resolves the server API key.
