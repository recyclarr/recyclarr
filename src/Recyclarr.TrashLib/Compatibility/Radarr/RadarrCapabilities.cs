namespace Recyclarr.TrashLib.Compatibility.Radarr;

// ReSharper disable once NotAccessedPositionalProperty.Global
//
// May get used one day; keep the parameter around so that calling
// code does not need to be changed later.
public record RadarrCapabilities(Version? Version)
{
    public RadarrCapabilities()
        : this((Version?) null)
    {
    }
}
