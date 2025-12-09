using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.Plan.Components;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines;

internal class PipelineAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterComposite<CompositeSyncPipeline, ISyncPipeline>();
        builder
            .RegisterTypes(
                // ORDER HERE IS IMPORTANT!
                // There are indirect dependencies between pipelines.
                typeof(GenericSyncPipeline<CustomFormatPipelineContext>),
                typeof(GenericSyncPipeline<QualityProfilePipelineContext>),
                typeof(GenericSyncPipeline<QualitySizePipelineContext>),
                typeof(GenericSyncPipeline<MediaNamingPipelineContext>)
            )
            .As<ISyncPipeline>()
            .OrderByRegistration();

        RegisterPlan(builder);
        RegisterQualityProfile(builder);
        RegisterQualitySize(builder);
        RegisterCustomFormat(builder);
        RegisterMediaNaming(builder);
    }

    private static void RegisterPlan(ContainerBuilder builder)
    {
        builder.RegisterType<PlanBuilder>();
        builder.RegisterType<DiagnosticsRenderer>();

        // ORDER HERE IS IMPORTANT!
        // CF must run before QP (QP references CFs from plan)
        builder
            .RegisterTypes(
                typeof(CustomFormatPlanComponent),
                typeof(QualityProfilePlanComponent),
                typeof(QualitySizePlanComponent),
                typeof(MediaNamingPlanComponent)
            )
            .As<IPlanComponent>()
            .OrderByRegistration();
    }

    private static void RegisterMediaNaming(ContainerBuilder builder)
    {
        builder.RegisterType<MediaNamingDataLister>();

        builder
            .RegisterType<RadarrMediaNamingConfigPhase>()
            .Keyed<IServiceBasedMediaNamingConfigPhase>(SupportedServices.Radarr);
        builder
            .RegisterType<SonarrMediaNamingConfigPhase>()
            .Keyed<IServiceBasedMediaNamingConfigPhase>(SupportedServices.Sonarr);

        builder
            .RegisterTypes(
                typeof(MediaNamingApiFetchPhase),
                typeof(MediaNamingTransactionPhase),
                typeof(MediaNamingPreviewPhase),
                typeof(MediaNamingApiPersistencePhase)
            )
            .AsImplementedInterfaces()
            .OrderByRegistration();
    }

    private static void RegisterQualityProfile(ContainerBuilder builder)
    {
        builder.RegisterType<QualityProfileStatCalculator>();
        builder.RegisterType<QualityProfileLogger>();

        builder
            .RegisterTypes(
                typeof(QualityProfileApiFetchPhase),
                typeof(QualityProfileTransactionPhase),
                typeof(QualityProfilePreviewPhase),
                typeof(QualityProfileApiPersistencePhase)
            )
            .AsImplementedInterfaces()
            .OrderByRegistration();
    }

    private static void RegisterQualitySize(ContainerBuilder builder)
    {
        builder.RegisterType<QualitySizeDataLister>();

        // Setup factory for creation of concrete IQualityItemLimits types
        builder.RegisterType<QualityItemLimitFactory>().As<IQualityItemLimitFactory>();
        builder
            .RegisterType<RadarrQualityItemLimitFetcher>()
            .Keyed<IQualityItemLimitFetcher>(SupportedServices.Radarr)
            .InstancePerLifetimeScope();
        builder
            .RegisterType<SonarrQualityItemLimitFetcher>()
            .Keyed<IQualityItemLimitFetcher>(SupportedServices.Sonarr)
            .InstancePerLifetimeScope();

        builder
            .RegisterTypes(
                typeof(QualitySizeApiFetchPhase),
                typeof(QualitySizeTransactionPhase),
                typeof(QualitySizePreviewPhase),
                typeof(QualitySizeApiPersistencePhase)
            )
            .AsImplementedInterfaces()
            .OrderByRegistration();
    }

    private static void RegisterCustomFormat(ContainerBuilder builder)
    {
        builder.RegisterType<CustomFormatDataLister>();
        builder.RegisterType<CustomFormatCachePersister>().As<ICachePersister<CustomFormatCache>>();
        builder.RegisterType<CustomFormatTransactionLogger>();

        builder
            .RegisterTypes(
                typeof(CustomFormatApiFetchPhase),
                typeof(CustomFormatTransactionPhase),
                typeof(CustomFormatPreviewPhase),
                typeof(CustomFormatApiPersistencePhase)
            )
            .AsImplementedInterfaces()
            .OrderByRegistration();
    }
}
