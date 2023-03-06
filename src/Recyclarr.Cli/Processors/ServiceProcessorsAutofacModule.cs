using Autofac;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Processors.Sync;

namespace Recyclarr.Cli.Processors;

public class ServiceProcessorsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<ConfigCreationProcessor>().As<IConfigCreationProcessor>();
        builder.RegisterType<SyncProcessor>().As<ISyncProcessor>();
        builder.RegisterType<SyncPipelineExecutor>();
        builder.RegisterType<ConfigListProcessor>();
    }
}
