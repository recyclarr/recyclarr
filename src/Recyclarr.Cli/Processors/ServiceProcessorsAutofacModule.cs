using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Processors.Delete;
using Recyclarr.Cli.Processors.ErrorHandling;
using Recyclarr.Cli.Processors.Sync;

namespace Recyclarr.Cli.Processors;

public class ServiceProcessorsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ConsoleExceptionHandler>();
        builder.RegisterType<FlurlHttpExceptionHandler>();

        // Sync
        builder.RegisterType<SyncProcessor>().As<ISyncProcessor>();

        // Configuration
        builder.RegisterType<ConfigManipulator>().As<IConfigManipulator>();
        builder.RegisterType<ConfigCreationProcessor>().As<IConfigCreationProcessor>();
        builder.RegisterType<ConfigListLocalProcessor>();
        builder.RegisterType<ConfigListTemplateProcessor>();

        // Delete
        builder.RegisterType<DeleteCustomFormatsProcessor>().As<IDeleteCustomFormatsProcessor>();

        builder.RegisterTypes(
                typeof(TemplateConfigCreator),
                typeof(LocalConfigCreator))
            .As<IConfigCreator>()
            .OrderByRegistration();
    }
}
