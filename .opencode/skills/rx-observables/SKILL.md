---
name: rx-observables
description: >-
  Use when writing or modifying code that uses System.Reactive (Rx.NET),
  IObservable<T>, subjects, or reactive/observable patterns in C#
---

# Rx.NET Patterns and Practices

Patterns, anti-patterns, and decision guidance for System.Reactive (Rx.NET) in C#. Covers interface
design, subject usage, subscription lifecycle, error handling, and testing.

## Exposing Observables

**Expose `IObservable<T>` as a property; never inherit from it.**

Classes should not implement `IObservable<T>` directly. Expose a property instead, enabling
consumers to compose operators before subscribing.

```csharp
public class SensorReadings
{
    private readonly Subject<double> _readings = new();

    // Wrap with AsObservable() to prevent downcasting to Subject
    public IObservable<double> Readings => _readings.AsObservable();

    internal void Report(double value) => _readings.OnNext(value);
}
```

Key rules:

- Always call `AsObservable()` on subjects before exposing them. This prevents consumers from
  calling `OnNext`/`OnError`/`OnCompleted`.
- Never return null from a method or property typed `IObservable<T>`. Return `Observable.Empty<T>()`
  or `Observable.Never<T>()` instead.
- Avoid implementing `IObservable<T>` or `IObserver<T>` yourself. Use `Observable.Create` or
  subjects.

## Subject Selection

Subjects are "mutable variables of the Rx world" (Erik Meijer). Use them only to generate a hot
observable imperatively from a local source with no direct external observable to adapt.

### Decision: Subject vs Observable.Create

Use `Observable.Create` when:

- Wrapping an external async/callback/event source into an observable
- Each subscriber needs independent state (cold observable)
- Cleanup logic ties naturally to the subscription's `IDisposable`

Use a `Subject` when:

- You need to imperatively push values from code you control (local source)
- Multiple subscribers must share the same live stream (hot observable)
- Modeling property-change or event-like notifications

If the source is an existing observable and you want hot behavior, use `Publish`/`RefCount` instead
of piping through a subject.

### Which Subject Type

| Type                 | Replays  | Seed | Use when                                            |
|----------------------|----------|------|-----------------------------------------------------|
| `Subject<T>`         | None     | No   | Fire-and-forget events; no history needed           |
| `BehaviorSubject<T>` | Last (1) | Yes  | Property-change semantics; always has current value |
| `ReplaySubject<T>`   | N or all | No   | Late subscribers need historical values             |
| `AsyncSubject<T>`    | Last     | No   | Single async result (like `Task<T>`)                |

`BehaviorSubject<T>` requires a seed value and immediately pushes it to new subscribers. Prefer it
for "current state" scenarios.

`ReplaySubject<T>` can replay a bounded window (`new ReplaySubject<T>(bufferSize)`) or a time
window. Always bound the buffer to avoid unbounded memory growth.

### Subjects Must Be Private

Subjects are implementation details. Expose only `IObservable<T>` publicly:

```csharp
private readonly BehaviorSubject<int> _count = new(0);
public IObservable<int> Count => _count.AsObservable();
```

## Imperative Read-back Is a Design Smell

Exposing `.Value` / `.Current` from a `BehaviorSubject<T>` publicly introduces imperatively-accessed
shared state, undermining the reactive model. Ben Lesh (RxJS lead): "using `getValue()` is a huge
code smell... you're doing something imperative in declarative paradigm."

**Acceptable**: Internal read-modify-write within the owning class (e.g.
`_subject.OnNext(_subject.Value + 1)`).

**Avoid**: Public `.Value` properties that let consumers pull state instead of subscribing.

Alternatives:

- Pass data as parameters to consumers instead of letting them read state.
- Use `Scan` within the observable chain for stateful accumulation.
- Use an OAPH-like pattern (subscribe internally, cache latest, expose read-only) when a synchronous
  read is genuinely necessary.

## Hot vs Cold Observables

**Cold**: Each `Subscribe` triggers independent execution. Factory methods like `Observable.Create`,
`Observable.Defer`, `Observable.Timer` produce cold observables.

