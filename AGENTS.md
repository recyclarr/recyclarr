# Recyclarr Development Guide

.NET CLI tool for synchronizing TRaSH Guides to Sonarr/Radarr.

## Project Context

- Uses SLNX format (`Recyclarr.slnx`) instead of traditional SLN files.
- Components: Cli (entry) -> Core (logic) -> TrashGuide/ServarrApi (integrations)
- Pipeline: `GenericSyncPipeline<TContext>` - Config -> Fetch -> Transaction -> Persist -> Preview
- DI: Autofac via `CompositionRoot`, `CoreAutofacModule`, `PipelineAutofacModule`. Every library
  gets its own Autofac Module to keep DI registration modular.
- Config: YAML + `schemas/config-schema.json` validation
- Testing: NUnit 4 + NSubstitute + AutoFixture + parallel execution
- Dotnet tools in `.config/dotnet-tools.json`
- CLI: `Spectre.Console` package for CLI framework

## Coding Standards & Development Requirements

- You MUST use dependency injection for all dependencies; NEVER manually 'new' objects in production
  code.
- Search existing code first: `rg "pattern"` before writing new code. Holistically and
  comprehensively make changes, don't just do it in isolation which ignores other important areas of
  code that might be in-scope or indirectly affected by a change.
- Reuse/extend existing implementations - zero duplication tolerance
- CRITICAL: Follow SOLID, DRY, YAGNI principles
- .NET 10.0 + nullable reference types
- Comment guidelines (implements global "comments must earn their place"). Examples:
  - LINQ chains (3+ operations): Brief comment stating transformation goal
  - Conditional blocks with non-obvious purpose: One-line comment (e.g., `// Explicit: user
    specified`)
  - Private methods: Block comment if name + parameters don't make purpose self-evident
  - Early returns/continues: Include reason if not obvious from context
  - Complex algorithms: Comment explaining approach at top, not line-by-line
  - General: Any code where a reader would pause and wonder "why?" or "what's happening here?"
  - NEVER: XML doc comments, commented-out code, restating what code literally does
- Zero warnings/analysis issues
- Prefer polymorphism over enums when modeling behavior or extensibility. Propose enum vs
  polymorphism tradeoffs for discussion rather than defaulting to enums.
- When registering types as themselves in Autofac, `RegisterType<>()` already registers "as self",
  so don't use `.AsSelf()`; it is redundant.

## Skills

Load skills for procedural knowledge:

- `csharp-coding` - C# 14/.NET 10 patterns and idioms
- `testing` - Integration-first TDD workflow
- `changelog` - CHANGELOG format and conventions
- `decisions` - Creating ADRs and PDRs in `docs/decisions/`

## Backward Compatibility

- **CODE**: No backward compatibility required - refactor freely
- **USER DATA**: Mandatory backward compatibility - User-observable things like YAML configs and
  settings files must remain functional.
- DEPRECATIONS: Removed or deprecated features need helpful user-facing diagnostics. Look at
  existing patterns in the code base for this BEFORE you make a modification. Follow these existing
  patterns.

## Sync Philosophy

All sync operations must be deterministic and atomic.

- **Errors** indicate configuration problems that would cause non-deterministic or partial sync.
  Errors are collected during validation and skip only the relevant pipeline, not the entire sync.
  Recyclarr preserves previous (service-side) sync state to avoid unintentional behavior from
  partial syncs.
- **Warnings** indicate deprecations or non-critical informational messages that don't affect sync
  determinism.
- Use `ISyncEventPublisher.AddError()` for configuration validation failures
- Use `ISyncEventPublisher.AddWarning()` only for deprecations and informational messages

## Console and Logging Output

The `--log` flag controls which output channel is visible to users:

- `--log` **omitted**: IAnsiConsole -> console (visible); ILogger -> file only
- `--log [level]`: IAnsiConsole -> void (hidden); ILogger -> file + console

Output channel usage:

- `IAnsiConsole`: User-facing output (progress, results, prompts). Visible by default.
- `ILogger`: Diagnostic information. Always written to log files; visible on console only with
  --log.
- NEVER use `Console.WriteLine`

### Dual Output Pattern

User-visible information must go to both console and log:

- Sync command: Use `ISyncEventPublisher` methods which handle both channels automatically via the
  diagnostics system (`SyncEventStorage`, `DiagnosticsRenderer`).
- Other commands: Must output to both channels manually:

```csharp
// User-visible information
console.WriteLine(message);
log.Information(message);

// Deprecations
console.MarkupLine("[darkorange bold][[DEPRECATED]][/] " + message);
log.Warning(message);
```

## Repository Structure

- `src/`: All C# source code
- `tests/`: All C# unit and integration tests
- `ci/`: Scripts and utilities for GitHub workflows
- `.github/`: GitHub actions and workflows
- `docs/`: Documentation
  - `architecture/`: Current system design (what is)
  - `decisions/`: MADR-based decision records
    - `architecture/`: Technical implementation decisions (ADRs)
    - `product/`: Strategic and upstream-driven decisions (PDRs)
  - `reference/`: External reference materials (Discord summaries, upstream docs)
  - `memory-bank/`: AI working memory

Some key files and directories:

- Primary CLI project is `src/Recyclarr.Cli/`
- `src/Recyclarr.Cli/CompositionRoot.cs` - DI setup
- `src/Recyclarr.Core/CoreAutofacModule.cs` - Service registration
- `Directory.Packages.props` - Package versions (Nuget central package management enabled)
- `schemas/**.json` - Schemas for different Recyclarr YAML files

## Tooling Requirements

