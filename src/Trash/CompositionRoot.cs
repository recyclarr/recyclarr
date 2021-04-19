using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using CliFx;
using Serilog;
using Serilog.Core;
using Trash.Config;
using Trash.Radarr.Api;
using Trash.Radarr.QualityDefinition;
using Trash.Sonarr.Api;
using Trash.Sonarr.QualityDefinition;
using Trash.Sonarr.ReleaseProfile;
using YamlDotNet.Serialization;

namespace Trash
{
    public static class CompositionRoot
    {
        private static void SetupLogging(ContainerBuilder builder)
        {
            builder.RegisterType<LoggingLevelSwitch>().SingleInstance();
            builder.Register(c =>
                {
                    const string template = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";
                    return new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(c.Resolve<LoggingLevelSwitch>())
                        .WriteTo.Console(outputTemplate: template)
                        .CreateLogger();
                })
                .As<ILogger>();
        }

        private static void SonarrRegistrations(ContainerBuilder builder)
        {
            builder.RegisterType<SonarrApi>().As<ISonarrApi>();

            // Release Profile Support
            builder.RegisterType<ReleaseProfileUpdater>();
            builder.RegisterType<ReleaseProfileGuideParser>().As<IReleaseProfileGuideParser>();

            // Quality Definition Support
            builder.RegisterType<SonarrQualityDefinitionUpdater>();
            builder.RegisterType<SonarrQualityDefinitionGuideParser>().As<ISonarrQualityDefinitionGuideParser>();
        }

        private static void RadarrRegistrations(ContainerBuilder builder)
        {
            builder.RegisterType<RadarrApi>().As<IRadarrApi>();

            // Quality Definition Support
            builder.RegisterType<RadarrQualityDefinitionUpdater>();
            builder.RegisterType<RadarrQualityDefinitionGuideParser>().As<IRadarrQualityDefinitionGuideParser>();
        }

        public static IContainer Setup()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<FileSystem>().As<IFileSystem>();

            // Configuration
            builder.RegisterType<ObjectFactory>().As<IObjectFactory>();
            builder.RegisterGeneric(typeof(ConfigurationLoader<>)).As(typeof(IConfigurationLoader<>));
            builder.RegisterGeneric(typeof(ConfigurationProvider<>))
                .As(typeof(IConfigurationProvider<>))
                .SingleInstance();

            // Register all types deriving from CliFx's ICommand. These are all of our supported subcommands.
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => t.IsAssignableTo(typeof(ICommand)));

            SetupLogging(builder);
            SonarrRegistrations(builder);
            RadarrRegistrations(builder);

            // builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
            return builder.Build();
        }
    }
}
