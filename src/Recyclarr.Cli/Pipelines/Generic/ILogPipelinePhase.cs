namespace Recyclarr.Cli.Pipelines.Generic;

public interface ILogPipelinePhase<in TContext>
{
    bool LogConfigPhaseAndExitIfNeeded(TContext context);
    void LogPersistenceResults(TContext context);
}