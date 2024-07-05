using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiPersistencePhase(IQualityDefinitionApiService api)
    : IApiPersistencePipelinePhase<QualitySizePipelineContext>
{
    public async Task Execute(QualitySizePipelineContext context, CancellationToken ct)
    {
        await api.UpdateQualityDefinition(context.TransactionOutput, ct);
    }
}
