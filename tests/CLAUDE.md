# Testing Philosophy and Guidelines

This document outlines the testing approach and philosophy for the Recyclarr project to guide
consistent, maintainable, and effective test development.

## Core Testing Philosophy

### Pragmatic Test Pyramid Approach

Our testing strategy combines integration testing with targeted lower-level tests:

**Primary Coverage**: High-level integration tests mock only external dependencies (Git, filesystem,
HTTP APIs) while using real business logic implementations. These tests provide majority coverage
through public interfaces and remain resilient to internal refactoring.

**Gap Filling**: Targeted lower-level tests reach specific control paths that integration tests
cannot access, particularly for negative testing and error conditions. These tests go "down a level
or two" to validate specific code paths without excessive granularity.

### Key Principles

Test system behavior rather than implementation details. Focus on interface contracts over specific
implementations. Prioritize test resilience to architectural changes. Ensure each test validates
real user scenarios or important edge cases.

## Testing Strategy by System

### System Integration Testing

Test complete workflows through public interfaces with real business logic implementations. Mock
only external dependencies (Git, filesystem, HTTP APIs). Focus on end-to-end behavior rather than
internal component interaction.

### Configuration System Testing

Integration tests cover complete YAML parsing, validation, transformation, environment variable
substitution, and secrets management. Targeted tests handle specific validation errors, parsing edge
cases, and migration scenarios.

### Pipeline Testing

Integration tests cover end-to-end pipeline execution with mocked external APIs, multi-service
synchronization, and configuration-to-execution workflows. Targeted tests handle specific error
conditions, transaction rollbacks, and data transformation edge cases.

## Test Organization

### Directory Structure

```txt
tests/
├── CLAUDE.md                           # This file
├── Recyclarr.Core.Tests/               # Core business logic tests
│   ├── IntegrationTests/               # High-level integration tests
│   └── Unit/                           # Targeted unit tests for specific paths
├── Recyclarr.Cli.Tests/                # CLI-specific tests
│   ├── IntegrationTests/               # Command execution integration tests
│   └── Unit/                           # CLI component unit tests
└── TestLibraries/                      # Shared test infrastructure
    ├── Recyclarr.TestLibrary/          # Core test utilities
    └── Recyclarr.Core.TestLibrary/     # Core-specific test utilities
```

### Test Structure Guidelines

Claude MUST follow these test structure patterns:

- Test classes use `internal sealed class {ClassName}Test` pattern
- Integration tests inherit from `IntegrationTestFixture`
- Custom mocks override `RegisterStubsAndMocks(ContainerBuilder builder)`
- Test data files stored in `Data/` directories as embedded resources

### Test Naming Conventions

Claude MUST follow these naming conventions:

- Test classes: `{ComponentUnderTest}Test` (e.g., `ConfigurationLoaderTest`,
  `TemplateConfigCreatorTest`)
- Integration tests: `{Component}IntegrationTest` when explicitly integration-focused
- Test methods: Use descriptive underscore-separated names describing behavior:
  - `Load_many_iterations_of_config`
  - `Can_handle_returns_true_with_templates`
  - `No_replace_when_file_exists_and_not_forced`
  - `Throw_when_templates_dir_does_not_exist`

## Test Infrastructure Guidelines

### AutoFixture Usage Patterns

Claude MUST use AutoFixture with these established patterns:

- `[AutoMockData]` for basic dependency injection
- `[InlineAutoMockData(params)]` for parameterized tests with auto-mocking
- `[Frozen]` or `[Frozen(Matching.ImplementedInterfaces)]` for shared mocks
- `[CustomizeWith(typeof(CustomizationType))]` for specialized configurations
- `[AutoMockData(typeof(TestClass), nameof(MethodName))]` for DI container integration

### NSubstitute Mocking Patterns

Claude MUST follow these NSubstitute patterns:

- `dependency.Method().Returns(value)` for method returns
- `dependency.Property.ReturnsNull()` for null property returns
- `dependency.Method(default!).ReturnsForAnyArgs(value)` for flexible matching
- `Verify.That<T>(x => x.Property.Should().Be(expected))` for complex assertions
- `dependency.Method().Returns([item1, item2])` for collection returns

### Test Data Management

Claude MUST follow these test data patterns:

- Store JSON test data in `Data/` directories as embedded resources
- Use factory classes like `NewCf`, `NewConfig`, `NewQualitySize` for test objects
- Use collection initializers and record `with` expressions for simple variations
- Use `MockFileSystem` and `MockFileData` for file system testing
- Ensure test data mirrors real-world scenarios and API responses

### Test Utilities and Libraries

Claude MUST leverage these shared test utilities:

#### Core Test Libraries

- `Recyclarr.TestLibrary`: Core testing utilities (AutoFixture, NSubstitute extensions)
- `Recyclarr.Core.TestLibrary`: Core-specific test fixtures and builders
- `Recyclarr.Cli.TestLibrary`: CLI-specific test utilities

#### Base Test Fixtures

- `IntegrationTestFixture`: Base class for integration tests with DI container setup
- `CliIntegrationFixture`: CLI-specific integration testing with composition root

#### Test Utilities

- `Verify.That<T>()`: Custom NSubstitute argument matcher using FluentAssertions
- `FileUtils`: File system testing utilities
- `MockData`: Common mock data generation
- `TestableLogger`: Observable logger for testing log output
- `TestConsole`: Console output verification for CLI tests

#### Test Data Builders

- `NewCf.DataWithScore(name, trashId, score)`: Factory for `CustomFormatData` objects
- `NewConfig.Radarr()`, `NewConfig.Sonarr()`: Factory for configuration objects
- `NewQualitySize`: Factory for quality size objects

### Mocking Strategy

Claude MUST follow these mocking guidelines:

#### What to Mock (External Dependencies)

- Git operations (LibGit2Sharp)
- Filesystem operations (when testing logic, not file I/O)
- HTTP API calls (Sonarr/Radarr APIs)
- External configuration sources

#### What NOT to Mock (Internal Dependencies)

- Business logic classes
- Data transformation logic
- Internal interfaces and contracts
- Domain models and value objects

#### Integration Test Mocking

- Use `RegisterStubsAndMocks(ContainerBuilder builder)` for custom mock registration
- Use `MockFileSystem` consistently for file operations
- Use `TestConsole` for console output verification
- Mock infrastructure while using real business logic components

## Assertion and Verification Guidelines

Claude MUST use FluentAssertions with these established patterns:

### Standard Assertions

- `result.Should().BeEquivalentTo(expected)` for deep object comparisons
- `act.Should().Throw<ExceptionType>().WithMessage("pattern")` for exception verification
- `collection.Should().HaveCount(expected)` and `collection.Should().Contain(item)` for collections
- `result.Should().Be(true)` or `result.Should().BeFalse()` for boolean assertions
- `result.Should().BeNull()` or `result.Should().NotBeNull()` for null assertions

### Advanced Assertion Patterns

- `result.Where(x => condition).Should().BeEquivalentTo(expected)` for filtered comparisons
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
