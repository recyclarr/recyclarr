using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeApiFetchPhase(IQualityDefinitionApiService api)
    : IPipelinePhase<QualitySizePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualitySizePipelineContext context,
        CancellationToken ct
    )
    {
        context.ApiFetchOutput = await api.GetQualityDefinition(ct);
        return PipelineFlow.Continue;
    }
}
