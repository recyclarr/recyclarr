using Autofac;
using JetBrains.Annotations;
using Recyclarr.Common.Autofac;
using Recyclarr.TrashLib.Config;
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

    public LifetimeScopedValue<IServiceProcessor> CreateProcessor(
        SupportedServices serviceType, IServiceConfiguration config)
    {
        var scope = _scope.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(config).As<IServiceConfiguration>().AsSelf();
        });

        return new LifetimeScopedValue<IServiceProcessor>(scope, scope.ResolveKeyed<IServiceProcessor>(serviceType));
    }
}
