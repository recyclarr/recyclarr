using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Recyclarr.TrashLib.Services.CustomFormat.Models;

public record CustomFormatData(
    string FileName,
    string Name,
    string TrashId,
    int? Score,
    [property: JsonExtensionData] JObject Json
)
{
    public string? Category { get; init; }
}
