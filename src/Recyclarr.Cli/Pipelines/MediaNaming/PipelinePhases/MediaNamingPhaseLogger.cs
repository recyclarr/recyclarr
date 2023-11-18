using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingPhaseLogger(ILogger log)
{
    // Returning 'true' means to exit. 'false' means to proceed.
    public bool LogConfigPhaseAndExitIfNeeded(ProcessedNamingConfig config)
    {
        if (config.InvalidNaming.Any())
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

        if (!differences.Any())
        {
            log.Debug("No media naming changes to process");
            return true;
        }

        return false;
    }

    public void LogPersistenceResults(MediaNamingDto oldDto, MediaNamingDto newDto)
    {
        var differences = oldDto switch
        {
            RadarrMediaNamingDto x => x.GetDifferences(newDto),
            SonarrMediaNamingDto x => x.GetDifferences(newDto),
            _ => throw new ArgumentException("Unsupported configuration type in LogPersistenceResults method")
        };

        if (differences.Any())
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
