namespace Recyclarr.Cli.Pipelines;

internal abstract class PreviewPipelinePhase<T> : IPipelinePhase<T>
    where T : PipelineContext
{
    public Task<bool> Execute(T context, CancellationToken ct)
    {
        if (!context.SyncSettings.Preview)
        {
            return Task.FromResult(true);
        }

        RenderPreview(context);
        return Task.FromResult(false);
    }

    protected abstract void RenderPreview(T context);
}
