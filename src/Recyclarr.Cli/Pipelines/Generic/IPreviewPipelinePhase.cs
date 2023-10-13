namespace Recyclarr.Cli.Pipelines.Generic;

public interface IPreviewPipelinePhase<in TContext>
{
    void Execute(TContext context);
}