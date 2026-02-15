using Autofac;
using Recyclarr.Config;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncScope(ILifetimeScope scope) : LifetimeScopeWrapper(scope)
{
    public SyncProcessor Processor { get; } = scope.Resolve<SyncProcessor>();
}
