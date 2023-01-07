using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.TestLibrary;

public static class NewCf
{
    public static CustomFormatData Data(string name, string trashId, int? score = null)
    {
        return Data(name, trashId, score, JObject.Parse($"{{'name':'{name}'}}"));
    }

    public static CustomFormatData Data(string name, string trashId, int? score, JObject json)
    {
        return new CustomFormatData("", name, trashId, score, json);
    }

    public static ProcessedCustomFormatData ProcessedWithScore(string name, string trashId, int score, JObject json)
    {
        return new ProcessedCustomFormatData(Data(name, trashId, score, json));
    }

    public static ProcessedCustomFormatData ProcessedWithScore(string name, string trashId, int score, int formatId = 0)
    {
        return new ProcessedCustomFormatData(Data(name, trashId, score))
        {
            FormatId = formatId
        };
    }

    public static ProcessedCustomFormatData Processed(string name, string trashId, JObject json)
    {
        return Processed(name, trashId, 0, json);
    }

    public static ProcessedCustomFormatData Processed(string name, string trashId, int formatId = 0)
    {
        return new ProcessedCustomFormatData(Data(name, trashId))
        {
            FormatId = formatId
        };
    }

    public static ProcessedCustomFormatData Processed(string name, string trashId, int formatId, JObject json)
    {
        return new ProcessedCustomFormatData(Data(name, trashId, null, json))
        {
            FormatId = formatId
        };
    }
}
