namespace Recyclarr.Cli.Pipelines;

internal abstract class PreviewPipelinePhase<T> : IPipelinePhase<T>
    where T : PipelineContext
{
    public Task<PipelineFlow> Execute(T context, CancellationToken ct)
    {
        if (!context.SyncSettings.Preview)
        {
            return Task.FromResult(PipelineFlow.Continue);
        }

        RenderPreview(context);

        // A preview acts as a dry run, and so we terminate the pipeline after rendering the preview
        // to prevent persisting changes to remote services.
        return Task.FromResult(PipelineFlow.Terminate);
    }

    protected abstract void RenderPreview(T context);
}
