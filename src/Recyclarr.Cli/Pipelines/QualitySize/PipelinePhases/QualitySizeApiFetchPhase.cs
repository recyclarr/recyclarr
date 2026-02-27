using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Config.Models;
using Recyclarr.Servarr.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeApiFetchPhase(
    IQualityDefinitionService api,
    IQualityItemLimitFactory limitFactory,
    IServiceConfiguration config
) : IPipelinePhase<QualitySizePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualitySizePipelineContext context,
        CancellationToken ct
    )
    {
        context.ApiFetchOutput = await api.GetQualityDefinitions(ct);
        context.Limits = await limitFactory.Create(config.ServiceType, ct);
        return PipelineFlow.Continue;
    }
}