- CSharpier is the ONLY formatting tool. Never use `dotnet format` or other formatters.
- MUST run `pre-commit run <file1> <file2> ...` for all changes
- Use `dotnet test` at solution level to verify all tests pass
- You MUST use the dotnet CLI when: adding packages, removing packages, adding projects to solution.
  Prioritize the CLI for all project-specific modifications if possible. Central package management
  is enabled via `Directory.Packages.props`.
- Avoid `--no-build` or `--no-restore` flags. Rely on simple invocations: `dotnet test` will always
  restore + build, so there's no need to do `dotnet build` followed by `dotnet test`.
- Use minimal verbosity for build/test commands to show only warnings and errors: `dotnet build -v m
  --no-incremental` and `dotnet test -v m --no-incremental`. Informational logs consume valuable
  context. When verbose output is needed for debugging, pipe to a log file (`dotnet test -v d 2>&1 >
  /tmp/test.log`) (do NOT use `tee`) and read from it with targeted searches (`rg "pattern"
  /tmp/test.log`).

## Scripts

All scripts are under `scripts/`:

**Development and Testing:**

- `test_coverage.py`: Run tests with code coverage. Outputs JSON coverage file paths.
  - Usage: `./scripts/test_coverage.py`
  - CRITICAL: Must succeed before running `query_coverage.py`. Investigate failures before
    proceeding - coverage data is invalid on failure.
- `query_coverage.py`: Query coverage results (AI-optimized output).
  - `./scripts/query_coverage.py files <pattern>... [-f N] [-l N]` - Coverage % for matching files
  - `./scripts/query_coverage.py uncovered <pattern>...` - Same but includes uncovered line numbers
  - `./scripts/query_coverage.py lowest [N]` - N files with lowest coverage (default: 10)
  - Output format: `path:pct:covered/total[:uncovered_lines]`
  - If this script lacks needed functionality, extend it rather than using raw jq/grep. This script
    must remain the single source for coverage analysis.
- `Docker-Debug.ps1`: Start external service dependencies (Sonarr, Radarr, Apprise) via docker
  compose. Use when debugging locally or testing integration scenarios.
  - Usage: `./scripts/Docker-Debug.ps1`
- `Docker-Recyclarr.ps1`: Run Recyclarr in a container (equivalent to `docker compose run`). Rarely
  used; auto-starts `Docker-Debug.ps1` if needed.
  - Usage: `./scripts/Docker-Recyclarr.ps1 sync`
- `query_issues.py`: Query Qodana issues from GitHub code scanning API.
  - Flags: `-p <path>`, `-r <rule>`, `-s <severity>` (default: warning), `-b <branch>`
  - Output: `path:line:severity:rule:message`
- `Run-E2ETests.ps1`: Run E2E tests (`Docker-Debug.ps1` required)

**ONLY for human use (AI must never run these):**

- `Prepare-Release.ps1`: Initiates a release of Recyclarr.
- `Update-Gitignore.ps1`: Updates the global `.gitignore`.
- `Install-Tooling.ps1`: Install or update local tools.
- `Commit-Gitignore.ps1`: Commit git ignore changes.

## Commits and Changelog

Use this priority order (highest to lowest) to determine commit type:

### Tier 1: User-Facing (require CHANGELOG)

Ask: "Would this warrant a CHANGELOG line that non-technical users would understand and care about?"

- `feat:` / `feat!:` -> CHANGELOG "Added" / "Removed/Changed" (new capability / breaking)
- `fix:` / `fix!:` -> CHANGELOG "Fixed" / "Removed/Changed" (bug users would report / breaking)
- `perf:` -> CHANGELOG "Changed" (significant performance improvement)

Multi-commit features: use `refactor` for infrastructure commits, `feat` only for the commit that
enables the user-facing capability.

### Tier 2: Path-Based (no CHANGELOG)

If Tier 1 doesn't apply, match file paths:

- `test:` -> `tests/**`
- `ci:` -> `.github/**`, `ci/**`
- `build:` -> `*.props`, `*.csproj`, `*.slnx`, `.config/dotnet-tools.json`
- `docs:` -> `docs/**`, `*.md` (including `CHANGELOG.md` if committed alone)

### Tier 3: Catch-All (no CHANGELOG)

If neither Tier 1 nor Tier 2 applies:

- `refactor:` -> C# changes in `src/**` (internal restructuring, code reorganization)
- `chore:` -> All other non-C# changes (`scripts/**`, `.renovate/*`, `renovate.json5`,
  `.editorconfig`, `.vscode/*`, `schemas/*`, linter configs, etc.)

### Scopes

Derive scope from primary file path:

- `src/*/Pipelines/*` -> `(sync)`
- `src/*/Config/*` -> `(config)`
- `src/*/Console/Commands/*` -> `(cli)`
- `src/*/Cache/*` -> `(cache)`
- `schemas/*` -> `(yaml)`

### CHANGELOG Format

Load the `changelog` skill for detailed CHANGELOG format and conventions.

**IMPORTANT**: When planning user-facing changes (`feat`, `fix`, `perf`), always include
`CHANGELOG.md` in scope. Verify changelog updates are part of the implementation plan before
starting work.

## Memory Bank

- Working memory is in `docs/memory-bank`. This is for AI use only to track persistent memory
  between working sessions. Use `ls docs/memory-bank` at the start of a session or task to see
  what's available.
- You will freely read from, update/modify, create, and delete these memories as you see fit. These
  memory bank files are entirely AI-managed.
- Memory bank files should be small, single-topic units. Avoid monolithic memory bank files.
  Instead, focus on decomposing memories into reusable chunks (multiple files). These are easier to
  organize and load in isolation based on information AI needs for a given task.
