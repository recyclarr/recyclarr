# Sync Event System Redesign

Branch: `partial-sync-status` (this work precedes the partial status phases)

## Problem

Three independent systems handle sync run state with inconsistent patterns:

1. **Progress** (`ProgressSource`): Observable (BehaviorSubject), singleton with ad-hoc subscription
2. **Diagnostics** (`SyncEventStorage`): Imperative list with `Add()`/`Clear()`, singleton
3. **Context** (`SyncContextSource`): Observable (BehaviorSubject), ambient state read back via
   `.Current` for event attribution and Serilog log scoping

This inconsistency creates confusion: some data flows reactively, some is accumulated imperatively,
some is ambient state. Consumers like `NotificationService` must join two separate systems with
different shapes. `SyncEventCollector` reads ambient state from `SyncContextSource` to stamp
instance/pipeline identity onto diagnostics (action-at-a-distance).

## Design: Unified Event-Based System

All sync run state flows through observables. Consumers subscribe and react independently. No
ambient state, no imperative accumulation, no stored transient state.

### Event Types (distinct records, one per observable)

```csharp
public record InstanceEvent(string Name, InstanceStatus Status);
public record PipelineEvent(string Instance, PipelineType Type, PipelineStatus Status, int? Count);
public record DiagnosticEvent(string? Instance, PipelineType? Pipeline, DiagnosticLevel Level, string Message);
```

Each event type gets its own observable on the sync run scope. Consumers subscribe to the ones they
need and use Rx join operators for composition. This is preferred over a polymorphic single-stream
approach because:

- Distinct types give compile-time safety (no `OfType<T>()` runtime filtering)
- Consumers subscribe to exactly what they need
- Rx operators (`CombineLatest`, `WithLatestFrom`, `Merge`) work naturally across observables

### SyncRunScope (the sync run boundary)

```csharp
public interface ISyncRunScope
{
    IObservable<InstanceEvent> Instances { get; }
    IObservable<PipelineEvent> Pipelines { get; }
    IObservable<DiagnosticEvent> Diagnostics { get; }
}
```

Internally backed by Subjects. `Dispose()` calls `OnCompleted()` on all subjects, which is the
lifecycle boundary. This is the key design insight: **sync scope disposal = "sync completed" event**.
All consumers that need end-of-sync behavior (diagnostics rendering, notifications) subscribe to
observables and react to `OnCompleted()` automatically via operators like `ToList()`. No explicit
orchestration from `SyncProcessor`.

### Observable Lifecycle Decisions

- **One set of observables scoped to the sync run.** Publishers (instance/pipeline level) are
  shorter-lived, but the observables they publish into span the whole run. Consumers (renderer,
  notifications, diagnostics) need the full run's data.
- **No per-instance or per-pipeline observable completion needed.** No consumer awaits completion at
  those boundaries. Events carry identity; consumers filter/group as needed. The mismatch between
  short-lived producers and long-lived consumers is the feature, not a problem.
