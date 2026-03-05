using Autofac;
using Recyclarr.Config;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncScopeFactory(ILifetimeScope scope)
{
    public LifetimeScopeWrapper<TEntry> Start<TEntry>()
        where TEntry : notnull
    {
        var childScope = scope.BeginLifetimeScope("sync");
        return new LifetimeScopeWrapper<TEntry>(childScope);
    }
}
