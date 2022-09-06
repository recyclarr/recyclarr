using Newtonsoft.Json.Linq;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;

namespace TrashLib.TestLibrary;

public static class NewCf
{
    public static CustomFormatData Data(string name, string trashId, int? score = null)
    {
        var json = JObject.Parse($"{{'name':'{name}'}}");
        return new CustomFormatData(name, trashId, score, new JObject(json));
    }

    public static ProcessedCustomFormatData Processed(string name, string trashId, int? score = null)
    {
        return new ProcessedCustomFormatData(Data(name, trashId, score));
    }

    public static ProcessedCustomFormatData Processed(string name, string trashId, int? score, JObject json)
    {
        return new ProcessedCustomFormatData(new CustomFormatData(name, trashId, score, json));
    }

    public static ProcessedCustomFormatData Processed(string name, string trashId, JObject json)
    {
        return Processed(name, trashId, null, json);
    }

    public static ProcessedCustomFormatData Processed(string name, string trashId, TrashIdMapping cacheEntry)
    {
        return new ProcessedCustomFormatData(Data(name, trashId))
        {
            CacheEntry = cacheEntry
        };
    }

    public static ProcessedCustomFormatData Processed(string name, string trashId, JObject json,
        TrashIdMapping? cacheEntry)
    {
        return new ProcessedCustomFormatData(new CustomFormatData(name, trashId, null, json))
        {
            CacheEntry = cacheEntry
        };
    }
}
