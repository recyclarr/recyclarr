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

        if (context.ConfigOutput is not {Qualities.Count: > 0})
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
        var qualityDefinitionName = context.ConfigOutput!.Type;

        var totalCount = context.TransactionOutput.Count;
        if (totalCount > 0)
        {
            log.Information("Total of {Count} sizes were synced for quality definition {Name}", totalCount,
                qualityDefinitionName);
        }
        else
        {
            log.Information("All sizes for quality definition {Name} are already up to date!", qualityDefinitionName);
        }
    }
}
