# Unit Test Debugging Session - Brain Dump

## Session Context

**Initial Problem**: 6 failing unit tests in Recyclarr codebase
**Branch**: `data-source-settings`  
**Date**: 2025-08-31
**Status**: 3 tests fixed, 3 remaining (significant progress made)

## Root Cause Analysis - Deep Understanding Gained

### Issue #1: TrashRepository Collection Migration ✅ FIXED
**Original Error**: `ISettings<TrashRepository>` not registered in DI container
**User Correction**: "The problem isn't that settings aren't available, it's that it can't be registered due to it being a collection. We no longer have just a single trash repo anymore."

**Key System Design Insight**:
- **Legacy Pattern**: Single `TrashRepository` accessed via `ISettings<TrashRepository>`
- **New Pattern**: Collection of providers via `ResourceProviderSettings.TrashGuides` (`IReadOnlyCollection<IUnderlyingResourceProvider>`)
- **Migration**: `RecyclarrSettings.Repositories` marked as "Replaced by ResourceProviders"

**Fix Applied**: Updated `ServiceCompatibilityIntegrationTest.Load_settings_yml_correctly_when_file_exists()` to:
```csharp
// Changed from:
var settings = Resolve<ISettings<TrashRepository>>();
settings.Value.CloneUrl.Should().Be("http://the_url.com");

// To:
var settings = Resolve<ISettings<ResourceProviderSettings>>();
var trashGuides = settings.Value.TrashGuides.First() as GitRepositorySource;
trashGuides?.CloneUrl.Should().Be(new Uri("http://the_url.com"));
```

### Issue #2: Template Creation System ✅ FIXED  
**Original Error**: Template files not being copied to expected locations
**User Correction**: Questioned assumption about mocking `GetTemplates()` method

**Key System Design Insight**:
- `ConfigTemplatesGitRepository` reads templates from ResourceProviders configuration, not hardcoded paths
- Expected flow: ResourceProviders settings → Repository processing → Template discovery → File copying
- Test was creating files at `/repos/config-templates-default/` but implementation expects `{ReposDirectory}/config-templates/{name}/`

**Fix Applied**: Updated both template tests to:
1. Configure ResourceProviders settings with ConfigTemplates repository
2. Place mock files in correct `{ReposDirectory}/config-templates/default/` location
3. Removed unnecessary `.FullName` calls as per user feedback

### Issue #3: App Data Directory Timing/DI Issue ✅ FIXED
**Original Error**: `NoHomeDirectoryException` in CLI integration tests
**User Correction**: "NUnit SetUp attribute should be inherited - investigate why it isn't working"

**Critical System Understanding Discovered**:
According to NUnit documentation: "The SetUp attribute is inherited from any base class. Therefore, if a base class has defined a SetUp method, that method will be called before each test method"

**The Real Root Cause Found Through Investigation**:
1. **NUnit DOES call** `IntegrationTestFixture.Setup()` which sets app data override to `/test/recyclarr`
2. **CLI command execution** goes through `AppDataDirSetupTask.OnStart(BaseCommandSettings cmd)`  
3. **`AppDataDirSetupTask`** calls `appDataSetup.SetAppDataDirectoryOverride(cmd.AppData ?? "")`
4. **`cmd.AppData` is null** (no `--app-data` provided), so it passes empty string `""`
5. **Empty string overwrites** the test's directory override
6. **Later resolution fails** when trying to use environment/default paths

**Historical Context from Git History (commit c9c7c052)**:
- Original issue #284: `--app-data` option wasn't working
- Changed from nullable property to method-based override
- Added `ArgumentNullException.ThrowIfNull(_appDataDirectoryOverride)` as safeguard against timing issues
- **Critical Timing Issue**: Services like `ILogger` → `LoggerFactory` → `FileLogSinkConfigurator(IAppPaths)` → `CreateAppPaths()` can trigger early DI resolution before app data override is set

**Fix Applied**: Modified `AppDataDirSetupTask.OnStart()`:
```csharp
// Changed from:
appDataSetup.SetAppDataDirectoryOverride(cmd.AppData ?? "");

// To:
if (cmd.AppData is not null)
{
    appDataSetup.SetAppDataDirectoryOverride(cmd.AppData);
}
```

**Design Intent Preserved**: 
- Safeguards remain for timing/DI dependency issues
- `--app-data` CLI option still works when explicitly provided
- Integration tests can pre-set overrides without interference

## Current Status - 2025-08-31 Session End

### ✅ Tests Fixed (3/6)
1. `ServiceCompatibilityIntegrationTest.Load_settings_yml_correctly_when_file_exists` - ResourceProviders migration
2. `ConfigCreationProcessorIntegrationTest.Template_id_matching_works` - ResourceProviders config + path fix
3. `TemplateConfigCreatorIntegrationTest.Template_id_matching_works` - ResourceProviders config + path fix

