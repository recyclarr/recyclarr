using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using CliFx;
using Serilog;
using Serilog.Core;
using Trash.Command.Helpers;
using Trash.Config;
using TrashLib.Cache;
using TrashLib.Config;
using TrashLib.Radarr;
using TrashLib.Sonarr;
using YamlDotNet.Serialization;

namespace Trash
{
    public static class CompositionRoot
    {
        private static void SetupLogging(ContainerBuilder builder)
        {
            builder.RegisterType<LogJanitor>().As<ILogJanitor>();
            builder.RegisterType<LoggingLevelSwitch>().SingleInstance();
            builder.Register(c =>
                {
                    var logPath = Path.Combine(AppPaths.LogDirectory,
                        $"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

                    const string consoleTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

                    return new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console(outputTemplate: consoleTemplate, levelSwitch: c.Resolve<LoggingLevelSwitch>())
                        .WriteTo.File(logPath)
                        .CreateLogger();
                })
                .As<ILogger>()
                .SingleInstance();
        }

        private static void ConfigurationRegistrations(ContainerBuilder builder)
        {
            builder.RegisterModule<ConfigAutofacModule>();

            builder.RegisterType<ConfigurationProvider>()
                .As<IConfigurationProvider>()
                .SingleInstance();

            builder.RegisterType<ObjectFactory>()
                .As<IObjectFactory>();

            builder.RegisterGeneric(typeof(ConfigurationLoader<>))
                .WithProperty(new AutowiringParameter())
                .As(typeof(IConfigurationLoader<>));

            // note: Do not allow consumers to resolve IServiceConfiguration directly; if this gets cached
            // they end up using the wrong configuration when multiple instances are used.
            // builder.Register(c => c.Resolve<IConfigurationProvider>().ActiveConfiguration)
            // .As<IServiceConfiguration>();
        }

        private static void CommandRegistrations(ContainerBuilder builder)
        {
            // Register all types deriving from CliFx's ICommand. These are all of our supported subcommands.
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => t.IsAssignableTo(typeof(ICommand)));

            // Used to access the chosen command class. This is assigned from CliTypeActivator
            builder.RegisterType<ActiveServiceCommandProvider>()
                .As<IActiveServiceCommandProvider>()
                .SingleInstance();

            builder.Register(c => c.Resolve<IActiveServiceCommandProvider>().ActiveCommand)
                .As<IServiceCommand>();
        }

        public static IContainer Setup()
        {
            return Setup(new ContainerBuilder());
        }

        public static IContainer Setup(ContainerBuilder builder)
        {
            builder.RegisterType<FileSystem>().As<IFileSystem>();

            builder.RegisterModule<CacheAutofacModule>();
            builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();

            ConfigurationRegistrations(builder);
            CommandRegistrations(builder);

            SetupLogging(builder);

            builder.RegisterModule<SonarrAutofacModule>();
            builder.RegisterModule<RadarrAutofacModule>();

            // builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
            return builder.Build();
        }
    }
}
