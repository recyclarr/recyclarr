namespace Recyclarr.Cli.Pipelines.Generic;

public interface IConfigPipelinePhase<in TContext>
    where TContext : IPipelineContext
{
    Task Execute(TContext context, CancellationToken ct);
}
