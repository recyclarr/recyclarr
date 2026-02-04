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

        builder.RegisterType<ProviderInitializationFactory>();
    }

    private static void RegisterStorageLayer(ContainerBuilder builder)
    {
        builder.RegisterType<GitProviderLocation>().AsSelf().InstancePerDependency();
        builder.RegisterType<LocalProviderLocation>().AsSelf().InstancePerDependency();

        builder.RegisterType<ResourceCacheCleanupService>().As<IResourceCacheCleanupService>();
    }

    private static void RegisterDomainLayer(ContainerBuilder builder)
    {
        builder.RegisterType<JsonResourceLoader>();

        builder.RegisterType<CategoryResourceQuery>();
        builder.RegisterType<CustomFormatResourceQuery>();
        builder.RegisterType<QualitySizeResourceQuery>();
        builder.RegisterType<MediaNamingResourceQuery>();
        builder.RegisterType<ConfigTemplatesResourceQuery>();
        builder.RegisterType<ConfigIncludesResourceQuery>();
        builder.RegisterType<CfGroupResourceQuery>();
        builder.RegisterType<QualityProfileResourceQuery>();
    }
}
