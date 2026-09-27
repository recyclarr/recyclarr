namespace Recyclarr.Server.Features.Instances.CustomFormats.Delete;

// Id is the custom format's id in Sonarr or Radarr, as listed by the instance custom formats
// collection.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record DeleteInstanceCustomFormatRequest
{
    public string Name { get; init; } = "";
    public int Id { get; init; }
}
