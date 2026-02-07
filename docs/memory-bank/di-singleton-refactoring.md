# DI Singleton Refactoring

Branch: `tunit-migration`

## Goal

Eliminate unnecessary `SingleInstance()` registrations to enable a future shared-root DI container
pattern for TUnit test isolation. Currently, `CoreDataSourceAttribute` builds a new Autofac
container per test. The idiomatic TUnit pattern is a static shared root with
`BeginLifetimeScope()` per test.

## Completed

### 1. Removed SingleInstance from stateless services

Commit: `ab7daa7a` refactor(config): remove singleton registrations from stateless services

Removed `SingleInstance()` from 11 services that had no mutable state:
- `ProviderInitializationFactory`, `JsonResourceLoader`
- 8 `*ResourceQuery` classes
- `NotificationService`

### 2. Added JSON resource caching

Commit: `139da960` perf: cache JSON resources to eliminate redundant disk I/O

`JsonResourceLoader` now uses `ConcurrentDictionary` to cache deserialized results by file path and
type. Registered as `InstancePerLifetimeScope()` so cache persists within a scope but isolates
between tests. Fixes `QualityProfileResourceQuery.Get()` being called twice per sync (via
`ConfiguredCustomFormatProvider` and `QualityProfilePlanComponent`).

### 3. Eliminated ProgressFactory singleton

Commit: `acffda38` refactor(cli): eliminate ProgressFactory singleton by inlining into
ProviderProgressHandler

`ProgressFactory` was a mutable singleton only because `ConsoleSetupTask` set `UseSilentFallback`
after construction. Replaced with a `bool silent` parameter on
`ProviderProgressHandler.InitializeProvidersAsync()`. List commands pass `settings.Raw`; non-list
commands pass `false`.

### 4. Made DefaultAppDataSetup stateless

Commit: `be2d3992` refactor(config): make DefaultAppDataSetup stateless

`DefaultAppDataSetup` had mutable directory override methods used only by test infrastructure.
Removed `IAppDataSetup` interface, override methods, and mutable fields. Now an internal stateless
factory instantiated inline in the DI lambda. Tests register `IAppPaths` directly. Directory
initialization consolidated into `AppPaths.Initialize()`.

### 5. Collapsed LoggerFactory into ReloadableLogger

Replaced `LoggerFactory` + `IndirectLoggerDecorator` (two classes) with a single `ReloadableLogger`
that implements `ILogger` directly. The bootstrap logger is built at construction; `Reload()` swaps
the inner logger after CLI args are parsed via `LoggerSetupTask`. Uses `volatile` for thread-safe
reference swap.

Reclassified as a legitimate singleton: Spectre.Console parses args after `ITypeRegistrar.Build()`,
so two-phase logger initialization is an architectural constraint, not a design smell.
`Serilog.Extensions.Hosting.ReloadableLogger` was considered but requires pulling in the hosting
package (inappropriate for a standalone CLI using Autofac).

## Remaining Work

### Remaining legitimate singletons (no action needed)

These have genuinely shared mutable state required in production:

| Service | Reason |
|---|---|
| `ReloadableLogger` | Two-phase init required by Spectre.Console lifecycle |
| `LoggingLevelSwitch` | Serilog primitive, mutable by design |
| `SyncEventStorage` | Shared diagnostics accumulator |
| `SyncContextSource` | Observable sync context tracker |
| `ProgressSource` | Observable progress tracker |
| `SyncEventCollector` | Subscribes to context, writes to storage |
| `ResourceRegistry<>` | Populated at init, read by all queries |

These are fine as singletons and are safe for Lazy/cached patterns:

| Service | Reason |
|---|---|
| `SecretsProvider` | Lazy, immutable after load |
| `SettingsProvider` | Lazy, immutable after load |
| `FlurlClientCache` | HTTP connection pooling |

### Future: CoreDataSourceAttribute refactoring

Once all unnecessary singletons are removed, `CoreDataSourceAttribute.CreateScope()` can be
refactored from building a new container per test to using a shared static root with
`BeginLifetimeScope()`. The remaining legitimate singletons would need to be overridden in child
scopes for test isolation (using Autofac's `BeginLifetimeScope(builder => ...)` pattern).

## Key Learnings

- In production, Recyclarr is a CLI that builds one container, does work, and exits.
  `SingleInstance()` vs `InstancePerLifetimeScope()` is semantically identical in production (no
  child scopes for most services). The distinction only matters for test isolation.
- `InstancePerLifetimeScope()` is the correct Autofac lifetime for per-test isolation with a shared
  root container.
- Child scopes created via `ConfigurationScopeFactory` and `AutofacTypeRegistrar` in production mean
  truly shared mutable state must remain `SingleInstance()`.
- TUnit's `DependencyInjectionDataSourceAttribute<TScope>.CreateScope()` is called once per test at
  execution time (not discovery), and the scope is disposed via `OnDispose`.
