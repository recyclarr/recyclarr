namespace TrashLib.Repo;

public record RadarrMetadata(
    IReadOnlyCollection<string> CustomFormats,
    IReadOnlyCollection<string> Qualities
);

public record SonarrMetadata(
    IReadOnlyCollection<string> ReleaseProfiles,
    IReadOnlyCollection<string> Qualities
);

public record JsonPaths(
    RadarrMetadata Radarr,
    SonarrMetadata Sonarr
);

public record RepoMetadata(
    JsonPaths JsonPaths
);
