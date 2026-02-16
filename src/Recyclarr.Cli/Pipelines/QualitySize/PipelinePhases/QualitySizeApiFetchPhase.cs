using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeApiFetchPhase(
    IQualityDefinitionApiService api,
    IQualityItemLimitFactory limitFactory,
    IServiceConfiguration config,
    ILogger log
) : IPipelinePhase<QualitySizePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualitySizePipelineContext context,
        CancellationToken ct
    )
    {
        if (context.Plan.QualitySizes.ResetBeforeSync)
        {
            log.Information("Resetting quality definitions to installation defaults");
            await api.ResetQualityDefinitions(ct);
        }

        context.ApiFetchOutput = await api.GetQualityDefinition(ct);
        context.Limits = await limitFactory.Create(config.ServiceType, ct);
        return PipelineFlow.Continue;
    }
}
