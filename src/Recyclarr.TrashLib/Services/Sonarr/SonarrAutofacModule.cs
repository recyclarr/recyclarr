using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.TrashLib.Services.Sonarr.Api;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile.Filters;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile.Guide;

namespace Recyclarr.TrashLib.Services.Sonarr;

public class SonarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SonarrApi>().As<ISonarrApi>();
        builder.RegisterType<ReleaseProfileApiService>().As<IReleaseProfileApiService>();

        builder.RegisterType<SonarrCompatibility>().As<ISonarrCompatibility>()
            .InstancePerLifetimeScope();

        builder.Register<SonarrCapabilities>(c => c.Resolve<ISonarrCompatibility>().Capabilities);

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
    }
}
