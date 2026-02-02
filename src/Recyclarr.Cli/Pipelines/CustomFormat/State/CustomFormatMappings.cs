using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.CustomFormat.State;

[SyncStateName("custom-format-mappings")]
[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "POCO")]
[SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "POCO"
)]
internal record CustomFormatMappings : SyncStateObject, ITrashIdMappings
{
    [JsonPropertyName("trash_id_mappings")]
    public List<TrashIdMapping> Mappings { get; set; } = [];
}
