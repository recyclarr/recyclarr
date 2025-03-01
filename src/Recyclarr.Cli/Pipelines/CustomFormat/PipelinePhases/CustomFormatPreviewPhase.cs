namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatPreviewPhase(CustomFormatTransactionLogger logger)
    : PreviewPipelinePhase<CustomFormatPipelineContext>
{
    protected override void RenderPreview(CustomFormatPipelineContext context)
    {
        logger.LogTransactions(context);
    }
}
