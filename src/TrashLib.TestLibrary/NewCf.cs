using Newtonsoft.Json.Linq;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.TestLibrary;

public static class NewCf
{
    public static CustomFormatData Data(string name, string filename, string trashId, int? score = null)
    {
        var json = JObject.Parse($"{{'name':'{name}'}}");
        return new CustomFormatData(name, filename, trashId, score, new JObject(json));
    }

    public static ProcessedCustomFormatData Processed(string name, string filename, string trashId, int? score = null)
    {
        return new ProcessedCustomFormatData(Data(name, filename, trashId, score));
    }

    public static ProcessedCustomFormatData Processed(string name, string filename, string trashId, int? score,
        JObject json)
    {
        return new ProcessedCustomFormatData(new CustomFormatData(name, filename, trashId, score, json));
    }

    public static ProcessedCustomFormatData Processed(string name, string filename, string trashId, JObject json)
    {
        return Processed(name, filename, trashId, null, json);
    }

    public static ProcessedCustomFormatData Processed(string name, string filename, string trashId,
        TrashIdMapping cacheEntry)
    {
        return new ProcessedCustomFormatData(Data(name, filename, trashId))
        {
            CacheEntry = cacheEntry
        };
    }

    public static ProcessedCustomFormatData Processed(string name, string filename, string trashId, JObject json,
        TrashIdMapping? cacheEntry)
    {
        return new ProcessedCustomFormatData(new CustomFormatData(name, filename, trashId, null, json))
        {
            CacheEntry = cacheEntry
        };
    }
}
