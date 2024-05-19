using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiFetchPhase(IQualityDefinitionApiService api)
    : IApiFetchPipelinePhase<QualitySizePipelineContext>
{
    public async Task Execute(QualitySizePipelineContext context)
    {
        context.ApiFetchOutput = await api.GetQualityDefinition();
    }
}
