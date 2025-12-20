using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Recyclarr.Cache;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Cache;

[CacheObjectName("quality-profile-cache")]
[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "POCO")]
[SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "POCO"
)]
internal record QualityProfileCacheObject : CacheObject, ITrashIdCacheObject
{
    [JsonPropertyName("TrashIdMappings")]
    public List<TrashIdMapping> Mappings { get; set; } = [];
}
