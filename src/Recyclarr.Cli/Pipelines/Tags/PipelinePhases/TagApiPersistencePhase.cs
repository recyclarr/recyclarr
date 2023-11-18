using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Tag;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagApiPersistencePhase(
    ILogger log,
    ServiceTagCache cache,
    ISonarrTagApiService api)
{
    public async Task Execute(IServiceConfiguration config, IEnumerable<string> tagsToCreate)
    {
        var createdTags = new List<SonarrTag>();
        foreach (var tag in tagsToCreate)
        {
            log.Debug("Creating Tag: {Tag}", tag);
            createdTags.Add(await api.CreateTag(config, tag));
        }

        cache.AddTags(createdTags);
    }
}
