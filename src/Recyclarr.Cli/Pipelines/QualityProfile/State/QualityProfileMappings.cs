using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.QualityProfile.State;

[SyncStateName("quality-profile-mappings")]
[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "POCO")]
[SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "POCO"
)]
internal record QualityProfileMappings : SyncStateObject, ITrashIdMappings
{
    [JsonPropertyName("TrashIdMappings")]
    public List<TrashIdMapping> Mappings { get; set; } = [];
}
