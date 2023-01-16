using Autofac;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.Services.Processors;

public class ServiceProcessorsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<ServiceProcessorFactory>();
        builder.RegisterType<RadarrProcessor>().Keyed<IServiceProcessor>(SupportedServices.Radarr);
        builder.RegisterType<SonarrProcessor>().Keyed<IServiceProcessor>(SupportedServices.Sonarr);
        builder.RegisterType<ConfigCreationProcessor>().As<IConfigCreationProcessor>();
    }
}
