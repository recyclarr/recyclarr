# AGENTS

.NET CLI tool for synchronizing TRaSH Guides to Sonarr/Radarr.

## Linear

- Issue prefix: `REC-` (e.g. REC-74 is Linear; #74 is GitHub)
- Issue statuses: Backlog, Todo, In Progress, Done, Canceled, Duplicate
- Project statuses: Backlog, Planned, In Progress, Completed, Canceled
- Labels: Bug, Tech Debt, Documentation, Templates, Blocked By Trash Guides

Issue lifecycle:

- MUST transition to "In Progress" when starting work
- MUST transition to "Done" after the final commit lands
- MUST check comments when reading issue details

Project lifecycle:

- MUST transition to "In Progress" when starting work on any issue in the project
- MUST transition to "Completed" when all issues are done
- SHOULD transition to "Canceled" if the project is abandoned

## Agent Architecture

Single primary agent with direct access to all files and tools. Subagents for bounded contexts:

- **trash-guides**: Read-only research against the TRaSH Guides repo (uses haiku for cost). MUST use
  this subagent for any question about TRaSH Guides content (custom formats, quality profiles,
  naming, quality sizes, trash_ids). NEVER use the generic explore agent for guides research.
- **commit**: Git operations after user approval

## Skills

ABSOLUTE REQUIREMENT: Load skills for procedural knowledge on-demand based on domain area:

- `csharp-coding` - C# 14/.NET 10 patterns and idioms
- `testing` - Any test related work (e.g. code coverage, running and editing tests, end to end
  tests)
- `changelog` - When updating CHANGELOG.md for format and conventions
- `decisions` - Creating ADRs and PDRs in `docs/decisions/`
- `linear-planning` - Creating or organizing Linear issues, projects, or initiatives

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

## Code Review Comments

The `// CodeReview:` marker flags questions or concerns for review before commit. A pre-commit hook
prevents accidental commits containing these markers.

Lifecycle:

1. Human adds `// CodeReview: <question>` during implementation
2. Agents MUST stop and present each unresolved CodeReview comment to the user before declaring work
   complete
3. Resolution: either address the concern (refactor, add context) or the user explicitly dismisses
   it
4. Remove the marker only after resolution; NEVER silently delete

When running `pre-commit run` mid-development (before commit), pass `SKIP=no-review-markers` to
suppress the hook: `SKIP=no-review-markers pre-commit run --files <files>`

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
  - XML doc comments (`/// <summary>`): Use for public/internal interfaces, classes, and non-obvious
    members where IntelliSense tooltips add value. Skip for private implementation details.
  - NEVER: Commented-out code, restating what code literally does
- Zero warnings/analysis issues
- Prefer polymorphism over enums when modeling behavior or extensibility. Propose enum vs
  polymorphism tradeoffs for discussion rather than defaulting to enums.
- When registering types as themselves in Autofac, `RegisterType<>()` already registers "as self",
  so don't use `.AsSelf()`; it is redundant.

## Backward Compatibility

- **CODE**: No backward compatibility required - refactor freely
- **USER DATA**: Mandatory backward compatibility - User-observable things like YAML configs and
  settings files must remain functional.
- DEPRECATIONS: Removed or deprecated features need helpful user-facing diagnostics. Look at
  existing patterns in the code base for this BEFORE you make a modification. Follow these existing
  patterns.
- **EXCEPTION**: Backward compatibility *NOT REQUIRED* if modifying unreleased functionality (use
  git logs since latest tag to determine; also check `[Unreleased]` section of `CHANGELOG.md`)

## Sync Philosophy

All sync operations must be deterministic.

**Independent pipelines** (Quality Profiles, Quality Sizes, Media Naming, Media Management):

- Items sync independently; partial sync within pipeline is acceptable
- Invalid items are skipped with errors; valid items proceed

**Dependent pipelines** (Custom Formats):

- All items must sync or the entire pipeline fails
- Failure cascades to skip dependent pipelines (CF failure → QP skipped)
- Rationale: QP scoring requires complete CF data; partial CFs cause silent mis-scoring

**Diagnostics:**

- `AddError()`: Issues that cause items or pipelines to be skipped
- `AddWarning()`: Deprecations and informational messages only

## YAML Error Handling

Two layers translate YamlDotNet failures into user-facing messages. Both produce the same enriched
exception type (`ConfigParsingException`); they differ in where translation happens.

- Can deserialization continue? → **Deprecation system** (`DeprecatedPropertyInspector` via
  `IYamlBehavior`). Skips the property, collects a warning, config loads normally.
- Did deserialization fail? → **YamlBehavior handlers + catch-site fallback**. Handlers inside the
  YamlDotNet pipeline (`INodeDeserializer`) catch structural mismatches where property/node context
  is needed. `ConfigParser` catch block is the final fallback.

Key constraint: YamlDotNet exceptions often lack property names or contain C# type names instead of
YAML names. Handlers that need property context MUST operate inside the pipeline, not post-hoc.

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
- `scripts/`: Convenience scripts
- `docs/`: Documentation
  - `architecture/`: Current system design (what is)
  - `decisions/`: MADR-based decision records
    - `architecture/`: Technical implementation decisions (ADRs)
    - `product/`: Strategic and upstream-driven decisions (PDRs)
  - `reference/`: External reference materials (Discord summaries, upstream docs)

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
  --no-incremental` and `dotnet test -v m`. Informational logs consume valuable context. When
  verbose output is needed for debugging, pipe to a log file (`dotnet test -v d 2>&1 >
  /tmp/test.log`) (do NOT use `tee`) and read from it with targeted searches (`rg "pattern"
  /tmp/test.log`).

**Development and Testing:**

All under `./scripts`. MUST use `testing` skill for test-related work.

- `coverage.py`: Run tests with coverage (`--run`) and query results (`files`, `uncovered`,
  `lowest`)
- `Run-E2ETests.ps1`: **MUST** use this to run E2E tests. NEVER use `dotnet test` for E2E.
- `Docker-Debug.ps1`: Start Docker services (Sonarr, Radarr, Apprise) for local debugging/E2E tests
- `Docker-Recyclarr.ps1`: Run Recyclarr in container; auto-starts Docker-Debug if needed
- `query_issues.py`: Query Qodana issues from GitHub code scanning API
  - Flags: `-p <path>`, `-r <rule>`, `-s <severity>` (default: warning), `-b <branch>`

**ONLY for human use (AI must never run these):**

- `prepare_release.py`: Initiates a release of Recyclarr.
- `Update-Gitignore.ps1`: Updates the global `.gitignore`.
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

MUST load the `changelog` skill for detailed CHANGELOG format and conventions.

**IMPORTANT**: When planning user-facing changes (`feat`, `fix`, `perf`), always include
`CHANGELOG.md` in scope. Verify changelog updates are part of the implementation plan before
starting work.
