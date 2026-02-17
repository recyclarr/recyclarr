using Autofac;
using Autofac.Extras.Ordering;
using FluentValidation;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.Cli.Pipelines.MediaManagement;
using Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.Plan.Components;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualityProfile.State;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Config.Models;
using Recyclarr.SyncState;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines;

internal class PipelineAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CompositeSyncPipeline>().As<IPipelineExecutor>();

        // Execution order is derived from pipeline dependencies via topological sort in
        // CompositeSyncPipeline, not registration order. See IPipelineMetadata.Dependencies.
        builder
            .RegisterTypes(
                typeof(GenericSyncPipeline<CustomFormatPipelineContext>),
                typeof(GenericSyncPipeline<QualityProfilePipelineContext>),
                typeof(GenericSyncPipeline<QualitySizePipelineContext>),
                typeof(GenericSyncPipeline<MediaNamingPipelineContext>),
                typeof(GenericSyncPipeline<MediaManagementPipelineContext>)
            )
            .As<ISyncPipeline>();

        RegisterPlan(builder);
        RegisterQualityProfile(builder);
        RegisterQualitySize(builder);
        RegisterCustomFormat(builder);
        RegisterMediaNaming(builder);
        RegisterMediaManagement(builder);
    }

    private static void RegisterPlan(ContainerBuilder builder)
    {
        builder.RegisterType<PlanBuilder>();
        builder.RegisterType<DiagnosticsRenderer>();
        builder.RegisterType<ExplicitCfGroupValidator>().As<IValidator<CustomFormatGroupConfig>>();

        // ORDER HERE IS IMPORTANT!
        // CF must run before QP (QP references CFs from plan)
        builder
            .RegisterTypes(
                typeof(CustomFormatPlanComponent),
                typeof(QualityProfilePlanComponent),
                typeof(QualitySizePlanComponent),
                typeof(MediaNamingPlanComponent),
                typeof(MediaManagementPlanComponent)
            )
            .As<IPlanComponent>()
            .OrderByRegistration();
    }

    private static void RegisterMediaNaming(ContainerBuilder builder)
    {
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
            .RegisterType<QualityProfileStatePersister>()
            .As<ISyncStatePersister<QualityProfileMappings>>();

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
        builder.RegisterType<ConfiguredCustomFormatProvider>().InstancePerLifetimeScope();
        builder.RegisterType<CategorizedCustomFormatProvider>();
        builder
            .RegisterType<CustomFormatStatePersister>()
            .As<ISyncStatePersister<CustomFormatMappings>>();
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

    private static void RegisterMediaManagement(ContainerBuilder builder)
    {
        builder
            .RegisterTypes(
                typeof(MediaManagementApiFetchPhase),
                typeof(MediaManagementTransactionPhase),
                typeof(MediaManagementPreviewPhase),
                typeof(MediaManagementApiPersistencePhase)
            )
            .AsImplementedInterfaces()
            .OrderByRegistration();
    }
}
