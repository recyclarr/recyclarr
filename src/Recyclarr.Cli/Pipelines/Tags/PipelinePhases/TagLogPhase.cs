using Recyclarr.Cli.Pipelines.Generic;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagLogPhase(ILogger log) : ILogPipelinePhase<TagPipelineContext>
{
    public bool LogConfigPhaseAndExitIfNeeded(TagPipelineContext context)
    {
        if (!context.ConfigOutput.Any())
        {
            log.Debug("No tags to process");
            return true;
        }

        return false;
    }

    public void LogPersistenceResults(TagPipelineContext context)
    {
        if (context.TransactionOutput.Any())
        {
            log.Information("Created {Count} Tags: {Tags}",
                context.TransactionOutput.Count,
                context.TransactionOutput);
        }
        else
        {
            log.Information("All tags are up to date!");
        }
    }
}
