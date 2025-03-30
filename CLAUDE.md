# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this
repository.

## Project Overview

Recyclarr is a .NET 9.0 CLI application that automatically synchronizes recommended settings from
TRaSH Guides to Sonarr/Radarr instances. It follows Clean Architecture principles with extensive
dependency injection using Autofac and comprehensive testing with NUnit.

## Development Commands

### Build and Test

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run tests in release mode for CI
dotnet test -c Release --logger GitHubActions

# Run tests for a specific project
dotnet test tests/Recyclarr.Core.Tests/
dotnet test tests/Recyclarr.Cli.Tests/

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Build and run the application
dotnet run --project src/Recyclarr.Cli/

# Publish release build
dotnet publish src/Recyclarr.Cli/ -c Release -o publish/

# Publish for specific runtime
ci/Publish.ps1 -Runtime <runtime-id>

# Run smoke tests
ci/SmokeTest.ps1 publish/<runtime>/recyclarr
```

### Code Quality

```bash
# Restore tools
dotnet tool restore

# Run code formatting
dotnet format

# Format code with CSharpier
dotnet csharpier .

# Check formatting without changes
dotnet format --verify-no-changes

# Run code cleanup (requires JetBrains tools)
jb cleanupcode Recyclarr.sln --profile="Recyclarr Cleanup"

# Run code analysis
jb inspectcode Recyclarr.sln

# Version the build (GitVersion)
dotnet gitversion
```

### Docker Development

```powershell
# Start debug services (Radarr, Sonarr, etc.)
./scripts/Docker-Debug.ps1

# Run Recyclarr in container with local changes
./scripts/Docker-Recyclarr.ps1 sync
```

## Architecture Overview

### Core Components

**Main Components**:

- **Recyclarr.Cli**: Entry point, commands, console interface, and configuration management
- **Recyclarr.Core**: Core business logic, services, and shared functionality
- **Recyclarr.TrashGuide**: TRaSH guides integration and data processing
- **Recyclarr.ServarrApi**: API clients for Sonarr/Radarr communication
- **Supporting libraries**: Platform abstraction, configuration, logging, notifications, etc.

**Pipeline Architecture**: Uses `GenericSyncPipeline<TContext>` for data synchronization with
phases: Config → API Fetch → Transaction → Persistence → Preview. All pipelines share common
infrastructure while supporting different service types (Sonarr, Radarr).

**Dependency Injection**: Autofac-based DI with composition root pattern. Main modules:

- `CompositionRoot` - Application bootstrap and container setup
- `CoreAutofacModule` - Core services and business logic
- `PipelineAutofacModule` - Pipeline-specific registrations

**Configuration System**: YAML-based configuration with schema validation, includes/templates
support, environment variable substitution, and secrets management. Schema file:
`schemas/config-schema.json`.

### Key Services

**Sync Services**: Custom Formats, Quality Profiles, Quality Definitions, Media Naming **API
Clients**: Servarr REST API integration for Sonarr/Radarr **TRaSH Integration**: Git repository
synchronization and guide processing **Notifications**: Apprise-based notification system

### Testing Strategy

- **Unit Tests**: NUnit 4 with NSubstitute mocking and AutoFixture data generation
- **Integration Tests**: Real-world scenario testing with comprehensive fixtures
- **Test Libraries**: Shared test infrastructure in `Recyclarr.TestLibrary` and
  `Recyclarr.Core.TestLibrary`
- **Parallel Execution**: Tests run in parallel for performance

## Key Files and Locations

### Core Architecture

- `src/Recyclarr.Cli/CompositionRoot.cs` - Main DI container setup
- `src/Recyclarr.Core/CoreAutofacModule.cs` - Core services registration
- `src/Recyclarr.Cli/Pipelines/GenericSyncPipeline.cs` - Pipeline pattern implementation

### Configuration

- `Directory.Build.props` - MSBuild configuration and compiler settings
- `Directory.Packages.props` - Centralized package version management
- `GitVersion.yml` - Semantic versioning configuration
- `.editorconfig` - Code formatting rules
- `.config/dotnet-tools.json` - Local development tools

### Build System

- `Recyclarr.sln` - Main solution file
- `.github/workflows/build.yml` - Multi-platform CI/CD pipeline
- `ci/` - Build scripts and automation tools

### Key Directories

- `src/` - Main source code organized by component
- `tests/` - Unit and integration tests mirroring src structure
- `ci/` - Build and deployment scripts
- `docker/` - Docker configuration and debugging setup
- `schemas/` - JSON schemas for configuration validation
- `scripts/` - PowerShell utility scripts

## Development Guidelines

### Code Style

- Uses nullable reference types throughout
- High warning level with extensive static analysis
- EditorConfig enforced formatting
- Follows Clean Architecture and SOLID principles
- Zero tolerance for warnings and analysis issues
- Code must pass CSharpier formatting
- Use "Recyclarr Cleanup" profile for code cleanup
- All new code requires XML documentation
- Follow conventional commits for commit messages
- Target .NET 9.0 with nullable reference types enabled

### Modern Language Features

- ALWAYS use collection expressions when assigning to or initializing collection types.
- Never use the UsedImplicitly attribute unless there is an explicit warning you're trying to
  address it is designed to solve.
- Rely on LINQ method chaining to perform collection transformations instead of hand-rolled foreach,
  while, or for loops.

### Testing

- Write comprehensive unit tests for all new functionality
- Use NSubstitute for mocking dependencies
- Leverage AutoFixture for test data generation
- Follow existing test patterns in test libraries
- Unit tests using NUnit framework
- Integration tests for end-to-end scenarios
- Test libraries provide common fixtures and utilities
- Tests run only on x64 platforms in CI

### Dependencies

- All package versions centralized in `Directory.Packages.props`
- Renovate handles automated dependency updates
- Security scanning with Snyk integration
- Age-based dependency update policies

## Common Patterns

### Adding New Sync Types

1. Create pipeline context in `Recyclarr.Core/Pipelines/`
2. Implement pipeline stages inheriting from base classes
3. Register pipeline in `PipelineAutofacModule`
4. Add configuration schema support
5. Create comprehensive tests

### Service Integration

- Inherit from `ServiceCommand` for CLI commands
- Use `IServarrApi` for API communication
- Implement `IServiceCompatibility` for version checking
- Add validation with FluentValidation

### Configuration Extensions

- Update `schemas/config-schema.json` for IntelliSense
- Add post-processing in configuration pipeline
- Support environment variable substitution
- Include secrets management considerations

## Running and Debugging

The application supports various runtime configurations:

- Development: Debug builds with detailed logging
- Testing: In-memory configurations and mocked dependencies
- Production: Optimized releases with file-based caching

Configuration files are YAML-based with comprehensive schema validation and template support.

## Notable Dependencies

- **Spectre.Console**: CLI framework and rich console output
- **Autofac**: Dependency injection container
- **Serilog**: Structured logging
- **FluentValidation**: Configuration validation
- **YamlDotNet**: YAML serialization
- **Flurl.Http**: HTTP client for API communication
- **LibGit2Sharp**: Git operations for repository management
