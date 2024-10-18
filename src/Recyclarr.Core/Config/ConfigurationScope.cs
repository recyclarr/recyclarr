using System.Diagnostics.CodeAnalysis;
using Autofac;

namespace Recyclarr.Config;

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor", Justification =
    "Base types are required to instruct Autofac which types we want to resolve")]
public abstract class ConfigurationScope(ILifetimeScope scope) : IDisposable
{
    protected ILifetimeScope Scope { get; } = scope;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Scope.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
