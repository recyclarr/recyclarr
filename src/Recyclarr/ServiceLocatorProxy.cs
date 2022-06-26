using Autofac;

namespace Recyclarr;

public sealed class ServiceLocatorProxy : IServiceLocatorProxy
{
    public ServiceLocatorProxy(ILifetimeScope container)
    {
        Container = container;
    }

    public ILifetimeScope Container { get; }

    public T Resolve<T>() where T : notnull
    {
        return Container.Resolve<T>();
    }

    public void Dispose()
    {
        Container.Dispose();
    }
}
