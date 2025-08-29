# CLAUDE.md

**IMPORTANT:** This file provides mandatory guidance for Claude Code when working with this repository.

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
dotnet tool restore
dotnet format
dotnet csharpier .
dotnet format --verify-no-changes

# JetBrains tools (if available)
jb cleanupcode Recyclarr.sln --profile="Recyclarr Cleanup"
jb inspectcode Recyclarr.sln

# Versioning
dotnet gitversion
```

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

**Pipeline Architecture**: `GenericSyncPipeline<TContext>` phases: Config → API Fetch → Transaction → Persistence → Preview. Shared infrastructure across Sonarr/Radarr.

**Dependency Injection**: Autofac with composition root:
- `CompositionRoot`: Application bootstrap/container setup
- `CoreAutofacModule`: Core services/business logic  
- `PipelineAutofacModule`: Pipeline registrations

**Configuration**: YAML with schema validation, templates, environment variables, secrets. Schema: `schemas/config-schema.json`.

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
- Run `dotnet csharpier .` on modified files to format them correctly
- Follow existing repository coding conventions and style

### Code Style

**YOU MUST:**
- Use nullable reference types throughout
- Pass CSharpier formatting (`dotnet csharpier .`)
- Use "Recyclarr Cleanup" profile for cleanup
- Add XML documentation to all new code
- Follow conventional commits
- Target .NET 9.0
- Zero tolerance for warnings/analysis issues

### Modern Language Features

**YOU MUST:**
- Use collection expressions for all collection assignments/initialization
- Use LINQ method chaining instead of manual loops
- Avoid `UsedImplicitly` attribute unless addressing specific warnings

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
