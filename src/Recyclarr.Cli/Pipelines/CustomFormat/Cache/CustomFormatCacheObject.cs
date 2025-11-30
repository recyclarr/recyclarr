using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Recyclarr.Cache;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

[CacheObjectName("custom-format-cache")]
[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "POCO")]
[SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "POCO"
)]
internal record CustomFormatCacheObject() : CacheObject(1), ITrashIdCacheObject
{
    // JsonPropertyName preserves backward compatibility with existing cache files
    // that use "TrashIdMappings" as the JSON key
    [JsonPropertyName("TrashIdMappings")]
    public List<TrashIdMapping> Mappings { get; set; } = [];
}
