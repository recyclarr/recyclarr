using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrashLib.Radarr.CustomFormat.Models;

public record CustomFormatData(
    string Name,
    string TrashId,
    int? Score,
    [property: JsonExtensionData] JObject ExtraJson
);
