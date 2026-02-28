using Autofac;
using Autofac.Extras.Ordering;
using FluentValidation;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.Cli.Pipelines.MediaManagement;
using Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming.Radarr;
using Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;
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
                typeof(GenericSyncPipeline<SonarrNamingPipelineContext>),
                typeof(GenericSyncPipeline<RadarrNamingPipelineContext>),
                typeof(GenericSyncPipeline<MediaManagementPipelineContext>)
            )
            .As<ISyncPipeline>();

        RegisterPlan(builder);
        RegisterQualityProfile(builder);
        RegisterQualitySize(builder);
        RegisterCustomFormat(builder);
        RegisterSonarrMediaNaming(builder);
        RegisterRadarrMediaNaming(builder);
        RegisterMediaManagement(builder);
    }

    private static void RegisterPlan(ContainerBuilder builder)
    {
        builder.RegisterType<PlanBuilder>();
        builder.RegisterType<ExplicitCfGroupValidator>().As<IValidator<CustomFormatGroupConfig>>();

        // ORDER HERE IS IMPORTANT!
        // CF must run before QP (QP references CFs from plan)
        builder
            .RegisterTypes(
                typeof(CustomFormatPlanComponent),
                typeof(QualityProfilePlanComponent),
                typeof(QualitySizePlanComponent),
                typeof(SonarrMediaNamingPlanComponent),
                typeof(RadarrMediaNamingPlanComponent),
                typeof(MediaManagementPlanComponent)
            )
            .As<IPlanComponent>()
            .OrderByRegistration();
    }

    private static void RegisterSonarrMediaNaming(ContainerBuilder builder)
    {
        builder
            .RegisterTypes(
                typeof(SonarrNamingApiFetchPhase),
                typeof(SonarrNamingTransactionPhase),
                typeof(SonarrNamingPreviewPhase),
                typeof(SonarrNamingApiPersistencePhase)
            )
            .AsImplementedInterfaces()
            .OrderByRegistration();
    }

    private static void RegisterRadarrMediaNaming(ContainerBuilder builder)
    {
        builder
            .RegisterTypes(
                typeof(RadarrNamingApiFetchPhase),
                typeof(RadarrNamingTransactionPhase),
                typeof(RadarrNamingPreviewPhase),
                typeof(RadarrNamingApiPersistencePhase)
            )
            .AsImplementedInterfaces()
            .OrderByRegistration();
    }

    private static void RegisterQualityProfile(ContainerBuilder builder)
    {
        builder.RegisterType<QualityProfileStatCalculator>();
        builder.RegisterType<QualityProfileLogger>();
        builder.RegisterType<QualityProfileStatePersister>().As<IQualityProfileStatePersister>();

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
        builder.RegisterType<CustomFormatStatePersister>().As<ICustomFormatStatePersister>();
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
