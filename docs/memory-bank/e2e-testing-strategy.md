# E2E Testing Strategy and Implementation

## Overview

End-to-end testing strategy for Recyclarr using .NET + Testcontainers for black-box testing against
real Sonarr/Radarr containers.

## Test Pyramid Philosophy

1. **E2E Tests** (Top) - Black-box tests against real services, minimal mocking
2. **Integration Tests** (Middle) - C# tests with controlled mocking
3. **Unit Tests** (Bottom) - Isolated component tests

## Implementation Architecture

### Technology Stack

- **Test Framework**: NUnit 4
- **Container Management**: Testcontainers for .NET 4.8.1
- **CLI Execution**: CliWrap (execute recyclarr as external process)
- **HTTP Client**: Flurl.Http (fluent API calls)
- **Assertions**: AwesomeAssertions (project standard)

### Rationale

- Industry standard for .NET Docker testing
- Matches existing test infrastructure (NUnit patterns)
- Type-safe assertions with AwesomeAssertions
- Better IDE support and debugging
- Single language (C#) for entire codebase
- Team familiarity

### Project Structure

```txt
tests/Recyclarr.E2ETests/
├── Recyclarr.E2ETests.csproj
├── RecyclarrSyncTests.cs          # Main test class
├── Fixtures/
│   └── recyclarr.yml               # Comprehensive config
└── Helpers/
    └── SonarrRadarrClient.cs       # API client helpers
```

### Black-Box Approach

- **Zero code dependencies** on Recyclarr.Cli or Recyclarr.Core
- Recyclarr binary published via `dotnet publish` (self-contained linux-x64)
- Executed as external process via CliWrap
- Tests only interact with binary and Sonarr/Radarr APIs

### Test Lifecycle

1. **OneTimeSetUp**:
   - Publish recyclarr binary once
   - Start Sonarr container (linuxserver/sonarr:latest)
   - Start Radarr container (linuxserver/radarr:latest)
   - Wait for health checks
   - Retrieve API keys from containers

2. **Test Execution**:
   - Create runtime config with container URLs/API keys
   - Execute `recyclarr sync --config <path>`
   - Assert exit code = 0
   - Verify state via Flurl HTTP calls to Sonarr/Radarr APIs

3. **OneTimeTearDown**:
   - Dispose Sonarr container
   - Dispose Radarr container

### Test Coverage

Single comprehensive test verifying:

- Quality profiles (Sonarr + Radarr)
- Custom formats (50+ formats from TRaSH guides)
- Quality definitions
- Media naming formats
- Exit code validation

## CI/CD Integration

### GitHub Actions Workflow

- **File**: `.github/workflows/e2e-tests.yml`
- **Trigger**: Push to `master` branch only
- **Steps**:
  1. Checkout repository
  2. Setup .NET 9.0
  3. Restore dependencies
  4. Run `dotnet test --filter Category=E2E`
  5. Upload artifacts on failure (test results, config files)

### Requirements

- Docker runtime available (GitHub hosted runners include Docker)
- ~10-15 minute timeout for container startup and sync

## Package Management

### Central Package Management

- Versions defined in `Directory.Packages.props`
- PackageReference in csproj files omit `Version` attribute
- Use `dotnet add package <name>` (without version) to add packages
- Manual edit of `Directory.Packages.props` required to set versions

### Key Packages

- `Testcontainers` - 4.8.1
- `CliWrap` - 3.9.0
- `Flurl.Http` - 4.0.2
- `AwesomeAssertions` - 9.3.0
- `NUnit` - 4.4.0

## Implementation Status

### Completed

- ✅ E2E test project created (`tests/Recyclarr.E2ETests`)
- ✅ Package dependencies configured (central package management)
- ✅ `RecyclarrSyncTests.cs` with NUnit lifecycle hooks
- ✅ `SonarrRadarrClient.cs` helper for API key retrieval
- ✅ Comprehensive `recyclarr.yml` fixture config
- ✅ GitHub Actions workflow configured
- ✅ Project added to `Recyclarr.slnx` solution
- ✅ Build successful (warnings only, no errors)

### Pending

- API key retrieval mechanism needs refinement (currently uses fallback GUID)
- AwesomeAssertions syntax needs verification for actual assertions
- Test execution validation (requires Docker locally)
- Consider expanding to multiple test cases vs single comprehensive test

## Key Technical Details

### NUnit Lifecycle Hooks

- `[OneTimeSetUp]` - Runs once before all tests (container startup)
- `[OneTimeTearDown]` - Runs once after all tests (container cleanup)
- Async methods fully supported

### Flurl.Http Usage

- `baseUrl.AppendPathSegment("path")` requires `using Flurl;`
- Fluent chaining: `.WithHeader("X-Api-Key", key).GetJsonAsync<T>()`
- More ergonomic than raw HttpClient for API testing

### CliWrap Patterns

- `ExecuteBufferedAsync()` captures stdout/stderr in memory
- `WithValidation(CommandResultValidation.None)` prevents exception on non-zero exit
- Access via `.StandardOutput` and `.ExitCode` properties

## Future Enhancements

### Test Expansion

- Separate tests per feature area (quality profiles, custom formats, etc.)
- Test error scenarios (invalid config, unreachable services)
- Test different Sonarr/Radarr versions via matrix

### Container Optimization

- Use pre-configured Sonarr/Radarr images with known API keys
- Reduce container startup time with health check tuning
- Consider container reuse across tests (if applicable)

### Configuration

- Template-based config generation (avoid string replacement)
- Test different config scenarios (minimal, comprehensive, edge cases)

## References

- Context7: Testcontainers for .NET docs
- Context7: Flurl.Http docs
- Context7: CliWrap docs
- Recyclarr project conventions (`CLAUDE.md`)
