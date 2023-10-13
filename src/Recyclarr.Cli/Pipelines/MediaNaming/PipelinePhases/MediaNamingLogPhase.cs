using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingLogPhase(ILogger log) : ILogPipelinePhase<MediaNamingPipelineContext>
{
    // Returning 'true' means to exit. 'false' means to proceed.
    public bool LogConfigPhaseAndExitIfNeeded(MediaNamingPipelineContext context)
    {
        var config = context.ConfigOutput;

        if (config.InvalidNaming.Count != 0)
        {
            foreach (var (topic, invalidValue) in config.InvalidNaming)
            {
                log.Error("An invalid media naming format is specified for {Topic}: {Value}", topic, invalidValue);
            }

            return true;
        }

        var differences = config.Dto switch
        {
            RadarrMediaNamingDto x => x.GetDifferences(new RadarrMediaNamingDto()),
            SonarrMediaNamingDto x => x.GetDifferences(new SonarrMediaNamingDto()),
            _ => throw new ArgumentException("Unsupported configuration type in LogConfigPhase method")
        };

        if (differences.Count == 0)
        {
            log.Debug("No media naming changes to process");
            return true;
        }

        return false;
    }

    public void LogPersistenceResults(MediaNamingPipelineContext context)
    {
        var differences = context.ApiFetchOutput switch
        {
            RadarrMediaNamingDto x => x.GetDifferences(context.TransactionOutput),
            SonarrMediaNamingDto x => x.GetDifferences(context.TransactionOutput),
            _ => throw new ArgumentException("Unsupported configuration type in LogPersistenceResults method")
        };

        if (differences.Count != 0)
        {
            log.Information("Media naming has been updated");
            log.Debug("Naming differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media naming is up to date!");
        }
    }
}
