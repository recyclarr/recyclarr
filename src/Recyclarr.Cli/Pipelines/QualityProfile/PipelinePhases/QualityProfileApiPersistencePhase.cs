using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public class QualityProfileApiPersistencePhase
{
    private readonly ILogger _log;
    private readonly IQualityProfileApiService _api;
    private readonly QualityProfileStatCalculator _statCalculator;

    public QualityProfileApiPersistencePhase(
        ILogger log,
        IQualityProfileApiService api,
        QualityProfileStatCalculator statCalculator)
    {
        _log = log;
        _api = api;
        _statCalculator = statCalculator;
    }

    public async Task Execute(IServiceConfiguration config, QualityProfileTransactionData transactions)
    {
        var profilesWithStats = transactions.UpdatedProfiles
            .Select(x => _statCalculator.Calculate(x))
            .ToLookup(x => x.HasChanges);

        // Profiles without changes (false) get logged
        var unchangedProfiles = profilesWithStats[false].ToList();
        if (unchangedProfiles.Any())
        {
            _log.Debug("These profiles have no changes and will not be persisted: {Profiles}",
                unchangedProfiles.Select(x => x.Profile.ProfileName));
        }

        // Profiles with changes (true) get sent to the service
        var changedProfiles = profilesWithStats[true].ToList();
        foreach (var profile in changedProfiles.Select(x => x.Profile))
        {
            var dto = profile.BuildUpdatedDto();

            switch (profile.UpdateReason)
            {
                case QualityProfileUpdateReason.New:
                    await _api.CreateQualityProfile(config, dto);
                    break;

                case QualityProfileUpdateReason.Changed:
                    await _api.UpdateQualityProfile(config, dto);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported UpdateReason: {profile.UpdateReason}");
            }
        }

        LogUpdates(changedProfiles);
    }

    private void LogUpdates(IReadOnlyCollection<ProfileWithStats> changedProfiles)
    {
        var createdProfiles = changedProfiles
            .Where(x => x.Profile.UpdateReason == QualityProfileUpdateReason.New)
            .Select(x => x.Profile.ProfileName)
            .ToList();

        if (createdProfiles.Count > 0)
        {
            _log.Information("Created {Count} Profiles: {Names}", createdProfiles.Count, createdProfiles);
        }

        var updatedProfiles = changedProfiles
            .Where(x => x.Profile.UpdateReason == QualityProfileUpdateReason.Changed)
            .Select(x => x.Profile.ProfileName)
            .ToList();

        if (updatedProfiles.Count > 0)
        {
            _log.Information("Updated {Count} Profiles: {Names}", updatedProfiles.Count, updatedProfiles);
        }

        if (changedProfiles.Count != 0)
        {
            var numProfiles = changedProfiles.Count;
            var numQuality = changedProfiles.Count(x => x.QualitiesChanged);
            var numScores = changedProfiles.Count(x => x.ScoresChanged);

            _log.Information(
                "A total of {NumProfiles} profiles were synced. {NumQuality} contain quality changes and " +
                "{NumScores} contain updated scores",
                numProfiles, numQuality, numScores);
        }
        else
        {
            _log.Information("All quality profiles are up to date!");
        }
    }
}
