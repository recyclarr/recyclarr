using Autofac;
using Recyclarr.Config;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Processors.Sync;

internal class InstanceScope(ILifetimeScope scope) : LifetimeScopeWrapper(scope)
{
    public InstanceSyncProcessor InstanceProcessor { get; } =
        scope.Resolve<InstanceSyncProcessor>();

    public IInstancePublisher Publisher { get; } = scope.Resolve<IInstancePublisher>();
}
