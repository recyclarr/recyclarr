using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeApiFetchPhase(IQualityDefinitionApiService api)
    : IPipelinePhase<QualitySizePipelineContext>
{
    public async Task<bool> Execute(QualitySizePipelineContext context, CancellationToken ct)
    {
        context.ApiFetchOutput = await api.GetQualityDefinition(ct);
        return true;
    }
}
