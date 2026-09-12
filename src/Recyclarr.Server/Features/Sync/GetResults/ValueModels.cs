using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetResults;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ValueChangeResponse<T>(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] T Current,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] T Desired
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record NamedServiceResourceResponse(string Name, int ServiceId);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record TrashIdNameResponse(string TrashId, string Name);
