using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.TrashLib.Services.ReleaseProfile.Api;
using Recyclarr.TrashLib.Services.ReleaseProfile.Filters;
using Recyclarr.TrashLib.Services.ReleaseProfile.Guide;

namespace Recyclarr.TrashLib.Services.ReleaseProfile;

public class ReleaseProfileAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ReleaseProfileApiService>().As<IReleaseProfileApiService>();

        builder.RegisterType<ReleaseProfileUpdater>().As<IReleaseProfileUpdater>();
        builder.RegisterType<SonarrReleaseProfileCompatibilityHandler>()
            .As<ISonarrReleaseProfileCompatibilityHandler>();
        builder.RegisterType<ReleaseProfileFilterPipeline>().As<IReleaseProfileFilterPipeline>();
        builder.RegisterType<ReleaseProfileGuideParser>();
        builder.RegisterType<ReleaseProfileGuideService>().As<IReleaseProfileGuideService>();

        // Release Profile Filters (ORDER MATTERS!)
        builder.RegisterTypes(
                typeof(IncludeExcludeFilter),
                typeof(StrictNegativeScoresFilter))
            .As<IReleaseProfileFilter>()
            .OrderByRegistration();
    }
}
