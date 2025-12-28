# Recyclarr Development Guide

.NET CLI tool for synchronizing TRaSH Guides to Sonarr/Radarr.

## Project Context

- Uses SLNX format (`Recyclarr.slnx`) instead of traditional SLN files.
- Components: Cli (entry) → Core (logic) → TrashGuide/ServarrApi (integrations)
- Pipeline: `GenericSyncPipeline<TContext>` - Config → Fetch → Transaction → Persist → Preview
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

### C# Requirements

Language Features:

- File-scoped namespaces: `namespace Recyclarr.Core;`
- Primary constructors: `class Service(IDep dep, ILogger logger)`
- Collection expressions (MANDATORY): `[]`, `[item]`, `[..spread]`. NEVER use `new[]`, `new
  List<T>()`, `Array.Empty<T>()`. For type inference, prefer `[new T { }, new T { }]` over casts;
  use `T[] x = [...]` only when simpler forms fail.
- Records for DTOs, `init` setters
- Pattern matching: `is not null`, switch expressions
- Spread operator for collections: `[..first, ..second]`

C# 14 Features (.NET 10):

- `field` keyword: `public string Name { get; set => field = value ?? throw; } = "";`
- Extension blocks: `extension(T src) { public bool IsEmpty => !src.Any(); }` (properties + statics)
- Null-conditional assignment: `obj?.Prop = value;` (RHS evaluated only if obj not null)
- Lambda modifiers without types: `(text, out result) => int.TryParse(text, out result)`
- Migration: Use new syntax for new code; opportunistically refactor existing code when revisiting

Required Idioms:

- Use `internal` for implementation classes (CLI apps, service implementations)
- Use `public` only for genuine external APIs
- Concrete classes implementing public interfaces should be `internal`
- Records for data models. Favor immutability where reasonable. Use immutable collections along with
  it (e.g. `IReadOnlyCollection`, `IReadOnlyDictionary`).
- System.Text.Json: Use `JsonSerializerOptions` for convention/style settings applied uniformly.
  Reserve attributes (`[JsonPropertyName]`, etc.) for special cases only. Check for existing options
  configuration before creating new instances.
- `[UsedImplicitly]`: Mark runtime-used members (deserialization, reflection, DI). Cheat sheet:
  - `[UsedImplicitly]` - type instantiated implicitly (DI, empty marker records)
  - `[UsedImplicitly(ImplicitUseKindFlags.Assign)]` - properties set via deserialization
  - `[UsedImplicitly(..., ImplicitUseTargetFlags.WithMembers)]` - applies to type AND all members
  - Common: `[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]` for
    DTOs/records deserialized from JSON/YAML
- Suppressing warnings: NEVER use `#pragma warning disable`. Use `[SuppressMessage]` with
  `Justification` on class/method level. Prefer class-level when multiple members need same
  suppression.
- LINQ method chaining over loops
- LINQ method syntax only; NEVER use query syntax (from/where/select keywords)
- Named arguments for boolean literals and consecutive same-type parameters to clarify intent (e.g.,
  `new Options(SendInfo: false, SendEmpty: true)` not `new Options(false, true)`)
- `ValueTask` for hot paths, `CancellationToken` everywhere (use `ct` for variable name)
- Avoid interface pollution: not every service class must have an interface. Add interfaces when
  justified (e.g. testability, more than one implementation)
- Local functions go after `return`/`continue` statements; add explicit `return;` or `continue;` if
  needed to separate main logic from local function definitions

### Testing Requirements

Test behavior, not implementation. Focus on meaningful business logic coverage.

**Integration-First TDD Workflow:**

1. Write a failing integration test for the happy path (red)
2. Implement until it passes (green)
3. Check coverage; add integration tests for uncovered edge cases
4. Use unit tests only when integration tests cannot reach specific code paths

**What NOT to Test** (low-value coverage):

- Console output, log messages, or UI formatting
- Auto-properties, DTOs, and simple data containers
- Implementation details that could change without affecting behavior

**Mandates:**

- Integration fixtures MUST inherit `IntegrationTestFixture` or `CliIntegrationFixture`
- NEVER make classes/methods `virtual` just for mocking - restructure the test instead
- NEVER remove valid coverage as a solution to test failures
- Hexagonal architecture: stub external dependencies, use real objects for business logic
- Fine-grained unit tests are disposable tools for RCA; keep only those that harden behavior

**Stack:** NUnit 4 + NSubstitute + AutoFixture + AwesomeAssertions (NOT FluentAssertions)

**E2E Tests:** Run via `./scripts/Run-E2ETests.ps1` only (never `dotnet test` directly).

See `tests/CLAUDE.md` for detailed patterns, assertions, and infrastructure.

