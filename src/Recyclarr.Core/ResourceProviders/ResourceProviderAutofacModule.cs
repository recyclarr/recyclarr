using Autofac;
using Recyclarr.ConfigTemplates;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.ResourceProviders.Storage;

namespace Recyclarr.ResourceProviders;

public class ResourceProviderAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterInfrastructure(builder);
        RegisterStorageLayer(builder);
        RegisterDomainLayer(builder);
    }

    private static void RegisterInfrastructure(ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(ResourceRegistry<>)).AsSelf().SingleInstance();

        builder.RegisterType<TrashGuidesStrategy>().As<IProviderTypeStrategy>();
        builder.RegisterType<ConfigTemplatesStrategy>().As<IProviderTypeStrategy>();
        builder.RegisterType<CustomFormatsStrategy>().As<IProviderTypeStrategy>();

        builder.RegisterType<ProviderInitializationFactory>().AsSelf().SingleInstance();
    }

    private static void RegisterStorageLayer(ContainerBuilder builder)
    {
        builder.RegisterType<GitProviderLocation>().AsSelf().InstancePerDependency();
        builder.RegisterType<LocalProviderLocation>().AsSelf().InstancePerDependency();

        builder.RegisterType<ResourceCacheCleanupService>().As<IResourceCacheCleanupService>();
    }

    private static void RegisterDomainLayer(ContainerBuilder builder)
    {
        builder.RegisterType<JsonResourceLoader>().AsSelf().SingleInstance();

        builder.RegisterType<CategoryResourceQuery>().AsSelf().SingleInstance();
        builder.RegisterType<CustomFormatResourceQuery>().AsSelf().SingleInstance();
        builder.RegisterType<QualitySizeResourceQuery>().AsSelf().SingleInstance();
        builder.RegisterType<MediaNamingResourceQuery>().AsSelf().SingleInstance();
        builder.RegisterType<ConfigTemplatesResourceQuery>().AsSelf().SingleInstance();
        builder.RegisterType<ConfigIncludesResourceQuery>().AsSelf().SingleInstance();
        builder.RegisterType<CfGroupResourceQuery>().AsSelf().SingleInstance();
        builder.RegisterType<QualityProfileResourceQuery>().AsSelf().SingleInstance();
    }
}
