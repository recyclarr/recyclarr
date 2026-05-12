using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming.Radarr;
using Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Pipelines.MediaManagement.PipelinePhases;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Pipelines.QualitySize.PipelinePhases;

namespace Recyclarr.Cli.Pipelines;

// Phase registration stays in Cli because OrderByRegistration requires all phases
// (including Cli-only preview phases) to be registered together in execution order.
internal class PipelineAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<Recyclarr.Pipelines.PipelineAutofacModule>();

        builder.RegisterType<CompositeSyncPipeline>().As<IPipelineExecutor>();

        RegisterCustomFormatPhases(builder);
        RegisterQualityProfilePhases(builder);
        RegisterQualitySizePhases(builder);
        RegisterSonarrMediaNamingPhases(builder);
        RegisterRadarrMediaNamingPhases(builder);
        RegisterMediaManagementPhases(builder);
    }

    private static void RegisterCustomFormatPhases(ContainerBuilder builder)
    {
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

    private static void RegisterQualityProfilePhases(ContainerBuilder builder)
    {
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

    private static void RegisterQualitySizePhases(ContainerBuilder builder)
    {
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

    private static void RegisterSonarrMediaNamingPhases(ContainerBuilder builder)
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

    private static void RegisterRadarrMediaNamingPhases(ContainerBuilder builder)
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

    private static void RegisterMediaManagementPhases(ContainerBuilder builder)
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
