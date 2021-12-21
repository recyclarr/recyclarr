using Autofac;
using TrashLib.Sonarr.Api;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.QualityDefinition;
using TrashLib.Sonarr.ReleaseProfile;

namespace TrashLib.Sonarr;

public class SonarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SonarrApi>().As<ISonarrApi>();
        builder.RegisterType<SonarrValidationMessages>().As<ISonarrValidationMessages>();

        builder.RegisterType<SonarrCompatibility>()
            .As<ISonarrCompatibility>()
            .SingleInstance();

        // Release Profile Support
        builder.RegisterType<ReleaseProfileUpdater>().As<IReleaseProfileUpdater>();
        builder.RegisterType<ReleaseProfileGuideParser>().As<IReleaseProfileGuideParser>();
        builder.RegisterType<SonarrReleaseProfileCompatibilityHandler>()
            .As<ISonarrReleaseProfileCompatibilityHandler>();

        // Quality Definition Support
        builder.RegisterType<SonarrQualityDefinitionUpdater>().As<ISonarrQualityDefinitionUpdater>();
        builder.RegisterType<SonarrQualityDefinitionGuideParser>().As<ISonarrQualityDefinitionGuideParser>();
    }
}
