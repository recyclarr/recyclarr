using Newtonsoft.Json;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Guide;

public class CustomFormatParser : ICustomFormatParser
{
    public CustomFormatData ParseCustomFormatData(string guideData, string fileName)
    {
        var cf = JsonConvert.DeserializeObject<CustomFormatData>(guideData);
        if (cf is null)
        {
            throw new JsonSerializationException($"Unable to parse JSON at file {fileName}");
        }

        return cf with {FileName = fileName};
    }
}
