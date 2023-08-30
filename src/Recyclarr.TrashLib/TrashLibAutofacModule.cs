using System.Reflection;
using Autofac;
using Autofac.Extras.Ordering;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using AutoMapper.EquivalencyExpression;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
using Recyclarr.TrashLib.ApiServices;
using Recyclarr.TrashLib.Compatibility;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Startup;
using Module = Autofac.Module;

namespace Recyclarr.TrashLib;

public class TrashLibAutofacModule : Module
{
    public Assembly? AdditionalMapperProfileAssembly { get; init; }

    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        RegisterAppPaths(builder);
        CommonRegistrations(builder);

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<RepoAutofacModule>();
        builder.RegisterModule<CompatibilityAutofacModule>();
        builder.RegisterModule<ApiServicesAutofacModule>();
        builder.RegisterModule<ConfigAutofacModule>();
        builder.RegisterType<ServiceRequestBuilder>().As<IServiceRequestBuilder>();
        builder.RegisterType<FlurlClientFactory>().As<IFlurlClientFactory>().SingleInstance();

        var mapperAssemblies = new List<Assembly> {ThisAssembly};
        if (AdditionalMapperProfileAssembly is not null)
        {
            mapperAssemblies.Add(AdditionalMapperProfileAssembly);
        }

        builder.RegisterAutoMapper(c => c.AddCollectionMappers(), false, mapperAssemblies.ToArray());
    }

    private static void CommonRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<RuntimeValidationService>().As<IRuntimeValidationService>();
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