## Backward Compatibility

- **CODE**: No backward compatibility required - refactor freely
- **USER DATA**: Mandatory backward compatibility - User-observable things like YAML configs and
  settings files must remain functional.
- DEPRECATIONS: Removed or deprecated features need helpful user-facing diagnostics. Look at
  existing patterns in the code base for this BEFORE you make a modification. Follow these existing
  patterns.

## Repository Structure

- `src/`: All C# source code
- `tests/`: All C# unit and integration tests
- `ci/`: contains scripts and other utilities utilized by github workflows
- `.github/`: contains actions and workflows for Github
- `docs`: Documentation for `architecture`, `decisions` (ADRs), `reference` (external info),
  `memory-bank` (working memory for AI use)

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
- `query_coverage.py`: Query coverage results (AI-optimized output).
  - `./scripts/query_coverage.py files <substring> [-f N] [-l N]` - Coverage % for matching files
  - `./scripts/query_coverage.py uncovered <substring>` - Same but includes uncovered line numbers
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

**ONLY for human use (AI must never run these):**

- `Prepare-Release.ps1`: Initiates a release of Recyclarr.
- `Update-Gitignore.ps1`: Updates the global `.gitignore`.
- `Install-Tooling.ps1`: Install or update local tools.
- `Commit-Gitignore.ps1`: Commit git ignore changes.

## Commits and Changelog

### Path-Based Classification (deterministic)

Use these mappings for non-src files:

- `test:` → `tests/**`
- `ci:` → `.github/**`, `ci/**`, `scripts/**`
- `build:` → `*.props`, `*.csproj`, `*.slnx`, `Directory.*`, `.config/dotnet-tools.json`
- `chore:` → `.renovate/*`, `renovate.json5`, `.editorconfig`, `.vscode/*`, `schemas/*`, linter
  configs
- `docs:` → `docs/**`, top-level `*.md` (except `CHANGELOG.md`)

### Source Code Classification (`src/**`)

For `src/**` files, CHANGELOG determines commit type.

**Decision heuristic:** Before choosing `feat` or `fix`, ask: "Would this warrant a line in the
changelog that non-technical users would understand and care about?" If no, use `refactor`. Internal
behavior changes (error handling, orchestration, edge case fixes) that users wouldn't notice or
describe in their own words are `refactor`, not `fix`. Multi-commit features should use `refactor`
for infrastructure commits and `feat` only for the commit that enables the user-facing capability.

- **CHANGELOG required** (user-facing change):
  - `feat:` → CHANGELOG "Added" (new user capability)
  - `fix:` → CHANGELOG "Fixed" (bug users would report)
  - `feat!:` / `fix!:` → CHANGELOG "Removed/Changed" (breaking change)
  - `perf:` → CHANGELOG "Changed" (significant performance improvement)
- **No CHANGELOG** (internal change):
  - `refactor:` → Internal restructuring, new infrastructure for future features, code
    reorganization. If users cannot observe it, use refactor.

### Scopes

Derive scope from primary file path:

- `src/*/Pipelines/*` → `(sync)`
- `src/*/Config/*` → `(config)`
- `src/*/Console/Commands/*` → `(cli)`
- `src/*/Cache/*` → `(cache)`
- `schemas/*` → `(yaml)`

### CHANGELOG Format

File: `CHANGELOG.md` (keepachangelog.com format)

Section order: Added, Changed, Deprecated, Removed, Fixed, Security

Entry format: `- Scope: Description`

```md
### Fixed

- Sync: Crash while processing quality profiles
```

Rules:

- Audience is non-technical end users
- One line per change
- Entries under "Fixed" should not start with "Fixed"
- New entries go under `[Unreleased]` section near the top of the file

Breaking changes format (required for any release with breaking changes):

```md
## [X.0.0] - YYYY-MM-DD

This release contains **BREAKING CHANGES**. See the [vX.0 Upgrade Guide][breakingX] for required
changes you may need to make.

[breakingX]: https://recyclarr.dev/guide/upgrade-guide/vX.0/

### Changed

- **BREAKING**: Description of breaking change
```

## Logging and Console Output

- Diagnostic information uses `ILogger` from serilog (facilitated via DI)
- User-facing messages use `IAnsiConsole` (facilitated via DI)
- NEVER use `Console.WriteLine`
- `ILogger.Debug()`: Diagnostics (requires `-d|--debug`)
- `ILogger.Information()`: User status (use sparingly)
- `ILogger.Warning()`: Non-critical issues (e.g. deprecations)
- `ILogger.Error()`: Critical failures (usually results in application stopping)
- Some user-facing logs still use Serilogs; this is legacy and will eventually be phased out.

