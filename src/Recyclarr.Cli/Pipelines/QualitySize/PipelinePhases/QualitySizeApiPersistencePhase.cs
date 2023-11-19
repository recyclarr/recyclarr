using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeApiPersistencePhase(ILogger log, IQualityDefinitionApiService api)
{
    public async Task Execute(IServiceConfiguration config, IList<ServiceQualityDefinitionItem> serverQuality)
    {
        await api.UpdateQualityDefinition(config, serverQuality);
        log.Information("Number of updated qualities: {Count}", serverQuality.Count);
    }
}
