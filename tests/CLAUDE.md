# Testing Patterns and Infrastructure

Detailed patterns for Recyclarr tests. See root `CLAUDE.md` for philosophy and mandates.

## Test Pyramid

- **E2E**: Critical user workflows against real services (Testcontainers)
- **Integration**: Complete workflows with mocked externals (Git, HTTP, filesystem)
- **Unit**: Edge cases integration cannot reach

## Directory Structure

```txt
tests/
├── Recyclarr.EndToEndTests/
├── Recyclarr.Core.Tests/IntegrationTests/
├── Recyclarr.Cli.Tests/IntegrationTests/
├── Recyclarr.TestLibrary/
└── Recyclarr.Core.TestLibrary/
```

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

## E2E Infrastructure

- Testcontainers for Sonarr/Radarr
- Isolated temp directories per run
- Test idempotency via clean state
- Run: `./scripts/Run-E2ETests.ps1` (outputs log file path)

## Debugging Test Failures

**Gather evidence before changing code.** Avoid guess-and-check cycles.

1. **Read assertion output carefully** - Diff output often reveals the issue immediately
2. **Add adhoc logs** - Trace execution in tests or production code; remove when done
3. **Compare with passing tests** - Diff similar working tests to spot differences
4. **Add intermediate assertions** - Verify state at each step to pinpoint divergence
5. **Simplify to minimal reproduction** - Strip test down, add back until failure
6. **Write adhoc granular tests** - Isolate suspected areas; remove when done
7. **Check test isolation** - Run alone (`--filter`) vs. suite to detect state leakage

## Anti-Patterns

- Over-mocking or mocking business logic
- Tests coupled to implementation details
- Duplicate coverage for same logical paths
- Production code added solely for testing
- Unexplained magic constants
