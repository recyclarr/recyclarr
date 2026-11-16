using Autofac;
using FluentValidation;
using Recyclarr.Common;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.CustomFormat.State;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.Plan.Components;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualityProfile.State;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Sync;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines;

public class PipelineAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ConfiguredCustomFormatProvider>().InstancePerLifetimeScope();

        RegisterPipelines(builder);
        RegisterCustomFormatSupport(builder);
        RegisterQualityProfileSupport(builder);
        RegisterQualitySizeSupport(builder);
        RegisterMediaNamingSupport(builder);
    }

    // Planning and execution order come from declared dependencies (PipelineGraph). Registration
    // order only breaks ties between independent pipelines.
    private static void RegisterPipelines(ContainerBuilder builder)
    {
        // One graph per detected-service scope, shared by planning and execution
        builder.RegisterType<PipelineGraph>().InstancePerLifetimeScope();
        builder.RegisterType<PlanBuilder>();
        builder.RegisterType<CompositeSyncPipeline>().As<IPipelineExecutor>();

        RegisterPipeline<PlannedCustomFormats, CustomFormatPlanner, CustomFormatSyncOperation>(
            builder,
            PipelineType.CustomFormat
        );
        RegisterPipeline<
            PlannedQualityProfiles,
            QualityProfilePlanner,
            QualityProfileSyncOperation
        >(builder, PipelineType.QualityProfile, PipelineType.CustomFormat);
        RegisterPipeline<PlannedQualitySizes, QualitySizePlanner, QualitySizeSyncOperation>(
            builder,
            PipelineType.QualitySize
        );
        RegisterPipeline<PlannedMediaNaming, MediaNamingPlanner, MediaNamingSyncOperation>(
            builder,
            PipelineType.MediaNaming
        );
        RegisterPipeline<
            PlannedMediaManagement,
            MediaManagementPlanner,
            MediaManagementSyncOperation
        >(builder, PipelineType.MediaManagement);
    }

    private static void RegisterPipeline<TPlan, TPlanner, TOperation>(
        ContainerBuilder builder,
        PipelineType type,
        params PipelineType[] dependencies
    )
        where TPlan : class
        where TPlanner : IPipelinePlanner<TPlan>
        where TOperation : ISyncOperation<TPlan>
    {
        builder.RegisterType<TPlanner>();
        builder.RegisterType<TOperation>();
        builder
            .Register(c => new Pipeline<TPlan>(
                type,
                dependencies,
                c.Resolve<TPlanner>(),
                c.Resolve<TOperation>()
            ))
            .As<IPipeline>();
    }

    private static void RegisterMediaNamingSupport(ContainerBuilder builder)
    {
        builder
            .RegisterServiceGateway<IMediaNamingPlanner>()
            .For<SonarrMediaNamingPlanner>(ServiceApiTarget.SonarrV3)
            .For<SonarrMediaNamingPlanner>(ServiceApiTarget.SonarrV5)
            .For<RadarrMediaNamingPlanner>(ServiceApiTarget.RadarrV3);
        builder
            .RegisterServiceGateway<IMediaNamingSync>()
            .For<SonarrNamingSync>(ServiceApiTarget.SonarrV3)
            .For<SonarrNamingSync>(ServiceApiTarget.SonarrV5)
            .For<RadarrNamingSync>(ServiceApiTarget.RadarrV3);
    }

    private static void RegisterCustomFormatSupport(ContainerBuilder builder)
    {
        builder.RegisterType<ExplicitCfGroupValidator>().As<IValidator<CustomFormatGroupConfig>>();
        builder.RegisterType<CategorizedCustomFormatProvider>();
        builder.RegisterType<CustomFormatStatePersister>().As<ICustomFormatStatePersister>();
        builder.RegisterType<CustomFormatTransactionLogger>();
    }

    private static void RegisterQualityProfileSupport(ContainerBuilder builder)
    {
        builder
            .RegisterServiceGateway<IQualityProfileServiceFields>()
            .For<SonarrV3QualityProfileFields>(ServiceApiTarget.SonarrV3)
            .For<SonarrV5QualityProfileFields>(ServiceApiTarget.SonarrV5)
            .For<RadarrQualityProfileFields>(ServiceApiTarget.RadarrV3);
        builder
            .RegisterServiceGateway<IQualityProfileSizePlanner>()
            .For<GlobalQualitySizeProfilePlanner>(ServiceApiTarget.SonarrV3)
            .For<SonarrV5QualityProfileSizePlanner>(ServiceApiTarget.SonarrV5)
            .For<GlobalQualitySizeProfilePlanner>(ServiceApiTarget.RadarrV3);
        builder.RegisterType<QualityProfileStatCalculator>();
        builder.RegisterType<QualityProfileLogger>();
        builder.RegisterType<QualityProfileStatePersister>().As<IQualityProfileStatePersister>();
    }

    private static void RegisterQualitySizeSupport(ContainerBuilder builder)
    {
        builder.RegisterType<QualitySizeResolver>();
        builder.RegisterType<QualityItemLimitFactory>().As<IQualityItemLimitFactory>();
        builder
            .RegisterType<RadarrQualityItemLimitFetcher>()
            .Keyed<IQualityItemLimitFetcher>(SupportedServices.Radarr)
            .InstancePerLifetimeScope();
        builder
            .RegisterType<SonarrQualityItemLimitFetcher>()
            .Keyed<IQualityItemLimitFetcher>(SupportedServices.Sonarr)
            .InstancePerLifetimeScope();
    }
}
