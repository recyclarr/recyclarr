using Autofac;
using Recyclarr.Config.Models;

namespace Recyclarr.Config;

/// <summary>
/// Creates an Autofac lifetime scope for processing one configured service instance.
/// </summary>
/// <remarks>
/// The returned wrapper owns the child scope and resolves its entry point on first access.
/// </remarks>
public class InstanceScopeFactory(ILifetimeScope scope)
{
    /// <summary>
    /// Opens an instance scope and resolves the entry point owned by its returned wrapper.
    /// </summary>
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
