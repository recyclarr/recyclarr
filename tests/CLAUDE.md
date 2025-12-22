# Testing Philosophy and Guidelines

**IMPORTANT:** This document defines mandatory testing requirements for Recyclarr.

## Core Testing Philosophy

**Test Pyramid Strategy:**

- **E2E (Tip)**: Critical user workflows produce expected system state. Minimal detail, high-level
  outcomes.
- **Integration (Middle)**: Complete workflows through public interfaces. Mock externals only (Git,
  filesystem, HTTP APIs). Real business logic.
- **Unit (Base)**: Targeted tests for paths integration cannot reach (error conditions, edge cases).

**Key Principles:**

- Test behavior, not implementation
- Focus on interface contracts
- Resilient to architectural changes
- Validate user scenarios and edge cases

## Test Types by Layer

### E2E Tests (`tests/Recyclarr.EndToEndTests/`)

**Purpose**: Validate critical user workflows end-to-end with real external services.

**Running E2E Tests**:

- **MANDATORY**: Use `./scripts/Run-E2ETests.ps1` - NEVER run `dotnet test` directly for E2E tests
- The script outputs a log file path; use `rg` to search logs without rerunning tests

**Scope**:

- Full `recyclarr sync` execution against containerized Sonarr/Radarr
- Verify expected configuration state (profiles exist, custom formats synced)
- High-level outcomes only - detailed validation belongs in integration/unit tests

**Infrastructure**:

- Use Testcontainers for external services
- Isolated temp directories for app data and published binaries
- Test idempotency via clean state per run

**When to Use**: Validating complete user workflows produce correct system state.

### Integration Tests

**Coverage**:

- Configuration: YAML parsing, validation, transformation, env vars, secrets
- Pipeline: End-to-end execution with mocked APIs, multi-service sync
- CLI: Command execution through composition root

**Requirements**:

- Inherit from `IntegrationTestFixture` or `CliIntegrationFixture`
- Mock externals only: Git (LibGit2Sharp), HTTP APIs, filesystem (MockFileSystem)
- Use real business logic, data transformation, domain models
- Override `RegisterStubsAndMocks(ContainerBuilder builder)` for custom mocks

### Unit Tests

**Coverage**: Validation errors, parsing edge cases, transaction rollbacks, data transformation edge
cases.

**Patterns**: `[Test, AutoMockData]` with `[Frozen]` dependencies, arrange mocks with `Returns()`.

## Test Organization

**Directory Structure**:

```txt
tests/
├── Recyclarr.EndToEndTests/     # E2E tests
├── Recyclarr.Core.Tests/
│   ├── IntegrationTests/
│   └── Unit/
├── Recyclarr.Cli.Tests/
│   ├── IntegrationTests/
│   └── Unit/
└── TestLibraries/
    ├── Recyclarr.TestLibrary/          # Core utilities
    └── Recyclarr.Core.TestLibrary/     # Core-specific utilities
```

**Naming Conventions**:

- Classes: `{Component}Test` or `{Component}IntegrationTest`
- Methods: Descriptive underscore-separated behavior (`Load_many_iterations_of_config`,
  `Throw_when_templates_dir_does_not_exist`)

**Structure**:

- `internal sealed class` pattern
- Store test data in `Data/` as embedded resources
- Use factory classes: `NewCf`, `NewConfig`, `NewQualitySize`

## Test Infrastructure

### AutoFixture Patterns

- `[AutoMockData]`: Basic DI
- `[InlineAutoMockData(params)]`: Parameterized tests
- `[Frozen]` or `[Frozen(Matching.ImplementedInterfaces)]`: Shared mocks
- `[CustomizeWith(typeof(CustomizationType))]`: Specialized config
- `[AutoMockData(typeof(TestClass), nameof(MethodName))]`: DI container integration

### NSubstitute Patterns

- `dependency.Method().Returns(value)`: Method returns
- `dependency.Property.ReturnsNull()`: Null returns
- `dependency.Method(default!).ReturnsForAnyArgs(value)`: Flexible matching
- `Verify.That<T>(x => x.Property.Should().Be(expected))`: Complex assertions
- `dependency.Method().Returns([item1, item2])`: Collections

### Utilities

**Base Fixtures**:

- `IntegrationTestFixture`: Integration tests with DI
- `CliIntegrationFixture`: CLI integration with composition root

**Key Tools**:

- `Verify.That<T>()`: NSubstitute matcher with AwesomeAssertions
- `TestableLogger`: Log message capture for assertions
- `TestConsole`: Console verification
- `MockFileSystem`, `MockFileData`: Filesystem testing

## Assertions (AwesomeAssertions)

**Preferred Patterns**:

- `result.Should().BeEquivalentTo(expected)`: Deep object comparison (prefer over multiple property
  assertions). Prefer strongly typed objects unless that requires specifying properties out of scope
  for the test; in that case, use anonymous types.
- `result.Select(x => x.Property).Should().BeEquivalentTo(expected)`: Property-based collection
  comparison
- `act.Should().Throw<ExceptionType>().WithMessage("pattern")`: Exceptions
- `collection.Should().HaveCount(n).And.Contain(item)`: Collections
- `result.Should().Be(true)` / `result.Should().BeFalse()`: Booleans

**Advanced**:

- `result.Where(x => condition).Should().BeEquivalentTo(expected)`: Filtered comparisons
- `result.Should().NotBeNull().And.BeOfType<Type>()`: Chained assertions

**Dictionary Assertions**:

- `dict.Should().ContainKey(key).WhoseValue.Should()...`: Safe key access with chained assertions
- Avoid `dict[key]!` or `dict![key]!` - use `ContainKey().WhoseValue` instead

**Anti-Patterns to Avoid**:

- Null-forgiving indexer access (`dict!["key"]!`) - use `ContainKey().WhoseValue` instead
- Redundant count checks before equivalence assertions (e.g., `HaveCount()` + `BeEquivalentTo()`)
- Multiple assertions on same subject instead of `.And` chaining
- Overly granular assertions when a single `BeEquivalentTo()` suffices

**NSubstitute Verification**:

- `mock.Received().Method(arguments)`: Call verification
- `Verify.That<T>(x => x.Property.Should().Be(expected))`: Complex argument verification

## Anti-Patterns

**Avoid**:

- Over-mocking dependencies or mocking business logic/domain models
- Coupling tests to implementation details
- Excessive test isolation or granularity
- Duplicate coverage for same logical paths
- Repeated setup across test classes
- Unexplained magic constants
- Too many assertions obscuring intent
- Production code added solely for testing

## Performance and CI/CD

**Requirements**:

- Integration tests with mocked externals run quickly (no network/disk I/O)
- Use `MockFileSystem` instead of real file operations
- No external state or timing dependencies
- Reliable in CI across environments and operating systems
- Consistent, deterministic results

## Success Metrics

- Tests survive internal refactoring without modification
- High confidence with reasonable test count
- Updates required only for behavior changes
- Rapid development without excessive maintenance
