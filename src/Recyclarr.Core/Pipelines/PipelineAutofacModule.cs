using Autofac;
using Autofac.Extras.Ordering;
using FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.CustomFormat.State;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.Plan.Components;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualityProfile.State;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines;

internal class PipelineAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ConfiguredCustomFormatProvider>().InstancePerLifetimeScope();

        RegisterPlan(builder);
        RegisterSyncPipelines(builder);
        RegisterCustomFormatSupport(builder);
        RegisterQualityProfileSupport(builder);
        RegisterQualitySizeSupport(builder);
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

    // Execution order is derived from pipeline dependencies via topological sort in
    // CompositeSyncPipeline, not registration order. See IPipelineMetadata.Dependencies.
    private static void RegisterSyncPipelines(ContainerBuilder builder)
    {
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
    }

    private static void RegisterCustomFormatSupport(ContainerBuilder builder)
    {
        builder.RegisterType<CategorizedCustomFormatProvider>();
        builder.RegisterType<CustomFormatStatePersister>().As<ICustomFormatStatePersister>();
        builder.RegisterType<CustomFormatTransactionLogger>();
    }

    private static void RegisterQualityProfileSupport(ContainerBuilder builder)
    {
        builder.RegisterType<QualityProfileStatCalculator>();
        builder.RegisterType<QualityProfileLogger>();
        builder.RegisterType<QualityProfileStatePersister>().As<IQualityProfileStatePersister>();
    }

    private static void RegisterQualitySizeSupport(ContainerBuilder builder)
    {
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
