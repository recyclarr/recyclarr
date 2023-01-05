using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public class CustomFormatTransactionData
{
    public Collection<ProcessedCustomFormatData> NewCustomFormats { get; } = new();
    public Collection<ProcessedCustomFormatData> UpdatedCustomFormats { get; } = new();
    public Collection<TrashIdMapping> DeletedCustomFormatIds { get; } = new();
    public Collection<ProcessedCustomFormatData> UnchangedCustomFormats { get; } = new();
    public Collection<ConflictingCustomFormat> ConflictingCustomFormats { get; } = new();
}

public class JsonTransactionStep : IJsonTransactionStep
{
    public CustomFormatTransactionData Transactions { get; } = new();

    public void Process(
        IEnumerable<ProcessedCustomFormatData> guideCfs,
        IReadOnlyCollection<JObject> serviceCfs)
    {
        foreach (var (guideCf, serviceCf) in guideCfs.Select(gcf => (gcf, FindServiceCf(serviceCfs, gcf))))
        {
            var guideCfJson = BuildNewServiceCf(guideCf.Json);

            // no match; we add this CF as brand new
            if (serviceCf == null)
            {
                guideCf.Json = guideCfJson;
                Transactions.NewCustomFormats.Add(guideCf);
                continue;
            }

            // If cache entry is NOT null, that means we found the service by its ID
            if (guideCf.CacheEntry is not null)
            {
                // Check for conflicts with upstream CFs with the same name but different ID.
                // If found, it is recorded and we skip this CF.
                if (DetectConflictingCustomFormats(serviceCfs, guideCf, serviceCf))
                {
                    continue;
                }
            }
            // Null cache entry use case
            else
            {
                // Set the cache for use later (like updating scores) if it hasn't been updated already.
                // This handles CFs that already exist in the service but aren't cached (they will be added to cache
                // later).
                guideCf.SetCache(guideCf.Json.Value<int>("id"));
            }

            guideCf.Json = (JObject) serviceCf.DeepClone();
            UpdateServiceCf(guideCf.Json, guideCfJson);

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

    private bool DetectConflictingCustomFormats(
        IReadOnlyCollection<JObject> serviceCfs,
        ProcessedCustomFormatData guideCf,
        JObject serviceCf)
    {
        var conflictingServiceCf = FindServiceCf(serviceCfs, null, guideCf.Name);
        if (conflictingServiceCf is null)
        {
            return false;
        }

        var conflictingId = conflictingServiceCf.Value<int>("id");
        if (conflictingId == serviceCf.Value<int>("id"))
        {
            return false;
        }

        Transactions.ConflictingCustomFormats.Add(new ConflictingCustomFormat(guideCf, conflictingId));
        return true;
    }

    public void RecordDeletions(IEnumerable<TrashIdMapping> deletedCfsInCache, IEnumerable<JObject> serviceCfs)
    {
        var cfs = serviceCfs.ToList();

        // The 'Where' excludes cached CFs that were deleted manually by the user in Radarr
        // FindRadarrCf() specifies 'null' for name because we should never delete unless an ID is found
        foreach (var del in deletedCfsInCache.Where(del => FindServiceCf(cfs, del.CustomFormatId) != null))
        {
            Transactions.DeletedCustomFormatIds.Add(del);
        }
    }

    private static JObject? FindServiceCf(IReadOnlyCollection<JObject> serviceCfs, ProcessedCustomFormatData guideCf)
    {
        return FindServiceCf(serviceCfs, guideCf.CacheEntry?.CustomFormatId, guideCf.Name);
    }

    private static JObject? FindServiceCf(IReadOnlyCollection<JObject> serviceCfs, int? cfId, string? cfName = null)
    {
        JObject? match = null;

        // Try to find match in cache first
        if (cfId is not null)
        {
            match = serviceCfs.FirstOrDefault(rcf => cfId == rcf.Value<int>("id"));
        }

        // If we don't find by ID, search by name (if a name was given)
        if (match is null && cfName is not null)
        {
            match = serviceCfs.FirstOrDefault(rcf => cfName.EqualsIgnoreCase(rcf.Value<string>("name")));
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
