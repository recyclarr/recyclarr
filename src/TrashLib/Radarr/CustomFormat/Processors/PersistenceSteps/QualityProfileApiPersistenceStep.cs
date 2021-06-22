using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Extensions;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Api.Models;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps
{
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
                p => p.Name,
                (s, p) => (s.Key, s.Value, p.First()),
                StringComparer.InvariantCultureIgnoreCase);

            foreach (var (profileName, scoreMap, profileData) in profileScores)
            {
                // `SelectMany` is only needed here because we used GroupJoin() above the loop.
                var formatItems = profileData.FormatItems;
                if (formatItems.Count == 0)
                {
                    _invalidProfileNames.Add(profileName);
                    continue;
                }

                foreach (var formatItem in formatItems)
                {
                    var map = FindScoreEntry(formatItem, scoreMap);

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

                    if (scoreToUse == null || reason == null || formatItem.Score == scoreToUse)
                    {
                        continue;
                    }

                    formatItem.Score = scoreToUse.Value;
                    _updatedScores.GetOrCreate(profileName)
                        .Add(new UpdatedFormatScore(formatItem.Name, scoreToUse.Value, reason.Value));
                }

                if (!_updatedScores.TryGetValue(profileName, out var updatedScores) || updatedScores.Count == 0)
                {
                    // No scores to update, so don't bother with the API call
                }

                await api.UpdateQualityProfile(profileData, profileData.Id);
            }
        }

        private static FormatMappingEntry? FindScoreEntry(QualityProfileData.FormatItemData formatItem,
            QualityProfileCustomFormatScoreMapping scoreMap)
        {
            return scoreMap.Mapping.FirstOrDefault(
                m => m.CustomFormat.CacheEntry != null &&
                     formatItem.Format == m.CustomFormat.CacheEntry.CustomFormatId);
        }
    }
}
