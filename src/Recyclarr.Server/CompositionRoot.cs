using System.IO.Abstractions;
using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Common;
using Recyclarr.Pipelines;
using Recyclarr.ResourceProviders;
using LoggingLevelSwitch = Serilog.Core.LoggingLevelSwitch;
using SerilogILogger = Serilog.ILogger;

namespace Recyclarr.Server;

internal static class CompositionRoot
{
    public static void Setup(ContainerBuilder builder)
    {
        var thisAssembly = typeof(CompositionRoot).Assembly;

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        RegisterLogger(builder);

        builder.RegisterModule<CoreAutofacModule>();
        builder.RegisterModule<PipelineAutofacModule>();
        builder.RegisterModule<ResourceProviderAutofacModule>();

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();
    }

    private static void RegisterLogger(ContainerBuilder builder)
    {
        builder.RegisterType<LoggingLevelSwitch>().SingleInstance();
        builder.RegisterType<ServerLogger>().AsSelf().As<SerilogILogger>().SingleInstance();
    }
}
