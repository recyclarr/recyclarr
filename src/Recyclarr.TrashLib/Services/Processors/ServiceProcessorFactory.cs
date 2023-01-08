using Autofac;
using JetBrains.Annotations;
using Recyclarr.Common.Autofac;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.Processors;

[UsedImplicitly]
public class ServiceProcessorFactory
{
    private readonly ILifetimeScope _scope;

    public ServiceProcessorFactory(ILifetimeScope scope)
    {
        _scope = scope;
    }

    public LifetimeScopedValue<IServiceProcessor<T>> CreateProcessor<T>(IServiceConfiguration config)
        where T : ServiceConfiguration
    {
        var scope = _scope.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(config).As<IServiceConfiguration>();
        });

        return new LifetimeScopedValue<IServiceProcessor<T>>(scope, scope.Resolve<IServiceProcessor<T>>());
    }
}
