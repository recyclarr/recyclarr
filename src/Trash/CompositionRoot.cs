using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Trash.Command;
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
        // private static void SetupMediator(ContainerBuilder builder)
        // {
        //     builder
        //         .RegisterType<Mediator>()
        //         .As<IMediator>()
        //         .InstancePerLifetimeScope();
        //
        //     builder.Register<ServiceFactory>(context =>
        //     {
        //         var c = context.Resolve<IComponentContext>();
        //         return t => c.Resolve(t);
        //     });
        //
        //     builder.RegisterAssemblyTypes(typeof(CompositionRoot).GetTypeInfo().Assembly).AsImplementedInterfaces();
        // }

        // private static void RegisterConfiguration<T>(ContainerBuilder builder)
        //     where T : BaseConfiguration
        // {
        //
        //     builder.Register(ctx =>
        //         {
        //             var selector = ctx.Resolve<IConfigurationProvider<T>>();
        //             if (selector.ActiveConfiguration == null)
        //             {
        //                 // If this exception is thrown, that means that a BaseCommand subclass has not implemented the
        //                 // appropriate logic to set the active configuration via an IConfigurationSelector.
        //                 throw new InvalidOperationException("No valid configuration has been selected");
        //             }
        //
        //             return selector.ActiveConfiguration;
        //         })
        //         .As<BaseConfiguration>()
        //         .AsSelf();
        // }

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

            // Register all types deriving from BaseCommand. These are all of our supported subcommands.
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => t.IsAssignableTo(typeof(IBaseCommand)));

            SonarrRegistrations(builder);
            RadarrRegistrations(builder);

            // builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
            return builder.Build();
        }
    }
}
