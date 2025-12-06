using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeApiFetchPhase(
    IQualityDefinitionApiService api,
    IQualityItemLimitFactory limitFactory,
    IServiceConfiguration config
) : IPipelinePhase<QualitySizePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualitySizePipelineContext context,
        CancellationToken ct
    )
    {
        context.ApiFetchOutput = await api.GetQualityDefinition(ct);
        context.Limits = await limitFactory.Create(config.ServiceType, ct);
        return PipelineFlow.Continue;
    }
}
