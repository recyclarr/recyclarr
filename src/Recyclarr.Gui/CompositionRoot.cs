using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using AutofacSerilogIntegration;
using Recyclarr.Common;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Gui;

public static class CompositionRoot
{
    public static void Setup(ContainerBuilder builder)
    {
        builder.RegisterLogger();

        builder.RegisterModule(new CommonAutofacModule(Assembly.GetExecutingAssembly()));

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<DefaultAppDataSetup>();
        builder.Register(c => c.Resolve<DefaultAppDataSetup>().CreateAppPaths())
            .As<IAppPaths>()
            .SingleInstance();
    }
}
