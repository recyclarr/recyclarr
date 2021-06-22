using System.IO.Abstractions;
using Autofac;
using BlazorPro.BlazorSize;
using Recyclarr.Code.Radarr;
using Recyclarr.Code.Settings;
using Recyclarr.Code.Settings.Persisters;
using Serilog;
using TrashLib.Config;
using TrashLib.Radarr;
using TrashLib.Radarr.Config;

namespace Recyclarr
{
    internal static class CompositionRoot
    {
        public static void Build(ContainerBuilder builder)
        {
            builder.Register(_ => new LoggerConfiguration().MinimumLevel.Debug().CreateLogger())
                .As<ILogger>()
                .SingleInstance();

            builder.RegisterType<ResizeListener>().As<IResizeListener>();
            builder.RegisterType<FileSystem>().As<IFileSystem>();
            builder.RegisterType<ResourcePaths>().As<IResourcePaths>();

            // Persisters
            builder.RegisterType<SettingsPersister>().As<ISettingsPersister>();
            builder.RegisterType<AppSettingsPersister>().As<IAppSettingsPersister>();

            SetupRadarrDependencies(builder);
        }

        private static void SetupRadarrDependencies(ContainerBuilder builder)
        {
            builder.RegisterModule<RadarrAutofacModule>();

            builder.RegisterType<CustomFormatRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<ConfigPersister<RadarrConfiguration>>()
                .As<IConfigPersister<RadarrConfiguration>>()
                .WithParameter(new NamedParameter("filename", "radarr.json"));

            builder.Register(c => c.Resolve<IConfigPersister<RadarrConfiguration>>().Load())
                .InstancePerLifetimeScope();
        }
    }
}