- **Observable completion and Autofac disposal align.** `OnCompleted` is terminal in Rx (no more
  emissions, subscribers can't re-subscribe). Disposal is the natural trigger for completion. They
  correlate because both represent "this scope is done."
- **Autofac does NOT auto-dispose child scopes.** The caller of `BeginLifetimeScope` owns the scope
  and must dispose it explicitly. Components resolved from a scope are disposed when the scope is
  disposed.

### Scoped Publishers (replace ambient state)

Identity is captured once at creation, then producers just emit events without knowing their context.
Same ergonomics as ambient state, but data flows as events instead of being stored.

```csharp
// Created per-instance; captures identity
public class InstancePublisher(string name, SyncRunScope scope)
{
    public void SetStatus(InstanceStatus status) =>
        scope.Publish(new InstanceEvent(name, status));
    public PipelinePublisher ForPipeline(PipelineType type) => new(name, type, scope);
}

// Created per-pipeline; captures instance + pipeline identity
public class PipelinePublisher(string instance, PipelineType pipeline, SyncRunScope scope)
{
    public void SetStatus(PipelineStatus status, int? count = null) =>
        scope.Publish(new PipelineEvent(instance, pipeline, status, count));
    public void AddError(string message) =>
        scope.Publish(new DiagnosticEvent(instance, pipeline, DiagnosticLevel.Error, message));
    public void AddWarning(string message) =>
        scope.Publish(new DiagnosticEvent(instance, pipeline, DiagnosticLevel.Warning, message));
}
```

Publishers are NOT DI-managed. They're runtime objects created by orchestration code and passed as
parameters. `SyncRunScope` is DI-managed (for consumer injection).

### Autofac Named Lifetime Scopes

All registrations centralized in `CompositionRoot` using `InstancePerMatchingLifetimeScope`:

```
Scope hierarchy:
  0. Root (no name) - CompositionRoot, singletons
  1. "sync" - per sync run: SyncRunScope, SyncProcessor, SyncProgressRenderer,
     NotificationService, DiagnosticsRenderer
  2. "instance" - per instance: IServiceConfiguration, InstanceSyncProcessor,
     CompositeSyncPipeline, pipeline phases
```

Benefits:
- Centralized registration (everything in CompositionRoot, not scattered)
- Testing: tests create named scopes and get correct resolution without ad-hoc registrations
- Named scopes enforce resolution correctness (resolving sync-scoped type outside "sync" scope
  throws `DependencyResolutionException` at runtime; caught immediately by tests)

#### Batched Registration Helper (to implement)

Eliminate repetition of scope tags per registration:

```csharp
public static class ContainerBuilderExtensions
{
    public static void RegisterMatchingScope(
        this ContainerBuilder builder,
        object tag,
        Action<ScopedRegistrationBuilder> configure)
    {
        var scoped = new ScopedRegistrationBuilder(builder, tag);
        configure(scoped);
    }
}

public class ScopedRegistrationBuilder(ContainerBuilder builder, object tag)
{
    public IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle>
        RegisterType<T>() where T : notnull
    {
        return builder.RegisterType<T>().InstancePerMatchingLifetimeScope(tag);
    }
}
```

Usage:
```csharp
builder.RegisterMatchingScope("sync", b =>
{
    b.RegisterType<SyncRunScope>().As<ISyncRunScope>();
    b.RegisterType<SyncProcessor>();
    b.RegisterType<SyncProgressRenderer>();
});
```

### Scope Wrapper Pattern (shared factory for scope creation)

Both sync and instance scopes follow the same pattern: create a named child scope, resolve a single
entry-point type, wrap in `IDisposable`. The existing `ConfigurationScopeFactory` /
`ConfigurationScope` pattern generalizes to both levels.

```csharp
// Shared base
public abstract class LifetimeScopeWrapper(ILifetimeScope scope) : IDisposable
{
    protected ILifetimeScope Scope { get; } = scope;
    public void Dispose() => Scope.Dispose();
}

// Sync level: resolves one entry point
internal class SyncScope(ILifetimeScope scope) : LifetimeScopeWrapper(scope)
{
    public SyncProcessor Processor => scope.Resolve<SyncProcessor>();
}

// Instance level: resolves one entry point
internal class InstanceScope(ILifetimeScope scope) : LifetimeScopeWrapper(scope)
{
    public InstanceSyncProcessor InstanceProcessor => scope.Resolve<InstanceSyncProcessor>();
}

// Shared factory
internal class LifetimeScopeFactory(ILifetimeScope scope)
{
    public T Start<T>(object tag, Action<ContainerBuilder>? configure = null)
        where T : LifetimeScopeWrapper
    {
        var childScope = scope.BeginLifetimeScope(tag, c => configure?.Invoke(c));
        return childScope.Resolve<T>();
    }
}
```

The single `Resolve` call per scope wrapper is the only service locator touch point. Everything else
cascades through constructor injection. This is an accepted tradeoff: something must bridge Autofac's
scope model with DI, and these wrappers isolate that to dedicated infrastructure objects with no
business logic.

### Where Sync Scope Is Created

`SyncCommand` creates the sync scope. This keeps business logic in processors, not command classes
(same principle as MVVM/MVC separating logic from views). Config loading also moves into
`SyncProcessor` (it's workflow logic, not command infrastructure).

```
SyncCommand.ExecuteAsync():
  migration.CheckNeededMigrations()
  providerProgressHandler.InitializeProvidersAsync()
  using var syncScope = factory.Start<SyncScope>("sync")
  return await syncScope.Processor.Process(settings, ct)
  // scope disposal -> OnCompleted -> consumers react

SyncProcessor.Process():
  LoadConfigs(settings)
  Pass instance names to renderer
  ProcessConfigs loop (creates instance scopes)
  // No explicit calls to diagnostics/notifications; they react to OnCompleted
```

## Consumer Patterns

### Diagnostics Renderer (reacts to OnCompleted)

No one calls `Report()`. The renderer subscribes in constructor; `ToList()` buffers all diagnostics;
`OnCompleted` triggers rendering automatically.

```csharp
internal class DiagnosticsRenderer(IAnsiConsole console, ISyncRunScope run) : IDisposable
{
    private readonly IDisposable _sub = run.Diagnostics
        .ToList()
        .Subscribe(diagnostics => Render(diagnostics));

    private void Render(IList<DiagnosticEvent> diagnostics) { /* existing rendering logic */ }
    public void Dispose() => _sub.Dispose();
}
```

`SyncEventStorage` goes away entirely. The observable IS the storage.

### Notification Service (same pattern)

Subscribes to multiple observables. `ToList()` on each. Reacts on `OnCompleted`.

### Progress Renderer

- Instance list passed as parameter to `RenderProgressAsync()` (static data, not an event)
- Subscribes to `Instances` and `Pipelines` observables for status updates
- Builds table state from events; render loop polls the state
- Threading consideration: events published from sync thread, render loop on Spectre.Console thread.
  Need synchronization in table state (lock or `ObserveOn`).

```csharp
internal class SyncProgressRenderer(IAnsiConsole console, ISyncRunScope run) : IDisposable
{
    private readonly ProgressTableState _state = new();
    private readonly IDisposable _sub = run.Instances.Subscribe(e => _state.ApplyInstance(e));
    // Also subscribe to Pipelines

    public async Task RenderProgressAsync(
        IReadOnlyList<string> instanceNames,
        Func<Task> action,
        CancellationToken ct)
    {
        _state.Initialize(instanceNames);
        // render loop polls _state.BuildTable()
    }

    public void Dispose() => _sub.Dispose();
}
```

### Log Scoping

Not event-driven. Explicit `using var _ = LogContext.PushProperty(...)` at `InstanceSyncProcessor`
level. Simple, no observables needed.

## What Goes Away

- `SyncContextSource` / `ISyncContextSource` (ambient state for event attribution and log scoping)
- `SyncEventStorage` (imperative list; replaced by observable)
- `SyncEventCollector` (bridge between ambient state and storage; both sides eliminated)
- `ProgressSource` singleton with `BehaviorSubject` (replaced by `SyncRunScope` observables)
- `IProgressSource` interface
- `PipelineProgressWriter` (replaced by `PipelinePublisher`)
- Ad-hoc subscription in `SyncProcessor` for snapshot capture
- `ConfigurationScopeFactory` / `ConfigurationScope` (generalized into `LifetimeScopeFactory` /
  `LifetimeScopeWrapper`)

## What Stays

- `PipelineResult` (Completed/Failed) for dependency cascade in `CompositeSyncPipeline`. This is
  imperative flow control, not event-based. Events report outcomes; they don't control flow.
- `ConfigDeprecationPostProcessor` stays in config loading pipeline as-is. No `IConfigDeprecationCheck`
  implementations exist. When the first one is needed, viable path is moving deprecation checking
  into `SyncProcessor` as an explicit step after config loading (inside sync scope). For now, remove
  `ISyncContextSource` dependency when that type goes away (compile error forces the decision).

## Pipeline Dependency Cascade

Considered using exceptions for cascade flow control (throw on failure, catch and skip dependents).
Rejected: pipeline failure from validation errors is expected behavior, not exceptional. Exceptions
for expected control flow is a .NET anti-pattern. Return value approach (`PipelineResult`) is the
right tool.

## Rx Knowledge Notes

- `OnCompleted` is terminal. No more emissions after it fires. Subscribers don't re-subscribe;
  they get a new observable if needed.
- `ToList()` (Rx operator, not LINQ): returns `IObservable<IList<T>>` that subscribes to source,
  buffers all `OnNext` items, emits the full list as single item when source fires `OnCompleted()`.
  Non-blocking.
- `.ToTask()` subscribes to a single-item observable and returns `Task<T>` that completes when the
  item is emitted.
- `ToList().ToTask()` combined: subscribes eagerly, buffers events, task resolves with full list
  when `OnCompleted` fires.

## Open Topics (still being discussed)

- Notification service consumer flow (same pattern as diagnostics renderer)
- Progress status consumer flow details
- Threading for progress table state
- Exact Rx operator usage in `SyncRunScope` implementation
