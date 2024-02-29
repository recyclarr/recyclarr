using Recyclarr.Cli.Pipelines.Generic;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatPreviewPhase(ILogger log) : IPreviewPipelinePhase<CustomFormatPipelineContext>
{
    public void Execute(CustomFormatPipelineContext context)
    {
        context.LogTransactions(log);
    }
}
