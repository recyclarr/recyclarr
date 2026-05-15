using Autofac;

namespace Recyclarr.Config;

public class SyncScopeFactory(ILifetimeScope scope)
{
    public LifetimeScopeWrapper<TEntry> Start<TEntry>()
        where TEntry : notnull
    {
        var childScope = scope.BeginLifetimeScope("sync");
        return new LifetimeScopeWrapper<TEntry>(childScope);
    }
}
