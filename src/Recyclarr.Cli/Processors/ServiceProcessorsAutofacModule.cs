using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Processors.Delete;
using Recyclarr.Cli.Processors.Sync;

namespace Recyclarr.Cli.Processors;

public class ServiceProcessorsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ConsoleExceptionHandler>();

        // Sync
        builder.RegisterType<SyncProcessor>().As<ISyncProcessor>();
        builder.RegisterType<SyncPipelineExecutor>();

        // Configuration
        builder.RegisterType<ConfigManipulator>().As<IConfigManipulator>();
        builder.RegisterType<ConfigCreationProcessor>().As<IConfigCreationProcessor>();
        builder.RegisterType<ConfigListProcessor>();

        // Delete
        builder.RegisterType<DeleteCustomFormatsProcessor>().As<IDeleteCustomFormatsProcessor>();

        builder.RegisterTypes(
                typeof(TemplateConfigCreator),
                typeof(LocalConfigCreator))
            .As<IConfigCreator>()
            .OrderByRegistration();
    }
}
