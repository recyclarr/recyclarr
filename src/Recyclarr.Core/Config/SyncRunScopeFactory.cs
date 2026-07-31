using Autofac;

namespace Recyclarr.Config;

/// <summary>
/// Opens a lifetime scope for one sync run. Everything a run accumulates (its event streams,
/// diagnostics subscriptions, and computed results) lives in that scope and dies with it, which is
/// what keeps concurrent runs from seeing each other.
/// </summary>
/// <remarks>
/// Registered as a singleton so runs are children of the root container. A run outlives whatever
/// asked for it, and a child of a disposed scope cannot resolve anything.
/// </remarks>
public class SyncRunScopeFactory(ILifetimeScope scope)
{
    public LifetimeScopeWrapper<TEntry> Start<TEntry>()
        where TEntry : notnull
    {
        var childScope = scope.BeginLifetimeScope("run");
        return new LifetimeScopeWrapper<TEntry>(childScope);
    }
}
