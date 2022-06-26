using System.IO.Abstractions;
using Autofac;
using AutofacSerilogIntegration;
using Common;
using TrashLib.Startup;

namespace Recyclarr.Gui;

public static class CompositionRoot
{
    public static IContainer Setup()
    {
        var builder = new ContainerBuilder();
        Setup(builder);
        return builder.Build();
    }

    public static void Setup(ContainerBuilder builder)
    {
        builder.RegisterLogger();

        builder.RegisterModule<CommonAutofacModule>();

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<DefaultAppDataSetup>();
        builder.Register(c => c.Resolve<DefaultAppDataSetup>().CreateAppPaths())
            .As<IAppPaths>()
            .SingleInstance();
    }
}
