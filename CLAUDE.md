# CLAUDE.md

**IMPORTANT:** This file provides mandatory guidance for Claude Code when working with this
repository.

## Project Overview

Recyclarr: .NET 9.0 CLI application synchronizing TRaSH Guides settings to Sonarr/Radarr.
Architecture: Clean Architecture + Autofac DI + NUnit testing.

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

### Code Quality

```bash
# Essential commands
dotnet tool restore                    # Restore required tools
dotnet csharpier format .             # Format code (ONLY formatting tool - authoritative)
dotnet csharpier check .              # Check formatting without making changes

# Versioning
dotnet gitversion
```

**CRITICAL CSHARPIER USAGE FOR AI:**

- **CSharpier is the ONLY authoritative code formatting solution for this project**
- **NEVER use:** `dotnet csharpier .` (this is WRONG - shows help instead of formatting)
- **ALWAYS use:** `dotnet csharpier format .` (CORRECT - formats all files in current directory)
- **For verification:** `dotnet csharpier check .` (CORRECT - checks formatting without changes)
- **DO NOT use:** `dotnet format`, `jb cleanupcode`, or any other formatting tools

### Docker Development

```powershell
./scripts/Docker-Debug.ps1      # Start debug services
./scripts/Docker-Recyclarr.ps1 sync  # Run in container
```

## Architecture Overview

### Core Components

- **Recyclarr.Cli**: Entry point, commands, console, configuration
- **Recyclarr.Core**: Business logic, services, shared functionality
- **Recyclarr.TrashGuide**: TRaSH guides integration/processing
- **Recyclarr.ServarrApi**: Sonarr/Radarr API clients
- **Supporting libraries**: Platform, config, logging, notifications

**Pipeline Architecture**: `GenericSyncPipeline<TContext>` phases: Config → API Fetch → Transaction
→ Persistence → Preview. Shared infrastructure across Sonarr/Radarr.

**Dependency Injection**: Autofac with composition root:

- `CompositionRoot`: Application bootstrap/container setup
- `CoreAutofacModule`: Core services/business logic
- `PipelineAutofacModule`: Pipeline registrations

**Configuration**: YAML with schema validation, templates, environment variables, secrets. Schema:
`schemas/config-schema.json`.

### Key Services

- **Sync**: Custom Formats, Quality Profiles/Definitions, Media Naming
- **API**: Servarr REST integration (Sonarr/Radarr)
- **TRaSH**: Git repo sync and guide processing
- **Notifications**: Apprise-based system

### Testing Strategy

- **Unit**: NUnit 4 + NSubstitute + AutoFixture
- **Integration**: Real scenarios with comprehensive fixtures
- **Libraries**: `Recyclarr.TestLibrary`, `Recyclarr.Core.TestLibrary`
- **Execution**: Parallel for performance

## Key Files and Locations

### Core Architecture

- `src/Recyclarr.Cli/CompositionRoot.cs`: DI container setup
- `src/Recyclarr.Core/CoreAutofacModule.cs`: Core services registration
- `src/Recyclarr.Cli/Pipelines/GenericSyncPipeline.cs`: Pipeline implementation

### Configuration

- `Directory.Build.props`: MSBuild/compiler settings
- `Directory.Packages.props`: Package versions
- `GitVersion.yml`: Semantic versioning
- `.editorconfig`: Code formatting

### Directories

- `src/`: Source code by component
- `tests/`: Tests mirroring src structure
- `ci/`: Build/deployment scripts
- `schemas/`: Configuration JSON schemas

## Development Guidelines

### Mandatory Development Requirements

**CLAUDE MUST:**

- Run tests after making changes to verify functionality (`dotnet test`)
- Run `dotnet csharpier format .` on modified files to format them correctly (NEVER use
  `dotnet csharpier .`)
- Follow existing repository coding conventions and style

MANDATORY: ZERO DUPLICATION POLICY

**AI COMPLIANCE REQUIREMENT - EVERY CODE CHANGE:**

1. **Search first**: `rg "similar_logic"` - Find existing implementations BEFORE writing
2. **Reuse existing**: Use/extend existing code instead of recreating
3. **Refactor, don't duplicate**: If similar exists, make it reusable
4. **Extract immediately**: Move repeated patterns to shared utilities
5. **Verify**: Search again before completion to ensure no duplication introduced

**Duplication = Immediate failure. Search codebase first, always.**

### Code Style

**YOU MUST:**

- Use nullable reference types throughout
- Pass CSharpier formatting (`dotnet csharpier format .`) - CSharpier is the ONLY formatting tool
- Add XML documentation to all new code
- Follow conventional commits
- Target .NET 9.0
- Zero tolerance for warnings/analysis issues

### Backward Compatibility

**CODE**: No backward compatibility required - refactor freely **USER DATA**: Mandatory backward
compatibility - YAML configs and settings files must remain functional

## C# Coding Standards

**MANDATORY:** Follow these modern C# patterns for consistency, performance, and maintainability.

### Language Features (C# 12/13)

**YOU MUST use:**

