using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Tag;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagApiPersistencePhase(ILogger log, ServiceTagCache cache, ISonarrTagApiService api)
    : IApiPersistencePipelinePhase<TagPipelineContext>
{
    public async Task Execute(TagPipelineContext context, IServiceConfiguration config)
    {
        var createdTags = new List<SonarrTag>();
        foreach (var tag in context.TransactionOutput)
        {
            log.Debug("Creating Tag: {Tag}", tag);
            createdTags.Add(await api.CreateTag(config, tag));
        }

        cache.AddTags(createdTags);
    }
}
