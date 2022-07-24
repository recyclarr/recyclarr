using Autofac;

namespace Recyclarr;

/// <remarks>
///     This class exists to make unit testing easier. Many methods for ILifetimeScope are extension
///     methods and make unit testing more difficult.
///     This class wraps Autofac to make it more
///     "mockable".
/// </remarks>
public interface IServiceLocatorProxy : IDisposable
{
    ILifetimeScope Container { get; }
    T Resolve<T>() where T : notnull;
}
