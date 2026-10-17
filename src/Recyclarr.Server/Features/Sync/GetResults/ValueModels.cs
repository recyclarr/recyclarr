using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetResults;

/// <summary>
/// A changed value: `current` is the value in the service and `desired` the value Recyclarr
/// writes. A preview job computes the change but does not write it.
/// </summary>
internal sealed record ValueChangeResponse<T>(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] T Current,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] T Desired
);

/// <summary>A resource in Sonarr or Radarr; `serviceId` is its ID there.</summary>
internal sealed record NamedServiceResourceResponse(string Name, int ServiceId);

internal sealed record TrashIdNameResponse(string TrashId, string Name);