**Hot**: All subscribers share a single underlying source. Subjects are inherently hot. Use
`Publish`/`RefCount` to share a cold source:

```csharp
IObservable<long> shared = Observable
    .Interval(TimeSpan.FromSeconds(1))
    .Publish()
    .RefCount();
```

`Publish().RefCount()` connects on first subscriber and disconnects when the last unsubscribes. Use
`Publish().AutoConnect(n)` if you need to wait for `n` subscribers before connecting.

## Subscription Lifecycle

Every `Subscribe` returns an `IDisposable`. Failing to dispose leaks subscriptions and can leak the
subscriber (prevented from GC by the observable's reference to it).

### Disposable Types

| Type                           | Behavior                                                   |
|--------------------------------|------------------------------------------------------------|
| `CompositeDisposable`          | Groups multiple disposables; disposes all together.        |
| `SerialDisposable`             | Holds one at a time; setting a new one disposes previous.  |
| `SingleAssignmentDisposable`   | Assigned once; throws on second assignment.                |
| `MultipleAssignmentDisposable` | Like Serial but does NOT dispose previous on reassignment. |
| `CancellationDisposable`       | Bridges `CancellationTokenSource` to `IDisposable`.        |
| `RefCountDisposable`           | Prevents disposal until all dependents dispose.            |
| `BooleanDisposable`            | Tracks `IsDisposed` state.                                 |
| `Disposable.Empty`             | No-op; useful as default or from `Observable.Create`.      |
| `Disposable.Create(action)`    | Runs `action` on first `Dispose` call (idempotent).        |

### Patterns

Collect subscriptions with `CompositeDisposable` and dispose in one call:

```csharp
private readonly CompositeDisposable _subscriptions = new();

public void Initialize()
{
    source1.Subscribe(HandleItem1).DisposeWith(_subscriptions);
    source2.Subscribe(HandleItem2).DisposeWith(_subscriptions);
}

public void Dispose() => _subscriptions.Dispose();
```

`DisposeWith` is in the `System.Reactive.Disposables.Fluent` namespace (available since Rx.NET 6.1).

Use `SerialDisposable` when replacing one subscription with another:

```csharp
private readonly SerialDisposable _current = new();

public void SwitchSource(IObservable<int> newSource)
{
    // Disposes previous subscription automatically
    _current.Disposable = newSource.Subscribe(Handle);
}
```

Only dispose subscriptions early if you need to unsubscribe before the observable completes. Finite
sequences (`Observable.Return`, `.Take`, etc.) clean up on completion.

## Error Handling

An `OnError` notification terminates the sequence. Subscribers that don't provide an `OnError`
handler will have the exception thrown on the calling thread.

**Always provide an `OnError` handler in `Subscribe`.**

### Operators

| Operator                              | Behavior                                              |
|---------------------------------------|-------------------------------------------------------|
| `Catch<TSource, TException>(handler)` | Catches typed exception; returns fallback observable. |
| `Catch(fallback1, fallback2, ...)`    | On error, moves to next sequence.                     |
| `Retry()` / `Retry(count)`            | Resubscribes on error. Bound the count.               |
| `OnErrorResumeNext(next)`             | Continues with next on error OR completion.           |
| `Finally(action)`                     | Runs on completion, error, or disposal.               |

Prefer typed `Catch<TSource, TException>` over untyped variants to avoid swallowing unexpected
exceptions:

```csharp
IObservable<string> resilient = source
    .Catch<string, TimeoutException>(_ => Observable.Empty<string>());
```

**Avoid**: Putting side effects in `Select`/`Where` callbacks. Use `Do` for explicit side effects
before operators that may error.

## Thread Safety

The Rx contract requires that `OnNext`, `OnError`, and `OnCompleted` calls are serialized
(non-concurrent, non-overlapping). Subjects do NOT enforce this internally. Concurrent `OnNext`
calls can produce out-of-order delivery or corrupt operator state.

If multiple threads may call `OnNext`:

```csharp
// Wrap with Synchronize to enforce sequential delivery
var safe = subject.Synchronize();
```

Or use `Observable.Create` which naturally scopes to a single subscriber.

**Never call `OnNext` concurrently on a subject.** This violates the Rx contract and causes subtle
threading bugs in downstream operators.

`ObserveOn(scheduler)` and `SubscribeOn(scheduler)` control which thread notifications are delivered
on. Place `ObserveOn`/`SubscribeOn` immediately before `Subscribe`, not buried mid-chain.

## Testing

Use `Microsoft.Reactive.Testing` NuGet package for `TestScheduler`.

### TestScheduler

`TestScheduler` virtualizes time, allowing time-dependent tests to run in microseconds instead of
real time:

```csharp
var scheduler = new TestScheduler();

// Advance virtual time instead of waiting real seconds
var results = scheduler.Start(() =>
    Observable.Interval(TimeSpan.FromSeconds(1), scheduler).Take(3)
);

Assert.That(results.Messages, Has.Count.EqualTo(4)); // 3 OnNext + 1 OnCompleted
```

Key methods:

- `AdvanceBy(ticks)`: Move clock forward by relative amount.
- `AdvanceTo(ticks)`: Move clock to absolute time.
- `Start(create)`: Creates, subscribes, disposes; returns recorded messages.

One tick = 100ns. Use `TimeSpan.FromSeconds(n).Ticks` for readability.

### Inject Schedulers

Accept `IScheduler` as a parameter in production code so tests can supply `TestScheduler`:

```csharp
public IObservable<T> PollEvery<T>(
    TimeSpan interval,
    Func<T> fetch,
    IScheduler scheduler)
{
    return Observable.Interval(interval, scheduler).Select(_ => fetch());
}
```

### Testing Without TestScheduler

For simpler cases, subscribe and collect values into a list:

```csharp
var results = new List<int>();
source.Subscribe(results.Add);
// ... trigger emissions ...
Assert.That(results, Is.EqualTo(new[] { 1, 2, 3 }));
```

Use `Subject<T>` in tests as a controllable source to push values imperatively. This is one of the
legitimate uses of subjects.

## Common Anti-patterns

1. **Implementing `IObservable<T>` on a class.** Use composition (expose a property) not
   inheritance.
2. **Exposing `Subject<T>` publicly.** Always wrap with `AsObservable()`. Public subjects let anyone
   call `OnNext` or `OnCompleted` on your stream.
3. **Using `getValue()`/`.Value` publicly.** Indicates imperative thinking in a reactive codebase.
   Subscribe to the stream instead.
4. **Subscribing without `OnError`.** Unhandled `OnError` throws. Always handle errors.
5. **Forgetting to dispose subscriptions.** Leaked subscriptions cause memory leaks and unexpected
   side effects.
6. **Concurrent `OnNext` on subjects.** Violates Rx contract. Use `Synchronize()` or restructure to
   avoid concurrent calls.
7. **Blocking operators (`First`, `Last`, `Single`).** These block the calling thread. Use async
   alternatives (`FirstAsync`) or test-specific patterns.
8. **Side effects in `Select`/`Where`.** Use `Do` for explicit side effects. Keep `Select` and
   `Where` pure.
9. **Unbounded `ReplaySubject`.** Always specify a buffer size or time window to prevent unbounded
   memory growth.
10. **Using a subject to bridge an existing observable.** Use `Publish`, `Replay`, or other sharing
    operators instead of subscribing to one observable and forwarding into a subject.

## Quick Reference

```txt
Need to...                          Use...
------------------------------------+-------------------------------------
Wrap event/callback/async -> Rx     Observable.Create / Observable.FromEvent
Push values imperatively (local)    Subject<T> (private, expose AsObservable)
Current-value property semantics    BehaviorSubject<T>
Share a cold observable             Publish().RefCount()
Group subscription disposal         CompositeDisposable + DisposeWith
Replace subscriptions               SerialDisposable
Handle errors in chain              Catch<TSource, TException>
Retry transient failures            Retry(count)
Control time in tests               TestScheduler (Microsoft.Reactive.Testing)
Enforce sequential OnNext           subject.Synchronize()
```
