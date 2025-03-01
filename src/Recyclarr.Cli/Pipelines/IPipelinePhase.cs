namespace Recyclarr.Cli.Pipelines;

internal interface IPipelinePhase<in TContext>
{
    Task<bool> Execute(TContext context, CancellationToken ct);
}
