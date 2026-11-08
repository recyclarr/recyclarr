using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetResults;

/// <summary>What the sync does to one managed resource.</summary>
internal enum ResourceAction
{
    [Description("The sync creates the resource; every `current` is null.")]
    Create,

    [Description("At least one managed value changes.")]
    Update,

    [Description("Every managed value already matches.")]
    Unchanged,
}

/// <summary>
/// One value Recyclarr manages: `desired` is the value after the sync and `current` the value in
/// the service, null when the sync creates the resource. `changed` is true when the sync writes a
/// different value; compare with it rather than with `current` and `desired`, because some values
/// are compared semantically. A preview job computes values but writes nothing.
/// </summary>
internal sealed record PlannedValueResponse<T>(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] T Desired,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] T? Current,
    bool Changed
);

/// <summary>A resource in Sonarr or Radarr; `serviceId` is its ID there.</summary>
internal sealed record NamedServiceResourceResponse(string Name, int ServiceId);

internal sealed record TrashIdNameResponse(string TrashId, string Name);
