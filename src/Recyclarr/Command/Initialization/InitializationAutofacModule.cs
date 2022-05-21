using Autofac;
using Autofac.Extras.Ordering;

namespace Recyclarr.Command.Initialization;

public class InitializationAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<ServiceInitializationAndCleanup>().As<IServiceInitializationAndCleanup>();

        // Initialization Services
        builder.RegisterTypes(
                typeof(ServiceInitializer),
                typeof(ServicePreInitializer))
            .As<IServiceInitializer>()
            .OrderByRegistration();

        // Cleanup Services
        builder.RegisterTypes(
                typeof(OldLogFileCleaner))
            .As<IServiceCleaner>()
            .OrderByRegistration();
    }
}
