using System.Text.RegularExpressions;
using Common.Extensions;
using Newtonsoft.Json.Linq;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Guide;

public class CustomFormatParser : ICustomFormatParser
{
    public CustomFormatData ParseCustomFormatData(string guideData, string fileName)
    {
        var obj = JObject.Parse(guideData);

        var name = obj.ValueOrThrow<string>("name");
        var trashId = obj.ValueOrThrow<string>("trash_id");
        int? finalScore = null;

        if (obj.TryGetValue("trash_score", out var score))
        {
            finalScore = (int) score;
        }

        // Remove any properties starting with "trash_". Those are metadata that are not meant for the remote service
        // itself. The service supposedly drops this anyway, but I prefer it to be removed. ToList() is important here
        // since removing the property itself modifies the collection, and we don't want the collection to get modified
        // while still looping over it.
        foreach (var trashProperty in obj.Properties().Where(x => Regex.IsMatch(x.Name, @"^trash_")).ToList())
        {
            trashProperty.Remove();
        }

        return new CustomFormatData(fileName, name, trashId, finalScore, obj);
    }
}
