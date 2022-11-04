namespace TrashLib.Services.Radarr;

public record RadarrCapabilities(Version Version)
{
    public RadarrCapabilities() : this(new Version())
    {
    }
}
