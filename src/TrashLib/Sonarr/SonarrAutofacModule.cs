using Autofac;
using Autofac.Extras.Ordering;
using TrashLib.Sonarr.Api;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.QualityDefinition;
using TrashLib.Sonarr.ReleaseProfile;
using TrashLib.Sonarr.ReleaseProfile.Filters;
using TrashLib.Sonarr.ReleaseProfile.Guide;

namespace TrashLib.Sonarr;

public class SonarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SonarrApi>().As<ISonarrApi>();
        builder.RegisterType<SonarrValidationMessages>().As<ISonarrValidationMessages>();
        builder.RegisterType<SonarrCompatibility>().As<ISonarrCompatibility>().SingleInstance();
        builder.RegisterType<ReleaseProfileLister>().As<IReleaseProfileLister>();

        // Release Profile Support
        builder.RegisterType<ReleaseProfileUpdater>().As<IReleaseProfileUpdater>();
        builder.RegisterType<LocalRepoReleaseProfileJsonParser>().As<ISonarrGuideService>();
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
        builder.RegisterType<SonarrQualityDefinitionGuideParser>().As<ISonarrQualityDefinitionGuideParser>();
    }
}
