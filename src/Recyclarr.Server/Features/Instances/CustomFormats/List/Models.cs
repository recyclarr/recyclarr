namespace Recyclarr.Server.Features.Instances.CustomFormats.List;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListInstanceCustomFormatsRequest
{
    /// <summary>The instance name from configuration; matching ignores case.</summary>
    public string Name { get; init; } = "";
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListInstanceCustomFormatsResponse(
    IReadOnlyList<InstanceCustomFormatSummaryResponse> Items
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record InstanceCustomFormatSummaryResponse
{
    /// <summary>The custom format's ID in Sonarr or Radarr. Use it to delete the format.</summary>
    public required int Id { get; init; }

    public required string Name { get; init; }
}
