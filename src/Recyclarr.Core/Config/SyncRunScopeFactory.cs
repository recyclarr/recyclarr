using Autofac;

namespace Recyclarr.Config;

/// <summary>
/// Opens a lifetime scope for one sync run so run-scoped services and resources have one owner.
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
