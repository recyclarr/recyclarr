# TUnit Migration Notes

Temporary notes for NUnit to TUnit migration. Delete after migration complete.

## Current Status (Updated)

**Build Status:** All errors resolved. Ready for testing.

**Completed:**
- [x] Removed all NUnit packages from Directory.Build.props and Directory.Packages.props
- [x] Removed AutoFixture.NUnit4 package and global using
- [x] Created custom `FrozenAttribute` with `Matching` enum (ExactType, ImplementedInterfaces)
- [x] Updated `AutoMockDataAttribute` to process `[Frozen]` attributes
- [x] Deleted unused `InlineAutoMockDataAttribute`
- [x] Updated `TestableLogger` to use `Console.Out` (TUnit auto-captures)
- [x] Created `TUnitAnsiConsole` and `TUnitAnsiConsoleOutput` using `Console.Out`
- [x] Deleted old `NUnitAnsiConsole` and `NUnitAnsiConsoleOutput`
- [x] Updated `IntegrationTestFixture`: `[SetUp]` → `[Before(Test)]`
- [x] Created base `CustomizeAttribute` class for AutoFixture
- [x] Updated `CustomizeWithAttribute` to use our `CustomizeAttribute`
- [x] Migrated E2E tests (`RecyclarrSyncTests.cs`) to TUnit syntax
- [x] Added TUnit package to E2E tests project
- [x] Converted `CompositionRootTest` to use `[MethodDataSource(nameof(...))]`

## Key TUnit Idioms

**Console Output:** TUnit automatically intercepts `Console.WriteLine()` and associates it with the current test. No custom TextWriter needed!

**Logging:** Use `Console.Out` for Serilog's `WriteTo.TextWriter()` - TUnit captures it automatically.

**TestContext Access:**
- Inside test methods: Inject `TestContext context` as parameter
- In static methods: `TestContext.Current?.Metadata.TestDetails` (note the `.Metadata` in the path)
- Test name: `TestContext.Current?.Metadata.TestDetails.TestName`

## E2E Test Migration Applied

| NUnit | TUnit | Notes |
|-------|-------|-------|
| `[TestFixture]` | (removed) | Not needed in TUnit |
| `[Explicit]` | `[Explicit]` | Same |
| `[NonParallelizable]` | `[NotInParallel]` | |
| `[OneTimeSetUp]` | `[Before(HookType.Class)]` | Must be static |
| `[OneTimeTearDown]` | `[After(HookType.Class)]` | Must be static |
| `[Test]` | `[Test]` | Same |
| `[Order(n)]` | `[NotInParallel(Order = n)]` | Ordering via NotInParallel |
| `[CancelAfter(ms)]` | `[Timeout(ms)]` | |
| `TestContext.CurrentContext.CancellationToken` | `CancellationToken` param | Add as method parameter |
| `TestContext.CurrentContext.TestDirectory` | `AppDomain.CurrentDomain.BaseDirectory` | |

## Files Modified

### New Files Created
- `tests/Recyclarr.TestLibrary/AutoFixture/Matching.cs`
- `tests/Recyclarr.TestLibrary/AutoFixture/FrozenAttribute.cs`
- `tests/Recyclarr.TestLibrary/AutoFixture/CustomizeAttribute.cs`
- `tests/Recyclarr.Core.TestLibrary/TestAnsiConsole.cs`
- `tests/Recyclarr.Core.TestLibrary/TestAnsiConsoleOutput.cs`

### Files Modified
- `tests/Directory.Build.props` - Removed NUnit packages and usings
- `Directory.Packages.props` - Removed AutoFixture.NUnit4
- `tests/Recyclarr.Core.TestLibrary/Recyclarr.Core.TestLibrary.csproj` - Removed NUnit
- `tests/Recyclarr.EndToEndTests/Recyclarr.EndToEndTests.csproj` - Added TUnit, OutputType=Exe
- `tests/Recyclarr.TestLibrary/AutoFixture/AutoMockDataAttribute.cs` - Added [Frozen] support
- `tests/Recyclarr.TestLibrary/AutoFixture/CustomizeWithAttribute.cs` - Uses our CustomizeAttribute
- `tests/Recyclarr.TestLibrary/TestableLogger.cs` - Uses Console.Out
- `tests/Recyclarr.Core.TestLibrary/IntegrationTestFixture.cs` - [SetUp] → [Before(Test)]
- `tests/Recyclarr.Core.TestLibrary/CoreDataSourceAttribute.cs` - NUnitAnsiConsole → TUnitAnsiConsole
- `tests/Recyclarr.EndToEndTests/RecyclarrSyncTests.cs` - Full TUnit migration
- `tests/Recyclarr.Cli.Tests/IntegrationTests/CompositionRootTest.cs` - MethodDataSource pattern

### Files Deleted
- `tests/Recyclarr.TestLibrary/AutoFixture/InlineAutoMockDataAttribute.cs`
- `tests/Recyclarr.Core.TestLibrary/NUnitAnsiConsole.cs`
- `tests/Recyclarr.Core.TestLibrary/NUnitAnsiConsoleOutput.cs`

## Next Steps

1. Build and verify no errors
2. Run tests to validate migration
3. **Known issue:** Rider "Run All Tests" includes E2E tests (marked `[Explicit]`). TUnit's Explicit works differently with Rider's test runner. Workaround: use `.runsettings` file with `TestCaseFilter` or configure Rider's default test filter.

## AutoFixture.TUnit Note

An official `AutoFixture.TUnit` package exists at github.com/AutoFixture/AutoFixture.TUnit but is not yet published to NuGet. We implemented minimal custom `[Frozen]` support instead.
