using Recyclarr.Cli.Pipelines.Generic;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatLogPhase(CustomFormatTransactionLogger cfLogger, ILogger log)
    : ILogPipelinePhase<CustomFormatPipelineContext>
{
    // Returning 'true' means to exit. 'false' means to proceed.
    public bool LogConfigPhaseAndExitIfNeeded(CustomFormatPipelineContext context)
    {
        if (context.InvalidFormats.Count != 0)
        {
            log.Warning(
                "These Custom Formats do not exist in the guide and will be skipped: {Cfs}",
                context.InvalidFormats
            );
        }

        // Do not exit when the config has zero custom formats. We still may need to delete old custom formats.
        return false;
    }

    public void LogTransactionNotices(CustomFormatPipelineContext context) { }

    public void LogPersistenceResults(CustomFormatPipelineContext context)
    {
        cfLogger.LogTransactions(context);
    }
}
