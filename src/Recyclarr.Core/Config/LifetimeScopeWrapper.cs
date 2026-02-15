using System.Diagnostics.CodeAnalysis;
using Autofac;

namespace Recyclarr.Config;

[SuppressMessage(
    "ReSharper",
    "SuggestBaseTypeForParameterInConstructor",
    Justification = "ILifetimeScope is required to instruct Autofac which type to resolve"
)]
public abstract class LifetimeScopeWrapper(ILifetimeScope scope) : IDisposable
{
    protected ILifetimeScope Scope { get; } = scope;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Scope.Dispose();
        }
    }
}
