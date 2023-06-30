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
        var profilesToUpdate = transactions.UpdatedProfiles.Select(x => x.UpdatedProfile with
        {
            FormatItems = x.UpdatedScores.Select(y => y.Dto with {Score = y.NewScore}).ToList()
        });

        foreach (var profile in profilesToUpdate)
        {
            await _api.UpdateQualityProfile(config, profile);
        }

        LogQualityProfileUpdates(transactions);
    }

    private void LogQualityProfileUpdates(QualityProfileTransactionData transactions)
    {
        var updatedScores = transactions.UpdatedProfiles
            .Select(x => (
                ProfileName: x.UpdatedProfile.Name,
                Scores: x.UpdatedScores
                    .Where(y => y.Reason != FormatScoreUpdateReason.New && y.Dto.Score != y.NewScore)
                    .ToList()))
            .Where(x => x.Scores.Any())
            .ToList();

        if (updatedScores.Count > 0)
        {
            foreach (var (profileName, scores) in updatedScores)
            {
                _log.Debug("> Scores updated for quality profile: {ProfileName}", profileName);

                foreach (var (dto, newScore, reason) in scores)
                {
                    _log.Debug("  - {Format} ({Id}): {OldScore} -> {NewScore} ({Reason})",
                        dto.Name, dto.Format, dto.Score, newScore, reason);
                }
            }

            _log.Information("Updated {ProfileCount} profiles and a total of {ScoreCount} scores",
                transactions.UpdatedProfiles.Count,
                updatedScores.Sum(s => s.Scores.Count));
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
