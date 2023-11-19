using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiFetchPhase(IQualityDefinitionApiService api)
{
    public async Task<IList<ServiceQualityDefinitionItem>> Execute(IServiceConfiguration config)
    {
        return await api.GetQualityDefinition(config);
    }
}
