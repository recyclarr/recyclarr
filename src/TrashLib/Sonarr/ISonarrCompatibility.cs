namespace TrashLib.Sonarr
{
    public interface ISonarrCompatibility
    {
        bool SupportsNamedReleaseProfiles { get; }
        bool ArraysNeededForReleaseProfileRequiredAndIgnored { get; }
        string InformationalVersion { get; }
        string MinimumVersion { get; }
    }
}
