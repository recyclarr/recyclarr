using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Command.Initialization.Cleanup;
using Recyclarr.Command.Initialization.Init;

namespace Recyclarr.Command.Initialization;

public class InitializationAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<ServiceInitializationAndCleanup>().As<IServiceInitializationAndCleanup>();
        builder.RegisterType<DefaultAppDataSetup>().As<IDefaultAppDataSetup>();

        // Initialization Services
        builder.RegisterTypes(
                typeof(InitializeAppDataPath),
                typeof(CheckMigrationNeeded),
                typeof(ServiceInitializer))
            .As<IServiceInitializer>()
            .OrderByRegistration();

        // Cleanup Services
        builder.RegisterTypes(
                typeof(OldLogFileCleaner))
            .As<IServiceCleaner>()
            .OrderByRegistration();
    }
}
