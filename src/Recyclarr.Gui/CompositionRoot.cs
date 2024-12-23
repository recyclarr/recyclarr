using Autofac;
using AutofacSerilogIntegration;
using Recyclarr.Platform;

namespace Recyclarr.Gui;

internal static class CompositionRoot
{
    public static void Setup(ContainerBuilder builder)
    {
        builder.RegisterLogger();

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<DefaultAppDataSetup>();
        builder
            .Register(c => c.Resolve<DefaultAppDataSetup>().CreateAppPaths())
            .As<IAppPaths>()
            .SingleInstance();
    }
}
