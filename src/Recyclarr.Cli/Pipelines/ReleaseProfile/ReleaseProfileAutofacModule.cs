using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;
using Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile;

public class ReleaseProfileAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ReleaseProfileApiService>().As<IReleaseProfileApiService>();
        builder.RegisterType<ReleaseProfileFilterPipeline>().As<IReleaseProfileFilterPipeline>();
        builder.RegisterType<ReleaseProfileDataLister>();

        // Release Profile Filters (ORDER MATTERS!)
        builder.RegisterTypes(
                typeof(IncludeExcludeFilter),
                typeof(StrictNegativeScoresFilter))
            .As<IReleaseProfileFilter>()
            .OrderByRegistration();

        builder.RegisterTypes(
                typeof(ReleaseProfileConfigPhase),
                typeof(ReleaseProfilePreviewPhase),
                typeof(ReleaseProfileApiFetchPhase),
                typeof(ReleaseProfileTransactionPhase),
                typeof(ReleaseProfileApiPersistencePhase),
                typeof(ReleaseProfileLogPhase))
            .AsImplementedInterfaces();
    }
}