### ❌ Tests Still Failing (3/6) - ROOT CAUSE IDENTIFIED
1. `CliCommandIntegrationTest.List_custom_format_radarr_score_sets` - Missing metadata.json file
2. `CliCommandIntegrationTest.List_custom_format_sonarr_score_sets` - Missing metadata.json file  
3. `CliCommandIntegrationTest.List_naming_sonarr` - Empty console output (needs ResourceProviders config)

**Current Test Output:**
```
Failed!  - Failed: 3, Passed: 294, Skipped: 0, Total: 297, Duration: 291 ms

Failed List_custom_format_sonarr_score_sets [149 ms]
Error: System.IO.FileNotFoundException: Could not find file '/test/recyclarr/repositories/trash-guides/default/metadata.json'
```

### 🔧 Fixes In Progress
**CustomFormatCategoryParser NullReferenceException**: Added null check in `Parse()` method to handle `null!` parameter from `CustomFormatsResourceQuery` (recent WIP change in commit 060b3003).

**List Command Tests**: Added ResourceProviders settings and updated file paths from:
- `Fs.CurrentDirectory().SubDirectory("repositories")` → `Paths.ReposDirectory`  
- Need to apply same fix to `List_naming_sonarr` test

## Key Architecture Insights Gained

### ResourceProviders Migration Pattern
- **Old**: Direct repository configuration in settings
- **New**: Flexible provider-based system supporting multiple repositories
- **Impact**: All tests using old patterns need ResourceProviders configuration

### DI Container Composition Differences  
- **Core Tests**: Use `CoreAutofacModule` only (limited scope)
- **CLI Tests**: Use `CompositionRoot.Setup()` (full application composition)
- **Rationale**: CLI tests need full app context, Core tests need isolation

### Timing/Dependency Chain Critical Path
```
CLI Command Start
→ CommandSetupInterceptor.Intercept()  
→ AppDataDirSetupTask.OnStart() [MUST BE FIRST]
→ Other setup tasks (LoggerSetupTask, etc.)  
→ ILogger resolution can trigger:
  → LoggerFactory → FileLogSinkConfigurator(IAppPaths) → CreateAppPaths()
  → REQUIRES: _appDataDirectoryOverride must be set BEFORE this chain
```

## Remaining Work

### Immediate Next Steps
1. **Fix `List_naming_sonarr` test**: Apply same ResourceProviders + path fix
2. **Test all fixes**: Run `dotnet test` to verify 0 failures
3. **Code quality**: Run `dotnet csharpier .` and `dotnet format` per CLAUDE.md requirements

### Test Fixes Still Needed
```csharp
// For List_naming_sonarr test - add ResourceProviders configuration:
const string settingsYaml = """
    resource_providers:
      trash_guides:
        - name: default
          clone_url: http://test.com
    """;
Fs.AddFile(Paths.AppDataDirectory.File("settings.yml"), new MockFileData(settingsYaml));

// And update path from:
var reposDir = Fs.CurrentDirectory().SubDirectory("repositories")...
// To:  
var reposDir = Paths.ReposDirectory.SubDirectory("trash-guides")...
```

## Technical Decisions Made

### Preserve vs Refactor Approach
**Decision**: Preserve existing timing safeguards rather than simplify
**Rationale**: The `ArgumentNullException.ThrowIfNull(_appDataDirectoryOverride)` safeguard catches real DI resolution timing issues that could cause subtle bugs

### Test Migration Strategy  
**Decision**: Update tests to use new ResourceProviders pattern rather than mock/stub the old system
**Rationale**: Tests should verify real behavior, and the old pattern is being phased out

### Null Handling in CustomFormatCategoryParser
**Decision**: Add defensive null check rather than require non-null parameter
**Rationale**: The `null!` is intentionally passed from `CustomFormatsResourceQuery` due to incomplete category parsing implementation (TODO comment in code)

## Files Modified

### Production Code
- `src/Recyclarr.Cli/Console/Setup/AppDataDirSetupTask.cs` - Fixed app data override logic
- `src/Recyclarr.Core/TrashGuide/CustomFormat/CustomFormatCategoryParser.cs` - Added null safety

### Test Code  
- `tests/Recyclarr.Cli.Tests/IntegrationTests/ServiceCompatibilityIntegrationTest.cs` - ResourceProviders migration
- `tests/Recyclarr.Cli.Tests/IntegrationTests/ConfigCreationProcessorIntegrationTest.cs` - ResourceProviders config + paths
- `tests/Recyclarr.Cli.Tests/IntegrationTests/TemplateConfigCreatorIntegrationTest.cs` - ResourceProviders config + paths  
- `tests/Recyclarr.Cli.Tests/IntegrationTests/CliCommandIntegrationTest.cs` - Partial fixes for list commands

