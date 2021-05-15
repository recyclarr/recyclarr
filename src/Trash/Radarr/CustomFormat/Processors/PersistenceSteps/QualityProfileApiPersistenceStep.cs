using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Trash.Extensions;
using Trash.Radarr.CustomFormat.Api;
using Trash.Radarr.CustomFormat.Models;

namespace Trash.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public class QualityProfileApiPersistenceStep : IQualityProfileApiPersistenceStep
    {
        private readonly List<string> _invalidProfileNames = new();
        private readonly Dictionary<string, List<QualityProfileCustomFormatScoreEntry>> _updatedScores = new();

        public IDictionary<string, List<QualityProfileCustomFormatScoreEntry>> UpdatedScores => _updatedScores;
        public IReadOnlyCollection<string> InvalidProfileNames => _invalidProfileNames;

        public async Task Process(IQualityProfileService api,
            IDictionary<string, List<QualityProfileCustomFormatScoreEntry>> cfScores)
        {
            var radarrProfiles = (await api.GetQualityProfiles())
                .Select(p => (Name: p["name"].ToString(), Json: p))
                .ToList();

            var profileScores = cfScores
                .GroupJoin(radarrProfiles,
                    s => s.Key,
                    p => p.Name,
                    (s, pList) => (s.Key, s.Value,
                        pList.SelectMany(p => p.Json["formatItems"].Children<JObject>()).ToList()),
                    StringComparer.InvariantCultureIgnoreCase);

            foreach (var (profileName, scoreList, jsonList) in profileScores)
            {
                if (jsonList.Count == 0)
                {
                    _invalidProfileNames.Add(profileName);
                    continue;
                }

                foreach (var (score, json) in scoreList
                    .Select(s => (s, FindJsonScoreEntry(s, jsonList)))
                    .Where(p => p.Item2 != null))
                {
                    var currentScore = (int) json!["score"];
                    if (currentScore == score.Score)
                    {
                        continue;
                    }

                    json!["score"] = score.Score;
                    _updatedScores.GetOrCreate(profileName).Add(score);
                }

                if (!_updatedScores.TryGetValue(profileName, out var updatedScores) || updatedScores.Count == 0)
                {
                    // No scores to update, so don't bother with the API call
                    continue;
                }

                var jsonRoot = (JObject) jsonList.First().Root;
                await api.UpdateQualityProfile(jsonRoot, (int) jsonRoot["id"]);
            }
        }

        private static JObject? FindJsonScoreEntry(QualityProfileCustomFormatScoreEntry score,
            IEnumerable<JObject> jsonList)
        {
            return jsonList.FirstOrDefault(j
                => score.CustomFormat.CacheEntry != null &&
                   (int) j["format"] == score.CustomFormat.CacheEntry.CustomFormatId);
        }
    }
}
