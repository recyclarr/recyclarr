using Autofac;

namespace Recyclarr;

public interface IServiceLocatorProxy : IDisposable
{
    ILifetimeScope Container { get; }
    T Resolve<T>() where T : notnull;
}
