using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiPersistencePhase(ILogger log, IQualityDefinitionApiService api)
    : IApiPersistencePipelinePhase<QualitySizePipelineContext>
{
    public async Task Execute(QualitySizePipelineContext context, CancellationToken ct)
    {
        var sizeData = context.TransactionOutput;
        if (sizeData.Count == 0)
        {
            log.Debug("No size data available to persist; skipping API call");
            return;
        }

        await api.UpdateQualityDefinition(sizeData, ct);
    }
}
