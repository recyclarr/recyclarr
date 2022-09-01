using Common.Extensions;
using Newtonsoft.Json.Linq;
using TrashLib.Services.CustomFormat.Api;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

internal class QualityProfileApiPersistenceStep : IQualityProfileApiPersistenceStep
{
    private readonly List<string> _invalidProfileNames = new();
    private readonly Dictionary<string, List<UpdatedFormatScore>> _updatedScores = new();

    public IDictionary<string, List<UpdatedFormatScore>> UpdatedScores => _updatedScores;
    public IReadOnlyCollection<string> InvalidProfileNames => _invalidProfileNames;

    public async Task Process(IQualityProfileService api,
        IDictionary<string, QualityProfileCustomFormatScoreMapping> cfScores)
    {
        var radarrProfiles = await api.GetQualityProfiles();

        // Match quality profiles in Radarr to ones the user put in their config.
        // For each match, we return a tuple including the list of custom format scores ("formatItems").
        // Using GroupJoin() because we want a LEFT OUTER JOIN so we can list which quality profiles in config
        // do not match profiles in Radarr.
        var profileScores = cfScores.GroupJoin(radarrProfiles,
            s => s.Key,
            p => p.Value<string>("name"),
            (s, p) => (s.Key, s.Value, p.SelectMany(pi => pi.Children<JObject>("formatItems")).ToList()),
            StringComparer.InvariantCultureIgnoreCase);

        foreach (var (profileName, scoreMap, formatItems) in profileScores)
        {
            if (formatItems.Count == 0)
            {
                _invalidProfileNames.Add(profileName);
                continue;
            }

            foreach (var json in formatItems)
            {
                var map = FindScoreEntry(json, scoreMap);

                int? scoreToUse = null;
                FormatScoreUpdateReason? reason = null;

                if (map != null)
                {
                    scoreToUse = map.Score;
                    reason = FormatScoreUpdateReason.Updated;
                }
                else if (scoreMap.ResetUnmatchedScores)
                {
                    scoreToUse = 0;
                    reason = FormatScoreUpdateReason.Reset;
                }

                if (scoreToUse == null || reason == null || json.Value<int>("score") == scoreToUse)
                {
                    continue;
                }

                json["score"] = scoreToUse.Value;
                _updatedScores.GetOrCreate(profileName)
                    .Add(new UpdatedFormatScore(json.ValueOrThrow<string>("name"), scoreToUse.Value, reason.Value));
            }

            if (!_updatedScores.TryGetValue(profileName, out var updatedScores) || updatedScores.Count == 0)
            {
                // No scores to update, so don't bother with the API call
                continue;
            }

            var jsonRoot = (JObject) formatItems.First().Root;
            await api.UpdateQualityProfile(jsonRoot, jsonRoot.Value<int>("id"));
        }
    }

    private static FormatMappingEntry? FindScoreEntry(JObject formatItem,
        QualityProfileCustomFormatScoreMapping scoreMap)
    {
        return scoreMap.Mapping.FirstOrDefault(
            m => m.CustomFormat.CacheEntry != null &&
                 formatItem.Value<int>("format") == m.CustomFormat.CacheEntry.CustomFormatId);
    }
}
