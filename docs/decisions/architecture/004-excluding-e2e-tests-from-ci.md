# ADR-004: Excluding E2E Tests from CI Test Runs

- **Status:** accepted
- **Date:** 2026-01-30

## Context and Problem Statement

During the NUnit to TUnit migration, we needed to exclude EndToEndTests from the default `dotnet
test` run in CI. TUnit uses Microsoft.Testing.Platform (MTP) instead of VSTest, which behaves
differently and lacks many traditional filtering mechanisms. Finding a working solution required
extensive experimentation as most conventional approaches failed.

## Decision Drivers

- E2E tests spin up Testcontainers (Sonarr/Radarr) and take 20+ seconds
- Unit tests should run on every CI build; E2E tests run separately
- Must work with Microsoft.Testing.Platform (not VSTest)
- Should maintain parallel test execution for performance
- Minimal maintenance burden for adding new test projects

## Considered Options

1. `dotnet test` with glob pattern (positional arguments)
2. Sequential loop over test projects
3. Parallel loop with background jobs and PID tracking
4. Solution filter (`.slnf`) file
5. Separate `UnitTests.slnx` solution file
6. `<IsTestProject>false</IsTestProject>` in E2E csproj
7. `<IsTestingPlatformApplication>false</IsTestingPlatformApplication>` in E2E csproj
8. Category/trait filtering with `--filter`
9. `--test-modules` glob pattern

## Decision Outcome

Chosen option: "`--test-modules` glob pattern", because it's the only approach that works with
Microsoft.Testing.Platform while maintaining parallel execution and requiring no project file
changes.

```yaml
run: |
  dotnet build -c Release
  dotnet test --test-modules "tests/**/bin/Release/**/Recyclarr.*.Tests.dll"
```

The pattern `Recyclarr.*.Tests.dll` matches unit test assemblies but excludes
`Recyclarr.EndToEndTests.dll`.

### Consequences

- Good, because it uses native MTP functionality without workarounds
- Good, because parallel test execution is preserved
- Good, because no changes to project files are needed
- Good, because adding new `*.Tests` projects automatically includes them
- Bad, because requires separate `dotnet build` step (`-c` flag incompatible with `--test-modules`)
- Bad, because relies on naming convention (projects must follow `*.Tests` pattern)

### IDE Filtering

E2E test classes are annotated with `[Category("E2E")]` for IDE filtering (Rider, VS). This
attribute is not used in CI workflows due to the exit code 8 issue documented above, but allows
developers to exclude E2E tests when running tests locally via the IDE's category filter.

## Failed Approaches (Reference)

Documenting why each alternative failed to prevent future retry attempts:

### Option 1: Glob Pattern as Positional Arguments

```bash
dotnet test -c Release tests/*.Tests
```

**Why it failed:** `dotnet test` does not accept multiple positional path arguments. It expects
either `--project` (single project) or `--solution` (single solution).

### Option 2: Sequential Loop

```bash
for p in tests/*.Tests; do
  dotnet test -c Release --project "$p"
done
```

**Why it failed:** Technically works but runs projects sequentially, losing parallel execution.
Approximately 1 second slower per project. Rejected for performance reasons, though viable as
fallback.

### Option 3: Parallel Loop with PID Tracking

```bash
pids=()
for p in tests/*.Tests; do
  dotnet test --project "$p" &
  pids+=($!)
done
for pid in "${pids[@]}"; do
  wait "$pid" || failed=1
done
```

**Why it failed:** Overly complex shell scripting. Output interleaves making failures hard to debug.
Error handling with PIDs is fragile. Rejected for maintainability.

### Option 4: Solution Filter (`.slnf`)

```json
{
  "solution": { "path": "../Recyclarr.slnx" },
  "projects": ["tests/Recyclarr.Cli.Tests/..."]
}
```

**Why it failed:** Solution filters (`.slnf`) are designed for traditional `.sln` files. They do not
work with the XML-based `.slnx` format. Build fails with "Json in solution filter file is
incorrectly formatted."

### Option 5: Separate `UnitTests.slnx`

Create a dedicated solution containing only unit test projects.

**Why it failed:** Not technically a failure, but rejected due to maintenance burden. Every new test
project requires updating two solution files. The `--test-modules` approach auto-discovers new
projects.

### Option 6: `<IsTestProject>false</IsTestProject>`

```xml
<PropertyGroup>
  <IsTestProject>false</IsTestProject>
</PropertyGroup>
```

**Why it failed:** With Microsoft.Testing.Platform, this property alone is insufficient. MTP checks
both `IsTestProject` AND `IsTestingPlatformApplication`. TUnit sets
`IsTestingPlatformApplication=true`, so the project is still discovered even with
`IsTestProject=false`.

### Option 7: `<IsTestingPlatformApplication>false</IsTestingPlatformApplication>`

```xml
<PropertyGroup>
  <IsTestProject>false</IsTestProject>
  <IsTestingPlatformApplication>false</IsTestingPlatformApplication>
</PropertyGroup>
```

**Why it failed:** Setting `IsTestingPlatformApplication=false` causes TUnit to not generate the
implicit `Main` method, resulting in compilation failure: "Program does not contain a static 'Main'
method suitable for an entry point."

### Option 8: Category/Trait Filtering

```bash
dotnet test --filter "Category!=E2E"
```

**Why it failed:** The filter correctly excludes E2E tests, but Microsoft.Testing.Platform returns
exit code 8 ("zero tests ran") when a test project has no matching tests. The E2E project contains
only E2E tests, so filtering them all out triggers this error. There is no way to suppress exit code
8 at the solution level while still failing on actual test failures.

### Option 9 (Chosen): `--test-modules` Glob Pattern

```bash
dotnet test --test-modules "tests/**/bin/Release/**/Recyclarr.*.Tests.dll"
```

**Why it works:** The `--test-modules` flag accepts glob patterns to select which test assemblies to
run. By targeting `*.Tests.dll`, we include unit test projects while excluding `EndToEndTests.dll`.
This is native MTP functionality with full parallel execution support.

**Caveats discovered:**

- Cannot combine with `-c`/`--configuration` flag (must build separately)
- Pattern must include `bin/` to avoid matching assemblies in `obj/` directories
- Pattern should be scoped to `tests/` to avoid matching unrelated DLLs
