using Recyclarr.Cli.Pipelines.Generic;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatPreviewPhase(CustomFormatTransactionLogger logger)
    : IPreviewPipelinePhase<CustomFormatPipelineContext>
{
    public void Execute(CustomFormatPipelineContext context)
    {
        logger.LogTransactions(context);
    }
}
