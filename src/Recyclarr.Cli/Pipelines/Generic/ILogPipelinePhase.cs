namespace Recyclarr.Cli.Pipelines.Generic;

public interface ILogPipelinePhase<in TContext>
    where TContext : IPipelineContext
{
    bool LogConfigPhaseAndExitIfNeeded(TContext context);
    void LogPersistenceResults(TContext context);
}
