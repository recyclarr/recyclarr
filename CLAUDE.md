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
- .NET 9.0 + nullable reference types
- DO NOT use XML documentation for ANY types.
- NO VERBOSE/USELESS C# COMMENTS. C# comments are code too, and thus incur a maintenance cost. They
  must have value. Focus on documenting the WHY, not WHAT code does. Preference for self-documenting
  code: Self-describing variable, class, function names, etc.
- Zero warnings/analysis issues
- Prefer polymorphism over enums when modeling behavior or extensibility. Propose enum vs
  polymorphism tradeoffs for discussion rather than defaulting to enums.

### C# Requirements

Language Features:

- File-scoped namespaces: `namespace Recyclarr.Core;`
- Primary constructors: `class Service(IDep dep, ILogger logger)`
- Collection expressions: `[]`, `[item]`, `[item1, item2]`, `[..collection]`
- Records for DTOs, `init` setters
- Pattern matching: `is not null`, switch expressions
- Spread operator for collections: `[..first, ..second]`

Required Idioms:

- Use `internal` for implementation classes (CLI apps, service implementations)
- Use `public` only for genuine external APIs
- Concrete classes implementing public interfaces should be `internal`
- Records for data models
- LINQ method chaining over loops
- LINQ method syntax only; NEVER use query syntax (from/where/select keywords)
- `ValueTask` for hot paths, `CancellationToken` everywhere (use `ct` for variable name)
- Avoid interface pollution: not every service class must have an interface. Add interfaces when
  justified (e.g. testability, more than one implementation)

### Testing Requirements

Core Mandates:

- **Tests must verify BEHAVIOR, not implementation detail!**
- ALWAYS test new functionality using a single, high level integration test that verifies the "happy
  path". Then, based on code coverage, consider other integration tests for failure or edge cases.
  Continue to validate code coverage (lines of code covered, NOT percentage statistics) after every
  single integration test is added. If an integration test is unable to exercise a significant area
  of code, only then should you consider a unit test (highest level of granularity).
- Avoid super granular unit tests with heavy mocking, even if you find this pattern in the existing
  code.
- Focus on high level integration tests that verify large chunks of the system. These are less
  brittle and result in more meaningful tests.
- Utilize hexagonal architecture (ports and adapters) methodology when writing tests: Stubs for
  external dependencies at a high level, with real objects in the center.
- Integration test fixtures MUST derive from one of the base test fixture classes:
  - `IntegrationTestFixture`: Integration tests for the Recyclarr.Core library.
  - `CliIntegrationTestFixture`: Integration tests for the Recyclarr.Cli library.

Patterns:

- NUnit: `[Test]`, `internal sealed class {Name}Test`
- NSubstitute + AutoFixture + **AwesomeAssertions** (NOT FluentAssertions)
- `Freeze<T>()`, `Should().BeEquivalentTo()`
- Static registration methods in modules
- `RegisterType<Impl>().As<IInterface>()`
- Lifecycle: `SingleInstance()`, `InstancePerLifetimeScope()`

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
- Avoid adding 'optimization' to `dotnet` CLI calls. For example, don't do `--no-build`,
  `--no-restore`, etc. Rely on simple invocations: `dotnet test` will always restore + build, so
  there's no need to do `dotnet build` followed by `dotnet test`.
- Use low-verbosity options for all tool commands. Generally, we only care about information we can
  act on (e.g. warnings, errors). Reason: Informational/debug logs consume valuable context.

## Scripts

All scripts under `scripts/`:

**Development and Testing:**

- `Docker-Debug.ps1`: Start external service dependencies (Sonarr, Radarr, Apprise) via docker
  compose. Use when debugging locally or testing integration scenarios.
  - Usage: `./scripts/Docker-Debug.ps1`
- `Docker-Recyclarr.ps1`: Run Recyclarr in a container (equivalent to `docker compose run`). Rarely
  used; auto-starts `Docker-Debug.ps1` if needed.
  - Usage: `./scripts/Docker-Recyclarr.ps1 sync`

**ONLY for human use (AI must never run these):**

- `Prepare-Release.ps1`: Initiates a release of Recyclarr.
- `Update-Gitignore.ps1`: Updates the global `.gitignore`.
- `Install-Tooling.ps1`: Install or update local tools.
- `Commit-Gitignore.ps1`: Commit git ignore changes.

## Release Notes / Changelogs

- Key File: `CHANGELOG.md`
- Follows keepachangelog.com format and rules
- Newest entries are at the top of the file under the `[Unreleased]` section.
- Audience: Non-technical end users
- When requested, add ONLY ONE line under the appropriate section for changes.
- Entries should be section-aware: For example, items under a section named "Fixed" shouldn't start
  with the word "Fixed".
- Entries should start with a general area/scope of the progrma to which the change applies. For
  example:

  ```md
  ### Fixed

  - Sync: Crash while processing quality profiles
  ```

## Conventional Commits

file path-based classification:

**Direct path mapping:**

- `ci:` → `.github/workflows/**`, `ci/*`, `scripts/*`
- `build:` → `*.props`, `*.csproj`, `*.slnx`, `Directory.*`, `.config/dotnet-tools.json`
- `chore:` → `.renovate/*`, `renovate.json5`, `.editorconfig`, `.gitignore`, `.csharpierignore`,
  `.yamllint.yml`, `.pre-commit-config.yaml`, `.markdownlint.json`, `.vscode/*`, `.dockerignore`
- `test:` → `tests/**/*.cs`, `tests/**/*.csproj`, `tests/**/Data/**`, `tests/**/*.md`
- `docs:` → Top-level `*.md` (exclude `tests/**/*.md`), `docs/**`, `LICENSE`, `CODEOWNERS`,
  `SECURITY.md`

**For `src/` files - inspect git diff + CHANGELOG.md:**

- `feat:` → New public class/interface/method, CHANGELOG "Added" section, new user-facing capability
- `fix:` → Bug fixes, CHANGELOG "Fixed", exception handling corrections
- `refactor:` → File moves/renames, internal restructuring, no CHANGELOG entry
- `perf:` → Performance improvements without functionality changes

**Breaking changes (!:):**

- YAML schema property removals in `schemas/**/*.json`
- CHANGELOG "Removed" section entries
- Settings migration in `Settings/Deprecations/`

**Scopes from paths:**

- `src/*/Pipelines/*` → `(sync)`, `src/*/Config/*` → `(config)`, `src/*/Console/Commands/*` →
  `(cli)`, `schemas/*` → `(yaml)`, `src/*/Cache/*` → `(cache)`

## Logging and Console Output

- Diagnostic information uses `ILogger` from serilog (facilitated via DI)
- User-facing messages use `IAnsiConsole` (facilitated via DI)
- NEVER use `Console.WriteLine`
- `ILogger.Debug()`: Diagnostics (requires `-d|--debug`)
- `ILogger.Information()`: User status (use sparingly)
- `ILogger.Warning()`: Non-critical issues (e.g. deprecations)
- `ILogger.Error()`: Critical failures (usually results in application stopping)
- Some user-facing logs still use Serilogs; this is legacy and will eventually be phased out.

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