## Sync Philosophy

**All sync operations must be deterministic and atomic.**

- **Errors** indicate configuration problems that would cause non-deterministic or partial sync.
  Errors are collected during validation and skip only the relevant pipeline, not the entire sync.
  Recyclarr preserves previous (service-side) sync state to avoid unintentional behavior from
  partial syncs.
- **Warnings** indicate deprecations or non-critical informational messages that don't affect sync
  determinism.
- Use `ISyncEventPublisher.AddError()` for configuration validation failures
- Use `ISyncEventPublisher.AddWarning()` only for deprecations and informational messages

## Recyclarr Runtime Behavior

Application Data Directories (Platform-Specific):

- Windows: `%APPDATA%\recyclarr`
- Linux: `~/.config/recyclarr`
- macOS: `~/Library/Application Support/recyclarr`
- Docker: `/config`

App Data Dir File Structure:

- `recyclarr.yml` - Main config file in app data directory
- `configs/` - Additional YAML files (auto-loaded, non-recursive)
- `includes/` - Include templates directory for reusable YAML snippets
- `settings.yml` - Global settings file (optional)
- `cache/` - Internal cache data
- `logs/` - Application logs
- `repositories/` - Local clones of TRaSH guides and config templates (resource providers)

Schema Validation (MANDATORY):

- ALWAYS validate config files using `schemas/config-schema.json` before ANY modifications
- Settings schema: `schemas/settings-schema.json`
- Add schema validation comment to YAML files: `# yaml-language-server:
  $schema=https://raw.githubusercontent.com/recyclarr/recyclarr/master/schemas/config-schema.json`

## Debugging and Diagnosis

When diagnosing user configuration issues (invalid trash_ids, missing custom formats, template
errors), reference the local cached repositories in the app data directory. These contain the
authoritative upstream data that Recyclarr uses.

First, check `settings.yml` for configured resource providers. The `resource_providers` section
defines where Recyclarr loads CFs and templates from:

- `type: custom-formats` + `path:` - Local CF JSON files (check `service:` for radarr/sonarr)
- `type: config-templates` + `path:` - Local include templates
- Official providers are cached under `cache/resources/`

Search ALL configured provider locations for valid trash_ids, not just the official cache.

Cache Structure (under `cache/resources/`):

- `config-templates/git/official/` - Recyclarr include templates
  - `templates.json` - Template registry with IDs
  - `{radarr,sonarr}/templates/` - Top-level config templates
  - `{radarr,sonarr}/includes/` - Reusable include snippets (quality-profiles, custom-formats, etc.)
- `trash-guides/git/official/docs/json/` - TRaSH Guides resource data
  - `{radarr,sonarr}/cf/` - Custom format JSONs (one per CF, filename = slug)
  - `{radarr,sonarr}/quality-size/` - Quality definition sizes
  - `{radarr,sonarr}/quality-profiles/` - Quality profile definitions
  - `{radarr,sonarr}/naming/` - Media naming schemes

Diagnosis Workflow:

1. Identify the problematic `trash_id` from user logs/errors
2. Search the appropriate `cf/` directory: `rg "trash_id_value"
   cache/resources/trash-guides/.../cf/`
3. If not found, check git history in the cached repo to understand why:
   - `cd cache/resources/trash-guides/git/official && git log --all -p -S "trash_id_value" --
     docs/json/`
   - This reveals when/why the CF was removed, renamed, or consolidated
4. Cross-reference with user's config to identify which file contains the stale reference
5. For template issues, check `config-templates/.../templates/` and `includes/`
6. Check git history for templates similarly if include references are broken

Key insight: Sonarr and Radarr have SEPARATE custom format definitions with different trash_ids.
Audio/HDR/codec CFs have different IDs per service. A common misconfiguration is using Radarr
trash_ids in Sonarr configs (or vice versa).

## Reference Repositories

- Trash Guides: <https://github.com/TRaSH-Guides/Guides><br>
  Houses official trash guides markdown files, and resources such as custom formats, naming, quality
  sizes
- Recyclarr Config Templates: <https://github.com/recyclarr/config-templates><br>
  Contains official Recyclarr Config and Include Templates

## Memory Bank

- Working memory is in `docs/memory-bank`. This is for AI use only to track persistent memory
  between working sessions. Use `ls docs/memory-bank` at the start of a session or task to see
  what's available.
- You will freely read from, update/modify, create, and delete these memories as you see fit. These
  memory bank files are entirely AI-managed.
- Memory bank files should be small, single-topic units. Avoid monolithic memory bank files.
  Instead, focus on decomposing memories into reusable chunks (multiple files). These are easier to
  organize and load in isolation based on information AI needs for a given task.
