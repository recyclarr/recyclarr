using Autofac;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Config;

/// <summary>
/// Opens a lifetime scope for one sync run so run-scoped services and resources have one owner.
/// </summary>
/// <remarks>
/// Registered as a singleton so runs are children of the root container. A run outlives whatever
/// asked for it, and a child of a disposed scope cannot resolve anything. The run and its instance
/// scopes read guide data from the index passed to <see cref="Start{TEntry}"/>; the caller keeps
/// those files unchanged until the run ends.
/// </remarks>
public class SyncRunScopeFactory(ILifetimeScope scope)
{
    public LifetimeScopeWrapper<TEntry> Start<TEntry>(GuideIndex guide)
        where TEntry : notnull
    {
        var childScope = scope.BeginLifetimeScope(
            "run",
            c => c.RegisterInstance(ResourceScope.For(guide))
        );
        return new LifetimeScopeWrapper<TEntry>(childScope);
    }
}
