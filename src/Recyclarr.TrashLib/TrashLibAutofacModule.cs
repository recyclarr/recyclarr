using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
using Recyclarr.TrashLib.ApiServices;
using Recyclarr.TrashLib.Compatibility;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Json;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Settings;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib;

public class TrashLibAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        RegisterAppPaths(builder);
        CommonRegistrations(builder);

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        builder.RegisterType<SettingsProvider>().As<ISettingsProvider>().SingleInstance();
        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<RepoAutofacModule>();
        builder.RegisterModule<CompatibilityAutofacModule>();
        builder.RegisterModule<ApiServicesAutofacModule>();
        builder.RegisterType<ServiceRequestBuilder>().As<IServiceRequestBuilder>();
        builder.RegisterType<FlurlClientFactory>().As<IFlurlClientFactory>().SingleInstance();
        builder.RegisterType<BulkJsonLoader>();
    }

    private static void CommonRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<RuntimeValidationService>().As<IRuntimeValidationService>();
    }

    private static void RegisterAppPaths(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultAppDataSetup>();

        builder.Register(c =>
            {
                var appData = c.Resolve<AppDataPathProvider>();
                var dataSetup = c.Resolve<DefaultAppDataSetup>();
                return dataSetup.CreateAppPaths(appData.AppDataPath);
            })
            .As<IAppPaths>()
            .SingleInstance();
    }
}
