using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagTransactionPhase : ITransactionPipelinePhase<TagPipelineContext>
{
    public void Execute(TagPipelineContext context)
    {
        // List of tags in config that do not already exist in the service. The goal is to figure out which tags need to
        // be created.
        context.TransactionOutput = context.ConfigOutput
            .Where(ct => context.ApiFetchOutput.All(st => !st.Label.EqualsIgnoreCase(ct)))
            .ToList();
    }
}
