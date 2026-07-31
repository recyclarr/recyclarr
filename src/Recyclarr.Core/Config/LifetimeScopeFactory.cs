using Autofac;
using Recyclarr.Config.Models;

namespace Recyclarr.Config;

/// <summary>
/// Creates an Autofac lifetime scope for processing one configured service instance.
/// </summary>
/// <remarks>
/// Entry resolution occurs before ownership is returned. A resolution failure disposes the child
/// scope so resources activated during the failed resolution do not leak.
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

        try
        {
            return new LifetimeScopeWrapper<TEntry>(childScope);
        }
        catch
        {
            childScope.Dispose();
            throw;
        }
    }
}
