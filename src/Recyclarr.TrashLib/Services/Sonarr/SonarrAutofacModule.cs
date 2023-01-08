using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.Sonarr.Api;
using Recyclarr.TrashLib.Services.Sonarr.Capabilities;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile.Filters;

namespace Recyclarr.TrashLib.Services.Sonarr;

public class SonarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SonarrApi>().As<ISonarrApi>();
        builder.RegisterType<ReleaseProfileApiService>().As<IReleaseProfileApiService>();

        builder.RegisterType<SonarrCapabilityEnforcer>();
        builder.RegisterType<SonarrCapabilityChecker>().As<ISonarrCapabilityChecker>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SonarrGuideDataLister>().As<ISonarrGuideDataLister>();
        builder.RegisterType<LocalRepoSonarrGuideService>()
            .As<SonarrGuideService>()
            .Keyed<IGuideService>(SupportedServices.Sonarr);

        // Release Profile Support
        builder.RegisterType<ReleaseProfileUpdater>().As<IReleaseProfileUpdater>();
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
