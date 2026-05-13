using Autofac;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming.Radarr;
using Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Cli.Pipelines;

internal class PipelineAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<Recyclarr.Pipelines.PipelineAutofacModule>();

        builder.RegisterType<CompositeSyncPipeline>().As<IPipelineExecutor>();

        builder
            .RegisterType<CustomFormatPreviewRenderer>()
            .As<IPreviewRenderer<CustomFormatPreviewData>>();
        builder
            .RegisterType<QualityProfilePreviewRenderer>()
            .As<IPreviewRenderer<QualityProfileTransactionData>>();
        builder
            .RegisterType<QualitySizePreviewRenderer>()
            .As<IPreviewRenderer<QualitySizePreviewData>>();
        builder
            .RegisterType<SonarrNamingPreviewRenderer>()
            .As<IPreviewRenderer<SonarrNamingData>>();
        builder
            .RegisterType<RadarrNamingPreviewRenderer>()
            .As<IPreviewRenderer<RadarrNamingData>>();
        builder
            .RegisterType<MediaManagementPreviewRenderer>()
            .As<IPreviewRenderer<MediaManagementData>>();
    }
}
