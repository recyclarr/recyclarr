using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagConfigPhase : IConfigPipelinePhase<TagPipelineContext>
{
    public Task Execute(TagPipelineContext context, IServiceConfiguration config)
    {
        context.ConfigOutput = ((SonarrConfiguration) config).ReleaseProfiles
            .SelectMany(x => x.Tags)
            .Distinct()
            .ToList();

        return Task.CompletedTask;
    }
}
