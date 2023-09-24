using Autofac;

namespace Recyclarr.Platform;

public class PlatformAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        RegisterAppPaths(builder);
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
