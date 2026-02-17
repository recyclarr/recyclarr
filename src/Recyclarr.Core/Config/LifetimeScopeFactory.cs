using Autofac;

namespace Recyclarr.Config;

public class LifetimeScopeFactory(ILifetimeScope scope)
{
    public T Start<T>(object tag, Action<ContainerBuilder>? configure = null)
        where T : LifetimeScopeWrapper
    {
        var childScope = scope.BeginLifetimeScope(tag, c => configure?.Invoke(c));
        return childScope.Resolve<T>();
    }

    public T Start<T>(Action<ContainerBuilder>? configure = null)
        where T : LifetimeScopeWrapper
    {
        var childScope = scope.BeginLifetimeScope(c => configure?.Invoke(c));
        return childScope.Resolve<T>();
    }
}
