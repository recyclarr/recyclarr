using Autofac;
using JetBrains.Annotations;
using Recyclarr.Config.Models;

namespace Recyclarr.Config;

[UsedImplicitly]
public class ConfigurationScopeFactory(ILifetimeScope scope)
{
    public T Start<T>(IServiceConfiguration config) where T : ConfigurationScope, new()
    {
        var childScope = scope.BeginLifetimeScope(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<T>();
        });

        var newScope = childScope.Resolve<T>();
        newScope.SetScope(childScope);
        return newScope;
    }
}
