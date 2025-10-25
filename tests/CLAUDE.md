# Testing Philosophy and Guidelines

**IMPORTANT:** This document defines mandatory testing requirements for Recyclarr.

## Core Testing Philosophy

**Primary Coverage**: Integration tests mock only externals (Git, filesystem, HTTP APIs) while using
real business logic. Provides majority coverage through public interfaces.

**Gap Filling**: Targeted lower-level tests for paths integration tests cannot reach (negative
testing, error conditions).

### Key Principles

**YOU MUST:**

- Test behavior, not implementation details
- Focus on interface contracts
- Ensure test resilience to architectural changes
- Validate real user scenarios and edge cases

## Testing Strategy by System

### System Integration Testing

**YOU MUST:**

- Test complete workflows through public interfaces
- Use real business logic implementations
- Mock only externals (Git, filesystem, HTTP APIs)
- Focus on end-to-end behavior

### Configuration System Testing

- **Integration**: Complete YAML parsing, validation, transformation, env vars, secrets
- **Targeted**: Validation errors, parsing edge cases, migrations

### Pipeline Testing

- **Integration**: End-to-end execution with mocked external APIs, multi-service sync
- **Targeted**: Error conditions, transaction rollbacks, data transformation edge cases

## Test Organization

### Directory Structure

```txt
tests/
├── Recyclarr.Core.Tests/
│   ├── IntegrationTests/     # High-level integration
│   └── Unit/                 # Targeted unit tests
├── Recyclarr.Cli.Tests/
│   ├── IntegrationTests/     # Command execution integration
│   └── Unit/                 # CLI unit tests
└── TestLibraries/
    ├── Recyclarr.TestLibrary/          # Core utilities
    └── Recyclarr.Core.TestLibrary/     # Core-specific utilities
```

### Test Structure Guidelines

**YOU MUST:**

- Use `internal sealed class {ClassName}Test` pattern
- Integration tests inherit from `IntegrationTestFixture`
- Custom mocks override `RegisterStubsAndMocks(ContainerBuilder builder)`
- Store test data in `Data/` directories as embedded resources

### Test Naming Conventions

**YOU MUST:**

- Test classes: `{ComponentUnderTest}Test`
- Integration tests: `{Component}IntegrationTest`
- Methods: Descriptive underscore-separated behavior names:
  - `Load_many_iterations_of_config`
  - `Can_handle_returns_true_with_templates`
  - `Throw_when_templates_dir_does_not_exist`

## Test Infrastructure Guidelines

### AutoFixture Usage Patterns

**YOU MUST use these patterns:**

- `[AutoMockData]`: Basic dependency injection
- `[InlineAutoMockData(params)]`: Parameterized tests with auto-mocking
- `[Frozen]` or `[Frozen(Matching.ImplementedInterfaces)]`: Shared mocks
- `[CustomizeWith(typeof(CustomizationType))]`: Specialized configurations
- `[AutoMockData(typeof(TestClass), nameof(MethodName))]`: DI container integration

### NSubstitute Mocking Patterns

**YOU MUST use these patterns:**

- `dependency.Method().Returns(value)`: Method returns
- `dependency.Property.ReturnsNull()`: Null property returns
- `dependency.Method(default!).ReturnsForAnyArgs(value)`: Flexible matching
- `Verify.That<T>(x => x.Property.Should().Be(expected))`: Complex assertions
- `dependency.Method().Returns([item1, item2])`: Collection returns

### Test Data Management

**YOU MUST:**

- Store JSON test data in `Data/` as embedded resources
- Use factory classes: `NewCf`, `NewConfig`, `NewQualitySize`
- Use collection initializers and record `with` expressions
- Use `MockFileSystem` and `MockFileData` for file system testing
- Mirror real-world scenarios and API responses

### Test Utilities and Libraries

**YOU MUST leverage:**

#### Libraries

- `Recyclarr.TestLibrary`: Core utilities (AutoFixture, NSubstitute extensions)
- `Recyclarr.Core.TestLibrary`: Core-specific fixtures/builders
- `Recyclarr.Cli.TestLibrary`: CLI utilities

#### Base Fixtures

- `IntegrationTestFixture`: Integration tests with DI container
- `CliIntegrationFixture`: CLI integration with composition root

#### Key Utilities

- `Verify.That<T>()`: NSubstitute matcher with FluentAssertions
- `TestableLogger`: Observable logger for testing log output
- `TestConsole`: Console output verification
- `MockFileSystem`, `MockFileData`: File system testing

#### Data Builders

- `NewCf.DataWithScore(name, trashId, score)`: CustomFormatData factory
- `NewConfig.Radarr()`, `NewConfig.Sonarr()`: Config factories
- `NewQualitySize`: Quality size factory

