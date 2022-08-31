using Autofac;
using Autofac.Extras.Ordering;
using TrashLib.Services.Sonarr.Api;
using TrashLib.Services.Sonarr.Config;
using TrashLib.Services.Sonarr.QualityDefinition;
using TrashLib.Services.Sonarr.ReleaseProfile;
using TrashLib.Services.Sonarr.ReleaseProfile.Filters;
using TrashLib.Services.Sonarr.ReleaseProfile.Guide;

namespace TrashLib.Services.Sonarr;

public class SonarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SonarrApi>().As<ISonarrApi>();
        builder.RegisterType<ReleaseProfileApiService>().As<IReleaseProfileApiService>();
        builder.RegisterType<SonarrValidationMessages>().As<ISonarrValidationMessages>();
        builder.RegisterType<SonarrCompatibility>().As<ISonarrCompatibility>().SingleInstance();
        builder.RegisterType<SonarrGuideDataLister>().As<ISonarrGuideDataLister>();

        // Release Profile Support
        builder.RegisterType<ReleaseProfileUpdater>().As<IReleaseProfileUpdater>();
        builder.RegisterType<LocalRepoSonarrGuideService>().As<ISonarrGuideService>();
        builder.RegisterType<SonarrReleaseProfileCompatibilityHandler>()
            .As<ISonarrReleaseProfileCompatibilityHandler>();
        builder.RegisterType<ReleaseProfileFilterPipeline>().As<IReleaseProfileFilterPipeline>();

        // Release Profile Filters (ORDER MATTERS!)
        builder.RegisterTypes(
                typeof(IncludeExcludeFilter),
                typeof(StrictNegativeScoresFilter))
            .As<IReleaseProfileFilter>()
            .OrderByRegistration();

        // Quality Definition Support
        builder.RegisterType<SonarrQualityDefinitionUpdater>().As<ISonarrQualityDefinitionUpdater>();
    }
}
