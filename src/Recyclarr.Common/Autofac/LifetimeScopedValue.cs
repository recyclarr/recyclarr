using Autofac;

namespace Recyclarr.Common.Autofac;

public sealed class LifetimeScopedValue<T> : IDisposable
{
    private readonly ILifetimeScope _scope;

    public LifetimeScopedValue(ILifetimeScope scope, T value)
    {
        _scope = scope;
        Value = value;
    }

    public T Value { get; }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