- **File-scoped namespaces**: `namespace Recyclarr.Core;` (single line, reduce indentation)
- **Primary constructors**: `class Service(IDep dep, ILogger logger)` for DI injection
- **Collection spread operator**: `[..collection1, item2, ..collection2]` for combining collections
- **Collection expressions**: `[]` empty, `[item]` single, `[item1, item2]` multiple
- **Required properties**: `public required string Name { get; init; }` for mandatory fields
- **Init-only properties**: `{ get; init; }` for immutable after construction
- **Pattern matching**: `is not null`, `switch` expressions, property patterns
- **Params collections**: `params IEnumerable<T>` for flexible method parameters

### Class Visibility

**YOU MUST use `internal` access modifier for implementation details:**

- **Library classes with public interfaces**: When a class implements a public interface registered
  in Autofac, make the class `internal` since only the interface needs external access
- **Executable application classes**: In leaf projects (CLI applications), ALL classes should be
  `internal` as they are implementation details with no external dependencies
- **Service implementations**: Concrete service classes should be `internal` when registered via
  interface in DI container
- **Pipeline components**: Pipeline stages, contexts, and handlers should be `internal` unless
  explicitly designed for extension

**Exception:** Only mark classes `public` when they are genuinely intended for external consumption
(e.g., public APIs, extension points, or shared library contracts).

### Repository Patterns (Observed)

**YOU MUST follow existing patterns:**

- **Records everywhere**: Use `record` types for all data models and DTOs
- **Readonly collections**: `IReadOnlyCollection<T>` for return types (established pattern)
- **Autofac registration**: Static methods in modules, `RegisterTypes()`, `As<T>()`, lifecycle
  management
- **LINQ chains**: Extensive use of `SelectMany()`, `Where()`, `ToList()`, `Distinct()`, `OrderBy()`
- **Nullable reference types**: `#nullable enable` globally (Directory.Build.props)

### LINQ and Collections

**YOU MUST:**

- **Method chaining over loops**: `collection.Where().SelectMany().ToList()` not `foreach`
- **Spread for concatenation**: `[..first, ..second]` instead of `Concat()` or `Union()`
- **Collection initialization**: `ICollection<T> items = [item1, item2];`
- **Flattening**: `SelectMany()` for nested collections
- **Terminal operations**: `ToList()`, `ToArray()`, `First()`, `Any()`, `Count()` appropriately

### Async/Performance

**YOU MUST:**

- **ValueTask for hot paths**: Use when frequent synchronous completion expected
- **CancellationToken everywhere**: All async methods must accept CancellationToken
- **ConfigureAwait(false)**: In library code to avoid deadlocks
- **Span\<T>/Memory\<T>**: For performance-critical collection operations
- **Local functions after control flow**: Prevent unreachable code warnings

### Testing Standards

**YOU MUST follow existing stack:**

- **NUnit framework**: `[Test]`, `[TestCase]`, `internal sealed class {Name}Test`
- **NSubstitute mocking**: `Freeze<T>()`, `Returns()`, `ReturnsForAnyArgs()`
- **AutoFixture data**: `[AutoMockData]`, `fixture.Create<T>()`
- **FluentAssertions**: `Should().BeEquivalentTo()`, `Should().Throw<T>()`
- **Integration fixtures**: Inherit from `IntegrationTestFixture`

### Autofac DI Patterns

**YOU MUST follow established patterns:**

- **Module registration**: Static methods like `RegisterCache(ContainerBuilder builder)`
- **Lifecycle management**: `SingleInstance()`, `InstancePerLifetimeScope()`
- **Interface binding**: `RegisterType<Impl>().As<IInterface>()`
- **Keyed services**: `Keyed<T>(key)` for strategy patterns
- **Assembly scanning**: `RegisterTypes()` with type filters

### Testing

**YOU MUST:**

- Write comprehensive unit tests for all new functionality
- Use NSubstitute for mocking + AutoFixture for data
- Follow existing patterns in test libraries
- Use NUnit framework
- Create integration tests for end-to-end scenarios

### Dependencies

**IMPORTANT:** Package versions centralized in `Directory.Packages.props`

## Common Patterns

### Adding New Sync Types

1. Create pipeline context in `Recyclarr.Core/Pipelines/`
2. Implement pipeline stages (inherit from base classes)
3. Register in `PipelineAutofacModule`
4. Add configuration schema support
5. Create comprehensive tests

### Service Integration

- Inherit from `ServiceCommand` (CLI commands)
- Use `IServarrApi` (API communication)
- Implement `IServiceCompatibility` (version checking)
- Add FluentValidation

### Configuration Extensions

- Update `schemas/config-schema.json`
- Add post-processing in configuration pipeline
- Support environment variables/secrets

## Runtime Configurations

- **Development**: Debug builds with detailed logging
- **Testing**: In-memory configs with mocked dependencies
- **Production**: Optimized releases with file-based caching

Configs: YAML with schema validation and templates.

## Key Dependencies

- **Spectre.Console**: CLI framework
- **Autofac**: DI container
- **Serilog**: Structured logging
- **FluentValidation**: Config validation
- **YamlDotNet**: YAML serialization
- **Flurl.Http**: HTTP client
- **LibGit2Sharp**: Git operations
