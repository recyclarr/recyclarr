---
name: testing
description: >
  Use when writing or modifying tests, improving coverage, debugging test failures, updating E2E
  fixtures, or working in tests/** directories.
---

# Testing Patterns and Infrastructure

## Technical Stack

- NUnit 4 test framework
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

## Integration Test Setup

```csharp
internal sealed class MyFeatureIntegrationTest : CliIntegrationFixture
{
    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        // Register custom mocks here
    }
}
```

Mock externals only: Git (LibGit2Sharp), HTTP APIs, filesystem (`MockFileSystem`).

## AutoFixture Attributes

- `[AutoMockData]`: Basic DI with mocks
- `[InlineAutoMockData(params)]`: Parameterized tests
- `[Frozen]` or `[Frozen(Matching.ImplementedInterfaces)]`: Shared mock instances
- `[CustomizeWith(typeof(T))]`: Custom configuration
- `[AutoMockData(typeof(TestClass), nameof(Method))]`: DI container integration

## NSubstitute Patterns

**Arrange (setting up dependencies):**

```csharp
dependency.Method().Returns(value);
dependency.Property.ReturnsNull();
dependency.Method(default!).ReturnsForAnyArgs(value);
dependency.Method().Returns([item1, item2]);
Verify.That<T>(x => x.Property.Should().Be(expected));
```

**Assert on observable outcomes, not mock interactions.** Verify the result, side effect, or state
change rather than asserting a method was called. Tests that assert `Received()` are coupled to
implementation; they break when internals are refactored even if behavior is correct.

```csharp
// Good: assert on the outcome
result.Should().BeEquivalentTo(expected);
fileSystem.AllFiles.Should().Contain(expectedPath);

// Last resort: verify interaction only when there is no observable outcome
mock.ReceivedWithAnyArgs().SetStatus(default, default);
```

If `Received()` feels like the only option, **challenge the design first**. Needing mock
verification often signals a testability problem (void method hiding a meaningful result, missing
return value, side effect with no observable state change). Flag this to the user as a potential
design improvement even if it is outside the current scope of work; do not silently accommodate
untestable designs.

**Argument matching** (returns setup and received verification): Prefer
`ReceivedWithAnyArgs()`/`ReturnsForAnyArgs()` with `default` over `Arg.Any<T>()`:

```csharp
// Good
mock.ReceivedWithAnyArgs().SetStatus(default, default);
mock.Method(default!, default!).ReturnsForAnyArgs(value);

// Bad
mock.Received().SetStatus(Arg.Any<Status>(), Arg.Any<int?>());
mock.Method(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(value);
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

- `dict!["key"]!` - use `ContainKey().WhoseValue` instead
- `HaveCount()` + `BeEquivalentTo()` - redundant; equivalence checks count
- Multiple assertions instead of `.And` chaining

## Utilities

- `IntegrationTestFixture`: Core library integration tests
- `CliIntegrationFixture`: CLI integration with composition root
- `Verify.That<T>()`: NSubstitute matcher with assertions
- `TestableLogger`: Capture log messages
- `NUnitAnsiConsole`: Console output verification
- `MockFileSystem`: Filesystem testing (avoid absolute paths)
- Factory classes: `NewCf`, `NewConfig`, `NewQualitySize`

## Filesystem Paths

Avoid absolute paths in `MockFileSystem` (platform-incompatible):

```csharp
// Good
Fs.CurrentDirectory().SubDirectory("a", "b").File("c.json")

// Bad
"/absolute/path/file.json"
```

## Debugging Test Failures

**Gather evidence before changing code.** Avoid guess-and-check cycles.

1. **Read assertion output carefully** - Diff output often reveals the issue immediately
2. **Add adhoc logs** - Trace execution in tests or production code; remove when done
3. **Compare with passing tests** - Diff similar working tests to spot differences
4. **Add intermediate assertions** - Verify state at each step to pinpoint divergence
5. **Simplify to minimal reproduction** - Strip test down, add back until failure
6. **Write adhoc granular tests** - Isolate suspected areas; remove when done
7. **Check test isolation** - Run alone (`--filter`) vs. suite to detect state leakage

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

## Running Tests

```bash
# Unit and integration tests
dotnet test -v m

# Specific test project
dotnet test -v m tests/Recyclarr.Cli.Tests/

# Single test by name
dotnet test -v m --filter "FullyQualifiedName~TestMethodName"

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

CRITICAL: `--run` must succeed before querying. Investigate failures - coverage data is invalid on
failure. Run coverage BEFORE writing tests to understand gaps.

---

## End-to-End Tests

E2E tests run the full Recyclarr CLI against containerized Sonarr/Radarr instances. Tests verify
that sync operations produce expected state in the services.

### Running E2E Tests

**MANDATORY**: Use `./scripts/Run-E2ETests.ps1` - never run `dotnet test` directly for E2E tests.
The script outputs a log file path; use `rg` to search logs without rerunning tests.

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

- `e2e00000000000000000000000000001` - E2E test Radarr quality profile
- `e2e00000000000000000000000000002` - E2E test Sonarr quality profile
- `e2e00000000000000000000000000003` - E2E test Sonarr guide-only profile
- `e2e00000000000000000000000000010` - E2E test Sonarr CF group
- `e2e00000000000000000000000000011` - E2E test Radarr CF group
- `cf000000000000000000000000000001` through `cf000000000000000000000000000008` - Local test CFs

**Convention**: Local test trash IDs use a `cf` prefix so YAML treats them as strings without
quoting. Never use all-numeric trash IDs in fixtures.

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
