using Autofac;

namespace Recyclarr.Config;

/// <summary>
/// Owns a lifetime scope and resolves its entry point on first access.
/// </summary>
/// <remarks>
/// Deferred resolution lets callers dispose the scope when entry activation fails. The wrapper must
/// remain alive while the entry point is used.
/// </remarks>
public sealed class LifetimeScopeWrapper<TEntry>(ILifetimeScope scope) : IDisposable
    where TEntry : notnull
{
    private readonly Lazy<TEntry> _entry = new(scope.Resolve<TEntry>);

    public TEntry Entry => _entry.Value;

    public void Dispose()
    {
        scope.Dispose();
    }
}
