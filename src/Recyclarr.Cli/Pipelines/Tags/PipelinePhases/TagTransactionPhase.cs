using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagTransactionPhase : ITransactionPipelinePhase<TagPipelineContext>
{
    public void Execute(TagPipelineContext context, IServiceConfiguration config)
    {
        // List of tags in config that do not already exist in the service. The goal is to figure out which tags need to
        // be created.
        context.TransactionOutput = context.ConfigOutput
            .Where(ct => context.ApiFetchOutput.All(st => !st.Label.EqualsIgnoreCase(ct)))
            .ToList();
    }
}