## System Architecture Understanding

### ResourceProviders vs Legacy Repositories
The system migrated from single-repository configuration to a flexible provider-based system:

**Legacy (`Repositories`)**:
```yaml
repositories:
  trash_guides:
    clone_url: http://example.com
    branch: master
```

**New (`ResourceProviders`)**:
```yaml  
resource_providers:
  trash_guides:
    - name: default
      clone_url: http://example.com
      reference: master
  config_templates:
    - name: default
      clone_url: http://example.com
```

### Test Infrastructure Patterns
- **CLI Integration Tests**: Use full `CompositionRoot.Setup()` for real app behavior
- **Core Integration Tests**: Use `CoreAutofacModule` only for isolated testing
- **Mock Strategy**: Mock externals (Git, HTTP, FileSystem) but use real business logic

### Critical Dependencies Chain
Understanding the app data directory dependency chain was crucial:
`CLI Commands → Setup Tasks → Logger → FileLogSinkConfigurator → IAppPaths → CreateAppPaths() → App Data Override`

This chain explains why the safeguards exist and why the timing is critical.

## Lessons Learned

1. **Don't assume test failures indicate missing registrations** - could be architectural migrations
2. **Investigate user corrections deeply** - they often reveal fundamental misunderstandings  
3. **Git history provides crucial context** - especially for understanding timing/DI issues
4. **NUnit documentation is authoritative** - but implementation details matter for understanding failures
5. **Integration tests reveal real system behavior** - when they fail, it's often due to system changes, not test issues

## CRITICAL DISCOVERY - Missing Default Repository Configuration

### Root Cause Analysis - Session 2025-08-31

**MAJOR ARCHITECTURAL ISSUE DISCOVERED**: The ResourceProviders migration **lost default repository behavior**

#### Old System Had Built-In Defaults
- `TrashRepository` default: `https://github.com/TRaSH-Guides/Guides.git` (Repositories.cs:13)
- `ConfigTemplateRepository` default: `https://github.com/recyclarr/config-templates.git` (Repositories.cs:21)
- These were **automatically available** without any configuration

#### New System Lost Defaults  
- `ResourceProviderSettings.TrashGuides` defaults to **empty collection** `[]`
- `ResourceProviderSettings.ConfigTemplates` defaults to **empty collection** `[]`
- **No official repositories are automatically configured**

#### Impact on Test Failures
```csharp
// TrashGuidesGitRepository.ProcessAllRepositoriesAsync() line 36:
foreach (var gitRepo in settings.Value.TrashGuides.OfType<GitRepositorySource>())

// If settings.Value.TrashGuides is empty:
// → No repositories processed
// → No metadata.json files created  
// → FileNotFoundException when trying to access metadata.json
```

### Required Default Behavior (Per User Clarification)

**Always-Include Pattern:**
1. **Official repos ALWAYS added first** - regardless of user config
2. **User repos merged second** - can override defaults by name
3. **Duplicate names = error** - prevent configuration conflicts

**Example Merge Logic:**
```csharp
// 1. Start with official defaults
var allRepos = new List<GitRepositorySource> { OfficialTrashGuides };

// 2. Merge user repos (name-based override)
foreach (var userRepo in settings.Value.TrashGuides.OfType<GitRepositorySource>())
{
    // If user specified "official", replace the default
    allRepos.RemoveAll(r => r.Name == userRepo.Name);
    allRepos.Add(userRepo);
}

// 3. Detect user-level duplicates (error condition)
var duplicates = allRepos.GroupBy(r => r.Name).Where(g => g.Count() > 1);
if (duplicates.Any()) throw new InvalidOperationException("Duplicate names");
```

### Files Requiring Default Configuration Logic
1. `src/Recyclarr.Core/TrashGuide/TrashGuidesGitRepository.cs` - Add official TRaSH guides default
2. `src/Recyclarr.Core/ConfigTemplates/ConfigTemplatesGitRepository.cs` - Add official config templates default

### Files Requiring Legacy Cleanup
1. `src/Recyclarr.Core/Settings/Models/Repositories.cs` - Remove hardcoded defaults from old system

## Next Session Pickup Points

**CRITICAL**: Fix missing default repository configuration FIRST, then remaining tests should pass automatically

1. **Implement official repository defaults** in Git repository classes (always-include pattern)
2. **Remove old repository defaults** from deprecated Repositories.cs classes  
3. **Remove explicit ResourceProviders configuration from tests** (should work with defaults)
4. **Verify all tests pass** with `dotnet test` (should be 0 failures after defaults implemented)
5. **Run code quality checks**: `dotnet csharpier .` and `dotnet format --verify-no-changes`

**Key Insight**: The test failures aren't really about missing test setup - they reveal that the ResourceProviders migration is **incomplete** because it lost the default repository behavior that users depend on.