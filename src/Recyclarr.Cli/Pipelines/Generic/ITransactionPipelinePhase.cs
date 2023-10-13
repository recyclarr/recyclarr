namespace Recyclarr.Cli.Pipelines.Generic;

public interface ITransactionPipelinePhase<in TContext>
{
    void Execute(TContext context);
}