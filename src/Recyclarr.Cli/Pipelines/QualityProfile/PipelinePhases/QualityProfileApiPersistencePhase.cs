using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public class QualityProfileApiPersistencePhase
{
    private readonly ILogger _log;
    private readonly IQualityProfileService _api;

    public QualityProfileApiPersistencePhase(ILogger log, IQualityProfileService api)
    {
        _log = log;
        _api = api;
    }

    public async Task Execute(IServiceConfiguration config, QualityProfileTransactionData transactions)
    {
        foreach (var profile in transactions.UpdatedProfiles.Select(x => x.UpdatedProfile))
        {
            await _api.UpdateQualityProfile(config, profile);
        }

        LogQualityProfileUpdates(transactions);
    }

    private void LogQualityProfileUpdates(QualityProfileTransactionData transactions)
    {
        var updatedScores = transactions.UpdatedProfiles
            .Select(x => (x.UpdatedProfile.Name, x.UpdatedScores))
            .ToList();

        if (updatedScores.Count > 0)
        {
            foreach (var (profileName, scores) in updatedScores)
            {
                _log.Debug("> Scores updated for quality profile: {ProfileName}", profileName);

                foreach (var (customFormatName, oldScore, newScore, reason) in scores)
                {
                    _log.Debug("  - {Format}: {OldScore} -> {NewScore} ({Reason})",
                        customFormatName, oldScore, newScore, reason);
                }
            }

            _log.Information("Updated {ProfileCount} profiles and a total of {ScoreCount} scores",
                transactions.UpdatedProfiles.Count,
                updatedScores.Sum(s => s.UpdatedScores.Count));
        }
        else
        {
            _log.Information("All quality profile scores are already up to date!");
        }

        if (transactions.InvalidProfileNames.Count > 0)
        {
            _log.Warning("The following quality profile names are not valid and should either be " +
                "removed or renamed in your YAML config");
            _log.Warning("{QualityProfileNames}", transactions.InvalidProfileNames);
        }
    }
}
