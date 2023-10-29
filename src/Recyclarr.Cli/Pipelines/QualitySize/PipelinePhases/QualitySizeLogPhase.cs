using Recyclarr.Cli.Pipelines.Generic;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeLogPhase(ILogger log) : ILogPipelinePhase<QualitySizePipelineContext>
{
    public bool LogConfigPhaseAndExitIfNeeded(QualitySizePipelineContext context)
    {
        if (context.ConfigError is not null)
        {
            log.Error(context.ConfigError);
            return true;
        }

        if (context.ConfigOutput is null)
        {
            log.Debug("No Quality Definitions to process");
            return true;
        }

        return false;
    }

    public void LogTransactionNotices(QualitySizePipelineContext context)
    {
    }

    public void LogPersistenceResults(QualitySizePipelineContext context)
    {
        // Do not check ConfigOutput for null here since that is done for us in the LogConfigPhase method
        log.Information("Processed Quality Definition: {QualityDefinition}", context.ConfigOutput!.Type);
    }
}
