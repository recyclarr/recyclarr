using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;

namespace TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public class CustomFormatTransactionData
{
    public Collection<ProcessedCustomFormatData> NewCustomFormats { get; } = new();
    public Collection<ProcessedCustomFormatData> UpdatedCustomFormats { get; } = new();
    public Collection<TrashIdMapping> DeletedCustomFormatIds { get; } = new();
    public Collection<ProcessedCustomFormatData> UnchangedCustomFormats { get; } = new();
}

internal class JsonTransactionStep : IJsonTransactionStep
{
    public CustomFormatTransactionData Transactions { get; } = new();

    public void Process(IEnumerable<ProcessedCustomFormatData> guideCfs,
        IReadOnlyCollection<JObject> serviceCfs)
    {
        foreach (var (guideCf, serviceCf) in guideCfs
                     .Select(gcf => (GuideCf: gcf, ServiceCf: FindServiceCf(serviceCfs, gcf))))
        {
            var guideCfJson = BuildNewServiceCf(guideCf.Json);

            // no match; we add this CF as brand new
            if (serviceCf == null)
            {
                guideCf.Json = guideCfJson;
                Transactions.NewCustomFormats.Add(guideCf);
            }
            // found match in radarr CFs; update the existing CF
            else
            {
                guideCf.Json = (JObject) serviceCf.DeepClone();
                UpdateServiceCf(guideCf.Json, guideCfJson);

                // Set the cache for use later (like updating scores) if it hasn't been updated already.
                // This handles CFs that already exist in Radarr but aren't cached (they will be added to cache
                // later).
                if (guideCf.CacheEntry == null)
                {
                    guideCf.SetCache(guideCf.Json.Value<int>("id"));
                }

                if (!JToken.DeepEquals(serviceCf, guideCf.Json))
                {
                    Transactions.UpdatedCustomFormats.Add(guideCf);
                }
                else
                {
                    Transactions.UnchangedCustomFormats.Add(guideCf);
                }
            }
        }
    }

    public void RecordDeletions(IEnumerable<TrashIdMapping> deletedCfsInCache, IEnumerable<JObject> serviceCfs)
    {
        var cfs = serviceCfs.ToList();

        // The 'Where' excludes cached CFs that were deleted manually by the user in Radarr
        // FindRadarrCf() specifies 'null' for name because we should never delete unless an ID is found
        foreach (var del in deletedCfsInCache.Where(
                     del => FindServiceCf(cfs, del.CustomFormatId) != null))
        {
            Transactions.DeletedCustomFormatIds.Add(del);
        }
    }

    private static JObject? FindServiceCf(IReadOnlyCollection<JObject> serviceCfs, ProcessedCustomFormatData guideCf)
    {
        return FindServiceCf(serviceCfs, guideCf.CacheEntry?.CustomFormatId);
    }

    private static JObject? FindServiceCf(IReadOnlyCollection<JObject> serviceCfs, int? cfId)
    {
        JObject? match = null;

        // Try to find match in cache first
        if (cfId is not null)
        {
            match = serviceCfs.FirstOrDefault(rcf => cfId == rcf.Value<int>("id"));
        }

        return match;
    }

    private static void UpdateServiceCf(JObject cfToModify, JObject cfToMergeFrom)
    {
        MergeProperties(cfToModify, cfToMergeFrom, JTokenType.Array);

        var serviceSpecs = cfToModify["specifications"]?.Children<JObject>() ?? new JEnumerable<JObject>();
        var guideSpecs = cfToMergeFrom["specifications"]?.Children<JObject>() ?? new JEnumerable<JObject>();

        var matchedGuideSpecs = guideSpecs
            .GroupBy(gs => serviceSpecs.FirstOrDefault(gss => KeyMatch(gss, gs, "name")))
            .SelectMany(kvp => kvp.Select(gs => new {GuideSpec = gs, ServiceSpec = kvp.Key}));

        var newServiceSpecs = new JArray();

        foreach (var match in matchedGuideSpecs)
        {
            if (match.ServiceSpec != null)
            {
                MergeProperties(match.ServiceSpec, match.GuideSpec);
                newServiceSpecs.Add(match.ServiceSpec);
            }
            else
            {
                newServiceSpecs.Add(match.GuideSpec);
            }
        }

        cfToModify["specifications"] = newServiceSpecs;
    }

    private static bool KeyMatch(JObject left, JObject right, string keyName)
        => left.Value<string>(keyName) == right.Value<string>(keyName);

    private static void MergeProperties(JObject serviceCf, JObject guideCfJson,
        JTokenType exceptType = JTokenType.None)
    {
        foreach (var guideProp in guideCfJson.Properties().Where(p => p.Value.Type != exceptType))
        {
            if (guideProp.Value.Type == JTokenType.Array &&
                serviceCf.TryGetValue(guideProp.Name, out var serviceArray))
            {
                ((JArray) serviceArray).Merge(guideProp.Value, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Merge
                });
            }
            else
            {
                serviceCf[guideProp.Name] = guideProp.Value;
            }
        }
    }

    private static JObject BuildNewServiceCf(JObject jsonPayload)
    {
        // Information on required fields from nitsua
        /*
            ok, for the specs.. you need name, implementation, negate, required, fields
            for fields you need name & value
            top level you need name, includeCustomFormatWhenRenaming, specs and id (if updating)
            everything else radarr can handle with backend logic
         */

        var specs = jsonPayload["specifications"];
        if (specs is not null)
        {
            foreach (var child in specs)
            {
                // convert from `"fields": {}` to `"fields": [{}]` (object to array of object)
                // Weirdly the exported version of a custom format is not in array form, but the API requires the array
                // even if there's only one element.
                var field = child["fields"];
                if (field is null)
                {
                    continue;
                }

                field["name"] = "value";
                child["fields"] = new JArray {field};
            }
        }

        return jsonPayload;
    }
}
