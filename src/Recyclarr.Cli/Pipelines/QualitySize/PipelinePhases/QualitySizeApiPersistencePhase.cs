using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiPersistencePhase(IQualityDefinitionApiService api)
    : IApiPersistencePipelinePhase<QualitySizePipelineContext>
{
    public async Task Execute(QualitySizePipelineContext context, IServiceConfiguration config)
    {
        await api.UpdateQualityDefinition(config, context.TransactionOutput);
    }
}
