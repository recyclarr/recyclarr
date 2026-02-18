using Autofac;
using Recyclarr.Config;

namespace Recyclarr.Cli.Processors.Sync;

internal class InstanceScope(ILifetimeScope scope) : LifetimeScopeWrapper(scope)
{
    public InstanceSyncProcessor InstanceProcessor { get; } =
        scope.Resolve<InstanceSyncProcessor>();
}
