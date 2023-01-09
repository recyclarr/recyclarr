using Autofac;
using Recyclarr.TrashLib.Services.Radarr.Config;
using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Services.Processors;

public class ServiceProcessorsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<ServiceProcessorFactory>();
        builder.RegisterType<RadarrProcessor>().As<IServiceProcessor<RadarrConfiguration>>();
        builder.RegisterType<SonarrProcessor>().As<IServiceProcessor<SonarrConfiguration>>();
        builder.RegisterType<ConfigCreationProcessor>().As<IConfigCreationProcessor>();
    }
}
