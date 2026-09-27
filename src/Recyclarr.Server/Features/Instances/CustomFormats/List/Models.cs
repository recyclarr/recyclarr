namespace Recyclarr.Server.Features.Instances.CustomFormats.List;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListInstanceCustomFormatsRequest
{
    public string Name { get; init; } = "";
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListInstanceCustomFormatsResponse(
    IReadOnlyList<InstanceCustomFormatSummaryResponse> Items
);

// Id is the custom format's id in Sonarr or Radarr, the key for DELETE.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record InstanceCustomFormatSummaryResponse(int Id, string Name);
