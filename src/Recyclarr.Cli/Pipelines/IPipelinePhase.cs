namespace Recyclarr.Cli.Pipelines;

internal interface IPipelinePhase<in TContext>
{
    /// <returns>
    /// true if processing should continue to the next phase, false to stop the pipeline.
    /// </returns>
    Task<PipelineFlow> Execute(TContext context, CancellationToken ct);
}
