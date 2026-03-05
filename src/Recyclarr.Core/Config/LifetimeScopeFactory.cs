using Autofac;
using Recyclarr.Config.Models;

namespace Recyclarr.Config;

public class InstanceScopeFactory(ILifetimeScope scope)
{
    public LifetimeScopeWrapper<TEntry> Start<TEntry>(IServiceConfiguration config)
        where TEntry : notnull
    {
        var childScope = scope.BeginLifetimeScope(
            "instance",
            c => c.RegisterInstance(config).As<IServiceConfiguration>().As(config.GetType())
        );
        return new LifetimeScopeWrapper<TEntry>(childScope);
    }
}
