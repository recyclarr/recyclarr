using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines;

internal abstract class PreviewPipelinePhase<T>(
    IAnsiConsole console,
    ISyncContextSource contextSource
) : IPipelinePhase<T>
    where T : PipelineContext
{
    protected IAnsiConsole Console => console;

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

    protected void RenderTitle(T context)
    {
        var instanceName = contextSource.Current.InstanceName;
        console.WriteLine();
        console.MarkupLine(
            $"── [bold]{context.PipelineDescription}[/] [red](Preview)[/] [dim][[{instanceName}]][/] ──"
        );
    }

    protected abstract void RenderPreview(T context);
}
