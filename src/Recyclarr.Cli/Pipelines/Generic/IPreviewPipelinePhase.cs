namespace Recyclarr.Cli.Pipelines.Generic;

public interface IPreviewPipelinePhase<in TContext>
    where TContext : IPipelineContext
{
    void Execute(TContext context);
}
