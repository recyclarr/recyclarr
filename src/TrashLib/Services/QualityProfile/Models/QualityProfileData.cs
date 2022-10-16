using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrashLib.Services.QualityProfile.Models;

public record QualityProfileData(
    string FileName,
    string Name,
    int? Score,
    [property: JsonExtensionData] JObject Json
)
{
    public string? Category { get; init; }
}
