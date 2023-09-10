using Recyclarr.Cli.Pipelines.QualitySize.Api;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

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
