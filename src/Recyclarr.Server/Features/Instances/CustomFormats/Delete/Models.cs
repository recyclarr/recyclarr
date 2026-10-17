namespace Recyclarr.Server.Features.Instances.CustomFormats.Delete;

internal sealed record DeleteInstanceCustomFormatRequest
{
    /// <summary>The instance name from configuration; matching ignores case.</summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The custom format's ID in Sonarr or Radarr, as the instance custom format list returns it.
    /// </summary>
    public int Id { get; init; }
}