### Mocking Strategy

#### What to Mock (External Dependencies)

**YOU MUST mock:**

- Git operations (LibGit2Sharp)
- HTTP API calls (Sonarr/Radarr APIs)
- External configuration sources
- Filesystem (when testing logic, not I/O)

#### What NOT to Mock (Internal Dependencies)

**NEVER mock:**

- Business logic classes
- Data transformation logic
- Internal interfaces/contracts
- Domain models/value objects

#### Integration Test Mocking

**YOU MUST:**

- Use `RegisterStubsAndMocks(ContainerBuilder builder)` for custom mocks
- Use `MockFileSystem` for file operations
- Use `TestConsole` for console verification
- Mock infrastructure, use real business logic

## Assertion and Verification Guidelines

**YOU MUST use FluentAssertions patterns:**

### Standard Assertions

- `result.Should().BeEquivalentTo(expected)`: Deep object comparisons (PREFER over multiple property
  assertions)
- `act.Should().Throw<ExceptionType>().WithMessage("pattern")`: Exception verification
- `collection.Should().HaveCount(expected).And.Contain(item)`: Collections
- `result.Should().Be(true)` / `result.Should().BeFalse()`: Booleans
- `result.Should().BeNull()` / `result.Should().NotBeNull()`: Null checks

### Object Comparison Best Practices

- **PREFER**: `result.Should().BeEquivalentTo(expected)` for multi-property object verification
- **AVOID**: Multiple individual property assertions (`obj.Prop1.Should().Be()`,
  `obj.Prop2.Should().Be()`)
- **BENEFIT**: Cleaner, more maintainable tests that are resilient to property additions

### Advanced Patterns

- `result.Where(x => condition).Should().BeEquivalentTo(expected)`: Filtered comparisons
- `result.Select(x => x.Property).Should().BeEquivalentTo(expected)` for property-based comparisons
- `result.Should().NotBeNull().And.BeOfType<Type>()` for chained assertions

### NSubstitute Verification

- `mock.Received().Method(arguments)` for call verification
- `mock.Received().Property` for property access verification
- `Verify.That<T>(x => x.Property.Should().Be(expected))` for complex argument verification

## Performance Considerations

### Test Execution Speed

Claude MUST ensure tests follow these performance guidelines:

- Integration tests with mocked external dependencies run quickly (no network/disk I/O)
- Use `MockFileSystem` instead of real file operations for speed
- Ensure tests don't depend on external state or timing

### CI/CD Integration

Claude MUST ensure tests are CI-friendly:

- All tests should run reliably in CI environment
- Tests should not depend on external services or network access
- Tests should produce consistent results across different environments
- Tests should work on different operating systems where applicable

## Common Testing Patterns

### Exception Testing Pattern

Use `[Test, AutoMockData]` with lambda expressions and
`act.Should().Throw<ExceptionType>().WithMessage("pattern")` for exception testing.

### Integration Test Pattern

Integration tests inherit from `IntegrationTestFixture`, override
`RegisterStubsAndMocks(ContainerBuilder builder)` for custom mocks, use `Fs.AddFile()` for test
data, resolve components with `Resolve<ComponentUnderTest>()`, and verify results with
FluentAssertions.

### Unit Test Pattern

Unit tests use `[Test, AutoMockData]` with `[Frozen]` for shared dependencies, arrange mocks with
`Returns()`, and verify results with FluentAssertions.

### Parameterized Test Pattern

Parameterized tests use `[Test, InlineAutoMockData(params)]` with multiple test cases for different
input scenarios.

## Anti-Patterns to Avoid

Claude MUST avoid these anti-patterns:

### Brittle Test Patterns

Avoid over-mocking dependencies, coupling tests to implementation details, excessive test isolation,
and duplicating test coverage for the same logical paths.

### Maintenance Anti-Patterns

Avoid repeating test setup across multiple test classes, using unexplained constants instead of
meaningful test data, too many assertions that obscure test intent, adding code to production solely
for testing, and improper async/await patterns in test methods.

## Migration Strategy for Existing Tests

When updating existing tests to follow this philosophy:

1. Look for tests that can be combined into higher-level integration tests
2. Keep targeted tests for important edge cases that integration tests can't reach
3. Remove tests that are tightly coupled to implementation details
4. Use shared builders and utilities to reduce duplication
5. Shift from testing implementation to testing behavior and contracts

## Success Metrics

- Tests survive internal refactoring without modification
- High confidence in system behavior with reasonable test count
- Test updates required only for actual behavior changes
- Tests enable rapid development without excessive maintenance overhead
