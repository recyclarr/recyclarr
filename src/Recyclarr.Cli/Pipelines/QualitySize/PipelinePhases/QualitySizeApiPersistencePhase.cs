using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiPersistencePhase
{
    private readonly ILogger _log;
    private readonly IQualityDefinitionApiService _api;

    public QualitySizeApiPersistencePhase(ILogger log, IQualityDefinitionApiService api)
    {
        _log = log;
        _api = api;
    }

    public async Task Execute(IServiceConfiguration config, IList<ServiceQualityDefinitionItem> serverQuality)
    {
        await _api.UpdateQualityDefinition(config, serverQuality);
        _log.Information("Number of updated qualities: {Count}", serverQuality.Count);
    }
}
