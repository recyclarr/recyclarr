using System;
using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Newtonsoft.Json.Linq;
using TrashLib.Config;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Cache;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public record CachedCustomFormat(ProcessedCustomFormatData CustomFormat, int Id);

    public class CustomFormatTransactionData
    {
        public List<ProcessedCustomFormatData> NewCustomFormats { get; } = new();
        public List<CachedCustomFormat> UpdatedCustomFormats { get; } = new();
        public List<TrashIdMapping> DeletedCustomFormatIds { get; } = new();
        public List<ProcessedCustomFormatData> UnchangedCustomFormats { get; } = new();
    }

    internal class JsonTransactionStep : IJsonTransactionStep
    {
        private readonly Func<IServiceConfiguration, ICustomFormatCache> _cacheFactory;

        public JsonTransactionStep(Func<IServiceConfiguration, ICustomFormatCache> cacheFactory)
        {
            _cacheFactory = cacheFactory;
        }

        public CustomFormatTransactionData Transactions { get; } = new();

        public void Process(
            IEnumerable<ProcessedCustomFormatData> guideCfs,
            IReadOnlyCollection<JObject> radarrCfs,
            RadarrConfig config)
        {
            var cache = _cacheFactory(config);

            foreach (var guideCf in guideCfs)
            {
                var mapping = cache.Mappings.FirstOrDefault(m => m.TrashId == guideCf.TrashId);
                var radarrCf = FindRadarrCf(radarrCfs, mapping?.CustomFormatId, guideCf.Name);
                var guideCfJson = BuildNewRadarrCf(guideCf.Json);

                // no match; we add this CF as brand new
                if (radarrCf == null)
                {
                    guideCf.Json = guideCfJson;
                    Transactions.NewCustomFormats.Add(guideCf);
                }
                // found match in radarr CFs; update the existing CF
                else
                {
                    guideCf.Json = (JObject) radarrCf.DeepClone();
                    UpdateRadarrCf(guideCf.Json, guideCfJson);

                    if (!JToken.DeepEquals(radarrCf, guideCf.Json))
                    {
                        Transactions.UpdatedCustomFormats.Add(
                            new CachedCustomFormat(guideCf, (int) guideCf.Json["id"]));
                    }
                    else
                    {
                        Transactions.UnchangedCustomFormats.Add(guideCf);
                    }
                }
            }
        }

        public void RecordDeletions(IEnumerable<ProcessedCustomFormatData> guideCfs,
            List<RadarrCustomFormatData> radarrCfs, RadarrConfig config)
        {
            var cache = _cacheFactory(config);

            foreach (var mapping in cache.Mappings
                .Where(m => guideCfs.None(cf => cf.TrashId == m.TrashId)))
            {
                // The 'Where' excludes cached CFs that were deleted manually by the user in Radarr
                var radarrCf = radarrCfs.FirstOrDefault(cf => cf.Id == mapping.CustomFormatId);
                if (radarrCf != null)
                {
                    Transactions.DeletedCustomFormatIds.Add(mapping);
                }
            }
        }

        private static JObject? FindRadarrCf(IReadOnlyCollection<JObject> radarrCfs, int? cfId, string? cfName)
        {
            JObject? match = null;

            // Try to find match in cache first
            if (cfId != null)
            {
                match = radarrCfs.FirstOrDefault(rcf => cfId == rcf["id"].Value<int>());
            }

            // If we don't find by ID, search by name (if a name was given)
            if (match == null && cfName != null)
            {
                match = radarrCfs.FirstOrDefault(rcf => cfName.EqualsIgnoreCase(rcf["name"].Value<string>()));
            }

            return match;
        }

        private static void UpdateRadarrCf(JObject cfToModify, JObject cfToMergeFrom)
        {
            MergeProperties(cfToModify, cfToMergeFrom, JTokenType.Array);

            var radarrSpecs = cfToModify["specifications"].Children<JObject>();
            var guideSpecs = cfToMergeFrom["specifications"].Children<JObject>();

            var matchedGuideSpecs = guideSpecs
                .GroupBy(gs => radarrSpecs.FirstOrDefault(gss => KeyMatch(gss, gs, "name")))
                .SelectMany(kvp => kvp.Select(gs => new {GuideSpec = gs, RadarrSpec = kvp.Key}));

            var newRadarrSpecs = new JArray();

            foreach (var match in matchedGuideSpecs)
            {
                if (match.RadarrSpec != null)
                {
                    MergeProperties(match.RadarrSpec, match.GuideSpec);
                    newRadarrSpecs.Add(match.RadarrSpec);
                }
                else
                {
                    newRadarrSpecs.Add(match.GuideSpec);
                }
            }

            cfToModify["specifications"] = newRadarrSpecs;
        }

        private static bool KeyMatch(JObject left, JObject right, string keyName)
            => left[keyName].Value<string>() == right[keyName].Value<string>();

        private static void MergeProperties(JObject radarrCf, JObject guideCfJson,
            JTokenType exceptType = JTokenType.None)
        {
            foreach (var guideProp in guideCfJson.Properties().Where(p => p.Value.Type != exceptType))
            {
                if (guideProp.Value.Type == JTokenType.Array &&
                    radarrCf.TryGetValue(guideProp.Name, out var radarrArray))
                {
                    ((JArray) radarrArray).Merge(guideProp.Value, new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Merge
                    });
                }
                else
                {
                    radarrCf[guideProp.Name] = guideProp.Value;
                }
            }
        }

        private static JObject BuildNewRadarrCf(JObject jsonPayload)
        {
            // Information on required fields from nitsua
            /*
                ok, for the specs.. you need name, implementation, negate, required, fields
                for fields you need name & value
                top level you need name, includeCustomFormatWhenRenaming, specs and id (if updating)
                everything else radarr can handle with backend logic
             */

            foreach (var child in jsonPayload["specifications"])
            {
                // convert from `"fields": {}` to `"fields": [{}]` (object to array of object)
                // Weirdly the exported version of a custom format is not in array form, but the API requires the array
                // even if there's only one element.
                var field = child["fields"];
                field["name"] = "value";
                child["fields"] = new JArray {field};
            }

            return jsonPayload;
        }
    }
}
