namespace Recyclarr.Cli.Pipelines.Generic;

public interface IApiPersistencePipelinePhase<in TContext>
    where TContext : IPipelineContext
{
    Task Execute(TContext context, CancellationToken ct);
}
