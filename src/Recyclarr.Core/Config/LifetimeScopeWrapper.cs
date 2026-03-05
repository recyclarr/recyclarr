using Autofac;

namespace Recyclarr.Config;

public sealed class LifetimeScopeWrapper<TEntry>(ILifetimeScope scope) : IDisposable
    where TEntry : notnull
{
    public TEntry Entry { get; } = scope.Resolve<TEntry>();

    public void Dispose()
    {
        scope.Dispose();
    }
}
