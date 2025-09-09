# Recyclarr Development Guide

## Project Context

.NET 9.0 CLI synchronizing TRaSH Guides to Sonarr/Radarr. Clean Architecture + Autofac + NUnit.

**IMPORTANT:** Uses SLNX format (`Recyclarr.slnx`) instead of traditional SLN files.

## Development Commands

### Build and Test

```bash
# Core commands
dotnet build
dotnet test
dotnet run --project src/Recyclarr.Cli/

# CI/Release
dotnet test -c Release --logger GitHubActions
dotnet publish src/Recyclarr.Cli/ -c Release -o publish/
ci/Publish.ps1 -Runtime <runtime-id>
ci/SmokeTest.ps1 publish/<runtime>/recyclarr

# Specific testing
dotnet test tests/Recyclarr.Core.Tests/
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

### Code Formatting

```bash
dotnet tool restore
dotnet csharpier format .    # ONLY formatting tool allowed
dotnet csharpier check .     # Verify formatting
```

**MANDATORY:**

- Use context7 to understand CSharpier before using it
- CSharpier is the ONLY formatting tool. Never use `dotnet format` or other formatters.

### Docker

```bash
./scripts/Docker-Debug.ps1           # External services
./scripts/Docker-Recyclarr.ps1 sync  # Container execution
```

## Architecture

- **Components**: Cli (entry) → Core (logic) → TrashGuide/ServarrApi (integrations)
- **Pipeline**: `GenericSyncPipeline<TContext>` - Config → Fetch → Transaction → Persist → Preview
- **DI**: Autofac via `CompositionRoot`, `CoreAutofacModule`, `PipelineAutofacModule`
- **Config**: YAML + `schemas/config-schema.json` validation
- **Testing**: NUnit 4 + NSubstitute + AutoFixture + parallel execution

## Key Files

- `src/Recyclarr.Cli/CompositionRoot.cs` - DI setup
- `src/Recyclarr.Core/CoreAutofacModule.cs` - Service registration
- `Directory.Packages.props` - Package versions
- `schemas/config-schema.json` - YAML validation

## Development Requirements

**MANDATORY WORKFLOW:**

1. Search existing code first: `rg "pattern"` before writing new code
2. Reuse/extend existing implementations - zero duplication tolerance
3. Run tests after changes: `dotnet test`
4. Format code: `dotnet csharpier format .`

**CODE STANDARDS:**

- .NET 9.0 + nullable reference types
- XML documentation for all public APIs
- Zero warnings/analysis issues
- Conventional commits
- Preserve YAML config backward compatibility

## C# Standards

**REQUIRED FEATURES:**

- File-scoped namespaces: `namespace Recyclarr.Core;`
- Primary constructors: `class Service(IDep dep, ILogger logger)`
- Collection expressions: `[]`, `[item]`, `[item1, item2]`, `[..collection]`
- Records for DTOs, `required` properties, `init` setters
- Pattern matching: `is not null`, switch expressions

**CLASS VISIBILITY:**

- Use `internal` for implementation classes (CLI apps, service implementations)
- Use `public` only for genuine external APIs
- Concrete classes implementing public interfaces should be `internal`

**PATTERNS:**

- Records for data models
- `IReadOnlyCollection<T>` return types
- LINQ method chaining over loops
- Spread operator for collections: `[..first, ..second]`
- `ValueTask` for hot paths, `CancellationToken` everywhere

**TESTING:**

- Use context7 to understand NUnit, NSubstitute, AutoFixture, FluentAssertions before using
- NUnit: `[Test]`, `internal sealed class {Name}Test`
- NSubstitute + AutoFixture + FluentAssertions
- `Freeze<T>()`, `Should().BeEquivalentTo()`

**AUTOFAC:**

- Use context7 to understand Autofac patterns before implementing DI
- Static registration methods in modules
- `RegisterType<Impl>().As<IInterface>()`
- Lifecycle: `SingleInstance()`, `InstancePerLifetimeScope()`

## Implementation Patterns

**New Sync Types:**

1. Context in `Recyclarr.Core/Pipelines/`
2. Pipeline stages (inherit base classes)
3. Register in `PipelineAutofacModule`
4. Update `schemas/config-schema.json`
5. Add tests

**Service Integration:**

- Use context7 to understand Spectre.Console and FluentValidation before implementing
- CLI: Inherit `ServiceCommand`
- API: Use `IServarrApi`
- Validation: `IServiceCompatibility` + FluentValidation

## Logging

**NEVER use Console.WriteLine** - inject `ILogger` everywhere.

- Use context7 to understand Serilog patterns before implementing logging
- `Debug()`: Diagnostics (requires `-d|--debug`)
- `Information()`: User status (use sparingly)
- `Warning()`: Non-critical issues
- `Error()`: Critical failures

## Dependencies

**MANDATORY:** Use context7 to understand each dependency before using: Spectre.Console, Autofac,
Serilog, FluentValidation, YamlDotNet, Flurl.Http, LibGit2Sharp

## Recyclarr Configuration & Operation

**Application Data Directories (Platform-Specific):**
- Windows: `%APPDATA%\recyclarr`
- Linux: `~/.config/recyclarr`
- macOS: `~/Library/Application Support/recyclarr`
- Docker: `/config`

**Configuration File Structure:**
- `recyclarr.yml` - Main config file in app data directory
- `configs/` - Additional YAML files (auto-loaded, non-recursive)
- `includes/` - Include templates directory for reusable YAML snippets
- `settings.yml` - Global settings file (optional)
- `cache/` - Internal cache data
- `logs/` - Application logs
- `repositories/` - Local clones of TRaSH guides and config templates

**Schema Validation (MANDATORY):**
- ALWAYS validate config files using `schemas/config-schema.json` before ANY modifications
- Settings schema: `schemas/settings-schema.json`
- Add schema validation comment to YAML files:
  `# yaml-language-server: $schema=https://raw.githubusercontent.com/recyclarr/recyclarr/master/schemas/config-schema.json`

**TRaSH Guides & Config Templates Integration:**
- TRaSH Guides repo auto-cloned to `${appdata}/repositories/trash_guides/`
- Config Templates repo auto-cloned to `${appdata}/repositories/config_templates/`
- Reference these local clones to understand `trash_ids`, custom formats, quality profiles
- Templates repo: `https://github.com/recyclarr/config-templates`
- Custom formats use `trash_ids` arrays (hexadecimal identifiers) from TRaSH guides

**Configuration Concepts:**
- `trash_ids`: Hexadecimal identifiers for TRaSH guide custom formats and profiles
- Include templates: Reusable YAML from config templates repo (use `include:` directive)
- Multiple config files supported in `configs/` directory (all `.yml`/`.yaml` files loaded)
- Secrets support via `secrets.yml` file (use `!secret` tags)
- Instance-based configuration: Multiple Sonarr/Radarr instances per config file
