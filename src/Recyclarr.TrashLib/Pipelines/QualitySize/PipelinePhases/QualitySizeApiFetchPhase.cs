using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.QualitySize.Api;

namespace Recyclarr.TrashLib.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiFetchPhase
{
    private readonly IQualityDefinitionService _api;

    public QualitySizeApiFetchPhase(IQualityDefinitionService api)
    {
        _api = api;
    }

    public async Task<IList<ServiceQualityDefinitionItem>> Execute(IServiceConfiguration config)
    {
        return await _api.GetQualityDefinition(config);
    }
}
