using Recyclarr.Cli.Pipelines.Tags.Api;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagApiPersistencePhase
{
    private readonly ILogger _log;
    private readonly ServiceTagCache _cache;
    private readonly ISonarrTagApiService _api;

    public TagApiPersistencePhase(
        ILogger log,
        ServiceTagCache cache,
        ISonarrTagApiService api)
    {
        _log = log;
        _cache = cache;
        _api = api;
    }

    public async Task Execute(IServiceConfiguration config, IEnumerable<string> tagsToCreate)
    {
        var createdTags = new List<SonarrTag>();
        foreach (var tag in tagsToCreate)
        {
            _log.Debug("Creating Tag: {Tag}", tag);
            createdTags.Add(await _api.CreateTag(config, tag));
        }

        _cache.AddTags(createdTags);
    }
}
