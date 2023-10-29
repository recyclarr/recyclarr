using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Tag;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagApiFetchPhase(ISonarrTagApiService api, ServiceTagCache cache)
    : IApiFetchPipelinePhase<TagPipelineContext>
{
    public async Task Execute(TagPipelineContext context, IServiceConfiguration config)
    {
        var tags = await api.GetTags(config);
        cache.AddTags(tags);
        context.ApiFetchOutput = tags;
    }
}
