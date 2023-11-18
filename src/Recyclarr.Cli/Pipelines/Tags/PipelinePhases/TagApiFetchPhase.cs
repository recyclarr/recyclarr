using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Tag;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagApiFetchPhase(ISonarrTagApiService api, ServiceTagCache cache)
{
    public async Task<IList<SonarrTag>> Execute(IServiceConfiguration config)
    {
        var tags = await api.GetTags(config);
        cache.Clear();
        cache.AddTags(tags);
        return tags;
    }
}
