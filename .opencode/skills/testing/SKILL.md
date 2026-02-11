---
name: testing
description: >
  Use when writing or modifying tests, improving coverage, debugging test failures, updating E2E
  fixtures, or working in tests/** directories.
---

# Testing Patterns and Infrastructure

## Technical Stack

- TUnit test framework
- NSubstitute for mocking
- AutoFixture for test data generation
- AwesomeAssertions for fluent assertions (NOT FluentAssertions)

## Test Pyramid

- **E2E**: Critical user workflows against real services (Testcontainers)
- **Integration**: Complete workflows with mocked externals (Git, HTTP, filesystem)
- **Unit**: Edge cases integration cannot reach

## Integration-First TDD Workflow

1. Write a failing integration test for the happy path (red)
2. Implement until it passes (green)
3. Check coverage (see Coverage Analysis section); add integration tests for uncovered edge cases
4. Use unit tests only when integration tests cannot reach specific code paths

## What NOT to Test

- Console output, log messages, UI formatting
- Auto-properties, DTOs, simple data containers
- Implementation details that could change without affecting behavior

## Naming

- Classes: `{Component}Test` or `{Component}IntegrationTest`
- Methods: Underscore-separated behavior (`Load_many_iterations_of_config`)
- Pattern: `internal sealed class`

## TUnit DI Data Source Architecture

Integration tests use TUnit's `DependencyInjectionDataSourceAttribute<TScope>` with Autofac. The
hierarchy:

```txt
DependencyInjectionDataSourceAttribute<ILifetimeScope>   (TUnit base)
  CoreDataSourceAttribute                                 (CoreAutofacModule + test doubles)
    CliDataSourceAttribute                                (CompositionRoot.Setup() superset)
      Custom overrides (per-test-class)                   (additional mocks)
```

**Key behaviors:**

- TUnit creates a **new class instance per test method** (full isolation by design)
- Each instance gets a fresh DI container scope
- Constructor parameters are resolved from the container
- `AnyConcreteTypeNotAlreadyRegisteredSource` is registered as a fallback resolver

### Basic integration test (CLI level)

```csharp
[CliDataSource]
internal sealed class MyFeatureIntegrationTest(
    IMyService sut,
    MockFileSystem fs,
    IAppPaths paths
)
{
    [Test]
    public void My_test()
    {
        sut.DoSomething();
        // assert
    }
}
```

### DI injection placement

- **Class-level** `[CliDataSource]`: Resolves constructor parameters. Use for dependencies shared
  across most/all test methods in the class.
- **Method-level** `[CliDataSource]`: Resolves method parameters. Use for dependencies needed by
  only one or a few test methods.

Class-level and method-level can coexist. The class attribute does NOT automatically inject method
parameters; each level requires its own attribute.

### Injecting registered types

Types registered via interfaces (e.g., `.As<IFoo>()`) must be injected by their **interface type**,
not the concrete type. Injecting the concrete type falls through to
`AnyConcreteTypeNotAlreadyRegisteredSource`, which creates a separate instance with potentially
different dependency resolution.

```csharp
// GOOD: Resolves the production registration
internal sealed class MyTest(IConfigCreationProcessor sut) { }

// BAD: Creates a second instance via AnyConcreteTypeNotAlreadyRegisteredSource
internal sealed class MyTest(ConfigCreationProcessor sut) { }
```

Exception: Types registered `.AsSelf()` or as concrete types can be injected directly.

### Custom data source overrides

When tests need additional mocks beyond the base `CliDataSourceAttribute`, create a custom attribute
that overrides `RegisterStubsAndMocks`:

```csharp
internal sealed class MyCustomDataSourceAttribute : CliDataSourceAttribute
{
    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterMockFor<IMyExternalService>();
    }
}
```

Use sparingly. Prefer the base attributes when possible.

### Child scope resolution

Some types are only available in child scopes (e.g., `PlanBuilder` lives inside a
`ConfigurationScope`). These cannot be constructor-injected and must be resolved from the child
scope:

```csharp
[CliDataSource]
internal sealed class MyTest(ConfigurationScopeFactory scopeFactory)
{
    [Test]
    public void My_test()
    {
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        // ...
    }
}
```

`Autofac` using is required for `scope.Resolve<>()` calls on child scopes.

## Unit Tests with AutoFixture

```csharp
[Test, AutoMockData]
public void My_unit_test([Frozen] IMyDependency dep, MySut sut)
{
    dep.Method().Returns(value);
    sut.DoSomething();
    dep.Received().Method();
}
```

- `[AutoMockData]`: Basic DI with mocks
- `[Frozen]` or `[Frozen(Matching.ImplementedInterfaces)]`: Shared mock instances
- `[CustomizeWith(typeof(T))]`: Custom ICustomization for parameter

## NSubstitute Patterns

```csharp
dependency.Method().Returns(value);
dependency.Property.ReturnsNull();
dependency.Method(default!).ReturnsForAnyArgs(value);
dependency.Method().Returns([item1, item2]);
mock.Received().Method(arguments);
Verify.That<T>(x => x.Property.Should().Be(expected));
```

## AwesomeAssertions

**Preferred:**

```csharp
result.Should().BeEquivalentTo(expected);
result.Select(x => x.Property).Should().BeEquivalentTo(expected);
act.Should().Throw<ExceptionType>().WithMessage("pattern");
collection.Should().HaveCount(n).And.Contain(item);
dict.Should().ContainKey(key).WhoseValue.Should().Be(expected);
```

**Anti-patterns:**

- `dict!["key"]!` -- use `ContainKey().WhoseValue` instead
- `HaveCount()` + `BeEquivalentTo()` -- redundant; equivalence checks count
- Multiple assertions instead of `.And` chaining

## Utilities

- `CoreDataSourceAttribute`: Core library integration tests (Autofac DI)
- `CliDataSourceAttribute`: CLI integration with full composition root
- `Verify.That<T>()`: NSubstitute matcher with assertions
- `TestableLogger`: Capture log messages
- `TestAnsiConsole`: Console output for tests (TUnit auto-captures Console.Out)
- `MockFileSystem`: Filesystem testing (avoid absolute paths)
- Factory classes: `NewCf`, `NewConfig`, `NewQualitySize`

## Filesystem Paths

Avoid absolute paths in `MockFileSystem` (platform-incompatible):

```csharp
// Good
fs.CurrentDirectory().SubDirectory("a", "b").File("c.json")

// Bad
"/absolute/path/file.json"
```

## Debugging Test Failures

**Gather evidence before changing code.** Avoid guess-and-check cycles.

1. **Read assertion output carefully** -- Diff output often reveals the issue immediately
2. **Add adhoc logs** -- Trace execution in tests or production code; remove when done
3. **Compare with passing tests** -- Diff similar working tests to spot differences
4. **Add intermediate assertions** -- Verify state at each step to pinpoint divergence
5. **Simplify to minimal reproduction** -- Strip test down, add back until failure
6. **Write adhoc granular tests** -- Isolate suspected areas; remove when done
7. **Check test isolation** -- Run alone (`--filter`) vs. suite to detect state leakage

## Test Framing

Tests serve as documentation. Choose framing based on what the test documents:

- **Positive tests** (expected behavior): Lead with what SHOULD happen, then verify absence of
  unintended side effects
- **Negative tests** (error conditions): Assert the error/rejection IS raised; essential for
  validating error paths

Both are equally important. The distinction is about clarity, not preference.

## Anti-Patterns

- Over-mocking or mocking business logic
- Tests coupled to implementation details
- Duplicate coverage for same logical paths
- Production code added solely for testing
- Unexplained magic constants
- Using `container.Resolve<T>()` instead of constructor/method injection

## Running Tests

```bash
# Unit and integration tests
dotnet test -v m

# Specific test project
dotnet test --project tests/Recyclarr.Cli.Tests -v m

# Single test class (TUnit tree filter)
dotnet test --project tests/Recyclarr.Cli.Tests -v m \
  --treenode-filter "/Recyclarr.Cli.Tests/Namespace/ClassName/**"

# E2E tests (requires Docker services)
./scripts/Run-E2ETests.ps1
```

## Coverage Analysis

Use coverage analysis to identify gaps before writing tests.

```bash
# Run tests + query uncovered lines (one-shot, preferred)
./scripts/coverage.py --run uncovered Platform Migration

# Run separately only when making multiple queries
./scripts/coverage.py --run
./scripts/coverage.py uncovered Platform
./scripts/coverage.py files Migration

# Find N files with lowest coverage
./scripts/coverage.py --run lowest 10
```

Patterns are **substring matches** (case-insensitive), not globs. Multiple patterns match files
containing ANY pattern. Examples:

- `Platform` matches `src/Recyclarr.Core/Platform/AppPaths.cs`
- `Platform Migration` matches files containing "Platform" OR "Migration"

Output format: `path:pct:covered/total[:uncovered_lines]`

CRITICAL: `--run` must succeed before querying. Investigate failures; coverage data is invalid on
failure. Run coverage BEFORE writing tests to understand gaps.

---

## End-to-End Tests

E2E tests run the full Recyclarr CLI against containerized Sonarr/Radarr instances. Tests verify
that sync operations produce expected state in the services.

### Running E2E Tests

**MANDATORY**: Use `./scripts/Run-E2ETests.ps1`; never run `dotnet test` directly for E2E tests. The
script outputs a log file path; use `rg` to search logs without rerunning tests.

### Resource Provider Strategy

The test uses multiple resource providers to verify different loading mechanisms:

#### Official Trash Guides (Pinned SHA)

```yaml
- name: trash-guides-pinned
  type: trash-guides
  clone_url: https://github.com/TRaSH-Guides/Guides.git
  reference: <pinned-sha>
  replace_default: true
```

**Purpose**: Baseline data that tests real-world compatibility.

**Use for**: Stable CFs that exist in official guides (e.g., `Bad Dual Groups`, `Obfuscated`).

**Why pinned**: Prevents upstream changes from breaking tests unexpectedly.

#### Local Custom Format Providers

```yaml
- name: sonarr-cfs-local
  type: custom-formats
  service: sonarr
  path: <local-path>
```

**Purpose**: Tests `type: custom-formats` provider behavior specifically.

**Use for**: CFs that need controlled structure or don't exist in official guides.

#### Trash Guides Override

```yaml
- name: radarr-override
  type: trash-guides
  path: <local-path>
```

**Purpose**: Tests override/layering behavior (higher precedence than official guides).

**Use for**:

- Quality profiles with known structure for testing inheritance
- CF groups with controlled members for testing group behavior
- CFs that override official guide CFs (e.g., HybridOverride)

### Fixture Directory Structure

```txt
Fixtures/
  recyclarr.yml              # Test configuration
  settings.yml               # Resource provider definitions
  custom-formats-sonarr/     # type: custom-formats provider (Sonarr)
  custom-formats-radarr/     # type: custom-formats provider (Radarr)
  trash-guides-override/     # type: trash-guides provider (override layer)
    metadata.json            # Defines paths for each resource type
    docs/
      Radarr/
        cf/                  # Custom formats
        cf-groups/           # CF groups
        quality-profiles/    # Quality profiles
      Sonarr/
        cf/
        cf-groups/
        quality-profiles/
```

### When to Use Each Provider Type

#### Use Official Guides When

- Testing sync of real-world CFs that are stable
- Testing compatibility with actual guide data structures
- The specific CF content doesn't matter, just that syncing works

#### Use Local Fixtures When

- Testing specific inheritance/override behavior
- Testing resources that don't exist in official guides
- Testing provider-specific loading behavior
- You need controlled, predictable resource structure

### Trash ID Conventions

- `e2e00000000000000000000000000001` -- E2E test Radarr quality profile
- `e2e00000000000000000000000000002` -- E2E test Sonarr quality profile
- `e2e00000000000000000000000000003` -- E2E test Sonarr guide-only profile
- `e2e00000000000000000000000000010` -- E2E test Sonarr CF group
- `e2e00000000000000000000000000011` -- E2E test Radarr CF group
- `00000000000000000000000000000001` through `00000000000000000000000000000007` -- Local test CFs

### Adding New Test Cases

1. **For new CFs**: Add JSON to appropriate `custom-formats-*` or `trash-guides-override/docs/*/cf/`
2. **For new QPs**: Add JSON to `trash-guides-override/docs/*/quality-profiles/`
3. **For new CF groups**: Add JSON to `trash-guides-override/docs/*/cf-groups/`
4. **Update metadata.json** if adding new resource type paths
5. **Update recyclarr.yml** to reference the new trash_ids
6. **Update test assertions** in `RecyclarrSyncTests.cs`

### metadata.json Structure

The metadata.json file tells Recyclarr where to find each resource type:

```json
{
  "json_paths": {
    "radarr": {
      "custom_formats": ["docs/Radarr/cf"],
      "qualities": [],
      "naming": [],
      "custom_format_groups": ["docs/Radarr/cf-groups"],
      "quality_profiles": ["docs/Radarr/quality-profiles"]
    },
    "sonarr": { "..." }
  }
}
```

**Important**: Paths must not contain spaces. Use `cf` instead of `Custom Formats`.
