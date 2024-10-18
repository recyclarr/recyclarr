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
        builder.RegisterType<DefaultAppDataSetup>().As<IAppDataSetup>().AsSelf().SingleInstance();
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<DefaultRuntimeInformation>().As<IRuntimeInformation>();

        builder.Register(c => c.Resolve<DefaultAppDataSetup>().CreateAppPaths())
            .As<IAppPaths>()
            .SingleInstance();
    }
}
