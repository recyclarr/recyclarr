using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiFetchPhase
{
    private readonly IQualityDefinitionApiService _api;

    public QualitySizeApiFetchPhase(IQualityDefinitionApiService api)
    {
        _api = api;
    }

    public async Task<IList<ServiceQualityDefinitionItem>> Execute(IServiceConfiguration config)
    {
        return await _api.GetQualityDefinition(config);
    }
}
