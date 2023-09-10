using Recyclarr.Cli.Pipelines.Tags.Api;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagApiFetchPhase
{
    private readonly ISonarrTagApiService _api;
    private readonly ServiceTagCache _cache;

    public TagApiFetchPhase(ISonarrTagApiService api, ServiceTagCache cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<IList<SonarrTag>> Execute(IServiceConfiguration config)
    {
        var tags = await _api.GetTags(config);
        _cache.Clear();
        _cache.AddTags(tags);
        return tags;
    }
}
