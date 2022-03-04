namespace TrashLib.Sonarr;

public record SonarrCapabilities
{
    public SonarrCapabilities()
    {
        Version = new Version();
    }

    public SonarrCapabilities(Version version)
    {
        Version = version;
    }

    public Version Version { get; }

    public bool SupportsNamedReleaseProfiles { get; init; }

    // Background: Issue #16 filed which points to a backward-breaking API
    // change made in Sonarr at commit [deed85d2f].
    //
    // [deed85d2f]: https://github.com/Sonarr/Sonarr/commit/deed85d2f9147e6180014507ef4f5af3695b0c61
    public bool ArraysNeededForReleaseProfileRequiredAndIgnored { get; init; }
}
