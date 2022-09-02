using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrashLib.Services.CustomFormat.Models;

public record CustomFormatData(
    string Name,
    string TrashId,
    int? Score,
    [property: JsonExtensionData] JObject ExtraJson
);
